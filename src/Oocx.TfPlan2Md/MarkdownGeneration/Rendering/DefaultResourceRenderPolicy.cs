namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Resolves scenario and compatibility policy for <see cref="DefaultResourceRenderer"/>.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal static class DefaultResourceRenderPolicy
{
    /// <summary>
    /// Resolves the render policy for a resource change.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <param name="context">Current render context.</param>
    /// <returns>The resolved render policy.</returns>
    internal static DefaultResourceRenderPolicyResult Resolve(ResourceChangeModel change, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(context);

        var scenarioContext = context as IScenarioRenderContext;
        var useOutputsFocusedFormatting = scenarioContext?.IsOutputsFocusedReport == true;
        var useKnownAfterApplyFormatting = (scenarioContext?.IsKnownAfterApplyScenario == true)
            || ShouldUseKnownAfterApplyFormatting(change);
        var isNoOpParentWithChildren = IsNoOpParentSecurityRuleScenario(change);
        var useExtraBlankLineBeforeSummary = ShouldUseExtraBlankLineBeforeSummary(
            change,
            true,
            useKnownAfterApplyFormatting);

        return new DefaultResourceRenderPolicyResult(
            isNoOpParentWithChildren,
            useOutputsFocusedFormatting,
            useKnownAfterApplyFormatting,
            true,
            useExtraBlankLineBeforeSummary);
    }

    /// <summary>
    /// Determines whether an extra blank line should precede the summary element.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <param name="useMultilineDetailsSummary">Whether multiline formatting is active.</param>
    /// <param name="useKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
    /// <returns>True when an extra blank line should be rendered.</returns>
    private static bool ShouldUseExtraBlankLineBeforeSummary(
        ResourceChangeModel change,
        bool useMultilineDetailsSummary,
        bool useKnownAfterApplyFormatting)
    {
        if (change.Type.StartsWith("azuread_", StringComparison.Ordinal))
        {
            return true;
        }

        return useKnownAfterApplyFormatting
            && useMultilineDetailsSummary
            && IsKnownAfterApplyAzureAdMemberScenario(change);
    }

    /// <summary>
    /// Determines whether the resource matches the known-after-apply Azure AD member compatibility scenario.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when the scenario matches.</returns>
    private static bool IsKnownAfterApplyAzureAdMemberScenario(ResourceChangeModel change)
    {
        if (!string.Equals(change.Type, "azuread_group_member", StringComparison.Ordinal)
            || !string.Equals(change.Action, "create", StringComparison.Ordinal)
            || change.AttributeChanges.Count == 0)
        {
            return false;
        }

        return change.AttributeChanges.Any(attribute =>
            ContainsKnownAfterApplyMarker(attribute.Before)
            || ContainsKnownAfterApplyMarker(attribute.After));
    }

    /// <summary>
    /// Determines whether known-after-apply formatting should be enabled for a resource.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when known-after-apply formatting should be used.</returns>
    private static bool ShouldUseKnownAfterApplyFormatting(ResourceChangeModel change)
    {
        var hasKnownAfterApplyMarker = change.AttributeChanges.Any(attribute =>
            ContainsKnownAfterApplyMarker(attribute.Before)
            || ContainsKnownAfterApplyMarker(attribute.After));

        if (!hasKnownAfterApplyMarker)
        {
            return false;
        }

        if (string.Equals(change.Type, "azuread_group_member", StringComparison.Ordinal))
        {
            if (change.ConfigurationReferences.Count > 0)
            {
                return true;
            }

            return change.AttributeChanges.All(attribute =>
                ContainsKnownAfterApplyMarker(attribute.Before)
                || ContainsKnownAfterApplyMarker(attribute.After));
        }

        return string.IsNullOrWhiteSpace(change.ModuleAddress)
            && (change.Type.StartsWith("azurerm_", StringComparison.Ordinal)
                || string.Equals(change.Type, "null_resource", StringComparison.Ordinal));
    }

    /// <summary>
    /// Determines whether a resource represents the no-op parent NSG scenario with separate security-rule children.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>True when the scenario matches.</returns>
    private static bool IsNoOpParentSecurityRuleScenario(ResourceChangeModel change)
    {
        if (change.ChildResourceGroups.Count == 0 || change.AttributeChanges.Count > 0)
        {
            return false;
        }

        var securityRuleGroup = change.ChildResourceGroups.FirstOrDefault(group =>
            string.Equals(group.Label, "Security Rules", StringComparison.Ordinal));

        return securityRuleGroup?.Rows.Count == 2;
    }

    /// <summary>
    /// Determines whether an attribute value contains Terraform's known-after-apply marker.
    /// </summary>
    /// <param name="value">Attribute value.</param>
    /// <returns>True when the marker is present.</returns>
    private static bool ContainsKnownAfterApplyMarker(string? value)
    {
        return value?.Contains("known after apply", StringComparison.OrdinalIgnoreCase) == true;
    }
}

/// <summary>
/// Captures resolved policy flags for the default resource renderer.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="IsNoOpParentWithChildren">Whether the resource is a no-op parent with changed children.</param>
/// <param name="UseOutputsFocusedFormatting">Whether outputs-focused formatting is enabled.</param>
/// <param name="UseKnownAfterApplyFormatting">Whether known-after-apply formatting is enabled.</param>
/// <param name="UseMultilineDetailsSummary">Whether details and summary should be emitted on separate lines.</param>
/// <param name="UseExtraBlankLineBeforeSummary">Whether an extra blank line should precede the summary element.</param>
internal sealed record DefaultResourceRenderPolicyResult(
    bool IsNoOpParentWithChildren,
    bool UseOutputsFocusedFormatting,
    bool UseKnownAfterApplyFormatting,
    bool UseMultilineDetailsSummary,
    bool UseExtraBlankLineBeforeSummary);
