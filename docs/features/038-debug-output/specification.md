# Feature: Debug Output

## Overview

tfplan2md currently provides no diagnostic output when processing Terraform plans. Users who encounter issues (such as principal mapping failures or unexpected template behavior) have no way to troubleshoot or understand what the tool is doing internally. This feature adds comprehensive debug output capabilities to help users diagnose problems and understand tfplan2md's behavior.

## User Goals

- **Troubleshoot principal mapping issues**: When principal mappings don't work as expected, users need to see which principals were loaded, how many of each type were found, and which IDs failed to map with context about where they're used
- **Understand template resolution**: Users need visibility into whether custom or built-in templates are being used
- **Diagnose processing issues**: Users need general diagnostic information about what tfplan2md is doing during execution

## Scope

### In Scope

- **Command-line option**: `--debug` flag to enable debug output
- **Output destination**: Debug information appended to the markdown report as an additional section
- **Principal mapping diagnostics**:
  - Report whether the mapping file was loaded successfully
  - Count of principals by type (users, groups, service principals, etc.)
  - List of failed mappings (IDs that were not found in the mapping file) with context showing which resource referenced each ID
- **Template resolution logging**: Report which template was used (custom path or built-in)
- **Markdown formatting**: Debug output formatted as markdown for consistency with the report

### Out of Scope

- Verbose trace-level logging of every internal operation (this is debug output for end-users, not developer debugging)
- Performance metrics or timing information
- Integration with structured logging frameworks (keep it simple for a CLI tool)
- Real-time log streaming or progress indicators (static output is sufficient)
- Multiple output destinations (stderr, file) - may be added in future if needed
- Multiple verbosity levels - start simple with single level

## User Experience

### Enabling Debug Output

Users enable debug output via the `--debug` command-line flag. When enabled without additional arguments, debug information is appended to the markdown report by default:

```bash
# Append debug info to markdown report (default behavior)
tfplan2md --debug plan.json -o report.md
```

### Debug Output Content

When debug output is enabled, users will see diagnostic information such as:

**Principal Mapping Diagnostics:**
```
Principal Mapping: Loaded successfully from 'principals.json'
Principal Mapping: Found 45 users, 12 groups, 8 service principals
Principal Mapping: Failed to resolve 3 principal IDs:
  - 12345678-1234-1234-1234-123456789012 (referenced in azurerm_role_assignment.example)
  - 87654321-4321-4321-4321-210987654321 (referenced in azurerm_role_assignment.reader)
  - abcdef12-3456-7890-abcd-ef1234567890 (referenced in azurerm_role_assignment.contributor)
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
- This is the default behavior when `--debug` is specified

### Error Handling

- If principal mapping file is specified but cannot be loaded, this should be reported in debug output (not just silently fail)
- Debug output generation should not cause the tool to fail if there are issues collecting diagnostics

### Enhanced Error Diagnostics (Issue 042)

When principal mapping fails to load, the debug output provides comprehensive diagnostics to help users troubleshoot:

**File Not Found:**
```markdown
### Principal Mapping

Principal Mapping: Failed to load from '/data/principals.json'

**Diagnostic Details:**
- File exists: ❌
- Directory exists: ✅
- Error type: FileNotFound
- Error message: File not found
- Details: Could not find file '/data/principals.json'

**Common Solutions:**
1. Verify the file path is correct
2. If using Docker, ensure the file is mounted:
   ```bash
   docker run -v $(pwd):/data oocx/tfplan2md \
     --principal-mapping /data/principals.json \
     /data/plan.json
   ```
3. Check the file exists on your host system
```

**JSON Parse Error:**
```markdown
### Principal Mapping

Principal Mapping: Failed to load from '/data/principals.json'

**Diagnostic Details:**
- File exists: ✅
- Directory exists: ✅
- Error type: JsonParseError
- Error message: Invalid JSON syntax
- Details: ExpectedStartOfPropertyNotFound at line 3, column 15

**Common Solutions:**
1. Validate JSON syntax using `jq` or an online validator
2. Check for trailing commas (not allowed in JSON)
3. Ensure all strings are properly quoted

**Expected Format:**
```json
{
  "00000000-0000-0000-0000-000000000001": "Jane Doe (User)",
  "11111111-1111-1111-1111-111111111111": "DevOps Team (Group)"
}
```
```

**Directory Not Found:**
```markdown
### Principal Mapping

Principal Mapping: Failed to load from '/nonexistent/dir/principals.json'

**Diagnostic Details:**
- File exists: ❌
- Directory exists: ❌
- Error type: DirectoryNotFound
- Error message: Directory not found
- Details: Could not find directory '/nonexistent/dir'

**Common Solutions:**
1. Verify the directory path exists
2. If using Docker, the directory must be mounted:
   ```bash
   docker run -v /host/path:/data oocx/tfplan2md \
     --principal-mapping /data/principals.json \
     /data/plan.json
   ```
3. Check directory permissions and accessibility
```

These enhanced diagnostics help users:
- Quickly identify the specific error type
- Understand file system issues (file vs directory problems)
- Get actionable Docker-specific guidance when running in containers
- See line and column numbers for JSON syntax errors
- Learn the expected file format with examples

## Success Criteria

- [x] Command-line option `--debug` is available and documented in help text
- [x] Debug output is appended to the markdown report as a new section when `--debug` is enabled
- [x] Principal mapping diagnostics are displayed when principal mapping is used:
  - [x] Shows whether mapping file loaded successfully
  - [x] Shows count of principals by type
  - [x] Lists IDs that failed to map with context (which resource referenced them)
  - [x] **(Enhanced)** Shows file existence status
  - [x] **(Enhanced)** Shows directory existence status
  - [x] **(Enhanced)** Shows specific error type (FileNotFound, JsonParseError, etc.)
  - [x] **(Enhanced)** Includes line/column information for JSON parse errors
  - [x] **(Enhanced)** Provides Docker-specific troubleshooting guidance
  - [x] **(Enhanced)** Shows actionable solutions based on error type
- [x] Template resolution is logged (custom vs built-in)
- [x] Debug output does not break existing functionality or report formatting
- [x] Debug output is disabled by default (no change to existing behavior)
- [x] Help text is updated to document the debug option
- [x] Feature is tested with various scenarios (mapping success/failure, with/without debug enabled)

## Design Decisions

Based on maintainer input, the following design decisions have been made:

1. **Flag naming**: Use `--debug` flag
2. **Default destination**: Debug output is appended to the markdown report
3. **Verbosity levels**: Start with a single debug level; additional levels may be added later if needed
4. **Output format**: Markdown format (consistent with the report itself)
5. **Principal mapping depth**: Include context showing which resource referenced each failed principal ID
6. **Additional diagnostic categories**: None initially; focus on principal mapping and template resolution
