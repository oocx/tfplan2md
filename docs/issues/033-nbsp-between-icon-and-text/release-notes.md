# Emoji Spacing Consistency Fixes

This patch release fixes emoji spacing issues in Scriban templates to ensure consistent markdown rendering across platforms (GitHub, Azure DevOps).

## 🐛 Bug Fixes

### Fixed emoji spacing in code analysis templates

Fixed inconsistent emoji spacing where regular spaces (U+0020) were used instead of non-breaking spaces (U+00A0) after emoji characters. This caused unpredictable line wrapping behavior in different markdown renderers.

**Templates fixed:**

1. **`_code_analysis_summary.sbn`**: Fixed spacing for status emojis (✅ ✓, 🚨, ⚠️) and severity indicators in the summary table (🚨 Critical, ⚠️ High/Medium, ℹ️ Low/Informational)

2. **`_code_analysis_metadata.sbn`**: Fixed spacing in per-resource security metadata line and severity breakdown (🔒 Security & Quality heading, 🚨 Critical, ⚠️ High/Medium, ℹ️ Low/Informational indicators)

3. **`_code_analysis_findings.sbn`**: Fixed spacing in severity icons within the findings table (🔒 Security & Quality Findings heading, severity column icons)

4. **`_code_analysis_other_findings.sbn`**: Fixed spacing in module and unmatched findings tables (severity column icons)

5. **`azapi/resource.sbn`**: Fixed spacing for action symbol in resource headings and documentation link icon (📚 View API Documentation)

**Impact:** These fixes ensure that emoji + text tokens (like `🚨 Critical` or `📚 Documentation`) stay together when markdown is rendered, preventing emojis from wrapping to separate lines in narrow layouts or certain rendering contexts.

## 🔗 Commits

- [`d1d181f`](https://github.com/oocx/tfplan2md/commit/d1d181f4711d3c8ed3a3cbefd79df76db2b88792) SNAPSHOT_UPDATE_OK: Add template emoji spacing test and increase Docker timeout

## 🧪 Testing Improvements (internal)

While not user-facing, this release includes significant test coverage improvements:

- Added `Templates_ShouldNotContainEmojiFollowedByRegularSpace()` architecture test that scans all 21 Scriban templates across all providers (core, AzureRM, AzApi, AzureAD, AzureDevOps) to prevent regression
- Extended template architecture tests to cover AzureAD and AzureDevOps providers
- Added comprehensive demo snapshot test with full provider coverage, code analysis, and principal mappings
- Increased Docker test timeout from 60s to 90s for reliability in CI environments

## ▶️ Getting Started

No usage changes — just pull the latest image:

```bash
docker pull oocx/tfplan2md:1.13.1
# or
docker pull oocx/tfplan2md:latest

# Usage remains the same
tfplan2md plan.json > plan.md
```

If you're generating reports with code analysis findings (using `--code-analysis-files`), you'll now see consistent emoji spacing in severity indicators and status messages across all markdown platforms.
