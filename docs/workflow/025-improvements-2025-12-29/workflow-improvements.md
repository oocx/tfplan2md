# Workflow Improvements: 2025-12-29

Based on the retrospective from [Visual Report Enhancements](../../features/024-visual-report-enhancements/retrospective.md), the following workflow improvements have been identified to address friction points and failures.

## Proposed Improvements

| # | Improvement | Description | Value | Effort | Priority |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | **Safe Merge Script** | Create `scripts/safe-merge.sh` to verify file integrity (size/content) post-merge, preventing the documentation loss seen in feature 024. | **High** | Medium | **Critical** |
| 2 | **Visual Feedback Skill** | Implement `.github/skills/visual-validator/` to allow agents to "see" rendered markdown (e.g., via image conversion), reducing retries for visual alignment. | **High** | High | High |
| 3 | **UAT Signal Detection** | Enhance `scripts/uat-run.sh` or create a skill to explicitly flag "reject/fail" signals in test output, preventing UAT Tester oversight. | **Medium** | Medium | Medium |
| 4 | **Detail Checklist Gate** | Update the **Developer** agent prompt to require a "Detail Checklist" for features with many small UI/UX items to prevent quality slips. | **Medium** | Low | Medium |
| 5 | **Merge Conflict Guardrail** | Update **Release Manager** instructions to require a manual "Conflict Check" step before finalizing merges, even if the CLI reports success. | **Medium** | Low | High |
| 6 | **Commit Amend Guideline** | Update **Developer** instructions to amend the previous commit when fixing issues in the commit just created (e.g., based on feedback), ensuring a clean "1 topic per commit" history. | **Medium** | Low | Medium |
| 7 | **Model Outage Protocol** | Define a fallback protocol for agents when primary models (like `gpt-5.1-codex-max`) are unavailable to ensure consistent quality. | **Low** | Low | Low |

## Recommendations

### 1. Safe Merge Script (Critical)
This is the highest priority because it addresses the **Critical Workflow Failure** where documentation was lost during the release phase. While the Visual Feedback Skill offers high value for development speed, the Safe Merge Script is essential for repository integrity.

### 2. Visual Feedback Skill (High)
The Developer agent struggled with Azure DevOps alignment and spacing issues due to the lack of visual feedback. Providing a way for agents to "see" the rendered output will significantly reduce the number of retries and manual interventions.

### 3. Merge Conflict Guardrail (High)
Even with a script, the Release Manager should have a manual verification step to ensure that the merge result is as expected, especially for critical documentation files.

### 4. Commit Amend Guideline (Medium)
To maintain a clean history, agents should avoid "fixup" commits for work they just did. If feedback requires changes to the most recent commit, agents should use `git commit --amend` instead of creating a new commit.

## Next Steps
1. Implement `scripts/safe-merge.sh`.
2. Update `docs/agents.md` with the new Developer checklist requirement.
3. Research markdown-to-image rendering tools for the Visual Feedback Skill.
