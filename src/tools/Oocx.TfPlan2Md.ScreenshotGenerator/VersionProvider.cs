using System.Reflection;

namespace Oocx.TfPlan2Md.ScreenshotGenerator;

/// <summary>
/// Supplies version information for the screenshot generator tool.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
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
