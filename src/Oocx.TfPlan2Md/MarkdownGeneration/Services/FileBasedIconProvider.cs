using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Loads icon rules from an embedded JSON resource and resolves matching icons.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class FileBasedIconProvider : IIconProvider
{
    /// <summary>
    /// Holds the icon rules in matching order for resolution.
    /// </summary>
    private readonly PatternMatchingRegistry<IconRule> _registry = new();
    private readonly Assembly _assembly;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedIconProvider"/> class.
    /// </summary>
    /// <param name="resourceName">The embedded resource name containing icon rules.</param>
    public FileBasedIconProvider(string resourceName)
        : this(resourceName, Assembly.GetExecutingAssembly())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedIconProvider"/> class.
    /// </summary>
    /// <param name="resourceName">The embedded resource name containing icon rules.</param>
    /// <param name="assembly">The assembly that contains the embedded resource.</param>
    public FileBasedIconProvider(string resourceName, Assembly assembly)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new ArgumentException("Icon rule resource name must be provided.", nameof(resourceName));
        }

        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        LoadRules(resourceName);
    }

    /// <summary>
    /// Attempts to resolve an icon for the given resolution context.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>An icon string when handled; otherwise null.</returns>
    public string? TryGetIcon(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _registry.ResolveAll(context)
            .Select(rule => rule.Icon)
            .FirstOrDefault(icon => !string.IsNullOrWhiteSpace(icon));
    }

    /// <summary>
    /// Loads icon rules from disk and registers them for matching.
    /// </summary>
    /// <param name="resourceName">The embedded resource name containing icon rules.</param>
    private void LoadRules(string resourceName)
    {
        try
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new ServiceRegistrationException($"Failed to load embedded icon rules '{resourceName}'.");
            }

            var model = JsonSerializer.Deserialize(stream, IconRulesJsonContext.Default.IconRulesModel);
            if (model?.Rules is null)
            {
                throw new ServiceRegistrationException($"Failed to parse icon rules from embedded resource '{resourceName}'.");
            }

            foreach (var rule in model.Rules)
            {
                RegisterRule(rule, resourceName);
            }
        }
        catch (ServiceRegistrationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new ServiceRegistrationException($"Failed to load icon rules from embedded resource '{resourceName}'.", ex);
        }
    }

    /// <summary>
    /// Registers a single icon rule after validation.
    /// </summary>
    /// <param name="rule">The icon rule to register.</param>
    /// <param name="resourceName">The source embedded resource for diagnostics.</param>
    private void RegisterRule(IconRule rule, string resourceName)
    {
        if (rule is null)
        {
            throw new ServiceRegistrationException($"Null icon rule encountered in '{resourceName}'.");
        }

        if (string.IsNullOrWhiteSpace(rule.Icon))
        {
            throw new ServiceRegistrationException($"Icon rule in '{resourceName}' must define an icon value.");
        }

        var pattern = new MatchPattern(
            rule.ProviderPattern,
            rule.ResourceTypePattern,
            rule.AttributeNamePattern,
            rule.ValuePattern);

        _registry.Register(pattern, rule);
    }
}
