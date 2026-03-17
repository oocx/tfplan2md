# Feature: Azure AD App Role Assignment Support

## Overview

Add human-readable summary display and GUID-to-name resolution for Azure AD role and permission assignment resources in Terraform plan reports. Currently, these resources display raw GUIDs for the assigned role, the principal receiving the role, and the target resource — making plan reviews difficult without cross-referencing Azure documentation. This feature resolves those GUIDs into meaningful names using the same patterns established for `azurerm_role_assignment`.

The feature covers three related Azure AD resource types:

- **`azuread_app_role_assignment`** — application permission grants (e.g., `User.Read.All`)
- **`azuread_directory_role_assignment`** — directory role grants (e.g., Global Reader)
- **`azuread_service_principal_delegated_permission_grant`** — delegated permission (OAuth2 scope) grants

## User Goals

- Quickly understand **which permission** is being assigned (e.g., `User.Read.All` instead of `df021288-bdef-4463-88db-98f22de89214`)
- Identify **who** is receiving the permission by seeing a display name instead of a principal GUID
- Understand **which resource API** the permission targets (e.g., `Microsoft Graph` instead of a service principal object ID)
- Review Azure AD permission and role assignment changes in pull requests without consulting Azure portal or documentation
- Get the same quality of readable output for Azure AD assignment resources as already exists for `azurerm_role_assignment`

## Scope

### In Scope

**1. Summary Display for `azuread_app_role_assignment`**

Register `azuread_app_role_assignment` in the Azure AD provider module so that plan reports show a human-readable summary line, consistent with the existing `azuread_*` resource summary pattern.

Summary format: `{principal} → {role} → {resource}`

**2. Summary Display for `azuread_directory_role_assignment`**

Register `azuread_directory_role_assignment` with a summary showing the principal and the directory role.

Summary format: `{principal} → {role_definition_id}`

**3. Summary Display for `azuread_service_principal_delegated_permission_grant`**

Register `azuread_service_principal_delegated_permission_grant` with a summary showing the service principal, granted claims, and target resource.

Summary format: `{service_principal} → {claims} → {resource}`

**4. Principal Mapping for `principal_object_id`**

Resolve the `principal_object_id` GUID to a display name using the existing `IPrincipalMapper` infrastructure. This reuses the same `--principal-mapping` / `--principals` CLI option and JSON mapping file already available for `azurerm_role_assignment`.

**5. App Role ID Resolution for `app_role_id`**

Resolve the `app_role_id` GUID to a well-known Microsoft Graph app role name (e.g., `User.Read.All`, `Directory.Read.All`). This follows the same embedded-JSON-to-frozen-dictionary pattern used by `AzureRoleDefinitionsRegistry` for Azure RBAC role definitions.

**6. Well-Known Microsoft Graph App Roles List**

Include an embedded JSON resource file mapping well-known Microsoft Graph application permission GUIDs to their permission names. The initial list covers the most commonly used Microsoft Graph app roles (application permissions). The list is scoped to **Microsoft Graph** only — the single most common resource API.

**7. Resource Object ID Resolution for `resource_object_id`**

The `resource_object_id` identifies the service principal of the target resource API (e.g., the Microsoft Graph service principal). This is a principal-like object and can be resolved using the same `IPrincipalMapper` infrastructure. When the user's principal mapping file includes the resource service principal's object ID, it will be resolved to a display name (e.g., `Microsoft Graph`).

> **Investigation result:** The `resource_object_id` is the object ID of a service principal representing the resource application. It varies per tenant (unlike `app_role_id` which is globally stable for Microsoft Graph). Therefore, it cannot be resolved via a static built-in mapping — it must come from the user-provided principal mapping file, just like any other service principal. The Terraform plan also provides a computed `resource_display_name` attribute which can be used as a fallback when available.

**8. Computed Attribute Fallbacks**

The Terraform `azuread_app_role_assignment` resource provides computed attributes (`principal_display_name`, `principal_type`, `resource_display_name`) in the plan state. These should be used as fallbacks when the mapping file does not contain an entry for the given GUID.

**9. Icon Mappings for Assignment Attributes**

Register attribute-level icon mappings for all three resource types. These icons appear in detail tables and summary lines:

| Attribute | Icon | Rationale |
|-----------|------|-----------|
| `app_role_id` | 🛡️ | Application role / permission being granted |
| `principal_object_id` | 👤 | The principal receiving the assignment |
| `resource_object_id` | 🎯 | The target resource API |
| `service_principal_object_id` | 💻 | Service principal (delegated permission grants) |
| `role_definition_id` | 🛡️ | Directory role being assigned |
| `claim_values` | 📋 | Delegated permission scopes/claims |

**10. Broadened Value Formatter Scope**

The `AppRoleIdFormatter` and `PrincipalIdFormatter` value formatters are registered with a provider-level match pattern (`^azuread$|.*/azuread$`) rather than being scoped to a single resource type. This means `app_role_id`, `principal_object_id`, and `resource_object_id` attributes are formatted consistently across **all** `azuread` resources, not just `azuread_app_role_assignment`.

### Out of Scope

- **Real-time Azure/Graph API lookups** — no authentication or network calls during report generation
- **App roles for non-Microsoft-Graph resource APIs** — only Microsoft Graph app roles are included in the built-in list; other resource APIs (e.g., SharePoint, Exchange) would require separate mapping files or future additions
- **Parent-child grouping** — grouping `azuread_app_role_assignment` under its parent `azuread_application` is tracked separately in Feature 045
- **Similar resources** (`azuread_application_api_access`, `azuread_application_permission_scope`) — these are separate resource types that may benefit from similar patterns in future features (see [Similar Resources Investigation](#similar-resources-investigation) below)

## User Experience

### Summary Display

The summary line follows the established Azure AD summary pattern. Each resource type has its own format:

#### `azuread_app_role_assignment`

Format: `{principal} → {role} → {resource}`

**Create:**
```html
<summary>➕ azuread_app_role_assignment <b><code>example</code></b> — 👤 My Service Principal (<code>principal-guid</code>) → <code>User.Read.All</code> → 🎯 Microsoft Graph (<code>resource-guid</code>)</summary>
```

**With unmapped GUIDs (no principal mapping file):**
```html
<summary>➕ azuread_app_role_assignment <b><code>example</code></b> — <code>abcdef01-2345-6789-abcd-ef0123456789</code> → <code>User.Read.All</code> → <code>00000003-0000-0000-c000-000000000000</code></summary>
```

**Delete:**
```html
<summary>❌ azuread_app_role_assignment <b><code>example</code></b> — 👤 terraform-automation (<code>principal-guid</code>) → <code>Directory.Read.All</code> → 🎯 Microsoft Graph (<code>resource-guid</code>)</summary>
```

#### `azuread_directory_role_assignment`

Format: `{principal} → {role_definition_id}`

**Create:**
```html
<summary>➕ azuread_directory_role_assignment <b><code>example</code></b> — 👤 My Service Principal (<code>principal-guid</code>) → <code>role-template-id</code></summary>
```

#### `azuread_service_principal_delegated_permission_grant`

Format: `{service_principal} → {claims} → {resource}`

**Create with claims:**
```html
<summary>➕ azuread_service_principal_delegated_permission_grant <b><code>example</code></b> — 💻 My App (<code>sp-guid</code>) → <code>User.Read, openid</code> → 🎯 Microsoft Graph (<code>resource-guid</code>)</summary>
```

**Create with no claims:**
```html
<summary>➕ azuread_service_principal_delegated_permission_grant <b><code>example</code></b> — <code>sp-guid</code> → <code>(no claims)</code> → <code>resource-guid</code></summary>
```

### Summary Components

#### `azuread_app_role_assignment`

The summary format is: `{action} azuread_app_role_assignment <b><code>{name}</code></b> — {principal} → {role} → {resource}`

| Component | Source | Resolution Order |
|-----------|--------|------------------|
| `{role}` | `app_role_id` | 1. Built-in Microsoft Graph app role mapping → 2. Raw GUID |
| `{principal}` | `principal_object_id` | 1. `IPrincipalMapper` lookup → 2. Computed `principal_display_name` from state → 3. Raw GUID |
| `{resource}` | `resource_object_id` | 1. `IPrincipalMapper` lookup → 2. Computed `resource_display_name` from state → 3. Raw GUID |

#### `azuread_directory_role_assignment`

The summary format is: `{action} azuread_directory_role_assignment <b><code>{name}</code></b> — {principal} → {role_definition_id}`

| Component | Source | Resolution Order |
|-----------|--------|------------------|
| `{principal}` | `principal_object_id` | 1. `IPrincipalMapper` lookup → 2. Raw GUID |
| `{role_definition_id}` | `role_definition_id` | Raw value (no built-in resolution) |

#### `azuread_service_principal_delegated_permission_grant`

The summary format is: `{action} azuread_service_principal_delegated_permission_grant <b><code>{name}</code></b> — {service_principal} → {claims} → {resource}`

| Component | Source | Resolution Order |
|-----------|--------|------------------|
| `{service_principal}` | `service_principal_object_id` | 1. `IPrincipalMapper` lookup → 2. Raw GUID |
| `{claims}` | `claim_values` | Joined array values, or `(no claims)` when empty |
| `{resource}` | `resource_object_id` | 1. `IPrincipalMapper` lookup → 2. Raw GUID |

### Value Formatting in Detail Tables

In the collapsible detail table, the same resolution is applied to individual attribute values:

| Attribute | Value |
|-----------|-------|
| `app_role_id` | `User.Read.All (df021288-bdef-4463-88db-98f22de89214)` |
| `principal_object_id` | `My Service Principal [abcdef01-2345-6789-abcd-ef0123456789]` |
| `resource_object_id` | `Microsoft Graph [11111111-2222-3333-4444-555555555555]` |
| `principal_display_name` | `My Service Principal` |
| `principal_type` | `ServicePrincipal` |
| `resource_display_name` | `Microsoft Graph` |

### Well-Known Microsoft Graph App Roles

The embedded JSON file maps Microsoft Graph application permission GUIDs to their permission names. Example entries:

```json
{
  "df021288-bdef-4463-88db-98f22de89214": "User.Read.All",
  "5b567255-7703-4780-807c-7be8301ae99b": "Group.Read.All",
  "7ab1d382-f21e-4acd-a863-ba3e13f7da61": "Directory.Read.All",
  "19dbc75e-c2e2-444c-a770-ec596d67b765": "Directory.ReadWrite.All",
  "741f803b-c850-494e-b5df-cde7c675a1ca": "User.ReadWrite.All",
  "62a82d76-70ea-41e2-9197-370581804d09": "Group.ReadWrite.All",
  "06b708a9-e830-4db3-a914-8e69da51d44f": "AppRoleAssignment.ReadWrite.All",
  "1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9": "Application.ReadWrite.All",
  "9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30": "Application.Read.All"
}
```

The full list should be sourced from the [Microsoft Graph permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference) or by querying the Microsoft Graph service principal via Azure CLI:

```bash
az ad sp show --id 00000003-0000-0000-c000-000000000000 \
  --query "appRoles[].{id:id, value:value}" -o json | \
  jq 'map({(.id): .value}) | add' > MicrosoftGraphAppRoles.json
```

## Similar Resources Investigation

The following Azure AD resources use similar GUID-based attributes and could benefit from the same resolution patterns in future features:

| Resource Type | Relevant Attributes | Notes |
|---------------|---------------------|-------|
| `azuread_application_api_access` | `api_client_id`, `role_ids`, `scope_ids` | References app roles and scopes on other applications |
| `azuread_application_permission_scope` | `scope_id` (GUID) | Defines delegated permission scopes |
| `azuread_application` | `app_role` blocks with `id` (GUID) | Defines app roles (not consumes them) |
| `azuread_application_app_role` | `role_id` (GUID) | Standalone app role definition resource |

**Recommendation:** The app role resolution infrastructure built for this feature (embedded JSON + resolver) should be designed so it can be reused by these related resources in future features. However, implementing support for these resources is explicitly out of scope for this feature.

## Success Criteria

- [ ] `azuread_app_role_assignment` resources display a human-readable summary line in plan reports
- [ ] `azuread_directory_role_assignment` resources display a summary line with principal and role definition
- [ ] `azuread_service_principal_delegated_permission_grant` resources display a summary line with service principal, claims, and resource
- [ ] Summary format follows the established pattern for each resource type
- [ ] `app_role_id` GUIDs are resolved to Microsoft Graph permission names using a built-in embedded JSON mapping
- [ ] Built-in mapping covers the most commonly used Microsoft Graph application permissions
- [ ] `principal_object_id` is resolved using the existing `IPrincipalMapper` infrastructure
- [ ] `resource_object_id` is resolved using the existing `IPrincipalMapper` infrastructure (service principal object IDs are principal-like)
- [ ] `service_principal_object_id` is resolved using `IPrincipalMapper` for delegated permission grants
- [ ] Computed attributes (`principal_display_name`, `resource_display_name`) are used as fallbacks when mapper lookups fail
- [ ] Unmapped GUIDs display the raw GUID gracefully (no errors)
- [ ] Icon mappings registered for all 6 assignment attributes (🛡️ `app_role_id`, 👤 `principal_object_id`, 🎯 `resource_object_id`, 💻 `service_principal_object_id`, 🛡️ `role_definition_id`, 📋 `claim_values`)
- [ ] Value formatters scoped to all `azuread` resources (not just `azuread_app_role_assignment`)
- [ ] The app role resolver follows the same embedded-JSON-to-frozen-dictionary pattern as `AzureRoleDefinitionsRegistry`
- [ ] All three resource types are registered in `AzureADModule.cs`
- [ ] Value formatters for `app_role_id`, `principal_object_id`, and `resource_object_id` are registered for detail table display
- [ ] Tests cover summary generation with mapped and unmapped GUIDs for all three resource types
- [ ] Tests cover fallback to computed attributes when mappings are unavailable
- [ ] Backward compatibility maintained — existing reports and CLI behavior are not affected

## Open Questions

1. **App role list completeness:** Should the initial embedded JSON include all ~400+ Microsoft Graph app roles, or only the most commonly used subset (e.g., top 50–100)? A comprehensive list can be generated via `az ad sp show` but may include rarely-used permissions.

2. **Display format for resolved app roles:** Should the resolved app role in the detail table show `User.Read.All (df021288-...)` (name + GUID, matching the Azure RBAC role format), or just `User.Read.All` (name only, since the GUID column is already in the raw attribute)?

3. **Icon for app role assignments:** Should `azuread_app_role_assignment` reuse the existing role icon pattern (`🛡️`) or use a dedicated permission icon?
