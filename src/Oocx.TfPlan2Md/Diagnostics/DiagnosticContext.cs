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
    /// Prefix used when listing resolved mapping counts.
    /// </summary>
    private const string FoundPrefix = "- Found ";
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
    /// Gets or sets the number of subscription mappings loaded from the mapping file.
    /// </summary>
    public int SubscriptionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of management group mappings loaded from the mapping file.
    /// </summary>
    public int ManagementGroupCount { get; set; }

    /// <summary>
    /// Gets or sets the number of tenant mappings loaded from the mapping file.
    /// </summary>
    public int TenantCount { get; set; }

    /// <summary>
    /// Gets or sets the number of custom role mappings loaded from the mapping file.
    /// </summary>
    public int RoleCount { get; set; }

    /// <summary>
    /// Gets or sets the number of Azure DevOps user mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    public int AzdoUserCount { get; set; }

    /// <summary>
    /// Gets or sets the number of Azure DevOps group mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    public int AzdoGroupCount { get; set; }

    /// <summary>
    /// Gets or sets the number of Azure DevOps project mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    public int AzdoProjectCount { get; set; }

    /// <summary>
    /// Gets or sets the number of Azure DevOps repository mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/095-azdo-repo-mapping-and-icons/specification.md.
    /// </remarks>
    public int AzdoRepositoryCount { get; set; }

    /// <summary>
    /// Gets the list of IDs that failed to resolve, along with the resource that referenced them.
    /// </summary>
    /// <remarks>
    /// Each entry captures a failed lookup with context about where it was referenced.
    /// The same ID may appear multiple times if referenced by different resources.
    /// </remarks>
    public List<FailedResolution> FailedResolutions { get; } = new();

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
    /// A markdown-formatted string with debug information wrapped in a collapsible details block,
    /// or a message indicating no diagnostics were collected.
    /// </returns>
    /// <remarks>
    /// The generated markdown follows this structure:
    /// <list type="bullet">
    /// <item><description>&lt;details&gt; tag (collapsed by default)</description></item>
    /// <item><description>&lt;summary&gt;🐛 Debug Information&lt;/summary&gt; (with non-breaking space U+00A0)</description></item>
    /// <item><description>&lt;br&gt; spacing tag</description></item>
    /// <item><description>### Principal Mapping subsection (if applicable)</description></item>
    /// <item><description>### Template Resolution subsection (if applicable)</description></item>
    /// <item><description>&lt;/details&gt; closing tag</description></item>
    /// </list>
    /// All resource addresses and principal IDs are formatted in code blocks for readability.
    /// If no diagnostics were collected, the message appears inside the details block.
    /// Related feature: docs/features/086-output-display-enhancements/specification.md.
    /// </remarks>
    public string GenerateMarkdownSection()
    {
        var sb = new StringBuilder();

        // Start collapsible details block (collapsed by default)
        sb.AppendLine("<details>");
        sb.AppendLine("<summary>🐛\u00A0Debug Information</summary>");
        sb.AppendLine("<br>");
        sb.AppendLine();

        var hasDiagnostics = false;

        // Principal Mapping section
        if (PrincipalMappingFileProvided)
        {
            hasDiagnostics = true;
            AppendPrincipalMappingSection(sb);
            sb.AppendLine();
        }

        // Template Resolution section
        if (TemplateResolutions.Count > 0)
        {
            hasDiagnostics = true;
            AppendTemplateResolutionSection(sb);
            sb.AppendLine();
        }

        if (!hasDiagnostics)
        {
            sb.AppendLine("No diagnostics collected.");
            sb.AppendLine();
        }

        // Close collapsible details block
        sb.AppendLine("</details>");

        return sb.ToString();
    }

    /// <summary>
    /// Appends principal mapping diagnostics to the output.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalMappingSection(StringBuilder sb)
    {
        sb.AppendLine("### Principal Mapping");
        sb.AppendLine();

        if (PrincipalMappingLoadedSuccessfully)
        {
            AppendPrincipalMappingSuccess(sb);
        }
        else
        {
            AppendPrincipalMappingFailure(sb);
        }
    }

    /// <summary>
    /// Appends successful principal mapping diagnostics.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalMappingSuccess(StringBuilder sb)
    {
        sb.Append("Principal Mapping: Loaded successfully from '");
        sb.Append(PrincipalMappingFilePath);
        sb.AppendLine("'");

        AppendPrincipalTypeCounts(sb);
        AppendEntityCounts(sb);
        AppendFailedResolutions(sb);
    }

    /// <summary>
    /// Appends principal type counts to the output.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalTypeCounts(StringBuilder sb)
    {
        if (PrincipalTypeCount.Count == 0)
        {
            return;
        }

        sb.Append(FoundPrefix);
        var typeCountStrings = PrincipalTypeCount
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Value} {kvp.Key}");
        sb.AppendJoin(", ", typeCountStrings);
        sb.AppendLine();
    }

    /// <summary>
    /// Appends entity mapping counts to the output.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendEntityCounts(StringBuilder sb)
    {
        AppendCount(sb, SubscriptionCount, "subscription");
        AppendCount(sb, ManagementGroupCount, "management group");
        AppendCount(sb, TenantCount, "tenant");
        AppendCount(sb, RoleCount, "custom role");

        // Azure DevOps entity counts
        if (AzdoUserCount > 0 || AzdoGroupCount > 0 || AzdoProjectCount > 0 || AzdoRepositoryCount > 0)
        {
            sb.Append(FoundPrefix);
            sb.Append(AzdoUserCount);
            sb.Append(" azdo user");
            if (AzdoUserCount != 1)
            {
                sb.Append('s');
            }
            sb.Append(", ");
            sb.Append(AzdoGroupCount);
            sb.Append(" azdo group");
            if (AzdoGroupCount != 1)
            {
                sb.Append('s');
            }
            sb.Append(", ");
            sb.Append(AzdoProjectCount);
            sb.Append(" azdo project");
            if (AzdoProjectCount != 1)
            {
                sb.Append('s');
            }
            sb.Append(", ");
            sb.Append(AzdoRepositoryCount);
            sb.Append(" azdo repositor");
            if (AzdoRepositoryCount != 1)
            {
                sb.Append("ies");
            }
            else
            {
                sb.Append('y');
            }
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Appends a count entry when present.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    /// <param name="count">The count to report.</param>
    /// <param name="label">The singular label for the count.</param>
    private static void AppendCount(StringBuilder sb, int count, string label)
    {
        if (count <= 0)
        {
            return;
        }

        sb.Append(FoundPrefix);
        sb.Append(count);
        sb.Append(' ');
        sb.Append(label);
        if (count != 1)
        {
            sb.Append('s');
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Appends failed resolution details to the output.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendFailedResolutions(StringBuilder sb)
    {
        if (FailedResolutions.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.Append("Failed to resolve ");
        sb.Append(FailedResolutions.Count);
        sb.Append(" mapping");
        if (FailedResolutions.Count != 1)
        {
            sb.Append('s');
        }
        sb.AppendLine(":");

        foreach (var failure in FailedResolutions)
        {
            sb.Append("- ");
            sb.Append(FormatResolutionType(failure.Type));
            sb.Append(" `");
            sb.Append(failure.Id);
            sb.Append("` (referenced in `");
            sb.Append(failure.ResourceAddress);
            sb.Append("`)");

            if (!string.IsNullOrWhiteSpace(failure.Reason))
            {
                sb.Append(" - ");
                sb.Append(failure.Reason);
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// Appends failed principal mapping diagnostics.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalMappingFailure(StringBuilder sb)
    {
        sb.Append("Principal Mapping: Failed to load from '");
        sb.Append(PrincipalMappingFilePath);
        sb.AppendLine("'");

        AppendPrincipalMappingFailureDiagnostics(sb);
    }

    /// <summary>
    /// Appends enhanced diagnostics for principal mapping failures.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalMappingFailureDiagnostics(StringBuilder sb)
    {
        if (!PrincipalMappingFileExists.HasValue && !PrincipalMappingErrorType.HasValue)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("**Diagnostic Details:**");

        AppendPrincipalMappingFileStatus(sb);
        AppendPrincipalMappingErrorDetails(sb);
        AppendPrincipalMappingCommonSolutions(sb);
    }

    /// <summary>
    /// Appends principal mapping file and directory status details.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalMappingFileStatus(StringBuilder sb)
    {
        if (PrincipalMappingFileExists.HasValue)
        {
            sb.Append("- File exists: ");
            sb.AppendLine(PrincipalMappingFileExists.Value ? "✅" : "❌");
        }

        if (PrincipalMappingDirectoryExists.HasValue)
        {
            sb.Append("- Directory exists: ");
            sb.AppendLine(PrincipalMappingDirectoryExists.Value ? "✅" : "❌");
        }
    }

    /// <summary>
    /// Appends principal mapping error details.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalMappingErrorDetails(StringBuilder sb)
    {
        if (PrincipalMappingErrorType.HasValue)
        {
            sb.Append("- Error type: ");
            sb.AppendLine(PrincipalMappingErrorType.Value.ToString());
        }

        if (!string.IsNullOrEmpty(PrincipalMappingErrorMessage))
        {
            sb.Append("- Error message: ");
            sb.AppendLine(PrincipalMappingErrorMessage);
        }

        if (!string.IsNullOrEmpty(PrincipalMappingErrorDetails))
        {
            sb.Append("- Details: ");
            sb.AppendLine(PrincipalMappingErrorDetails);
        }
    }

    /// <summary>
    /// Appends common solution guidance for principal mapping failures.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendPrincipalMappingCommonSolutions(StringBuilder sb)
    {
        if (!PrincipalMappingErrorType.HasValue)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("**Common Solutions:**");

        switch (PrincipalMappingErrorType.Value)
        {
            case PrincipalLoadError.FileNotFound:
                AppendPrincipalMappingFileNotFoundSolutions(sb);
                break;
            case PrincipalLoadError.DirectoryNotFound:
                AppendPrincipalMappingDirectoryNotFoundSolutions(sb);
                break;
            case PrincipalLoadError.JsonParseError:
                AppendPrincipalMappingJsonParseSolutions(sb);
                break;
            case PrincipalLoadError.AccessDenied:
                AppendPrincipalMappingAccessDeniedSolutions(sb);
                break;
            case PrincipalLoadError.EmptyFile:
                AppendPrincipalMappingEmptyFileSolutions(sb);
                break;
            case PrincipalLoadError.UnknownError:
                AppendPrincipalMappingUnknownErrorSolutions(sb);
                break;
        }
    }

    /// <summary>
    /// Appends common solutions for missing principal mapping files.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private static void AppendPrincipalMappingFileNotFoundSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Verify the file path is correct");
        sb.AppendLine("2. If using Docker, ensure the file is mounted:");
        sb.AppendLine("   ```bash");
        sb.AppendLine("   docker run -v $(pwd):/data oocx/tfplan2md \\");
        sb.AppendLine("     --principal-mapping /data/principals.json \\");
        sb.AppendLine("     /data/plan.json");
        sb.AppendLine("   ```");
        sb.AppendLine("3. Check the file exists on your host system");
    }

    /// <summary>
    /// Appends common solutions for missing principal mapping directories.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private static void AppendPrincipalMappingDirectoryNotFoundSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Verify the directory path exists");
        sb.AppendLine("2. If using Docker, the directory must be mounted:");
        sb.AppendLine("   ```bash");
        sb.AppendLine("   docker run -v /host/path:/data oocx/tfplan2md \\");
        sb.AppendLine("     --principal-mapping /data/principals.json \\");
        sb.AppendLine("     /data/plan.json");
        sb.AppendLine("   ```");
        sb.AppendLine("3. Check directory permissions and accessibility");
    }

    /// <summary>
    /// Appends common solutions for JSON parse errors.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private static void AppendPrincipalMappingJsonParseSolutions(StringBuilder sb)
    {
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
    }

    /// <summary>
    /// Appends common solutions for access denied errors.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private static void AppendPrincipalMappingAccessDeniedSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Check file permissions: `ls -l <file>`");
        sb.AppendLine("2. Ensure the file is readable: `chmod +r <file>`");
        sb.AppendLine("3. If using Docker, check container user permissions");
    }

    /// <summary>
    /// Appends common solutions for empty mapping files.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private static void AppendPrincipalMappingEmptyFileSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Verify the file contains principal mappings");
        sb.AppendLine("2. Use Azure CLI to generate principal mappings:");
        sb.AppendLine("   ```bash");
        sb.AppendLine("   az ad user list --query \"[].{id:id, name:displayName}\" -o json");
        sb.AppendLine("   ```");
    }

    /// <summary>
    /// Appends common solutions for unknown principal mapping errors.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private static void AppendPrincipalMappingUnknownErrorSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Check the error details above");
        sb.AppendLine("2. Verify file accessibility and format");
        sb.AppendLine("3. Check system logs for additional information");
    }

    /// <summary>
    /// Appends template resolution diagnostics.
    /// </summary>
    /// <param name="sb">The output builder.</param>
    private void AppendTemplateResolutionSection(StringBuilder sb)
    {
        sb.AppendLine("### Template Resolution");
        sb.AppendLine();

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
    }

    /// <summary>
    /// Formats a failed resolution type for diagnostic output.
    /// </summary>
    /// <param name="type">The resolution type.</param>
    /// <returns>Human-readable label for the type.</returns>
    private static string FormatResolutionType(FailedResolutionType type)
    {
        return type switch
        {
            FailedResolutionType.Principal => "Principal",
            FailedResolutionType.Subscription => "Subscription",
            FailedResolutionType.ManagementGroup => "Management group",
            FailedResolutionType.Tenant => "Tenant",
            FailedResolutionType.RoleDefinition => "Role definition",
            FailedResolutionType.AzdoUser => "Azure DevOps user",
            FailedResolutionType.AzdoGroup => "Azure DevOps group",
            FailedResolutionType.AzdoProject => "Azure DevOps project",
            FailedResolutionType.AzdoRepository => "Azure DevOps repository",
            _ => "Unknown"
        };
    }
}
