# Feature Specification: Resource Details Display Mode

## Overview

This feature adds a `--details` CLI argument that gives users control over whether resource details blocks (`<details>` HTML elements) are rendered as open or closed in the generated markdown report. This addresses the need for users to customize the initial presentation of their reports based on their specific review workflows and priorities.

Currently, all resource details blocks are rendered with the `open` attribute (`<details open>`), meaning they are expanded by default. This works well for small plans but can make large reports overwhelming. The `--details` option provides three modes to control this behavior:

- `--details closed` — All resource blocks are collapsed by default
- `--details open` — All resource blocks are expanded by default (current behavior)
- `--details auto` — Automatically expand only resources that have code analysis warnings attached

This feature integrates with the existing Static Code Analysis Integration (Feature 056) by using warnings as a signal for which resources need immediate attention.

## User Goals

### Primary Users

1. **DevOps Engineers Reviewing Large Plans**: Teams reviewing Terraform plans with dozens or hundreds of resources need a way to collapse all details by default and expand only specific resources of interest.

2. **Security-Focused Reviewers**: Security engineers want to immediately see resources with static analysis findings (security issues, policy violations) while keeping clean resources collapsed.

3. **PR Reviewers**: Developers reviewing infrastructure changes in PRs want the flexibility to choose the initial view state based on the complexity of the change.

### User Outcomes

- Control the initial expanded/collapsed state of resource details blocks
- Focus attention on resources with security/quality findings when using `--details auto`
- Improve readability of large reports by collapsing all resources initially
- Maintain current behavior with `--details open` for users who prefer everything expanded
- Reduce cognitive load when reviewing complex infrastructure changes

## Scope

### In Scope

1. **CLI Argument**: Add `--details <mode>` argument accepting three values: `closed`, `open`, `auto`
2. **Closed Mode**: Render all resource details blocks without the `open` attribute (collapsed by default)
3. **Open Mode**: Render all resource details blocks with the `open` attribute (current behavior, expanded by default)
4. **Auto Mode**: 
   - Render resource details blocks with `open` attribute only if the resource has code analysis findings attached
   - Resources without findings are rendered without the `open` attribute (collapsed)
   - Handles merged child resources: if a parent resource includes merged children and any of them have findings, the parent is opened
5. **Debug Block Behavior**: The debug details block (enabled with `--debug`) is always rendered collapsed (without `open` attribute) regardless of `--details` setting
6. **Default Behavior**: If `--details` is not specified, maintain current behavior (equivalent to `--details open`)
7. **Template Abstraction**: Avoid complex logic in Scriban templates by creating a helper function/method that determines the `open` attribute based on the resource and selected mode
8. **Documentation**: Update README.md, docs/features.md, and help text with the new option
9. **Testing**: Unit tests for the helper logic and integration tests for each mode

### Out of Scope

1. **Per-Resource Type Control**: Not implementing fine-grained control like `--details-for-type azurerm_firewall=open` (can be added in future if needed)
2. **Severity-Based Auto Mode**: Not implementing `auto` mode with severity thresholds (e.g., only open for critical/high findings) in this feature
3. **Interactive Toggling**: Not adding JavaScript or interactive controls to toggle details in the rendered markdown
4. **Module-Level Control**: Not implementing module-level details control (e.g., collapse all resources in specific modules)
5. **Large Attribute Details**: This feature only controls resource-level details blocks, not nested details blocks for large attribute values (they remain as-is)

## User Experience

### CLI Usage

```bash
# Close all resource details by default (collapsed view)
tfplan2md --details closed plan.json

# Open all resource details by default (current behavior)
tfplan2md --details open plan.json

# Auto mode: open only resources with code analysis findings
tfplan2md --details auto \
  --code-analysis-results checkov-results.sarif \
  plan.json

# Auto mode with multiple SARIF files
tfplan2md --details auto \
  --code-analysis-results "*.sarif" \
  plan.json

# Default behavior (no --details specified, equivalent to --details open)
tfplan2md plan.json
```

### Expected Behavior

#### Closed Mode (`--details closed`)

All resource details blocks are rendered without the `open` attribute:

```html
<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code></summary>
<br>
{content}
</details>
```

Users must manually click to expand resources they want to review.

#### Open Mode (`--details open`)

All resource details blocks are rendered with the `open` attribute (current behavior):

```html
<details open>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code></summary>
<br>
{content}
</details>
```

All resources are visible by default.

#### Auto Mode (`--details auto`)

Resources are selectively opened based on code analysis findings:

**Resource WITH findings** (expanded):
```html
<details open>
<summary>🔄 azurerm_storage_account <b><code>data</code></b> — <code>stdata</code></summary>
<br>

**Security & Quality:** 🚨 1 critical, ⚠️ 2 high

| Severity | Tool | Attribute | Finding | Remediation |
|----------|------|-----------|---------|-------------|
| 🚨 Critical | Checkov | ... | ... | ... |
| ⚠️ High | Trivy | ... | ... | ... |

{additional content}
</details>
```

**Resource WITHOUT findings** (collapsed):
```html
<details>
<summary>➕ azurerm_virtual_network <b><code>hub</code></b> — <code>vnet-hub</code></summary>
<br>
{content}
</details>
```

**Parent resource with merged child that has findings** (expanded):
If a parent resource has children merged into it (due to parent-child grouping feature) and any child has findings, the entire parent block is opened.

#### Debug Block Behavior

The debug details block is always collapsed regardless of `--details` setting:

```html
<details>
<summary>🐛 Debug Information</summary>
<br>
{debug content}
</details>
```

This applies even with `--details open`.

### Error Handling

**Invalid mode value**:
```bash
$ tfplan2md --details invalid plan.json
Error: Invalid value for --details. Allowed values: closed, open, auto
```

**Auto mode without code analysis files**:
This is valid — auto mode simply behaves like `closed` mode if no code analysis results are provided (no resources have findings, so none are opened).

```bash
# Valid, but behaves like --details closed
$ tfplan2md --details auto plan.json
```

## Success Criteria

- [ ] CLI accepts `--details` argument with three valid values: `closed`, `open`, `auto`
- [ ] Invalid `--details` values show clear error message and exit
- [ ] Closed mode renders all resource details blocks without `open` attribute
- [ ] Open mode renders all resource details blocks with `open` attribute
- [ ] Auto mode opens resource details blocks only when the resource has code analysis findings
- [ ] Auto mode correctly handles merged child resources (opens parent if any child has findings)
- [ ] Debug details block is always rendered collapsed, regardless of `--details` setting
- [ ] Default behavior (no `--details` specified) is equivalent to `--details open`
- [ ] Helper function/method exists to determine `open` attribute (not complex template logic)
- [ ] README.md updated with `--details` option in Installation section
- [ ] docs/features.md updated with detailed feature description
- [ ] Help text (`--help`) includes `--details` option documentation
- [ ] Unit tests cover helper logic for all three modes
- [ ] Integration tests verify correct HTML output for each mode
- [ ] Integration tests verify merged child resource behavior in auto mode
- [ ] Integration tests verify debug block is always collapsed

## Open Questions

None. The requirements are clear and well-defined based on the problem statement provided by the Maintainer.

## Implementation Notes

The problem statement suggests creating a helper function to render the `open` attribute instead of putting logic in Scriban templates. This is good architecture — it keeps templates simple and makes the logic testable.

Recommended approach:
1. Add a property or method to the resource model/view that indicates whether it should be opened
2. The property considers:
   - The selected `--details` mode
   - Whether the resource has code analysis findings (for auto mode)
   - Whether it's a merged parent with children that have findings (for auto mode)
3. The template simply checks this property: `{{ if resource.should_be_open }}open{{ end }}`

For the debug block, it should have its own flag or be explicitly handled as always closed.
