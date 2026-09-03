#!/usr/bin/env bash
# Verify the agent toolchain required on developer machines.
#
# These tools are NEVER required in CI: they exist to make interactive agent
# sessions cheaper (ast-grep, rtk) and to run the Code Reviewer role in a
# different model family (codex). No GitHub workflow invokes this script.
#
# Exit 0 when everything required is present, 1 otherwise.
set -euo pipefail

# rustup installs into ~/.cargo/bin, which is added to PATH by the shell profile.
# Non-login shells — including the ones agents run commands in — often do not
# source it, so the tools appear missing when they are installed. Look there.
if [ -d "$HOME/.cargo/bin" ] && [[ ":$PATH:" != *":$HOME/.cargo/bin:"* ]]; then
    PATH="$HOME/.cargo/bin:$PATH"
    CARGO_BIN_ADDED=1
fi

missing=0
warned=0

# Report a required tool. $1=command $2=what it is for $3=install hint
require() {
    local cmd=$1 purpose=$2 hint=$3
    if command -v "$cmd" >/dev/null 2>&1; then
        printf '  \033[32mok\033[0m    %-10s %s\n' "$cmd" "$(version_of "$cmd")"
    else
        printf '  \033[31mMISS\033[0m  %-10s %s\n' "$cmd" "$purpose"
        printf '        install: %s\n' "$hint"
        missing=$((missing + 1))
    fi
}

# Report an optional tool.
optional() {
    local cmd=$1 purpose=$2
    if command -v "$cmd" >/dev/null 2>&1; then
        printf '  \033[32mok\033[0m    %-10s %s\n' "$cmd" "$(version_of "$cmd")"
    else
        printf '  \033[33mskip\033[0m  %-10s %s (optional)\n' "$cmd" "$purpose"
        warned=$((warned + 1))
    fi
}

version_of() {
    "$1" --version 2>/dev/null | head -1 | cut -c1-60 || echo "(version unknown)"
}

echo "Agent toolchain"
echo

require ast-grep "structural C# search" "scripts/setup-agent-tools.sh"
require rtk      "compresses command output" "scripts/setup-agent-tools.sh"
require codex    "Code Reviewer role"        "npm install -g @openai/codex"

echo
echo "Already expected in this repo"
require jq  "state.json manipulation" "apt install jq"
require rg  "text search"             "apt install ripgrep"

echo
echo "Optional"
optional code2prompt "token-counted context bundles"

# `sg` is util-linux's setgid binary on most Linux distributions. If a script
# ever calls `sg` expecting ast-grep, it silently runs the wrong program.
if command -v sg >/dev/null 2>&1 && ! sg --version 2>&1 | grep -qi 'ast-grep'; then
    echo
    printf '  \033[33mnote\033[0m  `sg` on this machine is %s, not ast-grep.\n' \
        "$(command -v sg)"
    echo '        Always invoke ast-grep by its full name.'
fi

if [ "${CARGO_BIN_ADDED:-0}" = "1" ]; then
    echo
    printf '  \033[33mnote\033[0m  ~/.cargo/bin is not on PATH in non-login shells.\n'
    echo '        Tools were found only because this script added it. Add this to'
    echo '        your shell profile so agent sessions see them too:'
    echo '            export PATH="$HOME/.cargo/bin:$PATH"'
fi

echo
if [ "$missing" -gt 0 ]; then
    echo "FAIL: $missing required tool(s) missing. Run scripts/setup-agent-tools.sh"
    exit 1
fi

echo "OK: agent toolchain complete${warned:+ ($warned optional tool(s) absent)}"
