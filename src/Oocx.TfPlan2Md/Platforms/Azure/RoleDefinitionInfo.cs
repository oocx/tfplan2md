namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Represents information about an Azure role definition.
/// Related feature: docs/features/025-azure-role-definition-mapping/specification.md.
/// </summary>
/// <param name="Name">The friendly name of the role (e.g., "Owner", "Contributor").</param>
/// <param name="Id">The unique identifier (GUID) of the role definition.</param>
/// <param name="FullName">The complete formatted name of the role.</param>
public record RoleDefinitionInfo(string Name, string Id, string FullName);
