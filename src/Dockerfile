# Build stage - use Alpine SDK for musl compatibility
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /workspace

# Copy all files
COPY . .

# Install native toolchain prerequisites for NativeAOT (Alpine uses apk)
RUN apk add --no-cache clang build-base zlib-dev linux-headers bash lld

# Restore and build (skip tests - Playwright requires glibc)
RUN dotnet restore src/tfplan2md.slnx
RUN dotnet build src/tfplan2md.slnx --no-restore -c Release

# Publish NativeAOT for linux-musl-x64 with static musl linking (no dynamic libc dependency)
RUN dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -c Release -r linux-musl-x64 --self-contained true -o /app/publish \
	-p:StaticExecutable=true \
	-p:LinkerFlavor=lld \
	&& rm -f /app/publish/*.dbg

# Final runtime stage - pure scratch, no shared libraries needed (fully static binary)
FROM scratch AS runtime

LABEL org.opencontainers.image.source="https://github.com/oocx/tfplan2md" \
      org.opencontainers.image.url="https://github.com/oocx/tfplan2md" \
      org.opencontainers.image.title="tfplan2md" \
      org.opencontainers.image.description="Convert Terraform plan JSON to readable Markdown" \
      org.opencontainers.image.licenses="MIT"

WORKDIR /app
COPY --from=build /app/publish/ .
USER 1654

# Set the entrypoint to the native binary
ENTRYPOINT ["/app/tfplan2md"]
