namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Enumerates the types of failed mapping resolutions captured in diagnostics.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </summary>
internal enum FailedResolutionType
{
    /// <summary>
    /// A principal ID could not be resolved.
    /// </summary>
    Principal,

    /// <summary>
    /// A subscription ID could not be resolved.
    /// </summary>
    Subscription,

    /// <summary>
    /// A management group ID could not be resolved.
    /// </summary>
    ManagementGroup,

    /// <summary>
    /// A tenant ID could not be resolved.
    /// </summary>
    Tenant,

    /// <summary>
    /// A role definition ID could not be resolved.
    /// </summary>
    RoleDefinition
}
