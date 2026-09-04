# Bitbucket Render Target

This release adds a Bitbucket-specific render target for tfplan2md.

## ✨ Features

- **New CLI option:** `--render-target bitbucket`
- **Markdown-only output for Bitbucket comments:** removes or rewrites HTML-enhanced markdown that Bitbucket comments do not reliably support
- **Readable inline values:** inline code now preserves decoded literal characters such as `&`, `|`, and backticks
- **Readable large values:** HTML code blocks are rewritten to fenced markdown code blocks

## Why this matters

Users who post tfplan2md output into Bitbucket pull request comments no longer need to accept partially rendered HTML or missing details sections. The new render target degrades the report into plain markdown while keeping the plan readable.

## Example

```bash
tfplan2md plan.json --render-target bitbucket
```

Use this render target when the output will be consumed primarily in Bitbucket pull request comments or descriptions.