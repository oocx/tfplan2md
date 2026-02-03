using System.Text;

namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Collects diagnostic information during tfplan2md execution for debug output.
/// Related feature: docs/features/038-debug-output/.
/// </summary>
/// <remarks>
/// <para>
/// This class accumulates diagnostic information from various components during processing:
/// <list type="bullet">
/// <item><description>Principal mapping status (load success/failure, type counts, failed resolutions)</description></item>
/// <item><description>Template resolution decisions (which templates were used for each resource type)</description></item>
/// </list>
/// </para>
/// <para>
/// The diagnostic context is optional and only created when the --debug flag is enabled.
/// Components check for null before recording diagnostics, ensuring no performance impact
/// when debug mode is disabled.
/// </para>
/// <para>
/// Thread safety: This class is not thread-safe. It should only be accessed from a single
/// thread during report generation.
/// </para>
/// </remarks>
internal class DiagnosticContext
{
    /// <summary>
    /// Gets or sets a value indicating whether a principal mapping file was provided via CLI.
    /// </summary>
    public bool PrincipalMappingFileProvided { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the principal mapping file loaded successfully.
    /// </summary>
    /// <remarks>
    /// This is only meaningful when <see cref="PrincipalMappingFileProvided"/> is true.
    /// If the file was provided but failed to load (e.g., file not found, invalid JSON),
    /// this will be false.
    /// </remarks>
    public bool PrincipalMappingLoadedSuccessfully { get; set; }

    /// <summary>
    /// Gets or sets the path to the principal mapping file that was provided.
    /// </summary>
    /// <value>
    /// The file path if provided, otherwise null.
    /// </value>
    public string? PrincipalMappingFilePath { get; set; }

    /// <summary>
    /// Gets or sets whether the principal mapping file exists at the specified path.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// It helps distinguish between file-not-found errors and other loading errors.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public bool? PrincipalMappingFileExists { get; set; }

    /// <summary>
    /// Gets or sets whether the parent directory of the principal mapping file exists.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// It helps diagnose Docker volume mount issues where the mount point doesn't exist.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public bool? PrincipalMappingDirectoryExists { get; set; }

    /// <summary>
    /// Gets or sets the type of error that occurred when loading the principal mapping file.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// The error type determines what troubleshooting guidance is shown to the user.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public PrincipalLoadError? PrincipalMappingErrorType { get; set; }

    /// <summary>
    /// Gets or sets a user-friendly error message describing what went wrong.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// The message should be clear and actionable, not just the raw exception message.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public string? PrincipalMappingErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional technical details about the error.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// May include line/column numbers for JSON parse errors, exception details, etc.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public string? PrincipalMappingErrorDetails { get; set; }

    /// <summary>
    /// Gets the count of principals by type (e.g., "users", "groups", "service principals").
    /// </summary>
    /// <remarks>
    /// The dictionary key is the principal type name, and the value is the count of that type.
    /// Type detection is based on naming conventions or metadata in the mapping file.
    /// </remarks>
    public Dictionary<string, int> PrincipalTypeCount { get; } = new();

    /// <summary>
    /// Gets the list of principal IDs that failed to resolve, along with the resource that referenced them.
    /// </summary>
    /// <remarks>
    /// Each entry captures a failed principal lookup with context about where it was referenced.
    /// The same principal ID may appear multiple times if referenced by different resources.
    /// </remarks>
    public List<FailedPrincipalResolution> FailedResolutions { get; } = new();

    /// <summary>
    /// Gets the list of template resolution decisions made during rendering.
    /// </summary>
    /// <remarks>
    /// Each entry records which template was selected for a specific resource type.
    /// This helps users understand whether custom templates, built-in resource-specific
    /// templates, or the default template was used.
    /// </remarks>
    public List<TemplateResolution> TemplateResolutions { get; } = new();

    /// <summary>
    /// Generates a markdown section containing all collected diagnostic information.
    /// </summary>
    /// <returns>
    /// A markdown-formatted string with debug information, or a message indicating no diagnostics were collected.
    /// </returns>
    /// <remarks>
    /// The generated markdown follows this structure:
    /// <list type="bullet">
    /// <item><description>## Debug Information (level-2 heading)</description></item>
    /// <item><description>### Principal Mapping subsection (if applicable)</description></item>
    /// <item><description>### Template Resolution subsection (if applicable)</description></item>
    /// </list>
    /// All resource addresses and principal IDs are formatted in code blocks for readability.
    /// If no diagnostics were collected, returns a message indicating that.
    /// </remarks>
    public string GenerateMarkdownSection()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Debug Information");
        sb.AppendLine();

        var hasDiagnostics = false;

        // Principal Mapping section
        if (PrincipalMappingFileProvided)
        {
            hasDiagnostics = true;
            sb.AppendLine("### Principal Mapping");
            sb.AppendLine();

            if (PrincipalMappingLoadedSuccessfully)
            {
                sb.Append("Principal Mapping: Loaded successfully from '");
                sb.Append(PrincipalMappingFilePath);
                sb.AppendLine("'");

                // Type counts
                if (PrincipalTypeCount.Count > 0)
                {
                    sb.Append("- Found ");
                    var typeCountStrings = PrincipalTypeCount
                        .OrderBy(kvp => kvp.Key)
                        .Select(kvp => $"{kvp.Value} {kvp.Key}");
                    sb.AppendJoin(", ", typeCountStrings);
                    sb.AppendLine();
                }

                // Failed resolutions
                if (FailedResolutions.Count > 0)
                {
                    sb.AppendLine();
                    sb.Append("Failed to resolve ");
                    sb.Append(FailedResolutions.Count);
                    sb.Append(" principal ID");
                    if (FailedResolutions.Count != 1)
                    {
                        sb.Append('s');
                    }
                    sb.AppendLine(":");

                    foreach (var failure in FailedResolutions)
                    {
                        sb.Append("- `");
                        sb.Append(failure.PrincipalId);
                        sb.Append("` (referenced in `");
                        sb.Append(failure.ResourceAddress);
                        sb.AppendLine("`)");
                    }
                }
            }
            else
            {
                sb.Append("Principal Mapping: Failed to load from '");
                sb.Append(PrincipalMappingFilePath);
                sb.AppendLine("'");

                // Enhanced diagnostics for load failures (Issue 042)
                if (PrincipalMappingFileExists.HasValue || PrincipalMappingErrorType.HasValue)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Diagnostic Details:**");

                    // File existence check
                    if (PrincipalMappingFileExists.HasValue)
                    {
                        sb.Append("- File exists: ");
                        sb.AppendLine(PrincipalMappingFileExists.Value ? "✅" : "❌");
                    }

                    // Directory existence check
                    if (PrincipalMappingDirectoryExists.HasValue)
                    {
                        sb.Append("- Directory exists: ");
                        sb.AppendLine(PrincipalMappingDirectoryExists.Value ? "✅" : "❌");
                    }

                    // Error type
                    if (PrincipalMappingErrorType.HasValue)
                    {
                        sb.Append("- Error type: ");
                        sb.AppendLine(PrincipalMappingErrorType.Value.ToString());
                    }

                    // Error message
                    if (!string.IsNullOrEmpty(PrincipalMappingErrorMessage))
                    {
                        sb.Append("- Error message: ");
                        sb.AppendLine(PrincipalMappingErrorMessage);
                    }

                    // Error details
                    if (!string.IsNullOrEmpty(PrincipalMappingErrorDetails))
                    {
                        sb.Append("- Details: ");
                        sb.AppendLine(PrincipalMappingErrorDetails);
                    }

                    // Common solutions based on error type
                    if (PrincipalMappingErrorType.HasValue)
                    {
                        sb.AppendLine();
                        sb.AppendLine("**Common Solutions:**");

                        switch (PrincipalMappingErrorType.Value)
                        {
                            case PrincipalLoadError.FileNotFound:
                                sb.AppendLine("1. Verify the file path is correct");
                                sb.AppendLine("2. If using Docker, ensure the file is mounted:");
                                sb.AppendLine("   ```bash");
                                sb.AppendLine("   docker run -v $(pwd):/data oocx/tfplan2md \\");
                                sb.AppendLine("     --principal-mapping /data/principals.json \\");
                                sb.AppendLine("     /data/plan.json");
                                sb.AppendLine("   ```");
                                sb.AppendLine("3. Check the file exists on your host system");
                                break;

                            case PrincipalLoadError.DirectoryNotFound:
                                sb.AppendLine("1. Verify the directory path exists");
                                sb.AppendLine("2. If using Docker, the directory must be mounted:");
                                sb.AppendLine("   ```bash");
                                sb.AppendLine("   docker run -v /host/path:/data oocx/tfplan2md \\");
                                sb.AppendLine("     --principal-mapping /data/principals.json \\");
                                sb.AppendLine("     /data/plan.json");
                                sb.AppendLine("   ```");
                                sb.AppendLine("3. Check directory permissions and accessibility");
                                break;

                            case PrincipalLoadError.JsonParseError:
                                sb.AppendLine("1. Validate JSON syntax using `jq` or an online validator");
                                sb.AppendLine("2. Check for trailing commas (not allowed in JSON)");
                                sb.AppendLine("3. Ensure all strings are properly quoted");
                                sb.AppendLine();
                                sb.AppendLine("**Expected Format:**");
                                sb.AppendLine("```json");
                                sb.AppendLine("{");
                                sb.AppendLine("  \"00000000-0000-0000-0000-000000000001\": \"Jane Doe (User)\",");
                                sb.AppendLine("  \"11111111-1111-1111-1111-111111111111\": \"DevOps Team (Group)\"");
                                sb.AppendLine("}");
                                sb.AppendLine("```");
                                break;

                            case PrincipalLoadError.AccessDenied:
                                sb.AppendLine("1. Check file permissions: `ls -l <file>`");
                                sb.AppendLine("2. Ensure the file is readable: `chmod +r <file>`");
                                sb.AppendLine("3. If using Docker, check container user permissions");
                                break;

                            case PrincipalLoadError.EmptyFile:
                                sb.AppendLine("1. Verify the file contains principal mappings");
                                sb.AppendLine("2. Use Azure CLI to generate principal mappings:");
                                sb.AppendLine("   ```bash");
                                sb.AppendLine("   az ad user list --query \"[].{id:id, name:displayName}\" -o json");
                                sb.AppendLine("   ```");
                                break;

                            case PrincipalLoadError.UnknownError:
                                sb.AppendLine("1. Check the error details above");
                                sb.AppendLine("2. Verify file accessibility and format");
                                sb.AppendLine("3. Check system logs for additional information");
                                break;
                        }
                    }
                }
            }

            sb.AppendLine();
        }

        // Template Resolution section
        if (TemplateResolutions.Count > 0)
        {
            hasDiagnostics = true;
            sb.AppendLine("### Template Resolution");
            sb.AppendLine();

            // Group by unique resource types (avoid duplicates)
            var uniqueResolutions = TemplateResolutions
                .GroupBy(tr => tr.ResourceType)
                .Select(g => g.First())
                .OrderBy(tr => tr.ResourceType);

            foreach (var resolution in uniqueResolutions)
            {
                sb.Append("- `");
                sb.Append(resolution.ResourceType);
                sb.Append("`: ");
                sb.AppendLine(resolution.TemplateSource);
            }

            sb.AppendLine();
        }

        if (!hasDiagnostics)
        {
            sb.AppendLine("No diagnostics collected.");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
