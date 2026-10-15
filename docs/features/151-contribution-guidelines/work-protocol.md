# Work Protocol: Contribution guidelines

**Work Item:** `docs/features/151-contribution-guidelines/`
**Branch:** `feature/151-contribution-guidelines`
**Workflow Type:** Feature
**Created:** 2026-10-15

## Agent Work Log

### Technical Writer

- **Date:** 2026-10-15
- **Summary:** Rewrote `CONTRIBUTING.md` as a contribution policy written in the maintainer's voice: bug reports and feature ideas are welcome, pull requests are not accepted (with reasons), plus expectations about response times. Moved the development handbook to `docs/development.md`, which repeats the policy at the top. Added a first section to `AGENTS.md` telling agents that work for anyone other than the maintainer to stop and point their user to `CONTRIBUTING.md`. Updated the PR template, feature request template, README, SUPPORT and all references to the moved content.
- **Artifacts Produced:** CONTRIBUTING.md; docs/development.md; AGENTS.md; README.md; .github/SUPPORT.md; .github/pull_request_template.md; .github/ISSUE_TEMPLATE/feature_request.yml; docs/features.md; docs/spec.md; scripts/pr-github.sh; scripts/validate-release-notes.sh; .agents/skills/{create-pr-azdo,create-pr-github,pre-pr-checklist}/SKILL.md (regenerated into .claude/); DocumentationAlignmentTests.cs; release-notes.md; work-protocol.md
- **Problems Encountered:** The website contributing page still describes a pull-request process; changing it needs a website work item. Fixed two existing broken links to the feature 033 docs while moving the handbook. The branch was not pushed to reserve number 151 because the push was denied; push it before another agent computes a number.
