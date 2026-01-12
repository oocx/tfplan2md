# Architecture: AOT-Compiled Trimmed Docker Image

## Status

Proposed

## Context

This feature replaces the current .NET runtime-based Docker distribution with an AOT (NativeAOT) + trimmed publish to reduce image size, improve pull/start performance, and reduce attack surface, while preserving all existing user-facing behavior and CLI semantics.

Feature specification: docs/features/037-aot-trimmed-image/specification.md

### Current state (baseline)

- Dockerfile publishes a framework-dependent app and runs it via `dotnet tfplan2md.dll`.
- Runtime image is `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled` (ADR-002).
- Built-in Scriban templates are embedded resources (`MarkdownGeneration/Templates/**/*.sbn`) and loaded via `Assembly.GetManifestResourceStream()`.
- Report metadata reads assembly attributes (informational version and commit hash via `AssemblyMetadataAttribute`).

### Key constraints

- Must keep Scriban template rendering working (ADR-001).
- Must keep reflection-dependent behaviors working:
  - Embedded resource loading
  - Assembly attribute reading (version, commit)
  - Custom template loading from filesystem
- No “parallel build flavor” for users (full replacement).
- Must remain .NET 10 / C# 13.

## Options Considered

### Option 1: Stay framework-dependent + chiseled runtime (no AOT)

Keep current publish + `dotnet tfplan2md.dll` and rely on chiseled runtime image.

- Pros
  - Lowest engineering risk
  - No trimming/reflection hazards
  - Fast build times
- Cons
  - Does not meet the feature intent (AOT + trimming)
  - Larger image than necessary (includes full .NET runtime)

### Option 2: IL trimming only (no AOT)

Publish a trimmed framework-dependent or self-contained app without NativeAOT.

- Pros
  - Less risk than NativeAOT while still reducing size
  - Preserves normal .NET runtime behavior
- Cons
  - Out of scope vs spec (“Enable .NET AOT compilation”)
  - Still requires .NET runtime in-container (or self-contained runtime files)

### Option 3: NativeAOT + `runtime-deps:10.0-noble-chiseled`

Publish a native executable (NativeAOT) for `linux-x64` and run it on a minimal “runtime dependencies only” base image.

- Pros
  - Substantial image size reduction (no managed runtime layer)
  - Keeps a practical baseline of OS/runtime dependencies (glibc, CA certs, etc.)
  - Compatible with chiseled/distroless posture (aligns with ADR-002 direction)
  - More forgiving than `scratch` for typical .NET native executables
- Cons
  - Highest engineering risk: trimming/reflection compatibility (Scriban)
  - Longer CI build times
  - Requires pinning a Linux RID and validating platform deps

### Option 4: NativeAOT + `scratch`

Publish a fully static native executable and run it on `scratch`.

- Pros
  - Smallest possible runtime image
  - Minimal attack surface
- Cons
  - Often impractical unless the binary is fully static and bundles/avoids required runtime assets
  - Debuggability/operability is hardest (no certs/zoneinfo/etc unless copied)
  - Higher risk of runtime failures due to missing libc/ICU/certs

## Decision

Choose **Option 3: NativeAOT + `runtime-deps:10.0-noble-chiseled`**, with an explicit trimming-compatibility strategy for Scriban and the report model.

## Rationale

- `runtime-deps:*` keeps the container extremely small while still providing the baseline Linux dependencies that native .NET executables typically rely on.
- `scratch` is attractive but has a high probability of “death by missing runtime asset” (certificates, timezone data, libc expectations), which is counter to the “transparent migration” requirement.
- The repository already values distroless/chiseled images (ADR-002); `runtime-deps:*` is the closest AOT analogue.

## Key Design Decisions

### 1) Publish model: NativeAOT as the single distribution

- Publish a native executable for **Linux x64 (linux/amd64)** only (matching the current GitHub Actions Docker build environment, which produces an amd64 image).
- Docker entrypoint becomes `./tfplan2md` (not `dotnet tfplan2md.dll`).

Notes:
- This feature does not introduce multi-arch behavior. If the project later wants `linux/arm64`, that is a separate decision because NativeAOT is RID-specific and would require a multi-platform Docker build strategy.

### 2) Trimming compatibility strategy (Scriban + report model)

NativeAOT requires aggressive trimming and has stricter reflection constraints. The risk area is Scriban’s runtime member access on model objects.

Design principle:
- Prefer passing **Scriban-native types** (e.g., `ScriptObject`, `ScriptArray`) to templates where possible, because they avoid reflection and are “known” to the template engine.
- Where templates rely on reflection over POCO/record/class models, ensure **explicit preservation** of required members.

Recommended approach (incremental hardening):
1. Enable NativeAOT publish and run the full test suite.
2. Treat trimmer/AOT warnings as actionable signals; address them by preserving only what is needed.
3. Preserve template-facing models and helpers explicitly (e.g., via trimmer descriptors or `DynamicallyAccessedMembers` annotations).

Trade-off:
- Preserving a larger surface area reduces trimming benefits, but is preferable to runtime failures.

### 3) Embedded resource templates remain the source of built-ins

- Keep templates embedded as resources (already in csproj), loaded via `Assembly.GetManifestResourceStream()`.
- NativeAOT should still support manifest resources; the primary risk is not resources themselves, but trimming/reflection around the types involved in rendering.

### 4) Assembly metadata for version/commit remains authoritative

- Keep reading informational version and commit hash from assembly attributes.
- Ensure build embeds commit hash at build time (already done via `AssemblyMetadata` in the main csproj).

### 5) Globalization: prefer invariant unless proven needed

- Default recommendation: enable invariant globalization **if and only if** tests and demo rendering confirm no regressions.
- Rationale: tfplan2md output is primarily ASCII/Unicode text formatting with invariant casing/formatting; culture-sensitive behaviors appear unnecessary and would increase runtime dependencies.

## Consequences

### Positive

- Smaller Docker images and faster pulls.
- Reduced runtime attack surface compared to shipping the full runtime.
- Potential startup improvements in CI/CD.

### Negative / Risks

- **Primary risk:** Scriban + reflection under trimming/AOT may cause runtime failures if required members are trimmed.
- CI build time will increase.
- RID specificity: the Docker image will effectively be tied to `linux-x64` unless the release workflow is expanded.

## Implementation Notes (for Developer)

- Update Docker build to publish NativeAOT for `linux-x64` and run on `mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled`.
- Replace the runtime entrypoint with the native executable.
- Add a trimming compatibility mechanism for template-facing types (start broad, then tighten):
  - Preserve members used by templates (report model + nested models + helper-exposed types).
  - Avoid “preserve everything” unless necessary.
- Ensure the existing template architecture tests and markdown invariant tests run against the AOT build.
- Capture and document metrics requested in the specification (image size and build time before/after).
