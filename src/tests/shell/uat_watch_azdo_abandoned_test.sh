#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$REPO_ROOT"

mkdir -p .tmp/tests-shell
stub_dir=".tmp/tests-shell/az-stub-$$"
mkdir -p "$stub_dir"

cleanup() {
  rm -rf "$stub_dir" || true
}
trap cleanup EXIT

# Stub az CLI so scripts/uat-watch-azdo.sh can run without real Azure auth.
cat > "$stub_dir/az" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

# Auth check used by watch script
if [[ "${1:-}" == "account" && "${2:-}" == "show" ]]; then
  echo '{"tenantId":"stub"}'
  exit 0
fi

# Poll implementation used by uat-azdo.sh
if [[ "${1:-}" == "repos" && "${2:-}" == "pr" && "${3:-}" == "show" ]]; then
  echo '{"status":"abandoned","reviewers":[]}'
  exit 0
fi

echo "az-stub: unsupported args: $*" >&2
exit 1
EOF
chmod +x "$stub_dir/az"

PATH="$REPO_ROOT/$stub_dir:$PATH"

set +e
scripts/uat-watch-azdo.sh 123 --interval-seconds 1 --timeout-seconds 10 >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -ne 2 ]]; then
  echo "ERROR: expected exit code 2 for fatal polling error, got $rc" >&2
  exit 1
fi

echo "OK: watch script stops on abandoned PR (2)"
