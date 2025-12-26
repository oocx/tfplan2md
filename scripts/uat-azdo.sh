#!/usr/bin/env bash
# UAT Helper Script for Azure DevOps
# Usage: scripts/uat-azdo.sh <action> [args]
#
# Actions:
#   setup           - Configure Azure DevOps defaults and verify authentication
#   create <file>   - Create a UAT PR with initial comment from <file>
#   comment <pr-id> <file> - Add a comment to PR from <file>
#   poll <pr-id>    - Poll for new comments/threads and check for approval
#   cleanup <pr-id> - Abandon the PR after UAT completion
#
# Environment:
#   AZDO_ORG        - Azure DevOps organization URL (default: https://dev.azure.com/oocx)
#   AZDO_PROJECT    - Azure DevOps project name (default: test)
#   AZDO_REPO       - Azure DevOps repository name (default: test)

set -euo pipefail

AZDO_ORG="${AZDO_ORG:-https://dev.azure.com/oocx}"
AZDO_PROJECT="${AZDO_PROJECT:-test}"
AZDO_REPO="${AZDO_REPO:-test}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() { echo -e "${GREEN}[INFO]${NC} $*"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*" >&2; }

# Validate and set default artifact path
# Args: $1=artifact path (or empty for default), $2=simulate flag, $3=force flag
validate_artifact() {
    local artifact="${1:-}"
    local simulate="${2:-false}"
    local force="${3:-false}"
    
    # Default to comprehensive demo with inline-diff format for Azure DevOps
    if [[ -z "$artifact" ]]; then
        # Azure DevOps requires inline-diff format for better rendering
        if [[ -f "examples/comprehensive-demo/report-inline-diff.md" ]]; then
            artifact="examples/comprehensive-demo/report-inline-diff.md"
        else
            # Fallback to standard format if inline-diff not available
            artifact="examples/comprehensive-demo/report.md"
        fi
        log_info "No artifact specified, using Azure DevOps default: $artifact"
    fi
    
    # Check if artifact exists
    if [[ ! -f "$artifact" ]]; then
        log_error "Artifact not found: $artifact"
        exit 1
    fi
    
    # Block simulation artifacts in real UAT runs
    if [[ "$artifact" =~ simulation ]] && [[ "$simulate" != "true" ]] && [[ "$force" != "true" ]]; then
        log_error "Simulation artifact detected: $artifact"
        log_error "Simulation artifacts should not be used for real UAT."
        log_error "Use --simulate flag for simulation mode, or --force to override."
        exit 1
    fi
    
    log_info "✓ Using artifact: $artifact"
    echo "$artifact"
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
    local simulate="${UAT_SIMULATE:-false}"
    local force="${UAT_FORCE:-false}"
    
    # Validate and potentially set default artifact
    file="$(validate_artifact "$file" "$simulate" "$force")"

    local branch
    branch=$(git branch --show-current)
    local title="UAT: $(basename "$file" .md)"
    local description
    description=$(cat <<'EOF'
## Problem
Validate markdown rendering in real PR UIs.

## Change
Create a UAT PR and post the test markdown as PR comments.

## Verification
Maintainer visually reviews the PR comment rendering in Azure DevOps.
EOF
)
    
    # Ensure remote exists
    if ! git remote get-url azdo >/dev/null 2>&1; then
        log_info "Adding azdo remote..."
        git remote add azdo "https://oocx@dev.azure.com/oocx/$AZDO_PROJECT/_git/$AZDO_REPO"
    fi
    
    log_info "Pushing branch to Azure DevOps..."
    git push -u azdo HEAD:"$branch" --force
    
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
    pr_url=$(echo "$pr_result" | jq -r '.url // empty')
    
    log_info "PR created: #$pr_id"
    
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
    local pr_id="${1:-}"
    
    if [[ -z "$pr_id" ]]; then
        log_error "Usage: $0 poll <pr-id>"
        exit 1
    fi
    
    log_info "Polling threads for PR #$pr_id..."

    # First: check PR status + reviewer votes (strongest signal)
    local pr_json
    pr_json=$(az repos pr show --id "$pr_id" --organization "$AZDO_ORG" --output json)

    local pr_status
    pr_status=$(echo "$pr_json" | jq -r '.status // ""')
    if [[ "$pr_status" == "completed" ]]; then
        echo -e "${GREEN}✓ PR COMPLETED${NC}"
        echo "PR has been completed by Maintainer. UAT passed."
        return 0
    fi

    # Azure DevOps reviewer votes: 10=approved, 5=approved with suggestions
    local approved_vote_count
    approved_vote_count=$(echo "$pr_json" | jq '[.reviewers[]? | select((.vote // 0) >= 5)] | length')
    if [[ "$approved_vote_count" -gt 0 ]]; then
        echo -e "${GREEN}✓ REVIEWER APPROVAL DETECTED${NC}"
        echo "Found $approved_vote_count reviewer approval vote(s). UAT passed."
        return 0
    fi
    
    local threads
    threads=$(az devops invoke \
        --area git \
        --resource pullrequestthreads \
        --route-parameters project="$AZDO_PROJECT" repositoryId="$AZDO_REPO" pullRequestId="$pr_id" \
        --api-version 7.1 \
        --output json)
    
    # Check for approval keywords in NON-agent comments
    # (agent comments include the marker line: "This comment was generated by an AI agent.")
    local approval_found
    approval_found=$(echo "$threads" |
        jq -r '.value[].comments[]? | select((.content // "") | contains("This comment was generated by an AI agent.") | not) | .content // ""' |
        grep -iE '(approved|passed|accept|lgtm)' || true)
    
    echo ""
    echo "=== Thread Summary ==="
    echo "$threads" | jq -r '.value[] | "Thread \(.id): status=\(.status // "active"), comments=\(.comments | length)"'
    echo ""
    
    # Show latest comments
    echo "=== Recent Comments ==="
    echo "$threads" | jq -r '.value[-3:][].comments[] | "[\(.author.displayName // "unknown")]: \(.content[0:200])"' 2>/dev/null || echo "(no comments)"
    echo ""
    
    if [[ -n "$approval_found" ]]; then
        echo -e "${GREEN}✓ APPROVAL DETECTED${NC}"
        echo "Approval keyword found in comments. UAT passed."
        return 0
    fi
    
    echo -e "${YELLOW}⏳ AWAITING FEEDBACK${NC}"
    echo "No approval detected yet. Continue polling or check Azure DevOps UI."
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
    
    # Clean up remote branch
    local branch
    branch=$(git branch --show-current)
    log_info "Deleting remote branch from Azure DevOps..."
    git push azdo --delete "$branch" 2>/dev/null || log_warn "Could not delete remote branch (may already be deleted)"
    
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
        echo "  poll <pr-id>    - Poll for new comments/threads and check for approval"
        echo "  cleanup <pr-id> - Abandon the PR after UAT completion"
        exit 1
        ;;
esac
