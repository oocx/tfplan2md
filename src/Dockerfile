# Build stage - use Alpine SDK for musl compatibility
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /workspace

# Copy all files
COPY . .

# Install native toolchain prerequisites for NativeAOT (Alpine uses apk)
RUN apk add --no-cache clang build-base zlib-dev linux-headers bash

# Restore and build (skip tests - Playwright requires glibc)
RUN dotnet restore src/tfplan2md.slnx
RUN dotnet build src/tfplan2md.slnx --no-restore -c Release

# Publish NativeAOT for linux-musl-x64 (requires separate restore with RID and self-contained)
RUN dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -c Release -r linux-musl-x64 --self-contained true -o /app/publish \
	&& rm -f /app/publish/*.dbg

# Runtime stage - Alpine for musl dynamic linker
FROM alpine:3.21 AS base

# Final runtime stage - minimal FROM scratch with only musl dynamic linker
FROM scratch AS runtime
# Only ld-musl is needed; libgcc_s and libstdc++ are statically linked via libstdc++compat.a
COPY --from=base /lib/ld-musl-x86_64.so.1 /lib/

LABEL org.opencontainers.image.source="https://github.com/oocx/tfplan2md" \
      org.opencontainers.image.url="https://github.com/oocx/tfplan2md" \
      org.opencontainers.image.title="tfplan2md" \
      org.opencontainers.image.description="Convert Terraform plan JSON to readable Markdown" \
      org.opencontainers.image.licenses="MIT"

WORKDIR /app
COPY --from=build /app/publish/ .
COPY --from=build /workspace/examples/comprehensive-demo /examples/comprehensive-demo
USER 1654

# Set the entrypoint to the native binary
ENTRYPOINT ["/app/tfplan2md"]
