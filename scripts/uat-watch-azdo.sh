#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage:
  scripts/uat-watch-azdo.sh <pr-id> [--interval-seconds <n>] [--timeout-seconds <n>]

Watches an Azure DevOps PR for maintainer feedback/approval by repeatedly calling:
  scripts/uat-azdo.sh poll <pr-id>

Exit codes:
  0  Approval detected / PR completed
  1  Timed out waiting for approval
  2  Invalid usage / missing dependency

Defaults:
  --interval-seconds 60
  --timeout-seconds  3600

Notes:
  - Requires: az authenticated + azure-devops extension
  - Designed to be a single stable command to minimize approvals.
USAGE
}

main() {
  if [[ $# -lt 1 ]]; then
    usage
    exit 2
  fi

  local pr_id="$1"
  shift

  local interval_seconds=60
  local timeout_seconds=3600

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --interval-seconds)
        interval_seconds="$2"
        shift 2
        ;;
      --timeout-seconds)
        timeout_seconds="$2"
        shift 2
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      *)
        echo "Error: unknown arg: $1" >&2
        usage
        exit 2
        ;;
    esac
  done

  if ! command -v az >/dev/null 2>&1; then
    echo "Error: az is not installed." >&2
    exit 2
  fi

  if ! az account show >/dev/null 2>&1; then
    echo "Error: az is not authenticated. Run: az login" >&2
    exit 2
  fi

  local start
  start="$(date +%s)"
  local deadline=$((start + timeout_seconds))

  while true; do
    scripts/uat-azdo.sh poll "$pr_id"
    rc=$?
    if [[ $rc -eq 0 ]]; then
      exit 0
    fi
    if [[ $rc -eq 2 ]]; then
      echo "Polling failed with a fatal error (PR #$pr_id)." >&2
      exit 2
    fi

    local now
    now="$(date +%s)"
    if (( now >= deadline )); then
      echo "Timed out waiting for approval (PR #$pr_id)." >&2
      exit 1
    fi

    sleep "$interval_seconds"
  done
}

main "$@"
