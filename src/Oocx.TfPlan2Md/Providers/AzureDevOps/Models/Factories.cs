using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;

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

    /// <summary>
    /// Creates a VariableGroupViewModel for the given resource change.
    /// </summary>
    /// <param name="resourceChange">The resource change to create view model for.</param>
    /// <returns>The created view model.</returns>
    internal VariableGroupViewModel CreateViewModel(ResourceChange resourceChange)
    {
        return VariableGroupViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);
    }
}

/// <summary>
/// Factory adapter for creating <see cref="BuildDefinitionViewModel"/> instances.
/// Adapts the static factory to implement <see cref="IResourceViewModelFactory"/>.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
internal sealed class BuildDefinitionFactory : IResourceViewModelFactory
{
    private readonly LargeValueFormat _largeValueFormat;
    private readonly AzdoRepositoryMapper? _repositoryMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildDefinitionFactory"/> class.
    /// </summary>
    /// <param name="largeValueFormat">Format for rendering large values.</param>
    /// <param name="repositoryMapper">Optional mapper for Azure DevOps repository display names.</param>
    public BuildDefinitionFactory(LargeValueFormat largeValueFormat, AzdoRepositoryMapper? repositoryMapper = null)
    {
        _largeValueFormat = largeValueFormat;
        _repositoryMapper = repositoryMapper;
    }

    /// <summary>
    /// Creates a BuildDefinitionViewModel for the given resource change.
    /// </summary>
    /// <param name="resourceChange">The resource change to create view model for.</param>
    /// <returns>The created view model.</returns>
    internal BuildDefinitionViewModel CreateViewModel(ResourceChange resourceChange)
    {
        return BuildDefinitionViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat,
            _repositoryMapper);
    }
}
