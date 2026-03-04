# Issue: Binary builds failed (NETSDK1207 from JsonEmbedGenerator)

## Problem Description

The release workflow’s binary build jobs fail for every target RID (Linux/macOS/Windows, glibc and musl) during the `dotnet publish` step.

The failing run reported by Maintainer:
- Run: https://github.com/oocx/tfplan2md/actions/runs/22680751597
- Job: https://github.com/oocx/tfplan2md/actions/runs/22680751597/job/65750270770

## Steps to Reproduce

### In CI
1. Trigger the release workflow (tag push or `workflow_dispatch`).
2. Observe any `Build * Binary` job fail at the `Build Binary` step.

### Locally (Linux)
From repo root:

```bash
mkdir -p .tmp/aot-test
dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishAot=true \
  -o .tmp/aot-test
```

## Expected Behavior

All binary targets build successfully and get packaged/uploaded by the release workflow.

## Actual Behavior

The `dotnet publish` step fails quickly with:

- `error NETSDK1207: Ahead-of-time compilation is not supported for the target framework. [src/tools/JsonEmbedGenerator/JsonEmbedGenerator.csproj]`

This occurs across all platforms/RIDs in the release workflow.

## Root Cause Analysis

### Affected Components

- Analyzer/source generator project:
  - `TargetFramework` is `netstandard2.0` in [src/tools/JsonEmbedGenerator/JsonEmbedGenerator.csproj](src/tools/JsonEmbedGenerator/JsonEmbedGenerator.csproj)
- Main executable project references the generator as an analyzer:
  - Project reference in [src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj](src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj#L40-L47)
- Release workflow publishes with AOT enabled:
  - `-p:PublishAot=true` is passed in [ .github/workflows/release.yml ](.github/workflows/release.yml#L326-L350)

### What’s Broken

When the release workflow runs `dotnet publish` with `PublishAot=true`, that property is applied during the build and flows into project references used as analyzers.

`JsonEmbedGenerator` targets `netstandard2.0`, and the .NET SDK (10.0.103 in the failing run) errors out when AOT is enabled for a target framework that doesn’t support it, producing `NETSDK1207`. This halts publishing before actual AOT compilation of the main executable.

### Why It Happened

Recent changes integrated the `JsonEmbedGenerator` source generator into the main project, while the main project is configured/published with NativeAOT (`PublishAot`). This combination causes the AOT publish property to affect the analyzer project build.

A likely introduction point is commit `03412b35` ("finalize reflection-free embedded json generator integration"), which added/solidified the analyzer project reference.

## Suggested Fix Approach

High-confidence fix direction: ensure `PublishAot` does **not** flow into the analyzer project build.

Potential approaches (ordered by likely best fit):

1. **Strip AOT-related global properties for the analyzer project reference**
   - Update the `ProjectReference` to `JsonEmbedGenerator` in [src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj](src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj#L40-L47) to remove `PublishAot` from the properties passed when building that referenced analyzer project.
   - In MSBuild this is typically done via `GlobalPropertiesToRemove` (and possibly removing other publish-scoped properties like `RuntimeIdentifier`/`SelfContained` if needed).

2. **Conditionally change the generator’s target framework when `PublishAot` is enabled**
   - Example idea: keep `netstandard2.0` for normal builds, but switch to a `.NETCoreApp` target framework when `PublishAot=true` to satisfy the SDK’s AOT validation.
   - This is higher risk because it changes analyzer compatibility characteristics.

3. **Adjust release workflow to avoid applying `PublishAot` broadly**
   - If feasible, restructure the publish step so that AOT is applied only to the executable publish and not to referenced analyzer projects.
   - This may be hard because `dotnet publish` sets global properties by design.

## Related Tests / Verification

After applying the fix:
- Local repro command above should no longer throw `NETSDK1207`.
- Release workflow binary jobs should succeed for all targets.

## Additional Context

Investigation note: `scripts/check-workflow-status.sh logs --step "Build Binary"` currently fails because the script passes a string to `gh run view --log-failed`, but the installed `gh` expects `--log-failed` to be boolean. This didn’t affect root-cause identification but made step-scoped log retrieval harder.
