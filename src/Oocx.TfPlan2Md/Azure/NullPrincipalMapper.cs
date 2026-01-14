namespace Oocx.TfPlan2Md.Azure;

/// <summary>
/// A null implementation of <see cref="IPrincipalMapper"/> that returns principal IDs unchanged.
/// </summary>
/// <remarks>
/// This implementation is used when no principal mapping file is provided. It simply returns
/// the principal ID without attempting any name resolution.
/// </remarks>
internal sealed class NullPrincipalMapper : IPrincipalMapper
{
    /// <summary>
    /// Returns the principal ID unchanged.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <returns>The principal ID unchanged.</returns>
    public string GetPrincipalName(string principalId)
    {
        return principalId;
    }

    /// <summary>
    /// Returns the principal ID unchanged with type and resource context (ignored).
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">Principal type (ignored by null mapper).</param>
    /// <param name="resourceAddress">Resource address (ignored by null mapper).</param>
    /// <returns>The principal ID unchanged.</returns>
    public string GetPrincipalName(string principalId, string? principalType, string? resourceAddress = null)
    {
        return principalId;
    }

    /// <summary>
    /// Always returns null since no mapping is available.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <returns>Always null.</returns>
    public string? GetName(string principalId)
    {
        return null;
    }

    /// <summary>
    /// Always returns null since no mapping is available with type and resource context (ignored).
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">Principal type (ignored by null mapper).</param>
    /// <param name="resourceAddress">Resource address (ignored by null mapper).</param>
    /// <returns>Always null.</returns>
    public string? GetName(string principalId, string? principalType, string? resourceAddress = null)
    {
        return null;
    }
}
