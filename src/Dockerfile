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

# Runtime stage - Alpine for musl libraries
FROM alpine:3.21 AS base
RUN apk add --no-cache libgcc libstdc++

# Final runtime stage - minimal FROM scratch with only essential musl libraries
FROM scratch AS runtime
# Copy minimal musl libraries for NativeAOT binary
COPY --from=base /lib/ld-musl-x86_64.so.1 /lib/
COPY --from=base /usr/lib/libgcc_s.so.1 /usr/lib/
COPY --from=base /usr/lib/libstdc++.so.6 /usr/lib/

WORKDIR /app
COPY --from=build /app/publish/ .
COPY --from=build /workspace/examples/comprehensive-demo /examples/comprehensive-demo
USER 1654

# Set the entrypoint to the native binary
ENTRYPOINT ["/app/tfplan2md"]
