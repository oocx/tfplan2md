#!/usr/bin/env bash
# Install the agent toolchain on a developer machine.
#
# NOT for CI. Nothing in .github/workflows/ calls this, and nothing in CI needs
# the tools it installs. See scripts/agent-doctor.sh for what is required and why.
#
# Usage:
#   scripts/setup-agent-tools.sh            # install anything missing
#   scripts/setup-agent-tools.sh --dry-run  # print what would happen
set -euo pipefail

DRY_RUN=0
[ "${1:-}" = "--dry-run" ] && DRY_RUN=1

run() {
    if [ "$DRY_RUN" -eq 1 ]; then
        echo "  would run: $*"
    else
        echo "  + $*"
        "$@"
    fi
}

have() { command -v "$1" >/dev/null 2>&1; }

echo "Agent toolchain setup"
[ "$DRY_RUN" -eq 1 ] && echo "(dry run — nothing will be installed)"
echo

# --- Rust toolchain -------------------------------------------------------
# ast-grep is distributed as a cargo crate. rustup is the least surprising way
# to get a toolchain that can build it on any distribution.
if have cargo; then
    echo "cargo: present"
else
    echo "cargo: absent — installing rustup"
    if [ "$DRY_RUN" -eq 1 ]; then
        echo "  would run: curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y"
    else
        curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
        # shellcheck disable=SC1091
        . "$HOME/.cargo/env"
    fi
fi

# --- ast-grep -------------------------------------------------------------
# Structural search over C#: returns matched AST nodes instead of whole files.
if have ast-grep; then
    echo "ast-grep: present"
else
    echo "ast-grep: installing"
    run cargo install ast-grep --locked
fi

# --- rtk ------------------------------------------------------------------
# Compresses command output (git, tests, lint, gh, docker) before it reaches
# the model's context.
if have rtk; then
    echo "rtk: present"
else
    echo "rtk: installing"
    run cargo install --git https://github.com/rtk-ai/rtk --locked
fi

# --- codex ----------------------------------------------------------------
# Runs the Code Reviewer role in a different model family, so review does not
# inherit the author's blind spots.
if have codex; then
    echo "codex: present"
else
    echo "codex: installing"
    run npm install -g @openai/codex
fi

echo
echo "Next steps (not automated — they change global config):"
echo
echo "  1. Register RTK's Claude Code hook:      rtk init -g"
echo "  2. Enable tee mode so full output survives on failure, and exclude the"
echo "     reviewer's diff path, in ~/.config/rtk/config.toml — a truncated diff"
echo "     produces a confident review of code the reviewer never saw."
echo "  3. Authenticate codex:                   codex login"
echo
echo "Then verify:  scripts/agent-doctor.sh"
