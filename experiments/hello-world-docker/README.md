# Hello World Docker Image Size Experiment

This experiment measures the lower bound of a .NET Docker image using the **exact same** build
process and NativeAOT optimizations as `tfplan2md`, but with the smallest possible program:
a single `Console.WriteLine("Hello World")`.

## Purpose

`tfplan2md`'s Docker image is approximately 14.7 MB (compressed with UPX). This experiment
answers: *how much of that is .NET runtime overhead vs. application code?*

The result gives the theoretical minimum image size achievable with the current setup.

## Code

`Program.cs` contains exactly one line:

```csharp
System.Console.WriteLine("Hello World");
```

## Project Settings

`HelloWorld.csproj` uses **identical** NativeAOT size-optimization settings as `tfplan2md`:

| Setting | Value |
|---|---|
| `PublishAot` | `true` |
| `IlcDisableReflection` | `true` |
| `TrimMode` | `full` |
| `StripSymbols` | `true` |
| `InvariantGlobalization` | `true` |
| `IlcOptimizationPreference` | `Size` |
| `StackTraceSupport` | `false` |
| `IlcFoldIdenticalMethodBodies` | `true` |
| `EventSourceSupport` | `false` |
| `UseSizeOptimizedLinq` | `true` |
| `MetricsSupport` | `false` |
| `Http3Support` | `false` |
| `IlcGenerateStackTraceData` | `false` |
| `UseSystemResourceKeys` | `true` |
| `HttpActivityPropagationSupport` | `false` |
| `MetadataUpdaterSupport` | `false` |

## Docker Build Process

The `Dockerfile` mirrors the `tfplan2md` build exactly:

1. **Base image**: `mcr.microsoft.com/dotnet/sdk:10.0-alpine` (musl-compatible .NET SDK)
2. **Native toolchain**: `clang`, `lld`, `build-base` (required for NativeAOT static linking)
3. **Publish**: `dotnet publish -r linux-musl-x64 -p:StaticExecutable=true -p:LinkerFlavor=lld`
4. **Compression**: `upx --ultra-brute` (same as tfplan2md)
5. **Runtime image**: `FROM scratch` (zero OS overhead)

## How to Run

```bash
cd experiments/hello-world-docker

# Build the image
docker build -t hello-world-experiment .

# Measure image size
docker images hello-world-experiment

# Run to verify it works
docker run --rm hello-world-experiment
```

## Expected Results

| Metric | Value |
|---|---|
| Raw NativeAOT binary (before UPX) | ~1.2 MB |
| After `upx --ultra-brute` | ~0.5–0.7 MB |
| Final Docker image (FROM scratch) | ~0.5–0.7 MB |

Compared to `tfplan2md` (~14.7 MB compressed), this tells us roughly how much of the image
size is pure .NET runtime infrastructure vs. actual application code and embedded assets.

## Interpretation

The difference between this Hello World image and the `tfplan2md` image represents the cost of:

- Scriban template engine (the only external dependency)
- ~27 Scriban templates embedded as resources
- ~5 JSON data files embedded as resources (Azure role definitions, API mappings, icons)
- JSON parsing infrastructure (for Terraform plan files)
- All the tfplan2md business logic and markdown generation code
