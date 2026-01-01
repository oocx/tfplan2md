#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export PAGER=cat
export GIT_PAGER=cat

# Wrapper around `git diff` to reduce terminal approval friction.
# If no args are provided, default to a concise diffstat.
if [[ $# -eq 0 ]]; then
  git --no-pager diff --stat
else
  git --no-pager diff "$@"
fi
