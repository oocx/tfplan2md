using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Adapter for <see cref="NetworkSecurityGroupViewModelFactory"/>.
/// </summary>
internal sealed class NetworkSecurityGroupFactory : IResourceViewModelFactory
{
    private readonly LargeValueFormat _largeValueFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkSecurityGroupFactory"/> class.
    /// </summary>
    /// <param name="largeValueFormat">The format to use for large values.</param>
    internal NetworkSecurityGroupFactory(LargeValueFormat largeValueFormat)
    {
        _largeValueFormat = largeValueFormat;
    }

    /// <inheritdoc/>
    public void ApplyViewModel(
        ResourceChangeModel model,
        Parsing.ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        _ = principalMapper;
        _ = iconProviderRegistry;

        model.NetworkSecurityGroup = NetworkSecurityGroupViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);
    }
}

/// <summary>
/// Adapter for <see cref="FirewallNetworkRuleCollectionViewModelFactory"/>.
/// </summary>
internal sealed class FirewallNetworkRuleCollectionFactory : IResourceViewModelFactory
{
    private readonly LargeValueFormat _largeValueFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallNetworkRuleCollectionFactory"/> class.
    /// </summary>
    /// <param name="largeValueFormat">The format to use for large values.</param>
    internal FirewallNetworkRuleCollectionFactory(LargeValueFormat largeValueFormat)
    {
        _largeValueFormat = largeValueFormat;
    }

    /// <inheritdoc/>
    public void ApplyViewModel(
        ResourceChangeModel model,
        Parsing.ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        _ = principalMapper;
        _ = iconProviderRegistry;

        var viewModel = FirewallNetworkRuleCollectionViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);

        model.FirewallNetworkRuleCollection = viewModel;
        model.ChangedAttributesSummary = FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary(
            viewModel,
            action);
    }
}

/// <summary>
/// Adapter for <see cref="FirewallApplicationRuleCollectionViewModelFactory"/>.
/// </summary>
internal sealed class FirewallApplicationRuleCollectionFactory : IResourceViewModelFactory
{
    private readonly LargeValueFormat _largeValueFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallApplicationRuleCollectionFactory"/> class.
    /// </summary>
    /// <param name="largeValueFormat">The format to use for large values.</param>
    internal FirewallApplicationRuleCollectionFactory(LargeValueFormat largeValueFormat)
    {
        _largeValueFormat = largeValueFormat;
    }

    /// <inheritdoc/>
    public void ApplyViewModel(
        ResourceChangeModel model,
        Parsing.ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        _ = principalMapper;
        _ = iconProviderRegistry;

        var viewModel = FirewallApplicationRuleCollectionViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);

        model.FirewallApplicationRuleCollection = viewModel;
        model.ChangedAttributesSummary = FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(
            viewModel,
            action);
    }
}

/// <summary>
/// Adapter for <see cref="RoleAssignmentViewModelFactory"/>.
/// </summary>
internal sealed class RoleAssignmentFactory : IResourceViewModelFactory
{
    /// <summary>
    /// Mapper used to resolve principal names.
    /// </summary>
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Optional formatter for enriched scope display.
    /// </summary>
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignmentFactory"/> class.
    /// </summary>
    /// <param name="principalMapper">The mapper for resolving principal names.</param>
    /// <param name="scopeFormatter">Optional formatter for enriched scope display.</param>
    internal RoleAssignmentFactory(IPrincipalMapper principalMapper, EnrichedAzureScopeFormatter? scopeFormatter)
    {
        _principalMapper = principalMapper;
        _scopeFormatter = scopeFormatter;
    }

    /// <inheritdoc/>
    public void ApplyViewModel(
        ResourceChangeModel model,
        Parsing.ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        _ = principalMapper;
        _ = iconProviderRegistry;

        model.RoleAssignment = RoleAssignmentViewModelFactory.Build(
            resourceChange,
            action,
            attributeChanges,
            _principalMapper,
            _scopeFormatter);
    }
}
