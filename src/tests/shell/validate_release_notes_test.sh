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

# Case 3: changed docs/workflow item with release-notes.md and work-protocol.md should pass
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

# Case 4: .github/ changes without a work item folder should pass
git checkout -q "$BASE_SHA"
mkdir -p .github/skills/example-skill
cat > .github/skills/example-skill/SKILL.md <<'EOF'
# Example Skill
EOF
git add .github/skills/example-skill/SKILL.md
git commit -qm "ci: add example skill"
HEAD_SHA="$(git rev-parse HEAD)"

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: .github/ changes do not require a work item folder"

# Case 5: scripts/ changes without a work item folder should pass (tooling-only, not shipped)
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

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: scripts/ changes do not require a work item folder"

# Case 5b: src/tests/shell/ changes without a work item folder should pass
git checkout -q "$BASE_SHA"
mkdir -p src/tests/shell
cat > src/tests/shell/example_test.sh <<'EOF'
#!/usr/bin/env bash
echo "example test"
EOF
chmod +x src/tests/shell/example_test.sh
git add src/tests/shell/example_test.sh
git commit -qm "test: add shell test without work item docs"
HEAD_SHA="$(git rev-parse HEAD)"

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: src/tests/shell/ changes do not require a work item folder"

# Case 5c: src/Dockerfile changes without a work item folder should pass
git checkout -q "$BASE_SHA"
mkdir -p src
cat > src/Dockerfile <<'EOF'
FROM alpine:3.20
EOF
git add src/Dockerfile
git commit -qm "build: modify dockerfile without work item docs"
HEAD_SHA="$(git rev-parse HEAD)"

bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null
echo "OK: src/Dockerfile changes do not require a work item folder"

# Case 6: src/ changes outside src/tests/shell/ and src/Dockerfile without a work item folder should fail
git checkout -q "$BASE_SHA"
mkdir -p src/SomeProject
cat > src/SomeProject/Example.cs <<'EOF'
// example
EOF
git add src/SomeProject/Example.cs
git commit -qm "feat: add code without work item docs"
HEAD_SHA="$(git rev-parse HEAD)"

set +e
bash "$SCRIPT_PATH" --base-ref "$BASE_SHA" --head-ref "$HEAD_SHA" >/dev/null 2>&1
rc=$?
set -e

if [[ $rc -eq 0 ]]; then
  echo "ERROR: expected failure when work item docs are missing for src/ change"
  exit 1
fi
echo "OK: src/ changes (outside tests/shell/) fail without a work item folder"

# Case 7: screenshot references must use raw URLs plus metadata
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
