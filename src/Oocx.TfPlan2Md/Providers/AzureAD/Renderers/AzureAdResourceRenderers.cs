using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Renderers;

/// <summary>
/// Base class for AzureAD resource renderers that currently delegate to the default renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal abstract class AzureAdDelegatingRenderer(string resourceType) : IResourceRenderer
{
    /// <summary>
    /// Default fallback renderer.
    /// </summary>
    private readonly DefaultResourceRenderer _defaultRenderer = new();

    /// <inheritdoc />
    public string ResourceType { get; } = resourceType;

    /// <inheritdoc />
    public virtual void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        _defaultRenderer.Render(writer, change, context);
    }
}

/// <summary>
/// Renders <c>azuread_user</c> resources.
/// </summary>
internal sealed class UserRenderer : AzureAdDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRenderer"/> class.
    /// </summary>
    public UserRenderer()
        : base("azuread_user")
    {
    }
}

/// <summary>
/// Renders <c>azuread_group</c> resources.
/// </summary>
internal sealed class GroupRenderer : AzureAdDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupRenderer"/> class.
    /// </summary>
    public GroupRenderer()
        : base("azuread_group")
    {
    }
}

/// <summary>
/// Renders <c>azuread_group_without_members</c> resources.
/// </summary>
internal sealed class GroupWithoutMembersRenderer : AzureAdDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupWithoutMembersRenderer"/> class.
    /// </summary>
    public GroupWithoutMembersRenderer()
        : base("azuread_group_without_members")
    {
    }
}

/// <summary>
/// Renders <c>azuread_group_member</c> resources.
/// </summary>
internal sealed class GroupMemberRenderer : AzureAdDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupMemberRenderer"/> class.
    /// </summary>
    public GroupMemberRenderer()
        : base("azuread_group_member")
    {
    }
}

/// <summary>
/// Renders <c>azuread_service_principal</c> resources.
/// </summary>
internal sealed class ServicePrincipalRenderer : AzureAdDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServicePrincipalRenderer"/> class.
    /// </summary>
    public ServicePrincipalRenderer()
        : base("azuread_service_principal")
    {
    }
}

/// <summary>
/// Renders <c>azuread_invitation</c> resources.
/// </summary>
internal sealed class InvitationRenderer : AzureAdDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationRenderer"/> class.
    /// </summary>
    public InvitationRenderer()
        : base("azuread_invitation")
    {
    }
}
