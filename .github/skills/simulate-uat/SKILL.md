---
name: simulate-uat
description: Simulate the UAT workflow (create PR, comment, poll) on GitHub or Azure DevOps using a minimal test artifact and simulated fixes.
compatibility: Requires git. GitHub simulation uses repo scripts which require GitHub CLI (gh) authenticated. Azure DevOps simulation requires Azure CLI (az) + azure-devops extension. Network access required.
---

# Simulate UAT

## Purpose
Test the full UAT workflow (PR creation, commenting, polling, feedback handling) using a minimal test artifact instead of the real comprehensive demo. This is useful for:
- Testing changes to UAT scripts
- Validating the PR creation and monitoring flow
- Training the UAT agent on feedback handling
- Debugging platform-specific rendering issues

The simulation creates **real PRs** but uses a **minimal test report** and responds to feedback with **simulated fixes** (no actual code changes).

## Hard Rules
### Must
- Clearly label all PRs and comments as `[SIMULATION]`.
- Use the simulation test artifact: `artifacts/uat-simulation.md`.
- Create real PRs on GitHub/Azure DevOps to test the actual workflow.
- Respond to feedback with a simulated "fixed" report (append "[SIMULATED FIX]" section).
- Clean up all simulation PRs after testing.

### Must Not
- Use the real comprehensive demo artifact.
- Make actual code or template changes based on feedback.
- Hand off to Developer agent for fixes.
- Leave simulation PRs open after testing.

## Artifacts

### Minimal Test Artifact
The simulation uses `artifacts/uat-simulation.md`, a small test report that exercises key rendering features without the complexity of the full demo.

If this file doesn't exist, create it with this content:
```markdown
# [SIMULATION] UAT Test Report

This is a **simulated** UAT report for testing the PR workflow.

## Test Table

| Resource | Action | Status |
|----------|--------|--------|
| `azurerm_resource_group.test` | create | ✓ |
| `azurerm_storage_account.test` | update | ⚠️ |

## Test Code Block

\`\`\`hcl
resource "azurerm_resource_group" "test" {
  name     = "rg-test"
  location = "westeurope"
}
\`\`\`

## Test Inline Formatting

- Bold: **bold text**
- Code: `inline code`
- Diff: `-old` / `+new`

---
*This is a simulation artifact. Do not use for real UAT.*
```

### Simulated Fix Response
When feedback is received, respond with this template (do NOT fix actual code):
```markdown
# [SIMULATED FIX] Response to Feedback

## Original Feedback
> {paste the feedback here}

## Simulated Resolution
This is a **simulated fix response**. In a real UAT:
1. The UAT Tester would hand off to Developer
2. Developer would fix the issue
3. A new artifact would be generated
4. UAT would re-post for validation

For this simulation, consider the feedback "addressed" and proceed with cleanup.

---
*Simulation complete. No actual changes were made.*
```

## Actions

### 1. Prepare Simulation Artifact
```bash
# Ensure minimal artifact exists
if [[ ! -f "artifacts/uat-simulation.md" ]]; then
  echo "Creating simulation UAT artifact..."
  cat > artifacts/uat-simulation.md <<'EOF'
# [SIMULATION] UAT Test Report

This is a **simulated** UAT report for testing the PR workflow.

## Test Table

| Resource | Action | Status |
|----------|--------|--------|
| `azurerm_resource_group.test` | create | ✓ |
| `azurerm_storage_account.test` | update | ⚠️ |

## Test Code Block

```hcl
resource "azurerm_resource_group" "test" {
  name     = "rg-test"
  location = "westeurope"
}
```

## Test Inline Formatting

- Bold: **bold text**
- Code: `inline code`
- Diff: `-old` / `+new`

---
*This is a simulation artifact. Do not use for real UAT.*
EOF
fi
```

### 2. Create Simulation Branch
```bash
original_branch=$(git branch --show-current)
timestamp=$(date -u +%Y%m%d%H%M%S)
uat_branch="${original_branch}-uat-sim-${timestamp}"

git checkout -b "$uat_branch"
git push -u origin HEAD
```

### 3. Run GitHub Simulation
```bash
# Create PR with simulation label
UAT_ALLOW_MINIMAL=1 scripts/uat-github.sh create artifacts/uat-simulation.md
# PR title will be: "UAT: uat-minimal"

# Poll for comments/feedback
scripts/uat-github.sh poll <pr-number>

# If feedback received:
# 1. Write a simulated fix response markdown file
# 2. Do NOT make actual changes
# 3. Continue polling or proceed to cleanup

# Example simulated fix response file
cat > /tmp/uat-simulated-fix.md <<'EOF'
# [SIMULATED FIX] Response to Feedback

## Original Feedback
> (paste the feedback here)

## Simulated Resolution
This is a **simulated fix response**. In a real UAT:
1. The UAT Tester would hand off to Developer
2. Developer would fix the issue
3. A new artifact would be generated
4. UAT would re-post for validation

For this simulation, consider the feedback "addressed" and proceed with cleanup.

---
*Simulation complete. No actual changes were made.*
EOF

# Post the simulated response
scripts/uat-github.sh comment <pr-number> /tmp/uat-simulated-fix.md

# Cleanup
scripts/uat-github.sh cleanup <pr-number>
```

### 4. Run Azure DevOps Simulation
```bash
scripts/uat-azdo.sh setup
UAT_ALLOW_MINIMAL=1 scripts/uat-azdo.sh create artifacts/uat-simulation.md
scripts/uat-azdo.sh poll <pr-id>
# Handle feedback with simulated response file
scripts/uat-azdo.sh comment <pr-id> /tmp/uat-simulated-fix.md
scripts/uat-azdo.sh cleanup <pr-id>
```

### 5. Return to Original Branch
```bash
git checkout "$original_branch"
git branch -D "$uat_branch"  # Delete local simulation branch
```

## Golden Example
```bash
$ scripts/uat-github.sh create artifacts/uat-simulation.md
[INFO] Pushing branch to GitHub...
[INFO] Creating PR...
PR created: #99
[INFO] Posted uat-simulation.md as comment on PR #99

$ scripts/uat-github.sh poll 99
[INFO] Checking PR #99 for new comments...
[INFO] New comment from maintainer:
  "The table header looks off - can you check alignment?"

# Respond with simulated fix (do NOT actually fix)
$ cat > /tmp/uat-simulated-fix.md <<'EOF'
# [SIMULATED FIX] Response to Feedback

## Original Feedback
> The table header looks off - can you check alignment?

## Simulated Resolution
This is a **simulated fix response**. In a real UAT, Developer would fix the table formatting.

---
*Simulation complete. No actual changes were made.*
EOF

$ scripts/uat-github.sh comment 99 /tmp/uat-simulated-fix.md

$ scripts/uat-github.sh cleanup 99
[INFO] Closing PR #99...
[INFO] Simulation UAT complete.
```

## When to Use

| Scenario | Use This Skill? |
|----------|-----------------|
| Testing UAT script changes | ✓ Yes |
| Training UAT agent on feedback flow | ✓ Yes |
| Debugging PR creation/monitoring | ✓ Yes |
| Testing platform-specific rendering | ✓ Yes (with minimal artifact) |
| Actual feature validation | ✗ No, use `run-uat` |
| Pre-release verification | ✗ No, use `run-uat` |
