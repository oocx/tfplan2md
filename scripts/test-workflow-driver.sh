#!/usr/bin/env bash
# Exercise the workflow state machine in a throwaway repository.
#
# The driver decides whether roles are skipped and whether gates can be
# bypassed, so it needs coverage that does not depend on anyone remembering to
# check by hand. Every case here corresponds to a defect that a code review
# found in the driver, or to a rule the design depends on.
#
# Usage: scripts/test-workflow-driver.sh
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SANDBOX="$(mktemp -d)"
trap 'rm -rf "$SANDBOX"' EXIT

pass=0
fail=0

ok()   { printf '  \033[32mPASS\033[0m %s\n' "$1"; pass=$((pass + 1)); }
bad()  { printf '  \033[31mFAIL\033[0m %s\n     %s\n' "$1" "${2:-}"; fail=$((fail + 1)); }

# assert_eq <description> <expected> <actual>
assert_eq() {
    if [ "$2" = "$3" ]; then ok "$1"; else bad "$1" "expected '$2', got '$3'"; fi
}

# assert_exit <description> <expected-code> <command...>
assert_exit() {
    local desc=$1 want=$2; shift 2
    "$@" >/dev/null 2>&1
    local got=$?
    if [ "$got" -eq "$want" ]; then ok "$desc"; else bad "$desc" "expected exit $want, got $got"; fi
}

# Build a fresh repo with the workflow tooling and one work item.
# new_repo <type> <slug> <stage> [file-to-touch]
new_repo() {
    local type=$1 slug=$2 stage=$3 touch_file=${4:-README.md}
    local dir="$SANDBOX/$RANDOM$RANDOM"
    mkdir -p "$dir"
    cp -r "$REPO_ROOT/.agents" "$dir/"
    mkdir -p "$dir/scripts"
    cp "$REPO_ROOT"/scripts/{workflow-lib.sh,workflow-next.sh,workflow-gate.sh,wp-append.sh} "$dir/scripts/"

    git -C "$dir" init -q
    git -C "$dir" config user.email t@t.t
    git -C "$dir" config user.name t
    git -C "$dir" checkout -q -b main
    echo base > "$dir/README.md"
    git -C "$dir" add -A >/dev/null
    git -C "$dir" commit -qm "base"

    local folder
    folder="$(jq -r --arg t "$type" '.types[$t].folder' "$dir/.agents/workflow.json")"
    local prefix
    prefix="$(jq -r --arg t "$type" '.types[$t].branch_prefix' "$dir/.agents/workflow.json")"
    git -C "$dir" checkout -q -b "$prefix/$slug"

    mkdir -p "$dir/$folder/$slug"
    printf '# Work Protocol\n\n## Agent Work Log\n' > "$dir/$folder/$slug/work-protocol.md"
    jq -n --arg t "$type" --arg s "$slug" --arg st "$stage" \
        '{type:$t, slug:$s, stage:$st, status:"running",
          gates:{spec:"n/a", arch:"n/a", uat:"n/a"},
          attempts:{}, open_questions:[]}' > "$dir/$folder/$slug/state.json"

    mkdir -p "$(dirname "$dir/$touch_file")"
    echo change >> "$dir/$touch_file"
    git -C "$dir" add -A >/dev/null
    git -C "$dir" commit -qm "work"
    echo "$dir"
}

stage_of() { jq -r '.stage' "$(find "$1/docs" -name state.json | head -1)"; }
set_gate() {
    local f; f="$(find "$1/docs" -name state.json | head -1)"
    local t; t="$(mktemp)"
    jq --arg g "$2" --arg v "$3" '.gates[$g] = $v' "$f" > "$t" && mv "$t" "$f"
}
gate_of()  { jq -r --arg g "$2" '.gates[$g]' "$(find "$1/docs" -name state.json | head -1)"; }
attempts_of() { jq -r --arg s "$2" '.attempts[$s] // 0' "$(find "$1/docs" -name state.json | head -1)"; }

echo "Workflow driver"
echo

# --- stage advancement ------------------------------------------------------
R="$(new_repo feature 900-test requirements-engineer)"
assert_eq "feature starts at requirements-engineer" "requirements-engineer" "$(stage_of "$R")"

(cd "$R" && scripts/wp-append.sh --role "Requirements Engineer" --summary s) >/dev/null 2>&1
assert_eq "completing a stage advances to the next" "architect" "$(stage_of "$R")"
assert_eq "a gate declared always:true opens on completion" "pending" "$(gate_of "$R" spec)"
assert_exit "workflow-next blocks while a gate is pending" 2 \
    env -C "$R" scripts/workflow-next.sh

# --- a rejected gate must not unblock the run (Blocker) ---------------------
(cd "$R" && scripts/wp-append.sh --gate spec --decision rejected) >/dev/null 2>&1
assert_eq "rejection routes back to the gate's owning role" "requirements-engineer" "$(stage_of "$R")"
assert_eq "rejection counts an attempt" "1" "$(attempts_of "$R" requirements-engineer)"
# The original defect was not the stored label but the consequence: a stored
# "rejected" was simply not "pending", so the run walked straight past the gate
# into the stage the gate exists to guard.
if [ "$(stage_of "$R")" = "architect" ]; then
    bad "a rejection does not let the run enter the guarded stage" "stage advanced to architect anyway"
else
    ok "a rejection does not let the run enter the guarded stage"
fi
assert_exit "the run proceeds to redo the rejected stage" 0 \
    env -C "$R" scripts/workflow-next.sh

(cd "$R" && scripts/wp-append.sh --role "Requirements Engineer" --summary s2) >/dev/null 2>&1
assert_eq "the gate reopens after the rework" "pending" "$(gate_of "$R" spec)"
(cd "$R" && scripts/wp-append.sh --gate spec --decision approved) >/dev/null 2>&1
assert_eq "approval clears the gate" "approved" "$(gate_of "$R" spec)"
assert_exit "the run continues once approved" 0 env -C "$R" scripts/workflow-next.sh

# --- only the current stage's role may complete it (Blocker) ---------------
assert_exit "a --role that is not the current stage is refused" 1 \
    env -C "$R" scripts/wp-append.sh --role "Developer" --summary wrong
assert_eq "the refused append did not advance the stage" "architect" "$(stage_of "$R")"

# --- gate decisions are validated -------------------------------------------
assert_exit "an unrecognised decision on a yes/no gate is refused" 1 \
    env -C "$R" scripts/wp-append.sh --gate spec --decision "maybe later"
# A gate can only be decided while it is open. Pre-approving a gate that is
# still "n/a" would remove a mandatory human decision from the run.
R2="$(new_repo feature 901-arch architect)"
assert_exit "a decision on a gate that is not open is refused" 1 \
    env -C "$R2" scripts/wp-append.sh --gate arch --decision "Option B"
set_gate "$R2" arch contested
(cd "$R2" && scripts/wp-append.sh --role Architect --summary s) >/dev/null 2>&1
assert_eq "a contested architecture opens the gate" "pending" "$(gate_of "$R2" arch)"
(cd "$R2" && scripts/wp-append.sh --gate arch --decision "Option B: streaming") >/dev/null 2>&1
assert_eq "the architecture gate accepts a choice" "chosen: Option B: streaming" "$(gate_of "$R2" arch)"

R2b="$(new_repo feature 910-archauto architect)"
set_gate "$R2b" arch auto
(cd "$R2b" && scripts/wp-append.sh --role Architect --summary s) >/dev/null 2>&1
assert_eq "an uncontested architecture does not open the gate" "auto" "$(gate_of "$R2b" arch)"

# --- UAT gate opens before its stage (Blocker) ------------------------------
# The Maintainer is asked to approve the rendered output in the UAT PRs, so the
# gate must be decided AFTER the UAT Tester has created them, not before.
UAT_PATH="src/Oocx.TfPlan2Md/MarkdownGeneration/Renderer.cs"
R3="$(new_repo feature 902-uat uat-tester "$UAT_PATH")"
assert_exit "a UAT-triggering diff dispatches the UAT Tester" 0 \
    env -C "$R3" scripts/workflow-next.sh
assert_eq "UAT is flagged required, not yet decidable" "required" "$(gate_of "$R3" uat)"
(cd "$R3" && scripts/wp-append.sh --role "UAT Tester" --summary s) >/dev/null 2>&1
assert_eq "the gate opens once the PRs exist" "pending" "$(gate_of "$R3" uat)"
assert_exit "the run blocks on the open UAT gate" 2 env -C "$R3" scripts/workflow-next.sh
(cd "$R3" && scripts/wp-append.sh --gate uat --decision failed) >/dev/null 2>&1
assert_eq "a UAT failure routes to the Developer" "developer" "$(stage_of "$R3")"

R4="$(new_repo feature 903-nouat uat-tester "docs/notes.md")"
(cd "$R4" && scripts/workflow-next.sh) >/dev/null 2>&1
assert_eq "a non-user-visible diff skips UAT" "release-manager" "$(stage_of "$R4")"
assert_eq "the skip is recorded" "not-required" "$(gate_of "$R4" uat)"

# --- model escalation -------------------------------------------------------
R5="$(new_repo feature 904-esc developer)"
assert_eq "developer starts at its declared tier" "sonnet" \
    "$( (cd "$R5" && scripts/workflow-next.sh --json) | jq -r '.model')"
(cd "$R5" && scripts/wp-append.sh --rework code-reviewer --reason r) >/dev/null 2>&1
assert_eq "developer escalates one tier after rework" "opus" \
    "$( (cd "$R5" && scripts/workflow-next.sh --json) | jq -r '.model')"

# --- the reviewer runs in codex --------------------------------------------
R6="$(new_repo feature 905-review code-reviewer)"
assert_eq "the code-reviewer stage targets codex" "codex" \
    "$( (cd "$R6" && scripts/workflow-next.sh --json) | jq -r '.harness')"
assert_eq "the reviewer uses the deep codex model" "gpt-5.6-sol" \
    "$( (cd "$R6" && scripts/workflow-next.sh --json) | jq -r '.model')"

# --- other workflow types ---------------------------------------------------
R7="$(new_repo workflow 906-wf workflow-engineer)"
(cd "$R7" && scripts/wp-append.sh --role "Workflow Engineer" --summary s) >/dev/null 2>&1
assert_eq "a workflow item goes straight to release" "release-manager" "$(stage_of "$R7")"
assert_eq "no spec gate opens for a workflow item" "n/a" "$(gate_of "$R7" spec)"

# A workflow item that incidentally touches a user-visible path must not be
# reported as needing UAT: its stage list has no uat-tester to act on it.
R7b="$(new_repo workflow 912-wfweb workflow-engineer "website/src/_data/x.js")"
assert_exit "a workflow item is never a UAT candidate" 1 \
    env -C "$R7b" scripts/workflow-gate.sh uat

R8="$(new_repo fix 907-bug issue-analyst)"
(cd "$R8" && scripts/wp-append.sh --role "Issue Analyst" --summary s) >/dev/null 2>&1
assert_eq "a bug fix goes from analysis to the developer" "developer" "$(stage_of "$R8")"

# --- questions never block --------------------------------------------------
R9="$(new_repo feature 908-q developer)"
assert_exit "a question requires the assumption in force" 1 \
    env -C "$R9" scripts/wp-append.sh --question "why?"
(cd "$R9" && scripts/wp-append.sh --question "why?" --assumed "because") >/dev/null 2>&1
assert_eq "a recorded question does not change the stage" "developer" "$(stage_of "$R9")"
assert_exit "the run continues after a question" 0 env -C "$R9" scripts/workflow-next.sh

# --- repeated entries must not produce duplicate markdown headings ---------
# A role logs again on every rework. Bare repeats trip markdownlint MD024,
# which PR validation runs, so every rework loop would fail the build.
R12="$(new_repo feature 913-dup requirements-engineer)"
(cd "$R12" && scripts/wp-append.sh --role "Requirements Engineer" --summary a) >/dev/null 2>&1
(cd "$R12" && scripts/wp-append.sh --gate spec --decision rejected) >/dev/null 2>&1
(cd "$R12" && scripts/wp-append.sh --role "Requirements Engineer" --summary b) >/dev/null 2>&1
WP12="$(find "$R12/docs" -name work-protocol.md | head -1)"
assert_eq "a repeated role entry gets a distinct heading" "1" \
    "$(grep -c '^### Requirements Engineer (round 2)$' "$WP12")"
assert_eq "the original heading is untouched" "1" \
    "$(grep -c '^### Requirements Engineer$' "$WP12")"
# The numbered heading must still satisfy the completeness matcher. The check
# as a whole still fails here — a feature workflow needs seven roles — so
# assert on this role's row rather than the overall exit code.
assert_eq "the completeness matcher accepts a numbered heading" "1" \
    "$( (cd "$R12" && scripts/workflow-gate.sh work-protocol 2>&1) | grep -c '^  ok    Requirements Engineer$')"

# --- work-protocol completeness --------------------------------------------
R10="$(new_repo workflow 909-gate release-manager)"
assert_exit "release is refused while a required role has no entry" 1 \
    env -C "$R10" scripts/workflow-gate.sh work-protocol

# The Release Manager runs the check before doing its work, so requiring its own
# entry would make every release fail on itself.
if grep -q 'release-manager' <(jq -r '.types[].gate_blocking_stages[]' "$R10/.agents/workflow.json"); then
    bad "the release check does not require the Release Manager's own entry" "release-manager is gate-blocking"
else
    ok "the release check does not require the Release Manager's own entry"
fi

# An undecided gate must fail the pre-release check, not merely be printed.
R11="$(new_repo feature 911-openg release-manager)"
set_gate "$R11" spec pending
assert_exit "an undecided gate fails the pre-release check" 1 \
    env -C "$R11" scripts/workflow-gate.sh gates

echo
echo "$pass passed, $fail failed"
[ "$fail" -eq 0 ]
