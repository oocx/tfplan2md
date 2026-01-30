# Test Plan: Static Code Analysis Integration

## Overview

This test plan covers the integration of static code analysis results (SARIF 2.1.0) into the Terraform plan markdown report. It verifies SARIF parsing, severity mapping, resource/attribute matching, CLI integration, and markdown rendering across multiple presentation variants.

Reference: [Specification](specification.md), [Architecture](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| AC-1: Accept SARIF 2.1.0 format files as input | TC-01: SarifParser_ValidSarif210_ReturnsModel | Unit |
| AC-2: Support multiple SARIF files via CLI flag with wildcard patterns | TC-02: WildcardExpander_ValidPattern_ReturnsMatchingFiles, TC-03: WildcardExpander_RecursivePattern_ReturnsNestedFiles | Unit |
| AC-3: Parse findings from standard SARIF structure (`runs[].results[]`) | TC-01: SarifParser_ValidSarif210_ReturnsModel, TC-21-29: Tool Integration Tests | Unit, Integration |
| AC-4: Extract resource addresses from `logicalLocations[].fullyQualifiedName` | TC-04: ResourceMapper_MapLogicalLocation_ReturnsCorrectAddress, TC-23, TC-24 | Unit, Integration |
| AC-5: New flag: `--code-analysis-results <pattern>` | TC-05: CLI_CodeAnalysisResultsFlag_ParsesCorrectly | Unit |
| AC-6: New flag: `--code-analysis-minimum-level <level>` | TC-06: CLI_MinimumLevelFlag_FiltersFindings | Unit |
| AC-7: New flag: `--fail-on-static-code-analysis-errors <level>` | TC-19: CLI_FailOnErrors_ExitsWithErrorCode | Integration |
| AC-8: Display findings at the resource level | TC-08: MarkdownRenderer_ResourceFindings_RendersCorrectly, TC-21, TC-24, TC-27 | Unit, Integration |
| AC-9: Show severity level (Critical, High, Medium, Low, Informational) | TC-09: SeverityMapper_DeriveSeverity_ReturnsCorrectLevel, TC-26 | Unit, Integration |
| AC-10: Include specific attribute causing the issue when available | TC-10: ResourceMapper_ExtractAttribute_ReturnsAttributePath, TC-20, TC-23 | Unit, Integration |
| AC-11: Provide clickable remediation links | TC-11: MarkdownRenderer_RemediationLinks_AreClickable | Unit |
| AC-12: Critical findings must be visually prominent | TC-12: MarkdownRenderer_CriticalSeverity_HasVisualProminence | Unit |
| AC-13: Summary section with findings count by severity | TC-13: MarkdownRenderer_SummarySection_HasCorrectCounts, TC-30 | Unit, Integration |
| AC-14: List of analysis tools used | TC-14: MarkdownRenderer_SummarySection_ListsTools, TC-21, TC-24, TC-27, TC-30 | Unit, Integration |
| AC-15: Include resources with findings even if they wouldn't normally appear | TC-15: ReportModelBuilder_IncludeResourcesWithFindings_AddsResources | Unit |
| AC-16: Unmatched findings section at report end | TC-16: MarkdownRenderer_UnmatchedFindings_RendersAtEnd | Unit |
| AC-17: Group module-level findings per module | TC-17: MarkdownRenderer_ModuleFindings_GroupsCorrectly, TC-22 | Unit, Integration |
| AC-18: Skip malformed SARIF files with warning | TC-18: SarifParser_MalformedFile_ReturnsWarning | Unit |
| AC-19: Exit with error code on specified level | TC-19: CLI_FailOnErrors_ExitsWithErrorCode | Integration |

## User Acceptance Scenarios

> **Purpose**: Verify markdown rendering in real PR environments (GitHub and Azure DevOps) to ensure visual prominence, correct layout, and clickable links.

### Scenario 1: Comprehensive Security & Quality Report

**User Goal**: Review a single report containing changes from Terraform and findings from multiple security tools (Checkov, Trivy).

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comment.
- **Azure DevOps**: Verify rendering in Azure DevOps PR comment.

**Expected Output**:
- A summary section at the top showing tool names (e.g., Checkov, Trivy, TFLint) and findings counts.
- Status indicator showing high-priority findings.
- Resources like `azurerm_key_vault` showing inline or per-resource findings tables.
- High/Critical findings clearly marked with 🚨/⚠️ icons.
- Remediation links are clickable and lead to external documentation.

**Success Criteria**:
- [ ] Summary counts match input SARIF files.
- [ ] Findings are correctly mapped to their resources.
- [ ] Rendering is consistent across both platforms.
- [ ] Visual hierarchy makes critical issues stand out.

---

### Scenario 2: Edge Cases (Unmatched Findings & Modules)

**User Goal**: See findings that don't map to a specific resource in the plan (e.g., broad security policies or module-level issues).

**Test PR Context**:
- **GitHub/Azure DevOps**: Verify rendering of "Other Findings" section at the bottom.

**Expected Output**:
- A section at the end of the report named "Other Findings".
- Module-level findings grouped under their module name (extracted from resource address).
- Non-resource findings (orphaned or global) listed under "Unmatched Findings".

**Success Criteria**:
- [ ] Module findings are grouped correctly.
- [ ] Unmatched findings don't break the report layout.
- [ ] Empty "Other Findings" sections are suppressed if no such findings exist.

---

### Scenario 3: Error Handling and Metadata

**User Goal**: Understand why some code analysis results are missing if a file was corrupted, and see tool version information.

**Test PR Context**:
- **GitHub/Azure DevOps**: Verify rendering of warning messages and tool metadata.

**Expected Output**:
- A warning section at the top indicating which files failed to parse and a concise error message.
- Tool list includes version numbers (e.g., `Checkov 3.2.10`, `TFLint 0.50.0`) when available in SARIF.
- The rest of the report (Terraform plan + successful SARIF files) is rendered correctly.

**Success Criteria**:
- [ ] Error messages are helpful but not overwhelming.
- [ ] Tool versioning is accurately reflected.

## Test Cases

### TC-01: SarifParser_ValidSarif210_ReturnsModel

**Type:** Unit

**Description:**
Verifies that the `SarifParser` can correctly parse a standard SARIF 2.1.0 file subset.

**Preconditions:**
- A valid SARIF 2.1.0 file (e.g., from Checkov).

**Test Steps:**
1. Call `SarifParser.Parse(filePath)`.
2. Verify the returned model contains expected results, levels, and tools.

**Expected Result:**
Model populated with correct finding details and tool metadata.

---

### TC-02: WildcardExpander_ValidPattern_ReturnsMatchingFiles

**Type:** Unit

**Description:**
Verifies that wildcard patterns in CLI flags are expanded to the correct list of files, handled by the application (not just shell).

**Test Steps:**
1. Call expander with `*.sarif` in a directory with 3 matching and 2 non-matching files.
2. Verify exactly 3 files are returned.

---

### TC-03: WildcardExpander_RecursivePattern_ReturnsNestedFiles

**Type:** Unit

**Description:**
Verifies that recursive glob patterns (`**/*.sarif`) expand to files in nested directories.

**Test Steps:**
1. Create directory structure: `root/`, `root/subdir/`, `root/subdir/nested/`.
2. Place `.sarif` files at each level.
3. Call expander with `**/*.sarif`.
4. Verify all files from all levels are returned in deterministic order.

---

### TC-04: ResourceMapper_MapLogicalLocation_ReturnsCorrectAddress

**Type:** Unit

**Description:**
Verifies that Terraform resource addresses are correctly extracted from SARIF `logicalLocations`.

**Test Data:**
- `logicalLocations[0].fullyQualifiedName = "aws_s3_bucket.example"` -> `aws_s3_bucket.example`
- `logicalLocations[0].fullyQualifiedName = "module.vpc.aws_vpc.main"` -> `module.vpc.aws_vpc.main`

---

### TC-09: SeverityMapper_DeriveSeverity_ReturnsCorrectLevel

**Type:** Unit

**Description:**
Verifies the hybrid severity mapping logic (Primary: `security-severity`, Secondary: `rank`, Fallback: `level`).

**Test Data:**
- `security-severity = 9.5` -> Critical
- `security-severity = 7.2` -> High
- `level = "error"` (no severity prop) -> High
- `level = "note"` -> Low

---

### TC-15: ReportModelBuilder_IncludeResourcesWithFindings_AddsResources

**Type:** Unit

**Description:**
Verifies that resources NOT in the Terraform plan but having security findings are added to the report model so they are visible.

---

### TC-20: ResourceMapper_MultipleLocations_CreatesMultipleFindings

**Type:** Unit

**Description:**
Verifies that findings with multiple `locations[]` entries are mapped to each resource/attribute separately, creating distinct findings for each location.

**Test Steps:**
1. Parse SARIF result with 3 `locations[].logicalLocations[]` entries pointing to different resources.
2. Verify 3 separate findings are created in the model.
3. Verify each finding references the correct resource and attribute.

---

### TC-19: CLI_FailOnErrors_ExitsWithErrorCode

**Type:** Integration

**Description:**
Verifies that `tfplan2md` exits with a non-zero exit code when findings at or above the specified level are found.

**Test Steps:**
1. Run `tfplan2md` with `--fail-on-static-code-analysis-errors high`.
2. Provide SARIF with a Critical finding.
3. Verify exit code is non-zero.

---

## Tool-Specific Integration Tests

These tests validate end-to-end integration with real static analysis tools using their current versions. Each test generates SARIF output from the tool, passes it to tfplan2md, and validates the rendered report.

### Checkov Integration Tests

#### TC-21: Checkov_BasicSecurityFindings_RendersCorrectly

**Type:** Integration

**Description:**
Verifies that Checkov findings are correctly parsed and rendered in the report.

**Test Steps:**
1. Create a Terraform configuration with known security issues (e.g., S3 bucket without encryption, security group with 0.0.0.0/0).
2. Run `terraform plan -out=tfplan.binary && terraform show -json tfplan.binary > tfplan.json`.
3. Run `checkov -f tfplan.json --output sarif --output-file-path checkov.sarif`.
4. Run `tfplan2md tfplan.json --code-analysis-results checkov.sarif --out report.md`.
5. Verify report contains:
   - Checkov tool name and version in summary
   - Findings mapped to correct resources (e.g., `aws_s3_bucket.example`)
   - Severity levels derived from Checkov's `security-severity` property
   - Remediation links to Checkov documentation

**Expected Result:**
Report accurately reflects Checkov findings with correct severity, resource mapping, and remediation links.

---

#### TC-22: Checkov_ModuleLevelFindings_GroupsCorrectly

**Type:** Integration

**Description:**
Verifies that Checkov findings for resources within modules are grouped correctly.

**Test Data:**
- Terraform config with nested modules containing security issues.

**Test Steps:**
1. Create config: `module.network.aws_vpc.main` with missing flow logs.
2. Generate tfplan.json and run Checkov to produce SARIF.
3. Run tfplan2md with the SARIF file.
4. Verify findings are grouped under "Module: network" in the "Other Findings" section.

---

#### TC-23: Checkov_AttributeLevelFindings_MapsToAttribute

**Type:** Integration

**Description:**
Verifies that Checkov findings targeting specific attributes are mapped correctly.

**Test Data:**
- S3 bucket with `versioning` disabled.

**Test Steps:**
1. Generate SARIF from Checkov.
2. Verify finding logical location includes attribute path (e.g., `aws_s3_bucket.example.versioning.enabled`).
3. Verify report shows finding at attribute level, not just resource level.

---

### Trivy Integration Tests

#### TC-24: Trivy_MisconfigurationFindings_RendersCorrectly

**Type:** Integration

**Description:**
Verifies that Trivy misconfiguration findings are correctly parsed and rendered.

**Test Steps:**
1. Create Terraform config with misconfigurations (e.g., Azure storage account with `min_tls_version = "TLS1_0"`).
2. Run `terraform plan -out=tfplan.binary && terraform show -json tfplan.binary > tfplan.json`.
3. Run `trivy config tfplan.json --format sarif --output trivy.sarif`.
4. Run `tfplan2md tfplan.json --code-analysis-results trivy.sarif --out report.md`.
5. Verify:
   - Trivy tool name/version in summary
   - Findings mapped to `azurerm_storage_account.example`
   - Severity correctly derived from Trivy's SARIF output
   - Remediation links point to Trivy documentation

---

#### TC-25: Trivy_MultipleResultsPerResource_AllDisplayed

**Type:** Integration

**Description:**
Verifies that when Trivy reports multiple findings for the same resource, all are displayed.

**Test Data:**
- Azure Key Vault with multiple security issues (public network access, no purge protection, no RBAC).

**Test Steps:**
1. Generate SARIF with multiple findings for `azurerm_key_vault.vault`.
2. Run tfplan2md.
3. Verify all findings appear in the resource's "Security & Quality Findings" table.
4. Verify findings are sorted by severity (Critical → Informational).

---

#### TC-26: Trivy_SeverityMapping_HandlesNumericRanges

**Type:** Integration

**Description:**
Verifies severity mapping from Trivy's numeric `security-severity` field.

**Test Data:**
- Generate SARIF with findings at various severity levels.

**Test Steps:**
1. Verify findings with `security-severity >= 9.0` map to Critical.
2. Verify findings with `security-severity >= 7.0` map to High.
3. Verify findings with `security-severity >= 4.0` map to Medium.
4. Verify findings with `security-severity < 4.0` map to Low or Informational.

---

### TFLint Integration Tests

#### TC-27: TFLint_DeprecatedResourceWarnings_RendersCorrectly

**Type:** Integration

**Description:**
Verifies that TFLint warnings about deprecated resources/attributes are rendered.

**Test Steps:**
1. Create Terraform config using deprecated AWS resources or attributes.
2. Run `tflint --format sarif > tflint.sarif`.
3. Run `tfplan2md tfplan.json --code-analysis-results tflint.sarif`.
4. Verify:
   - TFLint tool name/version in summary
   - Warnings mapped to correct resources
   - Severity derived from TFLint's `level` field (typically "warning" → Medium)
   - Links to TFLint rule documentation

---

#### TC-28: TFLint_BestPracticeViolations_DisplaysWithContext

**Type:** Integration

**Description:**
Verifies that TFLint best practice violations include helpful context.

**Test Data:**
- Config violating AWS naming conventions or missing required tags.

**Test Steps:**
1. Generate SARIF from TFLint with best practice findings.
2. Verify report includes the rule ID (e.g., `aws_instance_invalid_type`).
3. Verify remediation link points to TFLint's rule documentation.

---

#### TC-29: TFLint_NoFindings_EmptyReport

**Type:** Integration

**Description:**
Verifies that when TFLint produces SARIF with no findings, the report handles it gracefully.

**Test Steps:**
1. Create valid Terraform config with no linting issues.
2. Run TFLint to generate SARIF with empty `results[]`.
3. Run tfplan2md.
4. Verify:
   - No "Code Analysis Summary" section appears (or shows 0 findings).
   - Rest of report (Terraform plan) renders normally.
   - No warnings or errors in output.

---

### Multi-Tool Integration Tests

#### TC-30: MultiTool_CombinedFindings_AggregatesCorrectly

**Type:** Integration

**Description:**
Verifies that findings from multiple tools (Checkov + Trivy + TFLint) are correctly aggregated.

**Test Steps:**
1. Create Terraform config with issues detectable by all three tools.
2. Generate SARIF from each tool.
3. Run `tfplan2md tfplan.json --code-analysis-results "*.sarif"`.
4. Verify:
   - Summary lists all three tools
   - Total counts aggregate findings from all tools
   - No duplicate findings (same resource/attribute/rule)
   - Findings are sorted by severity within each resource

---

#### TC-31: MultiTool_ConflictingSeverities_UsesMostSevere

**Type:** Integration

**Description:**
Verifies behavior when multiple tools report the same issue with different severities.

**Test Data:**
- Config with an issue detected by both Checkov (Critical) and Trivy (High).

**Test Steps:**
1. Generate SARIF from both tools for the same resource/attribute issue.
2. Run tfplan2md with both SARIF files.
3. Verify:
   - Both findings appear (they may have different rule IDs).
   - If they are truly duplicates (same rule/message), most severe is displayed OR both are shown with distinct descriptions.

---

#### TC-32: MultiTool_WildcardExpansion_FindsAllFiles

**Type:** Integration

**Description:**
Verifies that wildcard patterns correctly find SARIF files from multiple tools.

**Test Steps:**
1. Place SARIF files in directory: `checkov.sarif`, `trivy.sarif`, `tflint.sarif`, `other.txt`.
2. Run `tfplan2md tfplan.json --code-analysis-results "*.sarif"`.
3. Verify all 3 SARIF files are processed and `other.txt` is ignored.
4. Verify summary shows all three tools.

---

## Test Data Requirements

### Generated Test Data (via Real Tools)

The following test data will be generated during test execution using the actual tools:

**Terraform Configurations:**
- `security-issues.tf`: S3 buckets without encryption, security groups with 0.0.0.0/0, Key Vaults with public access
- `module-example/`: Nested module structure with VPC, subnets, and security configurations
- `deprecated-resources.tf`: AWS resources using deprecated attributes or resource types
- `best-practices.tf`: Resources missing tags, invalid naming conventions
- `azure-misconfig.tf`: Azure resources with TLS 1.0, no RBAC, missing diagnostics
- `clean-config.tf`: Valid configuration with no security/quality issues

**SARIF Files (generated during test setup):**
- Run Checkov, Trivy, and TFLint against each Terraform configuration
- Capture SARIF output for use in tests
- Store in `TestData/StaticAnalysis/Generated/`

### Static Test Data (pre-created)

- `malformed-json.sarif`: JSON with syntax errors (for error handling tests)
- `invalid-sarif-schema.sarif`: Valid JSON but missing required SARIF 2.1.0 fields
- `empty-results.sarif`: Valid SARIF with empty `runs[].results[]`
- `unmatched-findings.sarif`: Findings with logical locations not matching any resource pattern
- `multiple-locations.sarif`: Single finding with 3+ location entries

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| SARIF file with no results | Parse successfully, 0 findings added to model | TC-01_Empty, TC-29 |
| Findings for non-existent resources | Show in "Unmatched Findings" section | TC-16 |
| Attribute mapping failure | Show as resource-level finding (Attribute=null) | TC-10_Fallback |
| Conflicting severity signals | Follow priority: security-severity > level | TC-09_Priority, TC-26 |
| Multiple SARIF files for same resource | Merge findings in the display | TC-08_Merged, TC-25, TC-30 |
| Finding with multiple locations | Create separate finding entry for each location | TC-20 |
| Recursive glob pattern with no matches | Return empty list, no error | TC-03_Empty |
| Multiple tools detecting same issue | Both findings displayed (or deduplicated if identical) | TC-31 |
| Tool version information missing | Display tool name only | TC-21/24/27 variants |

## Non-Functional Tests

- **Performance**: Ensure parsing 10+ large SARIF files doesn't significantly slow down report generation (< 2s overhead). Test with TC-30 using multiple large SARIF files.
- **Graceful Degradation**: If SARIF parsing fails, the main Terraform report must still be generated correctly. Verified by TC-18 and TC-29.
- **Tool Version Compatibility**: Ensure tests run with current stable versions of Checkov, Trivy, and TFLint at test execution time (not pinned to specific versions).

## Test Infrastructure Requirements

### Tool Installation (Test Setup)

Integration tests require the following tools to be available in the test environment:

```bash
# Checkov (Python)
pip install checkov

# Trivy (Binary)
# Install from https://github.com/aquasecurity/trivy/releases

# TFLint (Binary)
# Install from https://github.com/terraform-linters/tflint/releases

# Terraform (for generating plans)
# Install from https://www.terraform.io/downloads
```

### Docker Test Image

Update the Docker integration test image to include:
- Terraform CLI
- Checkov
- Trivy
- TFLint

This ensures integration tests can run in CI/CD without requiring pre-installed tools on the host.

### Test Execution Strategy

1. **Unit tests** run first (fast, no external dependencies).
2. **Integration tests with real tools** run if tools are available (skip if not installed with warning).
3. **CI/CD environment** ensures tools are pre-installed in Docker image.

**Skip Logic:**
```csharp
[Test]
public async Task Checkov_Integration_Test()
{
    if (!IsCheckovAvailable())
    {
        Assert.Inconclusive("Checkov not installed, skipping integration test");
    }
    // ... test logic
}
```
