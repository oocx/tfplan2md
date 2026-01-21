using System.Globalization;
using System.Xml.Linq;

namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Parses Cobertura XML reports to extract repository-wide coverage metrics.
/// Related feature: docs/features/043-code-coverage-ci/specification.md
/// </summary>
internal sealed class CoberturaCoverageParser
{
    /// <summary>
    /// Parses the specified Cobertura report file and returns coverage metrics.
    /// </summary>
    /// <param name="reportPath">Path to the Cobertura XML report.</param>
    /// <returns>Coverage metrics parsed from the report.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the report file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the report is missing required attributes.</exception>
    internal CoverageMetrics Parse(string reportPath)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new InvalidDataException("Cobertura report path must be provided.");
        }

        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("Cobertura report file not found.", reportPath);
        }

        try
        {
            var document = XDocument.Load(reportPath);
            return ParseDocument(document, reportPath);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not FileNotFoundException)
        {
            throw new InvalidDataException("Cobertura report is malformed or unreadable.", exception);
        }
    }

    /// <summary>
    /// Parses a Cobertura XML document into coverage metrics.
    /// </summary>
    /// <param name="document">Loaded Cobertura XML document.</param>
    /// <param name="reportPath">Path to the report for diagnostics.</param>
    /// <returns>Coverage metrics parsed from the document.</returns>
    /// <exception cref="InvalidDataException">Thrown when required attributes are missing or invalid.</exception>
    private static CoverageMetrics ParseDocument(XDocument document, string reportPath)
    {
        var root = document.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "coverage", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Cobertura report root element must be <coverage>.");
        }

        var lineRate = ReadRateAttribute(root, "line-rate", reportPath);
        var branchRate = ReadOptionalRateAttribute(root, "branch-rate") ?? 0m;

        var linePercentage = RoundPercentage(lineRate * 100m);
        var branchPercentage = RoundPercentage(branchRate * 100m);

        return new CoverageMetrics(linePercentage, branchPercentage);
    }

    /// <summary>
    /// Reads a required rate attribute and converts it to a decimal fraction.
    /// </summary>
    /// <param name="element">XML element containing the attribute.</param>
    /// <param name="attributeName">Attribute name to read.</param>
    /// <param name="reportPath">Path to the report for diagnostics.</param>
    /// <returns>Rate expressed as a decimal fraction (0-1).</returns>
    /// <exception cref="InvalidDataException">Thrown when the attribute is missing or invalid.</exception>
    private static decimal ReadRateAttribute(XElement element, string attributeName, string reportPath)
    {
        var value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Cobertura report '{reportPath}' missing required '{attributeName}' attribute.");
        }

        if (!TryParseRate(value, out var parsed))
        {
            throw new InvalidDataException($"Cobertura report '{reportPath}' contains invalid '{attributeName}' value '{value}'.");
        }

        return parsed;
    }

    /// <summary>
    /// Reads an optional rate attribute and converts it to a decimal fraction.
    /// </summary>
    /// <param name="element">XML element containing the attribute.</param>
    /// <param name="attributeName">Attribute name to read.</param>
    /// <returns>Rate expressed as a decimal fraction (0-1) or null when missing.</returns>
    /// <exception cref="InvalidDataException">Thrown when the attribute value is invalid.</exception>
    private static decimal? ReadOptionalRateAttribute(XElement element, string attributeName)
    {
        var value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!TryParseRate(value, out var parsed))
        {
            throw new InvalidDataException($"Cobertura report contains invalid '{attributeName}' value '{value}'.");
        }

        return parsed;
    }

    /// <summary>
    /// Attempts to parse a Cobertura rate value using invariant culture.
    /// </summary>
    /// <param name="value">Rate string to parse.</param>
    /// <param name="rate">Parsed rate as a decimal fraction.</param>
    /// <returns>True when parsing succeeds; otherwise false.</returns>
    private static bool TryParseRate(string value, out decimal rate)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out rate);
    }

    /// <summary>
    /// Rounds a percentage value to two decimal places using midpoint rounding away from zero.
    /// </summary>
    /// <param name="value">Percentage value to round.</param>
    /// <returns>Rounded percentage value.</returns>
    private static decimal RoundPercentage(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
