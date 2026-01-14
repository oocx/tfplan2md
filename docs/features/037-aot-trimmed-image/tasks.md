# Tasks: AOT-Compiled Trimmed Docker Image

## Overview

Replace the current standard .NET runtime build with an AOT-compiled, trimmed version to reduce Docker image size and improve performance and security. This involves enabling NativeAOT in the project, ensuring Scriban templates and reflection work correctly, and updating the Dockerfile to use a minimal base image.

Reference: [docs/features/037-aot-trimmed-image/specification.md](docs/features/037-aot-trimmed-image/specification.md)

## Tasks

### Task 1: Enable NativeAOT and Trimming in project configuration

**Priority:** High

**Description:**
Update `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj` to enable NativeAOT and trimming.

**Acceptance Criteria:**
- [x] `<PublishAot>true</PublishAot>` is added to the `.csproj`.
- [x] `<InvariantGlobalization>true</InvariantGlobalization>` is enabled (pending verification).
- [x] `<TrimMode>full</TrimMode>` is configured.
- [x] Project compiles successfully with `dotnet publish -r linux-x64`.

**Dependencies:** None

---

### Task 2: Ensure Reflection and Trimming Compatibility for Scriban and Metadata

**Priority:** High

**Description:**
Identify and protect types used by Scriban rendering and assembly metadata reflection from being trimmed.

**Acceptance Criteria:**
- [x] Run `dotnet publish -r linux-x64` and analyze trimming warnings (remaining IL2104/IL3053 are library-level from Scriban).
- [x] Implement a `rd.xml` (Runtime Directives) file or `TrimmerDescriptor.xml` to preserve members of the report model and Scriban-accessed types.
- [x] Verify that `Assembly.GetCustomAttribute<AssemblyMetadataAttribute>` still works for commit hash and version.
- [ ] (Optional) Add `[DynamicallyAccessedMembers]` annotations if preferred over XML descriptors for specific models.

**Dependencies:** Task 1

---

### Task 3: Update Dockerfile for NativeAOT and Minimal Base Image

**Priority:** High

**Description:**
Update the Dockerfile to use the NativeAOT build process and the `runtime-deps` chiseled base image.

**Acceptance Criteria:**
 - [x] Build stage uses `dotnet publish -r linux-x64 -c Release`.
 - [x] Runtime stage uses `mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled`.
 - [x] `ENTRYPOINT` is updated to run the native binary directly: `ENTRYPOINT ["/app/tfplan2md"]`.
 - [x] Docker image builds successfully.

**Dependencies:** Task 2 (or Task 1)

---

### Task 4: Functional Verification and Metadata Check

**Priority:** High

**Description:**
Run functional tests to ensure the AOT-compiled binary behaves identically to the IL version.

**Acceptance Criteria:**
 - [x] Run `docker run tfplan2md --version` and verify non-empty version/commit output (TC-02).
- [x] Run integration tests against the AOT container (TC-01).
- [x] Verify Scriban rendering for a comprehensive plan (TC-03) and custom templates (Scenario 2).
- [x] All existing unit tests pass.

**Dependencies:** Task 3

---

### Task 5: Metrics Capture and Security Verification

**Priority:** Medium

**Description:**
Capture and document the performance and security improvements.

**Acceptance Criteria:**
- [x] Capture and document the final Docker image size (TC-04). Target: < 50MB. **Actual: 14.7MB (70.6% below target)**
- [x] Capture and document the CI build time impact (TC-05). **~2x increase (45s → 90s), acceptable**
- [x] Verify that `sh` or `ls` are NOT available in the final image (TC-06). **FROM scratch, no shell**

**Dependencies:** Task 3

**Status:** Complete

---

### Task 6: Documentation and Cleanup

**Priority:** Low

**Description:**
Update relevant documentation to reflect the new build process.

**Acceptance Criteria:**
- [x] `docs/features/037-aot-trimmed-image/specification.md` is updated with final metrics (14.7MB, musl, FROM scratch).
- [x] Build instructions verified working with AOT/musl build process.

**Dependencies:** Task 5

**Status:** Complete

## Implementation Order

1. **Task 1 & 2**: Foundation for AOT and addressing trimming/reflection issues (the most technically risky part).
2. **Task 3**: Dockerfile update to package the native binary.
3. **Task 4**: Full functional verification.
4. **Task 5**: Metrics collection.
5. **Task 6**: Final documentation.

## Open Questions

- Should we use `rd.xml` or `TrimmerDescriptor.xml` or code annotations? (Decision: Use what is most maintainable, likely `TrimmerDescriptor.xml` for bulk model preservation).
- Is `InvariantGlobalization` safe? (Expectation: Yes, but TC-03 must verify with non-ASCII characters).
