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
