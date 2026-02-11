using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Mappers;

/// <summary>
/// Maps azurerm_role_assignment resources to ScriptObject with RoleAssignmentViewModel.
/// </summary>
internal sealed class RoleAssignmentMapper : IResourceModelMapper
{
    private readonly RoleAssignmentFactory _factory;

    /// <summary>
    /// The Scriban key used for description fields.
    /// </summary>
    private const string DescriptionKey = "description";

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignmentMapper"/> class.
    /// </summary>
    /// <param name="factory">The factory for creating RoleAssignmentViewModel instances.</param>
    public RoleAssignmentMapper(RoleAssignmentFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <summary>
    /// Determines whether this mapper applies to the resource.
    /// </summary>
    /// <param name="resource">The resource to evaluate.</param>
    /// <returns><c>true</c> if the resource is azurerm_role_assignment; otherwise, <c>false</c>.</returns>
    public bool CanMap(ResourceChangeModel resource)
    {
        return resource.Type == "azurerm_role_assignment";
    }

    /// <summary>
    /// Enriches the ScriptObject with role_assignment property.
    /// </summary>
    /// <param name="resource">The resource change model.</param>
    /// <param name="scriptObject">The ScriptObject to enrich.</param>
    public void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)
    {
        if (resource.ResourceChange == null)
        {
            return;
        }

        var viewModel = _factory.CreateViewModel(resource.ResourceChange, resource.Action, resource.AttributeChanges);
        scriptObject["role_assignment"] = MapRoleAssignment(viewModel);
    }

    /// <summary>
    /// Maps a RoleAssignmentViewModel to a ScriptObject.
    /// </summary>
    /// <param name="ra">The view model to map.</param>
    /// <returns>A ScriptObject containing the mapped data.</returns>
    private static ScriptObject MapRoleAssignment(RoleAssignmentViewModel ra)
    {
        var obj = new ScriptObject
        {
            ["resource_name"] = ra.ResourceName,
            [DescriptionKey] = ra.Description,
            ["summary_text"] = ra.SummaryText
        };

        // Small attributes for table display
        var smallAttributes = new ScriptArray();
        foreach (var attr in ra.SmallAttributes)
        {
            smallAttributes.Add(MapRoleAssignmentAttribute(attr));
        }

        obj["small_attributes"] = smallAttributes;

        // Large attributes for collapsible display
        var largeAttributes = new ScriptArray();
        foreach (var attr in ra.LargeAttributes)
        {
            largeAttributes.Add(MapRoleAssignmentAttribute(attr));
        }

        obj["large_attributes"] = largeAttributes;

        return obj;
    }

    /// <summary>
    /// Maps a RoleAssignmentAttributeViewModel to a ScriptObject.
    /// </summary>
    /// <param name="attr">The attribute view model to map.</param>
    /// <returns>A ScriptObject containing the mapped attribute data.</returns>
    private static ScriptObject MapRoleAssignmentAttribute(RoleAssignmentAttributeViewModel attr)
    {
        return new ScriptObject
        {
            ["name"] = attr.Name,
            ["before"] = attr.Before,
            ["after"] = attr.After
        };
    }
}
