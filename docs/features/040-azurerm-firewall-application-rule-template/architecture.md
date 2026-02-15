# Architecture: Custom Template for azurerm_firewall_application_rule_collection

## Status

Proposed

## Context

The specification (docs/features/040-azurerm-firewall-application-rule-template/specification.md) defines the need for a custom Scriban template and supporting infrastructure for `azurerm_firewall_application_rule_collection` resources. This will provide semantic diffing of application firewall rules, similar to the existing `azurerm_firewall_network_rule_collection` implementation.

**Problem:** The website documentation claims tfplan2md "Works for all `azurerm_firewall_network_rule_collection` and `azurerm_firewall_application_rule_collection` resources," but application rule collections currently fall back to the default template showing index-based attribute changes instead of semantic rule-by-rule diffing.

**Reference Implementation:** The existing `azurerm_firewall_network_rule_collection` template (src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn) and its supporting classes serve as the architectural pattern to follow.

## Answers to Open Questions

### Question 1: Handling Optional Application Rule Properties

**Decision:** Support core properties in the initial implementation with conditional display for optional properties.

**Rationale:**
- **Core properties** (required for most use cases):
  - `name` - Rule identifier
  - `protocols` - HTTP/HTTPS/MSSQL protocols with ports
  - `source_addresses` - Source IP addresses/CIDR ranges
  - `target_fqdns` - Destination fully qualified domain names
  - `description` - Rule documentation

- **Optional properties** (include with conditional columns):
  - `source_ip_groups` - Alternative to source_addresses
  - `fqdn_tags` - Alternative to target_fqdns
  - `web_categories` - Content filtering categories (less commonly used)

**Implementation Approach:**
1. Always display core properties in the table
2. For optional properties (source_ip_groups, fqdn_tags), include columns that show data when present, empty cells when not
3. Defer `web_categories` to a future enhancement if it proves complex or rarely used
4. The Scriban template will use conditional logic to display columns only when at least one rule uses them

**Trade-offs:**
- ✅ Simpler initial implementation focusing on common scenarios
- ✅ Extensible design allows future enhancement without breaking changes
- ✅ Follows existing patterns from network rule collection template
- ⚠️ May need schema refinement if web_categories are frequently used

### Question 2: Protocol Formatting

**Decision:** Display protocols exactly as provided in Terraform state (e.g., `Https:443`, `Http:80,8080`).

**Rationale:**
- Azure Firewall application rules use protocol+port format natively (unlike network rules which separate protocol and port)
- Splitting into separate columns would require parsing and complicate the implementation
- The format is already semantic and readable (e.g., `Https:443`)
- Maintains consistency with how Terraform and Azure Portal display these values
- Semantic icons (🔒) could be added in a future enhancement but are not essential

**Implementation:**
- Single "Protocols" column in the table
- Display comma-separated list when multiple protocols (e.g., `Http:80, Https:443`)
- Use existing `FormatList` and `FormatListDiff` helpers from FirewallNetworkRuleCollectionViewModelFactory

**Trade-offs:**
- ✅ Simple, consistent with Terraform state representation
- ✅ Avoids complex parsing logic
- ✅ Extensible to add icons later without breaking existing output
- ⚠️ Less granular than split columns, but sufficient for readability

### Question 3: FQDN List Formatting

**Decision:** Display as comma-separated values inline with truncation for very long lists.

**Rationale:**
- Most application rules have 1-5 FQDNs, which fit comfortably in a table cell
- Comma-separated format matches existing pattern in network rule collection (destination_addresses, destination_ports)
- Truncation prevents excessive table width while preserving critical information

**Implementation:**
- Display all FQDNs as comma-separated list (e.g., `github.com, *.github.io, api.github.com`)
- If list exceeds 5 items, truncate to first 3 and append "... +N more"
- Use existing markdown escaping helpers to prevent formatting issues
- Apply same approach to `fqdn_tags` when present

**Example:**
- Short list: `github.com, *.github.io`
- Long list: `*.microsoft.com, *.azure.com, *.windows.net, ... +7 more`

**Trade-offs:**
- ✅ Balances readability with completeness
- ✅ Consistent with existing network rule formatting
- ✅ Prevents extremely wide tables
- ⚠️ Truncation may hide details, but full data remains in attribute changes fallback

### Question 4: Web Categories Support

**Decision:** Defer `web_categories` to a future enhancement.

**Rationale:**
- Web categories are a specialized Azure Firewall Premium feature for content filtering
- Less commonly used compared to FQDN-based rules
- Specification already marks this as "Out of Scope" for initial implementation
- Can be added later without breaking existing templates or view models

**Future Enhancement Path:**
- Add `web_categories` property to view model
- Add conditional column to template (only shown when at least one rule uses categories)
- Update factory to extract and format categories
- Add test cases covering category scenarios

**Trade-offs:**
- ✅ Simplifies initial implementation
- ✅ Focuses on high-value, commonly-used features first
- ✅ No breaking changes to add later
- ⚠️ Users with Premium tier firewalls using web categories will see generic attribute changes instead of semantic diff (but this is acceptable for initial release)

### Question 5: Test Data Source

**Decision:** Generate test data from a real `terraform plan -json` output with application rule changes.

**Rationale:**
- **Real-world accuracy:** Ensures test data matches actual Terraform output structure
- **Reference examples exist:** The firewall-rules-demo folder already contains network rule examples we can extend
- **Proven pattern:** Existing tests use real Terraform plan JSON files (e.g., examples/firewall-rules-demo/plan.json)
- **Comprehensive coverage:** Real plans include edge cases and optional fields that manual creation might miss

**Implementation Approach:**
1. Create a minimal Terraform configuration with `azurerm_firewall_application_rule_collection`
2. Generate plan output showing create, update (with added/modified/removed rules), and delete scenarios
3. Extract the relevant `resource_changes` entries for test cases
4. Place test data in `examples/firewall-application-rules-demo/plan.json`
5. Create corresponding expected markdown snapshot

**Test Scenarios to Cover:**
- Create: New collection with 2-3 application rules
- Update: Collection with rules added, modified, removed, and unchanged
- Delete: Existing collection with rules
- Edge cases: Empty descriptions, multiple protocols, FQDN tags, source_ip_groups

**Trade-offs:**
- ✅ High confidence in data accuracy
- ✅ Comprehensive test coverage
- ✅ Documents real-world usage patterns
- ⚠️ Requires Terraform/Azure credentials to generate (one-time setup)
- ⚠️ Can start with manually created JSON based on network rules if Terraform setup is blocked

## Technical Design

### Architecture Pattern

The implementation follows the established **Factory → ViewModel → Template** pattern used by `azurerm_firewall_network_rule_collection` and other resource-specific templates:

1. **Scriban Template** (`.sbn`): Presentation layer that renders markdown from precomputed view model
2. **View Model Classes**: Strongly-typed data structures containing formatted strings ready for display
3. **View Model Factory**: Business logic that extracts rules from Terraform state, computes diffs, and formats values
4. **Factory Adapter**: Registration shim that connects the factory to the rendering pipeline
5. **Resource Change Model**: Central model with property for this resource's view model

This pattern is documented in docs/features/026-template-rendering-simplification/specification.md.

### Component Structure

```
src/Oocx.TfPlan2Md/
├── Providers/AzureRM/
│   ├── Templates/azurerm/
│   │   └── firewall_application_rule_collection.sbn       # NEW: Scriban template
│   ├── Models/
│   │   ├── FirewallApplicationRuleCollectionViewModel.cs  # NEW: View model classes
│   │   ├── FirewallApplicationRuleCollectionViewModelFactory.cs  # NEW: Factory logic
│   │   └── Factories.cs                                   # MODIFIED: Add adapter
│   └── AzureRMModule.cs                                   # MODIFIED: Register factory
└── MarkdownGeneration/
    └── ResourceChangeModel.cs                             # MODIFIED: Add property

examples/
└── firewall-application-rules-demo/                       # NEW: Test data
    ├── plan.json                                          # Plan with application rules
    └── expected.md                                        # Expected markdown output

tests/
└── Oocx.TfPlan2Md.TUnit/
    └── Providers/AzureRM/
        └── FirewallApplicationRuleCollectionSummaryTests.cs  # NEW: Unit tests
```

### View Model Design

Three classes mirror the network rule collection structure:

#### 1. FirewallApplicationRuleCollectionViewModel

Main view model containing collection metadata and rule collections:

```csharp
public sealed class FirewallApplicationRuleCollectionViewModel
{
    public string? Name { get; init; }
    public string? Priority { get; init; }
    public string? Action { get; init; }  // Formatted with icons (🟢 Allow, 🔴 Deny)
    
    // For update scenario (shows all rules with change indicators)
    public IReadOnlyList<FirewallApplicationRuleChangeRowViewModel> RuleChanges { get; init; }
    
    // For create scenario (shows only new rules)
    public IReadOnlyList<FirewallApplicationRuleRowViewModel> AfterRules { get; init; }
    
    // For delete scenario (shows only deleted rules)
    public IReadOnlyList<FirewallApplicationRuleRowViewModel> BeforeRules { get; init; }
}
```

#### 2. FirewallApplicationRuleChangeRowViewModel

Represents a single rule in an update table with change indicator and inline diffs:

```csharp
public sealed class FirewallApplicationRuleChangeRowViewModel
{
    public required string Change { get; init; }  // ➕/🔄/❌/⏺️
    public required string Name { get; init; }
    public required string Protocols { get; init; }  // Formatted or diff
    public required string SourceAddresses { get; init; }  // Formatted or diff
    public required string SourceIpGroups { get; init; }  // Formatted or diff (optional)
    public required string TargetFqdns { get; init; }  // Formatted or diff
    public required string FqdnTags { get; init; }  // Formatted or diff (optional)
    public required string Description { get; init; }  // Formatted or diff
}
```

#### 3. FirewallApplicationRuleRowViewModel

Represents a single rule in create/delete tables (no change indicators or diffs):

```csharp
public sealed class FirewallApplicationRuleRowViewModel
{
    public required string Name { get; init; }
    public required string Protocols { get; init; }
    public required string SourceAddresses { get; init; }
    public required string SourceIpGroups { get; init; }  // Optional
    public required string TargetFqdns { get; init; }
    public required string FqdnTags { get; init; }  // Optional
    public required string Description { get; init; }
}
```

**Design Notes:**
- All properties are formatted strings ready for markdown rendering (no raw JSON in view models)
- Optional properties (source_ip_groups, fqdn_tags) are always present but may be empty strings
- Uses `required` keyword for mandatory properties (C# 11 feature)
- Follows existing FirewallNetworkRuleCollectionViewModel structure for consistency

### Factory Implementation

The `FirewallApplicationRuleCollectionViewModelFactory` static class builds view models from Terraform plan data:

#### Key Methods

1. **Build(ResourceChange, string, LargeValueFormat) → ViewModel**
   - Main entry point called by factory adapter
   - Extracts collection metadata (name, priority, action)
   - Extracts before/after rules from JSON state
   - Computes added, modified, removed, unchanged rules
   - Formats values using ScribanHelpers
   - Returns populated view model

2. **BuildChangedAttributesSummary(ViewModel, string) → string**
   - Generates summary line for update actions (e.g., "3🔧 ➕ allow-github, 🔄 allow-microsoft, ❌ allow-old")
   - Truncates to first 3 changes plus count
   - Returns empty string for non-update actions

3. **ExtractRules(object?) → IReadOnlyList&lt;RuleValues&gt;**
   - Parses rule array from before/after JSON state
   - Handles missing or malformed data gracefully
   - Returns empty list if rules not available

4. **BuildAdded/BuildRemoved/BuildModified/BuildUnchanged(...)**
   - Computes rule changes by comparing rule names (case-insensitive)
   - Creates formatted row view models for each change type
   - Uses `FormatList`, `FormatListDiff`, `FormatDiff` helpers

5. **FormatList(string, IReadOnlyList&lt;string&gt;, string) → string**
   - Formats string lists with semantic icons
   - Applies truncation for long lists (> 5 items)
   - Returns comma-separated values

6. **FormatListDiff(string, before, after, string, string) → string**
   - Compares two lists and formats inline diff if different
   - Uses existing `FormatDiff` helper for strikethrough/insertion markup

#### Internal Data Structure

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

This internal record holds raw string values during diff computation before formatting.

### Scriban Template Structure

The template (`firewall_application_rule_collection.sbn`) follows the structure of `firewall_network_rule_collection.sbn`:

1. **Outer container**: Collapsible `<details>` with styled border
2. **Summary line**: Uses `change.summary_html` (includes action icon, resource type, name)
3. **Code analysis metadata**: Includes `_code_analysis_metadata.sbn`
4. **Collection metadata**: Name, priority, action (formatted with icons)
5. **Conditional rendering**:
   - **Update with rule changes**: Show "Rule Changes" table with all rules and change indicators
   - **Create with rules**: Show "Rules" table with new rules
   - **Delete with rules**: Show "Rules (being deleted)" table
   - **Fallback**: Show attribute changes table when rules not available
6. **Code analysis findings**: Includes `_code_analysis_findings.sbn`

**Table Columns** (Update scenario):
- Change | Rule Name | Protocols | Source Addresses | Source IP Groups | Target FQDNs | FQDN Tags | Description

**Conditional Columns:**
- Source IP Groups: Only show column if at least one rule has source_ip_groups
- FQDN Tags: Only show column if at least one rule has fqdn_tags

**Implementation Note:** Scriban templates do not support dynamic column removal easily. For initial implementation, include all columns and accept empty cells when optional properties are not used. A future enhancement could add conditional column logic if needed.

### Factory Adapter and Registration

#### 1. Factory Adapter (Factories.cs)

```csharp
internal sealed class FirewallApplicationRuleCollectionFactory : IResourceViewModelFactory
{
    private readonly LargeValueFormat _largeValueFormat;

    internal FirewallApplicationRuleCollectionFactory(LargeValueFormat largeValueFormat)
    {
        _largeValueFormat = largeValueFormat;
    }

    public void ApplyViewModel(
        ResourceChangeModel model,
        Parsing.ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
        var viewModel = FirewallApplicationRuleCollectionViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);

        model.FirewallApplicationRuleCollection = viewModel;
        model.ChangedAttributesSummary = FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(
            viewModel,
            action);
    }
}
```

#### 2. Registration (AzureRMModule.cs)

Add to `RegisterFactories` method:

```csharp
registry.RegisterFactory(
    "azurerm_firewall_application_rule_collection", 
    new FirewallApplicationRuleCollectionFactory(_largeValueFormat));
```

#### 3. Resource Change Model Property

Add to `ResourceChangeModel.cs`:

```csharp
/// <summary>
/// Gets or sets the precomputed view model for azurerm_firewall_application_rule_collection resources.
/// Related feature: docs/features/040-azurerm-firewall-application-rule-template/specification.md.
/// </summary>
public FirewallApplicationRuleCollectionViewModel? FirewallApplicationRuleCollection { get; set; }
```

### Dependencies

The implementation depends on:

1. **Existing Infrastructure:**
   - `ScribanHelpers.FormatAttributeValueTable/Plain` - Semantic formatting with icons
   - `ScribanHelpers.FormatDiff` - Inline diff formatting (strikethrough/insertion)
   - `ScribanHelpers.EscapeMarkdown` - Markdown escaping for safe rendering
   - `IResourceViewModelFactory` interface - Factory adapter pattern
   - `IResourceViewModelFactoryRegistry` - Factory registration

2. **Existing Templates:**
   - `_code_analysis_metadata.sbn` - Code analysis badges
   - `_code_analysis_findings.sbn` - Security findings display

3. **External Libraries:**
   - `Scriban` - Template rendering engine
   - `System.Text.Json` - JSON parsing

4. **Test Infrastructure:**
   - `TUnit` - Test framework
   - `AwesomeAssertions` - Fluent assertions

**No new dependencies required.** All necessary infrastructure exists.

### Testing Approach

#### 1. View Model Factory Unit Tests

Create `FirewallApplicationRuleCollectionSummaryTests.cs` mirroring the network rule collection tests:

- `BuildChangedAttributesSummary_WhenNotUpdate_ReturnsEmpty()`
- `BuildChangedAttributesSummary_WhenNoChanges_ReturnsEmpty()`
- `BuildChangedAttributesSummary_WhenMoreThanThreeChanges_Truncates()`
- `BuildChangedAttributesSummary_WhenNameNotCodeWrapped_PreservesText()`

#### 2. Integration/Regression Tests

Create test data and expected markdown snapshots:

**Test Data:** `examples/firewall-application-rules-demo/plan.json`
- Contains resource_changes array with application rule collection scenarios
- Includes create, update (with all change types), and delete actions

**Expected Snapshots:** `examples/firewall-application-rules-demo/expected.md`
- Shows complete rendered markdown for all scenarios
- Used for visual regression testing

**Test Execution:**
- Existing test infrastructure will automatically discover and validate new test data
- Run: `dotnet test` to verify template rendering matches expected output

#### 3. Test Scenarios

Ensure coverage of:
- ✅ Create with 2-3 rules (various protocols, FQDNs)
- ✅ Update with added rule
- ✅ Update with modified rule (protocol change, FQDN change, description change)
- ✅ Update with removed rule
- ✅ Update with unchanged rule
- ✅ Update with multiple changes (added + modified + removed)
- ✅ Delete with rules
- ✅ Fallback to attribute changes when rules are computed
- ✅ Optional properties: source_ip_groups present
- ✅ Optional properties: fqdn_tags present
- ✅ Edge cases: empty descriptions, long FQDN lists (truncation)

## Implementation Guidance

The Developer agent should implement components in this order:

### Phase 1: View Model Classes
1. Create `FirewallApplicationRuleCollectionViewModel.cs`
2. Create `FirewallApplicationRuleChangeRowViewModel`
3. Create `FirewallApplicationRuleRowViewModel`
4. Follow existing FirewallNetworkRuleCollectionViewModel structure exactly
5. Use `required` keyword for mandatory properties
6. Add XML documentation comments for all properties

### Phase 2: View Model Factory
1. Create `FirewallApplicationRuleCollectionViewModelFactory.cs`
2. Implement `Build(ResourceChange, string, LargeValueFormat)` method
3. Implement `BuildChangedAttributesSummary(ViewModel, string)` method
4. Implement private helper methods:
   - `ExtractRules(object?)`
   - `BuildAdded/BuildRemoved/BuildModified/BuildUnchanged(...)`
   - `FormatRuleRows(...)`
   - `CreateAddedRow/CreateRemovedRow/CreateUnchangedRow/CreateDiffRow(...)`
   - `FormatList(...)` with truncation logic
   - `FormatListDiff(...)`
   - Rule comparison helpers
5. Use existing ScribanHelpers functions (don't reinvent formatting)
6. Follow FirewallNetworkRuleCollectionViewModelFactory as a reference

### Phase 3: Template
1. Create `firewall_application_rule_collection.sbn`
2. Copy structure from `firewall_network_rule_collection.sbn`
3. Adjust table columns for application rule properties
4. Update conditional rendering for optional columns (source_ip_groups, fqdn_tags)
5. Test template rendering manually before integration

### Phase 4: Registration
1. Add `FirewallApplicationRuleCollectionFactory` class to `Factories.cs`
2. Register factory in `AzureRMModule.RegisterFactories` method
3. Add `FirewallApplicationRuleCollection` property to `ResourceChangeModel`

### Phase 5: Test Data
1. Create Terraform configuration with azurerm_firewall_application_rule_collection
2. Generate plan.json with create, update, delete scenarios
3. Place in `examples/firewall-application-rules-demo/plan.json`
4. Generate expected markdown output
5. Place in `examples/firewall-application-rules-demo/expected.md`

### Phase 6: Unit Tests
1. Create `FirewallApplicationRuleCollectionSummaryTests.cs`
2. Mirror FirewallNetworkRuleCollectionSummaryTests structure
3. Test summary generation logic
4. Test edge cases

### Phase 7: Validation
1. Run all tests: `dotnet test`
2. Run markdown lint: `npm run lint:md`
3. Verify template output matches specification examples
4. Test with real Terraform plans if available

## Deviations from Network Rule Collection Pattern

### Differences from Network Rules

| Aspect | Network Rules | Application Rules |
|--------|---------------|-------------------|
| **Property: protocols** | Simple list (TCP, UDP, ICMP, Any) | Protocol + port format (Https:443, Http:80) |
| **Property: destination** | destination_addresses + destination_ports | target_fqdns (FQDNs instead of IPs) |
| **Property: source alternatives** | destination_ip_groups | source_ip_groups |
| **Property: FQDN tags** | N/A | fqdn_tags (Azure-defined FQDN groups) |
| **Property: web categories** | N/A | web_categories (content filtering - deferred) |
| **Columns in table** | 6 columns | 7 columns (8 if web_categories added later) |

### Shared Patterns

These patterns remain identical:
- ✅ Change detection by rule name (case-insensitive)
- ✅ Change indicators (➕/🔄/❌/⏺️)
- ✅ Inline diffs for modified properties
- ✅ Action icons (🟢 Allow, 🔴 Deny)
- ✅ Summary line format ("3🔧 ➕ rule1, 🔄 rule2, ❌ rule3")
- ✅ Fallback to attribute changes when rules not available
- ✅ Collapsible details sections
- ✅ Code analysis integration

### Code Reuse Opportunities

The factory can reuse these existing helpers without modification:
- `ScribanHelpers.FormatAttributeValueTable/Plain`
- `ScribanHelpers.FormatDiff`
- `ScribanHelpers.EscapeMarkdown`
- `ScribanHelpers.FormatCodeSummary`
- Rule comparison logic (adapted from network rules)
- List formatting patterns

## Risks and Mitigations

### Risk 1: Azure Firewall Schema Changes
**Risk:** Azure provider may add new properties to application rules in future Terraform versions.

**Mitigation:**
- Template design allows new properties to be added without breaking existing output
- Unknown properties fall back to generic attribute changes table
- Version constraints in dependencies ensure stability

### Risk 2: Incomplete Test Coverage
**Risk:** Real-world Terraform plans may contain edge cases not covered by test data.

**Mitigation:**
- Generate test data from real Terraform plans (not manual creation)
- Include diverse scenarios in test data (multiple protocols, long FQDN lists, etc.)
- Monitor real-world usage and add test cases for reported issues

### Risk 3: Template Complexity with Conditional Columns
**Risk:** Scriban templates have limited support for dynamic column creation/removal.

**Mitigation:**
- Accept empty cells for optional columns in initial implementation
- Keep column count reasonable (7-8 columns)
- Future enhancement could split into multiple tables if needed

### Risk 4: Protocol Formatting Ambiguity
**Risk:** Protocols with multiple ports (e.g., `Http:80,8080,8081`) may cause parsing confusion.

**Mitigation:**
- Display exactly as provided in Terraform state (no parsing required)
- Use existing list formatting helpers that handle commas correctly
- Document format in template comments

### Risk 5: FQDN List Truncation
**Risk:** Truncating long FQDN lists may hide important information.

**Mitigation:**
- Truncation threshold (5 items) is configurable in factory
- Full data remains available in attribute changes fallback
- Summary indicates "... +N more" so reviewers know to check details

## Success Criteria

Implementation is complete and successful when:

- [ ] All view model classes created with proper XML documentation
- [ ] View model factory correctly extracts and formats application rules
- [ ] Factory adapter created and registered in AzureRMModule
- [ ] ResourceChangeModel property added
- [ ] Scriban template created with correct structure
- [ ] Template renders all scenarios correctly (create, update, delete, fallback)
- [ ] Test data file created with realistic application rule changes
- [ ] Expected markdown snapshot created and matches specification examples
- [ ] Unit tests created and passing for summary generation
- [ ] All existing tests continue passing (`dotnet test`)
- [ ] Markdown lint passes (`npm run lint:md`)
- [ ] Code follows project conventions (XML comments, access modifiers, etc.)
- [ ] No new dependencies introduced
- [ ] Website documentation claim is now accurate

## Future Enhancements

These features are explicitly out of scope for the initial implementation but documented for future consideration:

1. **Web Categories Support**
   - Add `web_categories` property to view model
   - Add conditional column to template
   - Update factory to extract and format categories
   - Requires Premium tier Azure Firewall test data

2. **Protocol Semantic Icons**
   - Add 🔒 icon for HTTPS protocols
   - Add specific icons for MSSQL, HTTP, etc.
   - Enhancement to visual clarity without breaking existing output

3. **Conditional Column Removal**
   - Hide source_ip_groups column when no rules use it
   - Hide fqdn_tags column when no rules use it
   - Requires Scriban template logic enhancement

4. **NAT Rule Collections**
   - Separate feature: `azurerm_firewall_nat_rule_collection`
   - Would follow same pattern as network/application rules
   - Different properties (source/destination NAT)

5. **Azure Firewall Policy Support**
   - Modern alternative to classic rule collections
   - Resource: `azurerm_firewall_policy_rule_collection_group`
   - More complex hierarchical structure
   - Would be separate feature with different template

## References

- **Specification:** docs/features/040-azurerm-firewall-application-rule-template/specification.md
- **Reference Implementation:** src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn
- **Reference View Model:** src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModel.cs
- **Reference Factory:** src/Oocx.TfPlan2Md/Providers/AzureRM/Models/FirewallNetworkRuleCollectionViewModelFactory.cs
- **Pattern Documentation:** docs/features/026-template-rendering-simplification/specification.md
- **Project Architecture:** docs/architecture.md
- **Coding Standards:** docs/spec.md
- **Azure Firewall Terraform Docs:** https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/firewall_application_rule_collection
