#!/usr/bin/env bash
# validate-work-protocol.sh
#
# Reports which required agents have and have not yet logged their work in the
# current work item's work-protocol.md. Designed to be called at session start
# so every agent immediately sees the workflow state.
#
# The check is informational: the script exits 1 when required agents are
# missing (so callers can react), but agents are never hard-blocked here.
#
# Usage:
#   scripts/validate-work-protocol.sh [--work-item-dir <path>]
#
# Options:
#   --work-item-dir  Explicit path to the work item directory.
#                    Auto-detected from the current git branch when omitted.
#
# Exit codes:
#   0  All required agents have logged, or no work protocol found (nothing to check)
#   1  One or more required agents have not yet logged their work

set -euo pipefail

# Must be run from the repository root
REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
cd "$REPO_ROOT"

WORK_ITEM_DIR=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --work-item-dir)
      WORK_ITEM_DIR="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

# Auto-detect work item directory from git branch name
if [[ -z "$WORK_ITEM_DIR" ]]; then
  BRANCH=$(git branch --show-current 2>/dev/null || echo "")
  if [[ -z "$BRANCH" ]]; then
    exit 0
  fi

  if [[ "$BRANCH" =~ ^feature/([0-9]+-[^/]+) ]]; then
    WORK_ITEM_DIR="docs/features/${BASH_REMATCH[1]}"
  elif [[ "$BRANCH" =~ ^fix/([0-9]+-[^/]+) ]]; then
    WORK_ITEM_DIR="docs/issues/${BASH_REMATCH[1]}"
  elif [[ "$BRANCH" =~ ^workflow/([0-9]+-[^/]+) ]]; then
    WORK_ITEM_DIR="docs/workflow/${BASH_REMATCH[1]}"
  else
    # Branch pattern not recognized (e.g. copilot/* or main); skip check
    exit 0
  fi
fi

WORK_PROTOCOL_FILE="$WORK_ITEM_DIR/work-protocol.md"

if [[ ! -f "$WORK_PROTOCOL_FILE" ]]; then
  exit 0
fi

# Determine workflow type from the Work Protocol header.
# The line may use Markdown bold markers: **Workflow Type:** Feature
WORKFLOW_TYPE_RAW=$(grep -i "Workflow Type:" "$WORK_PROTOCOL_FILE" | head -1 \
  | sed 's/.*Workflow Type:[[:space:]]*//' \
  | sed 's/\*\*//g' \
  | tr -d '[:space:]' \
  | tr '[:upper:]' '[:lower:]' \
  || true)

case "$WORKFLOW_TYPE_RAW" in
  feature)
    WORKFLOW_TYPE="Feature"
    REQUIRED_AGENTS=("Requirements Engineer" "Architect" "Quality Engineer" "Task Planner" "Developer" "Technical Writer" "Code Reviewer" "Release Manager")
    ;;
  bugfix|"bug fix"|"bug-fix")
    WORKFLOW_TYPE="Bug Fix"
    REQUIRED_AGENTS=("Issue Analyst" "Developer" "Technical Writer" "Code Reviewer" "Release Manager")
    ;;
  workflow)
    WORKFLOW_TYPE="Workflow"
    REQUIRED_AGENTS=("Workflow Engineer" "Release Manager")
    ;;
  *)
    # Unknown workflow type; show logged agents without required-agent validation
    WORKFLOW_TYPE="${WORKFLOW_TYPE_RAW:-unknown}"
    REQUIRED_AGENTS=()
    ;;
esac

# Collect raw section headings (### …) from the Agent Work Log section.
# Headers often include a date suffix: "### Developer - 2026-02-10"
mapfile -t LOGGED_HEADINGS < <(grep -E "^###[[:space:]]+" "$WORK_PROTOCOL_FILE" | sed 's/^###[[:space:]]*//' || true)

echo "Work Protocol: $WORK_ITEM_DIR ($WORKFLOW_TYPE)"
echo ""

if [[ ${#REQUIRED_AGENTS[@]} -eq 0 ]]; then
  if [[ ${#LOGGED_HEADINGS[@]} -gt 0 ]]; then
    echo "Agents logged:"
    for heading in "${LOGGED_HEADINGS[@]}"; do
      echo "  ✅ $heading"
    done
  else
    echo "  (no agent entries yet)"
  fi
  exit 0
fi

# Check each required agent: a heading is considered a match when the agent
# name (case-insensitive) appears at the start of the heading text (allowing
# for date suffixes like "- 2026-02-10" or "(coding agent)" annotations).
MISSING=()
for agent in "${REQUIRED_AGENTS[@]}"; do
  FOUND=false
  AGENT_LOWER="${agent,,}"
  for heading in "${LOGGED_HEADINGS[@]}"; do
    HEADING_LOWER="${heading,,}"
    # Strip everything after " -" or " (" to get the bare agent name from heading
    HEADING_NAME=$(echo "$HEADING_LOWER" | sed 's/[[:space:]]*[-[(].*//')
    if [[ "$HEADING_NAME" == "$AGENT_LOWER" ]]; then
      FOUND=true
      break
    fi
  done

  if [[ "$FOUND" == true ]]; then
    echo "  ✅ $agent"
  else
    echo "  ⬜ $agent (not yet logged)"
    MISSING+=("$agent")
  fi
done

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo ""
  echo "  ⚠️  Missing required entries: ${MISSING[*]}"
  exit 1
fi

echo ""
echo "  ✅ All required agents have logged their work."
exit 0
