using System.Collections.Frozen;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Azure;

public class PrincipalMapper : IPrincipalMapper
{
    private readonly FrozenDictionary<string, string> _principals;

    public PrincipalMapper(string? mappingFile)
    {
        _principals = LoadMappings(mappingFile);
    }

    public string GetPrincipalName(string principalId)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return principalId ?? string.Empty;
        }

        if (_principals.TryGetValue(principalId, out var name))
        {
            return $"{name} [{principalId}]";
        }

        return principalId;
    }

    private static FrozenDictionary<string, string> LoadMappings(string? mappingFile)
    {
        if (string.IsNullOrWhiteSpace(mappingFile) || !File.Exists(mappingFile))
        {
            return FrozenDictionary<string, string>.Empty;
        }

        try
        {
            var content = File.ReadAllText(mappingFile);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
            if (parsed is null)
            {
                return FrozenDictionary<string, string>.Empty;
            }

            return parsed.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return FrozenDictionary<string, string>.Empty;
        }
    }
}
