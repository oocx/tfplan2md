namespace Oocx.TfPlan2Md.Azure;

public interface IPrincipalMapper
{
    string GetPrincipalName(string principalId);

    /// <summary>
    /// Gets the principal name with optional type awareness.
    /// Default implementation delegates to the basic overload for backward compatibility.
    /// </summary>
    string GetPrincipalName(string principalId, string? principalType)
    {
        return GetPrincipalName(principalId);
    }

    string? GetName(string principalId);

    /// <summary>
    /// Gets the principal name with optional type awareness.
    /// Default implementation delegates to the basic overload for backward compatibility.
    /// </summary>
    string? GetName(string principalId, string? principalType)
    {
        return GetName(principalId);
    }
}
