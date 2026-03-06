using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Renders Terraform plan reports to Markdown using pure C# renderers.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class MarkdownRenderer
{
    private static readonly HashSet<string> BuiltInTemplates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "default",
            "summary"
        };

    private readonly DiagnosticContext? _diagnosticContext;
    private readonly ValueFormatterRegistry? _valueFormatterRegistry;
    private readonly IconProviderRegistry? _iconProviderRegistry;
    private readonly ResourceRendererRegistry _resourceRendererRegistry;
    private readonly ReportRenderer _reportRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownRenderer"/> class.
    /// </summary>
    /// <param name="principalMapper">Unused in pure C# mode; preserved for API compatibility.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for template/render tracking.</param>
    /// <param name="providerRegistry">Optional provider registry for formatter/icon registrations.</param>
    /// <param name="valueFormatterRegistry">Optional preconfigured value formatter registry.</param>
    /// <param name="iconProviderRegistry">Optional preconfigured icon provider registry.</param>
    public MarkdownRenderer(
        IPrincipalMapper? principalMapper = null,
        DiagnosticContext? diagnosticContext = null,
        ProviderRegistry? providerRegistry = null,
        ValueFormatterRegistry? valueFormatterRegistry = null,
        IconProviderRegistry? iconProviderRegistry = null)
    {
        _ = principalMapper;
        _diagnosticContext = diagnosticContext;
        _valueFormatterRegistry = valueFormatterRegistry ?? CreateValueFormatterRegistry(providerRegistry);
        _iconProviderRegistry = iconProviderRegistry ?? CreateIconProviderRegistry(providerRegistry);
        _resourceRendererRegistry = CreateResourceRendererRegistry(providerRegistry);
        _reportRenderer = new ReportRenderer(resourceRendererRegistry: _resourceRendererRegistry);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownRenderer"/> class for callers
    /// that still provide a legacy template-directory argument.
    /// </summary>
    /// <param name="customTemplateDirectory">Legacy directory argument retained for source compatibility.</param>
    /// <param name="principalMapper">Principal mapper parameter kept for caller signature compatibility.</param>
    /// <param name="diagnosticContext">Diagnostic sink used for template-resolution events.</param>
    /// <param name="providerRegistry">Provider module registry used to build formatter and icon registries.</param>
    /// <param name="valueFormatterRegistry">Optional explicit value formatter registry override.</param>
    /// <param name="iconProviderRegistry">Optional explicit icon provider registry override.</param>
    public MarkdownRenderer(
        string customTemplateDirectory,
        IPrincipalMapper? principalMapper = null,
        DiagnosticContext? diagnosticContext = null,
        ProviderRegistry? providerRegistry = null,
        ValueFormatterRegistry? valueFormatterRegistry = null,
        IconProviderRegistry? iconProviderRegistry = null)
        : this(principalMapper, diagnosticContext, providerRegistry, valueFormatterRegistry, iconProviderRegistry)
    {
        _ = customTemplateDirectory;
    }

    /// <summary>
    /// Renders with the default built-in template.
    /// </summary>
    /// <param name="model">Report model to render.</param>
    /// <returns>Rendered markdown.</returns>
    public string Render(ReportModel model)
    {
        _diagnosticContext?.TemplateResolutions.Add(new TemplateResolution("_main", "Built-in template: default"));
        var context = CreateContext(model);
        try
        {
            return _reportRenderer.Render(model, context);
        }
        finally
        {
            MarkdownHelpers.ClearLineDiffCache();
        }
    }

    /// <summary>
    /// Renders with a built-in template name.
    /// </summary>
    /// <param name="model">Report model to render.</param>
    /// <param name="templateNameOrPath">Built-in template name.</param>
    /// <returns>Rendered markdown.</returns>
    /// <exception cref="MarkdownRenderException">Thrown when template name is unsupported.</exception>
    public string Render(ReportModel model, string templateNameOrPath)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateNameOrPath);

        if (string.Equals(templateNameOrPath, "default", StringComparison.OrdinalIgnoreCase))
        {
            return Render(model);
        }

        try
        {
            if (string.Equals(templateNameOrPath, "summary", StringComparison.OrdinalIgnoreCase))
            {
                _diagnosticContext?.TemplateResolutions.Add(new TemplateResolution("_main", "Built-in template: summary"));
                return RenderSummaryTemplate(model);
            }

            throw new MarkdownRenderException(
                $"Template '{templateNameOrPath}' not found. Available built-in templates: {string.Join(", ", BuiltInTemplates)}");
        }
        finally
        {
            MarkdownHelpers.ClearLineDiffCache();
        }
    }

    /// <summary>
    /// Asynchronously renders with a built-in template name.
    /// </summary>
    /// <param name="model">Report model to render.</param>
    /// <param name="templatePath">Built-in template name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rendered markdown.</returns>
    public Task<string> RenderAsync(ReportModel model, string templatePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Render(model, templatePath));
    }

    /// <summary>
    /// Renders a single resource change with its registered specific renderer.
    /// Returns null when no specialized renderer is registered.
    /// </summary>
    /// <param name="change">Resource change to render.</param>
    /// <param name="renderTarget">Render target for formatting behavior.</param>
    /// <param name="detailsDisplayMode">Details block display mode.</param>
    /// <returns>Rendered resource markdown, or null when no specialized renderer exists.</returns>
    public string? RenderResourceChange(
        ResourceChangeModel change,
        RenderTarget renderTarget = RenderTarget.AzureDevOps,
        DetailsDisplayMode detailsDisplayMode = DetailsDisplayMode.Auto)
    {
        ArgumentNullException.ThrowIfNull(change);

        var renderer = _resourceRendererRegistry.GetRenderer(change.Type);
        if (renderer is null)
        {
            _diagnosticContext?.TemplateResolutions.Add(new TemplateResolution(change.Type, "Default renderer"));
            return null;
        }

        _diagnosticContext?.TemplateResolutions.Add(new TemplateResolution(change.Type, "C# resource renderer"));

        var writer = new MarkdownWriter();
        var context = new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: renderTarget,
            detailsDisplayMode: detailsDisplayMode,
            valueFormatterRegistry: _valueFormatterRegistry,
            iconProviderRegistry: _iconProviderRegistry);

        renderer.Render(writer, change, context);
        return writer.Build();
    }

    private string RenderSummaryTemplate(ReportModel model)
    {
        var writer = new MarkdownWriter();
        // The summary template snapshot starts with a blank line (convention for embedded/include use cases).
        writer.Raw("\n");
        var headerRenderer = new HeaderRenderer(defaultReportTitle: "Terraform Plan Summary");

        headerRenderer.Render(writer, model);
        // The summary template does not use bold for the Total row to preserve baseline output.
        SummaryRenderer.Render(writer, model.Summary, boldTotal: false);
        ReportRenderer.RenderRefactoring(writer, model.RefactoringOperations);
        ReportRenderer.RenderFilteredResourceInfo(writer, model);
        CodeAnalysisSectionRenderer.RenderSummary(writer, model.CodeAnalysis);

        return writer.Build();
    }

    private RenderContext CreateContext(ReportModel model)
    {
        return new RenderContext(
            showSensitive: model.ShowSensitive,
            showUnchangedValues: model.ShowUnchangedValues,
            ignoreAzureIdCaseChanges: model.IgnoreAzureIdCaseChanges,
            renderTarget: model.RenderTarget,
            detailsDisplayMode: model.DetailsDisplayMode,
            valueFormatterRegistry: _valueFormatterRegistry,
            iconProviderRegistry: _iconProviderRegistry);
    }

    private static ResourceRendererRegistry CreateResourceRendererRegistry(ProviderRegistry? providerRegistry)
    {
        var registry = new ResourceRendererRegistry();

        providerRegistry?.RegisterAllResourceRenderers(registry);

        return registry;
    }

    private static IconProviderRegistry? CreateIconProviderRegistry(ProviderRegistry? providerRegistry)
    {
        if (providerRegistry is null)
        {
            return null;
        }

        var registry = new IconProviderRegistry();
        providerRegistry.RegisterAllIconProviders(registry);
        return registry;
    }

    private static ValueFormatterRegistry? CreateValueFormatterRegistry(ProviderRegistry? providerRegistry)
    {
        if (providerRegistry is null)
        {
            return null;
        }

        var registry = new ValueFormatterRegistry();
        providerRegistry.RegisterAllValueFormatters(registry);
        return registry;
    }
}
