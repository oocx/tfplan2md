using System.Reflection;
using System.Text;
using System.Text.Json;
using Scriban;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Renders Terraform plan reports to Markdown using Scriban templates.
/// </summary>
public class MarkdownRenderer
{
    private const string DefaultTemplateResourceName = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.default.sbn";
    private const string TemplateResourcePrefix = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.";

    private readonly string? _customTemplateDirectory;

    /// <summary>
    /// Creates a new MarkdownRenderer using embedded templates.
    /// </summary>
    public MarkdownRenderer()
    {
    }

    /// <summary>
    /// Creates a new MarkdownRenderer with a custom template directory.
    /// </summary>
    /// <param name="customTemplateDirectory">Path to custom template directory for resource-specific template overrides.</param>
    public MarkdownRenderer(string customTemplateDirectory)
    {
        _customTemplateDirectory = customTemplateDirectory;
    }

    /// <summary>
    /// Renders a report model to Markdown using the default embedded template.
    /// </summary>
    /// <param name="model">The report model to render.</param>
    /// <returns>The rendered Markdown string.</returns>
    public string Render(ReportModel model)
    {
        var templateText = LoadDefaultTemplate();
        return RenderWithTemplate(model, templateText);
    }

    /// <summary>
    /// Renders a report model to Markdown using a custom template file.
    /// </summary>
    /// <param name="model">The report model to render.</param>
    /// <param name="templatePath">Path to the custom template file.</param>
    /// <returns>The rendered Markdown string.</returns>
    public string Render(ReportModel model, string templatePath)
    {
        var templateText = File.ReadAllText(templatePath);
        return RenderWithTemplate(model, templateText);
    }

    /// <summary>
    /// Renders a report model to Markdown using a custom template file asynchronously.
    /// </summary>
    /// <param name="model">The report model to render.</param>
    /// <param name="templatePath">Path to the custom template file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The rendered Markdown string.</returns>
    public async Task<string> RenderAsync(ReportModel model, string templatePath, CancellationToken cancellationToken = default)
    {
        var templateText = await File.ReadAllTextAsync(templatePath, cancellationToken);
        return RenderWithTemplate(model, templateText);
    }

    /// <summary>
    /// Renders a single resource change using the appropriate resource-specific template.
    /// Falls back to the default template rendering if no specific template exists.
    /// </summary>
    /// <param name="change">The resource change to render.</param>
    /// <returns>The rendered Markdown string for this resource, or null if default handling should be used.</returns>
    public string? RenderResourceChange(ResourceChangeModel change)
    {
        var templateText = ResolveResourceTemplate(change.Type);
        if (templateText is null)
        {
            return null; // Use default template handling
        }

        try
        {
            return RenderResourceWithTemplate(change, templateText);
        }
        catch (ScribanHelperException ex)
        {
            // Return error message for this resource but allow other resources to render
            return $"### {change.ActionSymbol} {change.Address}\n\n⚠️ **Template Error:** {ex.Message}\n";
        }
    }

    /// <summary>
    /// Resolves a template for the given resource type.
    /// Resolution order: custom directory (if set) → embedded resources.
    /// Within each: Templates/{provider}/{resource}.sbn → Templates/default.sbn
    /// </summary>
    /// <param name="resourceType">The Terraform resource type (e.g., "azurerm_firewall_network_rule_collection").</param>
    /// <returns>The template text if a resource-specific template exists, null otherwise.</returns>
    private string? ResolveResourceTemplate(string resourceType)
    {
        var (provider, resource) = ParseResourceType(resourceType);
        if (provider is null || resource is null)
        {
            return null;
        }

        // Try custom template directory first
        if (_customTemplateDirectory is not null)
        {
            var customPath = Path.Combine(_customTemplateDirectory, provider, $"{resource}.sbn");
            if (File.Exists(customPath))
            {
                return File.ReadAllText(customPath);
            }
        }

        // Try embedded resource-specific template
        var embeddedResourceName = $"{TemplateResourcePrefix}{provider}.{resource}.sbn";
        var templateText = LoadEmbeddedTemplate(embeddedResourceName);
        return templateText;
    }

    /// <summary>
    /// Parses a Terraform resource type into provider and resource name.
    /// </summary>
    /// <param name="resourceType">The resource type (e.g., "azurerm_firewall_network_rule_collection").</param>
    /// <returns>Tuple of (provider, resource) or (null, null) if parsing fails.</returns>
    private static (string? Provider, string? Resource) ParseResourceType(string resourceType)
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

    private static string? LoadEmbeddedTemplate(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string RenderResourceWithTemplate(ResourceChangeModel change, string templateText)
    {
        var template = Template.Parse(templateText);
        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages);
            throw new MarkdownRenderException($"Template parsing failed: {errors}");
        }

        var scriptObject = new ScriptObject();
        scriptObject.Import(change, renamer: member => ToSnakeCase(member.Name));

        // Convert JsonElement properties to ScriptObjects for proper Scriban navigation
        if (change.BeforeJson is JsonElement beforeElement)
        {
            scriptObject["before_json"] = ScribanHelpers.ConvertToScriptObject(beforeElement);
        }
        if (change.AfterJson is JsonElement afterElement)
        {
            scriptObject["after_json"] = ScribanHelpers.ConvertToScriptObject(afterElement);
        }

        // Register custom helper functions
        ScribanHelpers.RegisterHelpers(scriptObject);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        context.MemberRenamer = member => ToSnakeCase(member.Name);

        try
        {
            return template.Render(context);
        }
        catch (Scriban.Syntax.ScriptRuntimeException ex)
        {
            throw new MarkdownRenderException($"Error rendering template: {ex.Message}", ex);
        }
    }

    private static string RenderWithTemplate(ReportModel model, string templateText)
    {
        var template = Template.Parse(templateText);
        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages);
            throw new MarkdownRenderException($"Template parsing failed: {errors}");
        }

        // Create a script object that properly exposes all properties including nested collections
        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: member => ToSnakeCase(member.Name));

        // Register custom helper functions
        ScribanHelpers.RegisterHelpers(scriptObject);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        context.MemberRenamer = member => ToSnakeCase(member.Name);

        try
        {
            var result = template.Render(context);
            return result;
        }
        catch (Scriban.Syntax.ScriptRuntimeException ex)
        {
            throw new MarkdownRenderException($"Error rendering template: {ex.Message}", ex);
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string LoadDefaultTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(DefaultTemplateResourceName);

        if (stream is null)
        {
            throw new MarkdownRenderException($"Default template not found: {DefaultTemplateResourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
