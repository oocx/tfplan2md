namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Represents the type of error that occurred when loading a principal mapping file.
/// Related feature: docs/issues/042-principal-loading-debug-context/
/// </summary>
/// <remarks>
/// This enum provides specific error categorization for principal mapping load failures,
/// enabling more detailed and actionable debug output. Each error type corresponds to
/// a different troubleshooting approach and user guidance.
/// </remarks>
internal enum PrincipalLoadError
{
    /// <summary>
    /// The principal mapping file does not exist at the specified path.
    /// </summary>
    /// <remarks>
    /// This is the most common error in Docker contexts where volume mounts are missing
    /// or incorrect. The debug output should guide users to verify file paths and
    /// Docker volume mount configuration.
    /// </remarks>
    FileNotFound,

    /// <summary>
    /// The parent directory of the principal mapping file does not exist.
    /// </summary>
    /// <remarks>
    /// This typically indicates a Docker volume mount issue where the mount point
    /// itself is incorrect or missing. Debug output should emphasize checking the
    /// volume mount configuration.
    /// </remarks>
    DirectoryNotFound,

    /// <summary>
    /// The principal mapping file contains invalid JSON syntax.
    /// </summary>
    /// <remarks>
    /// JSON parsing errors include specific line and column information to help users
    /// locate and fix syntax problems. Common issues include trailing commas, unquoted
    /// strings, and mismatched brackets.
    /// </remarks>
    JsonParseError,

    /// <summary>
    /// The principal mapping file is empty or contains no valid principal entries.
    /// </summary>
    /// <remarks>
    /// While not technically an error, an empty mapping file provides no value and may
    /// indicate user confusion about the file format or content. Debug output should
    /// provide examples of correct principal mapping structure.
    /// </remarks>
    EmptyFile,

    /// <summary>
    /// Access to the principal mapping file was denied due to permissions.
    /// </summary>
    /// <remarks>
    /// This error occurs when the file exists but cannot be read due to file system
    /// permissions. In Docker contexts, this may relate to user ID mismatches between
    /// host and container.
    /// </remarks>
    AccessDenied,

    /// <summary>
    /// An unexpected error occurred while loading the principal mapping file.
    /// </summary>
    /// <remarks>
    /// This catch-all category handles errors that don't fit the other specific types,
    /// such as IO errors, out-of-memory conditions, or other system-level failures.
    /// The original exception message should be preserved for debugging.
    /// </remarks>
    UnknownError
}
