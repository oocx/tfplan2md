using System.Text;

namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Collects diagnostic information during tfplan2md execution for debug output.
/// Related feature: docs/features/038-debug-output/
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
public class DiagnosticContext
{
    /// <summary>
    /// Gets or sets whether a principal mapping file was provided via CLI.
    /// </summary>
    public bool PrincipalMappingFileProvided { get; set; }

    /// <summary>
    /// Gets or sets whether the principal mapping file loaded successfully.
    /// </summary>
    /// <remarks>
    /// This is only meaningful when <see cref="PrincipalMappingFileProvided"/> is true.
    /// If the file was provided but failed to load (e.g., file not found, invalid JSON),
    /// this will be false.
    /// </remarks>
    public bool PrincipalMappingLoadedSuccessfully { get; set; }

    /// <summary>
    /// Gets or sets the type of error that occurred when loading the principal mapping file.
    /// </summary>
    /// <remarks>
    /// This provides detailed information about why the mapping file failed to load,
    /// enabling users to take appropriate corrective action.
    /// </remarks>
    public PrincipalMappingErrorType PrincipalMappingErrorType { get; set; } = PrincipalMappingErrorType.None;

    /// <summary>
    /// Gets or sets the error message from a failed principal mapping file load.
    /// </summary>
    /// <value>
    /// The detailed error message, or null if no error occurred.
    /// </value>
    public string? PrincipalMappingErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the parent directory of the principal mapping file exists.
    /// </summary>
    /// <remarks>
    /// This is checked when the mapping file is not found, to help diagnose whether
    /// the issue is an invalid directory path (e.g., missing Docker volume mount) or
    /// just an incorrect file name.
    /// </remarks>
    public bool? PrincipalMappingParentDirectoryExists { get; set; }

    /// <summary>
    /// Gets or sets the size of the principal mapping file in bytes.
    /// </summary>
    /// <remarks>
    /// This is captured when a parse error occurs, as it can help diagnose issues
    /// (e.g., empty file, truncated file, unexpectedly large file).
    /// </remarks>
    public long? PrincipalMappingFileSize { get; set; }

    /// <summary>
    /// Gets or sets the path to the principal mapping file that was provided.
    /// </summary>
    /// <value>
    /// The file path if provided, otherwise null.
    /// </value>
    public string? PrincipalMappingFilePath { get; set; }

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
    /// Gets or sets the type of error that occurred when loading a template file.
    /// </summary>
    /// <remarks>
    /// This provides detailed information about why a template failed to load.
    /// </remarks>
    public TemplateErrorType TemplateErrorType { get; set; } = TemplateErrorType.None;

    /// <summary>
    /// Gets or sets the error message from a failed template load.
    /// </summary>
    /// <value>
    /// The detailed error message, or null if no error occurred.
    /// </value>
    public string? TemplateErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the template file exists.
    /// </summary>
    /// <remarks>
    /// This is checked when template loading fails, to help diagnose whether
    /// the issue is a missing file (e.g., incorrect path, missing Docker volume mount)
    /// or a different error (e.g., parse error, permissions).
    /// </remarks>
    public bool? TemplateFileExists { get; set; }

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
                    sb.Append(string.Join(", ", typeCountStrings));
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
                sb.AppendLine();

                // Error classification
                sb.Append("**Error Type:** ");
                sb.AppendLine(PrincipalMappingErrorType.ToString());
                sb.AppendLine();

                // Detailed error message
                if (!string.IsNullOrEmpty(PrincipalMappingErrorMessage))
                {
                    sb.Append("**Error Message:** ");
                    sb.AppendLine(PrincipalMappingErrorMessage);
                    sb.AppendLine();
                }

                // Diagnostic details based on error type
                if (PrincipalMappingErrorType == PrincipalMappingErrorType.FileNotFound)
                {
                    sb.AppendLine("**File Existence Check:**");
                    sb.Append("- File exists: No");
                    sb.AppendLine();

                    if (PrincipalMappingParentDirectoryExists.HasValue)
                    {
                        sb.Append("- Parent directory exists: ");
                        sb.AppendLine(PrincipalMappingParentDirectoryExists.Value ? "Yes" : "No");
                    }
                    sb.AppendLine();

                    // Docker guidance
                    sb.AppendLine("**Troubleshooting:**");
                    if (PrincipalMappingParentDirectoryExists == false)
                    {
                        sb.AppendLine("The parent directory does not exist. This suggests:");
                        sb.AppendLine("- The file path may be incorrect");
                        sb.AppendLine("- If running in Docker: The volume mount may be missing or incorrect");
                        sb.AppendLine();
                        sb.AppendLine("To fix Docker volume mounts, use:");
                        sb.AppendLine("```bash");
                        sb.AppendLine("docker run -v /host/path/to/principals.json:/app/principals.json tfplan2md ...");
                        sb.AppendLine("```");
                    }
                    else
                    {
                        sb.AppendLine("The parent directory exists, but the file does not. Check:");
                        sb.AppendLine("- The file name is spelled correctly");
                        sb.AppendLine("- The file has been created in the expected location");
                    }
                }
                else if (PrincipalMappingErrorType == PrincipalMappingErrorType.ParseError)
                {
                    if (PrincipalMappingFileSize.HasValue)
                    {
                        sb.Append("**File Size:** ");
                        sb.Append(PrincipalMappingFileSize.Value);
                        sb.AppendLine(" bytes");
                        sb.AppendLine();

                        if (PrincipalMappingFileSize.Value == 0)
                        {
                            sb.AppendLine("**Note:** The file is empty. Ensure it contains valid JSON.");
                        }
                    }
                    sb.AppendLine();
                    sb.AppendLine("**Troubleshooting:**");
                    sb.AppendLine("The file contains invalid JSON. Common issues:");
                    sb.AppendLine("- Missing or extra commas");
                    sb.AppendLine("- Unclosed braces or brackets");
                    sb.AppendLine("- Invalid escape sequences");
                    sb.AppendLine("- Check the error message above for line/column information");
                }
                else if (PrincipalMappingErrorType == PrincipalMappingErrorType.ReadError)
                {
                    sb.AppendLine("**Troubleshooting:**");
                    sb.AppendLine("The file exists but could not be read. Check:");
                    sb.AppendLine("- File permissions (the process must have read access)");
                    sb.AppendLine("- The file is not locked by another process");
                    sb.AppendLine("- Disk I/O errors (check system logs)");
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

        // Template Errors section
        if (TemplateErrorType != TemplateErrorType.None)
        {
            hasDiagnostics = true;
            sb.AppendLine("### Template Loading Error");
            sb.AppendLine();

            sb.Append("**Error Type:** ");
            sb.AppendLine(TemplateErrorType.ToString());
            sb.AppendLine();

            if (!string.IsNullOrEmpty(TemplateErrorMessage))
            {
                sb.Append("**Error Message:** ");
                sb.AppendLine(TemplateErrorMessage);
                sb.AppendLine();
            }

            if (TemplateErrorType == TemplateErrorType.FileNotFound)
            {
                if (TemplateFileExists.HasValue)
                {
                    sb.Append("**File Exists:** ");
                    sb.AppendLine(TemplateFileExists.Value ? "Yes" : "No");
                    sb.AppendLine();
                }

                sb.AppendLine("**Troubleshooting:**");
                sb.AppendLine("The template file was not found. Check:");
                sb.AppendLine("- The template path is correct");
                sb.AppendLine("- If using a custom template, the file exists in the specified location");
                sb.AppendLine("- If running in Docker: The template file is mounted via volume");
                sb.AppendLine();
                sb.AppendLine("To mount custom templates in Docker:");
                sb.AppendLine("```bash");
                sb.AppendLine("docker run -v /host/path/to/template.sbn:/app/template.sbn tfplan2md ...");
                sb.AppendLine("```");
            }
            else if (TemplateErrorType == TemplateErrorType.ParseError)
            {
                sb.AppendLine("**Troubleshooting:**");
                sb.AppendLine("The template contains invalid Scriban syntax. Common issues:");
                sb.AppendLine("- Unclosed tags or blocks");
                sb.AppendLine("- Invalid variable references");
                sb.AppendLine("- Syntax errors in expressions");
                sb.AppendLine("- Check the Scriban documentation: https://github.com/scriban/scriban/blob/master/doc/language.md");
            }
            else if (TemplateErrorType == TemplateErrorType.ReadError)
            {
                sb.AppendLine("**Troubleshooting:**");
                sb.AppendLine("The template file exists but could not be read. Check:");
                sb.AppendLine("- File permissions");
                sb.AppendLine("- The file is not locked by another process");
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
