#!/usr/bin/env bash
# Script to close fixed issues with appropriate comments
# Run this script with: ./close-fixed-issues.sh

set -e

# Ensure gh CLI is authenticated
if ! gh auth status &>/dev/null; then
    echo "Error: gh CLI is not authenticated. Please run: gh auth login"
    exit 1
fi

echo "=== Closing Fixed Issues ==="
echo ""

# Issue #374
echo "Commenting on and closing issue #374..."
gh issue comment 374 --body "This issue was fixed by PR #377 (merged on 2026-01-29).

The CI workflow now implements Release Gating: it only creates a new version tag when the published Docker image would change. This includes changes to runtime code (\`src/\` excluding test directories), example files (\`examples/\`), and Docker build configuration.

Workflow/internal-tooling changes (\`.github/\`, \`scripts/\`, \`docs/\`, \`website/\`) intentionally do not trigger releases.

Evidence: See \`docs/spec.md\` line 84.

Closing as completed."

gh issue close 374 --reason completed
echo "✅ Issue #374 closed"
echo ""

# Issue #375
echo "Commenting on and closing issue #375..."
gh issue comment 375 --body "This issue was fixed by PR #377 (merged on 2026-01-29) and commit dd9b742 (2026-02-08).

The solution includes:
1. PR Validation guardrail that blocks version-bumping commit types for workflow-only changes
2. Commit Guardrails documented in docs/spec.md (line 86)
3. Agent instructions updated to enforce this rule

Workflow changes now correctly use \`workflow:\`, \`docs:\`, \`chore:\`, or \`ci:\` commit types instead of \`feat:\` or \`fix:\`, preventing unintended version bumps.

Closing as completed."

gh issue close 375 --reason completed
echo "✅ Issue #375 closed"
echo ""

# Issue #326
echo "Commenting on and closing issue #326..."
gh issue comment 326 --body "This issue was fixed by PR #334 (merged on 2026-01-21).

The implementation includes:
- Code coverage collection in PR validation workflow
- CoverageEnforcer tool for threshold enforcement
- Coverage overrides via PR comments
- Automated coverage summary and badges
- Historical coverage trend tracking

All success criteria from the issue have been met.

Closing as completed."

gh issue close 326 --reason completed
echo "✅ Issue #326 closed"
echo ""

echo "=== Summary ==="
echo "Closed 3 issues that were already fixed:"
echo "  - #374: Release gating for Docker changes"
echo "  - #375: Commit type guardrails for workflow changes"
echo "  - #326: Code coverage enforcement in CI"
echo ""
echo "Complete investigation report available in .tmp/issue-closure-report.md"
