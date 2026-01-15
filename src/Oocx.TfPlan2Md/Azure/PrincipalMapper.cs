using System.Collections.Frozen;
using System.Text.Json;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Azure;

/// <summary>
/// Maps Azure AD/Entra principal IDs to display names.
/// Related feature: docs/features/006-role-assignment-readable-display/
/// </summary>
/// <remarks>
/// The mapper loads principal information from a JSON file containing a dictionary
/// of principal IDs to display names. When a principal ID is encountered in role
/// assignments, it is replaced with the display name for improved readability.
/// </remarks>
internal class PrincipalMapper : IPrincipalMapper
{
    private readonly FrozenDictionary<string, string> _principals;
    private readonly DiagnosticContext? _diagnosticContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalMapper"/> class.
    /// </summary>
    /// <param name="mappingFile">Path to the JSON file containing principal ID to name mappings.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for recording load status and failed resolutions.</param>
    /// <remarks>
    /// If the mapping file is null, empty, or cannot be loaded, the mapper will return
    /// principal IDs unchanged. When a diagnostic context is provided, load status and
    /// failed resolutions are recorded for debug output.
    /// </remarks>
    public PrincipalMapper(string? mappingFile, DiagnosticContext? diagnosticContext = null)
    {
        _diagnosticContext = diagnosticContext;
        _principals = LoadMappings(mappingFile, diagnosticContext);
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
        var name = GetName(principalId);

        if (string.IsNullOrWhiteSpace(principalId))
        {
            return principalId ?? string.Empty;
        }

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
        var name = GetName(principalId, principalType, resourceAddress);

        if (string.IsNullOrWhiteSpace(principalId))
        {
            return principalId ?? string.Empty;
        }

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
    public string? GetName(string principalId, string? principalType, string? resourceAddress = null)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return null;
        }

        var found = _principals.TryGetValue(principalId, out var name);

        // Record failed resolution for diagnostics
        if (!found && _diagnosticContext != null && resourceAddress != null)
        {
            _diagnosticContext.FailedResolutions.Add(
                new FailedPrincipalResolution(principalId, resourceAddress));
        }

        return found ? name : null;
    }

    /// <summary>
    /// Loads principal mappings from a JSON file and records diagnostic information.
    /// </summary>
    /// <param name="mappingFile">Path to the JSON file containing principal mappings.</param>
    /// <param name="diagnosticContext">Optional diagnostic context to record load status.</param>
    /// <returns>A frozen dictionary of principal IDs to display names.</returns>
    /// <remarks>
    /// The JSON file should contain a flat dictionary of principal ID (GUID) to display name.
    /// If the file doesn't exist, is malformed, or cannot be read, an empty dictionary is
    /// returned and a warning is logged to stderr. When a diagnostic context is provided,
    /// the load status and principal type counts are recorded.
    /// Enhanced in issue 042 to provide detailed error diagnostics for troubleshooting.
    /// </remarks>
    private static FrozenDictionary<string, string> LoadMappings(string? mappingFile, DiagnosticContext? diagnosticContext)
    {
        if (string.IsNullOrWhiteSpace(mappingFile))
        {
            return FrozenDictionary<string, string>.Empty;
        }

        // Record that a mapping file was provided
        if (diagnosticContext != null)
        {
            diagnosticContext.PrincipalMappingFileProvided = true;
            diagnosticContext.PrincipalMappingFilePath = mappingFile;
        }

        // Pre-flight file system checks (Issue 042)
        var fileExists = File.Exists(mappingFile);
        var directory = Path.GetDirectoryName(mappingFile);
        var directoryExists = !string.IsNullOrEmpty(directory) && Directory.Exists(directory);

        if (diagnosticContext != null)
        {
            diagnosticContext.PrincipalMappingFileExists = fileExists;
            diagnosticContext.PrincipalMappingDirectoryExists = directoryExists;
        }

        // Handle file not found early
        if (!fileExists)
        {
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;

                // Determine error type based on directory existence
                if (!directoryExists)
                {
                    diagnosticContext.PrincipalMappingErrorType = PrincipalLoadError.DirectoryNotFound;
                    diagnosticContext.PrincipalMappingErrorMessage = "Directory not found";
                    diagnosticContext.PrincipalMappingErrorDetails = $"Could not find directory '{directory}'";
                }
                else
                {
                    diagnosticContext.PrincipalMappingErrorType = PrincipalLoadError.FileNotFound;
                    diagnosticContext.PrincipalMappingErrorMessage = "File not found";
                    diagnosticContext.PrincipalMappingErrorDetails = $"Could not find file '{mappingFile}'";
                }
            }

            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': File not found");
            return FrozenDictionary<string, string>.Empty;
        }

        try
        {
            var content = File.ReadAllText(mappingFile);
            var parsed = JsonSerializer.Deserialize(content, TfPlanJsonContext.Default.DictionaryStringString);

            if (parsed is null)
            {
                if (diagnosticContext != null)
                {
                    diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                    diagnosticContext.PrincipalMappingErrorType = PrincipalLoadError.EmptyFile;
                    diagnosticContext.PrincipalMappingErrorMessage = "File is empty or contains null";
                    diagnosticContext.PrincipalMappingErrorDetails = "The JSON file was parsed but resulted in null";
                }

                Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': File is empty or invalid");
                return FrozenDictionary<string, string>.Empty;
            }

            // Record successful load and count principals
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = true;

                // Count total principals (we don't have type information in the mapping file,
                // so we'll just report total count as "principals")
                diagnosticContext.PrincipalTypeCount["principals"] = parsed.Count;
            }

            return parsed.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException ex)
        {
            // JSON parsing errors - provide line/column information if available
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorType = PrincipalLoadError.JsonParseError;
                diagnosticContext.PrincipalMappingErrorMessage = "Invalid JSON syntax";

                // Extract line/column information if available
                var details = ex.Message;
                if (ex.LineNumber.HasValue || ex.BytePositionInLine.HasValue)
                {
                    details = $"{ex.Message} at line {ex.LineNumber}, column {ex.BytePositionInLine}";
                }
                diagnosticContext.PrincipalMappingErrorDetails = details;
            }

            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied errors
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorType = PrincipalLoadError.AccessDenied;
                diagnosticContext.PrincipalMappingErrorMessage = "Access denied";
                diagnosticContext.PrincipalMappingErrorDetails = ex.Message;
            }

            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
        catch (Exception ex)
        {
            // Catch-all for unexpected errors
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorType = PrincipalLoadError.UnknownError;
                diagnosticContext.PrincipalMappingErrorMessage = ex.GetType().Name;
                diagnosticContext.PrincipalMappingErrorDetails = ex.Message;
            }

            // Intentional swallow after logging: malformed or unreadable mapping files should gracefully
            // fall back to raw principal IDs instead of failing plan generation, but the user should know why.
            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
    }
}
