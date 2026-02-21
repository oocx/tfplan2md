using System.Text;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
/// <remarks>
/// These helpers transform JSON body content from azapi_resource resources into human-readable
/// markdown tables using dot-notation property paths. This makes Azure REST API resource
/// configurations easy to review in pull requests.
/// </remarks>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Splits property objects into small and large collections using the is_large flag.
    /// </summary>
    /// <param name="items">The items to partition.</param>
    /// <returns>Tuple containing small and large collections.</returns>
    private static (ScriptArray Small, ScriptArray Large) SplitBySize(ScriptArray items)
    {
        var smallItems = new ScriptArray();
        var largeItems = new ScriptArray();

        foreach (var item in items)
        {
            if (item is ScriptObject scriptObj && scriptObj["is_large"] is bool isLarge)
            {
                if (isLarge)
                {
                    largeItems.Add(scriptObj);
                }
                else
                {
                    smallItems.Add(scriptObj);
                }
            }
        }

        return (smallItems, largeItems);
    }

    /// <summary>
    /// Renders a message when no properties were produced for a section.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="smallCount">The number of small properties.</param>
    /// <param name="largeCount">The number of large properties.</param>
    /// <param name="message">The message to render when empty.</param>
    private static void RenderNoChangesMessage(StringBuilder sb, int smallCount, int largeCount, string message)
    {
        if (smallCount > 0 || largeCount > 0)
        {
            return;
        }

        sb.AppendLine(message);
        sb.AppendLine();
    }

    /// <summary>
    /// Determines whether a flattened property path is sensitive.
    /// Checks both the exact path and, for leaf properties in a sensitive parent object,
    /// whether any ancestor path is marked as globally sensitive (e.g., <c>body = true</c>).
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </summary>
    /// <param name="path">The flattened property path (e.g., <c>properties.administratorLoginPassword</c>).</param>
    /// <param name="sensitivePaths">Set of paths marked as sensitive by <see cref="FlattenSensitivity"/>.</param>
    /// <returns><c>true</c> when the property value should be masked.</returns>
    private static bool IsPathSensitive(string path, HashSet<string> sensitivePaths)
    {
        if (sensitivePaths.Count == 0)
        {
            return false;
        }

        // Direct match: the exact path is sensitive
        if (sensitivePaths.Contains(path))
        {
            return true;
        }

        // Global sensitivity: when the entire body (or a parent object) is marked as true,
        // FlattenSensitivity produces an empty-string entry ("") — all children are sensitive.
        if (sensitivePaths.Contains(string.Empty))
        {
            return true;
        }

        // Check parent paths: properties.a.b should be sensitive if properties.a is sensitive
        var dotIndex = path.LastIndexOf('.');
        while (dotIndex > 0)
        {
            var parent = path[..dotIndex];
            if (sensitivePaths.Contains(parent))
            {
                return true;
            }

            dotIndex = parent.LastIndexOf('.');
        }

        return false;
    }

    /// <summary>
    /// Reconstructs the full flattened path for an array entry so it can be checked against sensitivity.
    /// </summary>
    /// <param name="arrayPath">The display-level array path (e.g., <c>rules</c>).</param>
    /// <param name="index">The array index.</param>
    /// <param name="localPath">The entry's local path within the array item.</param>
    /// <returns>The reconstructed full path including <c>properties.</c> prefix.</returns>
    private static string ReconstructArrayEntryPath(string arrayPath, int index, string localPath)
    {
        var basePath = $"properties.{arrayPath}[{index}]";
        return string.IsNullOrEmpty(localPath) ? basePath : $"{basePath}.{localPath}";
    }
}
