#!/usr/bin/env bash
#
# next-issue-number.sh
#
# Determines the next available issue number by checking:
# 1. Local docs folders (docs/features/, docs/issues/, docs/workflow/)
# 2. Remote GitHub branches (feature/NNN-*, fix/NNN-*, workflow/NNN-*)
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

# Main logic
main() {
    local local_max
    local remote_max
    local overall_max
    local next_number
    
    local_max=$(find_local_max)
    remote_max=$(find_remote_max)
    
    # Find the maximum of both
    if [ "$local_max" -gt "$remote_max" ]; then
        overall_max=$local_max
    else
        overall_max=$remote_max
    fi
    
    # Calculate next number
    next_number=$((overall_max + 1))
    
    # Format as 3-digit zero-padded string
    printf "%03d" "$next_number"
}

main "$@"
