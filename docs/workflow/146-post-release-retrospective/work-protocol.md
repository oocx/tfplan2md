# Work Protocol: 146-post-release-retrospective

**Work Item:** `docs/workflow/146-post-release-retrospective/`
**Workflow Type:** workflow
**Created:** 2026-10-08

## Agent Work Log

<!-- Each role appends its entry below on completion. -->

### Workflow Engineer

- **Date:** 2026-10-08
- **Summary:** Moved retrospectives after release, separated workflow improvement from primary delivery, and added post-release work-item resume support.
- **Artifacts Produced:** docs/workflow.md, .agents/workflow.json, workflow roles and skill, workflow driver, regression assertion, regenerated ai-workflow.svg
- **Problems Encountered:** None

### Workflow Engineer (round 2)

- **Date:** 2026-10-09
- **Summary:** Aligned website workflow content, supporting guidance, and regression coverage with post-release retrospectives and Maintainer-triggered workflow improvements; regenerated agent adapters and validated the workflow and SVG.
- **Artifacts Produced:** website/src/_data/aiWorkflowPage.js, website/src/media-root/ai-workflow-improvement.svg, .agents/roles/release-manager.md, .agents/skills/retrospective-evidence/SKILL.md, .agents/skills/detect-diagram-crossings/SKILL.md, scripts/test-workflow-driver.sh, docs/workflow/146-post-release-retrospective/tasks.md, generated .claude adapters
- **Problems Encountered:** Baseline workflow regression had one stale UAT-skip expectation (60 passed, 1 failed) and the sandbox runner failed on an unrelated /mnt/einstein/paperless-export mount. Final workflow regression passed 61/61. Website build and HTML/CSS/JS lint passed, but Docker-backed Markdown lint could not run because this host has no Docker socket.

### Workflow Engineer (round 3)

- **Date:** 2026-10-09
- **Summary:** Replaced the cyclic diagram with four numbered workflows in reading order, removed exposed formatting, preserved subgraph geometry, and restored straight arrows. Three renderer regressions, reproducible rendering, complete geometry checks, website build/source lint, Markdown lint and link checks passed.
- **Artifacts Produced:** docs/workflow.md; scripts/render-workflow-diagram.py; scripts/workflow_diagram_svg.py; scripts/tests/test_render_workflow_diagram.py; website workflow SVG, page copy and display styles; diagram skill and generated adapter; tasks.md
- **Problems Encountered:** The earlier diagram validation missed visible markup and confusing layout. Chrome needed a sandbox override. Docker-backed Markdown lint was unavailable; local Markdown lint passed. Legacy crossing parsers needed a validation copy with resolved transforms and valid boundary connections excluded.
