using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
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
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
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
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
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
/// Adapter for <see cref="RoleAssignmentViewModelFactory"/>.
/// </summary>
internal sealed class RoleAssignmentFactory : IResourceViewModelFactory
{
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignmentFactory"/> class.
    /// </summary>
    /// <param name="principalMapper">The mapper for resolving principal names.</param>
    internal RoleAssignmentFactory(IPrincipalMapper principalMapper)
    {
        _principalMapper = principalMapper;
    }

    /// <inheritdoc/>
    public void ApplyViewModel(
        ResourceChangeModel model,
        Parsing.ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
        model.RoleAssignment = RoleAssignmentViewModelFactory.Build(
            resourceChange,
            action,
            attributeChanges,
            _principalMapper);
    }
}
