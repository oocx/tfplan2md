# Build stage - use Alpine SDK for musl compatibility
# Manifest list digest (multi-arch: linux/amd64, linux/arm/v7, linux/arm64); obtained via:
#   docker buildx imagetools inspect mcr.microsoft.com/dotnet/sdk:10.0-alpine
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:5c559aa5d99337e400d39ab4fa1f6979d126c29b20939d53658ed38300571e74 AS build
WORKDIR /workspace

# Copy all files
COPY . .

# Install native toolchain prerequisites for NativeAOT (Alpine uses apk)
# Versions pinned for reproducibility against Alpine 3.23. Package versions are identical
# across architectures on Alpine Linux.
# To refresh versions: docker run --rm <base-image> sh -c \
#   "apk info -v upx clang lld build-base zlib-dev linux-headers bash"
RUN apk add --no-cache \
    upx=5.0.2-r0 \
    clang=21.1.2-r2 \
    build-base=0.5-r3 \
    zlib-dev=1.3.2-r0 \
    linux-headers=6.16.12-r0 \
    bash=5.3.3-r1 \
    lld=21.1.2-r1

# Restore and build (skip tests - Playwright requires glibc)
RUN dotnet restore src/tfplan2md.slnx
RUN dotnet build src/tfplan2md.slnx --no-restore -c Release

# Publish NativeAOT with static musl linking (no dynamic libc dependency).
# TARGETARCH is set automatically by Docker Buildx for multi-platform builds.
ARG TARGETARCH
RUN case "${TARGETARCH}" in \
      amd64) \
        dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
          -c Release \
          -r linux-musl-x64 \
          --self-contained true \
          -o /app/publish \
          -p:StaticExecutable=true \
          -p:LinkerFlavor=lld ;; \
      arm64) \
        # TODO: NativeAOT is temporarily disabled for linux-musl-arm64 because .NET 10 Alpine \
        # currently crashes in ILCompiler with "Specified cast is not valid" during docker build. \
        # Periodically retest this path and re-enable AOT once the upstream bug is resolved. \
        dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
          -c Release \
          -r linux-musl-arm64 \
          --self-contained true \
          -o /app/publish \
          -p:PublishAot=false \
          -p:UseAppHost=true ;; \
      *) echo "Unsupported architecture: ${TARGETARCH}" && exit 1 ;; \
    esac

RUN upx --ultra-brute /app/publish/tfplan2md

# Final runtime stage - pure scratch, no shared libraries needed (fully static binary)
FROM scratch AS runtime

LABEL org.opencontainers.image.source="https://github.com/oocx/tfplan2md" \
      org.opencontainers.image.url="https://github.com/oocx/tfplan2md" \
      org.opencontainers.image.title="tfplan2md" \
      org.opencontainers.image.description="Convert Terraform plan JSON to readable Markdown" \
      org.opencontainers.image.licenses="MIT"

WORKDIR /app
COPY --from=build /app/publish/tfplan2md /app/tfplan2md
USER 1654

# Set the entrypoint to the native binary
ENTRYPOINT ["/app/tfplan2md"]
