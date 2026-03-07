using System.Linq;
using System.Text;

namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Helper routines for formatting diagnostic markdown sections.
/// Related feature: docs/features/110-refactoring-opportunities/tasks.md.
/// </summary>
internal static partial class DiagnosticMarkdownFormatter
{
    private static void AppendPrincipalMappingFailureDiagnostics(StringBuilder sb, DiagnosticReport report)
    {
        if (!report.PrincipalMappingFileExists.HasValue && !report.PrincipalMappingErrorType.HasValue)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("**Diagnostic Details:**");
        AppendPrincipalMappingFileStatus(sb, report);
        AppendPrincipalMappingErrorDetails(sb, report);
        AppendPrincipalMappingCommonSolutions(sb, report.PrincipalMappingErrorType);
    }

    private static void AppendPrincipalMappingFileStatus(StringBuilder sb, DiagnosticReport report)
    {
        if (report.PrincipalMappingFileExists.HasValue)
        {
            sb.Append("- File exists: ");
            sb.AppendLine(report.PrincipalMappingFileExists.Value ? "✅" : "❌");
        }

        if (report.PrincipalMappingDirectoryExists.HasValue)
        {
            sb.Append("- Directory exists: ");
            sb.AppendLine(report.PrincipalMappingDirectoryExists.Value ? "✅" : "❌");
        }
    }

    private static void AppendPrincipalMappingErrorDetails(StringBuilder sb, DiagnosticReport report)
    {
        if (report.PrincipalMappingErrorType.HasValue)
        {
            sb.Append("- Error type: ");
            sb.AppendLine(report.PrincipalMappingErrorType.Value.ToString());
        }

        if (!string.IsNullOrEmpty(report.PrincipalMappingErrorMessage))
        {
            sb.Append("- Error message: ");
            sb.AppendLine(report.PrincipalMappingErrorMessage);
        }

        if (!string.IsNullOrEmpty(report.PrincipalMappingErrorDetails))
        {
            sb.Append("- Details: ");
            sb.AppendLine(report.PrincipalMappingErrorDetails);
        }
    }

    private static void AppendPrincipalMappingCommonSolutions(StringBuilder sb, PrincipalLoadError? errorType)
    {
        if (!errorType.HasValue)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("**Common Solutions:**");

        switch (errorType.Value)
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

    private static void AppendPrincipalMappingAccessDeniedSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Check file permissions: `ls -l <file>`");
        sb.AppendLine("2. Ensure the file is readable: `chmod +r <file>`");
        sb.AppendLine("3. If using Docker, check container user permissions");
    }

    private static void AppendPrincipalMappingEmptyFileSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Verify the file contains principal mappings");
        sb.AppendLine("2. Use Azure CLI to generate principal mappings:");
        sb.AppendLine("   ```bash");
        sb.AppendLine("   az ad user list --query \"[].{id:id, name:displayName}\" -o json");
        sb.AppendLine("   ```");
    }

    private static void AppendPrincipalMappingUnknownErrorSolutions(StringBuilder sb)
    {
        sb.AppendLine("1. Check the error details above");
        sb.AppendLine("2. Verify file accessibility and format");
        sb.AppendLine("3. Check system logs for additional information");
    }

    private static void AppendTemplateResolutionSection(StringBuilder sb, IReadOnlyList<TemplateResolution> templateResolutions)
    {
        sb.AppendLine("### Template Resolution");
        sb.AppendLine();

        var uniqueResolutions = templateResolutions
            .GroupBy(tr => tr.ResourceType)
            .Select(group => group.First())
            .OrderBy(tr => tr.ResourceType, StringComparer.Ordinal);

        foreach (var resolution in uniqueResolutions)
        {
            sb.Append("- `");
            sb.Append(resolution.ResourceType);
            sb.Append("`: ");
            sb.AppendLine(resolution.TemplateSource);
        }
    }

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
