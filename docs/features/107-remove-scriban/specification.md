# Feature: Remove Scriban and Replace with Pure C# Rendering

## Overview

Remove the Scriban template engine from tfplan2md and replace all `.sbn` template files with
pure C# rendering methods. This eliminates the only third-party NuGet dependency, removes
~1,500 lines of Scriban-specific infrastructure, and makes all rendering errors detectable at
compile time rather than at runtime.

## Background

Scriban was originally added to support **user-customizable templates** — the idea that users
could write their own `.sbn` files to tailor the Markdown output. In practice, this feature
never worked correctly and nobody used it. All templates are now built-in. Since Scriban's
primary value proposition (user extensibility) no longer applies, it has become an internal
abstraction that adds more cost than benefit.

The cost is substantial:
- An `AotScriptObjectMapper` (~683 lines) is required to translate every C# model property
  into `ScriptObject` fields for NativeAOT compatibility
- 57 C# files import Scriban types
- Template variable names are opaque strings — typos produce empty output silently at runtime
- The `TrimmerRootDescriptor.xml` must preserve the entire Scriban assembly (~1.5 MB)
- Provider modules register helpers via `ScriptObject.Import(...)` delegates instead of typed
  C# interfaces

This decision is documented in [ADR-010](../../../docs/adr-010-scriban-removal-evaluation.md).

## User Goals

- The Markdown output produced by `tfplan2md` continues to look exactly the same as before
- Nothing changes from the user's perspective — no CLI flags, no output format changes, no
  new options or removed options

## Scope

### In Scope

- Remove the `Scriban` NuGet package reference from the project
- Delete all 27 `.sbn` template files
- Replace each template with an equivalent C# rendering class (e.g., `RoleAssignmentRenderer`,
  `NsgRenderer`, `DefaultResourceRenderer`, etc.)
- Introduce a `ResourceRendererRegistry` that maps resource type strings to `IResourceRenderer`
  implementations, replacing the template-based dispatch (`TemplateResolver`)
- Remove `AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver`, and the Scriban entries
  in `TrimmerRootDescriptor.xml`
- Retain all existing `ScribanHelpers` formatting logic (diff computation, Azure scope parsing,
  large value formatting, etc.) as regular C# classes — only the Scriban registration glue is
  removed
- Update provider modules to implement typed `IResourceRenderer` interfaces instead of
  registering helpers on `ScriptObject`
- Simplify sensitivity masking: apply at the C# model level or inline during rendering rather
  than as a separate pass over `ScriptObject`/`ScriptArray` trees
- Add the full target architecture document to `docs/features/107-remove-scriban/architecture.md`
- Update `docs/features.md`

### Out of Scope

- Any change to the Markdown output format or report content
- Any new user-facing feature or option
- Re-introducing user-customizable templates in any form
- Removing the rendering helper logic found in the `ScribanHelpers` files (that logic is kept
  but decoupled from Scriban)

## User Experience

This is a pure internal refactoring. From the user's perspective:

- The CLI interface is unchanged
- The Markdown output is byte-for-byte identical to the current output (verified by snapshot
  tests)
- No configuration changes are required

There are no new options, no removed options, and no changes to any public-facing behavior.

## Success Criteria

- [ ] The `Scriban` NuGet package reference is removed from all project files
- [ ] All 27 `.sbn` template files are deleted
- [ ] `AotScriptObjectMapper`, `TemplateLoader`, and `TemplateResolver` are deleted
- [ ] `TrimmerRootDescriptor.xml` no longer contains any Scriban-related entries
- [ ] No C# file in the project imports `using Scriban` or any `Scriban.*` namespace
- [ ] All existing snapshot tests continue to pass without modification to the expected snapshots
- [ ] The NativeAOT binary builds successfully without the Scriban trimmer preservation directive
- [ ] The project has zero third-party NuGet package references after this change
- [ ] All rendering logic is statically typed — no runtime string-based template variable names
- [ ] Provider modules implement a typed `IResourceRenderer` interface (or equivalent) instead
  of `RegisterHelpers(ScriptObject)`

## Open Questions

None — the architecture for this change is fully documented in ADR-010 and this feature’s
`docs/features/107-remove-scriban/architecture.md`.
