using System;
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
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkSecurityGroupFactory"/> class.
    /// </summary>
    internal NetworkSecurityGroupFactory()
    {
    }

    /// <summary>
    /// Creates a NetworkSecurityGroupViewModel for the given resource change.
    /// </summary>
    /// <param name="resourceChange">The resource change to create view model for.</param>
    /// <returns>The created view model.</returns>
    internal NetworkSecurityGroupViewModel CreateViewModel(Parsing.ResourceChange resourceChange)
    {
        return NetworkSecurityGroupViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName);
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
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Create view model and populate ChangedAttributesSummary
        var (_, changedAttributesSummary) = CreateViewModel(context.ResourceChange, context.Action);
        if (!string.IsNullOrWhiteSpace(changedAttributesSummary))
        {
            context.Model.ChangedAttributesSummary = changedAttributesSummary;
        }
    }

    /// <summary>
    /// Creates a FirewallNetworkRuleCollectionViewModel for the given resource change.
    /// </summary>
    /// <param name="resourceChange">The resource change to create view model for.</param>
    /// <param name="action">The action being performed.</param>
    /// <returns>The created view model and changed attributes summary.</returns>
    internal (FirewallNetworkRuleCollectionViewModel ViewModel, string? ChangedAttributesSummary) CreateViewModel(
        Parsing.ResourceChange resourceChange,
        string action)
    {
        var viewModel = FirewallNetworkRuleCollectionViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);

        var summary = FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary(
            viewModel,
            action);

        return (viewModel, summary);
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
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Create view model and populate ChangedAttributesSummary
        var (_, changedAttributesSummary) = CreateViewModel(context.ResourceChange, context.Action);
        if (!string.IsNullOrWhiteSpace(changedAttributesSummary))
        {
            context.Model.ChangedAttributesSummary = changedAttributesSummary;
        }
    }

    /// <summary>
    /// Creates a FirewallApplicationRuleCollectionViewModel for the given resource change.
    /// </summary>
    /// <param name="resourceChange">The resource change to create view model for.</param>
    /// <param name="action">The action being performed.</param>
    /// <returns>The created view model and changed attributes summary.</returns>
    internal (FirewallApplicationRuleCollectionViewModel ViewModel, string? ChangedAttributesSummary) CreateViewModel(
        Parsing.ResourceChange resourceChange,
        string action)
    {
        var viewModel = FirewallApplicationRuleCollectionViewModelFactory.Build(
            resourceChange,
            resourceChange.ProviderName,
            _largeValueFormat);

        var summary = FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(
            viewModel,
            action);

        return (viewModel, summary);
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
    /// Resolver used to format Azure role definition names for the current run.
    /// </summary>
    private readonly IRoleDefinitionResolver _roleDefinitionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleAssignmentFactory"/> class.
    /// </summary>
    /// <param name="principalMapper">The mapper for resolving principal names.</param>
    /// <param name="scopeFormatter">Optional formatter for enriched scope display.</param>
    /// <param name="roleDefinitionResolver">Optional run-scoped resolver for role definition names.</param>
    internal RoleAssignmentFactory(
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter? scopeFormatter,
        IRoleDefinitionResolver? roleDefinitionResolver = null)
    {
        _principalMapper = principalMapper;
        _scopeFormatter = scopeFormatter;
        _roleDefinitionResolver = roleDefinitionResolver ?? AzureRoleDefinitionResolver.CreateBuiltIn();
    }

    /// <summary>
    /// Creates a RoleAssignmentViewModel for the given resource change.
    /// </summary>
    /// <param name="resourceChange">The resource change to create view model for.</param>
    /// <param name="action">The action being performed.</param>
    /// <param name="attributeChanges">The attribute changes.</param>
    /// <returns>The created view model.</returns>
    internal RoleAssignmentViewModel CreateViewModel(
        Parsing.ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
        return RoleAssignmentViewModelFactory.Build(
            resourceChange,
            action,
            attributeChanges,
            _principalMapper,
            _scopeFormatter,
            _roleDefinitionResolver);
    }
}
