namespace Oocx.TfPlan2Md.Azure;

internal sealed class NullPrincipalMapper : IPrincipalMapper
{
    public string GetPrincipalName(string principalId)
    {
        return principalId;
    }

    public string GetPrincipalName(string principalId, string? principalType)
    {
        return GetPrincipalName(principalId);
    }

    public string? GetName(string principalId)
    {
        return null;
    }

    public string? GetName(string principalId, string? principalType)
    {
        return GetName(principalId);
    }
}
