# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all files
COPY . .

# Restore, build and test
RUN dotnet restore tfplan2md.slnx
RUN dotnet build tfplan2md.slnx --no-restore -c Release
RUN dotnet test tfplan2md.slnx --no-build -c Release

# Publish
RUN dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -c Release -o /app --no-restore

# Runtime stage - using chiseled (distroless) image for minimal attack surface
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .

# Set the entrypoint
ENTRYPOINT ["dotnet", "tfplan2md.dll"]
