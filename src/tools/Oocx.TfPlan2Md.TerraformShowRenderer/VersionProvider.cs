using System.Reflection;

namespace Oocx.TfPlan2Md.TerraformShowRenderer;

/// <summary>
/// Supplies version information for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
internal static class VersionProvider
{
    /// <summary>
    /// Reads the informational version from assembly metadata.
    /// </summary>
    /// <returns>A semantic version string.</returns>
    public static string GetVersion()
    {
        var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute?.InformationalVersion ?? "0.0.0";
    }
}
