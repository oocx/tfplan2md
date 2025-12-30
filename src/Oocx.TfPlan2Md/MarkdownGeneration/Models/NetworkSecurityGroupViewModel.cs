using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Provides precomputed rule data for the azurerm_network_security_group template.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
public sealed class NetworkSecurityGroupViewModel
{
    /// <summary>
    /// Gets the network security group name derived from the after/before state.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the rule changes for update scenarios (added, modified, removed, unchanged).
    /// </summary>
    public IReadOnlyList<SecurityRuleChangeRowViewModel> RuleChanges { get; init; } = Array.Empty<SecurityRuleChangeRowViewModel>();

    /// <summary>
    /// Gets the rules after the change, used for create and update tables.
    /// </summary>
    public IReadOnlyList<SecurityRuleRowViewModel> AfterRules { get; init; } = Array.Empty<SecurityRuleRowViewModel>();

    /// <summary>
    /// Gets the rules before the change, used for delete and update tables.
    /// </summary>
    public IReadOnlyList<SecurityRuleRowViewModel> BeforeRules { get; init; } = Array.Empty<SecurityRuleRowViewModel>();
}

/// <summary>
/// Represents a security rule row that includes a change indicator for update tables.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
public sealed class SecurityRuleChangeRowViewModel
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
    /// Gets the formatted priority value or diff.
    /// </summary>
    public required string Priority { get; init; }

    /// <summary>
    /// Gets the formatted direction value or diff.
    /// </summary>
    public required string Direction { get; init; }

    /// <summary>
    /// Gets the formatted access value or diff.
    /// </summary>
    public required string Access { get; init; }

    /// <summary>
    /// Gets the formatted protocol value or diff.
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Gets the formatted source addresses value or diff.
    /// </summary>
    public required string SourceAddresses { get; init; }

    /// <summary>
    /// Gets the formatted source ports value or diff.
    /// </summary>
    public required string SourcePorts { get; init; }

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
/// Represents a security rule row used for create/delete tables.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
public sealed class SecurityRuleRowViewModel
{
    /// <summary>
    /// Gets the formatted rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted priority value.
    /// </summary>
    public required string Priority { get; init; }

    /// <summary>
    /// Gets the formatted direction value.
    /// </summary>
    public required string Direction { get; init; }

    /// <summary>
    /// Gets the formatted access value.
    /// </summary>
    public required string Access { get; init; }

    /// <summary>
    /// Gets the formatted protocol value.
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Gets the formatted source addresses value.
    /// </summary>
    public required string SourceAddresses { get; init; }

    /// <summary>
    /// Gets the formatted source ports value.
    /// </summary>
    public required string SourcePorts { get; init; }

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
