# Architecture: Role Assignment Table Format

## Status

Proposed

## Context

The feature request "Role Assignment Table Format" requires displaying `azurerm_role_assignment` resources in a specific table format.
A key requirement is the formatting of the summary line and the details table:
- **Summary**: `Principal Name` (Type) → `Role Name` on `Scope Name`
- **Styling**: Specific parts must be wrapped in backticks (e.g., `` `Principal Name` ``).

Currently, the application uses Scriban helpers (`azure_principal_name`, `azure_role_name`, `azure_scope`) to retrieve this information. However, these helpers return **pre-formatted strings**, often including Markdown syntax (e.g., bolding `**Scope**`).

For example:
- `azure_scope` returns: `**resource-group** in subscription **sub-id**`
- `azure_principal_name` returns: `Name [ID]`

To achieve the required format (e.g., wrapping just the name in backticks), we cannot use the existing helpers as they conflate **data retrieval** with **presentation**.

## Options Considered

### Option 1: String Parsing in Template
Use regex or string manipulation within the Scriban template to strip existing markdown and extract names.
- **Pros**: No C# code changes.
- **Cons**: Extremely brittle. Relies on the specific output format of helpers. Hard to read and maintain in Scriban.

### Option 2: Formatting Parameters
Add arguments to existing helpers (e.g., `azure_scope(id, format: "raw")`).
- **Pros**: Reuses existing function names.
- **Cons**: Complicates the helper signatures. "Raw" might mean different things in different contexts.

### Option 3: Structured Data Helpers (Recommended)
Introduce new helpers that return **structured objects** (DTOs) instead of strings.
- `azure_principal_info(id)` -> `{ name: "Jane", id: "...", type: "User" }`
- `azure_role_info(id)` -> `{ name: "Reader", id: "..." }`
- `azure_scope_info(id)` -> `{ name: "rg-1", type: "Resource Group", ... }`

The template then has full control over formatting: `` `{{ p.name }}` ``.

## Decision

**Option 3: Structured Data Helpers**

We will expose the raw data to the Scriban templates. This aligns with the separation of concerns: C# handles logic/parsing, Scriban handles presentation.

## Rationale

- **Flexibility**: Templates can render data in any format (table, list, plain text, markdown) without changing C# code.
- **Maintainability**: The parsing logic is centralized in C#, but the rendering logic is fully in the template.
- **Robustness**: Avoids fragile string parsing.

## Implementation Notes

### 1. Interface Updates
- **`IPrincipalMapper`**: Add `string? GetName(string principalId)` to return the raw name without formatting.
  - Update `PrincipalMapper` and `NullPrincipalMapper`.

### 2. Helper Logic Refactoring
- **`AzureRoleDefinitionMapper`**: Add `GetRoleDefinition(string id)` returning `(string Name, string Id)`.
- **`AzureScopeParser`**: Refactor `ParseScope` to extract a `ScopeInfo` object first, then format it. Expose `Parse(string scope)` returning `ScopeInfo`.
  - `ScopeInfo` properties: `Name`, `Type` (e.g. "Resource Group"), `SubscriptionId`, `Level` (Subscription, ResourceGroup, Resource, ManagementGroup).

### 3. Scriban Helpers
Register new functions in `ScribanHelpers.cs`:
- `azure_principal_info`: Returns object with `name`, `id`.
- `azure_role_info`: Returns object with `name`, `id`.
- `azure_scope_info`: Returns object with `name`, `type`, `subscription_id`, `resource_group`.

### 4. Template Implementation
Update `role_assignment.sbn` to use these new helpers to construct the summary line and details table as specified in the feature spec.
