using System.Globalization;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.TestData;

/// <summary>
/// Provides deterministic metadata values for snapshot and markdown rendering tests.
/// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
/// </summary>
internal static class TestMetadataProvider
{
    /// <summary>
    /// Fixed metadata instance used to stabilize snapshot outputs.
    /// </summary>
    internal static readonly ReportMetadata SnapshotMetadata = new("2.0.0-test", "abc1234", DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture));

    /// <summary>
    /// Gets metadata provider that always returns <see cref="SnapshotMetadata"/>.
    /// </summary>
    internal static IMetadataProvider Instance { get; } = new FixedMetadataProvider(SnapshotMetadata);

    /// <summary>
    /// Fixed metadata provider used by snapshot tests.
    /// </summary>
    private sealed class FixedMetadataProvider : IMetadataProvider
    {
        private readonly ReportMetadata _metadata;

        internal FixedMetadataProvider(ReportMetadata metadata)
        {
            _metadata = metadata;
        }

        public ReportMetadata GetMetadata() => _metadata;
    }
}
