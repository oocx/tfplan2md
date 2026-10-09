# Tasks: Post-release retrospective and separate workflow improvements

| Priority | Task | Status | Rationale |
|---|---|---|---|
| High | Move feature and fix retrospectives after Release Manager completion; align the workflow driver configuration and regression check. | ✅ Done | A retrospective must analyze the completed release, not a pre-release candidate. |
| High | Show Workflow Engineer as a separate maintainer-triggered improvement process, outside the primary feature, fix, and website flows. | ✅ Done | Workflow changes are deliberate maintenance work, not an automatic step in every delivery. |
| Medium | Regenerate the published workflow diagram and align role/run-workflow guidance with the corrected process. | ✅ Done | Keep rendered and written workflow descriptions consistent. |
| Medium | Align the website overview and improvement diagram with the post-release handoff and Maintainer decision point. | ✅ Done | The public explanation must not imply that a retrospective starts workflow work automatically. |
| Medium | Correct post-release evidence guidance, conditional handoff wording, and the UAT-skip regression assertion. | ✅ Done | Supporting guidance and checks must preserve Release Manager before Retrospective. |
| High | Replace markup-heavy cyclic diagram with separate numbered workflow lanes and explicit reading order. | ✅ Done | Labels must render cleanly and the forward stage sequence must remain easy to follow. |
| Medium | Preserve Mermaid subgraph headings and nested layout coordinates in the generated website SVG. | ✅ Done | Website diagrams must show the same ordered workflows as the canonical documentation. |
| High | Run the pinned website Markdown checker through npm. | ✅ Done | PR Validation repeatedly failed at the Docker Hub download limit before it could finish checking the website. |
