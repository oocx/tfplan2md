# ADR-012: Investigate bflat for Smaller Docker Binary

## Status

Rejected

## Context

tfplan2md is distributed as a Docker image built from a fully-static NativeAOT binary targeting `linux-musl-x64`. The current binary is compressed with UPX `--ultra-brute` inside the Dockerfile, resulting in a ~14.7 MB final Docker image using `FROM scratch`. musl was chosen specifically because it produces the smallest static binary with the current NativeAOT approach; any Linux distribution could be used as an alternative.

A question was raised whether [bflat](https://flattened.net/) — a standalone C# compiler built on top of NativeAOT — could produce an even smaller binary.

The project has **zero third-party NuGet dependencies** (Scriban was removed as part of [Feature 107](features/107-remove-scriban)).

### What is bflat?

bflat (https://flattened.net/) is an open-source tool by Michal Strehovsky (a .NET runtime engineer at Microsoft). It bundles the Roslyn C# compiler with the NativeAOT (CoreRT) ahead-of-time compiler into a single portable toolchain that requires no .NET SDK, MSBuild, or NuGet. It can target:

- glibc-based Linux x64/arm64
- Windows x64/arm64
- Android x64/arm64 (API 21+)
- UEFI (bare metal, zero-stdlib only)

bflat offers two standard libraries:

- `DotNet` (default): a fork of the .NET runtime optimized for size
- `Zero`: a minimal stdlib with little more than primitive types, for sub-kilobyte binaries

With flags like `--no-reflection --no-stacktrace-data --no-globalization --no-exception-messages`, bflat binaries with the DotNet stdlib can be as small as ~700 KB for trivial programs.

## Investigation Findings

### Blocker: No musl Support — and Switching to glibc Would Make the Docker Image Larger

The bflat README explicitly states:

> **Support for musl-based Linux is in the works.**

As of the latest stable release (v8.0.2, February 2024) and the pre-release v10.0.0-rc.1 (November 2024), musl targets are **not supported**.

musl is not a hard requirement — a different Linux distro could be used for the Docker base. However, musl is specifically chosen because it produces the smallest static binary and supports `FROM scratch` containers cleanly. Switching to a glibc-based Docker image with bflat would require either:

- Including glibc shared libraries in the image (adds several MB) — the `FROM scratch` approach would be lost, or
- Statically linking against glibc — which produces binaries that are typically *larger* than musl static binaries, and carries known limitations (glibc's NSS plug-ins use `dlopen` at runtime)

Either path would likely increase the final Docker image size compared to the current musl approach. The motivation for investigating bflat was to reduce size, so this is a practical blocker even though musl itself is not mandatory.

### Blocker: No MSBuild / Source Generator Support

bflat intentionally replaces dotnet/MSBuild/NuGet with its own toolchain. The project still depends on MSBuild infrastructure that bflat cannot run:

- **`JsonEmbedGenerator` Roslyn source generator**: Embeds five JSON data files (role definitions, API documentation mappings, icons) as compiled-in C# constants. bflat does not support running Roslyn source generators.
- **`GenerateBuildInfo` MSBuild target**: Generates `BuildInfo.g.cs` at build time with version and commit-hash constants. Would need to be pre-generated and committed, or replicated in a wrapper script.
- **`SetSourceRevisionId` MSBuild target**: Runs `git rev-parse --short HEAD` at build time.

Working around these would require either committing generated files to source (fragile) or writing a pre-build script that replicates the MSBuild targets (high maintenance overhead).

### Blocker: .NET 10 Support Is Pre-Release Only

The project targets .NET 10. bflat's only .NET 10 release is `v10.0.0-rc.1`, a pre-release explicitly listing "Linux on all arches" as **Untested** and with several known issues. Using a pre-release tool in a production CI pipeline is unacceptable.

### Finding: Size Optimizations Already Equivalent

The project already applies every size optimization that bflat enables:

| Optimization | bflat flag | Current `.csproj` setting |
|---|---|---|
| Disable reflection | `--no-reflection` | `<IlcDisableReflection>true</IlcDisableReflection>` |
| No stack trace data | `--no-stacktrace-data` | `<IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>` |
| No globalization | `--no-globalization` | `<InvariantGlobalization>true</InvariantGlobalization>` |
| Size optimization | `-Os` | `<IlcOptimizationPreference>Size</IlcOptimizationPreference>` |
| Strip symbols | `--separate-symbols` | `<StripSymbols>true</StripSymbols>` |
| Full trim | (implicit) | `<TrimMode>full</TrimMode>` |
| Fold identical method bodies | (NativeAOT) | `<IlcFoldIdenticalMethodBodies>true</IlcFoldIdenticalMethodBodies>` |
| No event sources | (implicit) | `<EventSourceSupport>false</EventSourceSupport>` |
| No metrics | (implicit) | `<MetricsSupport>false</MetricsSupport>` |
| No HTTP/3 | (implicit) | `<Http3Support>false</Http3Support>` |
| Size-optimized LINQ | (implicit) | `<UseSizeOptimizedLinq>true</UseSizeOptimizedLinq>` |

Additionally, UPX `--ultra-brute` is applied in the Dockerfile (vs. bflat's recommendation of UPX with default options). The current approach is already at the frontier of what standard NativeAOT can achieve. For a complex real-world program like tfplan2md, bflat's size advantage over standard NativeAOT with equivalent flags is negligible compared to its Hello-World benchmarks.

### Finding: Community Tool Without Stable .NET 10 Release

bflat is a community project, not an official Microsoft product. Version alignment with .NET is manual and periodic: the latest stable release targets .NET 8 (v8.0.2, February 2024), and the .NET 10 pre-release has significant gaps. Taking a dependency on bflat for production builds would mean accepting version lag and less predictable update cadence.

## Decision

**Reject bflat for the Docker image and all release binaries.** The combination of missing musl support, MSBuild/source-generator build complexity, and the absence of a stable .NET 10 release makes bflat impractical. Even if those blockers were resolved, switching from musl to glibc for the Docker build would likely increase total image size, working against the goal that motivated the investigation.

## Alternatives for Further Size Reduction

The investigation and the removal of Scriban (zero NuGet dependencies, no `TrimmerRootDescriptor.xml`) already represent the most impactful size reduction available. Remaining levers, in approximate impact order:

1. **`sizoscope` analysis** ([GitHub](https://github.com/MichalStrehovsky/sizoscope)): Visualizes what contributes to the NativeAOT binary. Running it on the current binary can reveal any remaining large contributors.

2. **No-PIE linking** (`-p:IlcAdditionalLinkerFlags=-no-pie`): For static musl binaries, disabling position-independent executable generation yields a small additional size reduction.

3. **Re-evaluate bflat when musl support ships**: If bflat releases stable musl support alongside a stable .NET 10 release, a re-evaluation would be worthwhile. The MSBuild complexity would still need to be addressed (e.g., pre-generating `BuildInfo.g.cs` and the `JsonEmbedGenerator` output in a wrapper script).

## Consequences

- No changes to the Docker image or release pipeline.
- The investigation is documented here for future reference.
- bflat should be re-evaluated if it ships stable musl support and a stable .NET 10 release.

## References

- bflat repository: https://github.com/bflattened/bflat
- bflat homepage: https://flattened.net/
- bflat v10.0.0-rc.1 release notes: https://github.com/bflattened/bflat/releases/tag/v10.0.0-rc.1
- sizoscope (NativeAOT size analyzer): https://github.com/MichalStrehovsky/sizoscope
- [ADR-010: Evaluate Removing Scriban](adr-010-scriban-removal-evaluation.md)
- [ADR-011: UPX Binary Compression](adr-011-upx-binary-compression.md)
- [Feature 025: AOT-Trimmed Docker Image](features/025-aot-trimmed-image)
- [Feature 107: Remove Scriban](features/107-remove-scriban)
