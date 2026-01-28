#!/usr/bin/env bash
# UAT Helper Script for GitHub
# Usage: scripts/uat-github.sh <action> [args]
#
# Actions:
#   create <file> <test-description>   - Create a UAT PR with initial comment from <file>
#                                         test-description: Detailed, resource-specific validation instructions
#   comment <pr-number> <file> - Add a comment to PR from <file>
#   poll <pr-number> [--quiet] [--json] - Poll for new feedback and check for approval
#                                 Approval is determined by PR labels:
#                                   - uat-approved => APPROVED
#                                   - uat-rejected => REJECTED
#                                 --quiet: Output only STATUS: APPROVED|WAITING|REJECTED|CLOSED|ERROR (agent-friendly)
#                                 --json: Output raw PR JSON (comments + reviews + labels) for agent analysis
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
UAT_GITHUB_SUBMODULE_PATH="${UAT_GITHUB_SUBMODULE_PATH:-uat-repos/github}"

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

ensure_git_submodule() {
    local path="$1"
    if [[ -e "$path/.git" ]]; then
        return 0
    fi

    log_info "Initializing git submodule: $path"
    git submodule update --init --recursive "$path" >/dev/null
}


cmd_create() {
    local file="${1:-}"
    local test_description="${2:-}"
    local branch_override=""
    local force="${UAT_FORCE:-false}"

    if [[ $# -lt 2 ]]; then
        log_error "Usage: $0 create <file> <test-description> [--branch <name>]"
        exit 1
    fi

    shift 2
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --branch)
                branch_override="${2:-}"
                shift 2
                ;;
            *)
                log_error "Unknown argument: $1"
                exit 1
                ;;
        esac
    done
    
    # Validate and potentially set default artifact (platform-aware)
    file="$(validate_artifact github "$file" "$force")"
    
    if [[ -z "$test_description" ]]; then
        log_error "Test description is required. Usage: $0 create <file> <test-description>"
        log_error "Example: $0 create artifacts/report.md 'In module.security.azurerm_key_vault_secret.audit_policy, verify key_vault_id displays as \"Key Vault \\\`kv-name\\\` in resource group \\\`rg-name\\\`\" instead of full /subscriptions/ path'"
        exit 1
    fi
    
    local branch
    if [[ -n "$branch_override" ]]; then
        branch="$branch_override"
    else
        branch=$(git branch --show-current)
    fi
    local title="UAT: $(basename "$file" .md)"

    local body
    body=$(cat <<'EOF'
## Problem
Validate markdown rendering in real PR UIs.

## Change
Create a UAT PR and post the test markdown as PR comments.

## Test Instructions

**Feature-Specific Validation:**
__TEST_DESCRIPTION__

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
    - Apply label **`uat-approved`** if rendering is correct
    - Apply label **`uat-rejected`** (or comment with details) if rendering problems are found

## Verification
Maintainer visually reviews the PR comment rendering in GitHub.
EOF
)

    body="${body/__TEST_DESCRIPTION__/$test_description}"
    
    log_info "Using artifact: $file"

    # The UAT repo is intentionally separate from this repo.
    # We use a dedicated git submodule checkout to create a lightweight branch for the PR.
    ensure_git_submodule "$UAT_GITHUB_SUBMODULE_PATH"

    log_info "Preparing UAT branch '$branch' in submodule repo ($UAT_GITHUB_SUBMODULE_PATH)..."
    git -C "$UAT_GITHUB_SUBMODULE_PATH" fetch origin main >/dev/null 2>&1 || true

    local timestamp
    timestamp="$(date -u +%Y%m%d%H%M%S)"

    git -C "$UAT_GITHUB_SUBMODULE_PATH" checkout -B "$branch" origin/main >/dev/null 2>&1
    mkdir -p "$UAT_GITHUB_SUBMODULE_PATH/.uat"
    {
        echo "UAT marker commit (GitHub)"
        echo "Timestamp: $timestamp"
        echo "Artifact: $file"
    } > "$UAT_GITHUB_SUBMODULE_PATH/.uat/uat-run.txt"
    git -C "$UAT_GITHUB_SUBMODULE_PATH" add .uat/uat-run.txt
    git -C "$UAT_GITHUB_SUBMODULE_PATH" -c user.name="tfplan2md uat" -c user.email="uat@tfplan2md.invalid" commit -m "chore(uat): marker ${timestamp}" >/dev/null 2>&1 || true
    git -C "$UAT_GITHUB_SUBMODULE_PATH" push origin HEAD:"$branch" --force >/dev/null 2>&1
    
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
    local json=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --quiet)
                quiet=true
                shift
                ;;
            --json)
                json=true
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
        log_error "Usage: $0 poll <pr-number> [--quiet] [--json]"
        exit 1
    fi

    # JSON output is intended for machine/agent consumption; keep it JSON-only.
    if [[ "$json" == "true" ]]; then
        quiet=false
    fi
    
    if [[ "$quiet" != "true" && "$json" != "true" ]]; then
        log_info "Polling PR status for #$pr_number in $UAT_GITHUB_REPO..."
    fi

    # Read full PR JSON so agents can analyze raw comments and review state.
    # We intentionally do NOT attempt brittle keyword matching on comment text.
    local pr_json
    pr_json=$(PAGER=cat gh pr view "$pr_number" \
        --repo "$UAT_GITHUB_REPO" \
        --json state,url,title,comments,reviews,labels \
        2>/dev/null || echo "")
    if [[ -z "$pr_json" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: ERROR"
        else
            log_error "Failed to read PR JSON. Ensure gh is authenticated and the PR exists."
        fi
        return 2
    fi

    local pr_state
    pr_state=$(echo "$pr_json" | jq -r '.state // ""' 2>/dev/null || echo "")
    local pr_url
    pr_url=$(echo "$pr_json" | jq -r '.url // ""' 2>/dev/null || echo "")

    # GitHub reviews are often unavailable when the PR author is the same account
    # (e.g., agent pushes using the Maintainer account). Therefore, use labels as
    # the platform-native approval/rejection signal.
    local has_label_approved
    has_label_approved=$(echo "$pr_json" | jq -r '[.labels[]?.name] | any(. == "uat-approved")' 2>/dev/null || echo "false")

    local has_label_rejected
    has_label_rejected=$(echo "$pr_json" | jq -r '[.labels[]?.name] | any(. == "uat-rejected")' 2>/dev/null || echo "false")

    # Still compute latest review state (best-effort) for informational output.
    local latest_review_state
    latest_review_state=$(echo "$pr_json" | jq -r '[.reviews[]? | select(.submittedAt != null)] | sort_by(.submittedAt) | last | .state // ""' 2>/dev/null || echo "")

    # If requested, emit raw JSON (full comment/review bodies) for agent analysis.
    if [[ "$json" == "true" ]]; then
        echo "$pr_json"
    fi

    if [[ "$pr_state" == "MERGED" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: APPROVED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${GREEN}✓ PR MERGED${NC}"
            echo "PR has been merged."
            [[ -n "$pr_url" ]] && echo "URL: $pr_url"
        fi
        return 0
    fi

    if [[ "$pr_state" == "CLOSED" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: CLOSED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${RED}✗ PR CLOSED${NC}"
            echo "PR has been closed."
            [[ -n "$pr_url" ]] && echo "URL: $pr_url"
        fi
        return 2
    fi

    if [[ "$has_label_rejected" == "true" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: REJECTED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${RED}✗ REJECTED${NC}"
            echo "Label 'uat-rejected' is present."
            [[ -n "$pr_url" ]] && echo "URL: $pr_url"
        fi
        return 2
    fi

    if [[ "$has_label_approved" == "true" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: APPROVED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${GREEN}✓ APPROVED${NC}"
            echo "Label 'uat-approved' is present."
            [[ -n "$pr_url" ]] && echo "URL: $pr_url"
        fi
        return 0
    fi

    if [[ "$quiet" == "true" ]]; then
        echo "STATUS: WAITING"
        return 1
    fi

    if [[ "$json" != "true" ]]; then
        echo -e "${YELLOW}⏳ AWAITING REVIEW${NC}"
        [[ -n "$pr_url" ]] && echo "URL: $pr_url"
        echo "Approval labels: uat-approved=$has_label_approved, uat-rejected=$has_label_rejected"
        echo "Latest review state (best-effort): ${latest_review_state:-none}"
        echo ""
        echo "=== Non-agent comments (raw) ==="
        echo "$pr_json" | jq -r '.comments[]?
            | select((.body // "") | contains("This comment was generated by an AI agent.") | not)
            | "[\(.author.login // "unknown")]:\n\(.body // "")\n---"' || true
        echo ""
        echo "=== Reviews (raw) ==="
        echo "$pr_json" | jq -r '.reviews[]? | "[\(.author.login // "unknown")] \(.state // ""): \(.body // "")"' || true
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
    # Note: Do NOT use --delete-branch here. Branch deletion can switch the UAT repo
    # to main unexpectedly. When using scripts/uat-run.sh, branch deletion is handled
    # centrally via the UAT submodules during --cleanup-last.
    PAGER=cat gh pr close "$pr_number" --repo "$UAT_GITHUB_REPO" || log_warn "PR may already be closed"

    log_info "Cleanup complete."
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
        echo "  poll <pr-number> [--quiet] [--json] - Poll for new feedback and check for approval"
        echo "                                       --quiet: Output only STATUS: APPROVED|WAITING|REJECTED|CLOSED|ERROR"
        echo "                                       --json: Output raw PR JSON (comments + reviews + labels) for agent analysis"
        echo "  cleanup <pr-number>               - Close the PR after UAT completion"
        exit 1
        ;;
esac
