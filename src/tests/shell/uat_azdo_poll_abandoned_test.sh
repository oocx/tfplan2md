#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$REPO_ROOT"

# Ensure a workspace-local temp dir exists (avoid /tmp)
mkdir -p .tmp/tests-shell
stub_dir=".tmp/tests-shell/az-stub-$$"
mkdir -p "$stub_dir"

cleanup() {
  rm -rf "$stub_dir" || true
}
trap cleanup EXIT

# Stub az CLI so scripts can run without real Azure auth.
# We only implement the commands used by scripts/uat-azdo.sh poll.
cat > "$stub_dir/az" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "repos" && "${2:-}" == "pr" && "${3:-}" == "show" ]]; then
  # Return an abandoned PR with no reviewers.
  echo '{"status":"abandoned","reviewers":[]}'
  exit 0
fi

echo "az-stub: unsupported args: $*" >&2
exit 1
EOF
chmod +x "$stub_dir/az"

PATH="$REPO_ROOT/$stub_dir:$PATH"

set +e
scripts/uat-azdo.sh poll 123 >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -ne 2 ]]; then
  echo "ERROR: expected exit code 2 for abandoned PR, got $rc" >&2
  exit 1
fi

echo "OK: abandoned PR causes fatal exit (2)"
