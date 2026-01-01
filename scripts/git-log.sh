#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export PAGER=cat
export GIT_PAGER=cat

# Wrapper around `git log` to reduce terminal approval friction.
# If no args are provided, default to a compact recent history view.
if [[ $# -eq 0 ]]; then
  git --no-pager log --oneline -20
else
  git --no-pager log "$@"
fi
