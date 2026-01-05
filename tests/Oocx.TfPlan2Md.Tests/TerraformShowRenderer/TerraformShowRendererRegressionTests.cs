using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;
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
    [Fact]
    public async Task Render_Plan1_MatchesBaselineAsync()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan1.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan1.txt"));

        Assert.Equal(expected, output);
    }

    /// <summary>
    /// Ensures plan2 rendering matches the recorded terraform show output including replacement markers.
    /// </summary>
    [Fact]
    public async Task Render_Plan2_MatchesBaselineAsync()
    {
        var (plan, outputChanges) = await ParsePlanAsync("plan2.json");
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false, outputChanges);
        var expected = await File.ReadAllTextAsync(Path.Combine("TestData", "TerraformShow", "plan2.txt"));

        Assert.Equal(expected, output);
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
}
