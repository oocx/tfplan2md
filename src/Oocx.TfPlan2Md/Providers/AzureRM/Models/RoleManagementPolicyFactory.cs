using System;
using System.Collections.Generic;
using System.Text;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Applies summary overrides for AzureRM role management policies.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </summary>
internal sealed class RoleManagementPolicyFactory : IResourceViewModelFactory
{
    /// <summary>
    /// Terraform resource type handled by this factory.
    /// </summary>
    private const string ResourceType = "azurerm_role_management_policy";

    /// <summary>
    /// Attribute name for scope values.
    /// </summary>
    private const string ScopeAttribute = "scope";

    /// <summary>
    /// Formatter used to enrich scope display names.
    /// </summary>
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

    /// <summary>
    /// Resolver used to format Azure role definition names for the current run.
    /// </summary>
    private readonly IRoleDefinitionResolver _roleDefinitionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleManagementPolicyFactory"/> class.
    /// </summary>
    /// <param name="scopeFormatter">Optional scope formatter for display name enrichment.</param>
    /// <param name="roleDefinitionResolver">Optional run-scoped resolver for role definition names.</param>
    internal RoleManagementPolicyFactory(
        EnrichedAzureScopeFormatter? scopeFormatter,
        IRoleDefinitionResolver? roleDefinitionResolver = null)
    {
        _scopeFormatter = scopeFormatter;
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

        var state = ResourceChangeHelpers.ResolveActiveState(context.ResourceChange, context.Action);
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);

        var roleInfo = _roleDefinitionResolver.GetRoleDefinition(
            JsonFlattener.GetValue(flatState, AzureRoleAssignmentAttributes.RoleDefinitionId),
            JsonFlattener.GetValue(flatState, AzureRoleAssignmentAttributes.RoleDefinitionName),
            context.ResourceChange.Address);
        var roleName = !string.IsNullOrWhiteSpace(roleInfo.Name)
            ? roleInfo.Name
            : roleInfo.Id;
        var roleSummary = FormatAttributeValueTable(AzureRoleAssignmentAttributes.RoleDefinitionName, roleName, null);
        var roleSummaryHtml = FormatAttributeValueSummary(AzureRoleAssignmentAttributes.RoleDefinitionName, roleName, null);

        var scopeValue = JsonFlattener.GetValue(flatState, ScopeAttribute);
        var scopeText = FormatScopeMarkdown(scopeValue, context.ResourceChange.Address);

        if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(scopeText))
        {
            return;
        }

        context.Model.Summary = $"{roleSummary} in {scopeText}";
        context.Model.SummaryHtml = BuildSummaryHtml(context.Model, roleSummaryHtml, scopeText);
    }

    /// <summary>
    /// Formats a scope string using enrichment when available.
    /// </summary>
    /// <param name="scope">The raw scope value.</param>
    /// <param name="resourceAddress">The Terraform resource address referencing the scope.</param>
    /// <returns>Markdown scope text with code spans.</returns>
    private string FormatScopeMarkdown(string? scope, string resourceAddress)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return string.Empty;
        }

        return _scopeFormatter is null
            ? AzureScopeParser.ParseScope(scope)
            : _scopeFormatter.FormatScope(scope, resourceAddress);
    }

    /// <summary>
    /// Builds the summary HTML string using the role and scope values.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="roleSummaryHtml">The resolved role summary HTML.</param>
    /// <param name="scopeMarkdown">The formatted scope text with markdown code spans.</param>
    /// <returns>Summary HTML string for the resource.</returns>
    private static string BuildSummaryHtml(ResourceChangeModel model, string roleSummaryHtml, string scopeMarkdown)
    {
        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(model.Name)}</b>";
        var scopeHtml = ConvertMarkdownCodeToSummaryHtml(scopeMarkdown);
        return $"{prefix} — {roleSummaryHtml} in {scopeHtml}";
    }

    /// <summary>
    /// Converts markdown code spans (backticks) to HTML code spans for summary rendering.
    /// </summary>
    /// <param name="markdown">The markdown string containing code spans.</param>
    /// <returns>HTML-safe string for summary output.</returns>
    private static string ConvertMarkdownCodeToSummaryHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var segments = markdown.Split('`');
        if (segments.Length == 1)
        {
            return markdown;
        }

        var builder = new StringBuilder(markdown.Length + 16);
        for (var index = 0; index < segments.Length; index++)
        {
            if (index % 2 == 1)
            {
                builder.Append(FormatCodeSummary(segments[index]));
            }
            else
            {
                builder.Append(segments[index]);
            }
        }

        return builder.ToString();
    }
}
