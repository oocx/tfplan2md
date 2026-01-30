# Feature: Static Code Analysis Integration

## Overview

Integrate static code analysis findings from security and quality tools (Checkov, Trivy, TFLint, Semgrep) directly into the Terraform plan markdown report. This creates a unified, single-source-of-truth report for pull request reviews, eliminating the need to correlate information across multiple tool outputs, PR comments, and test result views.

## User Goals

### Primary Goal
Users need a **complete, unified view** of both infrastructure changes and security/quality findings in one report to make informed PR approval decisions efficiently.

### Current Pain Point
Users currently review Terraform PRs with fragmented information:
- Security findings appear in PR comments
- Quality checks show up in test result views
- Different tools produce separate outputs in various formats
- Manual correlation between findings and specific resources is time-consuming and error-prone

### Desired Outcome
A single markdown report that integrates:
- All Terraform plan changes (existing functionality)
- Security and quality findings mapped to specific resources and attributes
- Clear severity indicators with prominent display of critical issues
- Actionable remediation links
- Summary metrics for quick assessment

## Scope

### In Scope

#### Input Format
- Accept SARIF 2.1.0 format files as input
- Support multiple SARIF files via CLI flag with wildcard patterns
- Parse findings from standard SARIF structure (`runs[].results[]`)
- Extract resource addresses from `logicalLocations[].fullyQualifiedName`
- Extract remediation links from `rules[].helpUri`

#### CLI Interface
- New flag: `--code-analysis-results <pattern>` (can be specified multiple times)
- New flag: `--code-analysis-minimum-level <level>` for severity filtering
- New flag: `--fail-on-static-code-analysis-errors <level>` to exit with error code when findings at or above specified level are found

#### Finding Display
- Map findings to resources using Terraform resource addresses
- Display findings at the resource level (and attribute level when possible)
- Show severity level for each finding (Critical, High, Medium, Low, Informational)
- Include specific attribute causing the issue when available
- Provide clickable remediation links (not inline documentation)
- Use visual indicators (emojis/icons) with severity levels
- Critical findings must be visually prominent compared to low-severity findings
- Multiple presentation variants to evaluate:
  - Inline with resource attributes
  - Separate section per resource

#### Summary Section
- Total findings count by severity level
- List of analysis tools used (extracted from SARIF metadata)
- Overall status indicator for quick assessment
- Integration with existing summary template

#### Special Cases
- Include resources with findings even if they wouldn't normally appear in the report
- Display unmatched findings (can't be mapped to a plan resource) in a separate section at report end
- Group module-level findings per module
- Suppress empty finding tables/sections

#### Error Handling
- Skip malformed/invalid SARIF files with warning
- Include detailed error information in report for troubleshooting
- Complete report generation before exiting (even with `--fail-on-static-code-analysis-errors`)
- Report remains functional if all SARIF files are invalid (shows warnings only)

#### Testing
- Integration tests with current versions of Checkov, Trivy, and TFLint
- Test data using real SARIF outputs from these tools
- Coverage for mapping accuracy, severity handling, and edge cases

### Out of Scope

#### Not Included in This Feature
- Running static analysis tools (users must run tools separately and provide SARIF files)
- Converting non-SARIF formats to SARIF (users responsible for tool output format)
- Custom rule definitions or analysis logic
- Filtering by rule ID or tool name (only severity-based filtering)
- Inline remediation documentation (links only)
- Historical trending or comparison across multiple reports
- Integration with specific CI/CD platforms (generic CLI interface only)

#### Explicitly Deferred
- Support for SARIF versions other than 2.1.0
- Support for native tool formats (Checkov JSON, Trivy JSON, etc.)
- Custom presentation templates for findings
- Interactive filtering or collapsible sections (static markdown output only)

## User Experience

### Command Line Usage

#### Basic usage with single SARIF file:
```bash
tfplan2md tfplan.json --code-analysis-results checkov-results.sarif
```

#### Multiple files with wildcards:
```bash
tfplan2md tfplan.json \
  --code-analysis-results "*.sarif" \
  --code-analysis-results "security/*.sarif"
```

#### With severity filtering:
```bash
tfplan2md tfplan.json \
  --code-analysis-results "*.sarif" \
  --code-analysis-minimum-level high
```

#### Fail on critical/high findings (for CI/CD):
```bash
tfplan2md tfplan.json \
  --code-analysis-results "*.sarif" \
  --fail-on-static-code-analysis-errors high
```

### Report Output Structure

#### Summary Section (at top of report)
```markdown
## Code Analysis Summary

**Status:** ⚠️ 5 critical findings require attention

| Severity | Count |
|----------|-------|
| 🚨 Critical | 5 |
| ⚠️ High | 12 |
| ⚠️ Medium | 23 |
| ℹ️ Low | 8 |
| ℹ️ Informational | 15 |

**Tools Used:** Checkov, Trivy, TFLint
```

#### Findings at Resource Level
(Multiple presentation variants to be evaluated by Architect with mockups)

**Variant A: Inline with attributes**
```markdown
### aws_security_group.allow_tls (update)

**Attributes:**
- ingress.0.cidr_blocks: "0.0.0.0/0" 🚨 **Critical:** Public ingress rule detected [Details](link)
- description: "Allow TLS" ℹ️ **Low:** Missing security group description [Details](link)
```

**Variant B: Separate section per resource**
```markdown
### aws_security_group.allow_tls (update)

**Attributes:**
- ingress.0.cidr_blocks: "0.0.0.0/0"
- description: "Allow TLS"

**Security & Quality Findings:**
| Severity | Attribute | Finding | Remediation |
|----------|-----------|---------|-------------|
| 🚨 Critical | ingress.0.cidr_blocks | Public ingress rule detected | [Fix Guide](link) |
| ℹ️ Low | description | Missing detailed security group description | [Best Practices](link) |
```

#### Unmatched Findings Section (at end of report)
```markdown
## Other Findings

### Module: network-module
| Severity | Finding | Remediation |
|----------|---------|-------------|
| ⚠️ High | Module uses deprecated provider version | [Upgrade Guide](link) |

### Unmatched Findings
| Severity | Finding | Remediation |
|----------|---------|-------------|
| ⚠️ Medium | Terraform version constraint too permissive | [Best Practices](link) |
```

### Error Scenarios

#### Invalid SARIF File
Report includes warning section:
```markdown
⚠️ **Warning:** Unable to process code analysis file `invalid.sarif`
- Error: Invalid JSON structure at line 42
- The file may be corrupted or not in SARIF 2.1.0 format
- Verify the tool generated valid SARIF output
```

#### No SARIF Files Provided
Report functions exactly as current implementation (no changes, no warnings)

#### Exit Code with `--fail-on-static-code-analysis-errors`
- Report generated completely and written to output
- Tool exits with non-zero exit code if findings at/above specified level exist
- Exit code: 10 (to distinguish from other error types)
- Stderr message: "Static code analysis found N [level] or higher findings"

## Success Criteria

- [ ] Users can provide one or more SARIF 2.1.0 files via CLI flag
- [ ] Wildcards in file patterns are correctly expanded
- [ ] Findings are accurately mapped to resources using Terraform addresses
- [ ] Findings display severity level, affected attribute, and remediation link
- [ ] Critical findings are visually more prominent than low-severity findings
- [ ] Summary section shows total counts by severity and lists tools used
- [ ] Summary template includes code analysis summary when SARIF files provided
- [ ] Resources with findings appear in report even if not normally included
- [ ] Unmatched findings appear in separate section at report end
- [ ] Module-level findings are grouped per module
- [ ] Empty finding sections are suppressed
- [ ] Invalid SARIF files are skipped with detailed warning in report
- [ ] Report generation completes even with invalid SARIF files
- [ ] `--code-analysis-minimum-level` correctly filters findings
- [ ] `--fail-on-static-code-analysis-errors` exits with code 10 after report completion
- [ ] Report output is unchanged when no SARIF files provided (backward compatible)
- [ ] Integration tests pass with Checkov, Trivy, and TFLint current versions
- [ ] Multiple presentation variants evaluated by Architect with mockups
- [ ] Chosen presentation variant maintains readability with many findings
- [ ] Report adheres to existing style guide standards

## Open Questions

### For Architect

**Presentation Variant:** ✅ **DECIDED** - Use Variant C (Hybrid with non-collapsible findings table). See [presentation-drafts.md](presentation-drafts.md) for details.

1. **Severity Mapping:** SARIF defines levels as `error`, `warning`, `note`, `none`. How should these map to user-facing severity labels (Critical/High/Medium/Low/Informational)? Should we use SARIF's `properties.security-severity` or `rank` for finer granularity?

2. **Attribute-Level Mapping:** SARIF `physicalLocation.region` can point to specific JSON paths. Reliability must be validated with integration tests using Checkov, Trivy, and TFLint on real plan.json files that trigger warnings. Implement graceful fallback when attribute-level detail is unavailable.

3. **Visual Prominence:** What specific styling differences should distinguish critical from low-severity findings while maintaining accessibility and rendering consistency across GitHub/Azure DevOps?

4. **Summary Placement:** Should code analysis summary be a separate section (as shown in drafts), or integrated differently with existing plan summary metrics?

5. **Tool Name Extraction:** Where in SARIF structure is the most reliable location for tool names? Must be validated with integration tests. Likely candidate: `runs[].tool.driver.name`.

### For Maintainer
None at this time. All requirements clarified.
