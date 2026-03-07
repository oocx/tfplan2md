using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Applies summary overrides for AzureRM PIM eligible role assignments.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </summary>
internal sealed class PimEligibleRoleAssignmentFactory : IResourceViewModelFactory
{
    /// <summary>
    /// Terraform resource type handled by this factory.
    /// </summary>
    private const string ResourceType = "azurerm_pim_eligible_role_assignment";

    /// <summary>
    /// Attribute name for role definition IDs.
    /// </summary>
    private const string RoleDefinitionIdAttribute = "role_definition_id";

    /// <summary>
    /// Attribute name for role definition names.
    /// </summary>
    private const string RoleDefinitionNameAttribute = "role_definition_name";

    /// <summary>
    /// Attribute name for principal IDs.
    /// </summary>
    private const string PrincipalIdAttribute = "principal_id";

    /// <summary>
    /// Attribute name for principal types.
    /// </summary>
    private const string PrincipalTypeAttribute = "principal_type";

    /// <summary>
    /// Mapper used for resolving principal display names.
    /// </summary>
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Resolver used to format Azure role definition names for the current run.
    /// </summary>
    private readonly IRoleDefinitionResolver _roleDefinitionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PimEligibleRoleAssignmentFactory"/> class.
    /// </summary>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="roleDefinitionResolver">Optional run-scoped resolver for role definition names.</param>
    internal PimEligibleRoleAssignmentFactory(
        IPrincipalMapper principalMapper,
        IRoleDefinitionResolver? roleDefinitionResolver = null)
    {
        ArgumentNullException.ThrowIfNull(principalMapper);
        _principalMapper = principalMapper;
        _roleDefinitionResolver = roleDefinitionResolver ?? AzureRoleDefinitionResolver.CreateBuiltIn();
    }

    /// <inheritdoc />
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(context.Model.Type, ResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var state = ResolveActiveState(context.ResourceChange, context.Action);
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);

        var roleInfo = _roleDefinitionResolver.GetRoleDefinition(
            GetValue(flatState, RoleDefinitionIdAttribute),
            GetValue(flatState, RoleDefinitionNameAttribute),
            context.ResourceChange.Address);
        var roleName = !string.IsNullOrWhiteSpace(roleInfo.Name)
            ? roleInfo.Name
            : roleInfo.Id;

        var principalId = GetValue(flatState, PrincipalIdAttribute);
        var principalType = GetValue(flatState, PrincipalTypeAttribute);
        if (string.IsNullOrWhiteSpace(principalType)
            && !string.IsNullOrWhiteSpace(principalId)
            && _principalMapper.TryGetPrincipalType(principalId, out var inferredType)
            && !string.IsNullOrWhiteSpace(inferredType))
        {
            principalType = inferredType;
        }

        var principalName = !string.IsNullOrWhiteSpace(principalId)
            ? _principalMapper.GetName(principalId, principalType, context.ResourceChange.Address) ?? principalId
            : string.Empty;

        if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(principalName))
        {
            return;
        }

        var roleAttributeName = !string.IsNullOrWhiteSpace(roleInfo.Name)
            ? RoleDefinitionNameAttribute
            : RoleDefinitionIdAttribute;
        var roleSummary = FormatAttributeValueTable(roleAttributeName, roleName, null);
        var roleSummaryHtml = FormatAttributeValueSummary(roleAttributeName, roleName, null);
        var principalSummary = FormatPrincipalSummary(principalType, principalName, isSummaryHtml: false);
        var principalSummaryHtml = FormatPrincipalSummary(principalType, principalName, isSummaryHtml: true);

        context.Model.Summary = $"Assign {roleSummary} to {principalSummary}";
        context.Model.SummaryHtml = BuildSummaryHtml(context.Model, roleSummaryHtml, principalSummaryHtml);
    }

    /// <summary>
    /// Resolves the state object to use for summary generation based on the action.
    /// </summary>
    /// <param name="resourceChange">The resource change data.</param>
    /// <param name="action">The normalized Terraform action.</param>
    /// <returns>The resolved state object.</returns>
    private static object? ResolveActiveState(ResourceChange resourceChange, string action)
    {
        var state = action == "delete" ? resourceChange.Change.Before : resourceChange.Change.After;
        return state ?? resourceChange.Change.After ?? resourceChange.Change.Before;
    }

    /// <summary>
    /// Builds the summary HTML string using the role and principal summaries.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="roleSummaryHtml">The formatted role summary HTML.</param>
    /// <param name="principalSummaryHtml">The formatted principal summary HTML.</param>
    /// <returns>Summary HTML string for the resource.</returns>
    private static string BuildSummaryHtml(ResourceChangeModel model, string roleSummaryHtml, string principalSummaryHtml)
    {
        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(model.Name)}</b>";
        return $"{prefix} — Assign {roleSummaryHtml} to {principalSummaryHtml}";
    }

    /// <summary>
    /// Formats the principal summary with a type-aware icon when available.
    /// </summary>
    /// <param name="principalType">The resolved principal type.</param>
    /// <param name="principalName">The resolved principal name.</param>
    /// <param name="isSummaryHtml">Whether to format for summary HTML output.</param>
    /// <returns>Formatted principal summary value.</returns>
    private static string FormatPrincipalSummary(string? principalType, string principalName, bool isSummaryHtml)
    {
        var icon = principalType switch
        {
            "User" => "👤",
            "Group" => "👥",
            "ServicePrincipal" => "💻",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(icon))
        {
            return isSummaryHtml ? FormatCodeSummary(principalName) : FormatCodeTable(principalName);
        }

        var iconValue = $"{icon} {principalName}";
        return isSummaryHtml ? FormatIconValueSummary(iconValue) : FormatIconValueTable(iconValue);
    }

    /// <summary>
    /// Gets a flattened state value by key.
    /// </summary>
    /// <param name="state">The flattened state dictionary.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value when present; otherwise null.</returns>
    private static string? GetValue(Dictionary<string, string?> state, string key)
    {
        return state.TryGetValue(key, out var value) ? value : null;
    }

}
