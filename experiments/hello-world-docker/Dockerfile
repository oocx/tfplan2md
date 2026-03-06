# Build stage - use Alpine SDK for musl compatibility (same as tfplan2md)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /workspace

# Copy project files
COPY HelloWorld/ ./HelloWorld/

# Install native toolchain prerequisites for NativeAOT (same as tfplan2md Dockerfile)
RUN apk add --no-cache upx clang build-base zlib-dev linux-headers bash lld

# Publish NativeAOT for linux-musl-x64 with static musl linking (same flags as tfplan2md)
RUN dotnet publish HelloWorld/HelloWorld.csproj \
    -c Release \
    -r linux-musl-x64 \
    --self-contained true \
    -o /app/publish \
    -p:StaticExecutable=true \
    -p:LinkerFlavor=lld

RUN upx --ultra-brute /app/publish/hello-world

# Final runtime stage - pure scratch (same as tfplan2md)
FROM scratch AS runtime

WORKDIR /app
COPY --from=build /app/publish/hello-world /app/hello-world
USER 1654

ENTRYPOINT ["/app/hello-world"]
