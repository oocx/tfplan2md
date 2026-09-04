#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd "$REPO_ROOT"

# Ensure a workspace-local temp dir exists (avoid /tmp)
mkdir -p .tmp/tests-shell
test_dir=".tmp/tests-shell/validate-test-$$"
mkdir -p "$test_dir"

cleanup() {
  # Restore original comprehensive demo artifacts if they were backed up
  if [[ -f "$test_dir/comprehensive-demo.md.bak" ]]; then
    cp "$test_dir/comprehensive-demo.md.bak" artifacts/comprehensive-demo.md
  fi
  if [[ -f "$test_dir/comprehensive-demo-simple-diff.md.bak" ]]; then
    cp "$test_dir/comprehensive-demo-simple-diff.md.bak" artifacts/comprehensive-demo-simple-diff.md
  fi
  rm -rf "$test_dir" || true
}
trap cleanup EXIT

# Back up and replace real artifacts with test placeholders
mkdir -p artifacts
if [[ -f artifacts/comprehensive-demo.md ]]; then
  cp artifacts/comprehensive-demo.md "$test_dir/comprehensive-demo.md.bak"
fi
if [[ -f artifacts/comprehensive-demo-simple-diff.md ]]; then
  cp artifacts/comprehensive-demo-simple-diff.md "$test_dir/comprehensive-demo-simple-diff.md.bak"
fi

echo "# default github" > artifacts/comprehensive-demo-simple-diff.md
echo "# default azdo" > artifacts/comprehensive-demo.md

# Source helper
# shellcheck source=scripts/uat-helpers.sh
source scripts/uat-helpers.sh

fail=0

# Test: minimal artifact should be rejected
echo "# minimal" > "$test_dir/uat-minimal.md"
if validate_artifact github "$test_dir/uat-minimal.md" false false >/dev/null 2>&1; then
  echo "ERROR: minimal artifact should have been rejected"
  fail=1
else
  echo "OK: minimal artifact rejected"
fi

# Test: force override must accept
if artifact=$(validate_artifact github "$test_dir/uat-minimal.md" false true); then
  echo "OK: minimal artifact accepted with force: $artifact"
else
  echo "ERROR: minimal artifact should be accepted with force"
  fail=1
fi

# Test: simulation artifact rejected without simulate
echo "# sim" > "$test_dir/uat-simulation-2025-12-26.md"
if validate_artifact github "$test_dir/uat-simulation-2025-12-26.md" false false >/dev/null 2>&1; then
  echo "ERROR: simulation artifact should have been rejected"
  fail=1
else
  echo "OK: simulation artifact rejected"
fi

# Test: simulation artifact accepted with simulate flag
if artifact=$(validate_artifact github "$test_dir/uat-simulation-2025-12-26.md" true false); then
  echo "OK: simulation artifact accepted with simulate flag: $artifact"
else
  echo "ERROR: simulation artifact should be accepted with simulate"
  fail=1
fi

# Test: defaults select existing files
if artifact=$(validate_artifact github "" false false); then
  echo "OK: default github artifact selected: $artifact"
else
  echo "ERROR: default github artifact selection failed"
  fail=1
fi

if artifact=$(validate_artifact azdo "" false false); then
  echo "OK: default azdo artifact selected: $artifact"
else
  echo "ERROR: default azdo artifact selection failed"
  fail=1
fi

if [[ $fail -ne 0 ]]; then
  echo "One or more tests failed." >&2
  exit 1
fi

echo "All tests passed."