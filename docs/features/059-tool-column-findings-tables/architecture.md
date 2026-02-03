# Architecture: Tool Column in Findings Tables

## Status

Proposed - Awaiting maintainer decision on open questions

## Open Questions for Maintainer

Before proceeding with implementation, the following design decisions require maintainer input:

### 1. Tool Name Format

**Question:** Should the Tool column display tool names with version information or just the tool name?

**Option A: Name Only** (Recommended)
- Display: "Checkov"
- **Pros:**
  - Cleaner, more compact table
  - Tool name is most relevant for identifying which scanner found the issue
  - Version info is available in the Code Analysis Summary section
  - Reduces table width concerns
- **Cons:**
  - Version information not visible per-finding
  - If different versions produce different findings in the same report, can't distinguish

**Option B: Name + Version**
- Display: "Checkov 3.2.10"
- **Pros:**
  - Complete information per finding
  - Useful if different tool versions are used
- **Cons:**
  - Wider column
  - More visual clutter
  - Redundant when all findings from same tool version

**Recommendation:** Option A (Name Only) because:
- Version information is already shown in the Code Analysis Summary
- Multiple tool versions in a single report is a rare edge case
- Table width is a concern given the information density
- Users can cross-reference summary if version matters

### 2. Tool Name Capitalization

**Question:** Should we normalize tool name capitalization (e.g., "Checkov") or use the exact format from SARIF files?

**Option A: Use Exact SARIF Format** (Recommended)
- Display whatever the SARIF file provides (e.g., "checkov", "Checkov", "CHECKOV")
- **Pros:**
  - No logic needed - pass through ToolName as-is
  - Respects the tool's own branding/casing choice
  - Simpler implementation
  - No risk of "correcting" intentional formatting
- **Cons:**
  - Potential inconsistency if tools change their casing
  - Mixed casing if combining multiple SARIF sources

**Option B: Normalize Capitalization**
- Apply consistent casing rules (e.g., title case first letter)
- **Pros:**
  - Visual consistency across all findings
  - Professional appearance
- **Cons:**
  - Requires mapping logic/dictionary
  - May incorrectly change intentional branding (e.g., "tfsec" vs "TFsec")
  - Maintenance burden as new tools are added

**Recommendation:** Option A (Exact SARIF Format) because:
- Simpler and more maintainable
- Respects tool authors' branding decisions
- Most tools already use consistent casing in their SARIF output
- Edge case risk is low (inconsistent casing rarely matters)

### 3. Column Name Optimization

**Question:** Should we rename other columns to optimize table width?

**Context:** Current columns are:
- Security & Quality table: `Severity | Attribute | Finding | Remediation`
- Other Findings table: `Severity | Finding | Remediation`

With Tool added:
- Security & Quality: `Severity | Tool | Attribute | Finding | Remediation`
- Other Findings: `Severity | Tool | Finding | Remediation`

**Option A: Keep Current Names**
- Columns: `Severity | Tool | Attribute | Finding | Remediation`
- **Pros:**
  - Clear, descriptive column names
  - No confusion for existing users
  - Self-documenting
- **Cons:**
  - Wider table (5 columns for Security & Quality)
  - May wrap on narrow displays

**Option B: Shorten Column Names**
- Examples: `Sev | Tool | Attr | Finding | Remediation` or `Severity | Tool | Attribute | Finding | Link`
- **Pros:**
  - More compact
  - May prevent wrapping
- **Cons:**
  - Less clear, especially for new users
  - "Link" vs "Remediation" loses semantic meaning
  - Abbreviations feel less professional

**Recommendation:** Option A (Keep Current Names) because:
- Markdown tables in GitHub/Azure DevOps PRs have reasonable horizontal space
- Clarity is more important than compactness
- Tool names are typically short (5-10 chars)
- If width becomes an actual problem (needs validation), we can address it later with data, not speculation

## Context

Feature 056 (Static Analysis Integration) added support for displaying SARIF-based code analysis findings in the markdown report. Findings are currently shown in two types of tables:

1. **Per-Resource Security & Quality Findings** - Shown inline with resource changes
   - Current columns: `Severity | Attribute | Finding | Remediation`
   - Template: `_code_analysis_findings.sbn`

2. **Other Findings** - Module-level and unmatched findings
   - Current columns: `Severity | Finding | Remediation`
   - Template: `_code_analysis_other_findings.sbn`

The data model (`CodeAnalysisFindingModel`) already includes a `ToolName` property, populated from SARIF files during parsing. However, this information is not currently displayed in the findings tables.

**User Need:** When reviewing findings from multiple security/quality tools (Checkov, tfsec, Trivy, etc.), users need to quickly identify which tool produced each finding to assess its relevance and credibility.

**Constraints:**
- Must maintain compatibility with GitHub and Azure DevOps markdown rendering
- Must not break existing templates or customizations
- Tables are already information-dense; adding a column requires careful consideration

## Analysis

### Current Architecture

The code analysis pipeline consists of:

1. **SARIF Parsing** (`SarifParser`, `SarifRunReader`, `SarifResultReader`)
   - Extracts tool name from `runs[].tool.driver.name` in SARIF
   - Stores in `CodeAnalysisFinding.ToolName` (nullable string)

2. **Report Model Building** (`ReportModelBuilder.CodeAnalysis.cs`)
   - Maps findings to resources
   - Creates `CodeAnalysisFindingModel` instances
   - Already copies `ToolName` from `CodeAnalysisFinding` to `CodeAnalysisFindingModel`

3. **Template Rendering** (Scriban templates)
   - `_code_analysis_findings.sbn` - Per-resource findings
   - `_code_analysis_other_findings.sbn` - Module/unmatched findings

### ToolName Data Flow

The data is already available throughout the pipeline:

```
SARIF File
  └─> CodeAnalysisFinding.ToolName (SarifResultReader)
      └─> CodeAnalysisFindingModel.ToolName (ReportModelBuilder)
          └─> Available in Scriban templates as finding.tool_name
```

**Key Insight:** No changes to C# code are required. The `ToolName` property is already populated and available in templates. This is purely a template modification.

### Null/Empty Tool Name Handling

**Scenarios where ToolName may be null or empty:**
1. SARIF file missing `tool.driver.name` (malformed or minimal SARIF)
2. Very old SARIF format (pre-2.1.0)
3. Hand-crafted SARIF files for testing

**Current behavior:** `ToolName` is optional (nullable string). If not present, it's stored as `null`.

**Template handling:** Scriban templates should render a fallback when `tool_name` is null/empty.

## Design Decision

### Add Tool Column to Both Tables

**Templates to modify:**
1. `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_findings.sbn`
   - Add Tool column after Severity, before Attribute
   - New header: `| Severity | Tool | Attribute | Finding | Remediation |`

2. `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_other_findings.sbn`
   - Add Tool column after Severity, before Finding
   - New header: `| Severity | Tool | Finding | Remediation |`

**Column positioning rationale:**
- **Severity** remains first (most critical information for scanning)
- **Tool** comes second (context for interpreting the finding)
- **Attribute/Finding** comes next (the actual issue)
- **Remediation** remains last (action item)

This creates a natural reading flow: "How severe?" → "Who says?" → "What's wrong?" → "How to fix?"

### Template Implementation Pattern

Use the existing Scriban null-handling pattern seen in the codebase:

```scriban
{{ if finding.tool_name }}{{ finding.tool_name }}{{ else }}-{{ end }}
```

**Rationale:**
- Consistent with how other optional fields are handled in existing templates
- Displays `-` for missing tool names (matches Remediation column pattern)
- Gracefully handles null/empty without breaking table structure

**Version Handling (pending decision):**
- If maintainer chooses name only: `{{ finding.tool_name }}`
- If maintainer chooses name + version: Requires new property or template logic (see Alternatives section)

### Test Impact

**Snapshot Tests:**
All markdown snapshot tests that render code analysis findings will need updates:
- Files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/` containing findings tables
- Any tests asserting on table structure (e.g., `MarkdownRendererCodeAnalysisTests.cs` line 74, 157)

**Test Changes Required:**
1. Update snapshot expectations to include Tool column
2. Update assertions checking table headers to include "Tool"
3. Add new test case for null/empty tool name handling
4. Verify alignment with both GitHub and Azure DevOps markdown renderers

**Test Strategy:**
- Run `dotnet test` to identify failing snapshot tests
- Use `update-test-snapshots` skill to regenerate snapshots after template changes
- Manually verify a sample snapshot to ensure rendering looks correct

### Compatibility Considerations

**Backward Compatibility:**
- ✅ **Data Model:** No changes needed (ToolName already exists)
- ✅ **C# Code:** No code changes required
- ⚠️ **Custom Templates:** Users with custom `_code_analysis_*.sbn` templates will need to update them manually
  - This is expected and acceptable (templates are explicitly customization points)
  - Default templates will include the new column
  - Users can add/skip the column in their custom templates

**Forward Compatibility:**
- ✅ Adding the Tool column is purely additive
- ✅ Future tools will automatically populate the column
- ✅ No breaking changes to the SARIF parsing logic

## Alternatives Considered

### Alternative 1: Make Tool Column Optional (Config Flag)

**Approach:** Add a configuration option to enable/disable the Tool column

**Rejected because:**
- Adds complexity for minimal benefit
- Configuration sprawl (yet another option to document/maintain)
- The tool name is valuable information; no clear use case for hiding it
- Template customization already provides this flexibility

### Alternative 2: Tool Name with Version in Model

**Approach:** Create a new computed property `ToolNameWithVersion` in `CodeAnalysisFindingModel`

**Example:**
```csharp
public string ToolNameWithVersion => 
    !string.IsNullOrEmpty(ToolName) && !string.IsNullOrEmpty(ToolVersion)
        ? $"{ToolName} {ToolVersion}"
        : ToolName ?? string.Empty;
```

**Rejected because:**
- Requires C# changes (against the "no code changes" principle)
- Tool version is not currently available in `CodeAnalysisFindingModel` (would need to be added)
- Increases complexity for a decision that may not be needed
- Can be added later if maintainer chooses name+version format

**Note:** If maintainer chooses "name + version" format, this approach would be preferred over template-based concatenation.

### Alternative 3: Combine Tool and Severity Columns

**Approach:** Show tool name with severity in a single column: "🚨 Critical (Checkov)"

**Rejected because:**
- Reduces scannability (harder to filter/sort by tool mentally)
- Mixes different types of information (severity vs. source)
- Visual clutter in severity column
- Doesn't follow common security report patterns

### Alternative 4: Tool Icons/Logos

**Approach:** Use emoji or text icons to represent tools (e.g., ☁️ for Checkov)

**Rejected because:**
- Requires maintaining an icon mapping
- Not all tools have obvious emoji representations
- Less clear than text names
- Adds cognitive load (users must learn the mappings)

## Implementation Guidance

### For the Developer Agent

1. **Modify Template: _code_analysis_findings.sbn**
   - Locate the table header on line 9
   - Change from: `| Severity | Attribute | Finding | Remediation |`
   - Change to: `| Severity | Tool | Attribute | Finding | Remediation |`
   - Update the separator line to match (add `| -------- ` for Tool column)
   - In the table row (line 16), add Tool column after severity:
     - After `{{ finding.severity_icon }} {{ finding.severity }} |`
     - Add: `{{ if finding.tool_name }}{{ finding.tool_name }}{{ else }}-{{ end }} |`

2. **Modify Template: _code_analysis_other_findings.sbn**
   - Update TWO table headers (module findings line 7, unmatched findings line 21)
   - Change from: `| Severity | Finding | Remediation |`
   - Change to: `| Severity | Tool | Finding | Remediation |`
   - Update separator lines
   - In both table rows (lines 14 and 28), add Tool column after severity

3. **Run Tests and Update Snapshots**
   - Run `dotnet test` - expect snapshot failures
   - Use `update-test-snapshots` skill to regenerate all snapshots
   - Manually inspect 2-3 snapshots to verify Tool column renders correctly
   - Add a new test case in `MarkdownRendererCodeAnalysisTests.cs` for null tool name:
     ```csharp
     [Test]
     public void Render_CodeAnalysisFindingsTable_HandlesNullToolName()
     {
         // Create finding with ToolName = null
         // Assert table renders with "-" in Tool column
     }
     ```

4. **Update Test Assertions**
   - Search for test code asserting on table structure
   - Update line numbers and column counts where needed
   - Update string assertions like `"| Severity | Attribute |"` to include `"| Tool |"`

### Components Affected

**Files to Modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_findings.sbn`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_other_findings.sbn`
- All snapshot files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/` (via regeneration)
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererCodeAnalysisTests.cs` (add test, update assertions)

**Files NOT to Modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/CodeAnalysisFindingModel.cs` (ToolName already exists)
- `src/Oocx.TfPlan2Md/CodeAnalysis/*.cs` (no parsing changes needed)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.CodeAnalysis.cs` (ToolName already mapped)

## Consequences

### Positive

- ✅ **Improved Clarity:** Users can immediately see which tool produced each finding
- ✅ **Multi-Tool Support:** Better experience when running multiple scanners
- ✅ **No Code Changes:** Pure template change keeps the implementation simple
- ✅ **Data Already Available:** Leverages existing infrastructure
- ✅ **Consistent Layout:** Tool column applies uniformly across all findings tables
- ✅ **Professional Appearance:** Follows common security report patterns

### Negative

- ⚠️ **Table Width:** Adds one column, potentially causing wrapping on very narrow displays
  - Mitigation: Testing with GitHub/Azure DevOps will validate rendering
  - Mitigation: Tool names are typically short (5-10 characters)
- ⚠️ **Custom Template Breakage:** Users with custom templates must update them
  - Mitigation: This is expected for template customization
  - Mitigation: Release notes will document the change
- ⚠️ **Snapshot Churn:** All findings-related snapshots must be regenerated
  - Mitigation: Automated with `update-test-snapshots` skill
  - Mitigation: One-time cost during implementation

### Risks to Monitor

1. **Rendering Issues:** If tables wrap poorly in GitHub/Azure DevOps
   - **Monitor:** UAT phase (visual inspection of PR comments)
   - **Fallback:** Consider column name shortening (e.g., "Remediation" → "Link")

2. **Custom Template Users:** May not realize they need to update
   - **Monitor:** Release notes and changelog clearly document breaking change for custom templates
   - **Fallback:** Provide example migration snippet in release notes

3. **Tool Name Variance:** Tools may use inconsistent casing/formatting
   - **Monitor:** Collect real-world SARIF samples during UAT
   - **Fallback:** If problematic, implement normalization in a follow-up (separate decision)

## Questions for Technical Writer

After implementation, the Technical Writer should update:

1. **docs/features.md** - Add entry for feature 059 describing the Tool column
2. **README.md** - Update code analysis examples to show Tool column
3. **Release Notes** - Document the breaking change for custom template users

## Decision Summary

**Pending Maintainer Input:**
- Tool name format: Name only vs. Name + Version (recommend name only)
- Capitalization: Exact SARIF vs. Normalized (recommend exact SARIF)
- Column names: Keep current vs. Shorten (recommend keep current)

**Once maintainer decides:**
- Implement template changes as specified above
- Regenerate all test snapshots
- Add test for null tool name handling
- Proceed to Quality Engineer for test plan review
