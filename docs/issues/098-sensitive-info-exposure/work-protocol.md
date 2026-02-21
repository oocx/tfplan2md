# Work Protocol: Sensitive Information Exposure

**Work Item:** `docs/issues/098-sensitive-info-exposure/`
**Branch:** `fix/098-sensitive-info-exposure`
**Workflow Type:** Bug Fix
**Created:** 2026-02-21

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2026-02-21
- **Summary:** Investigated and confirmed all reported sensitive/secret exposure paths; documented root causes and a high-level fix approach for AzApi body rendering, Scriban template context sensitivity propagation, Azure DevOps variable secret masking, and hierarchical sensitivity path detection.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/work-protocol.md`, `docs/issues/098-sensitive-info-exposure/analysis.md`
- **Problems Encountered:** None

### Architect
- **Date:** 2026-02-21
- **Summary:** Reviewed the issue analysis as the effective specification and identified a cross-cutting architectural gap: Scriban templates can access raw JSON state without a fail-safe masking mechanism. Proposed a defense-in-depth approach where `before_json`/`after_json` are masked-by-default unless `--show-sensitive` is enabled, while still exposing `before_sensitive`/`after_sensitive` for advanced templates and layout decisions.
- **Artifacts Produced:** `docs/issues/098-sensitive-info-exposure/architecture.md`, `docs/adr-009-template-json-sensitivity-masking.md`
- **Problems Encountered:** No `specification.md` exists for this issue; proceeded using `analysis.md` as the requirements source.
