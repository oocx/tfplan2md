# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all files
COPY . .

# Install native toolchain prerequisites for NativeAOT
RUN apt-get update \
	&& apt-get install -y --no-install-recommends clang zlib1g-dev \
	&& rm -rf /var/lib/apt/lists/*

# Restore and run tests first (RID-agnostic for test compatibility)
RUN dotnet restore tfplan2md.slnx
RUN dotnet build tfplan2md.slnx --no-restore -c Release
RUN dotnet test tfplan2md.slnx --no-build -c Release

# Publish NativeAOT for linux-x64 (requires separate restore with RID and self-contained)
RUN dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -c Release -r linux-x64 --self-contained true -o /app/publish \
	&& rm -f /app/publish/*.dbg

# Runtime stage - NativeAOT on chiseled runtime-deps for minimal footprint
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS base

# Final runtime stage - minimal FROM scratch with only essential libraries
FROM scratch AS runtime
# Copy only essential libraries for NativeAOT binary
COPY --from=base /lib/x86_64-linux-gnu/ld-linux-x86-64.so.2 /lib/x86_64-linux-gnu/
COPY --from=base /lib/x86_64-linux-gnu/libc.so.6 /lib/x86_64-linux-gnu/
COPY --from=base /lib/x86_64-linux-gnu/libgcc_s.so.1 /lib/x86_64-linux-gnu/
COPY --from=base /lib/x86_64-linux-gnu/libstdc++.so.6 /lib/x86_64-linux-gnu/
COPY --from=base /lib/x86_64-linux-gnu/libm.so.6 /lib/x86_64-linux-gnu/
# Copy dynamic linker symlink directory
COPY --from=base /lib64 /lib64

WORKDIR /app
COPY --from=build /app/publish/ .
COPY examples/comprehensive-demo /examples/comprehensive-demo
USER 1654

# Set the entrypoint to the native binary
ENTRYPOINT ["/app/tfplan2md"]
