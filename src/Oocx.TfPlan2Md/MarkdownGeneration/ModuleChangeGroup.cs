using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Groups resource changes by module for hierarchical presentation.
/// </summary>
public class ModuleChangeGroup
{
    /// <summary>
    /// Gets the the module address (e.g. "module.network.module.subnet"). Empty string represents the root module.
    /// </summary>
    public required string ModuleAddress { get; init; }

    /// <summary>
    /// Gets the list of resource changes within this module.
    /// </summary>
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }
}
