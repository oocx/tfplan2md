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
