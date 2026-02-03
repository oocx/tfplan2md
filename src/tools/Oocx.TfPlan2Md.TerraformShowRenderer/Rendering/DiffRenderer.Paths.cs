using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>Path and comparison helpers for diff rendering. Related feature: docs/features/030-terraform-show-approximation/specification.md.</summary>
internal sealed partial class DiffRenderer
{
    /// <summary>Normalizes Terraform replacement paths to a lookup set.</summary>
    /// <param name="replacePaths">Paths from <c>change.replace_paths</c> in the plan.</param>
    /// <returns>Distinct normalized path strings used to flag replacements.</returns>
    private static HashSet<string> CollectReplacePaths(IReadOnlyList<IReadOnlyList<object>>? replacePaths)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (replacePaths is null)
        {
            return set;
        }

        foreach (var path in replacePaths)
        {
            var formatted = FormatPath(path.Select(segment => Convert.ToString(segment, CultureInfo.InvariantCulture) ?? string.Empty));
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                set.Add(formatted);
            }
        }

        return set;
    }

    /// <summary>Combines path segments using Terraform's forward-slash notation.</summary>
    /// <param name="segments">Ordered path segments for a change.</param>
    /// <returns>Slash-separated path string.</returns>
    private static string FormatPath(IEnumerable<string> segments) => string.Join('/', segments);

    /// <summary>Checks whether a path is marked unknown in <c>after_unknown</c>.</summary>
    /// <param name="root">Root element of the unknown tree.</param>
    /// <param name="path">Path to evaluate.</param>
    /// <returns><c>true</c> when the path is unknown; otherwise <c>false</c>.</returns>
    private static bool IsUnknownPath(JsonElement? root, IReadOnlyList<string> path)
    {
        if (root is null)
        {
            return false;
        }

        var current = root.Value;
        foreach (var segment in path)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                var match = current.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, segment, StringComparison.Ordinal));
                if (match.Value.ValueKind == JsonValueKind.Undefined)
                {
                    return false;
                }

                current = match.Value;
            }
            else if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index < current.GetArrayLength())
            {
                current = current.EnumerateArray().ElementAt(index);
            }
            else
            {
                return false;
            }
        }

        return current.ValueKind == JsonValueKind.True;
    }

    /// <summary>Checks whether a path is marked sensitive in <c>after_sensitive</c>.</summary>
    /// <param name="root">Root element of the sensitive tree.</param>
    /// <param name="path">Path to evaluate.</param>
    /// <returns><c>true</c> when the path is sensitive; otherwise <c>false</c>.</returns>
    // SonarAnalyzer S4144: Duplicate method bodies
    // Justification: IsSensitivePath and IsUnknownPath are semantically distinct domain concepts
    // representing separate Terraform plan JSON trees (after_sensitive vs after_unknown).
    // While implementation is currently identical, these concepts should remain separate methods
    // to maintain domain clarity and allow independent evolution if future Terraform versions
    // require different handling for sensitive vs unknown path navigation.
#pragma warning disable S4144
    private static bool IsSensitivePath(JsonElement? root, IReadOnlyList<string> path)
    {
        if (root is null)
        {
            return false;
        }

        var current = root.Value;
        foreach (var segment in path)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                var match = current.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, segment, StringComparison.Ordinal));
                if (match.Value.ValueKind == JsonValueKind.Undefined)
                {
                    return false;
                }

                current = match.Value;
            }
            else if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index < current.GetArrayLength())
            {
                current = current.EnumerateArray().ElementAt(index);
            }
            else
            {
                return false;
            }
        }

        return current.ValueKind == JsonValueKind.True;
    }
#pragma warning restore S4144

    /// <summary>Checks JSON equality while preserving formatting fidelity.</summary>
    /// <param name="left">Left-hand JSON element.</param>
    /// <param name="right">Right-hand JSON element.</param>
    /// <returns><c>true</c> when both values match exactly.</returns>
    private static bool AreEqual(JsonElement left, JsonElement right) => left.ValueKind == right.ValueKind && left.GetRawText() == right.GetRawText();

    /// <summary>Renders inline JSON values for arrow updates.</summary>
    /// <param name="value">Value to format inline.</param>
    /// <returns>Rendered inline value.</returns>
    private string InlineValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null => _valueRenderer.Render(value),
        _ => value.GetRawText()
    };

    /// <summary>Converts arbitrary values to <see cref="JsonElement"/> while preserving order.</summary>
    /// <param name="value">Value from the parsed plan.</param>
    /// <returns>Serialized <see cref="JsonElement"/> or <c>null</c> when no value exists.</returns>
    private static JsonElement? ToElement(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element;
        }

        return JsonSerializer.SerializeToElement(value);
    }
}
