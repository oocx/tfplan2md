---
description: Continue the current work item from wherever it stopped
---

Load the `run-workflow` skill, run `scripts/workflow-next.sh`, and continue the loop
from the stage it reports until a gate or completion.

If it exits 2, present the gate question together with any accumulated open questions,
and wait.
