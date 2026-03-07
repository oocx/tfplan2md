using System.Text.Json;
using Oocx.TfPlan2Md.Diagnostics;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Loads Azure mapping files and returns parsed sections for principal and entity resolution.
/// </summary>
/// <remarks>
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </remarks>
internal static class AzureMappingFileLoader
{
    /// <summary>
    /// Loads the mapping file and returns a parsed result for downstream mappers.
    /// </summary>
    /// <param name="mappingFile">Path to the JSON mapping file.</param>
    /// <param name="diagnosticContext">Optional diagnostic sink for recording load status.</param>
    /// <returns>The parsed mapping file result.</returns>
    internal static AzureMappingFileResult Load(string? mappingFile, IDiagnosticSink? diagnosticContext)
    {
        if (string.IsNullOrWhiteSpace(mappingFile))
        {
            return AzureMappingFileResult.Empty;
        }

        diagnosticContext?.RecordPrincipalMappingFileProvided(mappingFile);

        var fileExists = File.Exists(mappingFile);
        var directory = Path.GetDirectoryName(mappingFile);
        var directoryExists = !string.IsNullOrEmpty(directory) && Directory.Exists(directory);

        diagnosticContext?.RecordPrincipalMappingPathStatus(fileExists, directoryExists);

        if (!fileExists)
        {
            RecordMissingFileDiagnostics(diagnosticContext, mappingFile, directoryExists, directory);
            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': File not found");
            return AzureMappingFileResult.Empty;
        }

        try
        {
            var content = File.ReadAllText(mappingFile);

            var parsed = AzureMappingFileParser.TryParse(content, diagnosticContext);
            if (parsed is null)
            {
                RecordEmptyFileDiagnostics(diagnosticContext);
                Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': File is empty or invalid");
                return AzureMappingFileResult.Empty;
            }

            return parsed;
        }
        catch (JsonException ex)
        {
            RecordJsonErrorDiagnostics(diagnosticContext, ex);
            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return AzureMappingFileResult.Empty;
        }
        catch (UnauthorizedAccessException ex)
        {
            RecordAccessDeniedDiagnostics(diagnosticContext, ex);
            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return AzureMappingFileResult.Empty;
        }
        catch (Exception ex)
        {
            RecordUnexpectedErrorDiagnostics(diagnosticContext, ex);
            Console.Error.WriteLine($"Warning: Could not read principal mapping file '{mappingFile}': {ex.Message}");
            return AzureMappingFileResult.Empty;
        }
    }

    /// <summary>
    /// Records diagnostics when the mapping file path is invalid or missing.
    /// </summary>
    /// <param name="diagnosticContext">Optional diagnostic context to update.</param>
    /// <param name="mappingFile">The mapping file path.</param>
    /// <param name="directoryExists">Whether the parent directory exists.</param>
    /// <param name="directory">The parent directory path.</param>
    private static void RecordMissingFileDiagnostics(
        IDiagnosticSink? diagnosticContext,
        string mappingFile,
        bool directoryExists,
        string? directory)
    {
        if (diagnosticContext == null)
        {
            return;
        }

        if (!directoryExists)
        {
            diagnosticContext.RecordPrincipalMappingLoadFailure(
                PrincipalLoadError.DirectoryNotFound,
                "Directory not found",
                $"Could not find directory '{directory}'");
        }
        else
        {
            diagnosticContext.RecordPrincipalMappingLoadFailure(
                PrincipalLoadError.FileNotFound,
                "File not found",
                $"Could not find file '{mappingFile}'");
        }
    }

    /// <summary>
    /// Records diagnostics when the mapping file is empty or deserializes to null.
    /// </summary>
    /// <param name="diagnosticContext">Optional diagnostic context to update.</param>
    private static void RecordEmptyFileDiagnostics(IDiagnosticSink? diagnosticContext)
    {
        if (diagnosticContext == null)
        {
            return;
        }

        diagnosticContext.RecordPrincipalMappingLoadFailure(
            PrincipalLoadError.EmptyFile,
            "File is empty or contains null",
            "The JSON file was parsed but resulted in null");
    }

    /// <summary>
    /// Records diagnostics for JSON parsing errors.
    /// </summary>
    /// <param name="diagnosticContext">Optional diagnostic context to update.</param>
    /// <param name="exception">The JSON exception encountered.</param>
    private static void RecordJsonErrorDiagnostics(IDiagnosticSink? diagnosticContext, JsonException exception)
    {
        if (diagnosticContext == null)
        {
            return;
        }

        var details = exception.Message;
        if (exception.LineNumber.HasValue || exception.BytePositionInLine.HasValue)
        {
            details = $"{exception.Message} at line {exception.LineNumber}, column {exception.BytePositionInLine}";
        }

        diagnosticContext.RecordPrincipalMappingLoadFailure(
            PrincipalLoadError.JsonParseError,
            "Invalid JSON syntax",
            details);
    }

    /// <summary>
    /// Records diagnostics for access denied errors when reading the mapping file.
    /// </summary>
    /// <param name="diagnosticContext">Optional diagnostic context to update.</param>
    /// <param name="exception">The exception encountered.</param>
    private static void RecordAccessDeniedDiagnostics(IDiagnosticSink? diagnosticContext, UnauthorizedAccessException exception)
    {
        if (diagnosticContext == null)
        {
            return;
        }

        diagnosticContext.RecordPrincipalMappingLoadFailure(
            PrincipalLoadError.AccessDenied,
            "Access denied",
            exception.Message);
    }

    /// <summary>
    /// Records diagnostics for unexpected mapping file errors.
    /// </summary>
    /// <param name="diagnosticContext">Optional diagnostic context to update.</param>
    /// <param name="exception">The exception encountered.</param>
    private static void RecordUnexpectedErrorDiagnostics(IDiagnosticSink? diagnosticContext, Exception exception)
    {
        if (diagnosticContext == null)
        {
            return;
        }

        diagnosticContext.RecordPrincipalMappingLoadFailure(
            PrincipalLoadError.UnknownError,
            exception.GetType().Name,
            exception.Message);
    }

}
