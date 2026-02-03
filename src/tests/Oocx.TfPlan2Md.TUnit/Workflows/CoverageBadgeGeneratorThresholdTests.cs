using Oocx.TfPlan2Md.CoverageEnforcer;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Workflows;

/// <summary>
/// Validates badge color thresholds for coverage reporting.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
public class CoverageBadgeGeneratorThresholdTests
{
    /// <summary>
    /// Verifies the highest tier color is used for coverage at or above 90%.
    /// </summary>
    [Test]
    public async Task Badge_generator_uses_green_for_high_coverage()
    {
        var generator = new CoverageBadgeGenerator();

        var svg = generator.GenerateSvg(95m);

        await Assert.That(svg).Contains("#4c1", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the 70% tier color is used for medium coverage.
    /// </summary>
    [Test]
    public async Task Badge_generator_uses_yellow_for_seventies()
    {
        var generator = new CoverageBadgeGenerator();

        var svg = generator.GenerateSvg(72m);

        await Assert.That(svg).Contains("#dfb317", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the 60% tier color is used for lower coverage.
    /// </summary>
    [Test]
    public async Task Badge_generator_uses_orange_for_sixties()
    {
        var generator = new CoverageBadgeGenerator();

        var svg = generator.GenerateSvg(62m);

        await Assert.That(svg).Contains("#fe7d37", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the red color is used when coverage is below 60%.
    /// </summary>
    [Test]
    public async Task Badge_generator_uses_red_for_low_coverage()
    {
        var generator = new CoverageBadgeGenerator();

        var svg = generator.GenerateSvg(50m);

        await Assert.That(svg).Contains("#e05d44", StringComparison.Ordinal);
    }
}
