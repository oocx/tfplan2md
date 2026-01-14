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
