namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Categorizes different types of errors that can occur when loading template files.
/// Related feature: docs/features/038-debug-output/
/// </summary>
/// <remarks>
/// This enum helps users understand the specific nature of a template loading failure.
/// </remarks>
public enum TemplateErrorType
{
    /// <summary>
    /// No error occurred; template loaded successfully.
    /// </summary>
    None,

    /// <summary>
    /// The specified template file does not exist.
    /// </summary>
    /// <remarks>
    /// This may indicate an incorrect file path or, in Docker environments,
    /// a missing volume mount.
    /// </remarks>
    FileNotFound,

    /// <summary>
    /// The template file exists but could not be read due to permissions or I/O errors.
    /// </summary>
    ReadError,

    /// <summary>
    /// The template file exists but contains invalid Scriban syntax that could not be parsed.
    /// </summary>
    ParseError
}
