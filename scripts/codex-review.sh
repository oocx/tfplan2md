#!/usr/bin/env bash
# Run the Code Reviewer role in Codex, in a different model family from the
# author, and turn its structured result into code-review.md plus a verdict.
#
# This is the only place Codex CLI flags live. A CLI change is a one-file fix.
#
# Usage:
#   scripts/codex-review.sh [work-item-dir]
#
# Exit codes:
#   0  APPROVED
#   1  REWORK (including a failed or unparseable review — never approval)
#   2  codex unavailable; caller should fall back to a Claude reviewer
set -euo pipefail
# shellcheck source=scripts/workflow-lib.sh
. "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/workflow-lib.sh"

require_jq

# rustup and npm global installs are often absent from a non-login shell's PATH.
[ -d "$HOME/.cargo/bin" ] && PATH="$HOME/.cargo/bin:$PATH"

ROLE_FILE="$ROLES_DIR/code-reviewer.md"
SCHEMA="$REPO_ROOT/.agents/codex-review-schema.json"

DIR="${1:-$(work_item_dir)}"
[ -d "$DIR" ] || DIR="$REPO_ROOT/$DIR"
[ -d "$DIR" ] || die "work item directory not found: ${1:-}"
REPORT="$DIR/code-review.md"

BASE="origin/main"
git -C "$REPO_ROOT" rev-parse --verify --quiet "$BASE" >/dev/null || BASE="main"

MODEL="$(jq -r '.tiers.deep.codex' "$TIERS_JSON")"

if ! command -v codex >/dev/null 2>&1; then
    echo "codex is not installed — falling back to a Claude reviewer." >&2
    echo "Record 'reviewer: claude-fallback' in work-protocol.md." >&2
    exit 2
fi

RESULT="$(mktemp)"
trap 'rm -f "$RESULT"' EXIT

# `codex exec review` refuses a custom prompt alongside --base, so the role file
# could not be used with it. Plain `codex exec` takes both the instructions and
# the diff range, and accepts --sandbox: read-only means the reviewer physically
# cannot modify the code it is reviewing, which turns the role's "never edit
# src/" boundary from an instruction into a guarantee.
#
# The role file is piped in rather than passed as an argument, so it is not
# subject to argv length limits. Codex reads AGENTS.md from the repository
# itself, so project conventions need not be repeated here.
run_codex() {
    {
        cat "$ROLE_FILE"
        cat <<EOF

---

Review the changes on this branch. The diff under review is:

    git diff $BASE...HEAD

The work item is at ${DIR#"$REPO_ROOT"/}. Read its specification (or analysis),
test plan and tasks before reading the diff, and review against intent.

You are running in a read-only sandbox: you cannot modify files, and you cannot
run the test suite. Verify by reading the code, and by checking the evidence the
Developer recorded in work-protocol.md and in CI. If the evidence that the change
works is missing, that is itself a finding.

Respond only with the JSON object described by the output schema.
EOF
    } | codex exec \
            -C "$REPO_ROOT" \
            -m "$MODEL" \
            --sandbox read-only \
            --output-schema "$SCHEMA" \
            --output-last-message "$RESULT" \
            - >/dev/null
}

echo "Reviewing ${DIR#"$REPO_ROOT"/} against $BASE using codex ($MODEL)..."

if ! run_codex; then
    echo "codex review failed; retrying once..." >&2
    if ! run_codex; then
        echo "codex review failed twice — fall back to a Claude reviewer." >&2
        echo "Record 'reviewer: claude-fallback' in work-protocol.md." >&2
        exit 2
    fi
fi

if ! jq -e '.verdict' "$RESULT" >/dev/null 2>&1; then
    echo "codex returned no parseable verdict — treating as REWORK." >&2
    echo "Raw output:" >&2
    head -40 "$RESULT" >&2
    exit 1
fi

VERDICT="$(jq -r '.verdict' "$RESULT")"
BLOCKERS="$(jq '[.findings[] | select(.severity == "Blocker")] | length' "$RESULT")"

# A verdict of APPROVED alongside a Blocker is self-contradictory. Trust the
# finding, not the label.
if [ "$VERDICT" = "APPROVED" ] && [ "$BLOCKERS" -gt 0 ]; then
    echo "warning: verdict was APPROVED with $BLOCKERS blocker(s); overriding to REWORK." >&2
    VERDICT="REWORK"
fi

# --- render the report ------------------------------------------------------
{
    echo "# Code Review: $(basename "$DIR")"
    echo
    echo "**Reviewer:** codex ($MODEL) · **Base:** \`$BASE\` · **Date:** $(date +%Y-%m-%d)"
    echo
    echo "## Summary"
    echo
    jq -r '.summary' "$RESULT"
    for section in verification spec_compliance probed; do
        value="$(jq -r --arg s "$section" '.[$s] // empty' "$RESULT")"
        [ -n "$value" ] || continue
        case "$section" in
            verification)     echo; echo "## Verification Results" ;;
            spec_compliance)  echo; echo "## Specification Compliance" ;;
            probed)           echo; echo "## What I Tried To Break" ;;
        esac
        echo
        echo "$value"
    done
    echo
    echo "## Issues Found"
    if [ "$(jq '.findings | length' "$RESULT")" -eq 0 ]; then
        echo
        echo "None."
    else
        for sev in Blocker Major Minor Suggestion; do
            count="$(jq --arg s "$sev" '[.findings[] | select(.severity == $s)] | length' "$RESULT")"
            [ "$count" -gt 0 ] || continue
            echo
            echo "### ${sev}s"
            echo
            jq -r --arg s "$sev" '
                .findings[] | select(.severity == $s)
                | "- **\(.title)**"
                  + (if .file then " — `\(.file)\(if .line then ":\(.line)" else "" end)`" else "" end)
                  + "\n  \(.detail)"
            ' "$RESULT"
        done
    fi
    echo
    echo "## Decision"
    echo
    echo "\`VERDICT: $VERDICT\`"
} > "$REPORT"

echo "Wrote ${REPORT#"$REPO_ROOT"/}"
SEVERITIES="$(jq -r '"Findings: " + ([.findings[] | .severity] | group_by(.) | map("\(length) \(.[0])") | join(", ") // "none")' "$RESULT")"
echo "$SEVERITIES"
echo "VERDICT: $VERDICT"

# The reviewer is a subprocess and cannot append its own work-protocol entry,
# but completion still has exactly one owner — so the wrapper does it on the
# role's behalf. Without this the stage never advances: an APPROVED review
# would leave .stage at code-reviewer and be re-run forever.
if [ "$(state_get '.stage')" = "code-reviewer" ]; then
    "$REPO_ROOT/scripts/wp-append.sh" \
        --role "Code Reviewer" \
        --summary "Reviewed against $BASE in codex ($MODEL). Verdict: $VERDICT. $SEVERITIES" \
        --artifacts "${REPORT#"$REPO_ROOT"/}" \
        --problems "None" >/dev/null

    if [ "$VERDICT" = "REWORK" ]; then
        # The entry advanced the stage; route it back to the Developer.
        "$REPO_ROOT/scripts/wp-append.sh" --rework code-reviewer \
            --reason "code review returned REWORK" >/dev/null
    fi
else
    echo "note: stage is '$(state_get '.stage')', not code-reviewer — state not advanced." >&2
fi

[ "$VERDICT" = "APPROVED" ] && exit 0 || exit 1
