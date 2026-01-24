namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Represents information about an Azure Active Directory principal (user, group, or service principal).
/// Related feature: docs/features/024-azure-principal-mapping/specification.md.
/// </summary>
/// <param name="Name">The display name of the principal.</param>
/// <param name="Id">The unique identifier (object ID) of the principal.</param>
/// <param name="Type">The type of principal (User, Group, ServicePrincipal, or Unknown).</param>
/// <param name="FullName">The complete formatted name including type information.</param>
public record PrincipalInfo(string Name, string Id, string Type, string FullName);
