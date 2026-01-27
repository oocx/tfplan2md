#!/usr/bin/env bash
# UAT Helper Script for GitHub
# Usage: scripts/uat-github.sh <action> [args]
#
# Actions:
#   create <file> <test-description>   - Create a UAT PR with initial comment from <file>
#                                         test-description: Detailed, resource-specific validation instructions
#   comment <pr-number> <file> - Add a comment to PR from <file>
#   poll <pr-number> [--quiet] - Poll for new comments and check for approval
#                                 --quiet: Output only STATUS: APPROVED|WAITING|CLOSED|ERROR (agent-friendly)
#   cleanup <pr-number> - Close the PR after UAT completion
#
# Example:
#   scripts/uat-github.sh create artifacts/report.md \
#     "In module.security.azurerm_key_vault_secret.audit_policy, verify key_vault_id displays as 'Key Vault \`kv-name\` in resource group \`rg-name\`' instead of full /subscriptions/ path"
#
#   scripts/uat-github.sh poll 123 --quiet
#
# Environment:
#   UAT_GITHUB_REPO - Target repository for UAT PRs (default: oocx/tfplan2md-uat)

set -euo pipefail

# Prevent interactive pagers from blocking automation
export GH_PAGER=cat
export GH_FORCE_TTY=false
export PAGER=cat

# UAT repository configuration
UAT_GITHUB_REPO="${UAT_GITHUB_REPO:-oocx/tfplan2md-uat}"
UAT_OWNER="$(echo "$UAT_GITHUB_REPO" | cut -d/ -f1)"
UAT_REPO="$(echo "$UAT_GITHUB_REPO" | cut -d/ -f2)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() { echo -e "${GREEN}[INFO]${NC} $*"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*" >&2; }

# Artifact validation implemented in shared helper
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=/dev/null
source "$script_dir/uat-helpers.sh"


cmd_create() {
    local file="${1:-}"
    local test_description="${2:-}"
    local simulate="${UAT_SIMULATE:-false}"
    local force="${UAT_FORCE:-false}"
    
    # Validate and potentially set default artifact (platform-aware)
    file="$(validate_artifact github "$file" "$simulate" "$force")"
    
    if [[ -z "$test_description" ]]; then
        log_error "Test description is required. Usage: $0 create <file> <test-description>"
        log_error "Example: $0 create artifacts/report.md 'In module.security.azurerm_key_vault_secret.audit_policy, verify key_vault_id displays as \"Key Vault \\\`kv-name\\\` in resource group \\\`rg-name\\\`\" instead of full /subscriptions/ path'"
        exit 1
    fi
    
    local branch
    branch=$(git branch --show-current)
    local title="UAT: $(basename "$file" .md)"
    
    local simulation_header=""
    if [[ "$simulate" == "true" ]]; then
        title="[SIMULATION] $title"
        simulation_header="> ⚠️ **SIMULATION MODE**\n> This is a test of the UAT process using standard artifacts. Reported issues are likely expected or already known.\n\n"
    fi

    local body
    body=$(cat <<EOF
${simulation_header}## Problem
Validate markdown rendering in real PR UIs.

## Change
Create a UAT PR and post the test markdown as PR comments.

## Test Instructions

**Feature-Specific Validation:**
${test_description}

**General Verification:**
1. **Read the test artifact** posted as the first comment below
2. **Verify markdown rendering**:
   - Tables render correctly with proper alignment
   - Code blocks display with syntax highlighting
   - Links are clickable and formatted properly
   - Nested lists and formatting work as expected
3. **Check for rendering issues**:
   - Escaped characters displaying incorrectly
   - Missing or malformed table borders
   - Broken links or incorrect formatting
4. **Approve or provide feedback**:
   - Comment "approved" or "lgtm" if rendering is correct
   - Report specific issues if rendering problems are found

## Verification
Maintainer visually reviews the PR comment rendering in GitHub.
EOF
)
    
    log_info "Using artifact: $file"
    
    # Ensure UAT remote exists
    if ! git remote get-url uat-github >/dev/null 2>&1; then
        log_info "Adding uat-github remote for $UAT_GITHUB_REPO..."
        git remote add uat-github "https://github.com/$UAT_GITHUB_REPO.git"
    fi
    
    log_info "Pushing branch to UAT repository ($UAT_GITHUB_REPO)..."
    git push -u uat-github HEAD --force
    
    log_info "Creating PR in $UAT_GITHUB_REPO..."
    local pr_url
    pr_url=$(PAGER=cat gh pr create \
        --repo "$UAT_GITHUB_REPO" \
        --title "$title" \
        --body "$body" \
        --base main \
        --head "$branch")
    
    local pr_number
    pr_number=$(echo "$pr_url" | grep -oE '[0-9]+$')
    
    log_info "PR created: #$pr_number ($pr_url)"
    
    # Add the markdown content as a comment
    log_info "Adding initial UAT content as comment..."
    cmd_comment "$pr_number" "$file"
    
    echo ""
    echo "========================================="
    echo "UAT PR Created: #$pr_number"
    echo "URL: $pr_url"
    echo "========================================="
    echo ""
    echo "Next steps:"
    echo "  1. Maintainer reviews the PR comment in GitHub"
    echo "  2. Poll for feedback: $0 poll $pr_number"
    echo "  3. After approval: $0 cleanup $pr_number"
}

cmd_comment() {
    local pr_number="${1:-}"
    local file="${2:-}"
    
    if [[ -z "$pr_number" || -z "$file" || ! -f "$file" ]]; then
        log_error "Usage: $0 comment <pr-number> <markdown-file>"
        exit 1
    fi
    
    # Prefix comment to distinguish agent-generated content from human comments
    local prefix="🤖 **Copilot Code Reviewer** — _This comment was generated by an AI agent._

---

"
    local content
    content=$(cat "$file")
    
    echo "${prefix}${content}" | PAGER=cat gh pr comment "$pr_number" --repo "$UAT_GITHUB_REPO" --body-file -
    log_info "Comment added to PR #$pr_number"
}

cmd_poll() {
    local pr_number=""
    local quiet=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --quiet)
                quiet=true
                shift
                ;;
            *)
                # First non-flag argument is the PR number
                if [[ -z "$pr_number" ]]; then
                    pr_number="$1"
                    shift
                else
                    log_error "Unknown argument: $1"
                    exit 1
                fi
                ;;
        esac
    done
    
    if [[ -z "$pr_number" ]]; then
        log_error "Usage: $0 poll <pr-number> [--quiet]"
        exit 1
    fi
    
    if [[ "$quiet" != "true" ]]; then
        log_info "Polling comments for PR #$pr_number in $UAT_GITHUB_REPO..."
    fi
    
    # Get PR state (structured)
    local pr_state
    pr_state=$(PAGER=cat gh pr view "$pr_number" --repo "$UAT_GITHUB_REPO" --json state -q '.state' 2>/dev/null || echo "")
    if [[ -z "$pr_state" ]]; then
        log_error "Failed to read PR state. Ensure gh is authenticated and the PR exists."
        return 2
    fi
    
    if [[ "$pr_state" == "CLOSED" || "$pr_state" == "MERGED" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: CLOSED"
        else
            echo -e "${GREEN}✓ PR CLOSED${NC}"
            echo "PR has been closed by Maintainer. UAT passed."
        fi
        return 0
    fi
    
    # Get comments (structured) and check for approval keywords in NON-agent comments.
    # Agent comments include the marker line: "This comment was generated by an AI agent."
    local approval_found
    approval_found=$(PAGER=cat gh pr view "$pr_number" --repo "$UAT_GITHUB_REPO" --json comments -q '.comments[]
            | select((.body // "") | contains("This comment was generated by an AI agent.") | not)
            | select((.body // "") | test("(?i)(approved|passed|accept|lgtm)"))
            | .body' 2>/dev/null || true)

    # Check for failure keywords in NON-agent comments.
    local failure_found
    failure_found=$(PAGER=cat gh pr view "$pr_number" --repo "$UAT_GITHUB_REPO" --json comments -q '.comments[]
            | select((.body // "") | contains("This comment was generated by an AI agent.") | not)
            | select((.body // "") | test("(?i)(fail|reject|regression|issue|bug|error)"))
            | .body' 2>/dev/null || true)

    if [[ "$quiet" != "true" ]]; then
        echo ""
        echo "=== Recent Comments (JSON) ==="
        # Show up to the last 3 comments (author + truncated body). Avoid parsing formatted text output.
        PAGER=cat gh pr view "$pr_number" --repo "$UAT_GITHUB_REPO" --json comments -q '(
                if (.comments | length) == 0 then
                    "(no comments)"
                else
                    .comments[-3:][]
                    | "[\(.author.login // "unknown")]: \((.body // "") | gsub("\r"; "") | .[0:200])"
                end
            )' 2>/dev/null || echo "(failed to load comments)"
        echo ""
    fi
    
    if [[ -n "$failure_found" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: FAILED"
        else
            echo -e "${RED}✗ FAILURE DETECTED IN COMMENTS${NC}"
            echo "Failure keyword found in comments (e.g. \"fail\", \"reject\", \"error\"). Stopping UAT."
        fi
        return 2
    fi

    if [[ -n "$approval_found" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: APPROVED"
        else
            echo -e "${GREEN}✓ APPROVAL DETECTED${NC}"
            echo "Approval keyword found in comments. UAT passed."
        fi
        return 0
    fi
    
    if [[ "$quiet" == "true" ]]; then
        echo "STATUS: WAITING"
    else
        echo -e "${YELLOW}⏳ AWAITING FEEDBACK${NC}"
        echo "No approval detected yet. Continue polling or check GitHub UI."
    fi
    return 1
}

cmd_cleanup() {
    local pr_number="${1:-}"
    
    if [[ -z "$pr_number" ]]; then
        log_error "Usage: $0 cleanup <pr-number>"
        exit 1
    fi
    
    log_info "Closing PR #$pr_number in $UAT_GITHUB_REPO..."
    # Note: Do NOT use --delete-branch here. Branch deletion causes immediate
    # checkout to main, which breaks subsequent commands that need scripts.
    # Delete branches manually after both cleanups complete.
    PAGER=cat gh pr close "$pr_number" --repo "$UAT_GITHUB_REPO" || log_warn "PR may already be closed"
    
    log_info "Cleanup complete. Remember to delete the remote branch manually."
}

# Main dispatch
action="${1:-}"
shift || true

case "$action" in
    create)  cmd_create "$@" ;;
    comment) cmd_comment "$@" ;;
    poll)    cmd_poll "$@" ;;
    cleanup) cmd_cleanup "$@" ;;
    *)
        echo "UAT Helper Script for GitHub"
        echo ""
        echo "Usage: $0 <action> [args]"
        echo ""
        echo "Actions:"
        echo "  create <file> <test-description>  - Create a UAT PR with initial comment from <file>"
        echo "  comment <pr-number> <file>        - Add a comment to PR from <file>"
        echo "  poll <pr-number> [--quiet]        - Poll for new comments and check for approval"
        echo "                                       --quiet: Output only STATUS: APPROVED|WAITING|CLOSED|ERROR"
        echo "  cleanup <pr-number>               - Close the PR after UAT completion"
        exit 1
        ;;
esac
