namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Enumerates the types of failed mapping resolutions captured in diagnostics.
/// </summary>
/// <remarks>
/// Related features:
/// <list type="bullet">
/// <item><description>docs/features/063-azure-display-enhancements/specification.md.</description></item>
/// <item><description>docs/features/085-azdo-principal-mapping/specification.md.</description></item>
/// <item><description>docs/features/095-azdo-repo-mapping-and-icons/specification.md.</description></item>
/// </list>
/// </remarks>
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
    RoleDefinition,

    /// <summary>
    /// An Azure DevOps user ID could not be resolved.
    /// </summary>
    AzdoUser,

    /// <summary>
    /// An Azure DevOps group descriptor could not be resolved.
    /// </summary>
    AzdoGroup,

    /// <summary>
    /// An Azure DevOps project ID could not be resolved.
    /// </summary>
    AzdoProject,

    /// <summary>
    /// An Azure DevOps repository ID could not be resolved.
    /// Related feature: docs/features/095-azdo-repo-mapping-and-icons/specification.md.
    /// </summary>
    AzdoRepository
}
