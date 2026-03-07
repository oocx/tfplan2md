using System.Linq;
using System.Text;

namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Formats diagnostic snapshots as markdown debug sections.
/// Related feature: docs/features/110-refactoring-opportunities/tasks.md.
/// </summary>
internal static partial class DiagnosticMarkdownFormatter
{
    private const string FoundPrefix = "- Found ";

    /// <summary>
    /// Formats a diagnostic snapshot as markdown.
    /// </summary>
    /// <param name="report">The snapshot to format.</param>
    /// <returns>The markdown debug section.</returns>
    public static string Format(DiagnosticReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<details>");
        sb.AppendLine("<summary>🐛\u00A0Debug Information</summary>");
        sb.AppendLine("<br>");
        sb.AppendLine();

        var hasDiagnostics = false;

        if (report.PrincipalMappingFileProvided)
        {
            hasDiagnostics = true;
            AppendPrincipalMappingSection(sb, report);
            sb.AppendLine();
        }

        if (report.TemplateResolutions.Count > 0)
        {
            hasDiagnostics = true;
            AppendTemplateResolutionSection(sb, report.TemplateResolutions);
            sb.AppendLine();
        }

        if (!hasDiagnostics)
        {
            sb.AppendLine("No diagnostics collected.");
            sb.AppendLine();
        }

        sb.AppendLine("</details>");
        return sb.ToString();
    }

    private static void AppendPrincipalMappingSection(StringBuilder sb, DiagnosticReport report)
    {
        sb.AppendLine("### Principal Mapping");
        sb.AppendLine();

        if (report.PrincipalMappingLoadedSuccessfully)
        {
            AppendPrincipalMappingSuccess(sb, report);
            return;
        }

        AppendPrincipalMappingFailure(sb, report);
    }

    private static void AppendPrincipalMappingSuccess(StringBuilder sb, DiagnosticReport report)
    {
        sb.Append("Principal Mapping: Loaded successfully from '");
        sb.Append(report.PrincipalMappingFilePath);
        sb.AppendLine("'");

        AppendPrincipalTypeCounts(sb, report.PrincipalTypeCount);
        AppendEntityCounts(sb, report);
        AppendFailedResolutions(sb, report.FailedResolutions);
    }

    private static void AppendPrincipalTypeCounts(StringBuilder sb, IReadOnlyDictionary<string, int> principalTypeCount)
    {
        if (principalTypeCount.Count == 0)
        {
            return;
        }

        sb.Append(FoundPrefix);
        var typeCountStrings = principalTypeCount
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Value} {kvp.Key}");
        sb.AppendJoin(", ", typeCountStrings);
        sb.AppendLine();
    }

    private static void AppendEntityCounts(StringBuilder sb, DiagnosticReport report)
    {
        AppendCount(sb, report.SubscriptionCount, "subscription");
        AppendCount(sb, report.ManagementGroupCount, "management group");
        AppendCount(sb, report.TenantCount, "tenant");
        AppendCount(sb, report.RoleCount, "custom role");

        if (report.AzdoUserCount > 0 || report.AzdoGroupCount > 0 || report.AzdoProjectCount > 0 || report.AzdoRepositoryCount > 0)
        {
            sb.Append(FoundPrefix);
            sb.Append(report.AzdoUserCount);
            sb.Append(" azdo user");
            if (report.AzdoUserCount != 1)
            {
                sb.Append('s');
            }

            sb.Append(", ");
            sb.Append(report.AzdoGroupCount);
            sb.Append(" azdo group");
            if (report.AzdoGroupCount != 1)
            {
                sb.Append('s');
            }

            sb.Append(", ");
            sb.Append(report.AzdoProjectCount);
            sb.Append(" azdo project");
            if (report.AzdoProjectCount != 1)
            {
                sb.Append('s');
            }

            sb.Append(", ");
            sb.Append(report.AzdoRepositoryCount);
            sb.Append(" azdo repositor");
            if (report.AzdoRepositoryCount != 1)
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

    private static void AppendFailedResolutions(StringBuilder sb, IReadOnlyList<FailedResolution> failedResolutions)
    {
        if (failedResolutions.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.Append("Failed to resolve ");
        sb.Append(failedResolutions.Count);
        sb.Append(" mapping");
        if (failedResolutions.Count != 1)
        {
            sb.Append('s');
        }

        sb.AppendLine(":");

        foreach (var failure in failedResolutions)
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

    private static void AppendPrincipalMappingFailure(StringBuilder sb, DiagnosticReport report)
    {
        sb.Append("Principal Mapping: Failed to load from '");
        sb.Append(report.PrincipalMappingFilePath);
        sb.AppendLine("'");
        AppendPrincipalMappingFailureDiagnostics(sb, report);
    }
}
