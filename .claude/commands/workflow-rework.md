---
description: Send the current work item back for rework and continue
---

Rework reason: $ARGUMENTS

Run `scripts/wp-append.sh --rework <failing-stage> --reason "<reason>"`, then continue
the loop with the `run-workflow` skill.

If the target stage has already been attempted three times, stop and involve the
Maintainer instead of escalating again — repeated failure at one stage is usually a
specification problem.
