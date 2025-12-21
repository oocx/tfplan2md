namespace Oocx.TfPlan2Md.Azure;

public enum ScopeLevel
{
    Unknown,
    Subscription,
    ResourceGroup,
    Resource,
    ManagementGroup
}

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
    public static readonly ScopeInfo Empty = new(string.Empty, string.Empty, string.Empty, string.Empty, ScopeLevel.Unknown, string.Empty, string.Empty, string.Empty, string.Empty);
}
