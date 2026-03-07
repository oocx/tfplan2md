using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Azure role definitions using immutable built-in mappings and run-scoped custom mappings.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal sealed class AzureRoleDefinitionResolver : IRoleDefinitionResolver
{
    /// <summary>
    /// Shared built-in-only resolver used for default formatting paths and backwards-compatible helpers.
    /// </summary>
    private static readonly AzureRoleDefinitionResolver BuiltInOnlyResolver = new(
        AzureRoleDefinitionsRegistry.Load(),
        FrozenDictionary<string, string>.Empty,
        diagnosticContext: null);

    /// <summary>
    /// Immutable built-in role definitions loaded from the embedded payload.
    /// </summary>
    private readonly FrozenDictionary<string, string> _builtInRoles;

    /// <summary>
    /// Run-scoped custom role definitions loaded from the mapping file.
    /// </summary>
    private readonly FrozenDictionary<string, string> _customRoles;

    /// <summary>
    /// Optional diagnostic context used to record failed role resolutions for the current run.
    /// </summary>
    private readonly DiagnosticContext? _diagnosticContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureRoleDefinitionResolver"/> class.
    /// </summary>
    /// <param name="customRoles">Custom role mappings loaded for the current application run.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for failed lookup tracking.</param>
    internal AzureRoleDefinitionResolver(
        IReadOnlyList<MappingEntry> customRoles,
        DiagnosticContext? diagnosticContext = null)
        : this(
            AzureRoleDefinitionsRegistry.Load(),
            CreateCustomRoleLookup(customRoles),
            diagnosticContext)
    {
    }

    /// <summary>
    /// Returns a shared resolver containing only built-in role definitions.
    /// </summary>
    /// <returns>The shared built-in-only resolver instance.</returns>
    internal static IRoleDefinitionResolver CreateBuiltIn()
    {
        return BuiltInOnlyResolver;
    }

    /// <inheritdoc />
    public string GetRoleName(string? roleDefinitionId)
    {
        return GetRoleDefinition(roleDefinitionId, null).FullName;
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Role resolution handles multiple nullable inputs and mapping fallbacks.")]
    public RoleDefinitionInfo GetRoleDefinition(
        string? roleDefinitionId,
        string? roleDefinitionName,
        string? resourceAddress = null)
    {
        if (string.IsNullOrWhiteSpace(roleDefinitionId))
        {
            var fallbackName = roleDefinitionName ?? string.Empty;
            return new RoleDefinitionInfo(fallbackName, string.Empty, fallbackName);
        }

        var roleGuid = ExtractGuid(roleDefinitionId);
        var mappedName = string.Empty;
        var hasCustomMapping = !string.IsNullOrEmpty(roleGuid) && _customRoles.TryGetValue(roleGuid, out mappedName);
        var hasBuiltInMapping = !hasCustomMapping
            && !string.IsNullOrEmpty(roleGuid)
            && _builtInRoles.TryGetValue(roleGuid, out mappedName);
        var hasMapping = hasCustomMapping || hasBuiltInMapping;

#pragma warning disable S2583
#pragma warning disable S3358
        var name = hasMapping
            ? mappedName
            : roleDefinitionName ?? (string.IsNullOrEmpty(roleGuid) ? roleDefinitionId : roleGuid) ?? string.Empty;

        var fullName = hasMapping
            ? $"{mappedName} ({roleGuid})"
            : roleDefinitionName ?? roleDefinitionId ?? string.Empty;

        var id = string.IsNullOrEmpty(roleGuid) ? roleDefinitionId : roleGuid;

        var safeName = name ?? string.Empty;
        var safeId = id ?? string.Empty;
#pragma warning restore S3358
#pragma warning restore S2583

        RecordFailedResolution(hasMapping, roleGuid, roleDefinitionId, resourceAddress);

        return new RoleDefinitionInfo(safeName, safeId, fullName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureRoleDefinitionResolver"/> class using prebuilt lookups.
    /// </summary>
    /// <param name="builtInRoles">The immutable built-in role lookup.</param>
    /// <param name="customRoles">The immutable custom role lookup.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for failed lookup tracking.</param>
    private AzureRoleDefinitionResolver(
        FrozenDictionary<string, string> builtInRoles,
        FrozenDictionary<string, string> customRoles,
        DiagnosticContext? diagnosticContext)
    {
        _builtInRoles = builtInRoles;
        _customRoles = customRoles;
        _diagnosticContext = diagnosticContext;
    }

    /// <summary>
    /// Creates a frozen custom role lookup keyed by normalized role GUID.
    /// </summary>
    /// <param name="roles">The custom role mappings to convert.</param>
    /// <returns>An immutable lookup of custom role IDs to display names.</returns>
    private static FrozenDictionary<string, string> CreateCustomRoleLookup(IReadOnlyList<MappingEntry> roles)
    {
        if (roles.Count == 0)
        {
            return FrozenDictionary<string, string>.Empty;
        }

        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role.Id))
            {
                continue;
            }

            var key = ExtractGuid(role.Id);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            lookup[key] = role.DisplayName ?? string.Empty;
        }

        return lookup.Count == 0
            ? FrozenDictionary<string, string>.Empty
            : lookup.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Records a failed role definition lookup when diagnostics are enabled for the current run.
    /// </summary>
    /// <param name="hasMapping">Whether the role resolved successfully.</param>
    /// <param name="roleGuid">The extracted role GUID, when available.</param>
    /// <param name="roleDefinitionId">The raw role definition ID.</param>
    /// <param name="resourceAddress">The referencing Terraform resource address.</param>
    private void RecordFailedResolution(
        bool hasMapping,
        string? roleGuid,
        string? roleDefinitionId,
        string? resourceAddress)
    {
        if (hasMapping || _diagnosticContext == null || string.IsNullOrWhiteSpace(resourceAddress))
        {
            return;
        }

        var id = string.IsNullOrWhiteSpace(roleGuid) ? roleDefinitionId : roleGuid;
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _diagnosticContext.FailedResolutions.Add(new FailedResolution(
            FailedResolutionType.RoleDefinition,
            id,
            resourceAddress,
            "not found in mapping file or built-in roles"));
    }

    /// <summary>
    /// Extracts the role GUID from a full Azure role definition ID or returns the original value.
    /// </summary>
    /// <param name="roleDefinitionId">The raw role definition identifier.</param>
    /// <returns>The extracted GUID or the original input when no slash is present.</returns>
    private static string ExtractGuid(string roleDefinitionId)
    {
        var lastSlashIndex = roleDefinitionId.LastIndexOf('/');
        return lastSlashIndex >= 0 && lastSlashIndex < roleDefinitionId.Length - 1
            ? roleDefinitionId[(lastSlashIndex + 1)..]
            : roleDefinitionId;
    }
}
