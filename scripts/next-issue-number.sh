#!/usr/bin/env bash
#
# next-issue-number.sh
#
# Determines the next available issue number by checking:
# 1. Local docs folders (docs/features/, docs/issues/, docs/workflow/)
# 2. Remote GitHub branches (feature/NNN-*, fix/NNN-*, workflow/NNN-*)
# 3. Recent copilot/* remote branches (less than one week old)
#
# Returns the next number as a 3-digit zero-padded string (e.g., 033, 034, 135)
#
# Usage:
#   NEXT_NUMBER=$(scripts/next-issue-number.sh)
#   echo "Next issue number: $NEXT_NUMBER"

set -euo pipefail

# Find highest number in local docs folders
find_local_max() {
    local max=0
    
    # Check docs/features/, docs/issues/, docs/workflow/
    for dir in docs/features docs/issues docs/workflow; do
        if [ -d "$dir" ]; then
            for folder in "$dir"/[0-9]*-*/; do
                if [ -e "$folder" ]; then
                    # Extract number from folder name (e.g., 032-my-feature -> 032)
                    num=$(basename "$folder" | grep -oE '^[0-9]+' || true)
                    if [ -n "$num" ]; then
                        # Remove leading zeros for comparison
                        num=$((10#$num))
                        if [ "$num" -gt "$max" ]; then
                            max=$num
                        fi
                    fi
                fi
            done
        fi
    done
    
    echo "$max"
}

# Find highest number in remote branches
find_remote_max() {
    local max=0
    
    # Try to fetch remote branches
    if ! git ls-remote origin 'refs/heads/feature/*' 'refs/heads/fix/*' 'refs/heads/workflow/*' >/dev/null 2>&1; then
        >&2 echo "Warning: Could not fetch from GitHub. Using local data only."
        echo "$max"
        return
    fi
    
    # Get all remote branches and extract numbers
    git ls-remote origin 'refs/heads/feature/*' 'refs/heads/fix/*' 'refs/heads/workflow/*' 2>/dev/null | while read -r hash ref; do
        # Extract branch name from ref (e.g., refs/heads/feature/033-my-feature)
        branch=$(echo "$ref" | sed 's|refs/heads/||')
        # Extract number from branch name
        num=$(echo "$branch" | grep -oE '/[0-9]+' | grep -oE '[0-9]+' || true)
        if [ -n "$num" ]; then
            # Remove leading zeros for comparison
            num=$((10#$num))
            echo "$num"
        fi
    done | sort -n | tail -1 || echo "$max"
}

# Find highest number in recent copilot/* remote branches (less than one week old).
# copilot/* branches are created by GitHub Copilot coding agents and may contain
# in-progress work items under docs/features/, docs/issues/, docs/workflow/.
find_copilot_branch_max() {
    local max=0
    local one_week_ago
    one_week_ago=$(($(date +%s) - 7 * 24 * 3600))

    # Fetch copilot/* branch refs (shallow: only tip commits needed)
    if ! git fetch origin 'refs/heads/copilot/*:refs/remotes/origin/copilot/*' \
            --no-tags --quiet --depth=1 2>/dev/null; then
        >&2 echo "Warning: Could not fetch copilot/* branches (may not exist or network unavailable). Skipping copilot branch scan."
        echo "$max"
        return
    fi

    # For each fetched copilot/* branch, check age and inspect docs folders.
    # Use git for-each-ref to batch-query all commit timestamps in a single call.
    git for-each-ref --format='%(refname:short) %(committerdate:unix)' \
            'refs/remotes/origin/copilot/*' 2>/dev/null | \
    while IFS=' ' read -r remote_ref commit_time; do
        if [ "$commit_time" -lt "$one_week_ago" ]; then
            continue
        fi
        # Emit numbers from work-item docs folders in this branch
        for docdir in docs/features docs/issues docs/workflow; do
            git ls-tree --name-only "$remote_ref:$docdir" 2>/dev/null \
                | grep -oE '^[0-9]+' || true
        done
    done | sort -n | tail -1 || echo "$max"
}

# Main logic
main() {
    local local_max
    local remote_max
    local copilot_max
    local overall_max
    local next_number
    
    local_max=$(find_local_max)
    remote_max=$(find_remote_max)
    copilot_max=$(find_copilot_branch_max)
    
    # Find the maximum across all sources
    overall_max=$local_max
    if [ "$remote_max" -gt "$overall_max" ]; then
        overall_max=$remote_max
    fi
    if [ "$copilot_max" -gt "$overall_max" ]; then
        overall_max=$copilot_max
    fi
    
    # Calculate next number
    next_number=$((overall_max + 1))
    
    # Format as 3-digit zero-padded string
    printf "%03d" "$next_number"
}

main "$@"
