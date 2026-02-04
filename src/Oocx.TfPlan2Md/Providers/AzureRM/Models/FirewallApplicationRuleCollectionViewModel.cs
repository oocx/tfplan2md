using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Provides precomputed rule data for the azurerm_firewall_application_rule_collection template.
/// Related feature: docs/features/060-azurerm-firewall-application-rule-template/specification.md.
/// </summary>
public sealed class FirewallApplicationRuleCollectionViewModel
{
    /// <summary>
    /// Gets the firewall application rule collection name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the formatted priority value.
    /// </summary>
    public string? Priority { get; init; }

    /// <summary>
    /// Gets the formatted action value (Allow/Deny with icons).
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Gets the rule changes for update scenarios (added, modified, removed, unchanged).
    /// </summary>
    public IReadOnlyList<FirewallApplicationRuleChangeRowViewModel> RuleChanges { get; init; } = Array.Empty<FirewallApplicationRuleChangeRowViewModel>();

    /// <summary>
    /// Gets the rules after the change, used for create tables.
    /// </summary>
    public IReadOnlyList<FirewallApplicationRuleRowViewModel> AfterRules { get; init; } = Array.Empty<FirewallApplicationRuleRowViewModel>();

    /// <summary>
    /// Gets the rules before the change, used for delete tables.
    /// </summary>
    public IReadOnlyList<FirewallApplicationRuleRowViewModel> BeforeRules { get; init; } = Array.Empty<FirewallApplicationRuleRowViewModel>();
}

/// <summary>
/// Represents a firewall application rule row that includes a change indicator for update tables.
/// Related feature: docs/features/060-azurerm-firewall-application-rule-template/specification.md.
/// </summary>
public sealed class FirewallApplicationRuleChangeRowViewModel
{
    /// <summary>
    /// Gets the change symbol (➕/🔄/❌/⏺️).
    /// </summary>
    public required string Change { get; init; }

    /// <summary>
    /// Gets the formatted rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted protocols value or diff (e.g., Https:443, Http:80).
    /// </summary>
    public required string Protocols { get; init; }

    /// <summary>
    /// Gets the formatted source addresses value or diff.
    /// </summary>
    public required string SourceAddresses { get; init; }

    /// <summary>
    /// Gets the formatted source IP groups value or diff (optional, may be empty).
    /// </summary>
    public required string SourceIpGroups { get; init; }

    /// <summary>
    /// Gets the formatted target FQDNs value or diff.
    /// </summary>
    public required string TargetFqdns { get; init; }

    /// <summary>
    /// Gets the formatted FQDN tags value or diff (optional, may be empty).
    /// </summary>
    public required string FqdnTags { get; init; }

    /// <summary>
    /// Gets the formatted description value or diff.
    /// </summary>
    public required string Description { get; init; }
}

/// <summary>
/// Represents a firewall application rule row used for create/delete tables.
/// Related feature: docs/features/060-azurerm-firewall-application-rule-template/specification.md.
/// </summary>
public sealed class FirewallApplicationRuleRowViewModel
{
    /// <summary>
    /// Gets the formatted rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted protocols value (e.g., Https:443, Http:80).
    /// </summary>
    public required string Protocols { get; init; }

    /// <summary>
    /// Gets the formatted source addresses value.
    /// </summary>
    public required string SourceAddresses { get; init; }

    /// <summary>
    /// Gets the formatted source IP groups value (optional, may be empty).
    /// </summary>
    public required string SourceIpGroups { get; init; }

    /// <summary>
    /// Gets the formatted target FQDNs value.
    /// </summary>
    public required string TargetFqdns { get; init; }

    /// <summary>
    /// Gets the formatted FQDN tags value (optional, may be empty).
    /// </summary>
    public required string FqdnTags { get; init; }

    /// <summary>
    /// Gets the formatted description value.
    /// </summary>
    public required string Description { get; init; }
}
