using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Helpers for collecting attribute names across before/after values.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Collects attribute names that have non-null values across before and after objects.
    /// </summary>
    /// <param name="before">Original attribute set.</param>
    /// <param name="after">Updated attribute set.</param>
    /// <returns>Scriban array of attribute names present in either set.</returns>
    private static ScriptArray CollectAttributes(object? before, object? after)
    {
        var beforeDict = ToDictionary(before);
        var afterDict = ToDictionary(after);
        var keys = beforeDict.Keys.Union(afterDict.Keys).ToList();

        var attributes = new ScriptArray();
        foreach (var key in keys)
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            if (IsNullValue(beforeValue) && IsNullValue(afterValue))
            {
                continue;
            }

            attributes.Add(key);
        }

        return attributes;
    }
}
