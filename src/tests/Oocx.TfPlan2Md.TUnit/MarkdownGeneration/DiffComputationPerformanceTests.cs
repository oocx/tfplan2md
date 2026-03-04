using System.Diagnostics;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.RenderTargets.AzureDevOps;
using TUnit.Core;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests that diff computation completes in reasonable time for large inputs,
/// preventing O(m×n) blowup with the LCS algorithm.
/// </summary>
public class DiffComputationPerformanceTests
{
    [Test]
    public void FormatLargeValue_WithLargeMinifiedJson_CompletesWithinTimeLimit()
    {
        // Simulate a realistic scenario: two large minified JSON IAM policies
        // differing by a few characters. Without the size guard, this would take
        // O(50000²) = 2.5 billion iterations for character-level LCS.
        var before = GenerateLargeJsonPolicy(50_000, seed: 1);
        var after = GenerateLargeJsonPolicy(50_000, seed: 2);

        var stopwatch = Stopwatch.StartNew();
        var result = FormatLargeValue(before, after, "inline-diff");
        stopwatch.Stop();

        result.Should().NotBeNullOrEmpty();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "diff of large values should complete quickly due to LCS size guard");
    }

    [Test]
    public void FormatDiff_AzureDevOps_WithLargeValues_CompletesWithinTimeLimit()
    {
        // Test the full AzureDevOpsDiffFormatter path with large values
        var before = GenerateLargeJsonPolicy(50_000, seed: 1);
        var after = GenerateLargeJsonPolicy(50_000, seed: 2);

        var formatter = new AzureDevOpsDiffFormatter();

        var stopwatch = Stopwatch.StartNew();
        var result = formatter.FormatDiff(before, after);
        stopwatch.Stop();

        result.Should().NotBeNullOrEmpty();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "AzureDevOps diff of large values should complete quickly due to LCS size guard");
    }

    [Test]
    public void FormatLargeValue_WithSmallValues_StillProducesCharacterLevelDiff()
    {
        // Verify that small values still get the full character-level diff treatment
        var result = FormatLargeValue("abc", "abz", "inline-diff");

        // Should contain character-level highlighting (not just line-level)
        result.Should().Contain("background-color: #ffc0c0"); // Removed char highlight
        result.Should().Contain("background-color: #acf2bd"); // Added char highlight
    }

    [Test]
    public void LargeAttributesSummary_WithLargeValues_CompletesWithinTimeLimit()
    {
        // CountChangedLines internally calls BuildLineDiff → ComputeLcsPairs
        var before = GenerateLargeJsonPolicy(50_000, seed: 1);
        var after = GenerateLargeJsonPolicy(50_000, seed: 2);

        var attrs = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["name"] = "policy",
                ["before"] = before,
                ["after"] = after
            }
        };

        var stopwatch = Stopwatch.StartNew();
        var result = LargeAttributesSummary(attrs);
        stopwatch.Stop();

        result.Should().NotBeNullOrEmpty();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "large attributes summary should complete quickly due to LCS size guard");
    }

    /// <summary>
    /// Generates a large JSON-like policy string for performance testing.
    /// Different seeds produce different content to ensure the diff is non-trivial.
    /// </summary>
    private static string GenerateLargeJsonPolicy(int targetLength, int seed)
    {
        var random = new Random(seed);
        var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[targetLength];
        for (var i = 0; i < targetLength; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new string(result);
    }
}
