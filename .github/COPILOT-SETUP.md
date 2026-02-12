# GitHub Copilot Coding Agent Configuration

This repository uses GitHub Copilot coding agents for automated development workflows. This document describes the required repository configuration.

## Required Secrets

GitHub Copilot coding agents require secrets to be configured in a special `copilot` environment for security and isolation.

### Setting Up the `copilot` Environment

1. Navigate to **Repository Settings > Environments**
2. Create or select the environment named `copilot`
3. Add the following secrets:

| Secret Name | Description | Required For |
|-------------|-------------|--------------|
| `GH_UAT_TOKEN` | GitHub Personal Access Token for UAT repository access | UAT Tester agent |
| `AZDO_UAT_TOKEN` | Azure DevOps Personal Access Token for UAT repository access | UAT Tester agent |

### Token Requirements

#### GH_UAT_TOKEN
- **Type**: GitHub Personal Access Token (classic or fine-grained)
- **Scopes Required**:
  - `repo` (full control of private repositories)
  - `workflow` (update GitHub Actions workflows)
- **Repository Access**: Must have write access to `oocx/tfplan2md-uat`

#### AZDO_UAT_TOKEN
- **Type**: Azure DevOps Personal Access Token
- **Scopes Required**:
  - `Code (Read & Write)` - Create and manage pull requests
  - `Pull Request Threads (Read & Write)` - Add comments to PRs
- **Organization Access**: Must have access to `https://dev.azure.com/oocx/test`

## How It Works

1. **Setup Workflow**: `.github/workflows/copilot-setup-steps.yml`
   - Runs before the coding agent starts working
   - Authenticates GitHub CLI using `GH_UAT_TOKEN`
   - Authenticates Azure DevOps CLI using `AZDO_UAT_TOKEN`
   - Sets up required tools (.NET, Node.js, etc.)

2. **Coding Agents**: `.github/agents/*-coding-agent.agent.md`
   - Execute in the pre-configured environment
   - Use authenticated CLI tools without needing token details
   - Focus on task execution rather than authentication

## Security Notes

- Secrets in the `copilot` environment are **isolated** from other GitHub Actions workflows
- Copilot agent PRs are treated as "untrusted" similar to external contributor PRs
- Tokens should follow the principle of least privilege (minimum required scopes)
- Regular secret rotation is recommended

## Troubleshooting

### Authentication Failures

If the UAT Tester agent reports authentication errors:

1. **Verify the `copilot` environment exists** in Repository Settings > Environments
2. **Check secrets are configured**: `GH_UAT_TOKEN` and `AZDO_UAT_TOKEN` must be present
3. **Validate token permissions**: Ensure tokens have the required scopes
4. **Check token expiration**: Tokens may need to be regenerated
5. **Review setup workflow logs**: Check `.github/workflows/copilot-setup-steps.yml` run logs

### Git Push Failures in UAT Scripts

If UAT scripts fail with "Failed to push branch" errors:

**For GitHub UAT (`uat-github.sh`):**
```bash
# Verify GitHub CLI is authenticated
gh auth status

# Should show:
# ✓ Logged in to github.com account <username>
```

**For Azure DevOps UAT (`uat-azdo.sh`):**
```bash
# Verify Azure DevOps token is set
echo $AZURE_DEVOPS_EXT_PAT

# Should show the token (not empty)

# Verify Azure DevOps configuration
az devops configure --list

# Should show organization and project defaults
```

**Common Issues:**
- **Token not set**: copilot-setup-steps.yml may have failed silently
- **Token expired**: Regenerate token in GitHub/Azure DevOps settings
- **Insufficient permissions**: Token needs write access to UAT repositories
- **Network issues**: Check if UAT repository URLs are accessible

### Testing the Setup

To test if the environment is properly configured:

1. Trigger the copilot-setup-steps workflow manually:
   - Go to **Actions > Copilot Setup Steps**
   - Click **Run workflow**
   - Review the logs for any authentication errors

2. Check authentication status in the workflow logs:
   - Look for "✓ GitHub CLI authenticated for UAT"
   - Look for "✓ Azure DevOps CLI authenticated for UAT"

## References

- [GitHub Docs: Customizing Copilot coding agent environment](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment)
- [GitHub Docs: Using secrets in GitHub Actions](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
- [GitHub Docs: Using environments for deployment](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
