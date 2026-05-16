#!/usr/bin/env bash
# validate-work-protocol.sh — preToolUse hook for the report_progress tool.
#
# Denies report_progress (which commits and pushes to the PR) if any
# work-protocol.md that is part of the pending changes is missing required
# agent log entries for its workflow type.
#
# Required agents by workflow type:
#   Feature  (docs/features/): Requirements Engineer, Architect,
#                              Quality Engineer, Task Planner, Developer,
#                              Technical Writer, Code Reviewer, Release Manager
#   Bug Fix  (docs/issues/):   Issue Analyst, Developer, Technical Writer,
#                              Code Reviewer, Release Manager
#   Workflow (docs/workflow/): Workflow Engineer, Release Manager
#
# Trigger condition — the check is applied only after the "pre-Release-Manager"
# agent has run (Code Reviewer for features/bugs; Workflow Engineer for workflow).
# This prevents spurious denials for early-stage intermediate pushes (before those
# agents have had a chance to run).
#
# Input  (stdin): JSON hook payload with at least { "toolName": "..." }
# Output (stdout): JSON { permissionDecision, permissionDecisionReason } when denying;
#                  nothing (exit 0) when allowing.

set -euo pipefail

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName // empty' 2>/dev/null || true)

# Only intercept the report_progress tool; pass everything else through.
[[ "$TOOL_NAME" == "report_progress" ]] || exit 0

# ---------------------------------------------------------------------------
# Discover work-protocol.md files that are part of the current changes.
# We check:
#   1. Commits ahead of the remote tracking branch (not yet pushed).
#   2. Staged changes (git diff --cached).
#   3. All commits on this branch relative to the common ancestor with
#      origin/HEAD (catches already-pushed commits from the same session).
# ---------------------------------------------------------------------------
find_changed_protocols() {
  local files=""

  # Commits not yet pushed (most common case in a coding-agent session)
  if git rev-parse --abbrev-ref '@{u}' &>/dev/null 2>&1; then
    files+=$(git diff --name-only "@{u}..HEAD" 2>/dev/null | grep 'work-protocol\.md' || true)
    files+=$'\n'
  fi

  # Staged changes (uncommitted but staged)
  files+=$(git diff --cached --name-only 2>/dev/null | grep 'work-protocol\.md' || true)
  files+=$'\n'

  # All commits on this branch vs. common ancestor with origin/HEAD
  local base
  base=$(git merge-base HEAD origin/HEAD 2>/dev/null || true)
  if [[ -n "$base" ]]; then
    files+=$(git diff --name-only "${base}..HEAD" 2>/dev/null | grep 'work-protocol\.md' || true)
    files+=$'\n'
  fi

  echo "$files" | sort -u | grep -v '^[[:space:]]*$'
}

PROTOCOLS=$(find_changed_protocols 2>/dev/null || true)

# No work-protocol.md in the pending changes — allow through.
[[ -n "$PROTOCOLS" ]] || exit 0

ERRORS=()

while IFS= read -r protocol_path; do
  [[ -z "$protocol_path" ]] && continue
  [[ -f "$protocol_path" ]] || continue

  content=$(cat "$protocol_path")

  # Determine required agents and the "trigger agent" from the work-item path.
  # The trigger agent is the last required agent *before* Release Manager.
  # We only run the completeness check once the trigger agent has logged its
  # entry — this prevents blocking early-stage pushes.
  case "$protocol_path" in
    docs/features/*)
      trigger_agent="Code Reviewer"
      required_agents=("Requirements Engineer" "Architect" "Quality Engineer"
                       "Task Planner" "Developer" "Technical Writer"
                       "Code Reviewer" "Release Manager")
      ;;
    docs/issues/*)
      trigger_agent="Code Reviewer"
      required_agents=("Issue Analyst" "Developer" "Technical Writer"
                       "Code Reviewer" "Release Manager")
      ;;
    docs/workflow/*)
      trigger_agent="Workflow Engineer"
      required_agents=("Workflow Engineer" "Release Manager")
      ;;
    *)
      # Unknown work-item type — skip validation to avoid false positives.
      continue
      ;;
  esac

  # Only check completeness after the trigger agent has run.
  # Use a literal string match against the ### heading to avoid false positives.
  if ! grep -qF "### ${trigger_agent}" "$protocol_path" 2>/dev/null; then
    continue
  fi

  # Check that every required agent has a log entry.
  for agent in "${required_agents[@]}"; do
    if ! grep -qF "### ${agent}" "$protocol_path" 2>/dev/null; then
      ERRORS+=("  • ${protocol_path}: missing '### ${agent}' entry")
    fi
  done

done <<< "$PROTOCOLS"

# All checks passed — allow the push.
[[ ${#ERRORS[@]} -eq 0 ]] && exit 0

# Build a human-readable denial message.
MISSING=$(printf '%s\n' "${ERRORS[@]}")
REASON=$(printf '%s\n' \
  "🚫 Cannot push: work-protocol.md is missing required agent log entries." \
  "" \
  "Missing entries:" \
  "${MISSING}" \
  "" \
  "Action required: delegate to each missing agent and ask them to append" \
  "their '### <Agent Name>' log entry to work-protocol.md, then retry." \
  "Reference: docs/agents.md → Required Agents by Workflow Type.")

jq -cn --arg reason "$REASON" \
  '{permissionDecision: "deny", permissionDecisionReason: $reason}'
