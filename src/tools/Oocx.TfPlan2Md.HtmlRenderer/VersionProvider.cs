using System.Reflection;

namespace Oocx.TfPlan2Md.HtmlRenderer;

/// <summary>
/// Supplies version information for the HTML renderer tool.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal static class VersionProvider
{
    /// <summary>
    /// Gets the informational version from assembly metadata.
    /// </summary>
    /// <returns>A human-readable version string.</returns>
    public static string GetVersion()
    {
        var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute?.InformationalVersion ?? "0.0.0";
    }
}
