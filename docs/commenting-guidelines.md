# Code Commenting Guidelines

This document defines the standards for code comments in the tfplan2md project. Well-written comments make code more maintainable and help both human developers and AI agents understand and reason about the codebase.

## Core Principles

1. **Comments should explain "why", not "what"**
   - The code itself shows *what* it does
   - Comments should explain *why* a particular approach was chosen
   - Provide context that cannot be inferred from reading the code alone

2. **All class members must be documented**
   - Public, internal, and private members all require comments
   - Even if access is restricted, comments help maintainers understand design decisions

3. **Comments must add value**
   - Don't repeat what's already obvious from the code
   - Provide additional context, reasoning, or constraints
   - Link to relevant specifications, ADRs, or features when applicable

4. **Keep comments synchronized with code**
   - Update comments whenever code changes
   - Outdated comments are worse than no comments

## XML Documentation Comments

### Required Elements

All XML documentation comments must use C# triple-slash (`///`) syntax and include appropriate XML tags.

#### Classes and Types

```csharp
/// <summary>
/// Parses Terraform plan JSON files and extracts resource changes.
/// </summary>
/// <remarks>
/// This parser follows the Terraform JSON plan format specification v1.2.
/// It uses System.Text.Json for parsing due to better performance on large files
/// compared to Newtonsoft.Json (benchmark: see ADR-003).
/// </remarks>
/// <seealso cref="ResourceChange"/>
internal sealed class TerraformPlanParser
{
    // Implementation
}
```

**Required tags:**
- `<summary>` - Brief description of the type's purpose (one sentence preferred)
- `<remarks>` (optional but recommended) - Additional context, design decisions, or usage notes

**Optional tags:**
- `<seealso>` - References to related types
- `<example>` - Usage examples for complex types

#### Methods

```csharp
/// <summary>
/// Parses a Terraform plan JSON file and returns the resource changes.
/// </summary>
/// <param name="planFilePath">Absolute path to the Terraform plan JSON file.</param>
/// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
/// <returns>A collection of resource changes found in the plan.</returns>
/// <exception cref="FileNotFoundException">Thrown when the plan file does not exist.</exception>
/// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
/// <remarks>
/// This method uses streaming deserialization to handle large plan files efficiently.
/// Memory usage remains constant regardless of file size.
/// Related feature: Comprehensive Demo (docs/features/008-comprehensive-demo/)
/// </remarks>
internal async Task<IReadOnlyList<ResourceChange>> ParseAsync(
    string planFilePath,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

**Required tags:**
- `<summary>` - Clear description of what the method does
- `<param>` - Document each parameter (what it represents, constraints, valid values)
- `<returns>` - Describe the return value (what it contains, when it's empty, etc.)

**Conditional tags:**
- `<exception>` - Document all exceptions that can be thrown (use when method can throw)
- `<remarks>` - Additional context (use when there are important design decisions or references)
- `<example>` - Usage examples (use for complex methods or public APIs)

#### Properties

```csharp
/// <summary>
/// Gets the type of resource change (create, update, delete, no-op).
/// </summary>
/// <value>
/// A string representing the change action as defined in the Terraform plan.
/// </value>
internal string Action { get; init; }

/// <summary>
/// Gets whether sensitive values should be displayed in the output.
/// </summary>
/// <value>
/// <c>true</c> to show sensitive values; <c>false</c> to mask them.
/// Default is <c>false</c> for security.
/// </value>
/// <remarks>
/// This flag affects how the Scriban templates render resource attributes.
/// When false, values marked as sensitive in the plan are replaced with "[REDACTED]".
/// </remarks>
internal bool ShowSensitive { get; init; }
```

**Required tags:**
- `<summary>` - What the property represents
- `<value>` - Description of the property value, valid range, default value if applicable

**Optional tags:**
- `<remarks>` - Additional context about when/why to use this property

#### Fields

```csharp
/// <summary>
/// Default template name used when no custom template is specified.
/// </summary>
private const string DefaultTemplateName = "default.scriban";

/// <summary>
/// Cache of compiled Scriban templates to avoid recompilation.
/// </summary>
/// <remarks>
/// Templates are cached by their file path. Cache is thread-safe and uses
/// ConcurrentDictionary for lock-free reads.
/// </remarks>
private readonly ConcurrentDictionary<string, Template> _templateCache;
```

**Required tags:**
- `<summary>` - Purpose of the field

**Optional tags:**
- `<remarks>` - Important implementation details (threading, caching strategy, etc.)

### Advanced XML Tags

#### Cross-References

Use `<see>` for inline references and `<seealso>` for related items:

```csharp
/// <summary>
/// Maps Azure role definition IDs to human-readable names.
/// See <see cref="AzureScopeParser"/> for scope parsing logic.
/// </summary>
/// <seealso cref="IAzureRoleMapper"/>
/// <seealso cref="NullPrincipalMapper"/>
internal sealed class AzureRoleDefinitionMapper
{
    // Implementation
}
```

#### Code Examples

Use `<example>` with `<code>` tags for usage demonstrations:

```csharp
/// <summary>
/// Formats firewall rules in a before/after comparison format.
/// </summary>
/// <example>
/// <code>
/// var formatter = new FirewallRuleFormatter();
/// var result = formatter.Format(beforeRules, afterRules);
/// Console.WriteLine(result);
/// // Output:
/// // Before: Allow 80, 443
/// // After:  Allow 80, 443, 8080
/// </code>
/// </example>
internal string Format(IReadOnlyList<string> beforeRules, IReadOnlyList<string> afterRules)
{
    // Implementation
}
```

#### Inline Code References

Use `<c>` for inline code terms:

```csharp
/// <summary>
/// Sets the output format to either <c>markdown</c> or <c>json</c>.
/// </summary>
internal string OutputFormat { get; set; }
```

#### Lists and Tables

Use `<list>` for structured information:

```csharp
/// <summary>
/// Validates the Terraform plan format version.
/// </summary>
/// <remarks>
/// Supported versions:
/// <list type="bullet">
/// <item><description>1.0 - Initial plan format</description></item>
/// <item><description>1.1 - Added provider metadata</description></item>
/// <item><description>1.2 - Added sensitive value markers (current)</description></item>
/// </list>
/// </remarks>
internal void ValidateVersion(string version)
{
    // Implementation
}
```

## Implementation Comments (Non-XML)

For inline comments within method bodies, use `//` single-line comments:

### When to Use Implementation Comments

1. **Explaining non-obvious algorithms**
   ```csharp
   // Use binary search since role definitions are sorted by ID (O(log n))
   var index = Array.BinarySearch(roleDefinitions, targetId);
   ```

2. **Documenting workarounds or constraints**
   ```csharp
   // WORKAROUND: System.Text.Json doesn't support custom converters on nested properties
   // See: https://github.com/dotnet/runtime/issues/63791
   var json = JsonSerializer.Serialize(data, _manualOptions);
   ```

3. **Explaining business logic or domain rules**
   ```csharp
   // Azure RBAC requires assignments at subscription scope or lower
   // Management group assignments are handled separately
   if (scope.StartsWith("/subscriptions/"))
   {
       // Process subscription-scoped assignment
   }
   ```

4. **Marking future improvements**
   ```csharp
   // TODO: Consider caching role definitions to reduce API calls
   // Related to feature: role-assignment-readable-display
   var role = await FetchRoleDefinitionAsync(roleId);
   ```

### When NOT to Use Implementation Comments

Avoid comments that simply restate the code:

❌ **Bad:**
```csharp
// Increment counter by 1
counter++;

// Check if user is admin
if (user.Role == "Admin")
{
    // Do nothing
}
```

✅ **Good** (only comment if there's a reason):
```csharp
counter++;

// Skip admin users - they have global permissions by default
if (user.Role != "Admin")
{
    ApplyRolePermissions(user);
}
```

## Traceability to Features

When a class or method implements a specific feature, reference it in comments:

```csharp
/// <summary>
/// Generates a summary table showing resource counts by type.
/// </summary>
/// <remarks>
/// Implements feature: Summary Resource Type Breakdown
/// Specification: docs/features/005-summary-resource-type-breakdown/specification.md
/// </remarks>
internal sealed class ResourceTypeSummaryGenerator
{
    // Implementation
}
```

This helps trace code back to requirements and makes impact analysis easier during changes.

## Comment Maintenance

## Quality Metric Suppressions

Use suppressions sparingly to keep the codebase maintainable. Prefer refactoring over suppressing.

### Required Suppression Practices

1. **Use `SuppressMessage`** for most cases.
    - Place the attribute on the narrowest possible scope (method, property, or type).
    - Include a clear justification in the `Justification` named argument.
    - Reference the related feature or task in the justification when applicable.

2. **Document the why** directly above the suppressed member.
    - Add a short comment explaining the rationale and trade-offs.
    - Note any follow-up work if the suppression is temporary.

3. **Maintainer approval is required** for new suppressions.
    - Call out suppressions explicitly in the PR description or review summary.

### Line Length Exceptions

Line length suppressions are acceptable only for content that cannot be reasonably wrapped:

- Long URLs that must remain intact.
- Error messages or user-facing text where wrapping changes meaning.
- Serialized JSON or embedded data where formatting is required by the consumer.

If you must exceed the line length limit, add a brief comment explaining why the line cannot be split.

### During Code Reviews

Code reviewers must verify:
- All public/internal/private members have XML doc comments
- Comments explain "why" not just "what"
- Feature references are included where applicable
- No outdated comments remain

### During Refactoring

When modifying code:
1. Update all affected XML doc comments
2. Review inline comments for accuracy
3. Add new comments for new logic
4. Remove comments that no longer apply

## Tools and Validation

### Enabling XML Documentation File Generation

Ensure `.csproj` has XML documentation enabled:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Remove after all comments added -->
</PropertyGroup>
```

### IDE Support

Visual Studio and VS Code provide:
- IntelliSense showing XML doc comments
- Quick Info tooltips on hover
- Auto-generation of comment stubs (`///` above member)

## References

- [Microsoft C# XML Documentation Comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments)
- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Recommended XML Tags for C# Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)

## Summary

Good comments serve as documentation for both current and future maintainers (human and AI). They should:

- ✅ Explain *why* decisions were made
- ✅ Document all members (public, internal, private)
- ✅ Provide context not visible in the code
- ✅ Reference specifications and features for traceability
- ✅ Stay synchronized with code changes
- ❌ Not simply repeat what the code already shows
