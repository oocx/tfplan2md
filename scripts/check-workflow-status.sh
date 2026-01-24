#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export GH_PAGER=cat
export GH_FORCE_TTY=false
export PAGER=cat

usage() {
  cat <<'USAGE'
Usage:
  scripts/check-workflow-status.sh [--repo <owner/repo>] list [--branch <branch>] [--workflow <workflow-name>] [--limit <n>]
  scripts/check-workflow-status.sh [--repo <owner/repo>] watch <run-id> [--quiet]
  scripts/check-workflow-status.sh [--repo <owner/repo>] trigger <workflow-file> [--field key=value ...]
  scripts/check-workflow-status.sh [--repo <owner/repo>] view <run-id>

Commands:
  list      List workflow runs (default: all runs)
  watch     Watch a specific workflow run until completion
            --quiet: Output only final status line: WORKFLOW: SUCCESS|FAILURE|CANCELLED (agent-friendly)
  trigger   Trigger a workflow with optional input fields
  view      View details of a specific workflow run

Options:
  --repo <owner/repo>       Target repository (overrides GH_REPO)
  --branch <branch>         Filter runs by branch (for list command)
  --workflow <name>         Filter runs by workflow name/file (for list command)
  --limit <n>               Limit number of results (default: 1 for list)
  --field key=value         Workflow input field (for trigger command)

Examples:
  # List latest run on main branch
  scripts/check-workflow-status.sh --repo oocx/tfplan2md list --branch main --limit 1

  # List latest release workflow run
  scripts/check-workflow-status.sh --repo oocx/tfplan2md list --workflow release.yml --limit 1

  # Watch a specific run (verbose)
  scripts/check-workflow-status.sh --repo oocx/tfplan2md watch 12345678

  # Watch a specific run (quiet, agent-friendly)
  scripts/check-workflow-status.sh --repo oocx/tfplan2md watch 12345678 --quiet

  # View run details
  scripts/check-workflow-status.sh --repo oocx/tfplan2md view 12345678

  # Trigger release workflow with tag
  scripts/check-workflow-status.sh --repo oocx/tfplan2md trigger release.yml --field tag=v1.0.0

Notes:
  - All commands suppress interactive pagers (GH_PAGER=cat, GH_FORCE_TTY=false)
  - Designed for permanent approval in VS Code (single script vs multiple gh commands)
  - Use this instead of raw `gh run` commands to reduce approval friction
  - Use --quiet flag for agent consumption to minimize token usage
USAGE
}

repo=""

gh_safe() {
  unset GH_REPO
  if [[ -n "$repo" ]]; then
    GH_PAGER=cat GH_FORCE_TTY=false gh "$@" --repo "$repo"
  else
    GH_PAGER=cat GH_FORCE_TTY=false gh "$@"
  fi
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
  local quiet=false
  local run_id=""
  
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --quiet)
        quiet=true
        shift
        ;;
      *)
        if [[ -z "$run_id" ]]; then
          run_id="$1"
          shift
        else
          echo "Error: Unknown argument '$1' for watch command" >&2
          usage
          exit 1
        fi
        ;;
    esac
  done
  
  if [[ -z "$run_id" ]]; then
    echo "Error: watch command requires exactly one argument (run-id)" >&2
    usage
    exit 1
  fi
  
  if [[ "$quiet" == "true" ]]; then
    # Quiet mode: Run watch, capture output, and print only final status
    local watch_output
    watch_output=$(gh_safe run watch "$run_id" 2>&1) || true
    
    # Extract final status from the run
    local run_status
    run_status=$(gh_safe run view "$run_id" --json conclusion -q '.conclusion' 2>/dev/null || echo "UNKNOWN")
    
    if [[ "$run_status" == "success" ]]; then
      echo "WORKFLOW: SUCCESS"
    elif [[ "$run_status" == "failure" ]]; then
      echo "WORKFLOW: FAILURE"
    elif [[ "$run_status" == "cancelled" ]]; then
      echo "WORKFLOW: CANCELLED"
    else
      echo "WORKFLOW: $run_status"
    fi
  else
    # Normal mode: Pass through to gh run watch
    gh_safe run watch "$run_id"
  fi
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

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --repo)
        repo="$2"
        shift 2
        ;;
      *)
        break
        ;;
    esac
  done
  
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
