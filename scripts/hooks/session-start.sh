#!/usr/bin/env bash
# session-start.sh - SessionStart hook.
#
# Runs automatically when a new agent session begins. Calculates the
# next available work-item issue number and stores it in .next-issue-number
# so agents can read the pre-calculated value without running the script.
#
# Input (stdin): session JSON from the harness; not used.
# Output:        Ignored by the hook runner; result is written to .next-issue-number

set -euo pipefail

# Consume the hook input (required by the hook protocol even if unused)
INPUT=$(cat)

SOURCE=$(echo "$INPUT" | jq -r '.source // "unknown"' 2>/dev/null || echo "unknown")

# Calculate the next available issue number
NEXT_NUMBER=$(scripts/next-issue-number.sh 2>/dev/null) || {
    >&2 echo "Warning: next-issue-number.sh failed (exit code $?). Skipping .next-issue-number update."
    exit 0
}

# Store the result for agent consumption
echo "$NEXT_NUMBER" > .next-issue-number

>&2 echo "Session ($SOURCE): next available issue number is $NEXT_NUMBER (stored in .next-issue-number)"
