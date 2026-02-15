# Tasks: Custom Template for azurerm_firewall_application_rule_collection

## Overview

This feature adds a custom Scriban template and supporting infrastructure for `azurerm_firewall_application_rule_collection` resources to provide semantic diffing of application firewall rules. The implementation mirrors the existing `azurerm_firewall_network_rule_collection` pattern, adapting it to application rule properties (FQDN targets, HTTP/HTTPS protocols, etc.).

**Feature Documents:**
- Specification: `docs/features/040-azurerm-firewall-application-rule-template/specification.md`
- Architecture: `docs/features/040-azurerm-firewall-application-rule-template/architecture.md`
- Test Plan: `docs/features/040-azurerm-firewall-application-rule-template/test-plan.md`
- UAT Plan: `docs/features/040-azurerm-firewall-application-rule-template/uat-test-plan.md`

**Reference Implementation:** `azurerm_firewall_network_rule_collection` (network rule template and supporting classes)

---

## Tasks

### Task 1: Create View Model Classes

**Priority:** High

**Description:**
Create the three view model classes that hold formatted data for the Scriban template:
1. `FirewallApplicationRuleCollectionViewModel` - Main view model with collection metadata and rule collections
2. `FirewallApplicationRuleChangeRowViewModel` - Individual rule row for update scenarios (with change indicator and diffs)
3. `FirewallApplicationRuleRowViewModel` - Individual rule row for create/delete scenarios (no diffs)

These classes follow the exact structure of `FirewallNetworkRuleCollectionViewModel` and related classes, but with application rule properties (protocols with ports, target FQDNs, source IP groups, FQDN tags).

**Files to Create:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModel.cs`

**Acceptance Criteria:**
- [ ] `FirewallApplicationRuleCollectionViewModel` class created with properties:
  - `Name` (string?)
  - `Priority` (string?)
  - `Action` (string?) - will contain formatted action with icons
  - `RuleChanges` (IReadOnlyList<FirewallApplicationRuleChangeRowViewModel>) - for update scenario
  - `AfterRules` (IReadOnlyList<FirewallApplicationRuleRowViewModel>) - for create scenario
  - `BeforeRules` (IReadOnlyList<FirewallApplicationRuleRowViewModel>) - for delete scenario
- [ ] `FirewallApplicationRuleChangeRowViewModel` class created with `required` properties:
  - `Change` (string) - change indicator (➕/🔄/❌/⏺️)
  - `Name` (string) - rule name
  - `Protocols` (string) - formatted or diff
  - `SourceAddresses` (string) - formatted or diff
  - `SourceIpGroups` (string) - formatted or diff (optional, may be empty)
  - `TargetFqdns` (string) - formatted or diff
  - `FqdnTags` (string) - formatted or diff (optional, may be empty)
  - `Description` (string) - formatted or diff
- [ ] `FirewallApplicationRuleRowViewModel` class created with `required` properties:
  - `Name` (string)
  - `Protocols` (string)
  - `SourceAddresses` (string)
  - `SourceIpGroups` (string) - optional, may be empty
  - `TargetFqdns` (string)
  - `FqdnTags` (string) - optional, may be empty
  - `Description` (string)
- [ ] All properties use `init` keyword for immutability
- [ ] All non-nullable properties use `required` keyword (C# 11)
- [ ] XML documentation comments added for all classes and properties
- [ ] Classes are `public sealed`
- [ ] File follows project namespace convention (`Oocx.TfPlan2Md.Providers.AzureRM.Models`)
- [ ] Code compiles without warnings

**Dependencies:** None

**Testing Approach:**
- Compile check - ensure classes compile without errors
- Visual inspection - verify structure matches architecture design
- Reference comparison - compare against FirewallNetworkRuleCollectionViewModel for consistency

**Estimated Effort:** 30 minutes

**Notes:**
- These are pure data classes with no logic
- All properties are formatted strings (no raw JSON or complex types)
- The `required` keyword ensures all mandatory properties are set during initialization
- Optional properties (SourceIpGroups, FqdnTags) are required but may be empty strings

---

### Task 2: Create Test Data JSON File

**Priority:** High

**Description:**
Create a Terraform plan JSON test file containing `azurerm_firewall_application_rule_collection` resource changes that exercise all scenarios (create, update with all change types, delete, fallback). This test data will drive both snapshot tests and manual validation.

The test data should be realistic and derived from actual Terraform plan structure (see `examples/firewall-rules-demo/plan.json` as a reference for network rules).

**Files to Create:**
- `examples/firewall-application-rules-demo/plan.json`

**Acceptance Criteria:**
- [ ] JSON file contains valid Terraform plan structure with `resource_changes` array
- [ ] Contains **create scenario**: New application rule collection with 2-3 rules
  - Rules include various protocols (Http:80, Https:443, Mssql:1433)
  - Rules include multiple target FQDNs
  - Rules include descriptions
- [ ] Contains **update scenario**: Existing collection with rule changes:
  - At least one **added rule** (exists in after, not in before)
  - At least one **modified rule** (exists in both, different properties) - modify source_addresses to show diff
  - At least one **removed rule** (exists in before, not in after)
  - At least one **unchanged rule** (exists in both with identical properties)
- [ ] Contains **delete scenario**: Existing collection with rules being deleted
- [ ] Contains **fallback scenario**: Update with computed rules (rules not available in state)
- [ ] Test data includes **optional properties**:
  - At least one rule uses `source_ip_groups` instead of source_addresses
  - At least one rule uses `fqdn_tags` (e.g., "WindowsUpdate", "AppServiceEnvironment")
- [ ] Test data includes **edge cases**:
  - Rule with empty description
  - Rule with long FQDN list (> 5 items) to test truncation
  - Rule with multiple protocols (e.g., ["Http:80", "Https:443"])
- [ ] JSON is well-formed and passes validation
- [ ] Resource names follow convention: `azurerm_firewall_application_rule_collection.<name>`

**Dependencies:** None

**Testing Approach:**
- JSON validation - ensure file is parseable
- Manual inspection - verify all scenarios are covered
- Parse test - attempt to parse with tfplan2md to verify structure

**Estimated Effort:** 1 hour

**Notes:**
- Reference `examples/firewall-rules-demo/plan.json` for structure
- Can start with manually created JSON based on Terraform docs if generating from real Terraform is blocked
- Ensure rule names are unique within each collection for clear change detection
- The "before" and "after" states must align with action types (create has no before, delete has no after, update has both)

---

### Task 3: Create View Model Factory - Core Structure

**Priority:** High

**Description:**
Create the `FirewallApplicationRuleCollectionViewModelFactory` static class with the main entry point method `Build` and the internal data structure for holding raw rule values during processing. This establishes the factory skeleton before implementing the complex diff logic.

**Files to Create:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] Class created as `public static class FirewallApplicationRuleCollectionViewModelFactory`
- [ ] `Build` method signature implemented:
  ```csharp
  public static FirewallApplicationRuleCollectionViewModel Build(
      ResourceChange resourceChange,
      string providerName,
      LargeValueFormat largeValueFormat)
  ```
- [ ] Internal record created for raw rule data:
  ```csharp
  private sealed record ApplicationRuleValues(
      string Name,
      IReadOnlyList<string> Protocols,
      IReadOnlyList<string> SourceAddresses,
      IReadOnlyList<string> SourceIpGroups,
      IReadOnlyList<string> TargetFqdns,
      IReadOnlyList<string> FqdnTags,
      string Description);
  ```
- [ ] `Build` method extracts collection metadata:
  - `name` from resource change
  - `priority` from after/before state (formatted as string)
  - `action` from after/before state (formatted with icons: 🟢 Allow, 🔴 Deny)
- [ ] `Build` method returns a minimal view model (empty rule lists for now)
- [ ] XML documentation comments added for class and public method
- [ ] Appropriate using statements added (System.Text.Json, Parsing, etc.)
- [ ] Code compiles without warnings

**Dependencies:** Task 1 (View Model Classes)

**Testing Approach:**
- Compile check - ensure factory compiles
- Unit test - verify metadata extraction (name, priority, action formatting)
- Use test data from Task 2 to validate basic parsing

**Estimated Effort:** 45 minutes

**Notes:**
- Reference `FirewallNetworkRuleCollectionViewModelFactory` for structure
- Action formatting should reuse `ScribanHelpers.FormatAttributeValueTable` or similar
- Don't implement rule extraction or diff logic yet - focus on skeleton structure
- The `ApplicationRuleValues` record is internal to the factory for processing; it's not exposed in the view model

---

### Task 4: Implement Rule Extraction and Parsing

**Priority:** High

**Description:**
Implement the `ExtractRules` private method in the factory that parses the rule array from Terraform JSON state (before/after) and converts it to the internal `ApplicationRuleValues` record. This method must handle missing or malformed data gracefully.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] `ExtractRules` method signature:
  ```csharp
  private static IReadOnlyList<ApplicationRuleValues> ExtractRules(object? stateObject)
  ```
- [ ] Method extracts `rule` array from state object (JSON parsing)
- [ ] For each rule, extracts:
  - `name` (string)
  - `protocol` array (list of strings like "Https:443")
  - `source_addresses` array (list of strings)
  - `source_ip_groups` array (list of strings, optional)
  - `target_fqdns` array (list of strings)
  - `fqdn_tags` array (list of strings, optional)
  - `description` (string, may be null/empty)
- [ ] Method handles missing properties gracefully (returns empty lists for optional arrays)
- [ ] Method handles null or missing state object (returns empty list)
- [ ] Method handles malformed JSON (returns empty list or logs warning)
- [ ] Empty lists are returned as `Array.Empty<string>()` or `[]` (C# 12)
- [ ] Method uses System.Text.Json APIs for parsing

**Dependencies:** Task 3 (Core Factory Structure)

**Testing Approach:**
- Unit test - valid rule data returns correct ApplicationRuleValues
- Unit test - missing optional properties return empty lists
- Unit test - null state object returns empty list
- Unit test - malformed JSON returns empty list
- Use test data from Task 2 to verify real-world parsing

**Estimated Effort:** 45 minutes

**Notes:**
- Reference `FirewallNetworkRuleCollectionViewModelFactory.ExtractRules` for pattern
- Application rules use different property names than network rules (target_fqdns vs destination_addresses)
- Graceful handling is critical - template should never crash on unexpected data
- Consider using JsonElement for type-safe parsing

---

### Task 5: Implement Rule Change Detection Logic

**Priority:** High

**Description:**
Implement the private methods that compute added, modified, removed, and unchanged rules by comparing before and after rule lists. Rules are matched by name (case-insensitive), and properties are compared to detect modifications.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] `ComputeRuleChanges` method (or similar name) implemented:
  - Takes before and after rule lists
  - Returns categorized rules: added, modified, removed, unchanged
- [ ] **Added rules**: Rules in `after` not in `before` (by name, case-insensitive)
- [ ] **Removed rules**: Rules in `before` not in `after` (by name, case-insensitive)
- [ ] **Modified rules**: Rules in both with different property values
  - Compare: protocols, source_addresses, source_ip_groups, target_fqdns, fqdn_tags, description
  - Deep comparison for list properties (order-sensitive)
- [ ] **Unchanged rules**: Rules in both with identical property values
- [ ] Rule name comparison is case-insensitive (e.g., "Allow-GitHub" matches "allow-github")
- [ ] Method returns structured data (e.g., tuples or custom record) for each category
- [ ] XML documentation comments added

**Dependencies:** Task 4 (Rule Extraction)

**Testing Approach:**
- Unit test - added rule detected correctly
- Unit test - removed rule detected correctly
- Unit test - modified rule detected (single property change)
- Unit test - unchanged rule detected
- Unit test - case-insensitive name matching works
- Unit test - empty before/after lists handled

**Estimated Effort:** 1 hour

**Notes:**
- Reference `FirewallNetworkRuleCollectionViewModelFactory` for pattern
- Consider using dictionary/lookup for efficient name-based matching
- List comparison should be order-sensitive (["a", "b"] != ["b", "a"])
- Empty strings and null should be treated as equivalent for description

---

### Task 6: Implement List Formatting and Truncation

**Priority:** Medium

**Description:**
Implement helper methods for formatting string lists (protocols, FQDNs, source addresses) with semantic icons, comma separation, and truncation for long lists. This ensures consistent formatting across all properties.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] `FormatList` method signature:
  ```csharp
  private static string FormatList(
      string semanticIcon,
      IReadOnlyList<string> values,
      string emptyValue = "")
  ```
- [ ] Method formats lists as comma-separated values with semantic icon
  - Example: `🌐 github.com, *.github.io, api.github.com`
- [ ] Method truncates lists > 5 items to first 3 plus count indicator
  - Example: `🌐 fqdn1.com, fqdn2.com, fqdn3.com, ... +7 more`
- [ ] Method returns `emptyValue` if list is empty or null
- [ ] Method uses `ScribanHelpers.EscapeMarkdown` for each value to prevent formatting issues
- [ ] Semantic icons used:
  - Protocols: `🔌` or none (use existing pattern from network rules)
  - Source addresses: `📍` or none
  - FQDNs: `🌐` or none
  - IP groups: `🔗` or none
- [ ] XML documentation comments added

**Dependencies:** Task 4 (Rule Extraction)

**Testing Approach:**
- Unit test - short list formatted correctly (< 5 items)
- Unit test - long list truncated correctly (> 5 items)
- Unit test - empty list returns emptyValue
- Unit test - markdown special characters escaped
- Snapshot test - verify visual appearance in rendered markdown

**Estimated Effort:** 45 minutes

**Notes:**
- Truncation threshold (5) should be a constant for easy adjustment
- Consider reusing icon constants from ScribanHelpers or network rule factory if they exist
- Comma-space separation for readability: `", "` not just `","`
- Decide whether to use semantic icons or keep it minimal like network rules (maintainer preference)

---

### Task 7: Implement Inline Diff Formatting for Modified Rules

**Priority:** Medium

**Description:**
Implement helper methods that compare before/after values for rule properties and generate inline diffs using strikethrough/insertion markup. Only properties that actually changed should show diffs; unchanged properties show a single value.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] `FormatListDiff` method signature:
  ```csharp
  private static string FormatListDiff(
      string semanticIcon,
      IReadOnlyList<string> beforeValues,
      IReadOnlyList<string> afterValues,
      string beforeSemanticFormat,
      string afterSemanticFormat)
  ```
- [ ] Method compares before and after lists:
  - If identical: return formatted single value (no diff)
  - If different: return inline diff using `ScribanHelpers.FormatDiff`
- [ ] Diff format uses strikethrough and insertion (or `ScribanHelpers.FormatDiff` conventions)
  - Example: `<del style="color:#E5534B;">10.0.0.0/24</del> <ins style="color:#46954A;">10.0.0.0/16</ins>`
- [ ] Method handles empty before or after values gracefully
- [ ] Method reuses `ScribanHelpers.FormatDiff` for consistency (don't reinvent formatting)
- [ ] Handles string properties (description) with simple before/after diff
- [ ] XML documentation comments added

**Dependencies:** Task 6 (List Formatting)

**Testing Approach:**
- Unit test - identical lists return single value (no diff)
- Unit test - different lists return inline diff
- Unit test - empty before or after handled correctly
- Unit test - diff format matches ScribanHelpers conventions
- Snapshot test - verify visual appearance in rendered markdown

**Estimated Effort:** 45 minutes

**Notes:**
- Reference `FirewallNetworkRuleCollectionViewModelFactory` for diff pattern
- Diff should be concise - full before/after, not per-item diffs for lists
- Consider reusing existing diff helpers to avoid code duplication
- Description diffs may need special handling for empty strings

---

### Task 8: Implement Row Builder Methods for Change Scenarios

**Priority:** High

**Description:**
Implement private methods that create `FirewallApplicationRuleChangeRowViewModel` and `FirewallApplicationRuleRowViewModel` instances for each change scenario (added, modified, removed, unchanged). These methods apply formatting, diffs, and change indicators.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] `CreateAddedRow` method: Takes after rule, returns row with ➕ indicator and formatted values (no diffs)
- [ ] `CreateModifiedRow` method: Takes before and after rules, returns row with 🔄 indicator and inline diffs for changed properties
- [ ] `CreateRemovedRow` method: Takes before rule, returns row with ❌ indicator and formatted before values
- [ ] `CreateUnchangedRow` method: Takes rule, returns row with ⏺️ indicator and formatted values
- [ ] `CreateSimpleRow` method: Takes rule, returns row without change indicator (for create/delete tables)
- [ ] All methods use formatting helpers from Tasks 6 and 7
- [ ] Modified rows only show diffs for properties that actually changed (unchanged properties show single value)
- [ ] Optional properties (source_ip_groups, fqdn_tags) handled correctly:
  - If empty in both before/after, show empty string (no diff)
  - If changed, show diff
- [ ] XML documentation comments added

**Dependencies:** Task 5 (Change Detection), Task 6 (List Formatting), Task 7 (Inline Diffs)

**Testing Approach:**
- Unit test - added row has correct indicator and no diffs
- Unit test - modified row has correct indicator and diffs only for changed properties
- Unit test - removed row has correct indicator and before values
- Unit test - unchanged row has correct indicator and single values
- Unit test - simple row has no indicator
- Snapshot test - verify all change types in rendered markdown

**Estimated Effort:** 1 hour

**Notes:**
- Change indicators are UTF-8 symbols: ➕ (U+2795), 🔄 (U+1F504), ❌ (U+274C), ⏺️ (U+23FA)
- Reference `FirewallNetworkRuleCollectionViewModelFactory` for pattern
- Consider helper method to determine which properties changed to reduce duplication in CreateModifiedRow
- Empty optional properties should result in empty strings, not "null" or "-"

---

### Task 9: Complete Build Method with Action Routing

**Priority:** High

**Description:**
Complete the `Build` method in the factory to handle all Terraform action types (create, update, delete) by routing to the appropriate logic and populating the correct view model properties (RuleChanges, AfterRules, or BeforeRules).

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] `Build` method determines action type from `resourceChange` object (create, update, delete, etc.)
- [ ] **For create actions**:
  - Extract rules from `after` state
  - Build `FirewallApplicationRuleRowViewModel` list (no change indicators)
  - Populate `AfterRules` property
  - Leave `RuleChanges` and `BeforeRules` empty
- [ ] **For update actions**:
  - Extract rules from both `before` and `after` states
  - Compute rule changes (added, modified, removed, unchanged)
  - Build `FirewallApplicationRuleChangeRowViewModel` list with change indicators and diffs
  - Populate `RuleChanges` property
  - Leave `AfterRules` and `BeforeRules` empty
- [ ] **For delete actions**:
  - Extract rules from `before` state
  - Build `FirewallApplicationRuleRowViewModel` list (no change indicators)
  - Populate `BeforeRules` property
  - Leave `RuleChanges` and `AfterRules` empty
- [ ] **For other actions** (replace, read, no-op):
  - Handle gracefully (similar to update or return empty view model)
- [ ] Method returns fully populated `FirewallApplicationRuleCollectionViewModel`
- [ ] XML documentation comments updated

**Dependencies:** Task 8 (Row Builders)

**Testing Approach:**
- Unit test - create action populates AfterRules only
- Unit test - update action populates RuleChanges only
- Unit test - delete action populates BeforeRules only
- Integration test - build view model from test data (Task 2)
- Snapshot test - verify template rendering for all actions

**Estimated Effort:** 45 minutes

**Notes:**
- Action detection: check `resourceChange.Change.Actions` (typically a list)
- Common actions: `["create"]`, `["update"]`, `["delete"]`, `["create", "delete"]` (replace)
- If rules are not available (computed), return empty lists to trigger fallback in template
- Ensure action routing matches existing patterns from network rule factory

---

### Task 10: Implement Summary Generation Method

**Priority:** Medium

**Description:**
Implement the `BuildChangedAttributesSummary` method that generates a concise summary line for update actions showing the count and sample of changed rules. This summary appears in the resource change summary line in the report.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallApplicationRuleCollectionViewModelFactory.cs`

**Acceptance Criteria:**
- [ ] `BuildChangedAttributesSummary` method signature:
  ```csharp
  public static string BuildChangedAttributesSummary(
      FirewallApplicationRuleCollectionViewModel viewModel,
      string action)
  ```
- [ ] Method returns empty string for non-update actions (create, delete)
- [ ] Method returns empty string if no rule changes exist
- [ ] Method generates summary format: `N🔧 change1, change2, change3`
  - `N` = total count of changed rules (added + modified + removed)
  - `change1, change2, change3` = up to first 3 changes with indicators
  - Example: `3🔧 ➕ allow-github, 🔄 allow-microsoft, ❌ allow-old-site`
- [ ] Method truncates to first 3 changes if more than 3 exist
- [ ] Method uses change indicators from view model rows (➕/🔄/❌)
- [ ] Method escapes rule names if they contain special markdown characters
- [ ] XML documentation comments added

**Dependencies:** Task 9 (Complete Build Method)

**Testing Approach:**
- Unit test - non-update action returns empty
- Unit test - no changes returns empty
- Unit test - single change generates correct summary
- Unit test - more than 3 changes truncates correctly
- Unit test - change indicators included in summary
- Unit test - rule names with special chars escaped

**Estimated Effort:** 30 minutes

**Notes:**
- Reference `FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary`
- Summary is displayed in the collapsed state of the resource change, so it should be concise
- Ensure consistent wrench emoji (🔧 U+1F527) used across all resource types
- Consider reusing existing summary formatting logic if available

---

### Task 11: Create Scriban Template - Basic Structure

**Priority:** High

**Description:**
Create the Scriban template file with the basic HTML structure (collapsible details, summary line, collection metadata) but without the rule tables yet. This establishes the template skeleton before implementing conditional rendering logic.

**Files to Create:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_application_rule_collection.sbn`

**Acceptance Criteria:**
- [ ] Template file created at correct location
- [ ] Outer `<details>` container with styling:
  ```html
  <details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
  ```
- [ ] Summary line includes:
  - `{{ change.summary_html }}` (action icon + resource type + name)
- [ ] Code analysis metadata included: `{{~ include '_code_analysis_metadata' ~}}`
- [ ] Collection metadata section:
  - **Collection:** `{{ change.firewall_application_rule_collection.name }}`
  - **Priority:** `{{ change.firewall_application_rule_collection.priority }}`
  - **Action:** `{{ change.firewall_application_rule_collection.action }}`
- [ ] Placeholder comment for rule tables (to be implemented in next task)
- [ ] Code analysis findings included: `{{~ include '_code_analysis_findings' ~}}`
- [ ] Closing `</details>` tag
- [ ] Template uses `~` for whitespace control where appropriate (no extra blank lines)

**Dependencies:** Task 1 (View Model Classes)

**Testing Approach:**
- Manual render test - render template with minimal view model (no rules)
- Visual inspection - verify HTML structure and styling
- Whitespace check - ensure no consecutive blank lines
- Compare against network rule template for consistency

**Estimated Effort:** 30 minutes

**Notes:**
- Reference `firewall_network_rule_collection.sbn` for structure and styling
- The `~` in Scriban strips whitespace to prevent extra blank lines
- Collection metadata should be bold and use pipes as separators
- Action formatting (with icons) is done in the factory, not the template

---

### Task 12: Add Rule Tables to Scriban Template

**Priority:** High

**Description:**
Add conditional rendering logic and markdown tables to the Scriban template for displaying rules in create, update, and delete scenarios. Each scenario uses different table structures and headings.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_application_rule_collection.sbn`

**Acceptance Criteria:**
- [ ] **Update scenario** (when `rule_changes` is not empty):
  - Heading: `#### Rule Changes`
  - Table columns: `Change | Rule Name | Protocols | Source Addresses | Source IP Groups | Target FQDNs | FQDN Tags | Description`
  - Iterate over `change.firewall_application_rule_collection.rule_changes`
  - Each row: `| {{ row.change }} | {{ row.name }} | {{ row.protocols }} | {{ row.source_addresses }} | {{ row.source_ip_groups }} | {{ row.target_fqdns }} | {{ row.fqdn_tags }} | {{ row.description }} |`
- [ ] **Create scenario** (when `after_rules` is not empty):
  - Heading: `#### Rules`
  - Table columns: `Rule Name | Protocols | Source Addresses | Source IP Groups | Target FQDNs | FQDN Tags | Description` (no Change column)
  - Iterate over `change.firewall_application_rule_collection.after_rules`
- [ ] **Delete scenario** (when `before_rules` is not empty):
  - Heading: `#### Rules (being deleted)`
  - Table columns: Same as create scenario
  - Iterate over `change.firewall_application_rule_collection.before_rules`
- [ ] **Fallback scenario** (when no rules available):
  - Show attribute changes table: `{{~ include '_attribute_changes' ~}}`
- [ ] Conditional logic uses `if`/`else if`/`else` correctly
- [ ] No blank lines between table header and rows
- [ ] Whitespace control (`~`) used to prevent extra blank lines

**Dependencies:** Task 11 (Template Basic Structure)

**Testing Approach:**
- Manual render test - render template with create scenario
- Manual render test - render template with update scenario (all change types)
- Manual render test - render template with delete scenario
- Manual render test - render template with fallback scenario (empty rules)
- Visual inspection - verify table formatting in markdown preview
- Snapshot test - compare against expected markdown

**Estimated Effort:** 1 hour

**Notes:**
- Reference `firewall_network_rule_collection.sbn` for conditional logic pattern
- Ensure table rows are consecutive (no blank lines between rows)
- Optional columns (Source IP Groups, FQDN Tags) are always included, but may have empty cells
- Future enhancement could hide optional columns when all cells are empty, but not required for initial implementation

---

### Task 13: Register Factory Adapter and Update ResourceChangeModel

**Priority:** High

**Description:**
Create the factory adapter class that bridges the view model factory to the rendering pipeline, register it in `AzureRMModule`, and add the view model property to `ResourceChangeModel`. This wires the factory into the tfplan2md architecture.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/Factories.cs` (add adapter class)
- `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs` (register factory)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs` (add property)

**Acceptance Criteria:**
- [ ] `FirewallApplicationRuleCollectionFactory` class added to `Factories.cs`:
  ```csharp
  internal sealed class FirewallApplicationRuleCollectionFactory : IResourceViewModelFactory
  {
      private readonly LargeValueFormat _largeValueFormat;
      
      internal FirewallApplicationRuleCollectionFactory(LargeValueFormat largeValueFormat) { ... }
      
      public void ApplyViewModel(
          ResourceChangeModel model,
          Parsing.ResourceChange resourceChange,
          string action,
          IReadOnlyList<AttributeChangeModel> attributeChanges) { ... }
  }
  ```
- [ ] `ApplyViewModel` method calls `FirewallApplicationRuleCollectionViewModelFactory.Build` and assigns to model
- [ ] `ApplyViewModel` method calls `FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary` and assigns to `model.ChangedAttributesSummary`
- [ ] Factory registered in `AzureRMModule.RegisterFactories` method:
  ```csharp
  registry.RegisterFactory(
      "azurerm_firewall_application_rule_collection", 
      new FirewallApplicationRuleCollectionFactory(_largeValueFormat));
  ```
- [ ] `FirewallApplicationRuleCollection` property added to `ResourceChangeModel`:
  ```csharp
  /// <summary>
  /// Gets or sets the precomputed view model for azurerm_firewall_application_rule_collection resources.
  /// Related feature: docs/features/040-azurerm-firewall-application-rule-template/specification.md.
  /// </summary>
  public FirewallApplicationRuleCollectionViewModel? FirewallApplicationRuleCollection { get; set; }
  ```
- [ ] Code compiles without warnings
- [ ] All namespaces correctly resolved

**Dependencies:** Task 9 (Complete Build Method), Task 10 (Summary Method), Task 12 (Template)

**Testing Approach:**
- Compile check - ensure registration compiles
- Integration test - render report with application rule collection
- Verify factory is invoked for azurerm_firewall_application_rule_collection resources
- Verify template is used (not default template)

**Estimated Effort:** 30 minutes

**Notes:**
- Reference existing factory adapters in `Factories.cs` (e.g., `FirewallNetworkRuleCollectionFactory`)
- Ensure resource type string matches Terraform resource type exactly
- The factory adapter is internal, not public
- ResourceChangeModel property should follow existing naming convention

---

### Task 14: Create Unit Tests for Summary Generation

**Priority:** Medium

**Description:**
Create unit tests for the `BuildChangedAttributesSummary` method to verify summary line generation logic. This mirrors the existing `FirewallNetworkRuleCollectionSummaryTests`.

**Files to Create:**
- `tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

**Acceptance Criteria:**
- [ ] Test class created with proper namespace and naming
- [ ] Test: `BuildChangedAttributesSummary_WhenNotUpdate_ReturnsEmpty`
  - Verify non-update actions (create, delete) return empty string
- [ ] Test: `BuildChangedAttributesSummary_WhenNoChanges_ReturnsEmpty`
  - Verify update action with no rule changes returns empty string
- [ ] Test: `BuildChangedAttributesSummary_WhenOneChange_ReturnsFormattedSummary`
  - Verify single change generates correct format (e.g., "1🔧 ➕ allow-github")
- [ ] Test: `BuildChangedAttributesSummary_WhenThreeChanges_ReturnsAllThree`
  - Verify 3 changes are all included in summary
- [ ] Test: `BuildChangedAttributesSummary_WhenMoreThanThreeChanges_Truncates`
  - Verify only first 3 changes shown when more than 3 exist
  - Verify count reflects total changes (e.g., "5🔧 ...")
- [ ] Test: `BuildChangedAttributesSummary_WhenNameNotCodeWrapped_PreservesText`
  - Verify rule names are displayed without extra code formatting
- [ ] Tests use TUnit framework and AwesomeAssertions
- [ ] All tests pass

**Dependencies:** Task 10 (Summary Method)

**Testing Approach:**
- Run tests: `dotnet test --filter FirewallApplicationRuleCollectionSummaryTests`
- Verify all tests pass
- Verify test coverage of summary logic

**Estimated Effort:** 45 minutes

**Notes:**
- Reference `tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallNetworkRuleCollectionSummaryTests.cs`
- Use fluent assertions for readability
- Tests should create minimal view models (not full integration)
- Consider edge cases: null rule names, empty descriptions, special characters

---

### Task 15: Create Expected Markdown Snapshot

**Priority:** Medium

**Description:**
Create the expected markdown snapshot file that shows the correct rendering of all application rule scenarios. This snapshot will be used for regression testing and serves as the source of truth for template output.

**Files to Create:**
- `examples/firewall-application-rules-demo/expected.md`

**Acceptance Criteria:**
- [ ] Markdown file contains rendered output for all scenarios from test data (Task 2):
  - Create scenario with all rules displayed
  - Update scenario with all change types (added, modified, removed, unchanged)
  - Delete scenario with rules being deleted
  - Fallback scenario with attribute changes
- [ ] Rendered output matches specification examples (specification.md lines 84-171)
- [ ] All change indicators displayed correctly (➕/🔄/❌/⏺️)
- [ ] Inline diffs for modified properties use correct formatting
- [ ] Collection metadata displayed with icons (🟢 Allow, 🔴 Deny)
- [ ] Tables formatted correctly (pipes aligned, no blank rows)
- [ ] Optional properties (source_ip_groups, fqdn_tags) displayed when present
- [ ] FQDN lists truncated when > 5 items (if applicable in test data)
- [ ] File passes markdownlint validation
- [ ] No consecutive blank lines (max 1 blank line between sections)

**Dependencies:** Task 2 (Test Data), Task 12 (Template Complete)

**Testing Approach:**
- Generate snapshot: `tfplan2md examples/firewall-application-rules-demo/plan.json > examples/firewall-application-rules-demo/expected.md`
- Visual inspection - verify output matches specification examples
- Markdownlint - run `npm run lint:md` to verify no linting errors
- Manual testing - copy snapshot to GitHub comment to verify rendering

**Estimated Effort:** 30 minutes

**Notes:**
- This snapshot serves as the source of truth for template output
- Any future changes to template should update this snapshot
- Snapshot should be human-readable and representative of real-world output
- If tfplan2md is not yet working, create snapshot manually and verify after integration

---

### Task 16: Create Integration/Regression Test

**Priority:** Medium

**Description:**
Add the test data and expected snapshot to the automated test suite so that rendering is validated on every test run. This ensures no regressions in template rendering.

**Test Infrastructure:**
The existing test infrastructure automatically discovers and validates test cases in the `examples/` directory. No new test class is needed unless custom validation is required.

**Acceptance Criteria:**
- [ ] Test data `examples/firewall-application-rules-demo/plan.json` is in place
- [ ] Expected snapshot `examples/firewall-application-rules-demo/expected.md` is in place
- [ ] Run `dotnet test` and verify test passes
- [ ] Test validates that rendered output matches expected snapshot
- [ ] Test fails if template changes break expected output
- [ ] CI/CD pipeline includes this test (no special configuration needed)

**Dependencies:** Task 2 (Test Data), Task 15 (Expected Snapshot), Task 13 (Factory Registration)

**Testing Approach:**
- Run full test suite: `dotnet test`
- Verify new test case is executed
- Verify test passes with current implementation
- Intentionally break template and verify test fails (negative test)
- Fix template and verify test passes again

**Estimated Effort:** 15 minutes

**Notes:**
- The test infrastructure in `tests/Oocx.TfPlan2Md.TUnit/` automatically discovers examples
- No custom test class needed unless special validation is required
- If test fails, investigate differences between expected and actual output
- Consider using `update-test-snapshots` skill if expected output needs updating

---

### Task 17: Validation - End-to-End Testing

**Priority:** High

**Description:**
Perform comprehensive end-to-end validation of the implementation using the test data and real-world scenarios. Verify that all acceptance criteria from the specification are met and the feature works as expected.

**Validation Steps:**
1. **Build and Test:**
   - Run `dotnet build` - verify no compilation errors
   - Run `dotnet test` - verify all tests pass
   - Run `npm run lint:md` - verify markdown lint passes

2. **Template Rendering:**
   - Generate report: `tfplan2md examples/firewall-application-rules-demo/plan.json -o output.md`
   - Verify output matches expected snapshot
   - Visual inspection of all scenarios (create, update, delete, fallback)

3. **Markdown Validation:**
   - Copy output to GitHub comment/PR and verify rendering
   - Verify tables display correctly
   - Verify collapsible sections work
   - Verify inline diffs are readable

4. **Edge Cases:**
   - Verify FQDN truncation for long lists
   - Verify optional properties render correctly
   - Verify empty descriptions handled gracefully
   - Verify case-insensitive rule name matching

5. **Regression Testing:**
   - Run full test suite to ensure no existing tests broke
   - Verify network rule collection template still works
   - Verify other resource templates unaffected

**Acceptance Criteria:**
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Snapshot test passes
- [ ] Markdown lint passes
- [ ] Template renders all scenarios correctly (create, update, delete, fallback)
- [ ] Change indicators displayed correctly (➕/🔄/❌/⏺️)
- [ ] Inline diffs readable and accurate
- [ ] Collection metadata displayed with icons
- [ ] Optional properties (source_ip_groups, fqdn_tags) render correctly
- [ ] FQDN lists truncate when > 5 items
- [ ] No consecutive blank lines in output
- [ ] No markdown rendering errors in GitHub
- [ ] No regressions in existing functionality

**Dependencies:** All previous tasks

**Estimated Effort:** 1 hour

**Notes:**
- This is the final validation before UAT
- Any issues found should be fixed and tests re-run
- Document any known limitations or edge cases for UAT
- Ensure test data covers all scenarios from specification

---

### Task 18: Code Review and Security Scan

**Priority:** High

**Description:**
Request automated code review and run CodeQL security scan to identify any issues before UAT. Address all findings.

**Steps:**
1. **Commit all changes:**
   - Ensure all files are committed to the feature branch
   - Run `git status` to verify clean working directory

2. **Request code review:**
   - Use `code_review` tool with PR title and description
   - Review comments and decide which to address
   - Fix relevant issues

3. **Run security scan:**
   - Use `codeql_checker` tool to scan for vulnerabilities
   - Investigate all alerts
   - Fix any issues that require localized changes
   - Document any false positives or unfixable issues

4. **Re-run validation:**
   - Run `dotnet test` after fixes
   - Verify no new issues introduced

**Acceptance Criteria:**
- [ ] All changes committed to feature branch
- [ ] Code review completed via `code_review` tool
- [ ] All relevant code review comments addressed
- [ ] CodeQL scan completed via `codeql_checker` tool
- [ ] All security alerts investigated
- [ ] Fixable security issues resolved
- [ ] Security summary created documenting any unfixed issues or false positives
- [ ] All tests pass after fixes
- [ ] No new compiler warnings introduced

**Dependencies:** Task 17 (Validation)

**Estimated Effort:** 30 minutes

**Notes:**
- Code review tool may produce false positives - use judgment
- Security issues in template rendering are rare but possible (e.g., XSS)
- Document any intentional deviations from suggested fixes
- Re-run `code_review` if significant changes made

---

## Implementation Order

Recommended sequence for implementing tasks:

1. **Phase 1: Foundation** (Test data and view models)
   - Task 2: Create Test Data JSON File
   - Task 1: Create View Model Classes

2. **Phase 2: Factory Logic** (Core processing)
   - Task 3: Create View Model Factory - Core Structure
   - Task 4: Implement Rule Extraction and Parsing
   - Task 5: Implement Rule Change Detection Logic

3. **Phase 3: Formatting** (Display logic)
   - Task 6: Implement List Formatting and Truncation
   - Task 7: Implement Inline Diff Formatting for Modified Rules
   - Task 8: Implement Row Builder Methods for Change Scenarios

4. **Phase 4: Integration** (Complete factory and template)
   - Task 9: Complete Build Method with Action Routing
   - Task 10: Implement Summary Generation Method
   - Task 11: Create Scriban Template - Basic Structure
   - Task 12: Add Rule Tables to Scriban Template
   - Task 13: Register Factory Adapter and Update ResourceChangeModel

5. **Phase 5: Testing** (Unit tests and snapshots)
   - Task 14: Create Unit Tests for Summary Generation
   - Task 15: Create Expected Markdown Snapshot
   - Task 16: Create Integration/Regression Test

6. **Phase 6: Validation** (End-to-end testing)
   - Task 17: Validation - End-to-End Testing

7. **Phase 7: Quality Assurance** (Code review and security)
   - Task 18: Code Review and Security Scan

---

## Open Questions

### Question 1: Semantic Icons for List Formatting

**Question:** Should we use semantic icons for properties (🌐 for FQDNs, 📍 for addresses) or keep it minimal like the network rule template?

**Options:**
1. Add semantic icons for visual clarity (more colorful but potentially distracting)
2. Keep it minimal without icons (consistent with network rules, cleaner)

**Impact:** Task 6 (List Formatting)

**Recommendation Needed From:** Maintainer (UX preference)

---

### Question 2: FQDN Truncation Threshold

**Question:** What should be the truncation threshold for FQDN lists?

**Current Proposal:** 5 items (show first 3 + "... +N more")

**Alternatives:**
- No truncation (may cause very wide tables)
- Lower threshold: 3 items (more aggressive truncation)
- Higher threshold: 10 items (less truncation)

**Impact:** Task 6 (List Formatting), Task 15 (Expected Snapshot)

**Recommendation Needed From:** Maintainer (UX preference)

---

### Question 3: Conditional Column Display

**Question:** Should optional columns (Source IP Groups, FQDN Tags) be hidden when all rules have empty values?

**Current Approach:** Always show columns, accept empty cells

**Alternative:** Hide columns when no rules use them (requires Scriban template logic enhancement)

**Impact:** Task 12 (Template), Task 15 (Expected Snapshot)

**Trade-offs:**
- ✅ Simpler template logic with always-visible columns
- ✅ Consistent table structure across all scenarios
- ⚠️ Empty columns may seem wasteful but don't break functionality

**Recommendation Needed From:** Maintainer (UX preference)

---

## Risks and Mitigations

### Risk 1: Test Data Generation Complexity

**Risk:** Creating realistic test data from real Terraform plans may be blocked by lack of Azure credentials or Terraform setup.

**Mitigation:**
- Start with manually created JSON based on Terraform documentation
- Derive structure from network rule test data as a reference
- Request real-world plan JSON from maintainer if available

**Impact:** Task 2 (Test Data)

---

### Risk 2: Inline Diff Formatting Inconsistency

**Risk:** Diff formatting may not match existing patterns, causing inconsistent visual appearance.

**Mitigation:**
- Reuse `ScribanHelpers.FormatDiff` explicitly
- Reference network rule collection factory for diff patterns
- Visual inspection during Task 17 (Validation)

**Impact:** Task 7 (Inline Diffs)

---

### Risk 3: Template Whitespace Issues

**Risk:** Scriban template may produce consecutive blank lines or extra whitespace, causing markdownlint failures.

**Mitigation:**
- Use `~` whitespace control liberally in template
- Run markdownlint early and often
- Reference network rule template for whitespace patterns

**Impact:** Task 11 (Template), Task 12 (Template)

---

### Risk 4: Optional Property Handling

**Risk:** Optional properties (source_ip_groups, fqdn_tags) may not render correctly when missing or when switching between them and main properties.

**Mitigation:**
- Always populate optional property strings (use empty string, not null)
- Test edge cases explicitly in Task 17 (Validation)
- Handle null/missing gracefully in extraction logic (Task 4)

**Impact:** Task 4 (Rule Extraction), Task 8 (Row Builders)

---

## Definition of Done

Implementation is complete and successful when:

- [ ] All 18 tasks completed with acceptance criteria met
- [ ] All unit tests pass (`dotnet test`)
- [ ] All integration/regression tests pass
- [ ] Markdown lint passes (`npm run lint:md`)
- [ ] Code compiles without warnings
- [ ] Template renders all scenarios correctly (create, update, delete, fallback)
- [ ] Change indicators displayed correctly (➕/🔄/❌/⏺️)
- [ ] Inline diffs readable and accurate for modified rules
- [ ] Collection metadata displayed with icons (🟢/🔴)
- [ ] Optional properties render correctly when present
- [ ] FQDN lists truncate when > 5 items
- [ ] No consecutive blank lines in template output
- [ ] No markdown rendering errors in GitHub preview
- [ ] Code review completed and relevant issues addressed
- [ ] CodeQL security scan completed and issues resolved
- [ ] No regressions in existing functionality
- [ ] Test coverage includes all scenarios from specification
- [ ] Documentation accurate (if updates needed)
- [ ] Ready for UAT testing

---

## Next Steps After Completion

Once all tasks are complete and validated:

1. **Push to remote branch** - Ensure all commits are pushed
2. **Prepare UAT artifacts:**
   - Generate feature-specific artifact: `tfplan2md examples/firewall-application-rules-demo/plan.json > artifacts/firewall-application-rules-uat.md`
   - Generate comprehensive demo (regression): Use `generate-demo-artifacts` skill
3. **Hand off to UAT Tester agent** - Use UAT plan for validation
4. **Address UAT feedback** - Fix any issues found during UAT
5. **Hand off to Technical Writer** - Update documentation if needed
6. **Hand off to Release Manager** - Create PR and merge to main

---

## References

- **Specification:** `docs/features/040-azurerm-firewall-application-rule-template/specification.md`
- **Architecture:** `docs/features/040-azurerm-firewall-application-rule-template/architecture.md`
- **Test Plan:** `docs/features/040-azurerm-firewall-application-rule-template/test-plan.md`
- **UAT Plan:** `docs/features/040-azurerm-firewall-application-rule-template/uat-test-plan.md`
- **Agent Workflow:** `docs/agents.md`
- **Project Spec:** `docs/spec.md`
- **Reference Template:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn`
- **Reference View Model:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModel.cs`
- **Reference Factory:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModelFactory.cs`
