# ADR-003: Release Workflow Integration for Homebrew Formula Updates

## Status

Proposed

## Context

Feature 089 requires automating Homebrew formula updates as part of the release workflow. When a new stable version is released, the workflow must:

1. Extract SHA256 checksums from the consolidated `SHA256SUMS` artifact
2. Clone the Homebrew tap repository (`oocx/homebrew-tfplan2md`)
3. Update `Formula/tfplan2md.rb` with new version and checksums
4. Commit and push changes to the tap repository

This must happen **only for stable releases** (not prereleases) and **after successful binary builds** (dependency on build-binaries and consolidate-checksums jobs).

### Requirements from Specification

- **Automated updates**: No manual formula editing required
- **Skip prereleases**: Formula updates only for stable releases (e.g., `v1.2.0`, not `v1.2.0-beta.1`)
- **Secure authentication**: Use `HOMEBREW_TAP_TOKEN` GitHub secret for tap repository access
- **Graceful degradation**: Formula update failures should NOT block GitHub Release creation
- **Update latency**: Formula should be updated within 5 minutes of release publication

### Current Workflow State

The release workflow (`.github/workflows/release.yml`) has:

1. **release job**: Creates GitHub Release (lines 111-205)
2. **build-binaries job**: Builds multi-platform binaries using matrix strategy (lines 206-327)
3. **consolidate-checksums job**: Combines individual checksums into `SHA256SUMS` artifact (lines 357-395)

The new `update-homebrew-formula` job will run after `consolidate-checksums` and depend on both `release` and `consolidate-checksums` jobs.

## Options Considered

### Option 1: Dedicated Job with Script-Based Update (RECOMMENDED)

**Description**: Add a new job `update-homebrew-formula` that runs a shell script to update the formula.

**Workflow Structure**:

```yaml
update-homebrew-formula:
  name: Update Homebrew Formula
  runs-on: ubuntu-latest
  needs: [release, consolidate-checksums]
  # Only run for stable releases (not prereleases)
  if: needs.release.outputs.version != '' && needs.release.outputs.is_prerelease != 'true'
  
  steps:
    - name: Checkout tfplan2md repository
      uses: actions/checkout@v6

    - name: Download consolidated checksums
      uses: actions/download-artifact@v4
      with:
        name: checksums
        path: checksums

    - name: Checkout Homebrew tap repository
      uses: actions/checkout@v6
      with:
        repository: oocx/homebrew-tfplan2md
        token: ${{ secrets.HOMEBREW_TAP_TOKEN }}
        path: homebrew-tap

    - name: Update formula
      run: |
        bash scripts/update-homebrew-formula.sh \
          "${{ needs.release.outputs.version }}" \
          "checksums/SHA256SUMS" \
          "homebrew-tap/Formula/tfplan2md.rb"

    - name: Commit and push formula update
      working-directory: homebrew-tap
      run: |
        git config user.name "github-actions[bot]"
        git config user.email "github-actions[bot]@users.noreply.github.com"
        git add Formula/tfplan2md.rb
        git diff --staged --quiet || git commit -m "chore: update formula to v${{ needs.release.outputs.version }}"
        git push
```

**Pros**:
- ✅ **Clear separation**: Homebrew update is a distinct job (easy to monitor and debug)
- ✅ **Reusable script**: `scripts/update-homebrew-formula.sh` can be tested locally
- ✅ **Graceful degradation**: If job fails, release still succeeds (no blocker)
- ✅ **Skip prereleases**: `if` condition explicitly checks `is_prerelease != 'true'`
- ✅ **Idempotent**: `git diff --staged --quiet ||` only commits if changes exist
- ✅ **Proper authentication**: Uses `HOMEBREW_TAP_TOKEN` for tap repository access

**Cons**:
- ⚠️ **Additional workflow complexity**: Adds one more job to release workflow
- ⚠️ **Requires secret**: `HOMEBREW_TAP_TOKEN` must be configured in repository secrets

**Trade-offs**: Minor complexity increase is acceptable for clean automation.

---

### Option 2: Inline Script within Consolidate-Checksums Job

**Description**: Add formula update steps directly to `consolidate-checksums` job.

**Pros**:
- ✅ **Fewer jobs**: Reduces overall job count in workflow

**Cons**:
- ❌ **Tight coupling**: Mixes checksum consolidation with Homebrew-specific logic
- ❌ **Poor separation of concerns**: `consolidate-checksums` becomes multi-purpose
- ❌ **Harder to debug**: Formula update errors mixed with checksum errors
- ❌ **Cannot skip independently**: If formula update fails, checksum job fails

**Decision**: Rejected. Violates single responsibility principle.

---

### Option 3: Separate Workflow Triggered by Release Event

**Description**: Create `.github/workflows/update-homebrew.yml` triggered by `release` webhook.

**Pros**:
- ✅ **Complete separation**: Homebrew logic entirely separate from release workflow
- ✅ **Can retry independently**: Rerun workflow without rerunning release

**Cons**:
- ❌ **Race condition risk**: May run before checksums are uploaded to release
- ❌ **Additional complexity**: Need to download checksums from GitHub Release API
- ❌ **Harder to track**: Workflow runs in separate job graph
- ❌ **Duplicate condition logic**: Must reimplement prerelease check

**Decision**: Rejected. Dependency on checksums makes in-workflow approach cleaner.

---

### Option 4: GitHub Actions Marketplace Homebrew Action

**Description**: Use a third-party GitHub Action for Homebrew formula updates.

**Pros**:
- ✅ **Less custom code**: Leverage existing action

**Cons**:
- ❌ **No suitable action exists**: Most Homebrew actions are for installing brew, not updating formulae
- ❌ **Security risk**: Third-party action has access to `HOMEBREW_TAP_TOKEN`
- ❌ **Less control**: Harder to customize for our specific needs

**Decision**: Rejected. Custom script provides better control and security.

---

## Decision

**Implement Option 1: Dedicated Job with Script-Based Update**

Add `update-homebrew-formula` job to `.github/workflows/release.yml` that:
- Runs after `consolidate-checksums` job
- Only executes for stable releases (skips prereleases)
- Uses `scripts/update-homebrew-formula.sh` for update logic
- Authenticates to tap repository with `HOMEBREW_TAP_TOKEN` secret
- Commits and pushes formula changes with conventional commit message

## Rationale

1. **Clear separation of concerns**: Homebrew update is logically separate from checksum consolidation. Dedicated job makes this explicit.

2. **Graceful degradation**: If the job fails (e.g., network issue, authentication error), the GitHub Release still succeeds. Users can install binaries directly; Homebrew formula can be updated manually if needed.

3. **Testable script**: Extracting update logic into `scripts/update-homebrew-formula.sh` allows local testing and reuse outside the workflow.

4. **Proper dependency chain**: The job explicitly declares dependencies (`needs: [release, consolidate-checksums]`), ensuring checksums are available before attempting update.

5. **Idempotent commits**: Using `git diff --staged --quiet ||` ensures we only commit if the formula actually changed (useful for re-runs).

## Implementation Details

### Workflow Job Specification

Add the following job to `.github/workflows/release.yml` (after line 395, after consolidate-checksums job):

```yaml
  update-homebrew-formula:
    name: Update Homebrew Formula
    runs-on: ubuntu-latest
    needs: [release, consolidate-checksums]
    # Only run for stable releases (skip prereleases)
    if: needs.release.outputs.version != '' && needs.release.outputs.is_prerelease != 'true'
    
    steps:
      - name: Checkout tfplan2md repository
        uses: actions/checkout@v6

      - name: Download consolidated checksums
        uses: actions/download-artifact@v4
        with:
          name: checksums
          path: checksums

      - name: Checkout Homebrew tap repository
        uses: actions/checkout@v6
        with:
          repository: oocx/homebrew-tfplan2md
          token: ${{ secrets.HOMEBREW_TAP_TOKEN }}
          path: homebrew-tap

      - name: Update formula
        run: |
          bash scripts/update-homebrew-formula.sh \
            "${{ needs.release.outputs.version }}" \
            "checksums/SHA256SUMS" \
            "homebrew-tap/Formula/tfplan2md.rb"

      - name: Commit and push formula update
        working-directory: homebrew-tap
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add Formula/tfplan2md.rb
          # Only commit if there are changes
          if ! git diff --staged --quiet; then
            git commit -m "chore: update formula to v${{ needs.release.outputs.version }}"
            git push
            echo "✅ Homebrew formula updated to v${{ needs.release.outputs.version }}"
          else
            echo "ℹ️  No changes to formula (already up-to-date)"
          fi
```

### Update Script Implementation

Create `scripts/update-homebrew-formula.sh` (as specified in ADR-002):

```bash
#!/bin/bash
set -euo pipefail

VERSION="$1"
CHECKSUMS_FILE="$2"
FORMULA_FILE="$3"

echo "Updating Homebrew formula to version $VERSION"

# Extract checksums for each platform
echo "Extracting checksums from $CHECKSUMS_FILE..."
MACOS_X64_SHA=$(grep "macos-x64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')
MACOS_ARM64_SHA=$(grep "macos-arm64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')
LINUX_X64_SHA=$(grep "linux-x64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')

# Validate all checksums are present
if [ -z "$MACOS_X64_SHA" ] || [ -z "$MACOS_ARM64_SHA" ] || [ -z "$LINUX_X64_SHA" ]; then
  echo "❌ Error: Missing checksums in $CHECKSUMS_FILE"
  echo "Contents of $CHECKSUMS_FILE:"
  cat "$CHECKSUMS_FILE"
  exit 1
fi

# Validate checksum format (64 hex characters)
for sha in "$MACOS_X64_SHA" "$MACOS_ARM64_SHA" "$LINUX_X64_SHA"; do
  if ! echo "$sha" | grep -qE '^[a-f0-9]{64}$'; then
    echo "❌ Error: Invalid checksum format: $sha"
    exit 1
  fi
done

echo "Checksums validated:"
echo "  macOS x64: $MACOS_X64_SHA"
echo "  macOS ARM64: $MACOS_ARM64_SHA"
echo "  Linux x64: $LINUX_X64_SHA"

# Update formula file with version and checksums
echo "Updating $FORMULA_FILE..."
sed -i "s/{{VERSION}}/$VERSION/g" "$FORMULA_FILE"
sed -i "s/{{MACOS_X64_SHA256}}/$MACOS_X64_SHA/g" "$FORMULA_FILE"
sed -i "s/{{MACOS_ARM64_SHA256}}/$MACOS_ARM64_SHA/g" "$FORMULA_FILE"
sed -i "s/{{LINUX_X64_SHA256}}/$LINUX_X64_SHA/g" "$FORMULA_FILE"

echo "✅ Formula updated to version $VERSION"
```

**Notes**:
- Script uses `sed -i` for in-place replacement (works on Linux; macOS requires `sed -i ''` but workflow runs on ubuntu-latest)
- Exit code 1 on validation failure causes workflow step to fail (expected behavior)
- Verbose output helps with debugging in workflow logs

### Required GitHub Secret

**Secret Name**: `HOMEBREW_TAP_TOKEN`

**Scope**: Personal Access Token (classic) with `repo` scope OR Fine-grained PAT with:
- Repository access: `oocx/homebrew-tfplan2md`
- Permissions: `Contents: Read and write`

**Configuration**:
1. Create PAT in GitHub Settings → Developer settings → Personal access tokens
2. Add to repository secrets: Settings → Secrets and variables → Actions → New repository secret
3. Name: `HOMEBREW_TAP_TOKEN`
4. Value: `ghp_...` (the generated token)

**Security Notes**:
- Token is only used by `update-homebrew-formula` job (least privilege)
- Token is NOT exposed in logs (`${{ secrets.HOMEBREW_TAP_TOKEN }}` is masked)
- Token only grants access to tap repository (not main repository)

### Prerelease Handling

The `if` condition ensures formula is NOT updated for prereleases:

```yaml
if: needs.release.outputs.version != '' && needs.release.outputs.is_prerelease != 'true'
```

**Examples**:
- `v1.2.0` → Job runs, formula updated ✅
- `v1.2.0-beta.1` → Job skipped, formula unchanged ❌
- `v1.2.0-rc.2` → Job skipped, formula unchanged ❌

**Rationale**: Homebrew users expect stable versions. Prereleases can be installed manually via direct binary download.

### Error Handling and Rollback

**Formula Update Failure Scenarios**:

1. **Missing checksums in SHA256SUMS**:
   - Script exits with error code 1
   - Workflow step fails and job fails
   - GitHub Release is NOT affected (job is independent)
   - Manual fix: Update formula manually or re-run workflow

2. **Invalid checksum format**:
   - Script exits with error code 1
   - Same handling as scenario 1

3. **Network error during git push**:
   - `git push` fails with exit code 1
   - Job fails, but GitHub Release succeeds
   - Manual fix: Re-run workflow job or push formula update manually

4. **Authentication failure** (`HOMEBREW_TAP_TOKEN` invalid):
   - `git push` fails with "Authentication failed"
   - Job fails, but GitHub Release succeeds
   - Manual fix: Update token in repository secrets and re-run

**Rollback Strategy**:

If a bad formula is pushed (e.g., incorrect checksums), rollback by:

1. Manually revert commit in tap repository:
   ```bash
   cd homebrew-tap
   git revert HEAD
   git push
   ```

2. Or manually edit formula with correct values:
   ```bash
   cd homebrew-tap
   # Edit Formula/tfplan2md.rb manually
   git add Formula/tfplan2md.rb
   git commit -m "fix: correct checksums for v1.2.0"
   git push
   ```

**Prevention**: The script's validation logic (checksum presence and format checks) prevents most errors before pushing.

## Testing Strategy

### Pre-Production Testing

Before merging Homebrew support:

1. **Create test tap repository**: `oocx/homebrew-tfplan2md-test`
2. **Configure test secret**: `HOMEBREW_TAP_TOKEN_TEST` in repository secrets
3. **Modify workflow** to use test tap for a test release:
   ```yaml
   repository: oocx/homebrew-tfplan2md-test
   token: ${{ secrets.HOMEBREW_TAP_TOKEN_TEST }}
   ```
4. **Trigger test release**: Create a test tag (e.g., `v0.0.0-brew-test`)
5. **Verify formula update**: Check test tap repository for committed changes
6. **Test installation**: 
   ```bash
   brew tap oocx/tfplan2md-test
   brew install tfplan2md
   tfplan2md --version
   ```

### Production Validation

After first production release with Homebrew support:

1. **Monitor workflow**: Check `update-homebrew-formula` job in Actions tab
2. **Verify formula**: Check `oocx/homebrew-tfplan2md` repository for new commit
3. **Test installation**:
   ```bash
   brew tap oocx/tfplan2md
   brew install tfplan2md
   tfplan2md --version
   ```
4. **Verify checksums match**:
   ```bash
   # Download release archive
   curl -LO https://github.com/oocx/tfplan2md/releases/download/v1.2.0/tfplan2md_1.2.0_macos-x64.tar.gz
   # Verify checksum matches formula
   shasum -a 256 tfplan2md_1.2.0_macos-x64.tar.gz
   # Compare with formula's sha256 value
   ```

### Continuous Monitoring

For each subsequent release:

1. Check workflow run status (should be green ✅)
2. Spot-check formula version in tap repository
3. Periodically test installation on macOS and Linux

## Consequences

### Positive

- ✅ **Fully automated**: No manual formula editing required for releases
- ✅ **Fast updates**: Formula updated within ~2-5 minutes of release
- ✅ **Reliable**: Script validates checksums before pushing
- ✅ **Transparent**: All updates tracked in tap repository git history
- ✅ **Graceful degradation**: Formula update failures don't block releases
- ✅ **Idempotent**: Safe to re-run workflow if update fails

### Negative

- ⚠️ **Secret dependency**: Requires `HOMEBREW_TAP_TOKEN` to be configured and maintained
- ⚠️ **Token rotation**: If token expires, formula updates fail (need to update secret)
- ⚠️ **Additional job**: Adds ~30-60 seconds to release workflow (parallel with other jobs)

### Neutral

- 📝 **Monitoring requirement**: Should monitor job status to catch failures quickly
- 📝 **Manual fallback**: Can always update formula manually if automation fails
- 📝 **Testing overhead**: Each release should be spot-checked (lightweight validation)

## Implementation Notes

### For Developer Agent

**Changes required**:

1. **Create update script**: `scripts/update-homebrew-formula.sh` (as specified above)
   - Make executable: `chmod +x scripts/update-homebrew-formula.sh`
   - Commit to main repository

2. **Add workflow job**: Insert after `consolidate-checksums` job in `.github/workflows/release.yml`

3. **Configure secret**: Add `HOMEBREW_TAP_TOKEN` to repository secrets (Maintainer task)

4. **Create tap repository**: `oocx/homebrew-tfplan2md` with initial formula template (Maintainer task)

### For Quality Engineer

**Test plan**:

1. **Script unit test**:
   ```bash
   # Create test checksums file
   echo "abc123... tfplan2md_1.0.0_macos-x64.tar.gz" > test-checksums.txt
   echo "def456... tfplan2md_1.0.0_macos-arm64.tar.gz" >> test-checksums.txt
   echo "ghi789... tfplan2md_1.0.0_linux-x64.tar.gz" >> test-checksums.txt
   
   # Create test formula template
   cp homebrew-tap/Formula/tfplan2md.rb test-formula.rb
   
   # Run update script
   bash scripts/update-homebrew-formula.sh 1.0.0 test-checksums.txt test-formula.rb
   
   # Verify formula updated
   grep "version \"1.0.0\"" test-formula.rb
   grep "sha256 \"abc123...\"" test-formula.rb
   ```

2. **Workflow integration test**: Covered in Pre-Production Testing above

3. **Error handling test**:
   - Test with missing checksum (should fail)
   - Test with invalid checksum format (should fail)
   - Test with already-updated formula (should skip commit)

### Dependencies

**Blockers**:
- ✅ ADR-001 (Platform Build Fixes): macOS binaries must be available
- ✅ ADR-002 (Homebrew Formula Design): Formula structure must be defined
- 🔄 Tap repository created: `oocx/homebrew-tfplan2md` with template formula
- 🔄 Secret configured: `HOMEBREW_TAP_TOKEN` added to repository secrets

**Workflow Integration Points**:

```
release (creates GitHub Release)
  ↓
build-binaries (creates platform binaries)
  ↓
consolidate-checksums (creates SHA256SUMS artifact)
  ↓
update-homebrew-formula (updates tap repository formula) ← NEW JOB
```

## References

- **GitHub Actions Checkout**: https://github.com/actions/checkout
- **GitHub Actions Download Artifact**: https://github.com/actions/download-artifact
- **GitHub Actions Secrets**: https://docs.github.com/en/actions/security-guides/encrypted-secrets
- **Conventional Commits**: https://www.conventionalcommits.org/ (for formula commit messages)
- **ADR-002**: Homebrew Formula Design (formula structure)
- **ADR-001**: Platform Build Fixes (provides macOS binaries)
- **Release Workflow**: `.github/workflows/release.yml` (integration point)
