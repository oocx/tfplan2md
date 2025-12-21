# Test Plan Gap Analysis: Markdown Rendering (v0.26.0)

## Overview

Following the discovery of markdown rendering issues in release v0.26.0 (documented in `analysis.md`), this document analyzes gaps in our current testing strategy and proposes improvements to prevent recurrence.

## Identified Gaps

### 1. Lenient Structural Validation
**Current State:**
Tests like `Render_BreakingPlan_ParsesTablesWithMarkdig` use Markdig to parse the generated markdown and assert that `tables.Count > 0`.

**The Gap:**
Markdig is a forgiving parser. If a table is broken (e.g., by blank lines between rows), Markdig might:
- Parse it as multiple separate tables.
- Parse it as text paragraphs.
- Parse only the valid parts as tables.

As long as *one* valid table exists in the document (e.g., the Summary table), the assertion `tables.Count > 0` passes, masking issues in other tables (like the broken Role Assignment table).

**Prevention Strategy:**
- **Strict Table Counting:** Assert that the number of parsed tables equals the expected number of resources + summary tables.
- **Content Verification:** Verify that specific content (e.g., a resource name) is contained *within* a parsed `Table` object, not just in the document text.

### 2. Missing Automated Linting in Test Suite
**Current State:**
Linting (MD012, etc.) is intended to run in CI, but there is no unit or integration test that enforces these rules during local development or test execution.

**The Gap:**
Developers can run tests that pass (because they only check for content presence) while generating invalid markdown. The feedback loop is too long (waiting for CI) or non-existent if CI is skipped/misconfigured.

**Prevention Strategy:**
- **Integrated Linting Test:** Add a test case that executes `markdownlint` (or a functional equivalent) against the generated output of the `ComprehensiveDemo`.
- **Regex-based Style Assertions:** For critical rules like "no multiple blank lines" or "no blank lines in tables", add fast, regex-based assertions in the C# test suite.

### 3. Template-Specific Regression Testing
**Current State:**
We test the "Breaking Plan" and "Comprehensive Demo", but we don't isolate each built-in template to verify it produces valid markdown structure in isolation.

**The Gap:**
The Role Assignment template had a specific whitespace bug. Because it was one part of a large report, its failure was drowned out by the success of other parts.

**Prevention Strategy:**
- **Template Isolation Tests:** Create a data-driven test that renders *each* built-in template with mock data and validates:
    1. It produces exactly one table (if applicable).
    2. It contains no blank lines between table rows.
    3. It respects surrounding spacing.

## Action Plan

### 1. Update Test Plan
Update `docs/features/markdown-quality-validation/test-plan.md` to include:
- **TC-08:** Validate exact table count matches expected resource count.
- **TC-09:** Validate no multiple consecutive blank lines (Regex check).
- **TC-10:** Validate no blank lines within table blocks (Regex check).

### 2. Enhance `MarkdownValidationTests.cs`
- Implement `Render_ComprehensiveDemo_PassesMarkdownLint` (using CLI wrapper or strict Regex).
- Implement `Render_AllTemplates_ProduceValidTables` (iterating over all resource types).

### 3. Add Regression Tests
- Add specific test case for the Role Assignment table bug (blank lines between rows).
- Add specific test case for the MD012 bug (multiple blank lines after `</details>`).
