using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzApi;

/// <summary>
/// Regression tests for AzApi comparison policy extraction.
/// Related feature: docs/features/110-refactoring-opportunities/test-plan.md.
/// </summary>
[Category("Unit")]
public class AzApiBodyComparisonPolicyTests
{
    [Test]
    public async Task AzApiRenderModel_AllPolicyCapturedBeforeEmission()
    {
        var source = await File.ReadAllTextAsync(GetRepositoryFilePath(
            "src",
            "Oocx.TfPlan2Md",
            "Providers",
            "AzApi",
            "Helpers",
            "AzApiBodyRenderer.cs"));

        var updateMethodStart = source.IndexOf("internal static void RenderUpdateBody", StringComparison.Ordinal);
        var plannerCall = source.IndexOf("AzApiBodyRenderPlanner.BuildUpdatePlan", updateMethodStart, StringComparison.Ordinal);
        var headingCall = source.IndexOf("WriteHeading(writer, heading);", updateMethodStart, StringComparison.Ordinal);

        plannerCall.Should().BeGreaterThan(updateMethodStart);
        headingCall.Should().BeGreaterThan(plannerCall);
        source[headingCall..].Should().NotContain("AzApiBodyRenderPlanner", because: "render policy should be fully computed before markdown emission begins");
    }

    [Test]
    [Arguments("identical-bodies", "{\"properties\":{\"name\":\"demo\"}}", "{\"properties\":{\"name\":\"demo\"}}", null)]
    [Arguments("single-path-changed", "{\"properties\":{\"name\":\"demo\"}}", "{\"properties\":{\"name\":\"demo-2\"}}", null)]
    [Arguments("nested-object-changed", "{\"properties\":{\"settings\":{\"alpha\":\"one\",\"beta\":\"two\",\"gamma\":\"three\"}}}", "{\"properties\":{\"settings\":{\"alpha\":\"updated\",\"beta\":\"two\",\"gamma\":\"three\"}}}", null)]
    [Arguments("sensitive-value-masked", "{\"properties\":{\"secret\":\"top-secret\"}}", "{\"properties\":{\"secret\":\"top-secret\"}}", "{\"properties\":{\"secret\":true}}")]
    [Arguments("all-paths-deleted", "{\"properties\":{\"name\":\"demo\",\"count\":1}}", "{}", null)]
    public void AzApiBodyComparisonPolicy_Evaluate_ScenarioMatrix(
        string scenario,
        string beforeJson,
        string afterJson,
        string? sensitivityJson)
    {
        var plan = AzApiBodyRenderPlanner.BuildUpdatePlan(
            ParseJson(beforeJson),
            ParseJson(afterJson),
            sensitivityJson is null ? null : ParseJson(sensitivityJson),
            sensitivityJson is null ? null : ParseJson(sensitivityJson),
            showSensitive: false,
            ignoreAzureIdCaseChanges: false);

        switch (scenario)
        {
            case "identical-bodies":
                plan.HasChanges.Should().BeFalse();
                break;
            case "single-path-changed":
                plan.HasChanges.Should().BeTrue();
                plan.TableProperties.Should().ContainSingle(property => property.DisplayPath == "name" && property.IsChanged);
                break;
            case "nested-object-changed":
                plan.HasChanges.Should().BeTrue();
                plan.PrefixGroups.Should().ContainSingle(group => group.Prefix == "settings");
                plan.PrefixGroups[0].Properties.Should().Contain(property => property.DisplayPath == "alpha" && property.IsChanged);
                break;
            case "sensitive-value-masked":
                plan.HasChanges.Should().BeTrue();
                plan.TableProperties.Should().ContainSingle(property => property.DisplayPath == "secret" && property.IsSensitive && property.IsChanged);
                break;
            case "all-paths-deleted":
                plan.HasChanges.Should().BeTrue();
                plan.TableProperties.Should().HaveCount(2);
                plan.TableProperties.Select(property => property.DisplayPath).Should().BeEquivalentTo(["count", "name"]);
                break;
            default:
                throw new InvalidOperationException($"Unhandled scenario '{scenario}'.");
        }
    }

    private static object ParseJson(string json)
    {
        return JsonSerializer.Deserialize<object>(json)!;
    }

    private static string GetRepositoryFilePath(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../..", Path.Combine(segments)));
    }
}
