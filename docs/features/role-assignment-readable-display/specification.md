# Feature: Enhanced Azure Role Assignment Display

## Overview

Transform Azure role assignment outputs in terraform plan reports from cryptic GUIDs and technical paths into human-readable information that developers can understand without consulting Azure documentation.

## User Goals

- Quickly understand which Azure role is being assigned (e.g., "Reader" instead of `acdd72a7-3385-48ef-bd42-f606fba81ae7`)
- Easily comprehend the scope of role assignments with clear hierarchical context
- Optionally identify principals by name instead of only by GUID
- Reduce time spent looking up Azure role definition IDs and parsing resource paths

## Scope

### In Scope

**Role Definition Enhancement:**
- Map Azure built-in role definition GUIDs to friendly names
- Display format: `"Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)"`
- Include comprehensive mapping of all known Azure built-in roles at time of implementation
- Custom role definitions remain unchanged (no mapping available)

**Scope Path Enhancement:**
- Parse Azure resource scope paths and display with human-readable context
- Support multiple scope types:
  - Management Group: `"my-mg-group (Management Group)"`
  - Subscription: `"subscription-name (subscription-id)"` or just ID if name unavailable
  - Resource Group: `"foo in subscription bar (subscription-id)"`
  - Specific Resources: `"Key Vault vault1 in resource group foo of subscription example-subscription (example-subscription-id)"`
- Handle common Azure resource types in resource-level scopes
- Differentiate resource names/IDs from connecting phrases with styling (bold, code formatting, or similar)

**Principal ID Enhancement:**
- Optional principal mapping via command-line argument
- CLI flag: `--principal-mapping <path>` or `--principals <path>`
- JSON format: `{"guid": "Display Name (Type)"}`
- Type included in display: `"John Doe (User)"`, `"DevOps Team (Group)"`, `"terraform-sp (Service Principal)"`
- If no mapping provided or ID not found, display raw GUID
- Documentation for generating mapping file using Azure CLI

**Resource Type:**
- Only affects `azurerm_role_assignment` resources
- Uses existing resource-specific template system

### Out of Scope

- Real-time Azure API lookups (requires authentication, adds latency)
- Automatic principal name resolution without user-provided mapping
- Mapping for custom role definitions (requires API access)
- Subscription name lookup from subscription ID (no data source available)
- Support for non-Azure providers
- Interactive prompts for missing principal mappings

## User Experience

### Command-Line Interface

**Without principal mapping (default):**
```bash
tfplan2md plan.json > report.md
```

**With principal mapping:**
```bash
tfplan2md --principal-mapping principals.json plan.json > report.md
```

**Docker without principal mapping:**
```bash
docker run --rm -v $(pwd):/workspace oocx/tfplan2md:latest /workspace/plan.json > report.md
```

**Docker with principal mapping:**
```bash
docker run --rm -v $(pwd):/workspace oocx/tfplan2md:latest \
  --principal-mapping /workspace/principals.json /workspace/plan.json > report.md
```

*Note: The principals.json file must be in the mounted directory and referenced with the container path*

### Markdown Output

**Current output (technical):**
```markdown
### azurerm_role_assignment.example (create)

- **scope**: /subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg
- **role_definition_id**: /subscriptions/12345678-1234-1234-1234-123456789012/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7
- **principal_id**: abcdef01-2345-6789-abcd-ef0123456789
```

**Enhanced output:**
```markdown
### azurerm_role_assignment.example (create)

- **scope**: **my-rg** in subscription **example-sub** (12345678-1234-1234-1234-123456789012)
- **role_definition_id**: Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **principal_id**: John Doe (User) [abcdef01-2345-6789-abcd-ef0123456789]
```

*Note: Exact styling/formatting to be determined during implementation*

### Principal Mapping File Format

**JSON structure:**
```json
{
  "abcdef01-2345-6789-abcd-ef0123456789": "John Doe (User)",
  "11111111-2222-3333-4444-555555555555": "DevOps Team (Group)",
  "99999999-8888-7777-6666-555555555555": "terraform-sp (Service Principal)"
}
```

### Generating Principal Mapping

**Documentation will include Azure CLI commands:**

```bash
# Generate mapping for users, groups, and service principals
{
  echo "{"
  az ad user list --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s (User)\",\n", $1, $2}'
  az ad group list --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s (Group)\",\n", $1, $2}'
  az ad sp list --all --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s (Service Principal)\",\n", $1, $2}' | \
    sed '$ s/,$//'
  echo "}"
} > principals.json
```

**Example for limited scope (faster):**
```bash
# Only specific service principals
az ad sp list --filter "startswith(displayName,'terraform')" \
  --query "[].{id:id,name:displayName}" -o json | \
  jq 'map({(.id): (.displayName + " (Service Principal)")}) | add'
```

## Success Criteria

- [ ] Built-in Azure role definition GUIDs are mapped to friendly names
- [ ] Role display format includes both name and GUID
- [ ] Scope paths are parsed and displayed with hierarchical context
- [ ] Management group, subscription, resource group, and resource-level scopes are handled
- [ ] Resource names are visually distinguished from connecting text
- [ ] Optional `--principal-mapping` CLI argument loads JSON mapping file
- [ ] Principal IDs are enhanced when mapping is provided
- [ ] Principal IDs display raw GUID when no mapping exists
- [ ] Unmapped principals gracefully fall back to GUID display
- [ ] Documentation includes Azure CLI commands for generating mapping file
- [ ] Changes only affect `azurerm_role_assignment` resource type
- [ ] Tests cover all scope types and mapping scenarios
- [ ] Tests verify behavior with and without principal mapping
- [ ] Backward compatibility maintained (existing reports still work)

## Open Questions

1. **Styling approach**: How should resource names be visually differentiated from connecting text in markdown? (bold, code backticks, combination?)
2. **Subscription name handling**: Should we support user-provided subscription name mapping similar to principals?
3. **Error handling**: What should happen if the principal mapping file is malformed JSON?
4. **Azure role mapping source**: Should the built-in role mapping be embedded in code or loaded from a JSON file?
5. **Resource type coverage**: Which specific Azure resource types should be prioritized for resource-level scope parsing? (Key Vault, Storage Account, VM, etc.)
