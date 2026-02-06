using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Loads icon rules from a JSON file and resolves matching icons.
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

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedIconProvider"/> class.
    /// </summary>
    /// <param name="filePath">The JSON file path containing icon rules.</param>
    public FileBasedIconProvider(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Icon rule file path must be provided.", nameof(filePath));
        }

        LoadRules(filePath);
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
    /// <param name="filePath">The JSON file path containing icon rules.</param>
    private void LoadRules(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var model = JsonSerializer.Deserialize(stream, IconRulesJsonContext.Default.IconRulesModel);
            if (model?.Rules is null)
            {
                throw new ServiceRegistrationException($"Failed to parse icon rules from '{filePath}'.");
            }

            foreach (var rule in model.Rules)
            {
                RegisterRule(rule, filePath);
            }
        }
        catch (ServiceRegistrationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new ServiceRegistrationException($"Failed to load icon rules from '{filePath}'.", ex);
        }
    }

    /// <summary>
    /// Registers a single icon rule after validation.
    /// </summary>
    /// <param name="rule">The icon rule to register.</param>
    /// <param name="filePath">The source file path for diagnostics.</param>
    private void RegisterRule(IconRule rule, string filePath)
    {
        if (rule is null)
        {
            throw new ServiceRegistrationException($"Null icon rule encountered in '{filePath}'.");
        }

        if (string.IsNullOrWhiteSpace(rule.Icon))
        {
            throw new ServiceRegistrationException($"Icon rule in '{filePath}' must define an icon value.");
        }

        var pattern = new MatchPattern(
            rule.ProviderPattern,
            rule.ResourceTypePattern,
            rule.AttributeNamePattern,
            rule.ValuePattern);

        _registry.Register(pattern, rule);
    }
}
