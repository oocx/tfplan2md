#!/usr/bin/env bash
# session-start.sh - GitHub Copilot session start hook.
#
# Runs automatically when a new Copilot agent session begins. Performs these
# checks and stores the results so agents can act on them immediately:
#
#   1. Calculates the next available work-item issue number and stores it in
#      .next-issue-number so agents can read the pre-calculated value.
#
#   2. Warns when the current branch is 'main'. Agents must never commit
#      directly to main; they should create a feature/fix/workflow branch first.
#
#   3. Displays the current work-protocol.md status when a recognised work
#      item is active (branch matches feature/NNN-*, fix/NNN-*, workflow/NNN-*).
#      This lets every agent see at a glance which required agents have and
#      have not yet logged their work, reducing the risk of skipped steps.
#
# Input (stdin): JSON with fields: timestamp, cwd, source, initialPrompt
# Output:        Ignored by the hook runner; results written to .next-issue-number

set -euo pipefail

# Consume the hook input (required by the hook protocol even if unused)
INPUT=$(cat)

SOURCE=$(echo "$INPUT" | jq -r '.source // "unknown"' 2>/dev/null || echo "unknown")

# ── 1. Next available issue number ───────────────────────────────────────────

NEXT_NUMBER=$(scripts/next-issue-number.sh 2>/dev/null) || {
    >&2 echo "Warning: next-issue-number.sh failed (exit code $?). Skipping .next-issue-number update."
    exit 0
}

# Store the result for agent consumption
echo "$NEXT_NUMBER" > .next-issue-number

>&2 echo "Session ($SOURCE): next available issue number is $NEXT_NUMBER (stored in .next-issue-number)"

# ── 2. Warn when on the main branch ──────────────────────────────────────────

CURRENT_BRANCH=$(git branch --show-current 2>/dev/null || echo "")
if [[ "$CURRENT_BRANCH" == "main" ]]; then
  >&2 echo ""
  >&2 echo "┌─────────────────────────────────────────────────────────────┐"
  >&2 echo "│ ⚠️  WARNING: You are on the 'main' branch.                  │"
  >&2 echo "│   Do NOT commit or push here.                               │"
  >&2 echo "│   Create a feature/, fix/, or workflow/ branch FIRST.       │"
  >&2 echo "└─────────────────────────────────────────────────────────────┘"
  >&2 echo ""
fi

# ── 3. Work-protocol status ───────────────────────────────────────────────────

WORK_PROTOCOL_OUTPUT=$(scripts/validate-work-protocol.sh 2>/dev/null || true)
if [[ -n "$WORK_PROTOCOL_OUTPUT" ]]; then
  >&2 echo ""
  >&2 echo "┌─── Work Protocol Status ────────────────────────────────────┐"
  while IFS= read -r line; do
    >&2 printf "│ %-61s│\n" "$line"
  done <<< "$WORK_PROTOCOL_OUTPUT"
  >&2 echo "└─────────────────────────────────────────────────────────────┘"
  >&2 echo ""
fi
