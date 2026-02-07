namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Represents a failed attempt to resolve an ID to a display name during report generation.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </summary>
/// <param name="Type">The entity type that failed to resolve.</param>
/// <param name="Id">The identifier that could not be resolved.</param>
/// <param name="ResourceAddress">The Terraform resource address that referenced the ID.</param>
/// <param name="Reason">The reason the lookup failed.</param>
internal record FailedResolution(
    FailedResolutionType Type,
    string Id,
    string ResourceAddress,
    string Reason);
