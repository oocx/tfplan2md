#!/usr/bin/env bash
# Append to the work protocol and advance workflow state.
#
# Usage:
#   Complete a stage (appends the log entry and advances .stage):
#     scripts/wp-append.sh --role "Developer" --summary "..." \
#         [--artifacts "..."] [--problems "..."]
#
#   Record a question without blocking (away from a gate):
#     scripts/wp-append.sh --question "..." --assumed "..."
#
#   Record a gate decision and unblock the run:
#     scripts/wp-append.sh --gate spec --decision approved
#
#   Send a stage back for rework (increments .attempts, drives escalation):
#     scripts/wp-append.sh --rework code-reviewer --reason "..."
set -euo pipefail
# shellcheck source=scripts/workflow-lib.sh
. "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/workflow-lib.sh"

require_jq

ROLE="" SUMMARY="" ARTIFACTS="" PROBLEMS="None"
QUESTION="" ASSUMED=""
GATE="" DECISION=""
REWORK="" REASON=""

while [ $# -gt 0 ]; do
    case "$1" in
        --role)      ROLE="$2"; shift 2 ;;
        --summary)   SUMMARY="$2"; shift 2 ;;
        --artifacts) ARTIFACTS="$2"; shift 2 ;;
        --problems)  PROBLEMS="$2"; shift 2 ;;
        --question)  QUESTION="$2"; shift 2 ;;
        --assumed)   ASSUMED="$2"; shift 2 ;;
        --gate)      GATE="$2"; shift 2 ;;
        --decision)  DECISION="$2"; shift 2 ;;
        --rework)    REWORK="$2"; shift 2 ;;
        --reason)    REASON="$2"; shift 2 ;;
        -h|--help)   sed -n '2,20p' "$0"; exit 0 ;;
        *) die "unknown argument: $1" ;;
    esac
done

DIR="$(work_item_dir)"
WP="$DIR/work-protocol.md"
TODAY="$(date +%Y-%m-%d)"
TYPE="$(workflow_type_from_branch)"

# --- record a question, do not block ---------------------------------------
if [ -n "$QUESTION" ]; then
    [ -n "$ASSUMED" ] || die "--question requires --assumed: recording a question without the assumption you proceeded on defeats the purpose"
    STAGE="$(state_get '.stage')"
    tmp="$(mktemp)"
    jq --arg q "$QUESTION" --arg a "$ASSUMED" --arg s "$STAGE" --arg d "$TODAY" \
       '.open_questions += [{q:$q, assumed:$a, raised_by:$s, raised_at:$d}]' \
       "$(state_file)" > "$tmp"
    mv "$tmp" "$(state_file)"
    echo "Recorded question (run continues): $QUESTION"
    echo "  assumption: $ASSUMED"
    exit 0
fi

# --- record a gate decision -------------------------------------------------
if [ -n "$GATE" ]; then
    [ -n "$DECISION" ] || die "--gate requires --decision"
    case "$GATE" in spec|arch|uat) ;; *) die "unknown gate: $GATE" ;; esac

    # A gate is only cleared by an explicit approval. Anything else must not
    # let the run continue: storing a decision verbatim would mean recording
    # "rejected" reads as permission to proceed.
    case "$(printf '%s' "$DECISION" | tr '[:upper:]' '[:lower:]')" in
        approved|approve|passed|pass|yes|ok)
            VALUE="approved" ;;
        rejected|reject|failed|fail|no)
            VALUE="rework" ;;
        *)
            # The architecture gate is answered with a choice, not a yes/no.
            [ "$GATE" = "arch" ] || die "gate '$GATE' takes approved or rejected, not '$DECISION'"
            VALUE="chosen: $DECISION" ;;
    esac

    tmp="$(mktemp)"
    if [ "$VALUE" = "rework" ]; then
        # Route back to the role that produced the rejected artifact and count
        # the attempt. The gate reopens when that role completes again.
        # spec -> requirements-engineer, arch -> architect. The uat gate has no
        # after_stage, and a UAT failure is always the Developer's to fix.
        REWORK_TO="$(jq -r --arg g "$GATE" '.gates[$g].after_stage // "developer"' "$WORKFLOW_JSON")"
        jq --arg g "$GATE" --arg t "$REWORK_TO" \
           '.gates[$g] = "rework"
            | .stage = $t
            | .attempts[$t] = ((.attempts[$t] // 0) + 1)
            | .status = "running"' "$(state_file)" > "$tmp"
        mv "$tmp" "$(state_file)"
        echo "Gate '$GATE' rejected — back to $REWORK_TO, and the gate stays closed."
    else
        jq --arg g "$GATE" --arg v "$VALUE" '.gates[$g] = $v' "$(state_file)" > "$tmp"
        mv "$tmp" "$(state_file)"
        echo "Gate '$GATE' recorded as: $VALUE"
    fi
    exit 0
fi

# --- send a stage back for rework ------------------------------------------
if [ -n "$REWORK" ]; then
    TARGET="$(jq -r --arg s "$REWORK" '.rework_targets[$s] // "developer"' "$WORKFLOW_JSON")"
    tmp="$(mktemp)"
    jq --arg t "$TARGET" \
       '.stage = $t | .attempts[$t] = ((.attempts[$t] // 0) + 1) | .status = "running"' \
       "$(state_file)" > "$tmp"
    mv "$tmp" "$(state_file)"
    ATTEMPTS="$(state_get ".attempts[\"$TARGET\"]")"
    echo "Rework: $REWORK -> $TARGET (attempt $((ATTEMPTS + 1)))"
    [ -n "$REASON" ] && echo "  reason: $REASON"
    [ "$ATTEMPTS" -ge 1 ] && echo "  note: $TARGET now runs one tier deeper (see .agents/tiers.json)"
    exit 0
fi

# --- complete a stage -------------------------------------------------------
[ -n "$ROLE" ] || die "nothing to do — pass --role, --question, --gate or --rework (see --help)"
[ -n "$SUMMARY" ] || die "--role requires --summary"
[ -f "$WP" ] || die "no work-protocol.md in $DIR — the first role in a workflow creates it"

# Exactly one actor completes a stage: the role itself. Advancing on a --role
# that is not the current stage would skip whichever role actually is current,
# so refuse rather than advance blindly.
CURRENT_STAGE="$(state_get '.stage')"
EXPECTED_ROLE="$(role_name "$CURRENT_STAGE")"
if [ "$(printf '%s' "$ROLE" | tr '[:upper:]' '[:lower:]')" \
     != "$(printf '%s' "$EXPECTED_ROLE" | tr '[:upper:]' '[:lower:]')" ]; then
    die "current stage is '$CURRENT_STAGE' ($EXPECTED_ROLE), but --role said '$ROLE'.
       Only the role owning the current stage completes it. If this stage really
       is finished, the owning role should append its own entry."
fi

{
    printf '\n### %s\n\n' "$ROLE"
    printf -- '- **Date:** %s\n' "$TODAY"
    printf -- '- **Summary:** %s\n' "$SUMMARY"
    printf -- '- **Artifacts Produced:** %s\n' "${ARTIFACTS:-None}"
    printf -- '- **Problems Encountered:** %s\n' "$PROBLEMS"
} >> "$WP"

# Advance to the next stage in this workflow type.
STAGE="$(state_get '.stage')"
NEXT="$(jq -r --arg t "$TYPE" --arg s "$STAGE" \
    '.types[$t].stages as $st | ($st | index($s)) as $i
     | if $i == null then "unknown" else ($st[$i + 1] // "done") end' "$WORKFLOW_JSON")"

tmp="$(mktemp)"
if [ "$NEXT" = "done" ]; then
    jq '.status = "done"' "$(state_file)" > "$tmp"
else
    jq --arg n "$NEXT" '.stage = $n' "$(state_file)" > "$tmp"
fi
mv "$tmp" "$(state_file)"

# Open the gate that follows this stage, if there is one.
GATE_AFTER="$(jq -r --arg s "$STAGE" \
    '.gates | to_entries[] | select(.value.after_stage == $s) | .key' "$WORKFLOW_JSON")"
if [ -n "$GATE_AFTER" ]; then
    ALWAYS="$(jq -r --arg g "$GATE_AFTER" '.gates[$g].always' "$WORKFLOW_JSON")"
    CURRENT="$(state_get ".gates.$GATE_AFTER // \"n/a\"")"
    if [ "$ALWAYS" = "true" ] || [ "$CURRENT" = "contested" ] || [ "$CURRENT" = "rework" ]; then
        tmp="$(mktemp)"
        jq --arg g "$GATE_AFTER" '.gates[$g] = "pending"' "$(state_file)" > "$tmp"
        mv "$tmp" "$(state_file)"
        echo "Gate '$GATE_AFTER' is now pending — the run stops here until it is decided."
    fi
fi

echo "Appended $ROLE entry to ${WP#"$REPO_ROOT"/}"
[ "$NEXT" = "done" ] && echo "Work item complete." || echo "Next stage: $NEXT"
