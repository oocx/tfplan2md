#!/usr/bin/env bash
# Deny invocations of unauthorized sub-agent types from the task tool.
#
# The Workflow Orchestrator must only invoke custom agents (architect,
# code-reviewer, developer, issue-analyst, quality-engineer,
# release-manager, requirements-engineer, retrospective, task-planner,
# technical-writer, uat-tester, web-designer, workflow-engineer).
# Generic built-in agents (explore, task, general-purpose) are NOT permitted.
#
# This script is called as a preToolUse hook. It receives a JSON payload on
# stdin describing the pending tool invocation and writes a JSON
# permissionDecision to stdout when the call should be denied.
#
# Input schema (relevant fields):
#   toolName  — name of the tool being invoked (e.g. "task", "bash", "edit")
#   toolArgs  — JSON-encoded string of the tool's arguments
#
# Output schema (when denying):
#   permissionDecision       — "deny"
#   permissionDecisionReason — human-readable explanation

set -euo pipefail

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName')

# Only intercept the task tool; allow everything else.
if [ "$TOOL_NAME" != "task" ]; then
  exit 0
fi

AGENT_TYPE=$(echo "$INPUT" | jq -r '.toolArgs | fromjson | .agent_type // empty' 2>/dev/null || true)
# If toolArgs is absent or not valid JSON, AGENT_TYPE is empty and the case
# below won't match — the call is allowed through (safe fail-open behaviour).

case "$AGENT_TYPE" in
  explore|task|general-purpose)
    jq -cn \
      --arg agent "$AGENT_TYPE" \
      '{
        permissionDecision: "deny",
        permissionDecisionReason: ("Unauthorized agent type: \"" + $agent + "\". REMINDER: You are the Workflow Orchestrator. You must ONLY invoke custom agents (architect, code-reviewer, developer, issue-analyst, quality-engineer, release-manager, requirements-engineer, retrospective, task-planner, technical-writer, uat-tester, web-designer, workflow-engineer). Generic agents (explore, task, general-purpose) are NOT permitted. You are an orchestrator only — delegate using custom agents, never analyze or implement anything yourself.")
      }'
    ;;
esac
