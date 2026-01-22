namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Represents a principal ID that failed to resolve during principal mapping.
/// Related feature: docs/features/038-debug-output/
.
/// </summary>
/// <param name="PrincipalId">The GUID of the principal that could not be resolved to a display name.</param>
/// <param name="ResourceAddress">The Terraform resource address that referenced this principal ID.</param>
/// <remarks>
/// This record captures context about failed principal lookups to help users diagnose
/// missing entries in their principal mapping file. The resource address indicates where
/// the failed lookup occurred, allowing users to understand the impact.
/// </remarks>
internal record FailedPrincipalResolution(string PrincipalId, string ResourceAddress);
