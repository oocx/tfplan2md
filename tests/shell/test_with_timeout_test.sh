#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$REPO_ROOT"

chmod +x scripts/test-with-timeout.sh

fail=0

# Test: command should time out
start="$(date +%s)"
set +e
scripts/test-with-timeout.sh --timeout-seconds 1 --grace-seconds 1 -- bash -c 'sleep 10' >/dev/null 2>&1
rc=$?
set -e
end="$(date +%s)"
elapsed=$((end - start))

if [[ $rc -ne 124 ]]; then
  echo "ERROR: expected timeout exit code 124, got $rc" >&2
  fail=1
else
  echo "OK: timeout returns 124"
fi

if (( elapsed > 5 )); then
  echo "ERROR: timeout took too long (${elapsed}s)" >&2
  fail=1
else
  echo "OK: timeout completes quickly (${elapsed}s)"
fi

# Test: command exit code is preserved
set +e
scripts/test-with-timeout.sh --timeout-seconds 5 -- bash -c 'exit 7' >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -ne 7 ]]; then
  echo "ERROR: expected exit code 7, got $rc" >&2
  fail=1
else
  echo "OK: exit code preserved"
fi

if [[ $fail -ne 0 ]]; then
  echo "One or more tests failed." >&2
  exit 1
fi

echo "All tests passed."