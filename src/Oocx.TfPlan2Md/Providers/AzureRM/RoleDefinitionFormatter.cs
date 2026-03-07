using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Formats Azure role definition attributes with mapped role names.
/// </summary>
/// <remarks>
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </remarks>
internal sealed class RoleDefinitionFormatter : IValueFormatter
{
    /// <summary>
    /// Resolver used to format Azure role definition identifiers for the current run.
    /// </summary>
    private readonly IRoleDefinitionResolver _roleDefinitionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleDefinitionFormatter"/> class.
    /// </summary>
    /// <param name="roleDefinitionResolver">Optional run-scoped resolver for role definitions.</param>
    internal RoleDefinitionFormatter(IRoleDefinitionResolver? roleDefinitionResolver = null)
    {
        _roleDefinitionResolver = roleDefinitionResolver ?? AzureRoleDefinitionResolver.CreateBuiltIn();
    }

    /// <summary>
    /// Attempts to format role definition values into a human-readable form.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>A formatted role string when handled; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var roleInfo = _roleDefinitionResolver.GetRoleDefinition(context.Value, null);
        if (string.IsNullOrWhiteSpace(roleInfo.FullName))
        {
            return null;
        }

        var roleText = $"🛡️{MarkdownHelpers.NonBreakingSpace}{roleInfo.FullName}";
        return MarkdownHelpers.FormatCodeTable(roleText);
    }
}
