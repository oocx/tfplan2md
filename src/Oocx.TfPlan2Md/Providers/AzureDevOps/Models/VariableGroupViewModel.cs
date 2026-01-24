using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Provides precomputed variable data for the azuredevops_variable_group template.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </summary>
public sealed class VariableGroupViewModel
{
    /// <summary>
    /// Gets the variable group name derived from the after/before state.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the variable group description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the variable changes for update scenarios (added, modified, removed, unchanged).
    /// Merges both regular and secret variables into a unified list.
    /// </summary>
    public IReadOnlyList<VariableChangeRowViewModel> VariableChanges { get; init; } = Array.Empty<VariableChangeRowViewModel>();

    /// <summary>
    /// Gets the variables after the change, used for create operations.
    /// Merges both regular and secret variables into a unified list.
    /// </summary>
    public IReadOnlyList<VariableRowViewModel> AfterVariables { get; init; } = Array.Empty<VariableRowViewModel>();

    /// <summary>
    /// Gets the variables before the change, used for delete operations.
    /// Merges both regular and secret variables into a unified list.
    /// </summary>
    public IReadOnlyList<VariableRowViewModel> BeforeVariables { get; init; } = Array.Empty<VariableRowViewModel>();

    /// <summary>
    /// Gets the Key Vault integration blocks.
    /// </summary>
    public IReadOnlyList<KeyVaultRowViewModel> KeyVaultBlocks { get; init; } = Array.Empty<KeyVaultRowViewModel>();
}

/// <summary>
/// Represents a variable row that includes a change indicator for update tables.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </summary>
public sealed class VariableChangeRowViewModel
{
    /// <summary>
    /// Gets the change symbol (➕/🔄/❌/⏺️).
    /// </summary>
    public required string Change { get; init; }

    /// <summary>
    /// Gets the formatted variable name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted value or diff. For secret variables, shows "(sensitive / hidden)".
    /// For changed attributes, shows before/after with - and + prefixes.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the formatted enabled status or diff.
    /// </summary>
    public required string Enabled { get; init; }

    /// <summary>
    /// Gets the formatted content_type or diff.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the formatted expires value or diff.
    /// </summary>
    public required string Expires { get; init; }

    /// <summary>
    /// Gets a value indicating whether this variable has a large value (>100 chars or multi-line).
    /// Large values are moved to a separate collapsible section.
    /// </summary>
    public bool IsLargeValue { get; init; }
}

/// <summary>
/// Represents a variable row used for create/delete tables.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </summary>
public sealed class VariableRowViewModel
{
    /// <summary>
    /// Gets the formatted variable name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted value. For secret variables, shows "(sensitive / hidden)".
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the formatted enabled status.
    /// </summary>
    public required string Enabled { get; init; }

    /// <summary>
    /// Gets the formatted content_type.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the formatted expires value.
    /// </summary>
    public required string Expires { get; init; }

    /// <summary>
    /// Gets a value indicating whether this variable has a large value (>100 chars or multi-line).
    /// Large values are moved to a separate collapsible section.
    /// </summary>
    public bool IsLargeValue { get; init; }
}

/// <summary>
/// Represents a Key Vault block row for Key Vault integration table.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </summary>
public sealed class KeyVaultRowViewModel
{
    /// <summary>
    /// Gets the formatted Key Vault name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted service endpoint ID (GUID).
    /// </summary>
    public required string ServiceEndpointId { get; init; }

    /// <summary>
    /// Gets the formatted search depth value.
    /// </summary>
    public required string SearchDepth { get; init; }
}
