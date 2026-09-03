#!/usr/bin/env bash
# Resolve the next stage of the current work item and print everything needed
# to run it. Deterministic and cheap: this is what makes an unattended run
# resumable from a cold session.
#
# Usage:
#   scripts/workflow-next.sh          what to run next
#   scripts/workflow-next.sh --json   the same, as JSON
set -euo pipefail
# shellcheck source=scripts/workflow-lib.sh
. "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/workflow-lib.sh"

require_jq

AS_JSON=0
[ "${1:-}" = "--json" ] && AS_JSON=1

TYPE="$(workflow_type_from_branch)"
DIR="$(work_item_dir)"
REL_DIR="${DIR#"$REPO_ROOT"/}"
STAGE="$(state_get '.stage')"
STATUS="$(state_get '.status')"

# --- has the run finished? -------------------------------------------------
LAST="$(jq -r --arg t "$TYPE" '.types[$t].stages[-1]' "$WORKFLOW_JSON")"
if [ "$STATUS" = "done" ]; then
    echo "Work item $REL_DIR is complete (last stage: $LAST)."
    exit 0
fi

# --- is a gate open? -------------------------------------------------------
# A gate blocks the run until state records a decision. Gate state lives in
# .gates.<name>: "pending" blocks; anything else does not.
for gate in spec arch uat; do
    value="$(state_get ".gates.$gate // \"n/a\"")"
    if [ "$value" = "pending" ]; then
        prompt="$(jq -r --arg g "$gate" '.gates[$g].prompt' "$WORKFLOW_JSON")"
        echo "BLOCKED at gate: $gate"
        echo
        echo "$prompt"
        echo
        open_q="$(state_get '.open_questions | length')"
        if [ "$open_q" -gt 0 ]; then
            echo "Questions accumulated since the last gate ($open_q):"
            state_get '.open_questions[] | "  - \(.q)\n    assumed: \(.assumed)"'
            echo
        fi
        echo "Record the decision with:"
        echo "  scripts/wp-append.sh --gate $gate --decision <approved|rejected|the choice>"
        exit 2
    fi
done

# --- should this stage be skipped? -----------------------------------------
# Conditional stages run only when their condition holds. UAT is the only one
# today, and its condition is a path rule rather than a judgement.
while :; do
    is_conditional="$(jq -r --arg t "$TYPE" --arg s "$STAGE" \
        '.types[$t].conditional_stages | index($s) != null' "$WORKFLOW_JSON")"
    if [ "$is_conditional" = "true" ] && [ "$STAGE" = "uat-tester" ]; then
        if uat_required; then
            break
        fi
        NEXT="$(jq -r --arg t "$TYPE" --arg s "$STAGE" \
            '.types[$t].stages as $st | $st[(($st | index($s)) + 1)] // "done"' "$WORKFLOW_JSON")"
        echo "note: skipping uat-tester — the diff does not touch user-visible output" >&2
        state_set ".stage = \"$NEXT\" | .gates.uat = \"not-required\""
        STAGE="$NEXT"
        [ "$STAGE" = "done" ] && { state_set '.status = "done"'; echo "Work item complete."; exit 0; }
        continue
    fi
    break
done

# --- open a before-stage gate ----------------------------------------------
# UAT is declared with before_stage rather than after_stage, so nothing on the
# completion path opens it. Open it here, once, when the stage it guards is
# about to run and it has not already been decided.
GATE_BEFORE="$(jq -r --arg s "$STAGE" \
    '.gates | to_entries[] | select(.value.before_stage == $s) | .key' "$WORKFLOW_JSON")"
if [ -n "$GATE_BEFORE" ]; then
    CURRENT="$(state_get ".gates.$GATE_BEFORE // \"n/a\"")"
    case "$CURRENT" in
        approved|not-required) ;;                      # already settled
        *)
            tmp_state="$(mktemp)"
            jq --arg g "$GATE_BEFORE" '.gates[$g] = "pending"' "$(state_file)" > "$tmp_state"
            mv "$tmp_state" "$(state_file)"
            prompt="$(jq -r --arg g "$GATE_BEFORE" '.gates[$g].prompt' "$WORKFLOW_JSON")"
            echo "BLOCKED at gate: $GATE_BEFORE"
            echo
            echo "$prompt"
            echo
            echo "Record the decision with:"
            echo "  scripts/wp-append.sh --gate $GATE_BEFORE --decision <approved|rejected>"
            exit 2
            ;;
    esac
fi

# --- describe the stage ----------------------------------------------------
NAME="$(role_name "$STAGE")"
DECLARED_TIER="$(role_tier "$STAGE")"
TIER="$(effective_tier "$STAGE")"
ATTEMPTS="$(state_get ".attempts[\"$STAGE\"] // 0")"

if [ "$STAGE" = "code-reviewer" ]; then
    HARNESS="codex"
    MODEL="$(resolve_model "$STAGE" codex)"
    RUN="scripts/codex-review.sh $REL_DIR"
else
    HARNESS="claude"
    MODEL="$(resolve_model "$STAGE" claude)"
    RUN="spawn subagent '$NAME' (model: $MODEL)"
fi

if [ "$AS_JSON" -eq 1 ]; then
    jq -n \
        --arg type "$TYPE" --arg dir "$REL_DIR" --arg stage "$STAGE" \
        --arg name "$NAME" --arg tier "$TIER" --arg declared "$DECLARED_TIER" --arg model "$MODEL" \
        --arg harness "$HARNESS" --arg run "$RUN" \
        --argjson attempts "$ATTEMPTS" \
        --arg role_file ".agents/roles/$STAGE.md" \
        '{type:$type, work_item:$dir, stage:$stage, role:$name, tier:$tier, declared_tier:$declared,
          model:$model, harness:$harness, attempts:$attempts,
          role_file:$role_file, run:$run}'
    exit 0
fi

echo "Work item : $REL_DIR ($TYPE)"
echo "Next stage: $STAGE"
echo "Role      : $NAME"
printf 'Model     : %s (%s tier' "$MODEL" "$TIER"
[ "$ATTEMPTS" -ge 1 ] && printf ', escalated from %s — attempt %d' "$DECLARED_TIER" "$((ATTEMPTS + 1))"
printf ')\n'
echo "Role file : .agents/roles/$STAGE.md"
echo "Run       : $RUN"
echo
echo "Prompt:"
echo "  Act as the $NAME role. Read .agents/roles/$STAGE.md and the agent-runtime"
echo "  skill, then do your stage for the work item in $REL_DIR."
