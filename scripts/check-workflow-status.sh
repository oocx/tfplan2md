#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export GH_PAGER=cat
export GH_FORCE_TTY=false
export PAGER=cat

usage() {
  cat <<'USAGE'
Usage:
  scripts/check-workflow-status.sh list [--branch <branch>] [--workflow <workflow-name>] [--limit <n>]
  scripts/check-workflow-status.sh watch <run-id>
  scripts/check-workflow-status.sh trigger <workflow-file> [--field key=value ...]
  scripts/check-workflow-status.sh view <run-id>

Commands:
  list      List workflow runs (default: all runs)
  watch     Watch a specific workflow run until completion
  trigger   Trigger a workflow with optional input fields
  view      View details of a specific workflow run

Options:
  --branch <branch>         Filter runs by branch (for list command)
  --workflow <name>         Filter runs by workflow name/file (for list command)
  --limit <n>               Limit number of results (default: 1 for list)
  --field key=value         Workflow input field (for trigger command)

Examples:
  # List latest run on main branch
  scripts/check-workflow-status.sh list --branch main --limit 1

  # List latest release workflow run
  scripts/check-workflow-status.sh list --workflow release.yml --limit 1

  # Watch a specific run
  scripts/check-workflow-status.sh watch 12345678

  # View run details
  scripts/check-workflow-status.sh view 12345678

  # Trigger release workflow with tag
  scripts/check-workflow-status.sh trigger release.yml --field tag=v1.0.0

Notes:
  - All commands suppress interactive pagers (GH_PAGER=cat, GH_FORCE_TTY=false)
  - Designed for permanent approval in VS Code (single script vs multiple gh commands)
  - Use this instead of raw `gh run` commands to reduce approval friction
USAGE
}

gh_safe() {
  GH_PAGER=cat GH_FORCE_TTY=false gh "$@"
}

cmd_list() {
  local branch=""
  local workflow=""
  local limit="1"
  
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --branch)
        branch="$2"
        shift 2
        ;;
      --workflow)
        workflow="$2"
        shift 2
        ;;
      --limit)
        limit="$2"
        shift 2
        ;;
      *)
        echo "Error: Unknown option '$1' for list command" >&2
        usage
        exit 1
        ;;
    esac
  done
  
  local args=(run list --limit "$limit" --json databaseId,status,conclusion,name,headBranch,createdAt)
  
  if [[ -n "$branch" ]]; then
    args+=(--branch "$branch")
  fi
  
  if [[ -n "$workflow" ]]; then
    args+=(--workflow "$workflow")
  fi
  
  gh_safe "${args[@]}"
}

cmd_watch() {
  if [[ $# -ne 1 ]]; then
    echo "Error: watch command requires exactly one argument (run-id)" >&2
    usage
    exit 1
  fi
  
  local run_id="$1"
  gh_safe run watch "$run_id"
}

cmd_trigger() {
  if [[ $# -lt 1 ]]; then
    echo "Error: trigger command requires workflow file as first argument" >&2
    usage
    exit 1
  fi
  
  local workflow="$1"
  shift
  
  local args=(workflow run "$workflow")
  
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --field)
        args+=(--field "$2")
        shift 2
        ;;
      *)
        echo "Error: Unknown option '$1' for trigger command" >&2
        usage
        exit 1
        ;;
    esac
  done
  
  gh_safe "${args[@]}"
}

cmd_view() {
  if [[ $# -ne 1 ]]; then
    echo "Error: view command requires exactly one argument (run-id)" >&2
    usage
    exit 1
  fi
  
  local run_id="$1"
  gh_safe run view "$run_id" --json conclusion,status,jobs,workflowName,headBranch,createdAt,updatedAt
}

main() {
  if [[ $# -eq 0 ]]; then
    usage
    exit 1
  fi
  
  local command="$1"
  shift
  
  case "$command" in
    list)
      cmd_list "$@"
      ;;
    watch)
      cmd_watch "$@"
      ;;
    trigger)
      cmd_trigger "$@"
      ;;
    view)
      cmd_view "$@"
      ;;
    -h|--help|help)
      usage
      exit 0
      ;;
    *)
      echo "Error: Unknown command '$command'" >&2
      usage
      exit 1
      ;;
  esac
}

main "$@"
