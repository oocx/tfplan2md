using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Provides tfplan2md version metadata for report rendering.
/// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
/// </summary>
public interface IMetadataProvider
{
    /// <summary>
    /// Retrieves metadata for inclusion in the report model.
    /// </summary>
    /// <returns>Metadata containing version, commit hash, and generation timestamp.</returns>
    ReportMetadata GetMetadata();
}

/// <summary>
/// Immutable metadata describing the tfplan2md build and generation time.
/// </summary>
/// <param name="Version">Semantic version of tfplan2md.</param>
/// <param name="CommitHash">Short git commit hash (7 characters) for the build.</param>
/// <param name="GeneratedAtUtc">Timestamp in UTC when the report was generated.</param>
/// <remarks>
/// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
/// </remarks>
public readonly record struct ReportMetadata(string Version, string CommitHash, DateTimeOffset GeneratedAtUtc);

/// <summary>
/// Default metadata provider that reads build-time generated metadata and captures the current UTC time.
/// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
/// </summary>
public class AssemblyMetadataProvider : IMetadataProvider
{
    /// <summary>
    /// Gets metadata using generated build constants and the current UTC time.
    /// </summary>
    /// <returns>Metadata populated from generated build information.</returns>
    public ReportMetadata GetMetadata()
    {
        var version = NormalizeVersion(BuildInfo.InformationalVersion);
        var commit = NormalizeCommitHash(BuildInfo.CommitHash);

        return new ReportMetadata(version, commit, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Normalizes the informational version by removing build metadata and trimming whitespace.
    /// </summary>
    /// <param name="informationalVersion">Raw informational version string.</param>
    /// <returns>Normalized semantic version or a fallback.</returns>
    private static string NormalizeVersion(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return "unknown";
        }

        var trimmed = informationalVersion.Trim();
        var plusIndex = trimmed.IndexOf('+');

        return plusIndex >= 0 ? trimmed[..plusIndex] : trimmed;
    }

    /// <summary>
    /// Normalizes the commit hash by trimming whitespace and shortening to 7 characters.
    /// </summary>
    /// <param name="commitHash">Raw commit hash value.</param>
    /// <returns>Short commit hash (up to 7 characters) or "unknown" when not available.</returns>
    private static string NormalizeCommitHash(string? commitHash)
    {
        if (string.IsNullOrWhiteSpace(commitHash))
        {
            return "unknown";
        }

        var trimmed = commitHash.Trim();
        return trimmed.Length > 7 ? trimmed[..7] : trimmed;
    }
}
