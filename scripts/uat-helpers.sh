#!/usr/bin/env bash
# Shared UAT helper functions (artifact validation, etc.)
# Intended to be sourced from scripts/uat-*.sh

set -euo pipefail

# Prevent interactive pagers from blocking automation
export PAGER="${PAGER:-cat}"

# Reuse log helpers defined in calling scripts; if they don't exist, provide no-op functions
if ! declare -F log_info >/dev/null 2>&1; then
  log_info() { :; }
fi
if ! declare -F log_warn >/dev/null 2>&1; then
  log_warn() { :; }
fi
if ! declare -F log_error >/dev/null 2>&1; then
  # log_error should at least write to stderr
  log_error() { echo "$*" >&2; }
fi

# validate_artifact <platform> <artifact-path-or-empty> <force:false|true>
# Returns: echoes the resolved artifact path on success; returns non-zero on failure
validate_artifact() {
    local platform="${1:-}"
    local artifact="${2:-}"
    local force="${3:-false}"

    if [[ -z "$platform" ]]; then
        log_error "validate_artifact: missing platform argument (github|azdo)"
        return 2
    fi

    # Apply platform-specific defaults
    case "$platform" in
        github)
            if [[ -z "$artifact" ]]; then
                artifact="artifacts/comprehensive-demo-simple-diff.md"
                log_info "No artifact specified, using GitHub default: $artifact" >&2
            fi
            ;;
        azdo)
            if [[ -z "$artifact" ]]; then
                artifact="artifacts/comprehensive-demo.md"
                log_info "No artifact specified, using Azure DevOps default: $artifact" >&2
            fi
            ;;
        *)
            log_error "Unknown platform: $platform"
            return 2
            ;;
    esac

    # Check file existence
    if [[ ! -f "$artifact" ]]; then
        log_error "Artifact not found: $artifact"
        return 1
    fi

    # Block known minimal artifacts unless force is set
    if [[ "$artifact" =~ (simulation|uat-simulation|minimal|uat-minimal) ]] && [[ "$force" != "true" ]]; then
        log_error "Artifact appears to be a minimal/test artifact and should not be used for UAT: $artifact"
        log_error "Pick a real feature/comprehensive artifact, or override with UAT_FORCE=true."
        return 1
    fi

    log_info "✓ Using artifact: $artifact" >&2
    printf '%s\n' "$artifact"
}

# ---------------------------------------------------------------------------
# ensure_azdo_credential_helper <submodule-path>
#
# In WSL, the global git credential.helper often points to a Windows .exe
# (e.g. git-credential-manager.exe) that cannot execute natively, causing
# git push/fetch to hang indefinitely.
#
# This function detects the broken state and:
#   1. Attempts to re-register WSL interop so the Windows helper works
#   2. Falls back to a local Azure CLI credential helper if interop can't be fixed
# ---------------------------------------------------------------------------
ensure_azdo_credential_helper() {
    local submodule_path="${1:?ensure_azdo_credential_helper: submodule path required}"

    # Read the global credential.helper
    local global_helper
    global_helper="$(git config --global credential.helper 2>/dev/null || echo "")"

    # Nothing to fix if there is no global helper or it is not a Windows binary
    if [[ -z "$global_helper" ]] || [[ "$global_helper" != *".exe"* ]]; then
        return 0
    fi

    # Check if the Windows binary is actually executable (WSL interop working)
    local expanded_helper="${global_helper//\\ / }"
    if "$expanded_helper" --version >/dev/null 2>&1; then
        return 0
    fi

    # WSL interop is broken — try to re-register it
    log_warn "Windows credential helper cannot execute (WSL interop missing)."
    if [[ -f /proc/sys/fs/binfmt_misc/register ]]; then
        log_info "Attempting to re-register WSL interop..."
        if sudo sh -c 'echo :WSLInterop:M::MZ::/init:PF > /proc/sys/fs/binfmt_misc/register' 2>/dev/null; then
            # Verify it worked
            if "$expanded_helper" --version >/dev/null 2>&1; then
                log_info "WSL interop restored. Windows credential helper is working."
                return 0
            fi
        fi
    fi

    # Interop fix failed — fall back to Azure CLI credential helper
    local helpers_dir
    helpers_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    local helper_script="$helpers_dir/azdo-credential-helper.sh"

    if [[ ! -x "$helper_script" ]]; then
        log_error "Cannot fix credentials: WSL interop broken and fallback helper missing: $helper_script"
        log_error "Fix manually: sudo sh -c 'echo :WSLInterop:M::MZ::/init:PF > /proc/sys/fs/binfmt_misc/register'"
        return 1
    fi

    # Already configured?
    local current_local
    current_local="$(git -C "$submodule_path" config --local --get-all credential.helper 2>/dev/null | tail -n 1 || echo "")"
    if [[ "$current_local" == "$helper_script" ]]; then
        return 0
    fi

    log_info "Configuring Azure CLI credential helper as fallback for $submodule_path"
    git -C "$submodule_path" config --local --unset-all credential.helper 2>/dev/null || true
    git -C "$submodule_path" config --local --add credential.helper ""
    git -C "$submodule_path" config --local --add credential.helper "$helper_script"
}
