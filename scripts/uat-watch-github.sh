#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage:
  scripts/uat-watch-github.sh <pr-number> [--interval-seconds <n>] [--timeout-seconds <n>]

Watches a GitHub PR for maintainer feedback/approval by repeatedly calling:
  scripts/uat-github.sh poll <pr-number>

Exit codes:
  0  Approval detected / PR closed
  1  Timed out waiting for approval
  2  Invalid usage / missing dependency

Defaults:
  --interval-seconds 60
  --timeout-seconds  3600

Notes:
  - Requires: gh authenticated
  - Designed to be a single stable command to minimize approvals.
USAGE
}

main() {
  if [[ $# -lt 1 ]]; then
    usage
    exit 2
  fi

  local pr_number="$1"
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

  if ! command -v gh >/dev/null 2>&1; then
    echo "Error: gh is not installed." >&2
    exit 2
  fi

  if ! gh auth status >/dev/null 2>&1; then
    echo "Error: gh is not authenticated. Run: gh auth login" >&2
    exit 2
  fi

  local start
  start="$(date +%s)"
  local deadline=$((start + timeout_seconds))

  while true; do
    if scripts/uat-github.sh poll "$pr_number"; then
      exit 0
    fi

    local now
    now="$(date +%s)"
    if (( now >= deadline )); then
      echo "Timed out waiting for approval (PR #$pr_number)." >&2
      exit 1
    fi

    sleep "$interval_seconds"
  done
}

main "$@"
