using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Resolves template paths for Terraform resource types and exposes them to Scriban.
/// </summary>
internal sealed class TemplateResolver
{
    private readonly ScribanTemplateLoader _templateLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateResolver"/> class.
    /// </summary>
    /// <param name="templateLoader">The template loader used to check template availability.</param>
    public TemplateResolver(ScribanTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    /// <summary>
    /// Registers the <c>resolve_template</c> helper function with the provided script object.
    /// </summary>
    /// <param name="scriptObject">The script object receiving the helper.</param>
    public void Register(ScriptObject scriptObject)
    {
        scriptObject.Import("resolve_template", new Func<string?, string>(ResolveTemplate));
    }

    /// <summary>
    /// Resolves the template path for a given Terraform resource type.
    /// </summary>
    /// <param name="resourceType">Terraform resource type (e.g., <c>azurerm_network_security_group</c>).</param>
    /// <returns>Resolved template path (e.g., <c>azurerm/network_security_group</c>) or <c>_resource</c> when no match exists.</returns>
    public string ResolveTemplate(string? resourceType)
    {
        var (provider, resource) = ResourceTypeParser.Parse(resourceType ?? string.Empty);
        if (provider is null || resource is null)
        {
            return "_resource";
        }

        var candidate = $"{provider}/{resource}";
        return _templateLoader.TemplateExists(candidate) ? candidate : "_resource";
    }
}
