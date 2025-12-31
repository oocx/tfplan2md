#!/usr/bin/env bash
set -euo pipefail

# Wrapper to prevent hung test runs.
#
# Usage:
#   scripts/test-with-timeout.sh [--timeout-seconds <n>] [--grace-seconds <n>] [dotnet-test-args...]
#   scripts/test-with-timeout.sh [--timeout-seconds <n>] [--grace-seconds <n>] -- <command> [args...]
#
# Default behavior (no "--"): runs `dotnet test` with the provided args.
#
# Exit codes:
#   0..   Command exit code
#   124   Timed out
#   125   Wrapper error / invalid usage

timeout_seconds=60
grace_seconds=10
explicit_command=false

usage() {
  cat <<'USAGE'
Usage:
  scripts/test-with-timeout.sh [--timeout-seconds <n>] [--grace-seconds <n>] [dotnet-test-args...]
  scripts/test-with-timeout.sh [--timeout-seconds <n>] [--grace-seconds <n>] -- <command> [args...]

Examples:
  scripts/test-with-timeout.sh --timeout-seconds 1800 --no-build --configuration Release
  scripts/test-with-timeout.sh --timeout-seconds 5 -- bash -c 'sleep 30'
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --timeout-seconds)
      timeout_seconds="${2:-}"
      shift 2
      ;;
    --grace-seconds)
      grace_seconds="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    --)
      shift
      explicit_command=true
      break
      ;;
    *)
      break
      ;;
  esac
done

if [[ -z "$timeout_seconds" || -z "$grace_seconds" ]]; then
  usage >&2
  exit 125
fi

if ! [[ "$timeout_seconds" =~ ^[0-9]+$ ]] || ! [[ "$grace_seconds" =~ ^[0-9]+$ ]]; then
  echo "ERROR: timeout and grace must be integers (seconds)." >&2
  exit 125
fi

cmd=()
if [[ "$explicit_command" == "true" ]]; then
  if [[ ${#@} -eq 0 ]]; then
    usage >&2
    exit 125
  fi
  cmd=("$@");
else
  cmd=(dotnet test "$@")
fi

if ! command -v "${cmd[0]}" >/dev/null 2>&1; then
  echo "ERROR: command not found: ${cmd[0]}" >&2
  exit 125
fi

# Run the command in its own process group (best-effort), so we can terminate
# the whole tree if it hangs.

set +e
if command -v setsid >/dev/null 2>&1; then
  setsid "${cmd[@]}" &
else
  "${cmd[@]}" &
fi
pid=$!
set -e

kill_tree() {
  local signal="$1"

  # Try process group first; if that fails, fall back to the direct PID.
  kill -"$signal" -- "-$pid" >/dev/null 2>&1 && return 0
  kill -"$signal" -- "$pid" >/dev/null 2>&1 && return 0

  return 0
}

start_epoch="$(date +%s)"
while kill -0 "$pid" >/dev/null 2>&1; do
  now_epoch="$(date +%s)"
  elapsed=$((now_epoch - start_epoch))
  if (( elapsed >= timeout_seconds )); then
    echo "ERROR: test command timed out after ${timeout_seconds}s." >&2

    kill_tree TERM
    sleep "$grace_seconds" || true
    kill_tree KILL

    wait "$pid" >/dev/null 2>&1 || true
    exit 124
  fi

  sleep 1
done

wait "$pid"
exit $?
