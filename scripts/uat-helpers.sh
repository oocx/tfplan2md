#!/usr/bin/env bash
# Shared UAT helper functions (artifact validation, auth setup, freshness checks, submodule init, etc.)
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

# ---------------------------------------------------------------------------
# ensure_git_submodules
#
# Ensures both UAT git submodules are initialized. Called automatically by
# uat-run.sh before any UAT operations so agents do not need to do this
# manually.
#
# Submodule paths default to:
#   uat-repos/github  (override: UAT_GITHUB_SUBMODULE_PATH)
#   uat-repos/azdo    (override: AZDO_SUBMODULE_PATH)
# ---------------------------------------------------------------------------
ensure_git_submodules() {
    local github_path="${UAT_GITHUB_SUBMODULE_PATH:-uat-repos/github}"
    local azdo_path="${AZDO_SUBMODULE_PATH:-uat-repos/azdo}"

    local needs_init=false
    if [[ ! -e "$github_path/.git" ]]; then
        needs_init=true
    fi
    if [[ ! -e "$azdo_path/.git" ]]; then
        needs_init=true
    fi

    if [[ "$needs_init" == "false" ]]; then
        return 0
    fi

    log_info "Initializing UAT git submodules..."
    if ! git submodule update --init --recursive 2>&1; then
        log_error "Failed to initialize git submodules."
        log_error ""
        log_error "Remediation:"
        log_error "  Run: git submodule update --init --recursive"
        log_error "  Ensure you have network access and valid credentials for:"
        log_error "    - GitHub: $github_path"
        log_error "    - Azure DevOps: $azdo_path"
        return 1
    fi

    log_info "✓ UAT git submodules initialized"
}

# ---------------------------------------------------------------------------
# is_coding_agent_env
#
# Returns 0 (true) if running in a GitHub Copilot coding agent environment.
# Detection methods:
#   1. Current branch name starts with "copilot/"
#   2. GH_UAT_TOKEN or AZDO_UAT_TOKEN environment variables are present
# ---------------------------------------------------------------------------
is_coding_agent_env() {
    local current_branch
    current_branch="$(git branch --show-current 2>/dev/null || echo "")"
    if [[ "$current_branch" == copilot/* ]]; then
        return 0
    fi
    if [[ -n "${GH_UAT_TOKEN:-}" || -n "${AZDO_UAT_TOKEN:-}" ]]; then
        return 0
    fi
    return 1
}

# ---------------------------------------------------------------------------
# ensure_github_credential_helper
#
# Ensures GitHub CLI is authenticated for UAT git push operations.
#
# In coding agent environments (copilot/* branch or PAT vars present):
#   Uses GH_UAT_TOKEN to authenticate gh CLI and configure git credential helper.
#
# In local environments:
#   No action needed (relies on existing gh auth login).
# ---------------------------------------------------------------------------
ensure_github_credential_helper() {
    if ! is_coding_agent_env; then
        # Local development: rely on existing gh auth
        return 0
    fi

    if [[ -z "${GH_UAT_TOKEN:-}" ]]; then
        log_error "GH_UAT_TOKEN is not set in the coding agent environment."
        log_error ""
        log_error "Remediation:"
        log_error "  1. Create a token with repo scope"
        log_error "  2. Add secret: GH_UAT_TOKEN = <GitHub PAT with 'repo' scope on oocx/tfplan2md-uat>"
        log_error "  3. Export it in your shell before running UAT"
        log_error "  4. Re-run the UAT command"
        return 1
    fi

    # Always re-authenticate with the UAT token in coding agent environments.
    # The default GITHUB_TOKEN (ghu_...) only has access to the main repo, not
    # to oocx/tfplan2md-uat. We must ensure the UAT PAT is the active credential.

    # If GH_TOKEN is already set to the UAT value (set by an earlier call in the
    # parent process), all gh operations will already use the correct token.
    # Skip gh auth login in this case to avoid the "GH_TOKEN is being used"
    # error that gh emits when env tokens are present.
    if [[ "${GH_TOKEN:-}" == "${GH_UAT_TOKEN}" ]]; then
        log_info "✓ GH_TOKEN already configured for UAT repo access (coding agent mode)"
        return 0
    fi

    # Note: GITHUB_TOKEN and GH_TOKEN env vars block gh auth login --with-token.
    # Unset both so gh stores the credential.
    log_info "Configuring GitHub CLI with GH_UAT_TOKEN for UAT repo access (coding agent mode)..."
    if ! env -u GITHUB_TOKEN -u GH_TOKEN gh auth login --with-token <<< "${GH_UAT_TOKEN}" 2>&1; then
        log_error "Failed to authenticate GitHub CLI with GH_UAT_TOKEN."
        log_error ""
        log_error "Remediation:"
        log_error "  - Verify GH_UAT_TOKEN has 'repo' scope on oocx/tfplan2md-uat"
        log_error "  - Verify the token has not expired"
        log_error "  - Check that GH_UAT_TOKEN is exported in this shell"
        return 1
    fi

    # Configure the UAT submodule's local git credential helper to bypass GITHUB_TOKEN.
    # When GITHUB_TOKEN env var is set, gh auth git-credential uses it (the default
    # Actions token with no UAT repo access) rather than the stored PAT. Using
    # env -u GITHUB_TOKEN ensures gh uses the stored PAT instead.
    local github_submodule="${UAT_GITHUB_SUBMODULE_PATH:-uat-repos/github}"
    if [[ -e "$github_submodule/.git" ]]; then
        git -C "$github_submodule" config --local --unset-all "credential.https://github.com.helper" 2>/dev/null || true
        git -C "$github_submodule" config --local "credential.https://github.com.helper" ""
        git -C "$github_submodule" config --local --add "credential.https://github.com.helper" \
            '!env -u GITHUB_TOKEN /usr/bin/gh auth git-credential'
        log_info "✓ UAT submodule credential helper configured (bypasses GITHUB_TOKEN)"
    fi

    # Export GH_TOKEN (preferred over GITHUB_TOKEN by gh CLI) and unset GITHUB_TOKEN
    # so all subsequent gh operations in the calling script use the UAT PAT.
    # This is required because GITHUB_TOKEN is set to the default Actions integration
    # token, which does not have access to oocx/tfplan2md-uat.
    export GH_TOKEN="${GH_UAT_TOKEN}"
    unset GITHUB_TOKEN

    log_info "✓ GitHub CLI authenticated and git credential helper configured (coding agent mode)"
    return 0
}

# ---------------------------------------------------------------------------
# check_artifact_freshness <artifact-path>
#
# Validates that the artifact was generated from the current version of
# tfplan2md. Extracts the git commit hash embedded in the artifact header
# and checks if any source files (*.cs, *.sbn, *.csproj) changed since
# that commit.
#
# Returns 0 if the artifact is up-to-date, 1 if it is stale.
# Warns (and returns 0) if the commit hash cannot be verified.
# ---------------------------------------------------------------------------
check_artifact_freshness() {
    local artifact="${1:?check_artifact_freshness: artifact path required}"

    if [[ ! -f "$artifact" ]]; then
        log_error "Artifact not found: $artifact"
        return 1
    fi

    # Extract the git commit hash from the artifact header.
    # Expected format: "Generated by tfplan2md X.Y.Z (HASH) on ..."
    local artifact_hash
    artifact_hash="$(grep -m1 "^Generated by tfplan2md" "$artifact" 2>/dev/null \
        | grep -oE '\([0-9a-f]+\)' | tr -d '()' || echo "")"

    if [[ -z "$artifact_hash" ]]; then
        log_warn "Cannot verify freshness of '$artifact': no commit hash found in header."
        log_warn "Ensure the artifact was generated by tfplan2md (not manually created)."
        return 0
    fi

    # Resolve the short hash to a full commit hash
    local artifact_full_hash
    artifact_full_hash="$(git rev-parse "${artifact_hash}^{commit}" 2>/dev/null || echo "")"

    if [[ -z "$artifact_full_hash" ]]; then
        log_warn "Commit '$artifact_hash' from '$artifact' is not in local git history."
        log_warn "Cannot verify freshness. Proceeding anyway."
        return 0
    fi

    # Check if the artifact commit matches HEAD
    local current_full_hash
    current_full_hash="$(git rev-parse HEAD 2>/dev/null || echo "")"

    if [[ "$artifact_full_hash" == "$current_full_hash" ]]; then
        log_info "✓ Artifact '$artifact' is up-to-date (commit: $artifact_hash)"
        return 0
    fi

    # Check if HEAD is behind the artifact commit (artifact is from a newer commit)
    if git merge-base --is-ancestor HEAD "$artifact_full_hash" 2>/dev/null; then
        log_info "✓ Artifact '$artifact' is up-to-date (artifact commit $artifact_hash is newer than HEAD)"
        return 0
    fi

    # Check if any source files changed between the artifact commit and HEAD
    local changed_src
    changed_src="$(git diff --name-only "${artifact_full_hash}..HEAD" -- 'src/' 2>/dev/null \
        | grep -E '\.(cs|sbn|csproj)$' | head -5 || echo "")"

    if [[ -n "$changed_src" ]]; then
        log_error "Artifact '$artifact' is outdated."
        log_error "Built with commit '$artifact_hash', but source files changed since then:"
        while IFS= read -r changed_file; do
            log_error "  - $changed_file"
        done <<< "$changed_src"
        log_error ""
        log_error "Remediation:"
        log_error "  1. Regenerate the comprehensive demo artifacts:"
        log_error "       scripts/generate-demo-artifacts.sh"
        log_error "  2. For feature-specific artifacts, regenerate using:"
        log_error "       dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \\"
        log_error "         [your args] --output <artifact-path>"
        log_error "  3. Commit the updated artifacts and re-run UAT"
        return 1
    fi

    log_info "✓ Artifact '$artifact' is up-to-date (commit: $artifact_hash, no source changes since then)"
    return 0
}

# validate_artifact <platform> <artifact-path-or-empty> [simulate:false|true] [force:false|true]
# Returns: echoes the resolved artifact path on success; returns non-zero on failure
validate_artifact() {
    local platform="${1:-}"
    local artifact="${2:-}"
    local simulate="false"
    local force="false"

    # Handle variable number of arguments (backwards compatibility)
    if [[ $# -eq 3 ]]; then
        # Called as: validate_artifact <platform> <artifact> <force>
        force="$3"
    elif [[ $# -ge 4 ]]; then
        # Called as: validate_artifact <platform> <artifact> <simulate> <force>
        simulate="$3"
        force="$4"
    fi

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

    # Block known minimal artifacts unless simulate or force is set
    if [[ "$artifact" =~ (simulation|uat-simulation|minimal|uat-minimal) ]] && [[ "$simulate" != "true" ]] && [[ "$force" != "true" ]]; then
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
# Ensures git credentials are configured for Azure DevOps operations.
#
# In coding agent environments (copilot/* branch or PAT vars present):
#   Uses AZDO_UAT_TOKEN (or AZURE_DEVOPS_EXT_PAT) to configure git credentials
#   for the submodule. Maps AZDO_UAT_TOKEN → AZURE_DEVOPS_EXT_PAT if needed.
#
# In WSL: The global git credential.helper often points to a Windows .exe
# (e.g. git-credential-manager.exe) that cannot execute natively, causing
# git push/fetch to hang indefinitely.
#   1. Re-registers WSL interop or falls back to Azure CLI helper.
#
# In local dev (non-WSL): No action needed (working credentials assumed).
# ---------------------------------------------------------------------------
ensure_azdo_credential_helper() {
    local submodule_path="${1:?ensure_azdo_credential_helper: submodule path required}"

    if is_coding_agent_env; then
        # Coding agent environment: use AZDO_UAT_TOKEN or AZURE_DEVOPS_EXT_PAT

        # Map AZDO_UAT_TOKEN → AZURE_DEVOPS_EXT_PAT if needed
        if [[ -z "${AZURE_DEVOPS_EXT_PAT:-}" && -n "${AZDO_UAT_TOKEN:-}" ]]; then
            export AZURE_DEVOPS_EXT_PAT="${AZDO_UAT_TOKEN}"
            log_info "Using AZDO_UAT_TOKEN as AZURE_DEVOPS_EXT_PAT (coding agent mode)"
        fi

        if [[ -z "${AZURE_DEVOPS_EXT_PAT:-}" ]]; then
            log_error "Neither AZURE_DEVOPS_EXT_PAT nor AZDO_UAT_TOKEN is set."
            log_error ""
            log_error "Remediation:"
            log_error "  1. Create a token with repo scope"
            log_error "  2. Add secret: AZDO_UAT_TOKEN = <Azure DevOps PAT with 'Code (Read & Write)' scope>"
            log_error "  3. Export it in your shell before running UAT"
            log_error "  4. Re-run the UAT command"
            return 1
        fi

        # Configure git credentials for Azure DevOps git push in this submodule
        git -C "$submodule_path" config --local credential.https://dev.azure.com.username token
        git -C "$submodule_path" config --local credential.https://dev.azure.com.helper \
            '!f() { echo "password=${AZURE_DEVOPS_EXT_PAT}"; }; f'
        log_info "✓ Azure DevOps credentials configured (coding agent mode)"
        return 0
    fi

    # Local development environment: WSL handling
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
