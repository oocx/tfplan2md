using Oocx.TfPlan2Md.Azure;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Azure-specific helpers exposed to Scriban templates.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Builds scope metadata for templates from a raw scope string.
    /// </summary>
    /// <param name="scope">Raw Azure scope string.</param>
    /// <returns>ScriptObject containing parsed scope information.</returns>
    private static ScriptObject GetScopeInfo(string? scope)
    {
        var info = AzureScopeParser.Parse(scope);

        return new ScriptObject
        {
            ["name"] = info.Name,
            ["type"] = info.Type,
            ["subscription_id"] = info.SubscriptionId ?? string.Empty,
            ["resource_group"] = info.ResourceGroup ?? string.Empty,
            ["level"] = info.Level.ToString(),
            ["summary"] = info.Summary,
            ["summary_label"] = info.SummaryLabel,
            ["summary_name"] = info.SummaryName,
            ["details"] = info.Details
        };
    }

    /// <summary>
    /// Builds role definition metadata for templates.
    /// </summary>
    /// <param name="roleDefinitionId">Role definition identifier.</param>
    /// <param name="roleDefinitionName">Role definition name.</param>
    /// <returns>ScriptObject describing the role.</returns>
    private static ScriptObject GetRoleInfo(string? roleDefinitionId, string? roleDefinitionName)
    {
        var info = AzureRoleDefinitionMapper.GetRoleDefinition(roleDefinitionId, roleDefinitionName);

        return new ScriptObject
        {
            ["name"] = info.Name,
            ["id"] = info.Id,
            ["full_name"] = info.FullName
        };
    }

    /// <summary>
    /// Resolves principal metadata including friendly name and full display name.
    /// </summary>
    /// <param name="principalId">Principal identifier.</param>
    /// <param name="principalType">Principal type.</param>
    /// <param name="principalMapper">Mapper used to resolve names.</param>
    /// <returns>ScriptObject describing the principal.</returns>
    private static ScriptObject GetPrincipalInfo(string? principalId, string? principalType, IPrincipalMapper principalMapper)
    {
        var id = principalId ?? string.Empty;
        var type = principalType ?? string.Empty;
        var name = principalMapper.GetName(id, type) ?? id;
        var fullName = BuildPrincipalFullName(name, id, type);

        return new ScriptObject
        {
            ["name"] = name,
            ["id"] = id,
            ["type"] = type,
            ["full_name"] = fullName
        };
    }

    /// <summary>
    /// Resolves a principal name from an identifier using the configured mapper.
    /// </summary>
    /// <param name="principalId">Principal identifier.</param>
    /// <param name="principalMapper">Mapper used for resolution.</param>
    /// <returns>Resolved principal name or empty string.</returns>
    private static string ResolvePrincipalName(string? principalId, IPrincipalMapper principalMapper)
    {
        if (principalId is null)
        {
            return string.Empty;
        }

        return principalMapper.GetPrincipalName(principalId);
    }

    /// <summary>
    /// Constructs a readable principal display name including type and identifier when available.
    /// </summary>
    /// <param name="name">Friendly name.</param>
    /// <param name="principalId">Identifier.</param>
    /// <param name="principalType">Type.</param>
    /// <returns>Full display name string.</returns>
    private static string BuildPrincipalFullName(string name, string? principalId, string? principalType)
    {
        var typePart = string.IsNullOrWhiteSpace(principalType) ? string.Empty : $" ({principalType})";
        var idPart = string.IsNullOrWhiteSpace(principalId) ? string.Empty : $" [{principalId}]";
        return $"{name}{typePart}{idPart}".Trim();
    }
}
