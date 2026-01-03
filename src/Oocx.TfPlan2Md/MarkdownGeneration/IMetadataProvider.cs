using System;
using System.Linq;
using System.Reflection;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Provides tfplan2md version metadata for report rendering.
/// Related feature: docs/features/029-report-presentation-enhancements/specification.md
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
/// Related feature: docs/features/029-report-presentation-enhancements/specification.md
/// </remarks>
public readonly record struct ReportMetadata(string Version, string CommitHash, DateTimeOffset GeneratedAtUtc);

/// <summary>
/// Default metadata provider that reads assembly attributes and captures the current UTC time.
/// Related feature: docs/features/029-report-presentation-enhancements/specification.md
/// </summary>
public class AssemblyMetadataProvider : IMetadataProvider
{
    /// <summary>
    /// Assembly supplying version and commit information.
    /// </summary>
    private readonly Assembly _assembly;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyMetadataProvider"/> class.
    /// </summary>
    /// <param name="assembly">Assembly that contains the version metadata; when null, the executing assembly is used.</param>
    public AssemblyMetadataProvider(Assembly? assembly = null)
    {
        _assembly = assembly ?? Assembly.GetExecutingAssembly();
    }

    /// <summary>
    /// Gets metadata using assembly attributes and the current UTC time.
    /// </summary>
    /// <returns>Metadata populated from assembly attributes.</returns>
    public ReportMetadata GetMetadata()
    {
        var informationalVersion = _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = NormalizeVersion(informationalVersion);
        var commit = GetCommitHash();

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
    /// Gets the commit hash from assembly metadata or informational version metadata.
    /// </summary>
    /// <returns>Short commit hash (up to 7 characters) or "unknown" when not available.</returns>
    private string GetCommitHash()
    {
        var commit = _assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, "CommitHash", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(a.Key, "SourceRevisionId", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(commit))
        {
            commit = TryParseInformationalCommit();
        }

        if (string.IsNullOrWhiteSpace(commit))
        {
            return "unknown";
        }

        var trimmed = commit.Trim();
        return trimmed.Length > 7 ? trimmed[..7] : trimmed;
    }

    /// <summary>
    /// Extracts a commit hash from the informational version metadata when no assembly metadata is present.
    /// </summary>
    /// <returns>Commit hash string if present; otherwise null.</returns>
    private string? TryParseInformationalCommit()
    {
        var informationalVersion = _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return null;
        }

        var plusIndex = informationalVersion.IndexOf('+');
        if (plusIndex < 0 || plusIndex + 1 >= informationalVersion.Length)
        {
            return null;
        }

        var metadataPart = informationalVersion[(plusIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(metadataPart))
        {
            return null;
        }

        var firstSegment = metadataPart.Split(['.', '+', '-'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstSegment) ? null : firstSegment;
    }
}
