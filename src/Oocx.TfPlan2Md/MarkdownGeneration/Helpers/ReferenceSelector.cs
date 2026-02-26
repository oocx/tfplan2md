using System;
using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Selects the most useful Terraform configuration reference label for display.
/// </summary>
/// <remarks>
/// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
/// </remarks>
internal static class ReferenceSelector
{
    /// <summary>
    /// Selects the best display reference from a list of Terraform references.
    /// </summary>
    /// <param name="references">Reference list from <c>configuration.expressions.&lt;attr&gt;.references</c>.</param>
    /// <returns>
    /// A preferred label in priority order: static resource reference, <c>each.value.&lt;attr&gt;</c>,
    /// <c>var.&lt;name&gt;</c>/<c>local.&lt;name&gt;</c>; otherwise <see langword="null"/>.
    /// </returns>
    internal static string? SelectBestReference(IReadOnlyList<string> references)
    {
        if (references.Count == 0)
        {
            return null;
        }

        var staticResourceReference = SelectResourceLevelReference(references);
        if (!string.IsNullOrWhiteSpace(staticResourceReference))
        {
            return staticResourceReference;
        }

        foreach (var reference in references)
        {
            if (TryGetEachValueAttributeReference(reference, out var eachValueReference))
            {
                return eachValueReference;
            }
        }

        foreach (var reference in references)
        {
            if (TryGetVariableOrLocalReference(reference, out var variableReference))
            {
                return variableReference;
            }
        }

        return null;
    }

    /// <summary>
    /// Selects the best static resource-level reference from a list.
    /// </summary>
    /// <param name="references">Reference list from configuration expressions.</param>
    /// <returns>The resource-level reference (e.g., <c>azuread_group.admins</c>) or <see langword="null"/>.</returns>
    internal static string? SelectResourceLevelReference(IReadOnlyList<string> references)
    {
        foreach (var reference in references)
        {
            if (TryGetStaticResourceReference(reference, out var resourceReference))
            {
                return resourceReference;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to parse a static resource reference and normalize it to resource-level address.
    /// </summary>
    /// <param name="reference">Raw reference string.</param>
    /// <param name="resourceReference">Normalized resource-level reference.</param>
    /// <returns><see langword="true"/> when the reference targets a static resource; otherwise <see langword="false"/>.</returns>
    private static bool TryGetStaticResourceReference(string? reference, out string resourceReference)
    {
        resourceReference = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var parts = reference.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        if (IsNonResourcePrefix(parts[0]))
        {
            return false;
        }

        var modulePrefixLength = CountModulePrefixSegments(parts);
        if (modulePrefixLength < 0)
        {
            return false;
        }

        var remaining = parts.Length - modulePrefixLength;
        if (remaining is not (2 or 3))
        {
            return false;
        }

        resourceReference = string.Join('.', parts.Take(modulePrefixLength + 2));
        return !string.IsNullOrWhiteSpace(resourceReference);
    }

    /// <summary>
    /// Tries to parse an <c>each.value.&lt;attr&gt;</c> reference.
    /// </summary>
    /// <param name="reference">Raw reference string.</param>
    /// <param name="eachValueReference">Normalized each.value attribute reference.</param>
    /// <returns><see langword="true"/> when the reference matches <c>each.value.&lt;attr&gt;</c>; otherwise <see langword="false"/>.</returns>
    private static bool TryGetEachValueAttributeReference(string? reference, out string eachValueReference)
    {
        eachValueReference = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var parts = reference.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!parts[0].Equals("each", StringComparison.OrdinalIgnoreCase)
            || !parts[1].Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        eachValueReference = reference;
        return true;
    }

    /// <summary>
    /// Tries to parse a <c>var.&lt;name&gt;</c> or <c>local.&lt;name&gt;</c> reference.
    /// </summary>
    /// <param name="reference">Raw reference string.</param>
    /// <param name="variableReference">Normalized variable/local reference.</param>
    /// <returns><see langword="true"/> when matched; otherwise <see langword="false"/>.</returns>
    private static bool TryGetVariableOrLocalReference(string? reference, out string variableReference)
    {
        variableReference = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var parts = reference.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        var isVariable = parts[0].Equals("var", StringComparison.OrdinalIgnoreCase);
        var isLocal = parts[0].Equals("local", StringComparison.OrdinalIgnoreCase);
        if (!isVariable && !isLocal)
        {
            return false;
        }

        variableReference = reference;
        return true;
    }

    /// <summary>
    /// Counts leading module prefix segments in a reference.
    /// </summary>
    /// <param name="parts">Reference parts split by dot.</param>
    /// <returns>Module prefix segment count, or -1 when malformed.</returns>
    private static int CountModulePrefixSegments(string[] parts)
    {
        var index = 0;
        while (index < parts.Length && parts[index].Equals("module", StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= parts.Length)
            {
                return -1;
            }

            index += 2;
        }

        return index;
    }

    /// <summary>
    /// Determines whether a reference prefix is not a static managed resource prefix.
    /// </summary>
    /// <param name="prefix">First segment of a reference.</param>
    /// <returns><see langword="true"/> when the prefix is non-resource; otherwise <see langword="false"/>.</returns>
    private static bool IsNonResourcePrefix(string prefix)
    {
        return prefix.Equals("data", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("var", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("local", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("path", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("terraform", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("each", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("count", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("self", StringComparison.OrdinalIgnoreCase);
    }
}
