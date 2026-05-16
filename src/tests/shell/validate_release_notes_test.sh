#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
SCRIPT_PATH="${REPO_ROOT}/scripts/validate-release-notes.sh"
TEST_ROOT="${REPO_ROOT}/.tmp/validate-release-notes-test-$$"
# Base64 for a 1x1 transparent PNG used in screenshot URL/dimension validation cases.
TEST_PNG_BASE64='iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9WHZ1xQAAAAASUVORK5CYII='

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

# Case 2: changed docs/features item without work-protocol.md should fail
git checkout -q "$BASE_SHA"
mkdir -p docs/features/999-missing-work-protocol
cat > docs/features/999-missing-work-protocol/specification.md <<'EOF'
# Specification
EOF
cat > docs/features/999-missing-work-protocol/release-notes.md <<'EOF'
# Release Notes
EOF
git add docs/features/999-missing-work-protocol/specification.md docs/features/999-missing-work-protocol/release-notes.md
git commit -qm "docs: add feature with release notes"
HEAD_SHA="$(git rev-parse HEAD)"

set +e
bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -eq 0 ]]; then
  echo "ERROR: expected failure when work-protocol.md is missing"
  exit 1
fi
echo "OK: fails when changed feature work item has no work protocol"

# Case 3: changed docs/features item without Release Manager work-protocol entry should fail
git checkout -q "$BASE_SHA"
mkdir -p docs/features/999-missing-release-manager
cat > docs/features/999-missing-release-manager/specification.md <<'EOF'
# Specification
EOF
cat > docs/features/999-missing-release-manager/release-notes.md <<'EOF'
# Release Notes
EOF
cat > docs/features/999-missing-release-manager/work-protocol.md <<'EOF'
# Work Protocol

## Agent Work Log

### Developer
- **Date:** 2026-05-16
EOF
git add docs/features/999-missing-release-manager/specification.md \
  docs/features/999-missing-release-manager/release-notes.md \
  docs/features/999-missing-release-manager/work-protocol.md
git commit -qm "docs: add incomplete work protocol"
HEAD_SHA="$(git rev-parse HEAD)"

set +e
bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
rc=$?
set -e

if [[ $rc -eq 0 ]]; then
  echo "ERROR: expected failure when Release Manager entry is missing"
  exit 1
fi
echo "OK: fails when Release Manager entry is missing from work protocol"

# Case 4: changed docs/workflow item with release-notes.md and work-protocol.md should pass
git checkout -q "$BASE_SHA"
mkdir -p docs/workflow/999-example
cat > docs/workflow/999-example/tasks.md <<'EOF'
## Tasks
EOF
cat > docs/workflow/999-example/release-notes.md <<'EOF'
# Release Notes
EOF
cat > docs/workflow/999-example/work-protocol.md <<'EOF'
# Work Protocol

## Agent Work Log

### Release Manager
- **Date:** 2026-05-16
EOF
git add docs/workflow/999-example/tasks.md docs/workflow/999-example/release-notes.md docs/workflow/999-example/work-protocol.md
git commit -qm "docs: add workflow release artifacts"
HEAD_SHA="$(git rev-parse HEAD)"

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: passes when changed workflow work item includes release artifacts"

# Case 5: code changes without any work item docs should fail
git checkout -q "$BASE_SHA"
mkdir -p scripts
cat > scripts/example.sh <<'EOF'
#!/usr/bin/env bash
echo "example"
EOF
chmod +x scripts/example.sh
git add scripts/example.sh
git commit -qm "chore: add script without work item docs"
HEAD_SHA="$(git rev-parse HEAD)"

set +e
bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -eq 0 ]]; then
  echo "ERROR: expected failure when work item docs are missing for script change"
  exit 1
fi
echo "OK: fails when script changes lack a work item folder"

# Case 6: screenshot references must use raw URLs plus metadata
git checkout -q "$BASE_SHA"
mkdir -p docs/features/999-screenshot-validation
printf '%s' "$TEST_PNG_BASE64" | base64 -d > docs/features/999-screenshot-validation/example.png
cat > docs/features/999-screenshot-validation/release-notes.md <<'EOF'
# Release Notes

![Screenshot](./example.png)
EOF
cat > docs/features/999-screenshot-validation/work-protocol.md <<'EOF'
# Work Protocol

## Agent Work Log

### Release Manager
- **Date:** 2026-05-16
EOF
git add docs/features/999-screenshot-validation/example.png \
  docs/features/999-screenshot-validation/release-notes.md \
  docs/features/999-screenshot-validation/work-protocol.md
git commit -qm "docs: add invalid screenshot reference"
HEAD_SHA="$(git rev-parse HEAD)"

set +e
bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -eq 0 ]]; then
  echo "ERROR: expected failure for invalid screenshot reference"
  exit 1
fi
echo "OK: fails when screenshot references are missing raw URLs/metadata"

# Case 7: valid screenshot metadata should pass
git checkout -q "$BASE_SHA"
mkdir -p docs/features/999-valid-screenshot
printf '%s' "$TEST_PNG_BASE64" | base64 -d > docs/features/999-valid-screenshot/example.png
cat > docs/features/999-valid-screenshot/release-notes.md <<'EOF'
# Release Notes

<!-- release-screenshot: selector="summary:has-text('azurerm_resource_group.example')"; focus="Shows the changed summary line" -->
![Screenshot](https://raw.githubusercontent.com/oocx/tfplan2md/v1.0.0/docs/features/999-valid-screenshot/example.png)
EOF
cat > docs/features/999-valid-screenshot/work-protocol.md <<'EOF'
# Work Protocol

## Agent Work Log

### Release Manager
- **Date:** 2026-05-16
EOF
git add docs/features/999-valid-screenshot/example.png \
  docs/features/999-valid-screenshot/release-notes.md \
  docs/features/999-valid-screenshot/work-protocol.md
git commit -qm "docs: add valid screenshot reference"
HEAD_SHA="$(git rev-parse HEAD)"

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: passes when screenshot metadata and raw URL are valid"

# Case 8: malformed screenshot metadata should fail
git checkout -q "$BASE_SHA"
mkdir -p docs/features/999-malformed-screenshot
printf '%s' "$TEST_PNG_BASE64" | base64 -d > docs/features/999-malformed-screenshot/example.png
cat > docs/features/999-malformed-screenshot/release-notes.md <<'EOF'
# Release Notes

<!-- release-screenshot: selector="summary:has-text('azurerm_resource_group.example')"; focus="" -->
![Screenshot](https://raw.githubusercontent.com/oocx/tfplan2md/v1.0.0/docs/features/999-malformed-screenshot/example.png)
EOF
cat > docs/features/999-malformed-screenshot/work-protocol.md <<'EOF'
# Work Protocol

## Agent Work Log

### Release Manager
- **Date:** 2026-05-16
EOF
git add docs/features/999-malformed-screenshot/example.png \
  docs/features/999-malformed-screenshot/release-notes.md \
  docs/features/999-malformed-screenshot/work-protocol.md
git commit -qm "docs: add malformed screenshot metadata"
HEAD_SHA="$(git rev-parse HEAD)"

set +e
bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -eq 0 ]]; then
  echo "ERROR: expected failure for malformed screenshot metadata"
  exit 1
fi
echo "OK: fails when screenshot metadata is malformed"

echo "All tests passed."
