#!/usr/bin/env bash
# protect-changelog.sh - GitHub Copilot preToolUse hook.
#
# Denies any attempt to directly edit or create CHANGELOG.md. CHANGELOG.md is
# maintained by the Release Manager as part of the normal workflow. Agents other
# than the Release Manager must never edit it manually.
#
# Input (stdin): JSON with fields: toolName, toolArgs
# Output:        JSON permissionDecision when denying; nothing otherwise (exit 0)

set -euo pipefail

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName')

# Only intercept file-writing tools
if [[ "$TOOL_NAME" != "edit" && "$TOOL_NAME" != "create" ]]; then
  exit 0
fi

# Extract the file path from tool arguments
FILE_PATH=$(echo "$INPUT" | jq -r '.toolArgs | fromjson | .path // empty' 2>/dev/null || true)

if [[ -z "$FILE_PATH" ]]; then
  exit 0
fi

# Check if the target file is CHANGELOG.md (matches both ./CHANGELOG.md and CHANGELOG.md)
if [[ "$FILE_PATH" == *"CHANGELOG.md" ]]; then
  jq -cn '{
    permissionDecision: "deny",
    permissionDecisionReason: "CHANGELOG.md must not be edited manually. It is maintained by the Release Manager as part of the release workflow. If the changelog is missing or out of date, the workflow has not reached the Release Manager stage yet — the Workflow Orchestrator will invoke the Release Manager at the appropriate point in the process."
  }'
fi
