#!/usr/bin/env bash
# UAT Helper Script for Azure DevOps
# Usage: scripts/uat-azdo.sh <action> [args]
#
# Actions:
#   setup           - Configure Azure DevOps defaults and verify authentication
#   create <file> <test-description>   - Create a UAT PR with initial comment from <file>
#                                         test-description: Detailed, resource-specific validation instructions
#   comment <pr-id> <file> - Add a comment to PR from <file>
#   poll <pr-id> [--quiet] [--json] - Poll for new feedback and check for approval
#                                   --quiet: Output only STATUS: APPROVED|WAITING|REJECTED|CLOSED|ERROR (agent-friendly)
#                                   --json: Output raw PR JSON (PR + threads) for agent analysis
#   cleanup <pr-id> - Abandon the PR after UAT completion
#
# Example:
#   scripts/uat-azdo.sh create artifacts/report.md \
#     "In azurerm_firewall_network_rule_collection.rules summary, verify attribute values use code blocks instead of bold. In azurerm_key_vault_secret.audit_policy large values block, verify value attribute shows inline-diff style."
#
# Environment:
#   AZDO_ORG        - Azure DevOps organization URL (default: https://dev.azure.com/oocx)
#   AZDO_PROJECT    - Azure DevOps project name (default: test)
#   AZDO_REPO       - Azure DevOps repository name (default: test)

set -euo pipefail

# Prevent interactive pagers from blocking automation
export AZURE_CORE_PAGER=cat
export PAGER=cat

AZDO_ORG="${AZDO_ORG:-https://dev.azure.com/oocx}"
AZDO_PROJECT="${AZDO_PROJECT:-test}"
AZDO_REPO="${AZDO_REPO:-test}"
AZDO_SUBMODULE_PATH="${AZDO_SUBMODULE_PATH:-uat-repos/azdo}"

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


cmd_setup() {
    log_info "Checking Azure CLI authentication..."
    if ! az account show >/dev/null 2>&1; then
        log_error "Not authenticated. Please run: az login"
        exit 1
    fi
    
    log_info "Configuring Azure DevOps defaults..."
    az devops configure --defaults organization="$AZDO_ORG" project="$AZDO_PROJECT"
    
    log_info "Verifying azure-devops extension..."
    if ! az extension show --name azure-devops >/dev/null 2>&1; then
        log_warn "Installing azure-devops extension..."
        az extension add --name azure-devops
    fi
    
    log_info "Setup complete. Organization: $AZDO_ORG, Project: $AZDO_PROJECT, Repo: $AZDO_REPO"
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
    file="$(validate_artifact azdo "$file" "$force")"

    if [[ -z "$test_description" ]]; then
        log_error "Test description is required. Usage: $0 create <file> <test-description>"
        log_error "Example: $0 create artifacts/report.md 'In azurerm_role_assignment.contributor, verify principal displays as \"John Doe (john.doe@example.com)\" instead of GUID. Check all role assignments show resolved names.'"
        exit 1
    fi

    cmd_setup

    local branch
    if [[ -n "$branch_override" ]]; then
        branch="$branch_override"
    else
        branch=$(git branch --show-current)
    fi
    local title="UAT: $(basename "$file" .md)"

    local description
    description=$(cat <<'EOF'
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
    - Use **Approve** (vote) if rendering is correct
    - Use **Reject / Waiting for author** and describe the issues if rendering problems are found

## Verification
Maintainer visually reviews the PR comment rendering in Azure DevOps.
EOF
)

    description="${description/__TEST_DESCRIPTION__/$test_description}"
    
    # The UAT repo is intentionally separate from this repo.
    # We use a dedicated git submodule checkout to create a lightweight branch for the PR.
    ensure_git_submodule "$AZDO_SUBMODULE_PATH"

    log_info "Preparing UAT branch '$branch' in submodule repo ($AZDO_SUBMODULE_PATH)..."
    git -C "$AZDO_SUBMODULE_PATH" fetch origin main >/dev/null 2>&1 || true

    local timestamp
    timestamp="$(date -u +%Y%m%d%H%M%S)"

    git -C "$AZDO_SUBMODULE_PATH" checkout -B "$branch" origin/main >/dev/null 2>&1
    mkdir -p "$AZDO_SUBMODULE_PATH/.uat"
    {
        echo "UAT marker commit (Azure DevOps)"
        echo "Timestamp: $timestamp"
        echo "Artifact: $file"
    } > "$AZDO_SUBMODULE_PATH/.uat/uat-run.txt"
    git -C "$AZDO_SUBMODULE_PATH" add .uat/uat-run.txt
    git -C "$AZDO_SUBMODULE_PATH" -c user.name="tfplan2md uat" -c user.email="uat@tfplan2md.invalid" commit -m "chore(uat): marker ${timestamp}" >/dev/null 2>&1 || true
    git -C "$AZDO_SUBMODULE_PATH" push origin HEAD:"$branch" --force >/dev/null 2>&1
    
    log_info "Creating PR..."
    local pr_result
    pr_result=$(az repos pr create \
        --organization "$AZDO_ORG" \
        --project "$AZDO_PROJECT" \
        --repository "$AZDO_REPO" \
        --source-branch "$branch" \
        --target-branch main \
        --title "$title" \
        --description "$description" \
        --output json)
    
    local pr_id
    pr_id=$(echo "$pr_result" | jq -r '.pullRequestId')
    local pr_url
    pr_url=$(echo "$pr_result" | jq -r '.repository.webUrl + "/pullrequest/" + (.pullRequestId|tostring)')
    
    log_info "PR created: #$pr_id ($pr_url)"
    
    # Add the markdown content as a comment
    log_info "Adding initial UAT content as comment..."
    cmd_comment "$pr_id" "$file"
    
    echo ""
    echo "========================================="
    echo "UAT PR Created: #$pr_id"
    echo "URL: $AZDO_ORG/$AZDO_PROJECT/_git/$AZDO_REPO/pullrequest/$pr_id"
    echo "========================================="
    echo ""
    echo "Next steps:"
    echo "  1. Maintainer reviews the PR comment in Azure DevOps"
    echo "  2. Poll for feedback: $0 poll $pr_id"
    echo "  3. After approval: $0 cleanup $pr_id"
}

cmd_comment() {
    local pr_id="${1:-}"
    local file="${2:-}"
    
    if [[ -z "$pr_id" || -z "$file" || ! -f "$file" ]]; then
        log_error "Usage: $0 comment <pr-id> <markdown-file>"
        exit 1
    fi
    
    # Prefix comment to distinguish agent-generated content from human comments
    local prefix="🤖 **Copilot Code Reviewer** — _This comment was generated by an AI agent._

---

"
    local raw_content
    raw_content=$(cat "$file")
    local content="${prefix}${raw_content}"
    
    # Create a new thread with the comment
    local payload
    payload=$(jq -n --arg content "$content" '{
        "comments": [
            {
                "parentCommentId": 0,
                "content": $content,
                "commentType": 1
            }
        ],
        "status": 1
    }')
    
    az devops invoke \
        --area git \
        --resource pullrequestthreads \
        --route-parameters project="$AZDO_PROJECT" repositoryId="$AZDO_REPO" pullRequestId="$pr_id" \
        --http-method POST \
        --api-version 7.1 \
        --in-file <(echo "$payload") \
        --output json >/dev/null
    
    log_info "Comment added to PR #$pr_id"
}

cmd_poll() {
    local pr_id=""
    local quiet=false
    local json=false

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
                if [[ -z "$pr_id" ]]; then
                    pr_id="$1"
                    shift
                else
                    log_error "Unknown argument: $1"
                    exit 1
                fi
                ;;
        esac
    done

    if [[ -z "$pr_id" ]]; then
        log_error "Usage: $0 poll <pr-id> [--quiet] [--json]"
        exit 1
    fi

    # JSON output is intended for machine/agent consumption; keep it JSON-only.
    if [[ "$json" == "true" ]]; then
        quiet=false
    fi

    if [[ "$quiet" != "true" && "$json" != "true" ]]; then
        log_info "Polling PR status for #$pr_id..."
    fi

    # First: check PR status + reviewer votes (strongest signal)
    local pr_json
    pr_json=$(az repos pr show --id "$pr_id" --organization "$AZDO_ORG" --output json 2>/dev/null || echo "")
    if [[ -z "$pr_json" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: ERROR"
        else
            log_error "Failed to read PR JSON. Ensure az is authenticated and the PR exists."
        fi
        return 2
    fi

    local pr_status
    pr_status=$(echo "$pr_json" | jq -r '.status // ""')
    if [[ "$pr_status" == "completed" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: APPROVED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${GREEN}✓ PR COMPLETED${NC}"
            echo "PR has been completed."
        fi
        return 0
    fi

    if [[ "$pr_status" == "abandoned" ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: CLOSED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${RED}✗ PR ABANDONED${NC}" >&2
            echo "PR has been abandoned." >&2
        fi
        return 2
    fi

    # Azure DevOps reviewer votes: 10=approved, 5=approved with suggestions,
    # 0=no vote, -5=waiting for author, -10=rejected
    local approved_vote_count
    approved_vote_count=$(echo "$pr_json" | jq '[.reviewers[]? | select((.vote // 0) >= 5)] | length')

    local rejected_vote_count
    rejected_vote_count=$(echo "$pr_json" | jq '[.reviewers[]? | select((.vote // 0) < 0)] | length')

    if [[ "$rejected_vote_count" -gt 0 ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: REJECTED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${RED}✗ REVIEWER REJECTION DETECTED${NC}" >&2
            echo "Found $rejected_vote_count reviewer vote(s) < 0 (waiting/rejected)." >&2
        fi
        return 2
    fi

    if [[ "$approved_vote_count" -gt 0 ]]; then
        if [[ "$quiet" == "true" ]]; then
            echo "STATUS: APPROVED"
        elif [[ "$json" != "true" ]]; then
            echo -e "${GREEN}✓ REVIEWER APPROVAL DETECTED${NC}"
            echo "Found $approved_vote_count reviewer approval vote(s)."
        fi
        return 0
    fi
    
    local threads
    threads=$(az devops invoke \
        --area git \
        --resource pullrequestthreads \
        --route-parameters project="$AZDO_PROJECT" repositoryId="$AZDO_REPO" pullRequestId="$pr_id" \
        --api-version 7.1 \
        --output json 2>/dev/null || echo "")

    if [[ "$json" == "true" ]]; then
        jq -n --argjson pr "$pr_json" --argjson threads "${threads:-null}" '{pr:$pr, threads:$threads}'
    fi

    if [[ "$quiet" == "true" ]]; then
        echo "STATUS: WAITING"
        return 1
    fi

    if [[ "$json" != "true" ]]; then
        echo -e "${YELLOW}⏳ AWAITING REVIEW${NC}"
        echo ""
        echo "=== PR Summary (raw) ==="
        echo "$pr_json" | jq -r '"status=\(.status // ""), url=\(.url // "")"' || true
        echo ""
        echo "=== Threads / Comments (raw, non-agent only) ==="
        echo "$threads" | jq -r '.value[]? as $t
            | $t.comments[]?
            | select((.content // "") | contains("This comment was generated by an AI agent.") | not)
            | "[\(.author.displayName // "unknown")]:\n\(.content // "")\n---"' || true
    fi

    return 1
}

cmd_cleanup() {
    local pr_id="${1:-}"
    
    if [[ -z "$pr_id" ]]; then
        log_error "Usage: $0 cleanup <pr-id>"
        exit 1
    fi
    
    log_info "Abandoning PR #$pr_id..."
    az repos pr update \
        --id "$pr_id" \
        --status abandoned \
        --organization "$AZDO_ORG" \
        --output json >/dev/null
    
    log_info "PR #$pr_id abandoned."
    
    # Branch deletion is handled centrally by scripts/uat-run.sh --cleanup-last
    # via the dedicated AzDO UAT submodule.
    log_info "Cleanup complete."
}

# Main dispatch
action="${1:-}"
shift || true

case "$action" in
    setup)   cmd_setup "$@" ;;
    create)  cmd_create "$@" ;;
    comment) cmd_comment "$@" ;;
    poll)    cmd_poll "$@" ;;
    cleanup) cmd_cleanup "$@" ;;
    *)
        echo "UAT Helper Script for Azure DevOps"
        echo ""
        echo "Usage: $0 <action> [args]"
        echo ""
        echo "Actions:"
        echo "  setup           - Configure Azure DevOps defaults and verify authentication"
        echo "  create <file>   - Create a UAT PR with initial comment from <file>"
        echo "  comment <pr-id> <file> - Add a comment to PR from <file>"
        echo "  poll <pr-id> [--quiet] [--json] - Poll for new feedback and check for approval"
        echo "                                   --quiet: Output only STATUS: APPROVED|WAITING|REJECTED|CLOSED|ERROR"
        echo "                                   --json: Output raw PR JSON (PR + threads) for agent analysis"
        echo "  cleanup <pr-id> - Abandon the PR after UAT completion"
        exit 1
        ;;
esac
