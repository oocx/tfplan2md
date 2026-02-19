# Resource Details Display Mode Control

This release adds the `--details` CLI option, giving you control over whether resource details blocks are initially expanded or collapsed in the generated markdown report.

## ✨ Features

- **`--details` CLI argument**: Control the initial display state of resource details blocks with three modes:
  - `--details auto` (default): Automatically expand only resources with code analysis findings, collapse others
  - `--details open`: Expand all resource details blocks by default (shows full content immediately)
  - `--details closed`: Collapse all resource details blocks by default (clean, scannable view)
- **Smart auto mode**: When using `--details auto` with `--code-analysis-results`, resources with security/quality findings are automatically expanded to draw attention to issues
- **Merged child handling**: In auto mode, parent resources are expanded if any merged child resource has code analysis findings
- **Consistent debug behavior**: Debug information blocks (enabled with `--debug`) remain collapsed regardless of `--details` setting
- **Backward compatible**: Default behavior (when `--details` is not specified) is `auto`, which preserves the current behavior

## 💡 Use Cases

- **Large plan reviews**: Use `--details closed` to get a clean overview of all changes, then expand specific resources of interest
- **Security-focused reviews**: Use `--details auto` with static analysis to immediately see resources with findings while keeping clean resources collapsed
- **PR reviews**: Use `--details open` for small plans or when you want to review everything in detail
- **Customized workflows**: Choose the mode that best fits your team's review process

## ▶️ Getting Started

```bash
# Collapse all resources by default
terraform show -json plan.tfplan | tfplan2md --details closed

# Expand all resources by default
terraform show -json plan.tfplan | tfplan2md --details open

# Auto mode: expand only resources with code analysis findings
terraform show -json plan.tfplan | tfplan2md \
  --details auto \
  --code-analysis-results checkov-results.sarif

# Default behavior (auto mode)
terraform show -json plan.tfplan | tfplan2md
```

## 🔍 How It Works

The `--details` option controls the `open` attribute on HTML `<details>` elements:

- **`closed`**: All resource blocks rendered as `<details>` (no `open` attribute)
- **`open`**: All resource blocks rendered as `<details open>`
- **`auto`**: Resource blocks rendered with `open` only when code analysis findings are attached

Users can always manually expand/collapse details blocks by clicking the summary line, regardless of the initial state.
