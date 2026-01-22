using System.Text;
using System.Text.RegularExpressions;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.Diagnostics;
using Scriban;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Renders Terraform plan reports to Markdown using Scriban templates.
/// </summary>
internal class MarkdownRenderer
{
    private const string TemplateResourcePrefix = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.";

    private static readonly HashSet<string> BuiltInTemplates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "default",
            "summary"
        };

    private readonly Azure.IPrincipalMapper _principalMapper;
    private readonly ScribanTemplateLoader _templateLoader;
    private readonly TemplateResolver _templateResolver;
    private readonly DiagnosticContext? _diagnosticContext;

    /// <summary>
    /// Creates a new MarkdownRenderer using embedded templates.
    /// </summary>
    /// <param name="principalMapper">Optional principal mapper for resolving principal names.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for collecting debug information.</param>
    public MarkdownRenderer(Azure.IPrincipalMapper? principalMapper = null, DiagnosticContext? diagnosticContext = null)
    {
        _principalMapper = principalMapper ?? new Azure.NullPrincipalMapper();
        _templateLoader = new ScribanTemplateLoader(templateResourcePrefix: TemplateResourcePrefix);
        _templateResolver = new TemplateResolver(_templateLoader);
        _diagnosticContext = diagnosticContext;
    }

    /// <summary>
    /// Creates a new MarkdownRenderer with a custom template directory.
    /// </summary>
    /// <param name="customTemplateDirectory">Path to custom template directory for resource-specific template overrides.</param>
    /// <param name="principalMapper">Optional principal mapper for resolving principal names.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for collecting debug information.</param>
    public MarkdownRenderer(string customTemplateDirectory, Azure.IPrincipalMapper? principalMapper = null, DiagnosticContext? diagnosticContext = null)
    {
        _principalMapper = principalMapper ?? new Azure.NullPrincipalMapper();
        _templateLoader = new ScribanTemplateLoader(customTemplateDirectory, templateResourcePrefix: TemplateResourcePrefix);
        _templateResolver = new TemplateResolver(_templateLoader);
        _diagnosticContext = diagnosticContext;
    }

    /// <summary>
    /// Renders a report model to Markdown using the default embedded template.
    /// </summary>
    /// <remarks>
    /// Uses single-pass rendering via Scriban's include mechanism.
    /// Resource-specific templates are dispatched directly through the
    /// <c>resolve_template</c> helper function registered with the template context.
    /// </remarks>
    /// <param name="model">The report model to render.</param>
    /// <returns>The rendered Markdown string.</returns>
    public string Render(ReportModel model)
    {
        var defaultTemplate = LoadTemplate("default");

        // Record template resolution for main template
        _diagnosticContext?.TemplateResolutions.Add(
            new TemplateResolution("_main", "Built-in default template"));

        return RenderWithTemplate(model, defaultTemplate, "default");
    }

    /// <summary>
    /// Renders a report model to Markdown using a built-in template name or custom template file.
    /// Built-in names take precedence over file paths.
    /// </summary>
    /// <param name="model">The report model to render.</param>
    /// <param name="templateNameOrPath">Built-in template name (e.g., "summary") or path to a custom template file.</param>
    /// <returns>The rendered Markdown string.</returns>
    public string Render(ReportModel model, string templateNameOrPath)
    {
        var templateText = ResolveTemplateText(templateNameOrPath);

        // Record template resolution for main template
        var templateSource = _templateLoader.TryGetTemplate(templateNameOrPath, out _)
            ? $"Built-in template: {templateNameOrPath}"
            : File.Exists(templateNameOrPath)
                ? $"Custom template: {templateNameOrPath}"
                : "Unknown template source";

        _diagnosticContext?.TemplateResolutions.Add(
            new TemplateResolution("_main", templateSource));

        return RenderWithTemplate(model, templateText, templateNameOrPath);
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
        var templateText = await ResolveTemplateTextAsync(templatePath, cancellationToken);

        // Record template resolution for main template
        var templateSource = _templateLoader.TryGetTemplate(templatePath, out _)
            ? $"Built-in template: {templatePath}"
            : File.Exists(templatePath)
                ? $"Custom template: {templatePath}"
                : "Unknown template source";

        _diagnosticContext?.TemplateResolutions.Add(
            new TemplateResolution("_main", templateSource));

        return RenderWithTemplate(model, templateText, templatePath);
    }

    private string ResolveTemplateText(string templateNameOrPath)
    {
        if (_templateLoader.TryGetTemplate(templateNameOrPath, out var builtInTemplate))
        {
            return builtInTemplate;
        }

        if (File.Exists(templateNameOrPath))
        {
            return File.ReadAllText(templateNameOrPath);
        }

        throw new MarkdownRenderException($"Template '{templateNameOrPath}' not found. Available built-in templates: {string.Join(", ", BuiltInTemplates)}");
    }

    private async Task<string> ResolveTemplateTextAsync(string templateNameOrPath, CancellationToken cancellationToken)
    {
        if (_templateLoader.TryGetTemplate(templateNameOrPath, out var builtInTemplate))
        {
            return builtInTemplate;
        }

        if (File.Exists(templateNameOrPath))
        {
            return await File.ReadAllTextAsync(templateNameOrPath, cancellationToken);
        }

        throw new MarkdownRenderException($"Template '{templateNameOrPath}' not found. Available built-in templates: {string.Join(", ", BuiltInTemplates)}");
    }

    /// <summary>
    /// Renders a single resource change using the appropriate resource-specific template.
    /// Falls back to the default template rendering if no specific template exists.
    /// </summary>
    /// <param name="change">The resource change to render.</param>
    /// <returns>The rendered Markdown string for this resource, or null if default handling should be used.</returns>
    public string? RenderResourceChange(ResourceChangeModel change, LargeValueFormat largeValueFormat = LargeValueFormat.InlineDiff)
    {
        var templateSource = ResolveResourceTemplate(change.Type);
        if (templateSource is null)
        {
            return null; // Use default template handling
        }

        try
        {
            return RenderResourceWithTemplate(change, templateSource.Value, largeValueFormat);
        }
        catch (ScribanHelperException ex)
        {
            // Return error message for this resource but allow other resources to render
            return $"### {change.ActionSymbol}{ScribanHelpers.NonBreakingSpace}{change.Address}\n\n⚠️{ScribanHelpers.NonBreakingSpace}**Template Error:** {ex.Message}\n";
        }
    }

    /// <summary>
    /// Resolves a template for the given resource type.
    /// Resolution order: custom directory (if set) → embedded resources.
    /// Within each: Templates/{provider}/{resource}.sbn → Templates/default.sbn.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type (e.g., "azurerm_firewall_network_rule_collection").</param>
    /// <returns>The template text if a resource-specific template exists, null otherwise.</returns>
    private TemplateSource? ResolveResourceTemplate(string resourceType)
    {
        var (provider, resource) = ResourceTypeParser.Parse(resourceType);
        if (provider is null || resource is null)
        {
            return null;
        }

        var path = $"{provider}/{resource}";
        if (_templateLoader.TryGetTemplate(path, out var template))
        {
            // Record template resolution
            var templateSource = _templateLoader.HasCustomTemplateDirectory
                ? $"Custom resource-specific template: {path}.sbn"
                : $"Built-in resource-specific template: {path}.sbn";

            _diagnosticContext?.TemplateResolutions.Add(
                new TemplateResolution(resourceType, templateSource));

            return new TemplateSource(path, template);
        }

        // Record that default template is being used for this resource type
        _diagnosticContext?.TemplateResolutions.Add(
            new TemplateResolution(resourceType, "Default template"));

        return null;
    }

    /// <summary>
    /// Parses a Terraform resource type into provider and resource name.
    /// </summary>
    /// <param name="resourceType">The resource type (e.g., "azurerm_firewall_network_rule_collection").</param>
    /// <returns>Tuple of (provider, resource) or (null, null) if parsing fails.</returns>
    private string RenderResourceWithTemplate(ResourceChangeModel change, TemplateSource templateSource, LargeValueFormat largeValueFormat)
    {
        var template = Template.Parse(templateSource.Content, templateSource.Path);
        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages);
            throw new MarkdownRenderException($"Template parsing failed: {errors}");
        }

        var scriptObject = new ScriptObject();

        // Create a nested ScriptObject for the change using AOT-compatible mapping
        // Templates access properties via change.* for consistency with default.sbn include
        var changeObject = AotScriptObjectMapper.MapResourceChangeWithFormat(change, largeValueFormat);

        scriptObject["change"] = changeObject;

        // Register custom helper functions
        ScribanHelpers.RegisterHelpers(scriptObject, _principalMapper, largeValueFormat);
        RegisterRendererHelpers(scriptObject);

        var context = CreateTemplateContext(scriptObject);

        try
        {
            var rendered = template.Render(context);
            // Collapse blank lines between table rows (which breaks tables)
            rendered = Regex.Replace(rendered, @"(?<=\|[^\n]*)\n\s*\n(?=[ \t]*\|)", "\n");
            // Remove indentation from table rows (which causes them to be treated as code blocks)
            rendered = Regex.Replace(rendered, @"\n[ \t]+(\|)", "\n$1");
            rendered = NormalizeHeadingSpacing(rendered);
            return rendered;
        }
        catch (Scriban.Syntax.ScriptRuntimeException ex)
        {
            throw new MarkdownRenderException($"Error rendering template: {ex.Message}", ex);
        }
    }

    private string RenderWithTemplate(ReportModel model, string templateText, string templatePath)
    {
        var template = Template.Parse(templateText, templatePath);
        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages);
            throw new MarkdownRenderException($"Template parsing failed: {errors}");
        }

        // Create a script object without relying on Reflection.Emit (unsupported in NativeAOT)
        var scriptObject = CreateScriptObject(model);

        // Register custom helper functions
        ScribanHelpers.RegisterHelpers(scriptObject, _principalMapper, model.LargeValueFormat);
        RegisterRendererHelpers(scriptObject);

        var context = CreateTemplateContext(scriptObject);

        try
        {
            var result = template.Render(context);
            return NormalizeHeadingSpacing(result);
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

    /// <summary>
    /// Normalizes markdown output to ensure headings and surrounding content are separated by blank lines.
    /// This prevents rendering issues where content runs directly into headings or tables.
    /// </summary>
    private static string NormalizeHeadingSpacing(string markdown)
    {
        // Collapse runs of multiple blank lines (including whitespace-only lines) to a single blank line.
        markdown = Regex.Replace(markdown, @"\n([ \t]*\n){2,}", "\n\n");

        // Ensure exactly one blank line before any heading that follows non-blank content.
        // Match: newline, optional horizontal whitespace, non-whitespace content, newline(s), then heading.
        // If there's already a blank line (\n\n or more), the heading is fine.
        // Only add a blank line when there's exactly one newline before the heading.
        markdown = Regex.Replace(markdown, @"([^\n])\n(#{1,6}\s)", "$1\n\n$2");

        // Ensure a blank line after headings when the following line is not already blank.
        markdown = Regex.Replace(markdown, @"(#{1,6}\s.+)\n(?!\n)", "$1\n\n");

        // Remove trailing blank lines while keeping a single newline at EOF for POSIX tools.
        markdown = markdown.TrimEnd();
        return $"{markdown}\n";
    }

    private TemplateContext CreateTemplateContext(ScriptObject scriptObject)
    {
        var context = new TemplateContext
        {
            TemplateLoader = _templateLoader,
            MemberRenamer = member => ToSnakeCase(member.Name)
        };

        context.PushGlobal(scriptObject);
        return context;
    }

    private void RegisterRendererHelpers(ScriptObject scriptObject)
    {
        _templateResolver.Register(scriptObject);
    }

    /// <summary>
    /// Creates a ScriptObject from a ReportModel using explicit AOT-compatible mapping.
    /// Reflection-based Import does not work reliably under NativeAOT.
    /// </summary>
    private static ScriptObject CreateScriptObject(ReportModel model)
    {
        // Use explicit mapping for NativeAOT compatibility - reflection-based
        // Import fails at runtime even with TrimmerRootDescriptor preservation
        return AotScriptObjectMapper.MapReportModel(model);
    }

    private string LoadTemplate(string templateName)
    {
        if (_templateLoader.TryGetTemplate(templateName, out var template))
        {
            return template;
        }

        if (BuiltInTemplates.Contains(templateName))
        {
            throw new MarkdownRenderException($"Built-in template '{templateName}' not found.");
        }

        throw new MarkdownRenderException($"Template '{templateName}' not found.");
    }

    private readonly record struct TemplateSource(string Path, string Content);
}
