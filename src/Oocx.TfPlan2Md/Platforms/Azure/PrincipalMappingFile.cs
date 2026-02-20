using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Represents a principal mapping file in the nested format with separate sections for users, groups, and service principals.
/// </summary>
/// <remarks>
/// This format organizes principals by type, making it easier to maintain and understand the mappings.
/// Each section maps principal IDs (GUIDs) to human-readable display names.
/// All sections are optional - if a section is omitted, it will be null.
/// Related issue: fix/principal-mapping-format.
/// </remarks>
internal sealed class PrincipalMappingFile
{
    /// <summary>
    /// Gets or sets the mapping of user principal IDs (GUIDs) to display names (e.g., email addresses or full names).
    /// </summary>
    /// <example>
    /// "12345678-1234-1234-1234-123456789012": "jane.doe@contoso.com".
    /// </example>
    [JsonPropertyName("users")]
    public Dictionary<string, string>? Users { get; set; }

    /// <summary>
    /// Gets or sets the mapping of group principal IDs (GUIDs) to display names (e.g., group names).
    /// </summary>
    /// <example>
    /// "abcdef12-3456-7890-abcd-ef1234567890": "Platform Team".
    /// </example>
    [JsonPropertyName("groups")]
    public Dictionary<string, string>? Groups { get; set; }

    /// <summary>
    /// Gets or sets the mapping of service principal IDs (GUIDs) to display names (e.g., application names).
    /// </summary>
    /// <example>
    /// "11111111-2222-3333-4444-555555555555": "terraform-spn".
    /// </example>
    [JsonPropertyName("servicePrincipals")]
    public Dictionary<string, string>? ServicePrincipals { get; set; }

    /// <summary>
    /// Gets or sets the list of subscription ID mappings with display names.
    /// </summary>
    /// <remarks>
    /// Each entry uses the array-of-objects format required by feature 063.
    /// Related feature: docs/features/063-azure-display-enhancements/specification.md.
    /// </remarks>
    [JsonPropertyName("subscriptions")]
    public List<MappingEntry>? Subscriptions { get; set; }

    /// <summary>
    /// Gets or sets the list of management group ID mappings with display names.
    /// </summary>
    /// <remarks>
    /// Each entry uses the array-of-objects format required by feature 063.
    /// Related feature: docs/features/063-azure-display-enhancements/specification.md.
    /// </remarks>
    [JsonPropertyName("managementGroups")]
    public List<MappingEntry>? ManagementGroups { get; set; }

    /// <summary>
    /// Gets or sets the list of tenant ID mappings with display names.
    /// </summary>
    /// <remarks>
    /// Each entry uses the array-of-objects format required by feature 063.
    /// Related feature: docs/features/063-azure-display-enhancements/specification.md.
    /// </remarks>
    [JsonPropertyName("tenants")]
    public List<MappingEntry>? Tenants { get; set; }

    /// <summary>
    /// Gets or sets the list of custom role definition ID mappings with display names.
    /// </summary>
    /// <remarks>
    /// Each entry uses the array-of-objects format required by feature 063.
    /// Related feature: docs/features/063-azure-display-enhancements/specification.md.
    /// </remarks>
    [JsonPropertyName("roles")]
    public List<MappingEntry>? Roles { get; set; }

    /// <summary>
    /// Gets or sets the mapping of Azure DevOps user IDs (GUIDs) to display names.
    /// </summary>
    /// <remarks>
    /// Azure DevOps users are identified by unique GUIDs. This mapping allows
    /// displaying recognizable names in rendered Terraform plans.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    /// <example>
    /// "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith".
    /// </example>
    [JsonPropertyName("azdoUsers")]
    public Dictionary<string, string>? AzdoUsers { get; set; }

    /// <summary>
    /// Gets or sets the mapping of Azure DevOps group descriptors to display names.
    /// </summary>
    /// <remarks>
    /// Azure DevOps groups are identified by base64-encoded descriptors (e.g., "vssgp.Uy0xLTktMTU1MTM...").
    /// These descriptors can be very long (100+ characters). This mapping allows
    /// displaying recognizable team/group names in rendered Terraform plans.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    /// <example>
    /// "vssgp.Uy0xLTktMTU1MTM7NDI0NS0yNzY5MzQwNjk3...": "Platform Team".
    /// </example>
    [JsonPropertyName("azdoGroups")]
    public Dictionary<string, string>? AzdoGroups { get; set; }

    /// <summary>
    /// Gets or sets the mapping of Azure DevOps project IDs (GUIDs) to display names.
    /// </summary>
    /// <remarks>
    /// Azure DevOps projects are identified by unique GUIDs. This mapping allows
    /// displaying recognizable project names in rendered Terraform plans.
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    /// <example>
    /// "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project".
    /// </example>
    [JsonPropertyName("azdoProjects")]
    public Dictionary<string, string>? AzdoProjects { get; set; }

    /// <summary>
    /// Gets or sets the mapping of Azure DevOps repository IDs (GUIDs) to display names.
    /// </summary>
    /// <remarks>
    /// Azure DevOps repositories are identified by unique GUIDs. This mapping allows
    /// displaying recognizable repository names in rendered Terraform plans.
    /// Related feature: docs/features/095-azdo-repo-mapping-and-icons/specification.md.
    /// </remarks>
    /// <example>
    /// "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo".
    /// </example>
    [JsonPropertyName("azdoRepositories")]
    public Dictionary<string, string>? AzdoRepositories { get; set; }
}
