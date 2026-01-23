using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds <see cref="VariableGroupViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </summary>
internal static class VariableGroupViewModelFactory
{
    /// <summary>
    /// Creates a view model for the provided variable group change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <param name="largeValueFormat">Preferred large value format for diff rendering.</param>
    /// <returns>Populated <see cref="VariableGroupViewModel"/>.</returns>
    public static VariableGroupViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)
    {
        _ = providerName; // Not used for Azure DevOps variables

        var name = VariableGroupExtractors.ExtractName(change.Change.After) ?? VariableGroupExtractors.ExtractName(change.Change.Before);
        var description = VariableGroupExtractors.ExtractDescription(change.Change.After) ?? VariableGroupExtractors.ExtractDescription(change.Change.Before);

        var beforeVariables = VariableGroupExtractors.ExtractVariables(change.Change.Before);
        var afterVariables = VariableGroupExtractors.ExtractVariables(change.Change.After);

        var keyVaultBlocks = VariableGroupExtractors.ExtractKeyVaultBlocks(change.Change.After) ?? VariableGroupExtractors.ExtractKeyVaultBlocks(change.Change.Before);

        // Determine action type (create, update, delete)
        var actions = change.Change.Actions ?? Array.Empty<string>();
        var isCreate = actions.Contains("create") && !actions.Contains("delete");
        var isDelete = actions.Contains("delete") && !actions.Contains("create");

        if (isCreate)
        {
            return new VariableGroupViewModel
            {
                Name = name,
                Description = description,
                AfterVariables = VariableGroupFormatters.FormatVariableRows(afterVariables),
                KeyVaultBlocks = keyVaultBlocks
            };
        }
        else if (isDelete)
        {
            return new VariableGroupViewModel
            {
                Name = name,
                Description = description,
                BeforeVariables = VariableGroupFormatters.FormatVariableRows(beforeVariables),
                KeyVaultBlocks = keyVaultBlocks
            };
        }
        else // update or replace
        {
            var added = VariableGroupChangeBuilders.BuildAdded(afterVariables, beforeVariables);
            var removed = VariableGroupChangeBuilders.BuildRemoved(beforeVariables, afterVariables);
            var modified = VariableGroupChangeBuilders.BuildModified(beforeVariables, afterVariables, largeValueFormat);
            var unchanged = VariableGroupChangeBuilders.BuildUnchanged(beforeVariables, afterVariables);

            var changeRows = new List<VariableChangeRowViewModel>();
            changeRows.AddRange(added);
            changeRows.AddRange(modified);
            changeRows.AddRange(removed);
            changeRows.AddRange(unchanged);

            return new VariableGroupViewModel
            {
                Name = name,
                Description = description,
                VariableChanges = changeRows,
                AfterVariables = VariableGroupFormatters.FormatVariableRows(afterVariables),
                BeforeVariables = VariableGroupFormatters.FormatVariableRows(beforeVariables),
                KeyVaultBlocks = keyVaultBlocks
            };
        }
    }
}

/// <summary>
/// Represents raw variable values extracted from Terraform plan.
/// </summary>
/// <param name="Name">The variable name.</param>
/// <param name="Value">The variable value.</param>
/// <param name="Enabled">Whether the variable is enabled.</param>
/// <param name="ContentType">The content type.</param>
/// <param name="Expires">The expiration value.</param>
/// <param name="IsSecret">Whether this is a secret variable.</param>
internal record VariableValues(string Name, string? Value, bool? Enabled, string? ContentType, string? Expires, bool IsSecret);
