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
public class PrincipalMapper : IPrincipalMapper
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

        if (!File.Exists(mappingFile))
        {
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorMessage = $"File not found: {mappingFile}";

                // Check if parent directory exists to help diagnose the issue
                try
                {
                    var directory = Path.GetDirectoryName(mappingFile);
                    diagnosticContext.PrincipalMappingParentDirectoryExists = !string.IsNullOrEmpty(directory) && Directory.Exists(directory);
                    diagnosticContext.PrincipalMappingErrorType = diagnosticContext.PrincipalMappingParentDirectoryExists == false ? PrincipalMappingErrorType.DirectoryNotFound : PrincipalMappingErrorType.FileNotFound;
                }
                catch
                {
                    // If we can't determine parent directory existence, just mark as FileNotFound
                    diagnosticContext.PrincipalMappingErrorType = PrincipalMappingErrorType.FileNotFound;
                }
            }
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
                    diagnosticContext.PrincipalMappingErrorType = PrincipalMappingErrorType.ParseError;
                    diagnosticContext.PrincipalMappingErrorMessage = "JSON deserialization returned null";

                    // Capture file size for parse errors
                    try
                    {
                        var fileInfo = new FileInfo(mappingFile);
                        diagnosticContext.PrincipalMappingFileSize = fileInfo.Length;
                    }
                    catch
                    {
                        // Ignore if we can't get file size
                    }
                }
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
        catch (JsonException jsonEx)
        {
            // JSON parse error - record detailed error information
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorType = PrincipalMappingErrorType.ParseError;

                // Include line and column information if available
                var errorMessage = jsonEx.Message;
                if (jsonEx.LineNumber.HasValue && jsonEx.BytePositionInLine.HasValue)
                {
                    errorMessage = $"JSON parse error at line {jsonEx.LineNumber.Value}, column {jsonEx.BytePositionInLine.Value}: {jsonEx.Message}";
                }
                diagnosticContext.PrincipalMappingErrorMessage = errorMessage;

                // Capture file size for parse errors
                try
                {
                    var fileInfo = new FileInfo(mappingFile);
                    diagnosticContext.PrincipalMappingFileSize = fileInfo.Length;
                }
                catch
                {
                    // Ignore if we can't get file size
                }
            }

            // Intentional swallow after logging: malformed mapping files should gracefully
            // fall back to raw principal IDs instead of failing plan generation
            Console.Error.WriteLine($"Warning: Could not parse principal mapping file '{mappingFile}': {jsonEx.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
        catch (IOException ioEx)
        {
            // I/O error (permissions, file locked, etc.)
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorType = PrincipalMappingErrorType.ReadError;
                diagnosticContext.PrincipalMappingErrorMessage = $"I/O error reading file: {ioEx.Message}";
            }

            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ioEx.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
        catch (UnauthorizedAccessException accessEx)
        {
            // Permission denied
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorType = PrincipalMappingErrorType.ReadError;
                diagnosticContext.PrincipalMappingErrorMessage = $"Access denied: {accessEx.Message}";
            }

            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {accessEx.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
        catch (Exception ex)
        {
            // Other unexpected errors
            if (diagnosticContext != null)
            {
                diagnosticContext.PrincipalMappingLoadedSuccessfully = false;
                diagnosticContext.PrincipalMappingErrorType = PrincipalMappingErrorType.ReadError;
                diagnosticContext.PrincipalMappingErrorMessage = $"Unexpected error: {ex.Message}";
            }

            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return FrozenDictionary<string, string>.Empty;
        }
    }
}
