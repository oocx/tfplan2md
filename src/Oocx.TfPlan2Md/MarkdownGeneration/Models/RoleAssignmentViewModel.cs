using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Provides precomputed display data for the azurerm_role_assignment template.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
public sealed class RoleAssignmentViewModel
{
    /// <summary>
    /// Gets the extracted resource name from the full address.
    /// </summary>
    public string? ResourceName { get; init; }

    /// <summary>
    /// Gets the role assignment description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the formatted summary text for the summary element (principal → role on scope).
    /// </summary>
    public string? SummaryText { get; init; }

    /// <summary>
    /// Gets the ordered list of small (non-large) attributes for table display.
    /// </summary>
    public IReadOnlyList<RoleAssignmentAttributeViewModel> SmallAttributes { get; init; } = Array.Empty<RoleAssignmentAttributeViewModel>();

    /// <summary>
    /// Gets the ordered list of large attributes for collapsible display.
    /// </summary>
    public IReadOnlyList<RoleAssignmentAttributeViewModel> LargeAttributes { get; init; } = Array.Empty<RoleAssignmentAttributeViewModel>();
}

/// <summary>
/// Represents a formatted role assignment attribute for display.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
public sealed class RoleAssignmentAttributeViewModel
{
    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted before value.
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Gets the formatted after value.
    /// </summary>
    public string? After { get; init; }
}
