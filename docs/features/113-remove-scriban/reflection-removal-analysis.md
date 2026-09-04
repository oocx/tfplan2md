# Reflection Removal Analysis (NativeAOT + Trimming)

## Goal

Eliminate all **production** runtime reflection usage so the NativeAOT build can plausibly enable `IlcDisableReflection=true`, improve trimming predictability, and reduce binary size.

This document inventories the current reflection usages and proposes alternative approaches.

## Reflection Usage Inventory (Production)

Immediately after Scriban removal, production reflection usage was limited to:

1) **Assembly metadata / version**
- CLI `--version` output:
  - [src/Oocx.TfPlan2Md/ProgramEntry.cs](../../../src/Oocx.TfPlan2Md/ProgramEntry.cs)
- Report metadata (version + commit hash):
  - [src/Oocx.TfPlan2Md/MarkdownGeneration/IMetadataProvider.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/IMetadataProvider.cs)
- Tool version strings:
  - [src/tools/Oocx.TfPlan2Md.HtmlRenderer/VersionProvider.cs](../../../src/tools/Oocx.TfPlan2Md.HtmlRenderer/VersionProvider.cs)
  - [src/tools/Oocx.TfPlan2Md.TerraformShowRenderer/VersionProvider.cs](../../../src/tools/Oocx.TfPlan2Md.TerraformShowRenderer/VersionProvider.cs)
  - [src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/VersionProvider.cs](../../../src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/VersionProvider.cs)

2) **Embedded resource loading via `Assembly.GetManifestResourceStream(...)`**
- Azure role definitions registry:
  - [src/Oocx.TfPlan2Md/Platforms/Azure/AzureRoleDefinitionMapper.Roles.cs](../../../src/Oocx.TfPlan2Md/Platforms/Azure/AzureRoleDefinitionMapper.Roles.cs)
- AzAPI documentation mappings:
  - [src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.Loader.cs](../../../src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.Loader.cs)
- Icon rules JSON:
  - [src/Oocx.TfPlan2Md/MarkdownGeneration/Services/FileBasedIconProvider.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Services/FileBasedIconProvider.cs)

## Non-Production Reflection (Tests)

Unit tests use reflection to access non-public methods and emit test assemblies:
- [src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AssemblyMetadataProviderTests.cs](../../../src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AssemblyMetadataProviderTests.cs)
- [src/tests/Oocx.TfPlan2Md.TUnit/TerraformShowRenderer/DiffRendererHelperTests.cs](../../../src/tests/Oocx.TfPlan2Md.TUnit/TerraformShowRenderer/DiffRendererHelperTests.cs)

This does not impact published binary size, but these tests would need updating if production code removes the underlying reflection-based APIs.

## Why This Blocks `IlcDisableReflection=true`

Even though the current reflection usage is narrow, `IlcDisableReflection=true` is intentionally strict: it assumes reflection metadata can be removed aggressively and reflection APIs may be unsupported at runtime. Any direct dependence on `System.Reflection` types and APIs (including assembly attribute queries and manifest resource loading) is likely to become a runtime failure.

The project currently keeps reflection working by rooting some attribute types in:
- [src/Oocx.TfPlan2Md/TrimmerRootDescriptor.xml](../../../src/Oocx.TfPlan2Md/TrimmerRootDescriptor.xml)

## Options To Remove Reflection Completely

### Option A: Build-time generated constants for version + commit (recommended)

Replace:
- `Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()`
- `Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()`

With:
- a generated `internal static class BuildInfo { internal const string Version = "..."; internal const string CommitHash = "..."; }`

**How to generate**
- Use an MSBuild target to write a small `BuildInfo.g.cs` into `obj/` at build time.
- Inputs:
  - `$(Version)`
  - `$(SourceRevisionId)` (already produced in [src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj](../../../src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj))

**Pros**
- Eliminates all reflection needed for version and commit hash.
- Makes trimming deterministic; no trimmer roots needed for these attributes.
- No runtime I/O.

**Cons**
- Requires build-time codegen wiring in each project that prints a version (main + tools).

### Option B: Replace embedded resources with generated `ReadOnlySpan<byte>` payloads (recommended)

Replace `Assembly.GetManifestResourceStream(resourceName)` for JSON inputs with a generated `ReadOnlySpan<byte>` (or `byte[]`) containing the JSON bytes.

**How to generate**
- Add a build step that converts JSON files into a `.g.cs` file with:
  - `internal static ReadOnlySpan<byte> AzureRoleDefinitionsJson => "..."u8;` (UTF-8 string literal) for smaller resources, or
  - base64 + decode for very large resources, or
  - optionally gzip-compressed payload + decompression at startup.

Then deserialize with `JsonSerializer.Deserialize(ReadOnlySpan<byte>, <source-gen context>)`.

**Pros**
- Completely removes `Assembly` and manifest resource reflection usage.
- Keeps single-binary distribution (no extra files at runtime).
- Works well with NativeAOT.

**Cons**
- Large payloads increase IL size; compression reduces size but adds CPU at startup.
- Requires a small amount of build plumbing and a clear update workflow when JSON changes.

### Option C: Ship JSON as external content files

Mark the JSON files as publish content and read them via `AppContext.BaseDirectory`.

**Pros**
- No reflection and no codegen.
- Potentially smaller binary (data stays out of the executable).

**Cons**
- Changes distribution model: published output contains additional files.
- Must ensure single-file/self-contained packaging behavior remains acceptable.
- Requires robust path discovery and error messages.

### Option D: `.resx` + `ResourceManager`

Store JSON in `.resx` and access via strongly typed resources.

**Pros**
- Familiar tooling.

**Cons**
- Still assembly/resource metadata-driven; likely not compatible with strict reflection disabling goals.
- Not recommended if the end state is truly “no reflection”.

## Recommendation

Use a combination of:

- **Option A** for all version/commit metadata across the main app and tool projects.
- **Option B** for embedded JSON inputs (role definitions, icon rules, AzAPI documentation mappings).

This preserves a single-binary distribution while removing all direct `System.Reflection` usage from production projects.

## Decision

Target **Option B (generated in-binary payloads)** for embedded JSON inputs to keep a single-binary publish while eliminating reflection.

## Implementation Notes (for Developer Agent)

1) Replace metadata providers
- Introduce a build-generated `BuildInfo` in:
  - main app project
  - each tool project that prints a version
- Update call sites:
  - [src/Oocx.TfPlan2Md/ProgramEntry.cs](../../../src/Oocx.TfPlan2Md/ProgramEntry.cs)
  - [src/Oocx.TfPlan2Md/MarkdownGeneration/IMetadataProvider.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/IMetadataProvider.cs)
  - tool `VersionProvider` classes

2) Replace manifest-resource loaders
- Replace resource-stream loaders in:
  - [src/Oocx.TfPlan2Md/Platforms/Azure/AzureRoleDefinitionMapper.Roles.cs](../../../src/Oocx.TfPlan2Md/Platforms/Azure/AzureRoleDefinitionMapper.Roles.cs)
  - [src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.Loader.cs](../../../src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.Loader.cs)
  - [src/Oocx.TfPlan2Md/MarkdownGeneration/Services/FileBasedIconProvider.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Services/FileBasedIconProvider.cs)

3) Trimming / AOT settings follow-up
- After production code removes reflection:
  - remove `TrimmerRootDescriptor` entries that existed only for attribute reflection
  - set `IlcDisableReflection=true` in the NativeAOT project (if compatible with dependencies)

4) Tests
- Update tests that validate reflection-based code paths (metadata provider tests, etc.) to validate the new constant-based behavior.

## Open Question

Resolved: use **Option B (generated payloads)**.
