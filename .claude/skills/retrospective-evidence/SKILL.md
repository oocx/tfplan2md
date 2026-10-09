---
name: retrospective-evidence
description: Where to find objective evidence about a completed cycle - work protocol, workflow state, git history and CI.
---

# Retrospective Evidence

A retrospective is only as good as what it can point at. This is where the objective
record lives.

## The highest-signal source is `state.json`

```bash
jq '{attempts, open_questions, gates}' docs/<type>/NNN-<slug>/state.json
```

- **`attempts`** — a stage that ran three times is a workflow defect, not bad luck.
  Look at *why* it repeated: an unclear specification usually shows up as repeated
  Developer rework, not as a Requirements Engineer problem.
- **`open_questions`** — a question nobody answered means a role proceeded on an
  assumption. If the assumption turned out wrong, that is a finding about the gate
  design, not about the role.
- **`gates`** — a gate that was never contested when it should have been, or one that
  fired on something trivial, is a rule worth adjusting.

## The work protocol

`work-protocol.md` carries each role's own account, including the
**Problems Encountered** field. Read those first: they are the closest thing to a
first-hand incident report, and a problem that was logged and then ignored is a worse
finding than the problem itself.

## Git history

Start with the retained work item's Release Manager entry to identify the merged PR and
published release. Use the merged PR's recorded base and head commits to inspect the
delivery commit range, and corroborate it with the release and CI history:

```bash
git log --oneline <pr-base>..<pr-head>          # how often delivery work was redone
git log --format='%s' <pr-base>..<pr-head> | cut -d: -f1 | sort | uniq -c
```

Repeated commits touching the same file, or a string of `fix:` commits after a
`feat:`, tell you where the work actually went.

Do not use `origin/main..HEAD` for a post-release documentation follow-up. At that
point the delivery commits are already on `main`, so that range contains only the
follow-up branch and can hide the cycle being reviewed. If the Release Manager entry,
merged PR, release, or CI record does not establish a fact, say the evidence is
unavailable rather than reconstructing it from memory.

## CI

```bash
scripts/check-workflow-status.sh list
```

What failed, and *how late* it failed. A failure caught in PR Validation that a role
could have caught locally is an argument for moving the check earlier.

## What this skill replaced

Retrospectives used to be driven by VS Code chat exports, parsed by
`scripts/analyze-chat.py`. Those exports do not exist outside VS Code, and the metrics
they produced (model usage, tool invocation counts, approval friction) measured the
harness rather than the workflow. The sources above measure the work.

## Writing the finding

Every improvement opportunity needs three things or it is a wish:

1. The evidence — which of the above, quoted.
2. The change location — the file that would change.
3. The verification — how you would know it worked.
