# Feature: Custom Report Title

## Overview

Users can provide a custom title for generated reports via the `--report-title` CLI option. This allows users to add context-specific information to reports, making them more meaningful when shared in collaboration tools like Microsoft Teams or when distinguishing between different pipeline types (e.g., drift detection, deployment previews).

## User Goals

- Provide contextual information in report titles (e.g., repository name, environment, purpose)
- Distinguish between different types of reports when multiple pipelines generate Terraform plan reports
- Improve clarity when reports are shared in communication channels or stored for historical review
- Maintain flexibility to use default titles when custom context is not needed

## Scope

### In Scope

- Add `--report-title` CLI option that accepts a string value
- Automatically prepend `# ` to make the title a level-1 markdown heading
- Automatically escape markdown special characters that would interfere with heading rendering
- Make the custom title available to all templates (built-in and custom)
- When `--report-title` is not provided, each template uses its own default title
- Validate that the title is not empty when the option is provided
- Fail with error when title contains newlines

### Out of Scope

- Automatic title generation based on context (e.g., reading from environment variables, git branch names)
- Support for different heading levels (always level-1)
- Templates with multiple headings - only the main report title is customizable
- Title templates or interpolation (e.g., "Drift Detection for {repo}")

## User Experience

### Command-Line Usage

**Basic usage with custom title:**
```bash
tfplan2md plan.json --report-title "Drift Detection Results For my-repository-name"
```

**Output:**
```markdown
# Drift Detection Results For my-repository-name

**Terraform Version:** 1.14.0
...
```

**With special characters (automatically escaped):**
```bash
tfplan2md plan.json --report-title "Production # Environment Changes"
```

**Output:**
```markdown
# Production \# Environment Changes

**Terraform Version:** 1.14.0
...
```

**Default behavior (no option provided):**
```bash
tfplan2md plan.json
```

**Output:**
```markdown
# Terraform Plan Summary

**Terraform Version:** 1.14.0
...
```

**Pipeline integration example:**
```bash
terraform show -json plan.tfplan | \
  tfplan2md --report-title "Drift Detection - ${REPO_NAME}" \
  --template summary > drift-report.md
```

### Error Handling

**Empty title:**
```bash
tfplan2md plan.json --report-title ""
```

**Output:**
```
Error: --report-title cannot be empty.

tfplan2md - Convert Terraform plan JSON to Markdown
[... shows full --help output ...]
```

**Title with newlines:**
```bash
tfplan2md plan.json --report-title "Line 1
Line 2"
```

**Output:**
```
Error: --report-title cannot contain newlines.
```

### Template Integration

**In built-in templates:**
```scriban
{{ report_title ?? "# Terraform Plan Summary" }}

**Terraform Version:** {{ terraform_version }}
...
```

**When `--report-title` is provided:**
- `report_title` variable contains the escaped title with `# ` prepended
- Example: User provides `"Drift Detection # Results"`, template receives `"# Drift Detection \# Results"`

**When `--report-title` is NOT provided:**
- `report_title` variable is `null`/undefined
- Templates use Scriban's null-coalescing operator (`??`) to provide their own default
- Each template defines its own appropriate default title

## Success Criteria

- [ ] `--report-title` option is recognized by the CLI parser
- [ ] User-provided title replaces the default heading in all built-in templates (default, summary)
- [ ] Custom templates can access the title via the `report_title` variable
- [ ] Markdown special characters are automatically escaped (e.g., `#`, `*`, `_`, `[`, `]`, `` ` ``)
- [ ] Tool prepends `# ` to create a level-1 markdown heading automatically
- [ ] Empty string title shows error message and help output
- [ ] Title with newlines fails with clear error message
- [ ] When `--report-title` is not provided, templates use their default titles
- [ ] Generated reports with custom titles pass markdown linting (markdownlint-cli2)
- [ ] Documentation and help text include the new option
- [ ] Tests cover:
  - CLI option parsing (with and without value)
  - Character escaping (various markdown special characters)
  - Empty string validation
  - Newline detection
  - Template variable availability
  - Integration with both built-in templates

## Open Questions

None - all requirements have been clarified with the maintainer.
