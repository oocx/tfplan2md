# Feature: Custom Template for azurerm_firewall_application_rule_collection

## Overview

Add a custom Scriban template and supporting infrastructure for `azurerm_firewall_application_rule_collection` resources to provide semantic diffing of application firewall rules. This feature will mirror the existing implementation for `azurerm_firewall_network_rule_collection` but adapt to the specific properties and structure of application rules.

**Background**: The website documentation at `website/features/firewall-rules.html` currently claims that tfplan2md "Works for all `azurerm_firewall_network_rule_collection` and `azurerm_firewall_application_rule_collection` resources," but application rule collections currently fall back to the default template showing index-based attribute changes instead of semantic rule-by-rule diffing.

**Related Resources**:
- Existing implementation: `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn`
- Related feature: `docs/features/026-template-rendering-simplification/specification.md`

## User Goals

- **Infrastructure Engineers**: View clear, rule-by-rule changes to Azure Firewall application rule collections in pull request reviews, making it easy to understand exactly which application rules are being added, modified, or removed without having to mentally parse index-based array changes.

- **Security Reviewers**: Quickly scan application firewall rule changes to identify security-relevant modifications (e.g., new FQDN destinations, protocol changes, or rule deletions) in a human-readable format.

- **DevOps Teams**: Reduce cognitive load when reviewing Terraform plan outputs for Azure Firewall application rules, allowing faster and more confident approval of infrastructure changes.

## Scope

### In Scope

1. **Scriban Template**:
   - Create `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_application_rule_collection.sbn`
   - Display collection metadata (name, priority, action)
   - Show rule changes table for updates with change indicators (➕ added, 🔄 modified, ❌ removed, ⏺️ unchanged)
   - Show rules table for create actions (all rules being added)
   - Show rules table for delete actions (all rules being deleted)
   - Fall back to attribute changes when rules are not available
   - Integrate code analysis findings via `_code_analysis_metadata.sbn` and `_code_analysis_findings.sbn`

2. **View Model Classes**:
   - Create `FirewallApplicationRuleCollectionViewModel` with properties: name, priority, action, rule_changes, after_rules, before_rules
   - Create `FirewallApplicationRuleChangeRowViewModel` for update scenarios with change indicator and all rule properties
   - Create `FirewallApplicationRuleRowViewModel` for create/delete scenarios

3. **View Model Factory**:
   - Create `FirewallApplicationRuleCollectionViewModelFactory` to build view models from Terraform plan data
   - Extract application rules from before/after state
   - Compute added, modified, removed, and unchanged rules by comparing rule names
   - Format rule properties for display (FQDNs, protocols, source addresses, etc.)
   - Generate inline diffs for modified properties using existing `FormatDiff` helpers
   - Implement `BuildChangedAttributesSummary` method for summary line

4. **Factory Adapter**:
   - Create `FirewallApplicationRuleCollectionFactory` class in `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/Factories.cs`
   - Register factory in `AzureRMModule.cs` for resource type `azurerm_firewall_application_rule_collection`

5. **Resource Change Model**:
   - Add `FirewallApplicationRuleCollection` property to `ResourceChangeModel` class

6. **Application Rule Properties**:
   The template and view model should handle these Azure Firewall Application Rule properties:
   - `name` (string): Rule identifier
   - `protocols` (list): HTTP, HTTPS, MSSQL protocols (not TCP/UDP like network rules)
   - `source_addresses` (list): Source IP addresses/CIDR ranges
   - `source_ip_groups` (list): Source IP group references (optional)
   - `target_fqdns` (list): Destination fully qualified domain names
   - `fqdn_tags` (list): Azure-defined FQDN tags (e.g., Windows Update, App Service Environment)
   - `description` (string): Rule documentation

7. **Test Coverage**:
   - Create test data JSON file with before/after application rule states
   - Create expected markdown snapshot showing all change scenarios
   - Add regression test case to verify template rendering

### Out of Scope

- **Changes to Network Rule Collection template**: This feature only adds application rule collection support; the existing network rule collection template remains unchanged
- **Firewall Policy Support**: This feature focuses on classic Azure Firewall rule collections, not the newer `azurerm_firewall_policy_rule_collection_group` resources
- **NAT Rule Collections**: Only application rule collections are in scope; NAT rule collections would be a separate feature
- **Advanced Application Rule Properties**: Properties like `web_categories` or `terminate_tls` that are less commonly used may be deferred to future enhancements if they complicate the initial implementation
- **Custom Formatting Options**: The template will use the same semantic icons and formatting conventions as the network rule collection template

## User Experience

### Input

Users provide a Terraform plan JSON file containing changes to `azurerm_firewall_application_rule_collection` resources:

```bash
tfplan2md plan.json -o report.md
```

### Expected Output

#### Scenario 1: Update with Rule Changes

When application rules are added, modified, or removed, the report shows:

```markdown
<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 Update `azurerm_firewall_application_rule_collection.web_rules`</summary>

**Collection:** `web-rules` | **Priority:** `200` | **Action:** 🟢 Allow

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Target FQDNs | Description |
| -------- | ----------- | ----------- | ------------------ | ------------- | ------------- |
| ➕ | `allow-github` | `Https:443` | `10.0.1.0/24` | `github.com, *.github.io` | GitHub access |
| 🔄 | `allow-microsoft` | `Http:80, Https:443` | <del style="color:#E5534B;">10.0.0.0/24</del> <ins style="color:#46954A;">10.0.0.0/16</ins> | `*.microsoft.com` | Microsoft services |
| ❌ | `allow-old-site` | `Https:443` | `10.0.2.0/24` | `old-site.example.com` | Legacy site (removed) |
| ⏺️ | `allow-azure` | `Https:443` | `10.0.0.0/24` | `*.azure.com` | Azure services |

</details>
```

#### Scenario 2: Create with Application Rules

When a new application rule collection is created:

```markdown
<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ Create `azurerm_firewall_application_rule_collection.app_rules`</summary>

**Collection:** `app-rules` | **Priority:** `100` | **Action:** 🟢 Allow

#### Rules

| Rule Name | Protocols | Source Addresses | Target FQDNs | Description |
| ----------- | ----------- | ------------------ | ------------- | ------------- |
| `allow-github` | `Https:443` | `10.0.1.0/24` | `github.com, *.github.io` | GitHub access |
| `allow-azure` | `Https:443` | `10.0.0.0/24` | `*.azure.com` | Azure services |

</details>
```

#### Scenario 3: Delete with Application Rules

When an application rule collection is deleted:

```markdown
<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>❌ Delete `azurerm_firewall_application_rule_collection.old_rules`</summary>

**Collection:** `old-rules` | **Priority:** `300` | **Action:** 🔴 Deny

#### Rules (being deleted)

| Rule Name | Protocols | Source Addresses | Target FQDNs | Description |
| ----------- | ----------- | ------------------ | ------------- | ------------- |
| `block-social` | `Https:443` | `*` | `*.facebook.com, *.twitter.com` | Block social media |

</details>
```

#### Scenario 4: Fallback to Attribute Changes

When rule data is not available (e.g., computed values):

```markdown
<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 Update `azurerm_firewall_application_rule_collection.computed_rules`</summary>

**Collection:** `computed-rules` | **Priority:** `150` | **Action:** 🟢 Allow

<details>
<summary>Attribute Changes</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| priority | 150 | 200 |
| rule | (computed) | (computed) |

</details>

</details>
```

### Behavior Notes

1. **Change Detection**: Rules are matched by name (case-insensitive) between before and after states
2. **Change Indicators**: 
   - ➕ = Added rule (exists in after, not in before)
   - 🔄 = Modified rule (exists in both, but properties differ)
   - ❌ = Removed rule (exists in before, not in after)
   - ⏺️ = Unchanged rule (exists in both with identical properties)
3. **Inline Diffs**: Modified properties show before/after values with strikethrough and colored text
4. **Action Icons**: 🟢 Allow, 🔴 Deny displayed next to the action
5. **Summary Line**: For updates, shows count and sample of changed rules (e.g., "3🔧 ➕ allow-github, 🔄 allow-microsoft, ❌ allow-old-site")

## Success Criteria

- [ ] Template file created at correct location with proper structure
- [ ] View model classes created with all required properties
- [ ] View model factory correctly extracts rules and computes changes
- [ ] Factory adapter created and registered in AzureRMModule
- [ ] ResourceChangeModel updated with new property
- [ ] Test data file created with realistic application rule scenarios
- [ ] Expected markdown snapshot created and matches template output
- [ ] Regression test passes
- [ ] All existing tests continue to pass
- [ ] Documentation (if needed) updated to reflect actual implementation

## Open Questions

### Question 1: Handling Optional Application Rule Properties

Application rules support several optional properties:
- `source_ip_groups` (alternative to `source_addresses`)
- `fqdn_tags` (alternative to `target_fqdns`)
- `protocols` can include port numbers (e.g., `Https:443`, `Http:80`, `Mssql:1433`)

**Question**: Should the initial template:
1. Support all properties from the start (more complete but more complex)?
2. Start with core properties (`source_addresses`, `target_fqdns`, `protocols`, `description`) and defer optional properties to a future enhancement?
3. Show optional properties only when they are present (conditional columns in the table)?

**Recommendation Needed From**: Architect or Maintainer

---

### Question 2: Protocol Formatting

Network rules show protocols as simple strings (e.g., `TCP`, `UDP`). Application rules include port numbers in the protocol specification (e.g., `Https:443`, `Http:80,8080`).

**Question**: Should protocols be:
1. Displayed exactly as provided in the Terraform state (e.g., `Https:443`)?
2. Split into separate "Protocol" and "Port" columns for better readability?
3. Formatted with semantic icons (e.g., 🔒 for HTTPS)?

**Recommendation Needed From**: Maintainer (UX preference)

---

### Question 3: FQDN List Formatting

Application rules often have multiple target FQDNs (e.g., `["*.microsoft.com", "*.azure.com", "*.windows.net"]`).

**Question**: Should long FQDN lists be:
1. Displayed as comma-separated values inline (may cause wide tables)?
2. Displayed as comma-separated but with line breaks for readability?
3. Truncated with "... +N more" for lists exceeding a threshold (e.g., > 3 items)?

**Recommendation Needed From**: Maintainer (UX preference)

---

### Question 4: Web Categories Support

Application rules support Azure web categories for content filtering (e.g., blocking gambling sites, social media, etc.). This is an advanced feature that may not be commonly used.

**Question**: Should web categories be:
1. Included in the initial implementation (adds complexity)?
2. Deferred to a future enhancement (simpler initial release)?
3. Included only if test data shows they are used in the wild?

**Recommendation Needed From**: Architect or Maintainer

---

### Question 5: Test Data Source

We need realistic test data to ensure the template works correctly.

**Question**: Should test data be:
1. Created manually based on Terraform documentation examples?
2. Generated from a real `terraform plan -json` output with application rule changes?
3. Derived from the network rule collection test data structure but adapted for application rules?

**Recommendation Needed From**: Developer or Quality Engineer

---

## Next Steps

Once this specification is approved:

1. **Architect**: Review the specification and answer open questions, then create a technical design document
2. **Task Planner**: Break down the implementation into user stories with acceptance criteria
3. **Developer**: Implement the template, view models, factory, and tests according to the design
4. **Quality Engineer**: Define test scenarios and validate the implementation
5. **Technical Writer**: Update documentation to reflect the new capability (if not already accurate)
