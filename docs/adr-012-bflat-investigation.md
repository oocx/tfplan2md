# ADR-012: Investigate bflat for Smaller Docker Binary

## Status

Partially Validated — Promising for Standalone Binaries; Docker Blocked by Dynamic glibc Linking

## Context

tfplan2md is distributed as a Docker image built from a fully-static NativeAOT binary. The Dockerfile uses Alpine/musl for the build stage because it produces the smallest static binary with the current NativeAOT approach, but any Linux distribution is acceptable as a build base. The runtime stage uses `FROM scratch` with a fully-static binary. A question was raised whether [bflat](https://flattened.net/) — a standalone C# compiler built on top of NativeAOT — could produce an even smaller binary.

The project has **zero third-party NuGet dependencies** (Scriban was removed as part of [Feature 107](features/107-remove-scriban)).

### What is bflat?

bflat (https://flattened.net/) is an open-source tool by Michal Strehovsky (a .NET runtime engineer at Microsoft). It bundles the Roslyn C# compiler with the NativeAOT (CoreRT) ahead-of-time compiler into a single portable toolchain that requires no .NET SDK, MSBuild, or NuGet. It can target:

- glibc-based Linux x64/arm64
- Windows x64/arm64
- Android x64/arm64 (API 21+)
- UEFI (bare metal, zero-stdlib only)

bflat v10.0.0-rc.1 (pre-release) is the only available build targeting .NET 10.

## Experiment

### Setup

The experiment was conducted against `v10.0.0-rc.1` (linux-glibc-x64) on the same Ubuntu host used for standard CI builds.

**Build process:**

1. `dotnet build src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -c Release -p:EmitCompilerGeneratedFiles=true`
   — triggers `JsonEmbedGenerator` (Brotli-compressed JSON embeds) and `System.Text.Json` source generators, writing all generated `.cs` files to `obj/Release/net10.0/generated/`.
2. `BuildInfo.g.cs` is produced by the `GenerateBuildInfo` MSBuild target as part of the same step.
3. All hand-written and generated `.cs` files (280 total) are collected and passed to `bflat build` along with static brotli libraries (`libbrotlidec.a`, `libbrotlienc.a`, `libbrotlicommon.a`) — necessary because the generated JSON embed classes use `BrotliStream` at runtime and bflat does not bundle the native Brotli implementation.
4. Size optimization flags used: `-Os --no-stacktrace-data --no-globalization --no-exception-messages --no-debug-info --no-pie --separate-symbols`.

Note: the `--no-reflection` flag produces a binary that is ~28% smaller than the dotnet NativeAOT baseline, but crashes at runtime with `TypeInitialization_Type_NoTypeAvailable`. This is a known compatibility issue between bflat's aggressive reflection removal and `System.Text.Json` source-generated contexts. Without `--no-reflection`, the project works correctly.

### Results

All measurements are for `linux-x64` (glibc). The baseline is `dotnet publish -r linux-x64` with the current `.csproj` size optimization settings.

| Build | Pre-UPX | UPX `--best` | UPX `--ultra-brute` |
|---|---|---|---|
| dotnet NativeAOT (baseline) | 5.9 MB | 2.6 MB | 2.1 MB |
| bflat v10.0.0-rc.1 (with reflection) | **4.7 MB (−20%)** | **2.0 MB (−20%)** | **1.7 MB (−19%)** |
| bflat v10.0.0-rc.1 (--no-reflection) | 4.2 MB (−28%) — *broken* | — | — |

The bflat binary was tested functionally and produces correct output for all test inputs.

### Linkage Constraint: Dynamic glibc

bflat's Linux target links dynamically against glibc (`libc.so.6`, `libdl.so.2`, `libm.so.6`, `libpthread.so.0`) by default. Passing `-static` to the linker does not override this, because bflat's linker stubs force dynamic glibc linkage as part of its linker script. The resulting binary **requires a glibc runtime environment** and cannot run in a `FROM scratch` container.

This is the critical constraint for the Docker use case:

| Approach | Docker image size |
|---|---|
| Current (musl static, FROM scratch) | ~14.7 MB |
| bflat (glibc dynamic) + distroless/base (~20 MB) | ~25 MB — *larger* |
| bflat (glibc dynamic) + debian:12-slim (~75 MB) | ~80 MB — *much larger* |
| bflat (musl static) + FROM scratch | ~1.6 MB — *not yet possible* |

Switching to a glibc base image would make the Docker image larger than the current approach, not smaller. The size advantage bflat provides over NativeAOT (~20% pre-UPX) is negated by the larger base image required.

## Decision

**bflat is not adopted for the Docker image at this time**, because:

1. **bflat does not support musl** (stated as "in the works" in the README as of v10.0.0-rc.1). Without musl, the binary requires a glibc runtime environment, which means a larger Docker base image — the total image size would increase from ~14.7 MB to ~25 MB.

2. **The pre-release status** of bflat's .NET 10 support (`v10.0.0-rc.1`) makes production CI integration premature.

**bflat is promising for standalone binary distribution** (GitHub Release assets), where the Docker base image constraint does not apply. The standalone binaries would be ~20% smaller across all platforms, improving download speed.

## When to Revisit

Revisit this ADR when:

1. **bflat ships stable musl support** (which would allow `FROM scratch` and a smaller Docker image than the current approach).
2. **bflat ships a stable .NET 10 release** (v10.0.0 or later).
3. **The `--no-reflection` issue is resolved** — either by a bflat fix or by identifying which type initializer fails and adding a targeted workaround.

## Build Integration Notes

For future integration, the build process in the Dockerfile and release workflow would change as follows:

1. **Pre-step**: Run `dotnet build -p:EmitCompilerGeneratedFiles=true` to produce source generator output in `obj/`.
2. **bflat invocation**: Collect all `.cs` files (source + generated) and run `bflat build` with static brotli, size flags, and `--langversion latest`.
3. **Brotli requirement**: bflat does not bundle native Brotli. Static libraries (`libbrotlidec.a`, `libbrotlienc.a`, `libbrotlicommon.a`) must be installed and passed to the linker.
4. **`GenerateBuildInfo` target**: Currently implemented as an MSBuild target; must either be run via `dotnet build` first (then the generated `.cs` file included), or re-implemented as a pre-build shell script.

## Alternatives for Further Size Reduction (Without bflat)

1. **`sizoscope` analysis** ([GitHub](https://github.com/MichalStrehovsky/sizoscope)): Visualizes what contributes to the NativeAOT binary. Can identify remaining large contributors now that Scriban has been removed.
2. **No-PIE linking** (`-p:IlcAdditionalLinkerFlags=-no-pie`): For static musl binaries, disabling position-independent executable generation yields a small additional size reduction.

## Consequences

- No changes to the Docker image or release pipeline for now.
- The investigation confirms that bflat does produce meaningfully smaller binaries (~20%) and is technically feasible for this project with a pre-build source generation step.
- When bflat ships musl support, the Docker image could shrink from ~14.7 MB to approximately ~1.7 MB (bflat binary + UPX + FROM scratch).
- Standalone binary distribution could adopt bflat sooner for a ~20% size reduction.

## References

- bflat repository: https://github.com/bflattened/bflat
- bflat homepage: https://flattened.net/
- bflat v10.0.0-rc.1 release notes: https://github.com/bflattened/bflat/releases/tag/v10.0.0-rc.1
- sizoscope (NativeAOT size analyzer): https://github.com/MichalStrehovsky/sizoscope
- [ADR-010: Evaluate Removing Scriban](adr-010-scriban-removal-evaluation.md)
- [ADR-011: UPX Binary Compression](adr-011-upx-binary-compression.md)
- [Feature 025: AOT-Trimmed Docker Image](features/025-aot-trimmed-image)
- [Feature 107: Remove Scriban](features/107-remove-scriban)
