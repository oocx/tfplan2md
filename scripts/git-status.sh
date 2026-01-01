#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export PAGER=cat
export GIT_PAGER=cat

# Wrapper around `git status` to reduce terminal approval friction.
# If no args are provided, default to the common compact form.
if [[ $# -eq 0 ]]; then
  git status -sb
else
  git status "$@"
fi
