using System;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Parses severity levels from CLI input into normalized severity values.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class CodeAnalysisSeverityParser
{
    /// <summary>
    /// Attempts to parse a severity string into a <see cref="CodeAnalysisSeverity"/>.
    /// </summary>
    /// <param name="value">The severity string to parse.</param>
    /// <param name="severity">The parsed severity when successful.</param>
    /// <returns><c>true</c> when parsing succeeds; otherwise <c>false</c>.</returns>
    internal static bool TryParse(string? value, out CodeAnalysisSeverity severity)
    {
        severity = CodeAnalysisSeverity.Informational;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Equals("critical", StringComparison.OrdinalIgnoreCase))
        {
            severity = CodeAnalysisSeverity.Critical;
            return true;
        }

        if (value.Equals("high", StringComparison.OrdinalIgnoreCase))
        {
            severity = CodeAnalysisSeverity.High;
            return true;
        }

        if (value.Equals("medium", StringComparison.OrdinalIgnoreCase))
        {
            severity = CodeAnalysisSeverity.Medium;
            return true;
        }

        if (value.Equals("low", StringComparison.OrdinalIgnoreCase))
        {
            severity = CodeAnalysisSeverity.Low;
            return true;
        }

        if (value.Equals("informational", StringComparison.OrdinalIgnoreCase)
            || value.Equals("info", StringComparison.OrdinalIgnoreCase))
        {
            severity = CodeAnalysisSeverity.Informational;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a severity string into a nullable severity value.
    /// </summary>
    /// <param name="value">The severity string to parse.</param>
    /// <returns>The parsed severity, or <c>null</c> when parsing fails.</returns>
    internal static CodeAnalysisSeverity? ParseOptional(string? value)
    {
        return TryParse(value, out var severity) ? severity : null;
    }
}
