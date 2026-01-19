namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Represents a single coverage measurement entry for historical tracking.
/// Related feature: docs/features/043-code-coverage-ci/specification.md
/// </summary>
internal sealed record CoverageHistoryEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageHistoryEntry"/> record.
    /// </summary>
    /// <param name="timestamp">Timestamp of the coverage measurement.</param>
    /// <param name="commitSha">Commit SHA associated with the measurement.</param>
    /// <param name="lineCoverage">Line coverage percentage.</param>
    /// <param name="branchCoverage">Branch coverage percentage.</param>
    internal CoverageHistoryEntry(DateTimeOffset timestamp, string commitSha, decimal lineCoverage, decimal branchCoverage)
    {
        Timestamp = timestamp;
        CommitSha = commitSha;
        LineCoverage = lineCoverage;
        BranchCoverage = branchCoverage;
    }

    /// <summary>
    /// Gets the timestamp of the coverage measurement.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the commit SHA associated with the measurement.
    /// </summary>
    public string CommitSha { get; }

    /// <summary>
    /// Gets the line coverage percentage.
    /// </summary>
    public decimal LineCoverage { get; }

    /// <summary>
    /// Gets the branch coverage percentage.
    /// </summary>
    public decimal BranchCoverage { get; }
}
