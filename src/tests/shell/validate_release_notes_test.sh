#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
SCRIPT_PATH="${REPO_ROOT}/scripts/validate-release-notes.sh"
TEST_ROOT="${REPO_ROOT}/.tmp/validate-release-notes-test-$$"

cleanup() {
  rm -rf "$TEST_ROOT"
}
trap cleanup EXIT

mkdir -p "$TEST_ROOT"
cd "$TEST_ROOT"
git init -q
git config user.email "test@example.com"
git config user.name "Test User"
mkdir -p docs
touch docs/.gitkeep
git add docs/.gitkeep
git commit -qm "chore: initial"

BASE_SHA="$(git rev-parse HEAD)"

# Case 1: changed docs/issues item without release-notes.md should fail
mkdir -p docs/issues/999-missing-release-notes
cat > docs/issues/999-missing-release-notes/analysis.md <<'EOF'
# Analysis
EOF
git add docs/issues/999-missing-release-notes/analysis.md
git commit -qm "docs: add issue analysis"
HEAD_SHA="$(git rev-parse HEAD)"

set +e
bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -eq 0 ]]; then
  echo "ERROR: expected failure when release-notes.md is missing"
  exit 1
fi
echo "OK: fails when changed issue work item has no release notes"

# Case 2: changed docs/features item with release-notes.md should pass
git checkout -q "$BASE_SHA"
mkdir -p docs/features/999-with-release-notes
cat > docs/features/999-with-release-notes/specification.md <<'EOF'
# Specification
EOF
cat > docs/features/999-with-release-notes/release-notes.md <<'EOF'
# Release Notes
EOF
git add docs/features/999-with-release-notes/specification.md docs/features/999-with-release-notes/release-notes.md
git commit -qm "docs: add feature with release notes"
HEAD_SHA="$(git rev-parse HEAD)"

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: passes when changed feature work item includes release notes"

# Case 3: non feature/issue docs changes should be ignored
git checkout -q "$BASE_SHA"
mkdir -p docs/workflow/999-example
cat > docs/workflow/999-example/tasks.md <<'EOF'
## Tasks
EOF
git add docs/workflow/999-example/tasks.md
git commit -qm "docs: add workflow tasks"
HEAD_SHA="$(git rev-parse HEAD)"

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: ignores non feature/issue docs changes"

echo "All tests passed."
