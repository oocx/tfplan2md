using System.Globalization;

namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Generates a simple SVG badge for coverage reporting.
/// Related feature: docs/features/043-code-coverage-ci/specification.md
/// </summary>
internal sealed class CoverageBadgeGenerator
{
    /// <summary>
    /// Generates an SVG badge string for the provided coverage percentage.
    /// </summary>
    /// <param name="coveragePercentage">Coverage percentage in the 0-100 range.</param>
    /// <returns>SVG markup for the badge.</returns>
    internal string GenerateSvg(decimal coveragePercentage)
    {
        var valueText = string.Create(CultureInfo.InvariantCulture, $"{coveragePercentage:0.00}%");
        var color = ResolveColor(coveragePercentage);

        return $"""
<svg xmlns="http://www.w3.org/2000/svg" width="120" height="20" role="img" aria-label="coverage: {valueText}">
  <linearGradient id="s" x2="0" y2="100%">
    <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
    <stop offset="1" stop-opacity=".1"/>
  </linearGradient>
  <clipPath id="r">
    <rect width="120" height="20" rx="3" fill="#fff"/>
  </clipPath>
  <g clip-path="url(#r)">
    <rect width="60" height="20" fill="#555"/>
    <rect x="60" width="60" height="20" fill="{color}"/>
    <rect width="120" height="20" fill="url(#s)"/>
  </g>
  <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" font-size="11">
    <text x="30" y="14">coverage</text>
    <text x="90" y="14">{valueText}</text>
  </g>
</svg>
""";
    }

    /// <summary>
    /// Resolves the badge color based on the coverage percentage.
    /// </summary>
    /// <param name="coveragePercentage">Coverage percentage in the 0-100 range.</param>
    /// <returns>Hex color string.</returns>
    private static string ResolveColor(decimal coveragePercentage)
    {
        return coveragePercentage switch
        {
            >= 90m => "#4c1",
            >= 80m => "#97ca00",
            >= 70m => "#dfb317",
            >= 60m => "#fe7d37",
            _ => "#e05d44",
        };
    }
}
