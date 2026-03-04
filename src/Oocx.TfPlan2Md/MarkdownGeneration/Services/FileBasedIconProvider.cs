using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Loads icon rules from an embedded JSON resource and resolves matching icons.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Pattern-based icon rule loading naturally couples parsing and registry models.")]
internal sealed class FileBasedIconProvider : IIconProvider
{
    /// <summary>
    /// Holds the icon rules in matching order for resolution.
    /// </summary>
    private readonly PatternMatchingRegistry<IconRule> _registry = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedIconProvider"/> class.
    /// </summary>
    /// <param name="resourceName">The embedded resource name containing icon rules.</param>
    public FileBasedIconProvider(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new ArgumentException("Icon rule resource name must be provided.", nameof(resourceName));
        }

        LoadRules(resourceName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedIconProvider"/> class for raw JSON payload content.
    /// </summary>
    /// <param name="jsonPayload">UTF-8 JSON payload containing icon rules.</param>
    /// <param name="sourceName">Logical source name used in diagnostics.</param>
    internal FileBasedIconProvider(ReadOnlyMemory<byte> jsonPayload, string sourceName)
    {
        LoadRules(jsonPayload, sourceName);
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
        var payload = EmbeddedJsonPayloads.GetIconRulesPayload(resourceName);
        if (payload is null)
        {
            throw new ServiceRegistrationException($"Failed to load embedded icon rules '{resourceName}'.");
        }

        LoadRules(payload, resourceName);
    }

    /// <summary>
    /// Loads icon rules from a UTF-8 JSON payload and registers them for matching.
    /// </summary>
    /// <param name="jsonPayload">UTF-8 JSON payload containing icon rules.</param>
    /// <param name="sourceName">Logical source name used in diagnostics.</param>
    private void LoadRules(ReadOnlyMemory<byte> jsonPayload, string sourceName)
    {
        try
        {
            var model = JsonSerializer.Deserialize(jsonPayload.Span, IconRulesJsonContext.Default.IconRulesModel);
            if (model?.Rules is null)
            {
                throw new ServiceRegistrationException($"Failed to parse icon rules from embedded resource '{sourceName}'.");
            }

            foreach (var rule in model.Rules)
            {
                RegisterRule(rule, sourceName);
            }
        }
        catch (ServiceRegistrationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new ServiceRegistrationException($"Failed to load icon rules from embedded resource '{sourceName}'.", ex);
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
