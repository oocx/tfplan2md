# Feature: Terraform Show Output Approximation Tool

## Overview

Create a standalone development tool that generates output approximating `terraform show` from Terraform plan JSON files. This tool is needed to provide authentic "before tfplan2md" examples for website feature comparison pages.

**Problem**: Website feature pages require side-by-side comparisons showing what Terraform plans look like WITHOUT tfplan2md (raw `terraform show` output) versus WITH tfplan2md (formatted markdown reports). However, the project only has JSON format plan files, not the binary `.tfplan` files that `terraform show` requires.

**Solution**: A tool that reads Terraform plan JSON files and produces text output matching the format, structure, and appearance of real `terraform show` output, including ANSI color codes for authentic terminal rendering.

## User Goals

- **Website maintainers** want to generate authentic-looking Terraform output examples for feature comparison pages
- **Contributors** need a way to create "before" examples when demonstrating new tfplan2md features
- **Documentation writers** want to show realistic Terraform output without requiring actual Terraform binary plan files

## Scope

### In Scope

**Core Functionality**:
- Read Terraform plan JSON files (format version 1.2+)
- Generate text output approximating `terraform show` format
- Include ANSI color codes matching Terraform's terminal output
- Support all Terraform plan operations:
  - `create` - Resources being added
  - `update` - Resources being modified in-place  
  - `delete` - Resources being removed
  - `replace` - Resources being destroyed and recreated
  - `read` - Data source reads
  - `no-op` - Resources with no changes

**Output Sections**:
- Header with Terraform version information
- Plan summary (X to add, Y to change, Z to destroy)
- Detailed resource-by-resource breakdown showing attribute changes
- Footer with operation summary and instructions

**Output Format**:
- Action symbols: `+` (create), `~` (update), `-` (delete), `-/+` (replace)
- Proper indentation matching Terraform's style
- ANSI color codes: green for additions, yellow for updates, red for deletions
- Attribute-level diffs showing before/after values
- Known-after-apply indicators for computed values

**Tool Structure**:
- Standalone .NET tool project at `tools/Oocx.TfPlan2Md.TerraformShowRenderer/`
- CLI interface consistent with other project tools (HtmlRenderer, ScreenshotGenerator)
- Input: JSON plan file path
- Output: Text file or stdout with ANSI codes

### Out of Scope

**Explicitly NOT included**:
- Processing actual Terraform binary plan files (`.tfplan`)
- Executing Terraform commands or requiring Terraform installation
- Interactive prompts or user input beyond CLI arguments
- Real-time plan streaming or watching
- Plan validation or execution
- Support for Terraform versions older than 1.0
- Docker image distribution (this is a development tool only)
- CI/CD pipeline integration (website-only use case)

**Deferred to Future Work**:
- Plan comparison/diff between two JSON files
- Filtering output by resource type or module
- Custom output templates
- JSON schema validation beyond basic structure checks

## User Experience

### Command-Line Interface

The tool follows the same CLI patterns as existing project tools:

```bash
# Basic usage with output file
dotnet run --project tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input examples/comprehensive-demo/plan.json \
  --output examples/comprehensive-demo/plan-show.txt

# Output to stdout (for piping or inspection)
dotnet run --project tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input examples/comprehensive-demo/plan.json

# With short option names
dotnet run --project tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  -i plan.json -o plan-show.txt

# Show help
dotnet run --project tools/Oocx.TfPlan2Md.TerraformShowRenderer -- --help

# Show version
dotnet run --project tools/Oocx.TfPlan2Md.TerraformShowRenderer -- --version
```

### CLI Options

| Option | Short | Required | Description |
|--------|-------|----------|-------------|
| `--input <file>` | `-i` | Yes | Path to Terraform plan JSON file |
| `--output <file>` | `-o` | No | Output text file path (defaults to stdout) |
| `--no-color` | | No | Suppress ANSI color codes (plain text) |
| `--help` | `-h` | No | Display help information |
| `--version` | `-v` | No | Display tool version |

### Example Output

Given a JSON plan file with a resource creation, the tool produces:

```
Terraform will perform the following actions:

  # azurerm_resource_group.core will be created
  + resource "azurerm_resource_group" "core" {
      + id       = (known after apply)
      + location = "eastus"
      + name     = "rg-tfplan2md-demo"
      + tags     = {
          + "environment" = "demo"
          + "owner"       = "tfplan2md"
        }
    }

Plan: 1 to add, 0 to change, 0 to destroy.
```

*(Output includes appropriate ANSI color codes: green for `+`, attribute names, etc.)*

### Error Handling

**Clear error messages for common issues**:
- File not found: "Error: Input file not found: {path}"
- Invalid JSON: "Error: Failed to parse JSON plan file: {reason}"
- Unsupported format: "Error: Unsupported plan format version: {version}. Expected 1.2 or later."
- Write failure: "Error: Failed to write output file: {path}"

**Exit codes**:
- `0` - Success
- `1` - Invalid arguments or CLI usage error  
- `2` - File I/O error
- `3` - JSON parsing error
- `4` - Unsupported format version

## Success Criteria

The feature is complete when:

- [ ] Tool project created at `tools/Oocx.TfPlan2Md.TerraformShowRenderer/`
- [ ] CLI accepts `--input`, `--output`, `--no-color`, `--help`, `--version` options
- [ ] Tool reads Terraform JSON plan files (format version 1.2+)
- [ ] Output approximates `terraform show` format with high fidelity (pixel-perfect or close-enough based on implementation feasibility)
- [ ] ANSI color codes match Terraform's terminal output conventions
- [ ] All plan operations supported: create, update, delete, replace, read, no-op
- [ ] Output includes header, summary, detailed breakdown, and footer sections
- [ ] Works with comprehensive-demo plan.json file
- [ ] Clear error messages for invalid input or file errors
- [ ] Help text documents all CLI options
- [ ] Version output shows tool version
- [ ] `--no-color` flag strips ANSI codes for plain text output
- [ ] Output to stdout works when `--output` is omitted
- [ ] Tool documented in website code-examples.md
- [ ] README or docs explain tool purpose and usage

## Open Questions

None - all requirements clarified with maintainer.

## Notes

**Fidelity Target**: Aim for pixel-perfect match with real `terraform show` output. If this proves impractical during implementation, fall back to "close enough that it looks authentic to someone familiar with Terraform output" (showing same information in similar format). Plain text or significantly simplified output is NOT acceptable.

**Website Integration**: Once implemented, this tool will be used to generate "without tfplan2md" sections for website feature pages (backlog item #24). The generated output will be displayed in code blocks or pre-formatted sections to preserve ANSI color rendering.

**Maintenance Consideration**: This tool mirrors Terraform's output format, which may change across Terraform versions. The tool should aim for compatibility with Terraform 1.x output format. Future Terraform version changes may require tool updates.
