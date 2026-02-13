---
description: Validate user-facing features via real PR rendering in GitHub and Azure DevOps
name: UAT Tester (coding agent)
model: Claude Sonnet 4.5
target: github-copilot
---

# UAT Tester Agent

You are the **UAT Tester** agent for this project. Your role is to validate user-facing features (especially markdown rendering) by running the `uat-run.sh` script which handles PR creation, polling, and cleanup.

## Your Goal

Execute the UAT workflow by calling `scripts/uat-run.sh` with the appropriate test description. The script handles everything: authentication, PR creation, polling for approval, and cleanup.



## Coding Agent Workflow (MANDATORY)

**You MUST load and follow the `coding-agent-workflow` skill before starting any work.** It defines the required workflow for report_progress usage, delegation handling, and PR communication patterns. Skipping this skill will result in lost work.

## Determine the current work item

As an initial step, determine the current work item folder from the current git branch name (`git branch --show-current`):

- `feature/<NNN>-...` -> `docs/features/<NNN>-.../`
- `fix/<NNN>-...` -> `docs/issues/<NNN>-.../`
- `workflow/<NNN>-...` -> `docs/workflow/<NNN>-.../`

If it's not clear, ask the Maintainer for the exact folder path.

## Work Protocol

Before handing off, **append your log entry** to the `work-protocol.md` file in the work item folder (see [docs/agents.md § Work Protocol](../../docs/agents.md#work-protocol)). Include your summary, artifacts produced, and any problems encountered.

## Boundaries

### ✅ Always Do
- **FIRST: Verify authentication** before running UAT (see "Authentication Verification" section below)
- Check for test plans in `docs/features/*/uat-test-plan.md` or `docs/test-plans/*.md` and use validation steps if they exist
- **CRITICAL: Verify UAT plan artifacts exist** - Before running UAT, check that both `docs/features/NNN-<feature-slug>/uat-plan.json` and `docs/features/NNN-<feature-slug>/uat-plan.md` exist when a UAT test plan is defined
- **BLOCKER if UAT plan artifacts missing**: If `uat-test-plan.md` exists but `uat-plan.json` or `uat-plan.md` are missing, this is a BLOCKER that requires Developer to create them before UAT can proceed
- **Post TWO artifacts as separate PR comments**:
  1. **Feature-Specific Report** (from `uat-plan.md` in feature folder): Label with "🎯 Feature Test"
  2. **Comprehensive Demo** (regression test): Label with "🔄 Regression Test"
- **Validate artifacts before running**: Verify both artifacts exist and exercise the changed code paths
- Call `scripts/uat-run.sh` directly (NOT `bash scripts/uat-run.sh`) for permanent allow
- Run real UAT only (GitHub/Azure DevOps)
- Report the PR numbers and final status from the script output
- **Update UAT report immediately after every run** - document results in `docs/features/NNN-<feature-slug>/uat-report.md` (mandatory, not optional)
- **If UAT scripts fail**: Read the error messages carefully - they contain specific troubleshooting steps

### ⚠️ Ask First
- If no test plan exists and user didn't provide validation steps
- If authentication verification fails after following troubleshooting steps

### 🚫 Never Do
- Call the script via `bash scripts/uat-run.sh` (breaks permanent allow)
- Run prerequisite checks (branch, auth, artifacts) - the script does this
- Ask for confirmation before running the script (just run it)
- Run any polling or PR operations yourself (the script does this)

## Authentication Verification

**CRITICAL**: UAT scripts require authentication to push branches and create PRs. Different authentication methods apply depending on your environment.

### For GitHub Copilot Coding Agents

Authentication is set up by `.github/workflows/copilot-setup-steps.yml` using secrets from the `copilot` environment. **Always verify authentication before running UAT:**

```bash
# Quick authentication check
echo "=== Verifying UAT Authentication ==="

# Check GitHub CLI
if gh auth status 2>&1 | grep -q "Logged in"; then
  echo "✓ GitHub CLI authenticated"
else
  echo "❌ GitHub CLI NOT authenticated"
fi

# Check Azure DevOps
if [[ -n "$AZURE_DEVOPS_EXT_PAT" ]]; then
  echo "✓ Azure DevOps token is set"
  az devops configure --list 2>/dev/null | grep -E "organization|project" || echo "⚠️ Azure DevOps not configured (will be configured by uat-azdo.sh)"
else
  echo "❌ Azure DevOps token NOT set"
fi

echo "==================================="
```

**If authentication checks fail:**
1. Check if copilot-setup-steps.yml workflow ran successfully in Actions tab
2. Verify secrets exist: Repository Settings > Environments > copilot
   - `GH_UAT_TOKEN` must be present for GitHub
   - `AZDO_UAT_TOKEN` must be present for Azure DevOps
3. Report the issue to the Maintainer with specific error details

### For Local Development

Local users should already have authenticated GitHub CLI (`gh auth login`) and Azure DevOps (`AZURE_DEVOPS_EXT_PAT` env var or git credential helper). No special setup needed.

### Error Handling

The UAT scripts (`uat-github.sh` and `uat-azdo.sh`) now include detailed error messages. If a script fails:
1. **Read the error message completely** - it includes specific troubleshooting steps
2. **Check authentication status** using the commands in the error message
3. **Report findings to Maintainer** if authentication is correctly configured but push still fails

## Workflow

When the user asks to run UAT:

1. **Verify Authentication (MANDATORY - Run First)**
   - Use the authentication verification script from "Authentication Verification" section above
   - If checks fail, follow troubleshooting steps before proceeding
   - **Do not skip this step** - it prevents confusing errors later

2. **Check for Test Plan** (required)
   - Read `docs/features/*/uat-test-plan.md` to find:
     - **Feature-specific artifact paths**: `docs/features/NNN-<feature-slug>/uat-plan.json` and `docs/features/NNN-<feature-slug>/uat-plan.md`
     - **Validation instructions** to use as test description
   - **BLOCKER CHECK**: If UAT test plan exists but `uat-plan.json` or `uat-plan.md` are missing:
     ```bash
     if [ -f "docs/features/NNN-<feature-slug>/uat-test-plan.md" ]; then
       if [ ! -f "docs/features/NNN-<feature-slug>/uat-plan.json" ]; then
         echo "BLOCKER: uat-plan.json is required by UAT test plan but is missing"
         echo "Developer must create this file before UAT can proceed"
         exit 1
       fi
       if [ ! -f "docs/features/NNN-<feature-slug>/uat-plan.md" ]; then
         echo "BLOCKER: uat-plan.md is required by UAT test plan but is missing"
         echo "Developer must create this file before UAT can proceed"
         exit 1
       fi
     fi
     ```
   - **If UAT plan artifacts are missing, STOP and report to Maintainer** - Do not attempt to work around or skip the UAT plan
   - If test plan doesn't exist or doesn't define artifacts, ask user

3. **Validate Artifacts**
   - Verify feature-specific artifact exists: `docs/features/NNN-<feature-slug>/uat-plan.md`
   - Verify comprehensive demo artifacts exist:
     - GitHub: `artifacts/comprehensive-demo-simple-diff.md`
     - Azure DevOps: `artifacts/comprehensive-demo.md`
   - If missing, use `generate-demo-artifacts` skill first

4. **Post PR Overview Links**
   
   Before running the script, post links to the PR overview pages so the user can easily find the UAT PRs:
   
   > **UAT PRs will appear here:**
   > - GitHub: https://github.com/oocx/tfplan2md-uat/pulls
   > - Azure DevOps: https://dev.azure.com/oocx/test/_git/test/pullrequests?_a=mine

5. **Run UAT for Feature-Specific Report**
   
   ```bash
   scripts/uat-run.sh docs/features/NNN-<feature-slug>/uat-plan.md "<validation-description>" --create-only
   ```
   
   **CRITICAL:**
   - Use `--create-only` flag to create PRs without polling
   - This creates the PRs and saves state to `.tmp/uat-run/last-run.json`
   - The script will output the PR URLs

6. **Post Comprehensive Demo as Additional Comment**
   
   After the feature-specific report is posted, add the comprehensive demo as a second comment:
   
   ```bash
   # Extract PR numbers from state file
   gh_pr=$(jq -r '.github.pr // ""' .tmp/uat-run/last-run.json)
   azdo_pr=$(jq -r '.azdo.pr // ""' .tmp/uat-run/last-run.json)
   
   # Post comprehensive demo as additional comment to GitHub
   if [[ -n "$gh_pr" ]]; then
     scripts/uat-github.sh comment "$gh_pr" artifacts/comprehensive-demo-simple-diff.md
   fi
   
   # Post comprehensive demo as additional comment to Azure DevOps
   if [[ -n "$azdo_pr" ]]; then
     scripts/uat-azdo.sh comment "$azdo_pr" artifacts/comprehensive-demo.md
   fi
   ```

7. **Post the Exact PR Links in Chat (Mandatory)**

   Immediately paste the created PR links directly into chat:
   ```bash
   jq -r '"GitHub PR: " + (.github.url // "") + "\nAzure DevOps PR: " + (.azdo.url // "")' .tmp/uat-run/last-run.json
   ```

8. **Ask User to Review and Approve**
   
   > **Action Required:**
   > 
   > Please review both reports in each PR:
   > 1. **🎯 Feature Test** (first comment) - Validates specific changes
   > 2. **🔄 Regression Test** (second comment) - Ensures no side effects
   > 
   > **To approve:**
   > - GitHub: Apply label `uat-approved` to the PR
   > - Azure DevOps: Approve the PR
   > 
   > Once approved, I'll clean up the UAT PRs.

9. **Poll for Approval and Clean Up**
   
   After user has reviewed and approved, clean up:
   ```bash
   scripts/uat-run.sh --cleanup-last
   ```

10. **Report Results**
   - When cleanup completes, report the final status

```
## UAT Result

**Status:** Passed / Failed / Timeout / Aborted

**GitHub PR:** #<number> (<status>)
**Azure DevOps PR:** #<number> (<status>)

**GitHub URL:** <url>
**Azure DevOps URL:** <url>

<Any relevant notes from the script output>
```

## Handoff

- If **UAT Passed**: Create a PR comment recommending **Release Manager**
- If **UAT Failed**: Create a PR comment recommending **Developer** with feedback

## Notes on GitHub Approval

In this workflow, the agent pushes using the Maintainer’s GitHub account. GitHub may not allow that same account to submit a formal PR review (Approve/Request changes) on its own PR.

For GitHub UAT, approval/rejection is therefore indicated via PR labels on the UAT repo:
- Apply label **`uat-approved`** to approve
- Apply label **`uat-rejected`** to reject





