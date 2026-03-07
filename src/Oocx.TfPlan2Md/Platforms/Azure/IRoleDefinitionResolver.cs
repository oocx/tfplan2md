namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Azure role definition identifiers into display-ready role information.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal interface IRoleDefinitionResolver
{
    /// <summary>
    /// Resolves a role definition identifier into the formatted display name.
    /// </summary>
    /// <param name="roleDefinitionId">The role definition identifier to resolve.</param>
    /// <returns>The full formatted role name.</returns>
    string GetRoleName(string? roleDefinitionId);

    /// <summary>
    /// Resolves role definition details from an identifier and optional fallback name.
    /// </summary>
    /// <param name="roleDefinitionId">The role definition ID to look up.</param>
    /// <param name="roleDefinitionName">Optional fallback role name if lookup fails.</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>The resolved role definition information.</returns>
    RoleDefinitionInfo GetRoleDefinition(
        string? roleDefinitionId,
        string? roleDefinitionName,
        string? resourceAddress = null);
}
