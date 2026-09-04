# Work Protocol: Azure DevOps large principal mappings

**Work Item:** `docs/issues/140-azdo-large-principal-mapping/`
**Branch:** `fix/667-azdo-large-principal-mapping`
**Workflow Type:** Bug Fix
**Created:** 2026-09-01

## Agent Work Log

### Issue Analyst

- **Date:** 2026-09-01
- **Summary:** Confirmed that the large-value threshold bypassed Azure DevOps value formatters and identified flattened membership array paths as part of the affected rendering path.
- **Artifacts Produced:** Issue analysis findings in the work protocol.
- **Problems Encountered:** The issue's service-identity scenario requires descriptor keys rather than only GUID keys in `azdoUsers`.

### Developer

- **Date:** 2026-09-01
- **Summary:** Changed large-value classification so resolved provider values remain in the attribute table, and extended Azure DevOps member formatter matching to flattened array entries.
- **Artifacts Produced:** Production fix, regression test, and release notes.
- **Problems Encountered:** The local host provides .NET SDK 10.0.400 while the repository's locked NativeAOT dependencies target SDK 10.0.100 / runtime 10.0.8; the CI environment will run the exact locked toolchain.

### Technical Writer

- **Date:** 2026-09-01
- **Summary:** Updated Azure DevOps principal-mapping documentation to state that `azdoUsers` accepts membership descriptors, including `svc.*` service identities.
- **Artifacts Produced:** `README.md`, `docs/features.md`, and the Azure DevOps principal-mapping specification.
- **Problems Encountered:** None.

### Code Reviewer

- **Date:** 2026-09-01
- **Summary:** Reviewed the formatter-first classification change and requested an end-to-end report-rendering regression test. The requested coverage was added for long mapped `group` and `members[0]` descriptors.
- **Artifacts Produced:** Review findings and test-coverage verification recorded in the work protocol.
- **Problems Encountered:** No production correctness blocker found.
