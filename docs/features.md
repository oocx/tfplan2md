# Features

This document describes the features of `tfplan2md` from a user perspective.

## Overview

`tfplan2md` is a CLI tool that converts Terraform plan JSON files into human-readable markdown reports. It is designed primarily for use in CI/CD pipelines and is distributed as a Docker image.

## Input

- **Stdin (default)**: Read Terraform plan JSON from standard input
  ```bash
  terraform show -json plan.tfplan | tfplan2md
  ```
- **File path**: Read from a file specified as an argument
  ```bash
  tfplan2md plan.json
  ```

## Output

- **Stdout (default)**: Write markdown to standard output
- **File**: Write to a file using the `--output` flag
  ```bash
  tfplan2md plan.json --output plan.md
  ```

## Report Content

The default report includes:

- **Summary**: Overview of changes (e.g., "3 to add, 1 to change, 2 to destroy")
- **Detailed changes**: List of affected resources with their actions and attribute changes

### Sensitive Values

- Sensitive values are **masked by default** for security
- Use `--show-sensitive` flag to reveal sensitive values in the output

## Templates

Reports are generated using customizable templates powered by [Scriban](https://github.com/scriban/scriban).

- **Default template**: A built-in template is included in the Docker image
- **Custom templates**: Provide a custom template file using the `--template` flag
  ```bash
  tfplan2md plan.json --template /path/to/custom-template.md
  ```

### Template Data

Templates have access to all data required to render detailed change information, including:
- Resource addresses and types
- Change actions (create, update, delete, replace)
- Attribute changes (before/after values)
- Metadata from the Terraform plan

## CLI Interface

Simple single-command interface with flags:

| Flag | Description |
|------|-------------|
| `--output <file>` | Write output to a file instead of stdout |
| `--template <file>` | Use a custom Scriban template file |
| `--show-sensitive` | Show sensitive values unmasked |
| `--help` | Display help information |
| `--version` | Display version information |

## Error Handling

- **Exit codes**: `0` on success, non-zero on failure
- **Error messages**: Clear and actionable, written to stderr
- **No partial output**: The tool fails cleanly without producing incomplete reports
- **Lenient parsing**: Processes the plan as long as required fields are valid (no strict format validation)

## Terraform Compatibility

- Supports Terraform **1.14 and later**
- Lenient parsing approach—does not validate the full plan format, only the fields needed for report generation

## Distribution

### Docker Image

- **Base image**: .NET Chiseled (distroless) for minimal attack surface
- **Registry**: Docker Hub
- **Tagging**: Semantic versioning (`1.0.0`, `1.0`, `1`, `latest`)

### Usage Example

```bash
# From stdin
terraform show -json plan.tfplan | docker run -i tfplan2md

# From file (mounted volume)
docker run -v $(pwd):/data tfplan2md /data/plan.json --output /data/plan.md
```

## Future Considerations

The following features are not in scope for the initial version but may be added later:

- Specialized output formatting for specific resource types (e.g., firewall rules as tables)
