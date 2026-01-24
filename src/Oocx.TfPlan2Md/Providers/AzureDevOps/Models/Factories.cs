using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Factory adapter for creating <see cref="VariableGroupViewModel"/> instances.
/// Adapts the static factory to implement <see cref="IResourceViewModelFactory"/>.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
internal sealed class VariableGroupFactory : IResourceViewModelFactory
{
    private readonly LargeValueFormat _largeValueFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableGroupFactory"/> class.
    /// </summary>
    /// <param name="largeValueFormat">Format for rendering large values.</param>
    public VariableGroupFactory(LargeValueFormat largeValueFormat)
    {
        _largeValueFormat = largeValueFormat;
    }

    /// <inheritdoc/>
    public void ApplyViewModel(
        ResourceChangeModel model,
        ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
        model.VariableGroup = VariableGroupViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);
    }
}
