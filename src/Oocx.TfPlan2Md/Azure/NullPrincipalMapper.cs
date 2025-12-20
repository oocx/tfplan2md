namespace Oocx.TfPlan2Md.Azure;

internal sealed class NullPrincipalMapper : IPrincipalMapper
{
    public string GetPrincipalName(string principalId)
    {
        return principalId;
    }
}
