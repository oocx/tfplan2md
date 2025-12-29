# Workflow Improvements: 2025-12-29

Based on the retrospective from [Visual Report Enhancements](../../features/024-visual-report-enhancements/retrospective.md), the following workflow improvements have been identified to address friction points and failures.

## Proposed Improvements

| # | Improvement | Description | Value | Effort | Priority | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | **Safe Merge Script** | Create `scripts/safe-merge.sh` to verify file integrity (size/content) post-merge, preventing the documentation loss seen in feature 024. | **High** | Medium | **Critical** | Open |
| 2 | **Markdown Syntax Validator** | Create `scripts/validate-markdown.sh` to detect broken tables/headings before UAT. | **Medium** | Low | Medium | Open |
| 3 | **UAT Signal Detection** | Enhance `scripts/uat-run.sh` or create a skill to explicitly flag "reject/fail" signals in test output, preventing UAT Tester oversight. | **Medium** | Medium | Medium | Open |
| 4 | **Detail Checklist Gate** | Update the **Developer** agent prompt to require a "Detail Checklist" for features with many small UI/UX items to prevent quality slips. | **Medium** | Low | Medium | ✅ Done |
| 5 | **Merge Conflict Guardrail** | Update **Release Manager** instructions to require a manual "Conflict Check" step before finalizing merges, even if the CLI reports success. | **Medium** | Low | High | ✅ Done |
| 6 | **Commit Amend Guideline** | Update **Developer** instructions to amend the previous commit when fixing issues in the commit just created (e.g., based on feedback), ensuring a clean "1 topic per commit" history. | **Medium** | Low | Medium | ✅ Done |
| 7 | **Model Outage Protocol** | Define a fallback protocol for agents when primary models (like `gpt-5.1-codex-max`) are unavailable to ensure consistent quality. | **Low** | Low | Low | Open |

## Recommendations

### 1. Safe Merge Script (Critical)
This is the highest priority because it addresses the **Critical Workflow Failure** where documentation was lost during the release phase. While the Visual Feedback Skill offers high value for development speed, the Safe Merge Script is essential for repository integrity.

### 2. ~~Visual Feedback Skill~~ → Markdown Syntax Validator (Revised)

**Analysis (2025-12-29):** After discussion, we determined that the "visual feedback" problems from feature 024 fall into three categories:

| Problem Type | AI Can Solve Alone? | Solution |
| :--- | :--- | :--- |
| **Syntax errors** (broken tables/headings) | ✅ Yes | Static validation before UAT |
| **Code completeness** (missing icons) | ✅ Yes | Better checklists + test coverage |
| **Spacing/margins** | ❌ No | Requires human feedback |

**Key insights:**
- Broken tables and headings are **syntax errors**, not visual rendering issues. They can be detected with static analysis (column count mismatches, missing separators, headline formatting).
- Missing icons were a **code completeness** issue—the agent missed cases in logic, not a failure to "see" the output.
- Spacing/margin issues require **aesthetic judgment**—AI cannot determine if spacing is "too much" or "too little" without human feedback.

**Conclusion:** A screenshot-based visual feedback skill would be high effort and low value because:
1. Most problems can be caught with simpler static analysis.
2. Aesthetic judgment (spacing) cannot be automated—it still requires Maintainer feedback.

**Replacement:** Create `scripts/validate-markdown.sh` to detect broken tables and headings before UAT.

### 3. Merge Conflict Guardrail (High)
Even with a script, the Release Manager should have a manual verification step to ensure that the merge result is as expected, especially for critical documentation files.

### 4. Commit Amend Guideline (Medium)
To maintain a clean history, agents should avoid "fixup" commits for work they just did. If feedback requires changes to the most recent commit, agents should use `git commit --amend` instead of creating a new commit.

## Next Steps
1. Implement `scripts/safe-merge.sh` (Critical).
2. ~~Research markdown-to-image rendering tools for the Visual Feedback Skill.~~ (Deprioritized - see analysis above)
3. Implement `scripts/validate-markdown.sh` for broken table/heading detection (Medium priority).
