---
description: Start a new work item and drive it through the workflow in auto mode
---

Start a new work item for: $ARGUMENTS

Load the `run-workflow` skill. Determine the workflow type from the request, reserve a
work item number, and delegate to the entry role — do not ask clarifying questions
yourself, the entry role does that. Then drive the loop until a gate or completion.
