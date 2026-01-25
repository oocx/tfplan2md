namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Interface for mapping Azure AD/Entra principal IDs to display names.
/// </summary>
public interface IPrincipalMapper
{
    /// <summary>
    /// Gets the display name for a principal ID.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <returns>Display name with principal ID, or just the principal ID if not found.</returns>
    string GetPrincipalName(string principalId);

    /// <summary>
    /// Gets the principal name with optional type awareness and diagnostic context.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">The type of principal (optional, may not be used by implementations).</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>Display name with principal ID, or just the principal ID if not found.</returns>
    string GetPrincipalName(string principalId, string? principalType, string? resourceAddress = null)
    {
        return GetPrincipalName(principalId);
    }

    /// <summary>
    /// Gets only the display name (without ID) for a principal ID.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <returns>The display name if found, otherwise null.</returns>
    string? GetName(string principalId);

    /// <summary>
    /// Gets the principal name with optional type awareness.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">The type of principal (User, Group, ServicePrincipal) for type-aware resolution.</param>
    /// <returns>The display name if found, otherwise null.</returns>
    string? GetName(string principalId, string? principalType)
    {
        return GetName(principalId);
    }

    /// <summary>
    /// Gets the principal name with optional type awareness and diagnostic context.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">The type of principal (optional, may not be used by implementations).</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>The display name if found, otherwise null.</returns>
    string? GetName(string principalId, string? principalType, string? resourceAddress)
    {
        return GetName(principalId, principalType);
    }

    /// <summary>
    /// Attempts to resolve the principal type for a principal ID from mapping metadata.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">The resolved principal type when available.</param>
    /// <returns><c>true</c> when a type is available; otherwise <c>false</c>.</returns>
    bool TryGetPrincipalType(string principalId, out string? principalType)
    {
        principalType = null;
        return false;
    }
}
