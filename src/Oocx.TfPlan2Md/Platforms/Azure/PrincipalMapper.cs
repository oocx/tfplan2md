using System.Collections.Frozen;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Maps Azure AD/Entra principal IDs to display names.
/// Related feature: docs/features/006-role-assignment-readable-display/.
/// </summary>
/// <remarks>
/// The mapper loads principal information from a JSON file containing a dictionary
/// of principal IDs to display names. When a principal ID is encountered in role
/// assignments, it is replaced with the display name for improved readability.
/// </remarks>
internal class PrincipalMapper : IPrincipalMapper
{
    /// <summary>
    /// Maps principal IDs to resolved principal types when the mapping file provides type metadata.
    /// </summary>
    private readonly FrozenDictionary<string, string> _principalTypes;

    /// <summary>
    /// Maps principal IDs to display names for resolution.
    /// </summary>
    private readonly FrozenDictionary<string, string> _principals;

    /// <summary>
    /// Optional diagnostics for recording failed resolutions.
    /// </summary>
    private readonly IDiagnosticSink? _diagnosticSink;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalMapper"/> class.
    /// </summary>
    /// <param name="principals">Mapping of principal IDs to display names.</param>
    /// <param name="principalTypes">Mapping of principal IDs to resolved principal types.</param>
    /// <param name="diagnosticContext">Optional diagnostic sink for recording load status and failed resolutions.</param>
    /// <remarks>
    /// The mapper uses pre-parsed data so file I/O and diagnostics are handled upstream.
    /// Failed resolutions are recorded when a diagnostic context is provided.
    /// </remarks>
    public PrincipalMapper(
        IReadOnlyDictionary<string, string> principals,
        IReadOnlyDictionary<string, string> principalTypes,
        IDiagnosticSink? diagnosticContext = null)
    {
        _diagnosticSink = diagnosticContext;
        _principals = principals.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _principalTypes = principalTypes.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the display name for a principal ID without resource context.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <returns>
    /// Display name followed by principal ID in brackets if mapping exists,
    /// otherwise just the principal ID.
    /// </returns>
    public string GetPrincipalName(string principalId)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return principalId ?? string.Empty;
        }

        var name = GetName(principalId);

        return name is null
            ? principalId
            : $"{name} [{principalId}]";
    }

    /// <summary>
    /// Gets the display name for a principal ID with optional type and resource context.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">The type of principal (currently not used).</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>
    /// Display name followed by principal ID in brackets if mapping exists,
    /// otherwise just the principal ID.
    /// </returns>
    /// <remarks>
    /// If a diagnostic context was provided and the principal ID cannot be resolved,
    /// the failure is recorded with the resource address for troubleshooting.
    /// </remarks>
    public string GetPrincipalName(string principalId, string? principalType, string? resourceAddress = null)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return principalId ?? string.Empty;
        }

        var name = GetName(principalId, principalType, resourceAddress);

        return name is null
            ? principalId
            : $"{name} [{principalId}]";
    }

    /// <summary>
    /// Gets only the display name for a principal ID without resource context.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    public string? GetName(string principalId)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return null;
        }

        return _principals.TryGetValue(principalId, out var name) ? name : null;
    }

    /// <summary>
    /// Gets only the display name for a principal ID with optional type and resource context.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">The type of principal (currently not used).</param>
    /// <param name="resourceAddress">Optional Terraform resource address for diagnostic tracking.</param>
    /// <returns>
    /// The display name if found in the mapping file, otherwise null.
    /// </returns>
    /// <remarks>
    /// If a diagnostic context was provided and the principal ID cannot be resolved,
    /// the failure is recorded with the resource address for troubleshooting.
    /// </remarks>
    public string? GetName(string principalId, string? principalType, string? resourceAddress)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return null;
        }

        var found = _principals.TryGetValue(principalId, out var name);

        // Record failed resolution for diagnostics
        if (!found && _diagnosticSink != null && resourceAddress != null)
        {
            _diagnosticSink.RecordFailedResolution(
                new FailedResolution(
                    FailedResolutionType.Principal,
                    principalId,
                    resourceAddress,
                    "not found in mapping file"));
        }

        return found ? name : null;
    }

    /// <summary>
    /// Attempts to resolve the principal type for the provided ID using nested mapping metadata.
    /// </summary>
    /// <param name="principalId">The GUID of the principal.</param>
    /// <param name="principalType">The resolved principal type when available.</param>
    /// <returns><c>true</c> when the mapping contains a type; otherwise <c>false</c>.</returns>
    public bool TryGetPrincipalType(string principalId, out string? principalType)
    {
        principalType = null;

        if (string.IsNullOrWhiteSpace(principalId))
        {
            return false;
        }

        return _principalTypes.TryGetValue(principalId, out principalType);
    }

}
