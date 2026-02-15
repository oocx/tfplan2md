# Architecture: Multi-Platform Binary Distribution (Phase 1: Linux x64)

## Status

Proposed

## Context

This document provides the detailed implementation architecture for Phase 1 of ADR-008: Multi-Platform Binary Distribution. The high-level decisions have been approved by the maintainer:

- **ADR-008 Status**: Approved (linux-x64 only for Phase 1)
- **Feature Specification**: `docs/features/047-multi-platform-binary-distribution/specification.md`
- **Scope**: Linux x64 (glibc) binary distribution only
- **Distribution Model**: Supplementary to Docker (Docker remains primary)

This architecture document addresses concrete implementation details for the GitHub Actions workflow, build process, packaging, and release asset management.

## Analysis

The existing architecture already supports Native AOT compilation (currently used for Docker builds). The project's `.csproj` file has Native AOT configured with aggressive size optimizations. The release workflow has a clear structure with job dependencies that can be extended.

**Key Findings:**
1. Native AOT is already fully configured and working (`PublishAot=true`, `TrimMode=full`, etc.)
2. Release workflow uses `softprops/action-gh-release@v2` for GitHub Release creation
3. Current workflow structure: `release` job creates GitHub Release, `docker` job depends on it
4. Version extraction and release notes generation are already implemented
5. No cross-compilation needed—GitHub Actions ubuntu-latest runner natively supports linux-x64

**Architectural Fit:**
This feature adds a new parallel build path without modifying existing components. It follows the project's principle of minimal changes and leverages existing infrastructure.

## Architectural Decisions

### 1. GitHub Actions Workflow Design

**Decision:** Add a new job `build-linux-x64-binary` to `.github/workflows/release.yml` rather than creating a separate workflow.

**Rationale:**
- **Single workflow = atomic releases**: All artifacts (Docker + binary) are part of the same release process
- **Shared version context**: The `release` job's version outputs can be reused
- **Consistent trigger mechanism**: Binary build uses the same triggers as Docker (tag push, workflow_run, workflow_dispatch)
- **Easier maintenance**: One workflow file to manage for all release artifacts

**Job Structure:**
```yaml
jobs:
  release:
    # Existing job - creates GitHub Release
    outputs:
      version: ${{ steps.version.outputs.version }}
      # ... other outputs

  build-linux-x64-binary:
    name: Build Linux x64 Binary
    runs-on: ubuntu-latest
    needs: release  # Wait for release to exist before uploading assets
    # ... steps

  docker:
    # Existing job - unchanged
    needs: release
```

**Execution Order:**
1. `release` job runs first (creates GitHub Release)
2. `build-linux-x64-binary` and `docker` jobs run in parallel after `release` completes

**Benefits:**
- Binary build doesn't block Docker build or vice versa
- Total workflow time = max(binary_time, docker_time) rather than sum
- Failure in one doesn't prevent the other from completing

### 2. Matrix Strategy (Phase 1: Single Platform)

**Decision:** Do NOT use GitHub Actions matrix strategy in Phase 1. Implement as a single job.

**Rationale:**
- **YAGNI Principle**: Matrix adds complexity for a single platform
- **Simplicity**: Easier to understand and debug
- **Phase 2 Refactoring**: When adding Phase 2 platforms, refactor to matrix in a dedicated PR
- **Clear Phase 1 Scope**: Keeps Phase 1 focused and minimal

**Future Extensibility Design:**
When implementing Phase 2, the job can be refactored to:
```yaml
build-binaries:
  strategy:
    matrix:
      include:
        - os: ubuntu-latest
          rid: linux-x64
          archive: tar.gz
        - os: ubuntu-latest
          rid: linux-arm64
          archive: tar.gz
        # ... more platforms
```

**Phase 1 to Phase 2 Transition Plan:**
1. Phase 1: Single job `build-linux-x64-binary` with hardcoded RID
2. Phase 2: Rename to `build-binaries` and introduce matrix
3. Platform-specific logic parameterized via matrix variables

### 3. Build Process

**Decision:** Use `dotnet publish` with the following exact parameters:

```bash
dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishAot=true \
  -o artifacts/linux-x64
```

**Parameter Justification:**
- `-c Release`: Production-optimized build configuration
- `-r linux-x64`: Target Runtime Identifier for glibc-based Linux on x64
- `--self-contained true`: Include runtime dependencies (Native AOT requirement)
- `-p:PublishAot=true`: Enable Native AOT compilation
- `-o artifacts/linux-x64`: Consistent output directory structure

**Output:**
- Single executable: `artifacts/linux-x64/tfplan2md`
- Native Linux binary with execute permissions set

**Why No Additional Parameters:**
All Native AOT optimizations are already configured in `.csproj`:
- `TrimMode=full`
- `StripSymbols=true`
- `InvariantGlobalization=true`
- `IlcOptimizationPreference=Size`
- `IlcGenerateStackTraceData=false`

**Build Configuration Reuse:**
The `.csproj` file already contains all necessary Native AOT settings from Docker builds. No modifications to project file required.

### 4. Packaging Strategy

**Decision:** Create a tar.gz archive with the binary at the root level (flat structure).

**Packaging Command:**
```bash
cd artifacts/linux-x64
tar -czf ../../tfplan2md_${VERSION}_linux_x64.tar.gz tfplan2md
cd ../..
```

**Archive Structure:**
```
tfplan2md_1.12.0_linux_x64.tar.gz
└── tfplan2md  (executable, permissions preserved)
```

**Design Choices:**

1. **Flat vs Nested Structure**: Flat (binary at root)
   - **Rationale**: Simpler user experience; `tar -xzf` extracts binary directly to current directory
   - **Comparison**: OpenTofu uses flat structure; Terraform uses nested (`terraform` directory)
   - **Decision**: Follow OpenTofu convention (as stated in specification)

2. **Archive Format**: tar.gz (not zip)
   - **Rationale**: Standard for Linux binaries; preserves file permissions
   - **Execute Bit**: `tar` preserves executable permissions automatically
   - **Windows Consideration**: Phase 2 will use `.zip` for win-x64

3. **Compression**: gzip default compression level (6)
   - **Rationale**: Good balance of compression speed and size reduction
   - **Build Performance**: Default gzip compression is fast enough for CI/CD
   - **Alternative Considered**: High compression (`-9`) adds minimal size savings but increases build time

4. **Naming Convention**: `tfplan2md_<version>_linux_x64.tar.gz`
   - **Format**: `<tool>_<version>_<os>_<arch>.<ext>`
   - **Example**: `tfplan2md_1.12.0_linux_x64.tar.gz`
   - **Rationale**: Matches OpenTofu convention from specification
   - **Consistency**: Versioning uses semantic version without `v` prefix (e.g., `1.12.0`, not `v1.12.0`)

**File Permissions:**
The `dotnet publish` output includes execute permissions on the `tfplan2md` binary. The `tar -czf` command preserves these permissions in the archive by default. No explicit `chmod` required.

### 5. Checksum Generation

**Decision:** Generate `SHA256SUMS` file in standard `sha256sum` format.

**Checksum Command:**
```bash
sha256sum tfplan2md_${VERSION}_linux_x64.tar.gz > SHA256SUMS
```

**File Format:**
```
a3d5e8f7b2c1d6a9...  tfplan2md_1.12.0_linux_x64.tar.gz
```

**Format Details:**
- Checksum (64 hex characters), two spaces, filename
- Standard format recognized by `sha256sum -c` command
- Compatible with GNU coreutils and macOS/BSD shasum tools

**Generation Timing:**
Checksums are generated **after packaging, before upload** to ensure they match the exact files being released.

**Workflow Step Order:**
1. Build binary (`dotnet publish`)
2. Package binary (`tar -czf`)
3. Generate checksums (`sha256sum`)
4. Upload both archive and checksums to GitHub Release

**Phase 2 Consideration:**
When multiple platforms are added, the `SHA256SUMS` file will contain checksums for all binaries:
```
<hash>  tfplan2md_1.12.0_linux_x64.tar.gz
<hash>  tfplan2md_1.12.0_linux_arm64.tar.gz
<hash>  tfplan2md_1.12.0_darwin_arm64.tar.gz
```

**Design Choice**: Single `SHA256SUMS` file for all platforms (matches OpenTofu pattern).

**Checksum Verification:**
Users verify with: `sha256sum -c SHA256SUMS --ignore-missing`
- `--ignore-missing`: Only verify files present in current directory (allows selective download)

### 6. Release Asset Upload

**Decision:** Use `softprops/action-gh-release@v2` to upload binary assets (same action already used for release creation).

**Upload Action Configuration:**
```yaml
- name: Upload Binary to GitHub Release
  uses: softprops/action-gh-release@v2
  with:
    tag_name: v${{ needs.release.outputs.version }}
    files: |
      tfplan2md_${{ needs.release.outputs.version }}_linux_x64.tar.gz
      SHA256SUMS
```

**Why This Action:**
- **Already in use**: The `release` job uses it to create the GitHub Release
- **Idempotent**: Can be called multiple times on the same release (adds/updates assets)
- **Reliable**: Mature, widely-used GitHub Action (2M+ uses)
- **Simple**: No need to manage GitHub API tokens or pagination

**Asset Upload Coordination:**
1. `release` job creates the GitHub Release (empty initially)
2. `build-linux-x64-binary` job uploads binary assets to the existing release
3. `docker` job completes (Docker image push, no GitHub asset upload)

**Upload Order:**
Assets are uploaded in a single action call, so order is atomic. Both files appear simultaneously.

**Idempotency:**
If the workflow is re-run (manual trigger or retry), the action overwrites existing assets with the same name. This is safe because:
- Assets are generated from the same tagged commit
- Build is deterministic (Native AOT with fixed configuration)

**Error Handling:**
If upload fails, the release still exists but has missing assets. User can:
- Re-run the workflow (GitHub Actions "Re-run failed jobs")
- Manually upload assets (release exists, just missing files)

**Alternative Considered:** GitHub REST API via `gh` CLI
- **Rejected**: More complex error handling; need to check if asset exists before upload
- **Benefit of Action**: Handles edge cases (asset exists, partial upload, network retry)

### 7. Error Handling and Workflow Reliability

**Decision:** Binary build failure does not block Docker build or invalidate the release.

**Independent Job Failures:**
- Each job can fail independently
- `release` job must succeed for others to run (critical path)
- `build-linux-x64-binary` and `docker` are parallel and independent

**Failure Scenarios:**

| Scenario | Result | User Impact |
|----------|--------|-------------|
| Binary build fails, Docker succeeds | Release exists with Docker image only | Users can pull Docker image; binary not available |
| Docker build fails, Binary succeeds | Release exists with binary only | Users can download binary; Docker image not pushed |
| Both fail after release created | Release exists with no assets | Rare; indicates fundamental issue (should fail in PR validation) |
| Release job fails | No release created; other jobs skipped | Clean failure; no partial release |

**Retry Strategies:**

1. **Build Failures (dotnet publish errors)**:
   - No automatic retry (not transient)
   - Requires code fix and new workflow run
   - **Prevention**: PR validation workflow catches build issues before merge

2. **Upload Failures (network/API issues)**:
   - `softprops/action-gh-release@v2` has built-in retry logic
   - GitHub Actions automatically retries on network errors
   - Manual retry: "Re-run failed jobs" in GitHub UI

3. **Validation Failures (see section 8)**:
   - If validation step fails, job stops before upload
   - Prevents uploading corrupt artifacts

**Monitoring:**
- GitHub Actions sends email on workflow failure
- Release page shows which assets are present/missing

**Recovery:**
If binary upload fails but Docker succeeds:
1. Re-run the `build-linux-x64-binary` job only (GitHub Actions UI)
2. Binary is re-built and uploaded to the existing release
3. No need to re-tag or recreate the release

### 8. Validation and Testing Strategy

**Decision:** Include smoke test validation before uploading artifacts.

**Validation Steps:**

1. **Binary Executable Check:**
   ```bash
   # Verify binary is executable
   test -x artifacts/linux-x64/tfplan2md || exit 1
   ```

2. **Binary Execution Test (Smoke Test):**
   ```bash
   # Verify binary runs and shows help
   artifacts/linux-x64/tfplan2md --help > /dev/null || exit 1
   ```

3. **Archive Integrity Check:**
   ```bash
   # Verify tar.gz can be extracted
   tar -tzf tfplan2md_${VERSION}_linux_x64.tar.gz > /dev/null || exit 1
   ```

4. **Checksum Verification:**
   ```bash
   # Verify checksums are correct
   sha256sum -c SHA256SUMS || exit 1
   ```

**Why These Tests:**
- **Catches build failures early**: Before uploading bad artifacts
- **Fast execution**: < 10 seconds total
- **High confidence**: Validates the entire build → package → checksum pipeline

**Not Included (Out of Scope for Phase 1):**
- End-to-end Terraform plan processing test (deferred to Phase 2)
- Platform-specific runner testing (Phase 1 is ubuntu-latest only)
- Integration tests with real Terraform plans (covered by existing test suite)

**Test Execution Timing:**
Validation runs after packaging and checksum generation, before upload:
```yaml
- name: Build binary
- name: Package binary
- name: Generate checksums
- name: Validate artifacts  # <-- New step
- name: Upload to GitHub Release
```

**Validation Failure Behavior:**
If validation fails, the workflow stops and does not upload artifacts. This prevents releasing broken binaries.

**Future Testing (Phase 2+):**
When multiple platforms are added, consider:
- Testing on actual target platform runners (e.g., `runs-on: ubuntu-latest` for linux-x64, `runs-on: macos-latest` for darwin-arm64)
- End-to-end test with sample Terraform plan JSON
- Cross-platform matrix testing

### 9. Future Extensibility Design

**Decision:** Optimize for Phase 1 simplicity while documenting Phase 2/3 refactoring approach.

**Phase 1 Design Choices That Support Future Expansion:**

1. **Job Naming:**
   - Phase 1: `build-linux-x64-binary` (specific)
   - Phase 2: Rename to `build-binaries` with matrix
   - **Why**: Clear intent; easy to find when refactoring

2. **Output Directory Structure:**
   - Phase 1: `artifacts/linux-x64/`
   - Phase 2+: `artifacts/{rid}/` (e.g., `artifacts/linux-arm64/`, `artifacts/darwin-arm64/`)
   - **Why**: Consistent pattern; easy to parameterize

3. **Packaging Logic:**
   - Phase 1: Inline bash commands
   - Phase 2: Extract to reusable script (`scripts/package-binary.sh $RID $VERSION $ARCHIVE_TYPE`)
   - **Why**: DRY principle; centralized packaging logic

4. **Checksum File:**
   - Phase 1: Single platform in `SHA256SUMS`
   - Phase 2+: Append checksums for each platform (single file for all binaries)
   - **Format**: Standard `sha256sum` format is already multi-line compatible

5. **Workflow Structure:**
   - Current: `release` → `build-linux-x64-binary` || `docker`
   - Phase 2: `release` → `build-binaries` (matrix) || `docker`
   - **Why**: No fundamental restructuring needed, just job parameterization

**Refactoring Approach for Phase 2:**

When implementing Phase 2 (linux-arm64, darwin-x64, darwin-arm64, win-x64):

1. **Rename Job:**
   ```yaml
   build-linux-x64-binary → build-binaries
   ```

2. **Introduce Matrix:**
   ```yaml
   strategy:
     matrix:
       include:
         - os: ubuntu-latest
           rid: linux-x64
           archive: tar.gz
         - os: ubuntu-latest
           rid: linux-arm64
           archive: tar.gz
         - os: macos-latest
           rid: darwin-arm64
           archive: tar.gz
         - os: macos-latest
           rid: darwin-x64
           archive: tar.gz
         - os: windows-latest
           rid: win-x64
           archive: zip
   ```

3. **Parameterize Steps:**
   ```yaml
   - name: Build binary
     run: |
       dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
         -c Release \
         -r ${{ matrix.rid }} \
         --self-contained true \
         -p:PublishAot=true \
         -o artifacts/${{ matrix.rid }}
   ```

4. **Aggregate Checksums:**
   - Each matrix job generates partial checksums
   - Final step combines them into single `SHA256SUMS`
   - Alternative: Upload individual checksums, then merge in separate job

**Design Principles:**
- **Phase 1**: Optimize for clarity and working code
- **Phase 2**: Refactor for DRY and scalability
- **Phase 3**: Polish and optimize

**No Premature Optimization:**
Phase 1 does not include matrix strategy or platform-agnostic scripts because:
- Single platform doesn't benefit from abstraction
- Simpler to understand and review
- Easier to validate in isolation
- Refactoring to matrix is straightforward (well-understood GitHub Actions pattern)

## Implementation Notes

### High-Level Changes Required

**File to Modify:**
- `.github/workflows/release.yml` (add new job)

**New Job Structure:**
```yaml
build-linux-x64-binary:
  name: Build Linux x64 Binary
  runs-on: ubuntu-latest
  needs: release

  steps:
    - name: Checkout
      uses: actions/checkout@v6

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.x'

    - name: Build Linux x64 Binary
      run: |
        dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
          -c Release \
          -r linux-x64 \
          --self-contained true \
          -p:PublishAot=true \
          -o artifacts/linux-x64

    - name: Package Binary
      run: |
        VERSION="${{ needs.release.outputs.version }}"
        cd artifacts/linux-x64
        tar -czf ../../tfplan2md_${VERSION}_linux_x64.tar.gz tfplan2md
        cd ../..

    - name: Generate Checksums
      run: |
        VERSION="${{ needs.release.outputs.version }}"
        sha256sum tfplan2md_${VERSION}_linux_x64.tar.gz > SHA256SUMS

    - name: Validate Artifacts
      run: |
        VERSION="${{ needs.release.outputs.version }}"
        # Verify binary is executable
        test -x artifacts/linux-x64/tfplan2md
        # Smoke test: verify binary runs
        artifacts/linux-x64/tfplan2md --help > /dev/null
        # Verify archive integrity
        tar -tzf tfplan2md_${VERSION}_linux_x64.tar.gz > /dev/null
        # Verify checksums
        sha256sum -c SHA256SUMS

    - name: Upload Binary to GitHub Release
      uses: softprops/action-gh-release@v2
      with:
        tag_name: v${{ needs.release.outputs.version }}
        files: |
          tfplan2md_${{ needs.release.outputs.version }}_linux_x64.tar.gz
          SHA256SUMS
```

### Docker Build Unchanged

**No modifications to `docker` job required.**

The `docker` job will continue to:
1. Depend on `release` job (`needs: release`)
2. Build Native AOT Docker image using existing Dockerfile
3. Push to Docker Hub with version tags

**Parallel Execution Confirmed:**
Both `build-linux-x64-binary` and `docker` depend on `release`, so they run in parallel after release creation completes.

### Workflow Execution Flow

```
┌─────────────────────┐
│   Release Trigger   │
│ (tag, workflow_run, │
│  workflow_dispatch) │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│   release (job)     │
│  - Create Release   │
│  - Extract Version  │
│  - Release Notes    │
└──────────┬──────────┘
           │
           ├─────────────────────┬─────────────────────┐
           v                     v                     v
┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐
│ build-linux-x64  │  │   docker (job)   │  │   (future)     │
│    -binary       │  │ - Build Image    │  │ build-darwin   │
│ - Build Binary   │  │ - Push to Hub    │  │    -arm64      │
│ - Package        │  └──────────────────┘  │   (Phase 2)    │
│ - Checksums      │                        └────────────────┘
│ - Upload         │
└──────────────────┘
```

### Components Affected

**Modified Files:**
- `.github/workflows/release.yml` (add `build-linux-x64-binary` job)

**Unmodified Files:**
- `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj` (Native AOT already configured)
- `src/Dockerfile` (Docker build unchanged)
- All source code (no code changes required)

**New Release Assets:**
- `tfplan2md_<version>_linux_x64.tar.gz`
- `SHA256SUMS`

### Integration Points

1. **Version Sharing:**
   - `build-linux-x64-binary` job uses `needs.release.outputs.version`
   - Ensures consistent version across Docker and binary

2. **Release Creation:**
   - `release` job creates GitHub Release first
   - Binary job uploads assets to existing release

3. **Checkout Consistency:**
   - Both jobs checkout the same commit (tag ref)
   - Ensures binary matches tagged source code

4. **Permissions:**
   - Workflow has `contents: write` permission (already present)
   - Sufficient for asset upload via `softprops/action-gh-release@v2`

### Build Time Estimates

**Expected Build Times:**
- Native AOT compilation: ~2-3 minutes
- Packaging and checksums: ~5 seconds
- Upload: ~10-30 seconds (depends on file size)
- **Total**: ~3-4 minutes

**Parallel Execution Benefit:**
- Docker build: ~5-7 minutes (existing)
- Binary build: ~3-4 minutes (new)
- **Total workflow time**: ~7 minutes (max of both, not sum)

**No Significant Time Increase:**
The specification's NFR1 requirement of "complete within 10 minutes" is easily met.

## Consequences

### Positive

1. **Clear Implementation Path**: Concrete technical decisions provide unambiguous guidance for Developer agent
2. **Minimal Risk**: No changes to existing Docker build; binary build is isolated
3. **Fast Iteration**: Simple Phase 1 design can be validated and refined quickly
4. **User Value**: Users get downloadable Linux binaries without waiting for full multi-platform support
5. **Extensibility**: Design accommodates Phase 2/3 without major refactoring
6. **Reliability**: Validation steps catch issues before release assets are uploaded

### Negative

1. **Technical Debt Awareness**: Phase 2 will require refactoring from single job to matrix (planned, documented)
2. **Maintenance Overhead**: One additional job to monitor and debug (minimal impact)
3. **Partial Multi-Platform**: Users may expect all platforms immediately (mitigated by release notes)

### Neutral

1. **Documentation Update**: Technical Writer will need to document binary download process (separate task)
2. **Release Notes Template**: May need to mention binary availability (handled by Release Manager)

## Risks and Mitigations

### Risk: Native AOT Build Failure in CI

**Likelihood:** Low  
**Impact:** Medium (release blocked)

**Mitigation:**
- Native AOT already working in Docker builds (proven stable)
- Validation step in PR validation workflow will catch issues before merge
- If build fails, Docker release can still succeed independently

### Risk: Archive Permissions Not Preserved

**Likelihood:** Very Low  
**Impact:** Medium (users must manually `chmod +x`)

**Mitigation:**
- `tar` preserves permissions by default (standard behavior)
- Validation step verifies binary is executable before upload
- If issue found, can be fixed in patch release

### Risk: Checksum Mismatch or Incorrect Format

**Likelihood:** Low  
**Impact:** High (security concern, user trust)

**Mitigation:**
- Checksums generated immediately after packaging (same workflow step)
- Validation step verifies checksums with `sha256sum -c` before upload
- Standard `sha256sum` tool ensures correct format

### Risk: Workflow Execution Order Issues

**Likelihood:** Very Low  
**Impact:** Medium (asset upload fails)

**Mitigation:**
- Explicit `needs: release` dependency ensures release exists before binary upload
- `softprops/action-gh-release@v2` is idempotent (safe to retry)
- GitHub Actions guarantees job dependency ordering

### Risk: Binary Size Larger Than Expected

**Likelihood:** Medium  
**Impact:** Low (acceptable per specification)

**Mitigation:**
- Native AOT configuration already optimized for size (`IlcOptimizationPreference=Size`)
- Glibc binaries typically 15-30MB (expected and documented in specification)
- Users who need smaller binaries should use Docker image (14.7MB)

## Alternatives Considered (Implementation Level)

### Alternative 1: Separate Workflow for Binary Builds

**Option:** Create `.github/workflows/build-binaries.yml` separate from `release.yml`

**Rejected Rationale:**
- **Atomicity**: Separate workflows are harder to coordinate (race conditions)
- **Version Syncing**: Need to share version context across workflows (complex)
- **Trigger Management**: Must keep triggers in sync between workflows
- **User Experience**: One workflow = one release process (simpler mental model)

### Alternative 2: Cross-Compilation from Alpine Docker Image

**Option:** Use the existing Docker build process to extract linux-x64 binary

**Rejected Rationale:**
- **Architecture Mismatch**: Docker uses `linux-musl-x64` (Alpine), specification requires `linux-x64` (glibc)
- **Wrong Binary**: Extracting from Docker would produce musl-based binary, not glibc
- **Confusion**: Users would report "binary doesn't work" on Ubuntu/Debian/RHEL
- **Clear Separation**: Docker and binary builds target different use cases

### Alternative 3: Upload Checksums Per-Platform in Phase 1

**Option:** Use `SHA256SUMS.linux-x64` instead of `SHA256SUMS`

**Rejected Rationale:**
- **Inconsistent with OpenTofu**: Single `SHA256SUMS` file is standard pattern
- **User Confusion**: Different checksum files for each platform is non-standard
- **Phase 2 Complexity**: Would need to consolidate later anyway
- **Tool Compatibility**: `sha256sum -c` expects single file with `--ignore-missing` flag

### Alternative 4: Build Binary Inside Docker Container

**Option:** Use Docker container to build linux-x64 binary (containerized build)

**Rejected Rationale:**
- **Unnecessary Complexity**: GitHub Actions ubuntu-latest runner already supports linux-x64 natively
- **No Benefit**: Docker overhead with no advantage (same dotnet SDK, same RID)
- **Slower**: Docker layer building adds time
- **Consistent with Phase 2**: Other platforms (macOS, Windows) won't use Docker anyway

### Alternative 5: GitHub CLI (`gh release upload`) Instead of Action

**Option:** Use `gh release upload` command instead of `softprops/action-gh-release@v2`

**Rejected Rationale:**
- **Reinventing Wheel**: Action already handles edge cases (retry, overwrite, idempotency)
- **More Code**: Need to write error handling and retry logic
- **Consistency**: Release creation already uses this action (same tool for same task)
- **Maintenance**: Action is actively maintained by community

## Open Questions

**None.** All architectural decisions for Phase 1 implementation are documented above.

If questions arise during implementation, Developer should:
1. Check this architecture document first
2. Refer to ADR-008 for high-level decisions
3. Consult the Feature Specification for requirements
4. Ask Maintainer if architectural changes are needed

## Definition of Done

Architecture design is complete when:

- [x] All 8 architecture decision areas are addressed (workflow design, build process, packaging, checksums, upload, error handling, validation, extensibility)
- [x] Concrete implementation guidance is provided (exact commands, step ordering)
- [x] Workflow job structure is clearly defined
- [x] Integration points are documented (version sharing, permissions, dependencies)
- [x] Validation and testing strategy is defined
- [x] Future extensibility approach for Phase 2/3 is documented
- [x] Risks are identified and mitigations provided
- [x] Alternatives are considered and rationale documented
- [x] Maintainer has approved this architecture

## References

- **Feature Specification**: `docs/features/047-multi-platform-binary-distribution/specification.md`
- **ADR-008**: `docs/adr-008-multi-platform-binary-distribution.md`
- **Current Release Workflow**: `.github/workflows/release.yml`
- **Project Specification**: `docs/spec.md`
- **Architecture Overview**: `docs/architecture.md`
- **.NET Native AOT Documentation**: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
- **GitHub Actions Matrix Documentation**: https://docs.github.com/en/actions/using-jobs/using-a-matrix-for-your-jobs
- **softprops/action-gh-release**: https://github.com/softprops/action-gh-release
