using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Provides precomputed rule data for the azurerm_firewall_network_rule_collection template.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
public sealed class FirewallNetworkRuleCollectionViewModel
{
    /// <summary>
    /// Gets the firewall network rule collection name.
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
    public IReadOnlyList<FirewallRuleChangeRowViewModel> RuleChanges { get; init; } = Array.Empty<FirewallRuleChangeRowViewModel>();

    /// <summary>
    /// Gets the rules after the change, used for create tables.
    /// </summary>
    public IReadOnlyList<FirewallRuleRowViewModel> AfterRules { get; init; } = Array.Empty<FirewallRuleRowViewModel>();

    /// <summary>
    /// Gets the rules before the change, used for delete tables.
    /// </summary>
    public IReadOnlyList<FirewallRuleRowViewModel> BeforeRules { get; init; } = Array.Empty<FirewallRuleRowViewModel>();
}

/// <summary>
/// Represents a firewall rule row that includes a change indicator for update tables.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
public sealed class FirewallRuleChangeRowViewModel
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
    /// Gets the formatted protocols value or diff.
    /// </summary>
    public required string Protocols { get; init; }

    /// <summary>
    /// Gets the formatted source addresses value or diff.
    /// </summary>
    public required string SourceAddresses { get; init; }

    /// <summary>
    /// Gets the formatted destination addresses value or diff.
    /// </summary>
    public required string DestinationAddresses { get; init; }

    /// <summary>
    /// Gets the formatted destination ports value or diff.
    /// </summary>
    public required string DestinationPorts { get; init; }

    /// <summary>
    /// Gets the formatted description value or diff.
    /// </summary>
    public required string Description { get; init; }
}

/// <summary>
/// Represents a firewall rule row used for create/delete tables.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
public sealed class FirewallRuleRowViewModel
{
    /// <summary>
    /// Gets the formatted rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted protocols value.
    /// </summary>
    public required string Protocols { get; init; }

    /// <summary>
    /// Gets the formatted source addresses value.
    /// </summary>
    public required string SourceAddresses { get; init; }

    /// <summary>
    /// Gets the formatted destination addresses value.
    /// </summary>
    public required string DestinationAddresses { get; init; }

    /// <summary>
    /// Gets the formatted destination ports value.
    /// </summary>
    public required string DestinationPorts { get; init; }

    /// <summary>
    /// Gets the formatted description value.
    /// </summary>
    public required string Description { get; init; }
}
