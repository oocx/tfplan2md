namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Categorizes different types of errors that can occur when loading principal mapping files.
/// Related feature: docs/features/038-debug-output/
/// </summary>
/// <remarks>
/// This enum helps users understand the specific nature of a principal mapping load failure,
/// enabling them to take appropriate corrective action (e.g., fix file path, fix JSON syntax,
/// check Docker volume mounts).
/// </remarks>
public enum PrincipalMappingErrorType
{
    /// <summary>
    /// No error occurred; mapping loaded successfully.
    /// </summary>
    None,

    /// <summary>
    /// The specified mapping file does not exist.
    /// </summary>
    /// <remarks>
    /// This may indicate an incorrect file path or, in Docker environments,
    /// a missing volume mount.
    /// </remarks>
    FileNotFound,

    /// <summary>
    /// The parent directory of the specified mapping file does not exist.
    /// </summary>
    /// <remarks>
    /// This is a more specific case of FileNotFound, indicating that the entire
    /// directory path is invalid, not just the file name.
    /// </remarks>
    DirectoryNotFound,

    /// <summary>
    /// The mapping file exists but contains invalid JSON that could not be parsed.
    /// </summary>
    /// <remarks>
    /// This typically indicates a syntax error in the JSON file.
    /// The error message should include line/column information if available.
    /// </remarks>
    ParseError,

    /// <summary>
    /// The mapping file exists but could not be read due to permissions or I/O errors.
    /// </summary>
    ReadError
}
