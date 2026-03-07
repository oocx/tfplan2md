using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Maps Azure DevOps user IDs to display names.
/// </summary>
/// <remarks>
/// Azure DevOps users are identified by unique GUIDs. This mapper resolves
/// user IDs to human-readable names for improved report readability.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal sealed class AzdoUserMapper
{
    /// <summary>
    /// Maps Azure DevOps user IDs to display names.
    /// </summary>
    private readonly FrozenDictionary<string, string> _userMappings;

    /// <summary>
    /// Optional diagnostics for recording failed resolutions.
    /// </summary>
    private readonly IDiagnosticSink? _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoUserMapper"/> class.
    /// </summary>
    /// <param name="userMappings">Mapping of user IDs to display names.</param>
    /// <param name="diagnostics">Optional diagnostic sink for recording failed resolutions.</param>
    public AzdoUserMapper(FrozenDictionary<string, string> userMappings, IDiagnosticSink? diagnostics)
    {
        _userMappings = userMappings;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets only the display name for a user ID without resource context.
    /// </summary>
    /// <param name="userId">The GUID of the user.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    public string? GetName(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return _userMappings.TryGetValue(userId, out var name) ? name : null;
    }

    /// <summary>
    /// Gets only the display name for a user ID with optional resource context.
    /// </summary>
    /// <param name="userId">The GUID of the user.</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    /// <remarks>
    /// If a diagnostic context was provided and the user ID cannot be resolved,
    /// the failure is recorded with the resource address for troubleshooting.
    /// </remarks>
    public string? GetName(string userId, string? resourceAddress)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var found = _userMappings.TryGetValue(userId, out var name);

        // Record failed resolution for diagnostics
        if (!found && _diagnostics != null && resourceAddress != null)
        {
            _diagnostics.RecordFailedResolution(
                new FailedResolution(
                    FailedResolutionType.AzdoUser,
                    userId,
                    resourceAddress,
                    "not found in mapping file"));
        }

        return found ? name : null;
    }

    /// <summary>
    /// Gets the formatted entity name for display (DisplayName [ID] or just ID if not mapped).
    /// </summary>
    /// <param name="userId">The GUID of the user.</param>
    /// <returns>
    /// Display name followed by user ID in brackets if mapping exists,
    /// otherwise just the user ID.
    /// </returns>
    public string GetEntityName(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return userId ?? string.Empty;
        }

        var displayName = GetName(userId);
        return displayName is null
            ? userId
            : $"{displayName} [{userId}]";
    }
}
