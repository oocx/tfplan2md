# Terminal Tool Output Truncation

Sometimes the VS Code chat terminal tool captures only part of a command’s output (even when the command succeeds). This can make wrapper scripts *look* unreliable when the underlying operation completed.

## Practical Guidance

- Prefer small, single-purpose terminal invocations over long chained one-liners.
- Verify outcomes via GitHub data (e.g., PR exists / state / URL) rather than relying only on captured stdout.
- If a GitHub chat tool exists for the data you need, prefer it for inspection.
