#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export GH_PAGER=cat
export GH_FORCE_TTY=false
export PAGER=cat

usage() {
  cat <<'USAGE'
Usage:
  scripts/analyze-run.sh [run-id] [options]

Options:
  run-id               GitHub Actions run ID (default: latest run for current branch)
  --job <job-name>     Filter logs for a specific job
  --failed             Only show logs for failed jobs/steps (default if no run-id provided)
  --workflow <name>    Filter by workflow name (e.g., "CI", "PR Validation")

Examples:
  scripts/analyze-run.sh
  scripts/analyze-run.sh 123456789
  scripts/analyze-run.sh --workflow "PR Validation" --failed
USAGE
}

RUN_ID=""
JOB_FILTER=""
FAILED_ONLY=false
WORKFLOW_FILTER=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --job)
      JOB_FILTER="$2"
      shift 2
      ;;
    --failed)
      FAILED_ONLY=true
      shift
      ;;
    --workflow)
      WORKFLOW_FILTER="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      if [[ -z "$RUN_ID" && "$1" != -* ]]; then
        RUN_ID="$1"
        shift
      else
        echo "Unknown option: $1" >&2
        usage
        exit 1
      fi
      ;;
  esac
done

# If no RUN_ID, find the latest run for the current branch
if [[ -z "$RUN_ID" ]]; then
  BRANCH=$(git branch --show-current)
  QUERY=".[]"
  if [[ -n "$WORKFLOW_FILTER" ]]; then
    QUERY=".[] | select(.workflowName == \"$WORKFLOW_FILTER\")"
  fi
  
  RUN_ID=$(gh run list --branch "$BRANCH" --limit 1 --json databaseId,workflowName --jq "$QUERY | .databaseId")
  
  if [[ -z "$RUN_ID" ]]; then
    echo "Error: No runs found for branch '$BRANCH'$( [[ -n "$WORKFLOW_FILTER" ]] && echo " and workflow '$WORKFLOW_FILTER'" )." >&2
    exit 1
  fi
  echo "Analyzing latest run: $RUN_ID" >&2
fi

# Fetch run details
echo "--- Run Summary ---"
gh run view "$RUN_ID"

# Fetch logs
echo -e "\n--- Logs ---"
if [[ -n "$JOB_FILTER" ]]; then
  gh run view "$RUN_ID" --job "$JOB_FILTER" --log
elif [[ "$FAILED_ONLY" == "true" ]]; then
  # Find failed jobs
  FAILED_JOBS=$(gh run view "$RUN_ID" --json jobs --jq '.jobs[] | select(.conclusion == "failure") | .name')
  if [[ -z "$FAILED_JOBS" ]]; then
    echo "No failed jobs found."
  else
    for job in $FAILED_JOBS; do
      echo -e "\n[FAILED JOB: $job]"
      gh run view "$RUN_ID" --job "$job" --log | grep -iE "error|failed|exception|critical" || gh run view "$RUN_ID" --job "$job" --log | tail -n 50
    done
  fi
else
  gh run view "$RUN_ID" --log | tail -n 100
fi
