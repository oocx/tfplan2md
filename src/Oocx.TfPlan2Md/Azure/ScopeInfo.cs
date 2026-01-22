namespace Oocx.TfPlan2Md.Azure;

/// <summary>
/// Defines the hierarchical level of an Azure scope.
/// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
/// </summary>
public enum ScopeLevel
{
    /// <summary>
    /// The scope level could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// The scope is at the subscription level.
    /// </summary>
    Subscription,

    /// <summary>
    /// The scope is at the resource group level.
    /// </summary>
    ResourceGroup,

    /// <summary>
    /// The scope is at the individual resource level.
    /// </summary>
    Resource,

    /// <summary>
    /// The scope is at the management group level.
    /// </summary>
    ManagementGroup
}

/// <summary>
/// Represents structured information about an Azure resource scope.
/// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
/// </summary>
/// <param name="Name">The name of the resource or scope.</param>
/// <param name="Type">The type of resource (e.g., "Key Vault", "Storage Account").</param>
/// <param name="SubscriptionId">The subscription ID, if applicable.</param>
/// <param name="ResourceGroup">The resource group name, if applicable.</param>
/// <param name="Level">The hierarchical level of this scope.</param>
/// <param name="Summary">A brief summary of the scope.</param>
/// <param name="SummaryLabel">The label prefix for the summary.</param>
/// <param name="SummaryName">The name portion of the summary.</param>
/// <param name="Details">The complete detailed description of the scope.</param>
public record ScopeInfo(
    string Name,
    string Type,
    string? SubscriptionId,
    string? ResourceGroup,
    ScopeLevel Level,
    string Summary,
    string SummaryLabel,
    string SummaryName,
    string Details)
{
    /// <summary>
    /// Gets an empty ScopeInfo instance representing an unresolved or invalid scope.
    /// </summary>
    public static readonly ScopeInfo Empty = new(string.Empty, string.Empty, string.Empty, string.Empty, ScopeLevel.Unknown, string.Empty, string.Empty, string.Empty, string.Empty);
}
