# Work Protocol: musl Builds Fail to Build

**Work Item:** `docs/issues/114-musl-build-failure/`
**Branch:** `copilot/fix-musl-build-failures`
**Workflow Type:** Bug Fix
**Created:** 2026-03-16

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2026-03-16
- **Summary:** Investigated the musl build failure from GitHub Actions run 23117834110. Identified root cause: the "Install UPX (Linux)" step runs `apt-get` without `sudo` on the GitHub Actions runner host (non-root), but the non-musl Linux builds run inside Docker containers as root where `sudo` is not needed. Documented findings in analysis.md with a suggested fix.
- **Artifacts Produced:** `docs/issues/114-musl-build-failure/analysis.md`, `docs/issues/114-musl-build-failure/work-protocol.md`
- **Problems Encountered:** None
