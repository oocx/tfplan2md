using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Registry that maps resource types to their corresponding view model factories.
/// </summary>
/// <remarks>
/// This registry pattern reduces ReportModelBuilder's class coupling by eliminating
/// direct dependencies on specific factory classes.
/// Related feature: docs/features/046-code-quality-metrics-enforcement/specification.md.
/// </remarks>
internal sealed class ResourceViewModelFactoryRegistry
{
    private readonly Dictionary<string, IResourceViewModelFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceViewModelFactoryRegistry"/> class.
    /// </summary>
    /// <param name="largeValueFormat">The format to use for large values.</param>
    /// <param name="principalMapper">The mapper for resolving principal names.</param>
    public ResourceViewModelFactoryRegistry(LargeValueFormat largeValueFormat, IPrincipalMapper principalMapper)
    {
        // Register all resource-specific factories
        _factories["azurerm_network_security_group"] = new NetworkSecurityGroupFactory(largeValueFormat);
        _factories["azurerm_firewall_network_rule_collection"] = new FirewallNetworkRuleCollectionFactory(largeValueFormat);
        _factories["azurerm_role_assignment"] = new RoleAssignmentFactory(principalMapper);
        _factories["azuredevops_variable_group"] = new VariableGroupFactory(largeValueFormat);
    }

    /// <summary>
    /// Tries to get a factory for the specified resource type.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type (e.g., "azurerm_network_security_group").</param>
    /// <param name="factory">The factory if one is registered for this type; otherwise, null.</param>
    /// <returns>True if a factory was found; otherwise, false.</returns>
    public bool TryGetFactory(string resourceType, out IResourceViewModelFactory? factory)
    {
        return _factories.TryGetValue(resourceType, out factory);
    }

    /// <summary>
    /// Adapter for <see cref="NetworkSecurityGroupViewModelFactory"/>.
    /// </summary>
    private sealed class NetworkSecurityGroupFactory : IResourceViewModelFactory
    {
        private readonly LargeValueFormat _largeValueFormat;

        public NetworkSecurityGroupFactory(LargeValueFormat largeValueFormat)
        {
            _largeValueFormat = largeValueFormat;
        }

        public void ApplyViewModel(
            ResourceChangeModel model,
            Parsing.ResourceChange resourceChange,
            string action,
            System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges)
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
    private sealed class FirewallNetworkRuleCollectionFactory : IResourceViewModelFactory
    {
        private readonly LargeValueFormat _largeValueFormat;

        public FirewallNetworkRuleCollectionFactory(LargeValueFormat largeValueFormat)
        {
            _largeValueFormat = largeValueFormat;
        }

        public void ApplyViewModel(
            ResourceChangeModel model,
            Parsing.ResourceChange resourceChange,
            string action,
            System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges)
        {
            model.FirewallNetworkRuleCollection = FirewallNetworkRuleCollectionViewModelFactory.Build(
                resourceChange,
                resourceChange.ProviderName,
                _largeValueFormat);
        }
    }

    /// <summary>
    /// Adapter for <see cref="RoleAssignmentViewModelFactory"/>.
    /// </summary>
    private sealed class RoleAssignmentFactory : IResourceViewModelFactory
    {
        private readonly IPrincipalMapper _principalMapper;

        public RoleAssignmentFactory(IPrincipalMapper principalMapper)
        {
            _principalMapper = principalMapper;
        }

        public void ApplyViewModel(
            ResourceChangeModel model,
            Parsing.ResourceChange resourceChange,
            string action,
            System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges)
        {
            model.RoleAssignment = RoleAssignmentViewModelFactory.Build(
                resourceChange,
                action,
                attributeChanges,
                _principalMapper);
        }
    }

    /// <summary>
    /// Adapter for <see cref="VariableGroupViewModelFactory"/>.
    /// </summary>
    private sealed class VariableGroupFactory : IResourceViewModelFactory
    {
        private readonly LargeValueFormat _largeValueFormat;

        public VariableGroupFactory(LargeValueFormat largeValueFormat)
        {
            _largeValueFormat = largeValueFormat;
        }

        public void ApplyViewModel(
            ResourceChangeModel model,
            Parsing.ResourceChange resourceChange,
            string action,
            System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges)
        {
            model.VariableGroup = VariableGroupViewModelFactory.Build(
                resourceChange,
                resourceChange.ProviderName,
                _largeValueFormat);
        }
    }
}
