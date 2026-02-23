# Fixed: Resource identity attributes no longer receive redundant contextual expansion

This patch fixes a bug where a resource's own identity attributes (`id`, `name`) were incorrectly decorated with the full "readable display name" expansion intended only for references to other resources.

## 🐛 Bug fixes

### Resource identity attributes now show only semantic icons

**Problem:** When rendering attribute tables, a resource's own `id` and `name` attributes would receive the full "readable display name" formatting that includes contextual information like resource group and subscription. This caused redundant and confusing output where the resource's identifier was decorated with information already visible in the resource's summary line.

**Example of incorrect behavior:**
```markdown
| Attribute | Value |
|-----------|-------|
| id | Storage Account `🆔 sttfplan2mdlogs` in resource group `📁 rg-tfplan2md-demo` of subscription `🔑 Production (...)` |
| name | Storage Account `🆔 sttfplan2mdlogs` in resource group `📁 rg-tfplan2md-demo` of subscription `🔑 Production (...)` |
```

**Correct behavior (after fix):**
```markdown
| Attribute | Value |
|-----------|-------|
| id | `🆔 /subscriptions/.../resourceGroups/rg-tfplan2md-demo/providers/Microsoft.Storage/storageAccounts/sttfplan2mdlogs` |
| name | `🆔 sttfplan2mdlogs` |
```

**Why this matters:**
- The full "readable display name" is valuable when an attribute **references another resource** (e.g., `scope`, `key_vault_id`, `parent_id`, `managedEnvironmentId`)
- When showing a resource's **own identity**, the expanded format is redundant because:
  - The user is already looking at that resource's details section
  - The resource group and subscription are typically shown in the summary line
  - The full contextual expansion adds visual clutter without providing new information

**Root cause:** The `AzureResourceIdFormatter` was registered to format ANY Azure resource ID pattern across all attributes, without distinguishing between a resource's own identity attributes and reference attributes pointing to other resources.

**Fix:** Modified `AzureResourceIdFormatter.TryFormat()` to exclude `id` and `name` attributes from full readable display name formatting. These attributes now receive only semantic icon decoration (handled by the semantic formatting layer), while reference attributes continue to receive the full contextual expansion.

### Impact on reports

- **Identity attributes** (`id`, `name`): Show semantic icon (🆔) without contextual expansion
- **Reference attributes** (`scope`, `key_vault_id`, `parent_id`, etc.): Continue to show full readable display name with resource type, name, resource group, and subscription

### Affected providers

This fix applies to all Azure providers that use the `AzureResourceIdFormatter`:
- **azurerm** - Azure Resource Manager resources
- **azapi** - Azure API resources

## 🔗 Commits

- [`c207d0a`](https://github.com/oocx/tfplan2md/commit/c207d0a) fix: exclude id and name attributes from Azure resource ID full display name formatting
- [`5586dc2`](https://github.com/oocx/tfplan2md/commit/5586dc2) docs: add issue analysis for readable display name on identity attributes

## 🧪 Test coverage

Added 4 new test cases to verify the fix:

1. **AzureRM: id attribute excluded** - Verifies `id` attribute does not receive full display name formatting
2. **AzureRM: name attribute excluded** - Verifies `name` attribute does not receive full display name formatting
3. **AzApi: id attribute excluded** - Verifies `id` attribute does not receive full display name formatting for AzApi provider
4. **AzApi: name attribute excluded** - Verifies `name` attribute does not receive full display name formatting for AzApi provider

All existing tests continue to pass, confirming reference attributes still receive full readable display name formatting.
