using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Mappers;

/// <summary>
/// Maps azuredevops_variable_group resources to ScriptObject with VariableGroupViewModel.
/// </summary>
internal sealed class VariableGroupMapper : IResourceModelMapper
{
    private readonly VariableGroupFactory _factory;

    /// <summary>
    /// The Scriban key used for description fields.
    /// </summary>
    private const string DescriptionKey = "description";

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableGroupMapper"/> class.
    /// </summary>
    /// <param name="factory">The factory for creating VariableGroupViewModel instances.</param>
    public VariableGroupMapper(VariableGroupFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <summary>
    /// Determines whether this mapper applies to the resource.
    /// </summary>
    /// <param name="resource">The resource to evaluate.</param>
    /// <returns><c>true</c> if the resource is azuredevops_variable_group; otherwise, <c>false</c>.</returns>
    public bool CanMap(ResourceChangeModel resource)
    {
        return resource.Type == "azuredevops_variable_group";
    }

    /// <summary>
    /// Enriches the ScriptObject with variable_group property.
    /// </summary>
    /// <param name="resource">The resource change model.</param>
    /// <param name="scriptObject">The ScriptObject to enrich.</param>
    public void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)
    {
        if (resource.ResourceChange == null)
        {
            return;
        }

        var viewModel = _factory.CreateViewModel(resource.ResourceChange);
        scriptObject["variable_group"] = MapVariableGroup(viewModel);
    }

    /// <summary>
    /// Maps a VariableGroupViewModel to a ScriptObject.
    /// </summary>
    /// <param name="vg">The view model to map.</param>
    /// <returns>A ScriptObject containing the mapped data.</returns>
    private static ScriptObject MapVariableGroup(VariableGroupViewModel vg)
    {
        var obj = new ScriptObject
        {
            ["name"] = vg.Name,
            [DescriptionKey] = vg.Description
        };

        // Variable changes for update scenarios
        var variableChanges = new ScriptArray();
        foreach (var variable in vg.VariableChanges)
        {
            variableChanges.Add(MapVariableChangeRow(variable));
        }

        obj["variable_changes"] = variableChanges;

        // After variables for create scenarios
        var afterVariables = new ScriptArray();
        foreach (var variable in vg.AfterVariables)
        {
            afterVariables.Add(MapVariableRow(variable));
        }

        obj["after_variables"] = afterVariables;

        // Before variables for delete scenarios
        var beforeVariables = new ScriptArray();
        foreach (var variable in vg.BeforeVariables)
        {
            beforeVariables.Add(MapVariableRow(variable));
        }

        obj["before_variables"] = beforeVariables;

        // Key Vault blocks
        var keyVaultBlocks = new ScriptArray();
        foreach (var kv in vg.KeyVaultBlocks)
        {
            keyVaultBlocks.Add(MapKeyVaultRow(kv));
        }

        obj["key_vault_blocks"] = keyVaultBlocks;

        return obj;
    }

    /// <summary>
    /// Maps a VariableChangeRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="variable">The variable change row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped variable change data.</returns>
    private static ScriptObject MapVariableChangeRow(VariableChangeRowViewModel variable)
    {
        return new ScriptObject
        {
            ["change"] = variable.Change,
            ["change_icon"] = variable.ChangeIcon,
            ["name"] = variable.Name,
            ["value"] = variable.Value,
            ["enabled"] = variable.Enabled,
            ["content_type"] = variable.ContentType,
            ["expires"] = variable.Expires,
            ["is_large_value"] = variable.IsLargeValue
        };
    }

    /// <summary>
    /// Maps a VariableRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="variable">The variable row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped variable data.</returns>
    private static ScriptObject MapVariableRow(VariableRowViewModel variable)
    {
        return new ScriptObject
        {
            ["name"] = variable.Name,
            ["value"] = variable.Value,
            ["enabled"] = variable.Enabled,
            ["content_type"] = variable.ContentType,
            ["expires"] = variable.Expires,
            ["is_large_value"] = variable.IsLargeValue
        };
    }

    /// <summary>
    /// Maps a KeyVaultRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="kv">The key vault row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped key vault data.</returns>
    private static ScriptObject MapKeyVaultRow(KeyVaultRowViewModel kv)
    {
        return new ScriptObject
        {
            ["name"] = kv.Name,
            ["service_endpoint_id"] = kv.ServiceEndpointId,
            ["search_depth"] = kv.SearchDepth
        };
    }
}
