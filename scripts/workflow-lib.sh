#!/usr/bin/env bash
# Shared helpers for the workflow driver scripts. Sourced, not executed.
#
# Locating the work item is deliberately deterministic: it comes from the branch
# name, so a cold session resumes without being told where it is.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORKFLOW_JSON="$REPO_ROOT/.agents/workflow.json"
TIERS_JSON="$REPO_ROOT/.agents/tiers.json"
ROLES_DIR="$REPO_ROOT/.agents/roles"

die() { echo "error: $*" >&2; exit 1; }

require_jq() {
    command -v jq >/dev/null 2>&1 || die "jq is required (scripts/agent-doctor.sh)"
}

# Print the current branch name.
current_branch() {
    git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD
}

# Derive the workflow type from the branch prefix.
# Echoes one of: feature | fix | workflow | website
workflow_type_from_branch() {
    local branch prefix
    branch="$(current_branch)"
    prefix="${branch%%/*}"
    case "$prefix" in
        feature|fix|workflow|website) echo "$prefix" ;;
        *) die "branch '$branch' has no recognised work-item prefix (feature/, fix/, workflow/, website/)" ;;
    esac
}

# Echo the work item directory for the current branch, e.g.
# docs/workflow/125-harness-neutral-agent-workflow
work_item_dir() {
    local branch type folder slug dir
    branch="$(current_branch)"
    type="$(workflow_type_from_branch)"
    folder="$(jq -r --arg t "$type" '.types[$t].folder' "$WORKFLOW_JSON")"
    slug="${branch#*/}"
    dir="$REPO_ROOT/$folder/$slug"

    if [ ! -d "$dir" ]; then
        # Tolerate a branch slug that differs from the folder slug after the
        # numeric prefix, which happens when a folder is renamed mid-flight.
        local num="${slug%%-*}"
        local match
        match="$(find "$REPO_ROOT/$folder" -maxdepth 1 -type d -name "${num}-*" | head -1)"
        [ -n "$match" ] || die "no work item folder for branch '$branch' under $folder/"
        dir="$match"
    fi
    echo "$dir"
}

state_file() { echo "$(work_item_dir)/state.json"; }

# Read a jq path out of state.json, e.g. state_get '.stage'
state_get() {
    local f
    f="$(state_file)"
    [ -f "$f" ] || die "no state.json in $(work_item_dir)"
    jq -r "$1" "$f"
}

# Apply a jq expression to state.json in place.
state_set() {
    local f tmp
    f="$(state_file)"
    tmp="$(mktemp)"
    jq "$1" "$f" > "$tmp" && mv "$tmp" "$f"
}

# Display name from a role file's frontmatter, e.g. role_name developer -> Developer
role_name() {
    local file="$ROLES_DIR/$1.md"
    [ -f "$file" ] || die "no role file for stage '$1' ($file)"
    sed -n 's/^name:[[:space:]]*//p' "$file" | head -1
}

role_tier() {
    local file="$ROLES_DIR/$1.md"
    [ -f "$file" ] || die "no role file for stage '$1' ($file)"
    sed -n 's/^tier:[[:space:]]*//p' "$file" | head -1
}

# The tier a stage actually runs at, after escalation. A stage that has been
# reworked runs one tier deeper: cheap models get first crack, expensive ones
# only see the cases that failed once.
effective_tier() {
    local stage=$1 tier attempts
    tier="$(role_tier "$stage")"
    attempts="$(state_get ".attempts[\"$stage\"] // 0")"
    if [ "$attempts" -ge 1 ]; then
        tier="$(jq -r --arg t "$tier" '.escalation[$t]' "$TIERS_JSON")"
    fi
    echo "$tier"
}

# Resolve the model for a stage. Args: <stage> <harness: claude|codex>
resolve_model() {
    local tier
    tier="$(effective_tier "$1")"
    jq -r --arg t "$tier" --arg h "${2:-claude}" '.tiers[$t][$h]' "$TIERS_JSON"
}

# Does the diff touch user-visible output? Exit 0 if UAT is warranted.
uat_required() {
    local pattern base
    pattern="$(jq -r '.uat_trigger_paths' "$WORKFLOW_JSON")"
    base="origin/main"
    git -C "$REPO_ROOT" rev-parse --verify --quiet "$base" >/dev/null || base="main"
    git -C "$REPO_ROOT" diff --name-only "$base...HEAD" | grep -qE "$pattern"
}
