using System;
using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Builds change row view models for Azure DevOps variable group variables.
/// </summary>
/// <remarks>
/// Extracted from VariableGroupViewModelFactory to improve maintainability.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </remarks>
internal static class VariableGroupChangeBuilders
{
    /// <summary>
    /// Builds change rows for variables that only exist in the after state.
    /// </summary>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <returns>Ordered added variable rows.</returns>
    public static List<VariableChangeRowViewModel> BuildAdded(
        IReadOnlyList<VariableValues> afterVariables,
        IReadOnlyList<VariableValues> beforeVariables)
    {
        var beforeNames = new HashSet<string>(beforeVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        return afterVariables
            .Where(variable => !beforeNames.Contains(variable.Name))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(VariableGroupFormatters.CreateAddedRow)
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that only exist in the before state.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <returns>Ordered removed variable rows.</returns>
    public static List<VariableChangeRowViewModel> BuildRemoved(
        IReadOnlyList<VariableValues> beforeVariables,
        IReadOnlyList<VariableValues> afterVariables)
    {
        var afterNames = new HashSet<string>(afterVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        return beforeVariables
            .Where(variable => !afterNames.Contains(variable.Name))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(VariableGroupFormatters.CreateRemovedRow)
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that exist in both states but differ.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="largeValueFormat">Preferred diff format.</param>
    /// <returns>Ordered modified variable rows.</returns>
    public static List<VariableChangeRowViewModel> BuildModified(
        IReadOnlyList<VariableValues> beforeVariables,
        IReadOnlyList<VariableValues> afterVariables,
        LargeValueFormat largeValueFormat)
    {
        var beforeLookup = beforeVariables.ToDictionary(v => v.Name, StringComparer.OrdinalIgnoreCase);

        return afterVariables
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && !VariablesEqual(before!, after))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(after => VariableGroupFormatters.CreateDiffRow(beforeLookup[after.Name], after, largeValueFormat))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that remain unchanged between states.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <returns>Ordered unchanged variable rows.</returns>
    public static List<VariableChangeRowViewModel> BuildUnchanged(
        IReadOnlyList<VariableValues> beforeVariables,
        IReadOnlyList<VariableValues> afterVariables)
    {
        var beforeLookup = beforeVariables.ToDictionary(v => v.Name, StringComparer.OrdinalIgnoreCase);

        return afterVariables
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && VariablesEqual(before!, after))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(VariableGroupFormatters.CreateUnchangedRow)
            .ToList();
    }

    /// <summary>
    /// Determines if two variables are equal.
    /// </summary>
    /// <param name="before">Before variable.</param>
    /// <param name="after">After variable.</param>
    /// <returns>True if equal; otherwise false.</returns>
    private static bool VariablesEqual(VariableValues before, VariableValues after)
    {
        return before.Value == after.Value
            && before.Enabled == after.Enabled
            && before.ContentType == after.ContentType
            && before.Expires == after.Expires
            && before.IsSecret == after.IsSecret;
    }
}
