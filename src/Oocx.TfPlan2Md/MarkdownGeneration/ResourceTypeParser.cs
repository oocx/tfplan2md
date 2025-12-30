namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Parses Terraform resource type identifiers into provider and resource components.
/// </summary>
internal static class ResourceTypeParser
{
    /// <summary>
    /// Parses a Terraform resource type into provider and resource name.
    /// </summary>
    /// <param name="resourceType">Resource type (e.g., <c>azurerm_network_security_group</c>).</param>
    /// <returns>Tuple of provider and resource name, or (null, null) when parsing fails.</returns>
    public static (string? Provider, string? Resource) Parse(string resourceType)
    {
        if (string.IsNullOrEmpty(resourceType))
        {
            return (null, null);
        }

        var underscoreIndex = resourceType.IndexOf('_');
        if (underscoreIndex <= 0 || underscoreIndex >= resourceType.Length - 1)
        {
            return (null, null);
        }

        var provider = resourceType[..underscoreIndex];
        var resource = resourceType[(underscoreIndex + 1)..];
        return (provider, resource);
    }
}
