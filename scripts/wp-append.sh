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

# Attempts before the run stops for a human. See the rework branch below.
MAX_ATTEMPTS=3

INIT_TYPE="" INIT_SLUG=""
ROLE="" SUMMARY="" ARTIFACTS="" PROBLEMS="None"
QUESTION="" ASSUMED=""
GATE="" DECISION=""
REWORK="" REASON=""

while [ $# -gt 0 ]; do
    case "$1" in
        --init)      INIT_TYPE="$2"; INIT_SLUG="${3:-}"; shift 3 ;;
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

# --- create a work item -----------------------------------------------------
if [ -n "$INIT_TYPE" ]; then
    [ -n "$INIT_SLUG" ] || die "--init needs a type and a slug"
    folder="$(jq -r --arg t "$INIT_TYPE" '.types[$t].folder // empty' "$WORKFLOW_JSON")"
    [ -n "$folder" ] || die "unknown workflow type '$INIT_TYPE'"
    first="$(jq -r --arg t "$INIT_TYPE" '.types[$t].stages[0]' "$WORKFLOW_JSON")"
    target="$REPO_ROOT/$folder/$INIT_SLUG"
    mkdir -p "$target"
    [ -f "$target/state.json" ] && die "$folder/$INIT_SLUG/state.json already exists"

    jq -n --arg t "$INIT_TYPE" --arg s "$INIT_SLUG" --arg st "$first" \
        '{type:$t, slug:$s, stage:$st, status:"running",
          gates:{spec:"n/a", arch:"n/a", uat:"n/a"},
          attempts:{}, open_questions:[]}' > "$target/state.json"

    if [ ! -f "$target/work-protocol.md" ]; then
        {
            printf '# Work Protocol: %s\n\n' "$INIT_SLUG"
            printf '**Work Item:** `%s/%s/`\n' "$folder" "$INIT_SLUG"
            printf '**Workflow Type:** %s\n' "$INIT_TYPE"
            printf '**Created:** %s\n\n' "$(date +%Y-%m-%d)"
            printf '## Agent Work Log\n\n'
            printf '<!-- Each role appends its entry below on completion. -->\n'
        } > "$target/work-protocol.md"
    fi
    echo "Created $folder/$INIT_SLUG/ (state.json, work-protocol.md); first stage: $first"
    exit 0
fi

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

    # Only an open gate can be decided. Without this, a caller can pre-approve a
    # gate while it is still "n/a" and workflow-next will later see "approved"
    # and never stop there — silently removing a mandatory human decision.
    GATE_STATE="$(state_get ".gates.$GATE // \"n/a\"")"
    case "$GATE_STATE" in
        pending) ;;
        *) die "gate '$GATE' is '$GATE_STATE', not pending — there is no open decision to record." ;;
    esac

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
        # A rejection goes back to whoever can act on it. That is usually the
        # role whose artifact was rejected (spec -> Requirements Engineer,
        # arch -> Architect), but a UAT failure is a defect in the code, not in
        # the UAT run, so rework_targets redirects it to the Developer.
        REWORK_TO="$(jq -r --arg g "$GATE" \
            '(.gates[$g].after_stage // "developer") as $owner
             | .rework_targets[$owner] // $owner' "$WORKFLOW_JSON")"
        jq --arg g "$GATE" --arg t "$REWORK_TO" \
           '.gates[$g] = "rework"
            | .stage = $t
            | .attempts[$t] = ((.attempts[$t] // 0) + 1)
            | .status = "running"' "$(state_file)" > "$tmp"
        mv "$tmp" "$(state_file)"
        echo "Gate '$GATE' rejected — back to $REWORK_TO, and the gate stays closed."

        # The same cap as the rework branch. A gate rejected over and over is
        # the clearest possible signal that the disagreement is not going to be
        # resolved by another attempt at the same stage.
        GATE_ATTEMPTS="$(state_get ".attempts[\"$REWORK_TO\"]")"
        if [ "$GATE_ATTEMPTS" -ge "$MAX_ATTEMPTS" ]; then
            tmp="$(mktemp)"
            jq '.status = "blocked"' "$(state_file)" > "$tmp"
            mv "$tmp" "$(state_file)"
            echo
            echo "BLOCKED: $REWORK_TO has now been rejected $GATE_ATTEMPTS times."
            echo "Stop and involve the Maintainer."
            exit 3
        fi
    else
        jq --arg g "$GATE" --arg v "$VALUE" '.gates[$g] = $v' "$(state_file)" > "$tmp"
        mv "$tmp" "$(state_file)"
        echo "Gate '$GATE' recorded as: $VALUE"
    fi
    exit 0
fi

# --- send a stage back for rework ------------------------------------------
if [ -n "$REWORK" ]; then
    # Default to sending work back to the stage that failed. The explicit
    # rework_targets entries cover the cases where that is wrong — a code review
    # or UAT failure is the Developer's to fix, not the reviewer's.
    VALID_STAGE="$(jq -r --arg t "$TYPE" --arg s "$REWORK" \
        '.types[$t].stages | index($s) != null' "$WORKFLOW_JSON")"
    [ "$VALID_STAGE" = "true" ] \
        || die "'$REWORK' is not a stage of the $TYPE workflow. Valid: $(jq -r --arg t "$TYPE" '.types[$t].stages | join(", ")' "$WORKFLOW_JSON")"
    TARGET="$(jq -r --arg s "$REWORK" '.rework_targets[$s] // $s' "$WORKFLOW_JSON")"
    tmp="$(mktemp)"
    jq --arg t "$TARGET" \
       '.stage = $t | .attempts[$t] = ((.attempts[$t] // 0) + 1) | .status = "running"' \
       "$(state_file)" > "$tmp"
    mv "$tmp" "$(state_file)"
    ATTEMPTS="$(state_get ".attempts[\"$TARGET\"]")"
    echo "Rework: $REWORK -> $TARGET (attempt $((ATTEMPTS + 1)))"
    [ -n "$REASON" ] && echo "  reason: $REASON"
    [ "$ATTEMPTS" -ge 1 ] && echo "  note: $TARGET now runs one tier deeper (see .agents/tiers.json)"

    # Escalating forever is not a strategy. Repeated failure at one stage is
    # usually a specification problem wearing an implementation costume, and the
    # tier ladder tops out after one step anyway — attempt 7 is indistinguishable
    # from attempt 2. Enforce the cap here rather than in prose, because
    # codex-review.sh drives this path in-process and never reads the docs.
    if [ "$ATTEMPTS" -ge "$MAX_ATTEMPTS" ]; then
        tmp="$(mktemp)"
        jq '.status = "blocked"' "$(state_file)" > "$tmp"
        mv "$tmp" "$(state_file)"
        echo
        echo "BLOCKED: $TARGET has now failed $ATTEMPTS times."
        echo "Stop and involve the Maintainer. Repeated failure at one stage is usually a"
        echo "specification problem, not an implementation one."
        exit 3
    fi
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

# A role logs more than once whenever it is reworked. Bare repeated headings
# trip markdownlint MD024, which PR validation runs over docs/ — so every
# rework loop would fail the build. Number the repeats. workflow-gate.sh's
# matcher already tolerates a parenthesised suffix.
OCCURRENCE=$(grep -ciE "^#{2,4}[[:space:]]+${ROLE}([[:space:]]*\(.*\))?[[:space:]]*$" "$WP" || true)
if [ "$OCCURRENCE" -gt 0 ]; then
    HEADING="$ROLE (round $((OCCURRENCE + 1)))"
else
    HEADING="$ROLE"
fi

{
    printf '\n### %s\n\n' "$HEADING"
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

# The UAT gate hangs off a different stage per type: the UAT Tester for a
# feature or fix, the Web Designer for a website change.
UAT_AFTER="$(jq -r --arg t "$TYPE" '.types[$t].uat_gate_after // "null"' "$WORKFLOW_JSON")"
UAT_DUE=0
if [ -z "$GATE_AFTER" ] && [ "$UAT_AFTER" = "$STAGE" ] && uat_required; then
    GATE_AFTER="uat"
    # The path rule has fired, so the gate is due now. Do not depend on
    # workflow-next having run first to mark it "required" — a driver that
    # completes a stage without re-querying would skip a mandatory approval.
    UAT_DUE=1
fi
if [ -n "$GATE_AFTER" ]; then
    ALWAYS="$(jq -r --arg g "$GATE_AFTER" '.gates[$g].always' "$WORKFLOW_JSON")"
    CURRENT="$(state_get ".gates.$GATE_AFTER // \"n/a\"")"

    # The Architect signals a contested choice in its own field. The driver owns
    # .gates.* exclusively: a role that could write the control field could
    # overwrite a rejection with "auto" and walk past a decision the Maintainer
    # already refused.
    CONTESTED="$(state_get '.arch_contested // false')"
    if [ "$GATE_AFTER" = "arch" ] && [ "$CONTESTED" = "true" ]; then
        CURRENT="contested"
    fi
    if [ "$ALWAYS" = "true" ] || [ "$UAT_DUE" = "1" ] || [ "$CURRENT" = "contested" ] \
       || [ "$CURRENT" = "rework" ] || [ "$CURRENT" = "required" ]; then
        tmp="$(mktemp)"
        jq --arg g "$GATE_AFTER" '.gates[$g] = "pending"' "$(state_file)" > "$tmp"
        mv "$tmp" "$(state_file)"
        echo "Gate '$GATE_AFTER' is now pending — the run stops here until it is decided."
    fi
fi

echo "Appended \"$HEADING\" entry to ${WP#"$REPO_ROOT"/}"
[ "$NEXT" = "done" ] && echo "Work item complete." || echo "Next stage: $NEXT"
