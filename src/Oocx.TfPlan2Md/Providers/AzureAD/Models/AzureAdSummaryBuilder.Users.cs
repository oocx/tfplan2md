using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// User-focused summary builders for Azure AD resources.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal static partial class AzureAdSummaryBuilder
{
    /// <summary>
    /// Builds summary HTML for Azure AD user resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildUserSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IconProviderRegistry? iconProviderRegistry)
    {
        var displayName = JsonStateReader.GetStringProperty(state, "display_name");
        var upn = JsonStateReader.GetStringProperty(state, "user_principal_name");
        var mail = JsonStateReader.GetStringProperty(state, "mail");

        var summaryText = string.Empty;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            summaryText = FormatSummaryValue(model, "display_name", displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(upn))
        {
            var upnText = FormatSummaryValue(model, "user_principal_name", upn, iconProviderRegistry);
            upnText = $"({upnText})";
            summaryText = string.IsNullOrEmpty(summaryText) ? upnText : $"{summaryText} {upnText}";
        }

        if (!string.IsNullOrWhiteSpace(mail))
        {
            var mailText = FormatSummaryValue(model, "mail", mail, iconProviderRegistry);
            summaryText = string.IsNullOrEmpty(summaryText) ? mailText : $"{summaryText} {mailText}";
        }

        if (string.IsNullOrEmpty(summaryText))
        {
            summaryText = FormatCodeSummary(model.Name);
        }

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD service principal resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildServicePrincipalSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IconProviderRegistry? iconProviderRegistry)
    {
        var displayName = JsonStateReader.GetStringProperty(state, "display_name");
        var appId = JsonStateReader.GetStringProperty(state, "application_id");
        var description = JsonStateReader.GetStringProperty(state, "description");

        var summaryText = string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            summaryText = FormatSummaryValue(model, "display_name", displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(appId))
        {
            var appText = FormatSummaryValue(model, "application_id", appId, iconProviderRegistry);
            appText = $"({appText})";
            summaryText = string.IsNullOrEmpty(summaryText) ? appText : $"{summaryText} {appText}";
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            summaryText = string.IsNullOrEmpty(summaryText)
                ? EscapeMarkdown(description)
                : $"{summaryText} {EscapeMarkdown(description)}";
        }

        if (string.IsNullOrEmpty(summaryText))
        {
            summaryText = FormatCodeSummary(model.Name);
        }

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD invitation resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildInvitationSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IconProviderRegistry? iconProviderRegistry)
    {
        var email = JsonStateReader.GetStringProperty(state, "user_email_address");
        var userType = JsonStateReader.GetStringProperty(state, "user_type");

        var summaryText = string.Empty;
        if (!string.IsNullOrWhiteSpace(email))
        {
            summaryText = FormatSummaryValue(model, "user_email_address", email, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(userType))
        {
            var typeText = $"({FormatCodeSummary(userType)})";
            summaryText = string.IsNullOrEmpty(summaryText) ? typeText : $"{summaryText} {typeText}";
        }

        if (string.IsNullOrEmpty(summaryText))
        {
            summaryText = FormatCodeSummary(model.Name);
        }

        return BuildSummaryHtml(model, summaryText);
    }
}
