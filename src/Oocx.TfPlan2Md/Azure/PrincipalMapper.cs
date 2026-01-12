using System.Collections.Frozen;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;

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
        var name = GetName(principalId);

        if (string.IsNullOrWhiteSpace(principalId))
        {
            return principalId ?? string.Empty;
        }

        return name is null
            ? principalId
            : $"{name} [{principalId}]";
    }

    public string? GetName(string principalId)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return null;
        }

        return _principals.TryGetValue(principalId, out var name)
            ? name
            : null;
    }

    public string GetPrincipalName(string principalId, string? principalType)
    {
        return GetPrincipalName(principalId);
    }

    public string? GetName(string principalId, string? principalType)
    {
        return GetName(principalId);
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
            var parsed = JsonSerializer.Deserialize(content, TfPlanJsonContext.Default.DictionaryStringString);
            if (parsed is null)
            {
                return FrozenDictionary<string, string>.Empty;
            }

            return parsed.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // Intentional swallow after logging: malformed or unreadable mapping files should gracefully
            // fall back to raw principal IDs instead of failing plan generation, but the user should know why.
            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
    }
}
