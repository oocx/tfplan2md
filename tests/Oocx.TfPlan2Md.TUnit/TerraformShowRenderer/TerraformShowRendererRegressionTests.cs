using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.Assertions;
using Renderer = Oocx.TfPlan2Md.TerraformShowRenderer.Rendering.TerraformShowRenderer;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Regression coverage against captured terraform show baselines.
/// Related acceptance: TC-06, TC-07.
/// </summary>
public sealed class TerraformShowRendererRegressionTests
{
    /// <summary>
    /// Ensures plan1 rendering matches the recorded terraform show output.
    /// </summary>
    [Test]
    public async Task Render_Plan1_MatchesBaselineAsync()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan1.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan1.txt"));

        await Assert.That(output).IsEqualTo(expected);
    }

    /// <summary>
    /// Ensures plan2 rendering matches the recorded terraform show output including replacement markers.
    /// </summary>
    [Test]
    public async Task Render_Plan2_MatchesBaselineAsync()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan2.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan2.txt"));

        await Assert.That(output).IsEqualTo(expected);
    }

    /// <summary>
    /// Parses a Terraform plan JSON from the Terraform show regression folder.
    /// </summary>
    /// <param name="fileName">Plan file name to parse.</param>
    /// <returns>Parsed terraform plan model.</returns>
    private static async Task<(TerraformPlan Plan, JsonElement? OutputChanges)> ParsePlanAsync(string fileName)
    {
        var parser = new TerraformPlanParser();
        var json = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", fileName)).ConfigureAwait(false);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("output_changes", out var outputChanges);
        var outputChangesClone = outputChanges.ValueKind == JsonValueKind.Undefined ? (JsonElement?)null : outputChanges.Clone();

        var plan = await parser.ParseAsync(stream).ConfigureAwait(false);
        return (plan, outputChangesClone);
    }

    /// <summary>
    /// Ensures plan1 rendering contains the same content as baseline, ignoring leading whitespace.
    /// </summary>
    [Test]
    public async Task Render_Plan1_MatchesBaselineContent()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan1.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan1.txt"));

        TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, output);
    }

    /// <summary>
    /// Ensures plan2 rendering contains the same content as baseline, ignoring leading whitespace.
    /// </summary>
    [Test]
    public async Task Render_Plan2_MatchesBaselineContent()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan2.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan2.txt"));

        TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, output);
    }

    /// <summary>
    /// Ensures plan1 no-color rendering contains the same content as baseline, ignoring leading whitespace.
    /// </summary>
    [Test]
    public async Task Render_Plan1_NoColor_MatchesBaselineContent()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan1.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan1.nocolor.txt"));

        TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, output);
    }

    [Test]
    public async Task Render_Plan1_NoColor_HasNoConsecutiveEmptyLines()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan1.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true, outputChanges);

        var lines = output.Replace("\r\n", "\n").Split('\n');
        var previousBlank = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var isBlank = string.IsNullOrWhiteSpace(lines[i]);
            if (isBlank && previousBlank)
            {
                Assert.Fail($"Found consecutive blank lines at line {i}");
            }

            previousBlank = isBlank;
        }
    }

    /// <summary>
    /// Ensures plan2 no-color rendering contains the same content as baseline, ignoring leading whitespace.
    /// </summary>
    [Test]
    public async Task Render_Plan2_NoColor_MatchesBaselineContent()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan2.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan2.nocolor.txt"));

        TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, output);
    }
}
