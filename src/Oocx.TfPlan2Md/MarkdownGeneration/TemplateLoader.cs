using System.Reflection;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Loads Scriban templates from embedded resources and optional custom template directories.
/// </summary>
internal sealed class ScribanTemplateLoader : ITemplateLoader
{
    private const string TemplateExtension = ".sbn";
    private readonly string? _customTemplateDirectory;
    private readonly Assembly _assembly;
    private readonly string _coreTemplateResourcePrefix;
    private readonly IEnumerable<string> _providerTemplateResourcePrefixes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScribanTemplateLoader"/> class.
    /// </summary>
    /// <param name="customTemplateDirectory">Optional custom template directory for overrides.</param>
    /// <param name="assembly">Assembly that embeds built-in templates.</param>
    /// <param name="coreTemplateResourcePrefix">Namespace prefix for core embedded templates.</param>
    /// <param name="providerTemplateResourcePrefixes">Namespace prefixes for provider-specific templates.</param>
    public ScribanTemplateLoader(
        string? customTemplateDirectory = null,
        Assembly? assembly = null,
        string coreTemplateResourcePrefix = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.",
        IEnumerable<string>? providerTemplateResourcePrefixes = null)
    {
        _customTemplateDirectory = string.IsNullOrWhiteSpace(customTemplateDirectory)
            ? null
            : customTemplateDirectory;
        _assembly = assembly ?? Assembly.GetExecutingAssembly();
        _coreTemplateResourcePrefix = coreTemplateResourcePrefix;
        _providerTemplateResourcePrefixes = providerTemplateResourcePrefixes ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether a custom template directory was provided.
    /// </summary>
    public bool HasCustomTemplateDirectory => _customTemplateDirectory is not null;

    /// <inheritdoc />
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        var callerPath = callerSpan.FileName ?? string.Empty;
        var combined = CombinePath(callerPath, templateName);
        return EnsureExtension(combined);
    }

    /// <inheritdoc />
    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var template = LoadInternal(templatePath);
        if (template is null)
        {
            throw new ScriptRuntimeException(callerSpan, $"Template '{templatePath}' not found.");
        }

        return template;
    }

    /// <inheritdoc />
    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return new ValueTask<string>(Load(context, callerSpan, templatePath));
    }

    /// <summary>
    /// Attempts to load a template by path.
    /// </summary>
    /// <param name="templatePath">Normalized template path (e.g., "default" or "azurerm/network_security_group").</param>
    /// <param name="template">The loaded template text when found.</param>
    /// <returns>True if the template could be loaded.</returns>
    public bool TryGetTemplate(string templatePath, out string template)
    {
        var loaded = LoadInternal(templatePath);
        if (loaded is null)
        {
            template = string.Empty;
            return false;
        }

        template = loaded;
        return true;
    }

    /// <summary>
    /// Determines whether a template exists in the custom directory or embedded resources.
    /// </summary>
    /// <param name="templatePath">Normalized template path (without extension required).</param>
    /// <returns>True when the template can be loaded.</returns>
    public bool TemplateExists(string templatePath)
    {
        return LoadInternal(templatePath) is not null;
    }

    private string? LoadInternal(string templatePath)
    {
        var normalized = EnsureExtension(NormalizePath(templatePath));

        // 1. Check custom template directory first (highest priority)
        if (_customTemplateDirectory is not null)
        {
            var customPath = Path.Combine(_customTemplateDirectory, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(customPath))
            {
                return File.ReadAllText(customPath);
            }
        }

        // 2. Try core templates
        var coreResourceName = _coreTemplateResourcePrefix + normalized.Replace('/', '.');
        using (var stream = _assembly.GetManifestResourceStream(coreResourceName))
        {
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        // 3. Try provider-specific templates
        foreach (var providerPrefix in _providerTemplateResourcePrefixes)
        {
            var providerResourceName = providerPrefix + normalized.Replace('/', '.');
            using var stream = _assembly.GetManifestResourceStream(providerResourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        // Template not found
        return null;
    }

    private static string CombinePath(string? currentPath, string templateName)
    {
        var normalizedTemplate = NormalizePath(templateName);
        if (string.IsNullOrEmpty(currentPath))
        {
            return normalizedTemplate;
        }

        var basePath = NormalizePath(currentPath);
        var lastSeparator = basePath.LastIndexOf('/');
        if (lastSeparator < 0)
        {
            return normalizedTemplate;
        }

        var directory = basePath[..lastSeparator];
        return string.IsNullOrEmpty(directory)
            ? normalizedTemplate
            : $"{directory}/{normalizedTemplate}";
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Replace('\\', '/').Trim();
        normalized = normalized.TrimStart('/');
        if (normalized.EndsWith(TemplateExtension, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^TemplateExtension.Length];
        }

        return normalized;
    }

    private static string EnsureExtension(string path)
    {
        if (path.EndsWith(TemplateExtension, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return path + TemplateExtension;
    }
}
