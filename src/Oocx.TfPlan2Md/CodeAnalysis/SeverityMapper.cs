using System;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Derives normalized severity levels for code analysis findings.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class SeverityMapper
{
    /// <summary>
    /// Derives a normalized severity for a SARIF finding using the hybrid mapping strategy.
    /// </summary>
    /// <param name="finding">The finding to evaluate.</param>
    /// <returns>The derived severity level.</returns>
    internal static CodeAnalysisSeverity DeriveSeverity(CodeAnalysisFinding finding)
    {
        if (finding.SecuritySeverity is not null)
        {
            return MapSecuritySeverity(finding.SecuritySeverity.Value);
        }

        if (finding.Rank is not null)
        {
            return MapRankSeverity(finding.Rank.Value);
        }

        return MapSarifLevel(finding.Level);
    }

    /// <summary>
    /// Maps numeric security severity values to normalized severity buckets.
    /// </summary>
    /// <param name="securitySeverity">The security severity value reported by SARIF.</param>
    /// <returns>The mapped severity level.</returns>
    private static CodeAnalysisSeverity MapSecuritySeverity(double securitySeverity)
    {
        if (securitySeverity >= 9.0)
        {
            return CodeAnalysisSeverity.Critical;
        }

        if (securitySeverity >= 7.0)
        {
            return CodeAnalysisSeverity.High;
        }

        if (securitySeverity >= 4.0)
        {
            return CodeAnalysisSeverity.Medium;
        }

        if (securitySeverity >= 1.0)
        {
            return CodeAnalysisSeverity.Low;
        }

        return CodeAnalysisSeverity.Informational;
    }

    /// <summary>
    /// Maps SARIF rank values to normalized severity buckets using best-effort thresholds.
    /// </summary>
    /// <param name="rank">The SARIF rank value.</param>
    /// <returns>The mapped severity level.</returns>
    private static CodeAnalysisSeverity MapRankSeverity(double rank)
    {
        if (double.IsNaN(rank) || double.IsInfinity(rank))
        {
            return CodeAnalysisSeverity.Informational;
        }

        var normalized = NormalizeRank(rank);
        if (normalized <= 10.0)
        {
            return CodeAnalysisSeverity.Critical;
        }

        if (normalized <= 30.0)
        {
            return CodeAnalysisSeverity.High;
        }

        if (normalized <= 60.0)
        {
            return CodeAnalysisSeverity.Medium;
        }

        if (normalized <= 90.0)
        {
            return CodeAnalysisSeverity.Low;
        }

        return CodeAnalysisSeverity.Informational;
    }

    /// <summary>
    /// Normalizes rank values into a 0-100 scale to align with threshold comparisons.
    /// </summary>
    /// <param name="rank">The rank value supplied by SARIF.</param>
    /// <returns>The normalized rank value.</returns>
    private static double NormalizeRank(double rank)
    {
        var normalized = rank <= 1.0 ? rank * 100.0 : rank;
        return Math.Clamp(normalized, 0.0, 100.0);
    }

    /// <summary>
    /// Maps SARIF result level strings to normalized severities.
    /// </summary>
    /// <param name="level">The SARIF result level.</param>
    /// <returns>The mapped severity level.</returns>
    private static CodeAnalysisSeverity MapSarifLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return CodeAnalysisSeverity.Informational;
        }

        if (level.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return CodeAnalysisSeverity.High;
        }

        if (level.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return CodeAnalysisSeverity.Medium;
        }

        if (level.Equals("note", StringComparison.OrdinalIgnoreCase))
        {
            return CodeAnalysisSeverity.Low;
        }

        return CodeAnalysisSeverity.Informational;
    }
}
