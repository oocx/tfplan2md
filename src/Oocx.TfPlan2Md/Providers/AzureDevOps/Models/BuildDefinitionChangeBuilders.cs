using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Builds change row view models for Azure DevOps build definition variables.
/// </summary>
/// <remarks>
/// Follows the pattern from VariableGroupChangeBuilders to improve maintainability.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </remarks>
internal static class BuildDefinitionChangeBuilders
{
    /// <summary>
    /// Builds change rows for variables that only exist in the after state.
    /// </summary>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Ordered added variable rows.</returns>
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildAdded(
        IReadOnlyList<BuildDefinitionVariableValues> afterVariables,
        IReadOnlyList<BuildDefinitionVariableValues> beforeVariables,
        string? providerName)
    {
        var beforeNames = new HashSet<string>(beforeVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        return afterVariables
            .Where(variable => !beforeNames.Contains(variable.Name))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(variable => BuildDefinitionFormatters.CreateAddedRow(variable, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that only exist in the before state.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Ordered removed variable rows.</returns>
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildRemoved(
        IReadOnlyList<BuildDefinitionVariableValues> beforeVariables,
        IReadOnlyList<BuildDefinitionVariableValues> afterVariables,
        string? providerName)
    {
        var afterNames = new HashSet<string>(afterVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        return beforeVariables
            .Where(variable => !afterNames.Contains(variable.Name))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(variable => BuildDefinitionFormatters.CreateRemovedRow(variable, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that exist in both states but differ.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="largeValueFormat">Preferred diff format.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Ordered modified variable rows.</returns>
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildModified(
        IReadOnlyList<BuildDefinitionVariableValues> beforeVariables,
        IReadOnlyList<BuildDefinitionVariableValues> afterVariables,
        LargeValueFormat largeValueFormat,
        string? providerName)
    {
        var beforeLookup = beforeVariables.ToDictionary(v => v.Name, StringComparer.OrdinalIgnoreCase);

        return afterVariables
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && !VariablesEqual(before!, after))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(after => BuildDefinitionFormatters.CreateDiffRow(beforeLookup[after.Name], after, largeValueFormat, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for variables that remain unchanged between states.
    /// </summary>
    /// <param name="beforeVariables">Variables from the before state.</param>
    /// <param name="afterVariables">Variables from the after state.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Ordered unchanged variable rows.</returns>
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildUnchanged(
        IReadOnlyList<BuildDefinitionVariableValues> beforeVariables,
        IReadOnlyList<BuildDefinitionVariableValues> afterVariables,
        string? providerName)
    {
        var beforeLookup = beforeVariables.ToDictionary(v => v.Name, StringComparer.OrdinalIgnoreCase);

        return afterVariables
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && VariablesEqual(before!, after))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(variable => BuildDefinitionFormatters.CreateUnchangedRow(variable, providerName))
            .ToList();
    }

    /// <summary>
    /// Determines if two variables are equal by comparing all their attributes.
    /// </summary>
    /// <param name="before">Before variable.</param>
    /// <param name="after">After variable.</param>
    /// <returns>True if equal; otherwise false.</returns>
    private static bool VariablesEqual(BuildDefinitionVariableValues before, BuildDefinitionVariableValues after)
    {
        return before.Value == after.Value
            && before.IsSecret == after.IsSecret
            && before.AllowOverride == after.AllowOverride;
    }
}
