#!/usr/bin/env bash
# Tests for scripts/hooks/validate-work-protocol.sh
#
# The hook is a preToolUse hook that denies report_progress when
# work-protocol.md is missing required agent log entries.
# Each test case invokes the hook script with a crafted stdin payload
# inside a temporary git repo with a work-protocol.md that is "ahead of
# the tracking branch" via a local commit.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
HOOK_SCRIPT="${REPO_ROOT}/scripts/hooks/validate-work-protocol.sh"
TEST_ROOT="${REPO_ROOT}/.tmp/validate-work-protocol-test-$$"

cleanup() { rm -rf "$TEST_ROOT"; }
trap cleanup EXIT

mkdir -p "$TEST_ROOT"
cd "$TEST_ROOT"

# ---------------------------------------------------------------------------
# Set up a bare "origin" repo and a local clone so @{u} tracking works.
# ---------------------------------------------------------------------------
ORIGIN="${TEST_ROOT}/origin.git"
git init -q --bare "$ORIGIN"

REPO="${TEST_ROOT}/repo"
git clone -q "$ORIGIN" "$REPO"
cd "$REPO"
git config user.email "test@example.com"
git config user.name "Test"

# Create an initial commit on main so origin/HEAD exists.
mkdir -p docs
touch docs/.gitkeep
git add .
git commit -qm "chore: initial"
# Push to the bare (empty) repo, creating the remote main branch.
git push -q origin "HEAD:main"
git branch --set-upstream-to=origin/main main 2>/dev/null || true

# Helper: reset to a clean state from origin/main
reset_to_origin() {
  git checkout -q main
  git reset -q --hard origin/main
  git clean -qfdx
}

# Helper: invoke the hook with a report_progress payload; returns its exit code.
# Stdout (jq output) is captured for assertions.
run_hook() {
  echo '{"toolName":"report_progress"}' | bash "$HOOK_SCRIPT"
}

# Helper: verify the hook ALLOWS (exit 0, no JSON output)
assert_allow() {
  local label="$1"
  local output
  output=$(run_hook 2>/dev/null)
  if [[ -n "$output" ]]; then
    echo "❌ ERROR ($label): expected ALLOW but hook produced output: $output"
    exit 1
  fi
  echo "OK: $label"
}

# Helper: verify the hook DENIES (produces JSON with permissionDecision=deny)
assert_deny() {
  local label="$1"
  local output
  output=$(run_hook 2>/dev/null)
  local decision
  decision=$(echo "$output" | jq -r '.permissionDecision // empty' 2>/dev/null || true)
  if [[ "$decision" != "deny" ]]; then
    echo "❌ ERROR ($label): expected DENY but hook produced: $output"
    exit 1
  fi
  echo "OK: $label"
}

# ---------------------------------------------------------------------------
# Case 1: non-report_progress tool — always allowed
# ---------------------------------------------------------------------------
reset_to_origin
output=$(echo '{"toolName":"bash"}' | bash "$HOOK_SCRIPT" 2>/dev/null)
if [[ -n "$output" ]]; then
  echo "❌ ERROR: non-report_progress tool should always be allowed"
  exit 1
fi
echo "OK: non-report_progress tool is allowed"

# ---------------------------------------------------------------------------
# Case 2: no work-protocol.md in the pending commits — allow
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/features/001-no-protocol
echo "# spec" > docs/features/001-no-protocol/specification.md
git add .
git commit -qm "docs: add spec"
assert_allow "no work-protocol.md in pending commits — allowed"

# ---------------------------------------------------------------------------
# Case 3: Feature work-protocol.md — trigger agent (Code Reviewer) NOT yet
#          present — early-stage push — allow
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/features/002-early-stage
cat > docs/features/002-early-stage/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Requirements Engineer
- Date: 2026-01-01
- Summary: Created spec.
EOF
git add .
git commit -qm "docs: requirements"
assert_allow "feature work-protocol without Code Reviewer — early-stage push allowed"

# ---------------------------------------------------------------------------
# Case 4: Feature work-protocol.md — Code Reviewer present, Release Manager
#          missing — DENY
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/features/003-missing-rm
cat > docs/features/003-missing-rm/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Requirements Engineer
- Date: 2026-01-01
- Summary: Created spec.

### Architect
- Date: 2026-01-01
- Summary: ADR.

### Quality Engineer
- Date: 2026-01-01
- Summary: Test plan.

### Task Planner
- Date: 2026-01-01
- Summary: Tasks.

### Developer
- Date: 2026-01-01
- Summary: Implemented.

### Technical Writer
- Date: 2026-01-01
- Summary: Docs updated.

### Code Reviewer
- Date: 2026-01-01
- Summary: Approved.
EOF
git add .
git commit -qm "docs: through code review"
assert_deny "feature work-protocol with Code Reviewer but no Release Manager — denied"

# ---------------------------------------------------------------------------
# Case 5: Feature work-protocol.md — all required agents present — allow
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/features/004-complete
cat > docs/features/004-complete/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Requirements Engineer
- Date: 2026-01-01
- Summary: done.

### Architect
- Date: 2026-01-01
- Summary: done.

### Quality Engineer
- Date: 2026-01-01
- Summary: done.

### Task Planner
- Date: 2026-01-01
- Summary: done.

### Developer
- Date: 2026-01-01
- Summary: done.

### Technical Writer
- Date: 2026-01-01
- Summary: done.

### Code Reviewer
- Date: 2026-01-01
- Summary: done.

### Release Manager
- Date: 2026-01-01
- Summary: done.
EOF
git add .
git commit -qm "docs: complete feature"
assert_allow "complete feature work-protocol — allowed"

# ---------------------------------------------------------------------------
# Case 6: Bug fix — Code Reviewer present, Release Manager missing — DENY
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/issues/005-missing-rm
cat > docs/issues/005-missing-rm/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Issue Analyst
- Date: 2026-01-01
- Summary: Investigated.

### Developer
- Date: 2026-01-01
- Summary: Fixed.

### Technical Writer
- Date: 2026-01-01
- Summary: Docs.

### Code Reviewer
- Date: 2026-01-01
- Summary: Approved.
EOF
git add .
git commit -qm "docs: bug fix through code review"
assert_deny "bug-fix work-protocol with Code Reviewer but no Release Manager — denied"

# ---------------------------------------------------------------------------
# Case 7: Bug fix — all agents present — allow
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/issues/006-complete
cat > docs/issues/006-complete/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Issue Analyst
- Date: 2026-01-01
- Summary: done.

### Developer
- Date: 2026-01-01
- Summary: done.

### Technical Writer
- Date: 2026-01-01
- Summary: done.

### Code Reviewer
- Date: 2026-01-01
- Summary: done.

### Release Manager
- Date: 2026-01-01
- Summary: done.
EOF
git add .
git commit -qm "docs: complete bug fix"
assert_allow "complete bug-fix work-protocol — allowed"

# ---------------------------------------------------------------------------
# Case 8: Workflow — Workflow Engineer present, Release Manager missing — DENY
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/workflow/007-missing-rm
cat > docs/workflow/007-missing-rm/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Workflow Engineer
- Date: 2026-01-01
- Summary: Updated agents.
EOF
git add .
git commit -qm "workflow: engineer done"
assert_deny "workflow work-protocol with Workflow Engineer but no Release Manager — denied"

# ---------------------------------------------------------------------------
# Case 9: Workflow — both agents present — allow
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/workflow/008-complete
cat > docs/workflow/008-complete/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Workflow Engineer
- Date: 2026-01-01
- Summary: done.

### Release Manager
- Date: 2026-01-01
- Summary: done.
EOF
git add .
git commit -qm "workflow: complete"
assert_allow "complete workflow work-protocol — allowed"

# ---------------------------------------------------------------------------
# Case 10: Feature — Code Reviewer present but missing earlier required agent
#           (e.g. Architect) — DENY (all required agents checked)
# ---------------------------------------------------------------------------
reset_to_origin
mkdir -p docs/features/009-missing-architect
cat > docs/features/009-missing-architect/work-protocol.md <<'EOF'
# Work Protocol
## Agent Work Log

### Requirements Engineer
- Date: 2026-01-01
- Summary: done.

### Quality Engineer
- Date: 2026-01-01
- Summary: done.

### Task Planner
- Date: 2026-01-01
- Summary: done.

### Developer
- Date: 2026-01-01
- Summary: done.

### Technical Writer
- Date: 2026-01-01
- Summary: done.

### Code Reviewer
- Date: 2026-01-01
- Summary: done.

### Release Manager
- Date: 2026-01-01
- Summary: done.
EOF
git add .
git commit -qm "docs: feature missing architect"
# Architect is missing — should deny
output=$(run_hook 2>/dev/null)
decision=$(echo "$output" | jq -r '.permissionDecision // empty' 2>/dev/null || true)
if [[ "$decision" != "deny" ]]; then
  echo "❌ ERROR: feature missing Architect — expected DENY but got: $output"
  exit 1
fi
reason=$(echo "$output" | jq -r '.permissionDecisionReason // empty' 2>/dev/null || true)
if ! echo "$reason" | grep -q "Architect"; then
  echo "❌ ERROR: denial message should mention 'Architect', got: $reason"
  exit 1
fi
echo "OK: feature missing Architect — denied with correct reason"

echo "All tests passed."
