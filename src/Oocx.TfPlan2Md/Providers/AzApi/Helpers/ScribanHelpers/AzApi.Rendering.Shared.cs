using System.Text;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
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
}
