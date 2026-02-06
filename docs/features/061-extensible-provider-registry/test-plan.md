# Test Plan: Extensible Provider Registry System

## Overview

This test plan covers the extensible provider registry system, which allows for pattern-based matching of resource view model factories, value formatters, and icon providers. It also covers the additional requirement of ensuring full snapshot test coverage for icons and all currently used resource providers.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Service registry supports registration of factories, formatters, and icons | TC-01, TC-02 | Unit |
| Pattern matching correctly evaluates regex for provider/resource/attribute/value | TC-03 | Unit |
| Null patterns match all values for that dimension | TC-04 | Unit |
| Specificity resolution selects most specific service | TC-05 | Unit |
| Services can decline and trigger fallback | TC-06 | Unit |
| Default behavior used when no match or all decline | TC-07 | Unit |
| File-based icon provider loads rules from JSON | TC-08 | Unit |
| File parsing handles errors gracefully | TC-09 | Unit |
| Services are registered during application startup | TC-10 | Integration |
| Existing functionality continues to work correctly | TC-11, TC-12 | Snapshot |
| **Additional: Full icon snapshot coverage** | TC-13, TC-14 | Snapshot |
| **Additional: AzureAD provider snapshot coverage** | TC-15 | Snapshot |
| **Additional: AzureDevOps provider snapshot coverage** | TC-16 | Snapshot |

## User Acceptance Scenarios

### Scenario 1: Built-in Icons and Formatting

**User Goal**: View a Terraform plan for Azure resources and see consistent icons and human-readable values.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR descriptions.

**Expected Output**:
- Boolean values show ✅/❌ icons.
- Azure locations show 🌍 icon.
- Network rules show icons for protocols (🔗, 📨, 📡) and actions (✅, ⛔).
- Subscriptions show 🔑 icon.

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown.
- [ ] Output renders correctly in Azure DevOps Markdown.
- [ ] Icons are correctly placed and do not cause line breaks.

---

### Scenario 2: Azure AD and Azure DevOps Support

**User Goal**: View plans containing Azure AD users/groups and Azure DevOps variable groups.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR descriptions.

**Expected Output**:
- Azure AD Users show 🆔 icon for UPN and 📧 for mail.
- Azure AD Groups show 👥 icon.
- Azure AD Service Principals show 💻 icon.
- Azure DevOps Variable Groups show formatted lists of variables.

**Success Criteria**:
- [ ] Correct icons displayed for AD identities.
- [ ] Variable groups show correctly formatted key-value pairs.

## Test Cases

### TC-01: ResourceViewModelFactoryRegistry_Registration_Success

**Type:** Unit

**Description:** Verifies that resource view model factories can be registered and resolved using the new registry system (optional stretch goal).

**Expected Result:** Factory is successfully registered and returned when matching.

---

### TC-02: ValueFormatterRegistry_Registration_Success

**Type:** Unit

**Description:** Verifies that value formatters can be registered and resolved.

---

### TC-03: PatternMatchingRegistry_RegexMatching_CorrectResolution

**Type:** Unit

**Description:** Verifies that the registry correctly matches strings against regex patterns for all four dimensions (provider, resource type, attribute, value).

**Test Data:**
- Provider: `^azurerm$`
- Resource: `^azurerm_.*$`
- Attribute: `^name$`
- Value: `^prod-.*$`

---

### TC-04: PatternMatchingRegistry_WildcardMatching_CorrectResolution

**Type:** Unit

**Description:** Verifies that `null` patterns act as wildcards.

---

### TC-05: PatternMatchingRegistry_SpecificityResolution_FollowsRules

**Type:** Unit

**Description:** Verifies that more specific rules (more non-null matchers) win over less specific rules.
If counts are equal, priority is: Value > Attribute > Resource Type > Provider.

---

### TC-06: PatternMatchingRegistry_Fallback_IteratesNextMatch

**Type:** Unit

**Description:** Verifies that if the first resolved service returns `null` (declines), the next match in specificity order is tried.

---

### TC-07: PatternMatchingRegistry_NoMatch_ReturnsNull

**Type:** Unit

**Description:** Verifies that if no patterns match, the registry returns `null`.

---

### TC-08: FileBasedIconProvider_LoadFromJson_CorrectRules

**Type:** Unit

**Description:** Verifies that `FileBasedIconProvider` correctly parses the JSON format and populates its internal registry.

---

### TC-09: FileBasedIconProvider_InvalidJson_ThrowsException

**Type:** Unit

**Description:** Verifies that malformed JSON or invalid regex patterns in the JSON file throw a `ServiceRegistrationException`.

---

### TC-10: ProviderModule_Registration_Integration

**Type:** Integration

**Description:** Verifies that `AzureRMModule` and other modules use the new registration methods to register their icons and formatters.

---

### TC-11: Snapshot_ComprehensiveDemo_RegressionCheck

**Type:** Snapshot

**Description:** Ensures the comprehensive demo snapshot still matches exactly.

---

### TC-12: Snapshot_BreakingPlan_RegressionCheck

**Type:** Snapshot

**Description:** Ensures escaping and special characters handle the new registry logic correctly.

---

### TC-13: Snapshot_AzureAD_CoversMissingIcons

**Type:** Snapshot

**Description:** New snapshot test in `AzureAdSnapshotTests.cs` covering Azure AD resources (users, groups, service principals) to ensure 👥, 💻, and 📧 icons are correctly rendered and captured.

---

### TC-14: Snapshot_AzureDevOps_CoversProvider

**Type:** Snapshot

**Description:** New snapshot test in `AzureDevOpsSnapshotTests.cs` covering Azure DevOps resources (projects, variable groups) to ensure the provider and icons are covered by snapshots.

## Test Data Requirements

- `azuread-snapshot-plan.json` - Combined plan with user, group, and service principal.
- `azuredevops-snapshot-plan.json` - Plan with variable groups and other AzDO resources.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Overlapping regex patterns | Specificity algorithm determines winner | TC-05 |
| Case sensitivity in patterns | Should follow RegexOptions (ignored by default) | TC-03 |
| Nested attributes in AzApi | Value formatters should see the full path | TC-10 |

## Open Questions

- Should we migrate ALL existing icons immediately? (Recommended: Yes, to verify the system works fully).
