#!/usr/bin/env bash
# Evaluate workflow gates and completeness checks.
#
# These are the deterministic guarantees of the workflow: they hold whether or
# not a harness hook fired, and whether or not a role remembered to check.
#
# Usage:
#   scripts/workflow-gate.sh work-protocol   required roles have logged entries
#   scripts/workflow-gate.sh gates           every required gate is decided
#   scripts/workflow-gate.sh uat             does the diff warrant UAT?
#   scripts/workflow-gate.sh status          all gates and open questions
#   scripts/workflow-gate.sh all             every check (used before release)
set -euo pipefail
# shellcheck source=scripts/workflow-lib.sh
. "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/workflow-lib.sh"

require_jq

CHECK="${1:-all}"
TYPE="$(workflow_type_from_branch)"
DIR="$(work_item_dir)"
REL_DIR="${DIR#"$REPO_ROOT"/}"
WP="$DIR/work-protocol.md"

fail=0

check_work_protocol() {
    echo "Work protocol: $REL_DIR/work-protocol.md"
    if [ ! -f "$WP" ]; then
        echo "  FAIL: file does not exist"
        fail=1
        return
    fi

    local stages missing=()
    stages="$(jq -r --arg t "$TYPE" '.types[$t].gate_blocking_stages[]' "$WORKFLOW_JSON")"

    while IFS= read -r stage; do
        [ -n "$stage" ] || continue
        local name
        name="$(role_name "$stage")"
        # Match a heading for the role, tolerating a parenthesised suffix such
        # as "### Workflow Engineer (phase 2)".
        if grep -qiE "^#{2,4}[[:space:]]+${name}([[:space:]]*\(.*\))?[[:space:]]*$" "$WP"; then
            printf '  ok    %s\n' "$name"
        else
            printf '  MISS  %s\n' "$name"
            missing+=("$name")
        fi
    done <<< "$stages"

    if [ ${#missing[@]} -gt 0 ]; then
        echo
        echo "  FAIL: ${#missing[@]} required role(s) have no work-protocol entry."
        echo "  A release cannot proceed until every required role has logged its work."
        fail=1
    fi
}

check_uat() {
    echo "UAT trigger:"

    # The path rule alone is not enough. A workflow or website work item has no
    # uat-tester stage, so reporting "REQUIRED" for one is misleading — there is
    # no stage that could act on it. Internal tooling that incidentally touches a
    # user-visible path does not become a UAT candidate.
    local has_stage
    has_stage="$(jq -r --arg t "$TYPE" \
        '.types[$t].stages | index("uat-tester") != null' "$WORKFLOW_JSON")"
    if [ "$has_stage" != "true" ]; then
        echo "  not applicable — the '$TYPE' workflow has no UAT stage"
        return 1
    fi

    if uat_required; then
        echo "  REQUIRED — the diff touches user-visible output:"
        local pattern base
        pattern="$(jq -r '.uat_trigger_paths' "$WORKFLOW_JSON")"
        base="origin/main"
        git -C "$REPO_ROOT" rev-parse --verify --quiet "$base" >/dev/null || base="main"
        git -C "$REPO_ROOT" diff --name-only "$base...HEAD" | grep -E "$pattern" | sed 's/^/    /'
        return 0
    fi
    echo "  not required — no user-visible output changed"
    return 1
}

check_gates() {
    echo "Gates:"
    local unresolved=0
    while IFS=$'\t' read -r name value; do
        case "$value" in
            pending|rework|required)
                printf '  BLOCK %-6s %s\n' "$name" "$value"
                unresolved=$((unresolved + 1)) ;;
            *)
                printf '  ok    %-6s %s\n' "$name" "$value" ;;
        esac
    done < <(state_get '.gates | to_entries[] | "\(.key)\t\(.value)"')

    if [ "$unresolved" -gt 0 ]; then
        echo
        echo "  FAIL: $unresolved gate(s) still awaiting a decision."
        fail=1
    fi
}

check_status() {
    echo "State: $REL_DIR/state.json"
    printf '  stage : %s\n' "$(state_get '.stage')"
    printf '  status: %s\n' "$(state_get '.status')"
    echo "  gates :"
    state_get '.gates | to_entries[] | "    \(.key): \(.value)"'
    local n
    n="$(state_get '.open_questions | length')"
    if [ "$n" -gt 0 ]; then
        echo "  open questions ($n):"
        state_get '.open_questions[] | "    - \(.q)\n      assumed: \(.assumed)"'
    else
        echo "  open questions: none"
    fi
    local a
    a="$(state_get '.attempts | length')"
    if [ "$a" -gt 0 ]; then
        echo "  rework:"
        state_get '.attempts | to_entries[] | "    \(.key): \(.value) rework(s)"'
    fi
}

case "$CHECK" in
    work-protocol) check_work_protocol ;;
    uat)           check_uat; exit $? ;;
    status)        check_status ;;
    gates)         check_gates ;;
    all)           check_status; echo; check_gates; echo; check_work_protocol; echo; check_uat || true ;;
    *)             die "unknown check: $CHECK (expected work-protocol, gates, uat, status or all)" ;;
esac

exit "$fail"
