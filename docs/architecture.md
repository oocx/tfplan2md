# Architecture

## Technology Stack
- **.NET 10** - Latest LTS framework
- **C# 13** - Modern language features (records, nullable reference types, pattern matching)
- **Docker** - The application will be delivered as a containerized CLI tool

## Modern C# Patterns (C# 13)
- **Records** for immutable data types: `public record TerraformPlan(string Version, List<Change> Changes);`
- **Pattern Matching**: Use `is` and switch expressions for complex branching
- **Nullable Reference Types**: Enable `<Nullable>enable</Nullable>` in `.csproj`; use `!` operator carefully
- **File-scoped namespaces**: `namespace TfPlan2Md;` at the top of files (not inside blocks)

## Dependency Injection
- Use .NET's built-in `Microsoft.Extensions.DependencyInjection`
- Register services in `Program.cs` or a dedicated extensions class
- Constructor injection is preferred over property injection

## Terraform Plan Parsing
When implementing plan-to-markdown conversion:
- Parse JSON terraform plans (use `System.Text.Json` for .NET 10)
- Handle structured outputs with strongly-typed C# classes
- Support incremental resource processing for large plans

## Error Handling
- Use custom exception types inheriting from `ApplicationException`
- Avoid bare `catch` blocks; always log the exception
- Return meaningful error messages to users, not stack traces

## Build Configuration

### Shared Build Properties
The `Directory.Build.props` file at the repository root defines shared build configuration:
- `TreatWarningsAsErrors`: All warnings are treated as errors
- `EnforceCodeStyleInBuild`: Code style is enforced during build
- `EnableNETAnalyzers`: .NET analyzers are enabled
- `AnalysisLevel`: Set to `latest-recommended` for current best practices
- Microsoft.CodeAnalysis.NetAnalyzers package is included for static analysis

### SDK Version
The `global.json` file pins the .NET SDK version to ensure consistent builds across environments.

### Code Style
The `.editorconfig` file defines comprehensive C# code style rules including:
- Naming conventions (interfaces, types, fields, constants)
- Formatting rules (braces, spacing, indentation)
- Language feature preferences (var, expression-bodied members, pattern matching)
- Analyzer severity configurations

### Local Tools
The `dotnet-tools.json` manifest defines local .NET tools:
- **Husky.Net**: Git hooks for pre-commit validation and commit message linting

## Git Hooks

Pre-commit and commit-msg hooks are configured via Husky.Net (`.husky/` directory):

| Hook | Tasks |
|------|-------|
| `pre-commit` | `dotnet format --verify-no-changes`, `dotnet build` |
| `commit-msg` | Validate Conventional Commits format |
