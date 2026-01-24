# Issue: Add More Context to --debug for Principal Loading

## Problem Description

Users encounter issues when tfplan2md cannot load a principal mapping file, but the current `--debug` output provides minimal actionable information to help them fix the problem. While the debug feature (Feature 038) currently reports whether the principal mapping file loaded successfully or failed, it doesn't provide enough detail about **why** it failed or **how** to resolve the issue, especially when running in Docker containers.

### Current Behavior

When a principal mapping file fails to load, the `--debug` output shows:
```markdown
### Principal Mapping

Principal Mapping: Failed to load from '/path/to/file.json'
```

Additionally, a warning is written to stderr:
```
Warning: Could not read principal mapping file '/path/to/file.json': <exception message>
```

This minimal feedback leaves users uncertain about:
- Whether the file exists at all
- Whether the parent directory is accessible
- What specific parsing error occurred (if it's a JSON syntax issue)
- How to correctly mount files into Docker containers
- Whether similar issues affect template loading

## Steps to Reproduce

### Scenario 1: File Not Found
```bash
docker run -v $(pwd):/data oocx/tfplan2md \
  --debug \
  --principal-mapping /data/principals.json \
  /data/plan.json
```
When `principals.json` doesn't exist, output shows "Failed to load" but doesn't confirm if `/data/` directory exists or is accessible.

### Scenario 2: Directory Not Mounted
```bash
docker run oocx/tfplan2md \
  --debug \
  --principal-mapping /data/principals.json \
  /data/plan.json
```
When volume mount is forgotten, the error is the same as Scenario 1, providing no hint about the Docker mount issue.

### Scenario 3: JSON Parsing Error
```bash
# principals.json contains invalid JSON
echo '{"id1": "Name1", invalid}' > principals.json
docker run -v $(pwd):/data oocx/tfplan2md \
  --debug \
  --principal-mapping /data/principals.json \
  /data/plan.json
```
Error shows generic exception message without line/column information or specific syntax error details.

### Scenario 4: Template File Not Found
```bash
docker run -v $(pwd):/data oocx/tfplan2md \
  --debug \
  --template /data/custom-template.sbn \
  /data/plan.json
```
Template failures throw exceptions but debug output doesn't provide actionable guidance for template-related errors.

## Expected Behavior

The `--debug` output should provide comprehensive, actionable diagnostic information that helps users fix problems quickly:

### For Principal Mapping Failures

```markdown
### Principal Mapping

Principal Mapping: Failed to load from '/data/principals.json'

**Diagnostic Details:**
- File exists: ❌ No
- Parent directory exists: ✅ Yes (/data/)
- Parent directory accessible: ✅ Yes

**Common Solutions:**
1. Verify the file path is correct
2. If using Docker, ensure the file is mounted:
   ```
   docker run -v $(pwd):/data oocx/tfplan2md \
     --principal-mapping /data/principals.json \
     /data/plan.json
   ```
3. Check file permissions (file must be readable)
```

### For JSON Parsing Errors

```markdown
### Principal Mapping

Principal Mapping: Failed to load from '/data/principals.json'

**Diagnostic Details:**
- File exists: ✅ Yes
- File size: 247 bytes
- Parsing error: Invalid JSON syntax at line 3, column 15
- Error message: Unexpected character 'i' after property value

**Expected Format:**
The principal mapping file should be valid JSON with this structure:
```json
{
  "principal-guid-1": "Display Name (Type)",
  "principal-guid-2": "Another Name (Type)"
}
```

**Common Solutions:**
1. Validate JSON syntax using `jq` or an online validator
2. Check for trailing commas (not allowed in JSON)
3. Ensure all strings are properly quoted
```

### For Template Loading Failures

Similar detailed diagnostics should apply to template loading errors, providing:
- File existence checks
- Directory accessibility
- Parsing errors with line/column information
- Docker mount instructions specific to templates
- Examples of correct template usage

## Root Cause Analysis

### Affected Components

#### 1. Principal Loading Error Handling
- **File**: `src/Oocx.TfPlan2Md/Azure/PrincipalMapper.cs`
- **Lines**: 149-210 (LoadMappings method)
- **Issue**: The try-catch block in `LoadMappings()` catches all exceptions but only records a boolean failure flag and writes a generic warning to stderr. It doesn't distinguish between:
  - File not found (`FileNotFoundException`)
  - Directory not found (`DirectoryNotFoundException`)
  - Permission denied (`UnauthorizedAccessException`)
  - JSON parsing errors (`JsonException`)
  - Other IO errors (`IOException`)

#### 2. Diagnostic Context Limited Scope
- **File**: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`
- **Lines**: 28-193
- **Issue**: The `DiagnosticContext` class only stores:
  - `PrincipalMappingFileProvided` (bool)
  - `PrincipalMappingLoadedSuccessfully` (bool)
  - `PrincipalMappingFilePath` (string)
  
  It doesn't store:
  - Detailed error information (exception type, message, parsing location)
  - File system diagnostic results (file exists, directory exists, permissions)
  - Actionable remediation suggestions

#### 3. Template Loading Error Handling
- **File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs`
- **Lines**: 125-145 (ResolveTemplateText methods)
- **Issue**: Template loading throws `MarkdownRenderException` immediately when file is not found, preventing debug diagnostics from being collected. The error message is minimal:
  ```csharp
  throw new MarkdownRenderException($"Template '{templateNameOrPath}' not found. Available built-in templates: {string.Join(", ", BuiltInTemplates)}");
  ```
  
  It doesn't provide:
  - Docker mount instructions for custom templates
  - File system diagnostics (directory exists, permissions)
  - Distinction between "not found" vs "not accessible"

#### 4. Debug Output Generation
- **File**: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`
- **Method**: `GenerateMarkdownSection()` (lines 96-192)
- **Issue**: The markdown generation only outputs the limited information stored in the context. It doesn't provide:
  - User guidance for common Docker scenarios
  - Links to documentation
  - Structured troubleshooting steps

### What's Broken

1. **Insufficient Exception Handling**: The `LoadMappings()` method uses a single catch-all `catch (Exception ex)` block that treats all errors the same way. Different error types require different diagnostic information and user guidance.

2. **No File System Diagnostics**: When a file fails to load, the code doesn't check:
   - Does the parent directory exist?
   - Is the parent directory accessible?
   - Is it a permissions issue vs a missing file issue?

3. **Limited Error Context**: The `DiagnosticContext` was designed to track "what happened" (success/failure) but not "why it failed" or "how to fix it."

4. **No User Guidance**: The debug output provides diagnostic facts but no actionable remediation steps, especially for the Docker use case (which is the recommended deployment method).

5. **Template Loading Throws Early**: Template errors throw exceptions before debug diagnostics can be collected and formatted, preventing users from seeing helpful troubleshooting information in the markdown output.

6. **Inconsistent Error Handling**: Principal mapping silently falls back to raw IDs with a stderr warning, while template loading throws exceptions. This inconsistency makes debugging harder.

### Why It Happened

1. **Feature 038 Scope**: The initial debug feature (038) focused on tracking successful operations (principal counts, template resolutions, failed ID lookups) rather than comprehensive error diagnostics. This was appropriate for the MVP but left gaps in error scenarios.

2. **Graceful Degradation Philosophy**: The design intentionally allows the tool to continue when principal mapping fails (falling back to raw IDs), which is good for reliability but resulted in minimal error reporting.

3. **Docker Context Not Prioritized**: The error messages and diagnostics were written for general file system errors without specific consideration for the Docker container context, which is the primary deployment method and introduces unique troubleshooting challenges (volume mounts, path mapping).

4. **Exception Message Reliance**: The current implementation relies on generic .NET exception messages (e.g., "Could not find file") which don't provide context-specific guidance.

## Gap Analysis: Current vs Desired Behavior

| Scenario | Current Debug Output | Desired Debug Output |
|----------|---------------------|---------------------|
| **File not found** | "Failed to load from 'path'" | File exists: ❌, Directory exists: ✅/❌, Docker mount instructions |
| **Directory not found** | "Failed to load from 'path'" | Directory exists: ❌, Parent path info, Docker volume mount example |
| **JSON parse error** | Generic exception message in stderr | Specific line/column, syntax error description, JSON format example |
| **Permission denied** | Generic exception message in stderr | Permission issue identified, chmod/chown suggestions, Docker uid/gid info |
| **Template not found** | Exception thrown (no debug output) | File existence check, Docker mount example, available templates list |
| **Template parse error** | Exception with line numbers | Enhanced error with context, template syntax link, common mistakes |
| **No mapping file** | (no output, working as designed) | No change needed - this is not an error |
| **Success but no IDs found** | Shows count of 0 principals | Should validate file format, warn if empty or malformed structure |

## Specific Scenarios to Address

### 1. Principal Mapping File Not Found

**Detection:**
- Catch `FileNotFoundException` specifically
- Check `File.Exists(mappingFile)`
- Check `Directory.Exists(Path.GetDirectoryName(mappingFile))`

**Diagnostics to Collect:**
- File path (already collected)
- File exists: boolean
- Parent directory path
- Parent directory exists: boolean
- Parent directory accessible: boolean (try `Directory.GetFiles()`)

**User Guidance:**
```markdown
**Common Solutions:**
1. Verify the file path: `<path>`
2. If using Docker, ensure proper volume mount:
   ```bash
   docker run -v $(pwd):/data oocx/tfplan2md \
     --principal-mapping /data/principals.json \
     /data/plan.json
   ```
3. Check the file exists on your host system
```

### 2. Directory Not Found

**Detection:**
- `DirectoryNotFoundException` or parent directory doesn't exist

**Diagnostics to Collect:**
- Full path attempted
- Parent directory path
- Whether parent's parent exists (to identify mount point issues)

**User Guidance:**
```markdown
**Common Solutions:**
1. Verify the directory path exists: `<directory>`
2. If using Docker, the directory must be mounted:
   - Host path: `$(pwd)` or absolute path
   - Container path: `/data` (or your chosen mount point)
   - Example: `docker run -v /host/path:/data oocx/tfplan2md ...`
3. Check directory permissions and accessibility
```

### 3. JSON Parsing Error

**Detection:**
- Catch `JsonException`
- Extract `LineNumber`, `BytePositionInLine` if available

**Diagnostics to Collect:**
- File size
- Line number of error (if available)
- Column/position (if available)
- Specific error message
- First few lines of file (if readable)

**User Guidance:**
```markdown
**Expected Format:**
```json
{
  "00000000-0000-0000-0000-000000000001": "Jane Doe (User)",
  "11111111-1111-1111-1111-111111111111": "DevOps Team (Group)"
}
```

**Common JSON Errors:**
- Trailing commas (not allowed in JSON)
- Missing quotes around strings
- Unescaped special characters
- Mismatched brackets or braces

**Validation Tools:**
- Command line: `jq . principals.json`
- Online: https://jsonlint.com
```

### 4. Permission Denied

**Detection:**
- Catch `UnauthorizedAccessException`
- Check file permissions (if possible on platform)

**Diagnostics to Collect:**
- File owner/permissions (Unix-like systems)
- Whether running in container
- Container user ID (if in Docker)

**User Guidance:**
```markdown
**Common Solutions:**
1. Check file permissions: `ls -l <file>`
2. Ensure the file is readable: `chmod +r <file>`
3. If using Docker, check container user permissions:
   - Docker runs as a specific user (often root or a non-root user)
   - File must be readable by the container's user
   - Consider: `chmod 644 <file>` for host files
```

### 5. Template File Not Found

**Detection:**
- Template loading in `ResolveTemplateText()` checks `File.Exists()`
- Should collect diagnostics before throwing exception

**Diagnostics to Collect:**
- Template path
- File exists: boolean
- Directory exists: boolean
- List of built-in templates available

**User Guidance:**
```markdown
**Common Solutions:**
1. Verify template file exists: `<path>`
2. Use a built-in template: `--template default` or `--template summary`
3. If using a custom template with Docker:
   ```bash
   docker run -v $(pwd):/data oocx/tfplan2md \
     --template /data/my-template.sbn \
     /data/plan.json
   ```
4. Check template file extension (.sbn or .scriban)

**Available Built-in Templates:**
- default: Full report with resource changes
- summary: Compact summary with counts

**Documentation:**
- Template syntax: https://github.com/scriban/scriban
```

### 6. Template Parsing Error

**Detection:**
- Catch template parse errors with line/column info
- Already partially handled in `RenderWithTemplate()`

**Diagnostics to Collect:**
- Template path
- Parse error message
- Line and column numbers
- Specific syntax error

**User Guidance:**
```markdown
**Common Template Errors:**
- Unclosed tags: `{{ without }}`
- Invalid Scriban syntax
- Undefined functions or filters
- Incorrect variable references

**Resources:**
- Scriban syntax guide: https://github.com/scriban/scriban/blob/master/doc/language.md
- Default template example: `/examples/comprehensive-demo/`
```

### 7. Empty or Malformed Principal Mapping

**Detection:**
- File loads successfully but `parsed` is empty or null
- Check for `parsed.Count == 0` after successful deserialization

**Diagnostics to Collect:**
- File size
- Number of principals loaded (0)
- Whether JSON is valid but empty

**User Guidance:**
```markdown
**Warning:**
Principal mapping file is empty or contains no valid entries.

**Expected Format:**
```json
{
  "principal-guid-1": "Display Name (Type)",
  "principal-guid-2": "Another Name (Type)"
}
```

**To generate principal mappings using Azure CLI:**
```bash
# List users
az ad user list --query "[].{id:id, name:displayName}" -o json

# List groups  
az ad group list --query "[].{id:id, name:displayName}" -o json

# List service principals
az ad sp list --query "[].{id:id, name:displayName}" -o json
```
```

## Impact on User Experience

### Current Pain Points

1. **Trial and Error**: Users must repeatedly try different paths and configurations without clear feedback about what's wrong.

2. **Docker Confusion**: New users don't understand Docker volume mounts and get identical "file not found" errors whether the file is missing or the mount is incorrect.

3. **Time Waste**: Consulting external documentation, Azure CLI, or support channels to understand cryptic errors.

4. **Frustration with JSON**: Syntax errors in JSON files are reported with generic messages, forcing users to use external tools to debug.

5. **Incomplete Diagnostics**: The `--debug` flag promises diagnostic information but doesn't deliver comprehensive troubleshooting data for common error scenarios.

### Benefits of Enhancement

1. **Self-Service Resolution**: Users can diagnose and fix issues themselves using the detailed debug output.

2. **Docker Onboarding**: Clear, actionable instructions for Docker volume mounts reduce the learning curve.

3. **Faster Iteration**: Specific error locations (line/column for JSON) allow immediate fixes without external validation tools.

4. **Reduced Support Burden**: Comprehensive diagnostics reduce the need for community support or issue reports for common problems.

5. **Professional UX**: Detailed, helpful error messages demonstrate quality and polish, improving user confidence in the tool.

6. **Consistency**: Unified approach to error diagnostics across all loading operations (principals, templates, input files).

## Suggested Fix Approach

### Phase 1: Enhanced Diagnostic Context (Core Infrastructure)

**Goal**: Expand `DiagnosticContext` to capture detailed error information.

**Changes**:
1. Add new properties to `DiagnosticContext`:
   ```csharp
   public string? PrincipalMappingErrorType { get; set; }
   public string? PrincipalMappingErrorMessage { get; set; }
   public string? PrincipalMappingErrorDetails { get; set; }
   public Dictionary<string, bool> FileSystemChecks { get; } = new();
   ```

2. Create a new `LoadingDiagnostic` record for structured error info:
   ```csharp
   public record LoadingDiagnostic(
       string Path,
       bool FileExists,
       bool DirectoryExists,
       string? ErrorType,
       string? ErrorMessage,
       string? ErrorDetails,
       List<string> Suggestions
   );
   ```

3. Update `DiagnosticContext` to store loading diagnostics for both principals and templates.

### Phase 2: Enhanced Principal Loading (PrincipalMapper.cs)

**Goal**: Add comprehensive file system diagnostics and specific error handling.

**Changes to LoadMappings()**:

1. **Pre-flight Checks** (before file read):
   ```csharp
   var fileExists = File.Exists(mappingFile);
   var directory = Path.GetDirectoryName(mappingFile);
   var directoryExists = !string.IsNullOrEmpty(directory) && Directory.Exists(directory);
   
   if (diagnosticContext != null)
   {
       diagnosticContext.FileSystemChecks["PrincipalMappingFileExists"] = fileExists;
       diagnosticContext.FileSystemChecks["PrincipalMappingDirectoryExists"] = directoryExists;
   }
   ```

2. **Specific Exception Handling**:
   ```csharp
   catch (FileNotFoundException ex)
   {
       // Specific handling for file not found
   }
   catch (DirectoryNotFoundException ex)
   {
       // Specific handling for directory not found
   }
   catch (UnauthorizedAccessException ex)
   {
       // Specific handling for permission denied
   }
   catch (JsonException ex)
   {
       // Specific handling for JSON parsing errors
       // Extract line number, column, and error details
   }
   catch (Exception ex)
   {
       // Fallback for unexpected errors
   }
   ```

3. **Empty File Detection**:
   ```csharp
   if (parsed is not null && parsed.Count == 0)
   {
       // Record warning about empty mapping file
   }
   ```

### Phase 3: Enhanced Template Loading (MarkdownRenderer.cs)

**Goal**: Add diagnostics for template loading instead of immediate exceptions.

**Changes to ResolveTemplateText()**:

1. **Collect Diagnostics Before Throwing**:
   ```csharp
   if (!File.Exists(templateNameOrPath))
   {
       if (_diagnosticContext != null)
       {
           // Record detailed diagnostics about template loading failure
           var directory = Path.GetDirectoryName(templateNameOrPath);
           _diagnosticContext.TemplateLoadingError = new LoadingDiagnostic(
               Path: templateNameOrPath,
               FileExists: false,
               DirectoryExists: !string.IsNullOrEmpty(directory) && Directory.Exists(directory),
               ErrorType: "FileNotFound",
               ErrorMessage: $"Template '{templateNameOrPath}' not found",
               ErrorDetails: $"Available built-in templates: {string.Join(", ", BuiltInTemplates)}",
               Suggestions: new List<string>
               {
                   "Verify the template file exists",
                   "Use a built-in template: --template default or --template summary",
                   "If using Docker, ensure proper volume mount"
               }
           );
       }
       
       throw new MarkdownRenderException(...);
   }
   ```

2. **Enhanced Template Parse Errors**:
   ```csharp
   if (template.HasErrors)
   {
       if (_diagnosticContext != null)
       {
           // Record template parsing diagnostics with line numbers
       }
       
       var errors = string.Join(Environment.NewLine, template.Messages);
       throw new MarkdownRenderException($"Template parsing failed: {errors}");
   }
   ```

### Phase 4: Enhanced Markdown Output (DiagnosticContext.cs)

**Goal**: Generate comprehensive, actionable debug output with user guidance.

**Changes to GenerateMarkdownSection()**:

1. **Enhanced Principal Mapping Section**:
   ```markdown
   ### Principal Mapping
   
   Principal Mapping: Failed to load from '<path>'
   
   **Diagnostic Details:**
   - File exists: ❌ No
   - Directory exists: ✅ Yes (<directory>)
   - Error type: FileNotFoundException
   - Error message: <message>
   
   **Common Solutions:**
   1. Verify the file path is correct
   2. If using Docker, ensure the file is mounted: ...
   3. Check file permissions
   
   **Expected Format:**
   ... (show JSON example)
   ```

2. **Add Template Diagnostics Section**:
   ```markdown
   ### Template Loading
   
   Template: Failed to load from '<path>'
   
   **Diagnostic Details:**
   ... (similar structure to principal mapping)
   
   **Available Built-in Templates:**
   - default: Full report with resource changes
   - summary: Compact summary with counts
   
   **Resources:**
   ... (documentation links)
   ```

3. **Docker-Specific Guidance**: Add a section specifically for Docker users with common patterns:
   ```markdown
   ### Docker Usage Tips
   
   When using tfplan2md with Docker, files must be mounted as volumes:
   
   **Principal mapping:**
   ```bash
   docker run -v $(pwd):/data oocx/tfplan2md \
     --principal-mapping /data/principals.json \
     /data/plan.json
   ```
   
   **Custom template:**
   ```bash
   docker run -v $(pwd):/data oocx/tfplan2md \
     --template /data/my-template.sbn \
     /data/plan.json
   ```
   ```

### Phase 5: Testing and Validation

**New Tests Required**:

1. **Unit Tests for Diagnostic Collection**:
   - Test file system checks (file exists, directory exists)
   - Test specific exception types (FileNotFoundException, JsonException, etc.)
   - Test error message formatting
   - Test suggestion generation

2. **Integration Tests for Debug Output**:
   - Test principal mapping with missing file
   - Test principal mapping with missing directory
   - Test principal mapping with invalid JSON (various syntax errors)
   - Test principal mapping with empty file
   - Test principal mapping with permission denied
   - Test template loading with missing file
   - Test template loading with parse errors

3. **End-to-End Docker Tests**:
   - Test error messages when volume mount is missing
   - Test error messages when path is incorrect in container
   - Validate Docker-specific guidance in output

4. **Markdown Output Tests**:
   - Verify debug section formatting
   - Verify code blocks render correctly
   - Verify suggestions are actionable
   - Verify error details are clear

### Phase 6: Documentation Updates

**README.md**:
- Update `--debug` flag documentation with examples of error scenarios
- Add troubleshooting section with common Docker issues
- Add link to principal mapping generation guide

**Feature 038 Documentation**:
- Update specification to include error diagnostic scenarios
- Document new diagnostic properties
- Add examples of enhanced debug output

**New Troubleshooting Guide** (docs/troubleshooting.md):
- Common principal mapping errors
- Docker volume mount issues
- Template loading problems
- JSON syntax validation
- File permission issues

## Related Tests That Should Pass

After implementing the enhancements, the following tests should continue to pass:

### Existing Tests (Regression Prevention)
- [ ] `PrincipalMapperDiagnosticsTests.*` - All existing diagnostic tests
- [ ] `DiagnosticContextTests.*` - All existing context tests
- [ ] `DebugOutputIntegrationTests.*` - All existing integration tests
- [ ] `CliParserTests.*` - CLI parsing still works
- [ ] `MarkdownRendererTests.*` - Template loading still works

### New Tests (Required for Feature)
- [ ] `PrincipalMapper_FileNotFound_RecordsDetailedDiagnostics`
- [ ] `PrincipalMapper_DirectoryNotFound_RecordsDetailedDiagnostics`
- [ ] `PrincipalMapper_JsonParseError_RecordsLineAndColumn`
- [ ] `PrincipalMapper_PermissionDenied_RecordsAccessError`
- [ ] `PrincipalMapper_EmptyFile_RecordsWarning`
- [ ] `MarkdownRenderer_TemplateNotFound_RecordsDiagnostics`
- [ ] `MarkdownRenderer_TemplateParseError_RecordsLineNumbers`
- [ ] `DiagnosticContext_FileSystemChecks_IncludedInOutput`
- [ ] `DiagnosticContext_ErrorDetails_FormattedWithSuggestions`
- [ ] `DiagnosticContext_DockerGuidance_IncludedForFilePaths`
- [ ] `EndToEnd_DebugWithMissingFile_GeneratesActionableOutput`
- [ ] `EndToEnd_DebugWithInvalidJson_ShowsParseError`

## Additional Context

### Docker Context Detection

Consider detecting when tfplan2md is running inside a Docker container (by checking for `/.dockerenv` or `/proc/1/cgroup`) to automatically include Docker-specific guidance in error messages.

### Progressive Enhancement

The enhancement should be implemented in phases to allow incremental testing and validation:
1. Phase 1-2: Principal mapping diagnostics (highest user impact)
2. Phase 3: Template loading diagnostics
3. Phase 4: Enhanced markdown output with suggestions
4. Phase 5-6: Testing and documentation

### Backward Compatibility

All enhancements must maintain backward compatibility:
- `--debug` flag continues to work as before (adds more info, doesn't break existing output)
- Non-debug mode is unchanged
- Existing tests continue to pass
- API signatures for `PrincipalMapper` and `MarkdownRenderer` remain compatible

### Performance Considerations

File system checks (File.Exists, Directory.Exists) have minimal performance impact but should only be performed when debug mode is enabled or when an error occurs, not during normal successful operations.

### Security Considerations

- Don't expose sensitive file system information (full paths from container perspective may differ from host)
- Sanitize error messages to avoid leaking system details
- Be cautious about displaying file contents in error messages
- Limit output to diagnostic information, not actual file content

## Definition of Done

This analysis is complete and ready for Developer handoff when:
- [x] Root cause is clearly identified in PrincipalMapper.cs and MarkdownRenderer.cs
- [x] All specific error scenarios are documented with examples
- [x] Gap analysis shows current vs desired behavior
- [x] Implementation approach is broken into phases
- [x] Impact on user experience is articulated
- [x] Testing requirements are defined
- [x] Related documentation updates are identified
- [x] Backward compatibility concerns are addressed
- [x] Security and performance considerations are noted

## References

- Feature 038: Debug Output - `docs/features/038-debug-output/specification.md`
- Feature 006: Principal Mapping - `docs/features/006-role-assignment-readable-display/specification.md`
- Current Implementation: `src/Oocx.TfPlan2Md/Azure/PrincipalMapper.cs`
- Diagnostics: `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`
- Template Loading: `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs`
- Existing Tests: `src/tests/Oocx.TfPlan2Md.TUnit/Azure/PrincipalMapperDiagnosticsTests.cs`
