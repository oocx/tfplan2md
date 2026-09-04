## Pre-PR Checklist Skill and .github/ Validation Exemption

Adds a `pre-pr-checklist` agent skill that ensures every change satisfies the
repository's minimum requirements before a PR is created.

Simultaneously updates the PR validation pipeline and the release-notes guardrail
to exempt all `.github/` changes (skills, agents, workflows, copilot instructions)
from the full test and work-item requirements, consistent with how `ci.yml` already
treats `.github/`-only merges.
