# Feature: Debug Output

## Overview

tfplan2md currently provides no diagnostic output when processing Terraform plans. Users who encounter issues (such as principal mapping failures or unexpected template behavior) have no way to troubleshoot or understand what the tool is doing internally. This feature adds comprehensive debug output capabilities to help users diagnose problems and understand tfplan2md's behavior.

## User Goals

- **Troubleshoot principal mapping issues**: When principal mappings don't work as expected, users need to see which principals were loaded, how many of each type were found, and which IDs failed to map
- **Understand template resolution**: Users need visibility into whether custom or built-in templates are being used
- **Diagnose processing issues**: Users need general diagnostic information about what tfplan2md is doing during execution
- **Choose output destination**: Users need flexibility to direct debug output to different destinations (markdown report, stderr, or file) based on their workflow

## Scope

### In Scope

- **Command-line option** to enable debug output (e.g., `--debug` or `--debug-output`)
- **Multiple output destinations**: Support for outputting debug information to:
  - An additional section appended to the markdown report
  - Standard error (stderr)
  - A separate debug log file
- **Principal mapping diagnostics**:
  - Report whether the mapping file was loaded successfully
  - Count of principals by type (users, groups, service principals, etc.)
  - List of failed mappings (IDs that were not found in the mapping file)
- **Template resolution logging**: Report which template was used (custom path or built-in)
- **Other diagnostic messages**: Log other relevant processing steps and decisions made by tfplan2md

### Out of Scope

- Verbose trace-level logging of every internal operation (this is debug output for end-users, not developer debugging)
- Performance metrics or timing information (unless specifically requested by maintainer)
- Integration with structured logging frameworks (keep it simple for a CLI tool)
- Real-time log streaming or progress indicators (static output is sufficient)

## User Experience

### Enabling Debug Output

Users enable debug output via a command-line flag:

```bash
# Append debug info to markdown report (default behavior)
tfplan2md --debug plan.json -o report.md

# Write debug info to stderr
tfplan2md --debug stderr plan.json -o report.md

# Write debug info to a separate file
tfplan2md --debug debug.log plan.json -o report.md
```

### Debug Output Content

When debug output is enabled, users will see diagnostic information such as:

**Principal Mapping Diagnostics:**
```
Principal Mapping: Loaded successfully from 'principals.json'
Principal Mapping: Found 45 users, 12 groups, 8 service principals
Principal Mapping: Failed to resolve 3 principal IDs:
  - 12345678-1234-1234-1234-123456789012
  - 87654321-4321-4321-4321-210987654321
  - abcdef12-3456-7890-abcd-ef1234567890
```

**Template Resolution:**
```
Template: Using custom template from 'my-template.scriban'
```
or
```
Template: Using built-in template (no custom template specified)
```

**Other Diagnostics:**
- Input file processing information
- Any warnings or non-fatal issues encountered
- Configuration settings that affect output

### Output Destination Behavior

**Appended to report (default):**
- Debug information is added as a new section at the end of the markdown report
- Section header: `## Debug Information` (or similar)
- Formatted as markdown (code blocks, lists, etc.)

**Stderr:**
- Plain text output to standard error
- Allows report to remain clean while debug info goes to console/logs
- Suitable for CI/CD pipelines where stderr is captured separately

**File:**
- Plain text output to specified file path
- File is created/overwritten
- Allows both report and console to remain clean

### Error Handling

- If debug output file cannot be written, report the error to stderr and continue (don't fail the entire operation)
- If principal mapping file is specified but cannot be loaded, this should be reported in debug output (not just silently fail)
- Invalid debug output destinations should result in a clear error message

## Success Criteria

- [ ] Command-line option `--debug` (or similar) is available and documented in help text
- [ ] Debug output can be directed to markdown report, stderr, or a file
- [ ] Principal mapping diagnostics are displayed when principal mapping is used:
  - [ ] Shows whether mapping file loaded successfully
  - [ ] Shows count of principals by type
  - [ ] Lists IDs that failed to map
- [ ] Template resolution is logged (custom vs built-in)
- [ ] Debug output does not break existing functionality or report formatting
- [ ] Debug output is disabled by default (no change to existing behavior)
- [ ] Help text is updated to document the debug option
- [ ] Feature is tested with various scenarios (mapping success/failure, different output destinations)

## Open Questions

1. **Flag naming**: Should it be `--debug`, `--debug-output`, `--diagnostics`, or something else?
2. **Default destination**: Should debug output default to stderr or markdown report when no destination is specified?
3. **Verbosity levels**: Should there be multiple levels of debug output (e.g., `--debug` vs `--debug-verbose`), or is a single level sufficient?
4. **Structured format**: For file output, should there be an option for JSON or other structured formats, or is plain text sufficient?
5. **Principal mapping depth**: For failed mappings, should we show just the IDs or also context about where they were referenced?
6. **Other diagnostic categories**: What other aspects of tfplan2md processing would benefit from debug output? (e.g., resource filtering, value formatting decisions, etc.)
