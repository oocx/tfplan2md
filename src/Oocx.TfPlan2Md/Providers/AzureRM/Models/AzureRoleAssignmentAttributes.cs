namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Shared Terraform attribute-name constants for Azure role assignment resources.
/// Centralises the names used across <c>RoleManagementPolicyFactory</c>,
/// <c>PimEligibleRoleAssignmentFactory</c>, and <c>RoleAssignmentViewModelFactory</c>
/// to prevent typos and keep changes in one place.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </summary>
internal static class AzureRoleAssignmentAttributes
{
    /// <summary>Terraform attribute name for role definition IDs.</summary>
    internal const string RoleDefinitionId = "role_definition_id";

    /// <summary>Terraform attribute name for role definition names.</summary>
    internal const string RoleDefinitionName = "role_definition_name";

    /// <summary>Terraform attribute name for principal IDs.</summary>
    internal const string PrincipalId = "principal_id";

    /// <summary>Terraform attribute name for principal types.</summary>
    internal const string PrincipalType = "principal_type";
}
