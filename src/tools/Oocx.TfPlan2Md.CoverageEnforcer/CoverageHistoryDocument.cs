namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Represents the persisted coverage history document.
/// Related feature: docs/features/043-code-coverage-ci/specification.md
/// </summary>
internal sealed class CoverageHistoryDocument
{
    /// <summary>
    /// Gets or sets the list of coverage history entries.
    /// </summary>
    public List<CoverageHistoryEntry> Entries { get; init; } = [];
}
