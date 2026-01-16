using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Azure;

/// <summary>
/// Represents a principal mapping file in the nested format with separate sections for users, groups, and service principals.
/// </summary>
/// <remarks>
/// This format organizes principals by type, making it easier to maintain and understand the mappings.
/// Each section maps principal IDs (GUIDs) to human-readable display names.
/// All sections are optional - if a section is omitted, it will be null.
/// Related issue: fix/principal-mapping-format
/// </remarks>
internal sealed class PrincipalMappingFile
{
    /// <summary>
    /// Maps user principal IDs (GUIDs) to display names (e.g., email addresses or full names).
    /// </summary>
    /// <example>
    /// "12345678-1234-1234-1234-123456789012": "jane.doe@contoso.com"
    /// </example>
    [JsonPropertyName("users")]
    public Dictionary<string, string>? Users { get; set; }

    /// <summary>
    /// Maps group principal IDs (GUIDs) to display names (e.g., group names).
    /// </summary>
    /// <example>
    /// "abcdef12-3456-7890-abcd-ef1234567890": "Platform Team"
    /// </example>
    [JsonPropertyName("groups")]
    public Dictionary<string, string>? Groups { get; set; }

    /// <summary>
    /// Maps service principal IDs (GUIDs) to display names (e.g., application names).
    /// </summary>
    /// <example>
    /// "11111111-2222-3333-4444-555555555555": "terraform-spn"
    /// </example>
    [JsonPropertyName("servicePrincipals")]
    public Dictionary<string, string>? ServicePrincipals { get; set; }
}
