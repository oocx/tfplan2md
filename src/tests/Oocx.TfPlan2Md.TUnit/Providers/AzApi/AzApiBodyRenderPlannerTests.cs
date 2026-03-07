using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzApi;

/// <summary>
/// Unit tests for AzApi body render planning.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
[Category("Unit")]
public class AzApiBodyRenderPlannerTests
{
    /// <summary>
    /// Verifies casing-only Azure resource ID changes are filtered during planning when the flag is enabled.
    /// </summary>
    [Test]
    public async Task BuildUpdatePlan_AzureIdCasingOnlyChangeIgnored_HasNoVisibleChanges()
    {
        var before = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/APP-RG-GWC/providers/Microsoft.Compute/diskAccesses/app-gwc"
            }
            """);
        var after = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/app-rg-gwc/providers/Microsoft.Compute/diskAccesses/app-gwc"
            }
            """);

        var plan = AzApiBodyRenderPlanner.BuildUpdatePlan(before, after, null, null, showSensitive: false, ignoreAzureIdCaseChanges: true);

        plan.HasChanges.Should().BeFalse();
        plan.TableProperties.Should().BeEmpty();
        plan.PrefixGroups.Should().BeEmpty();
        plan.ArrayGroups.Should().BeEmpty();
        plan.LargeProperties.Should().BeEmpty();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies grouped prefix planning keeps the full group when one member changes.
    /// </summary>
    [Test]
    public async Task BuildUpdatePlan_GroupedPrefixChange_ReturnsFullPrefixGroup()
    {
        var before = ParseJson("""
            {
                "properties": {
                    "settings": {
                        "alpha": "one",
                        "beta": "two",
                        "gamma": "three"
                    }
                }
            }
            """);
        var after = ParseJson("""
            {
                "properties": {
                    "settings": {
                        "alpha": "updated",
                        "beta": "two",
                        "gamma": "three"
                    }
                }
            }
            """);

        var plan = AzApiBodyRenderPlanner.BuildUpdatePlan(before, after, null, null, showSensitive: false, ignoreAzureIdCaseChanges: false);

        plan.HasChanges.Should().BeTrue();
        plan.TableProperties.Should().BeEmpty();
        plan.PrefixGroups.Should().ContainSingle();
        plan.PrefixGroups[0].Prefix.Should().Be("settings");
        plan.PrefixGroups[0].Properties.Select(property => property.DisplayPath)
            .Should().BeEquivalentTo("alpha", "beta", "gamma");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies sensitivity decisions are captured in the create/delete plan before markdown emission.
    /// </summary>
    [Test]
    public async Task BuildCreateDeletePlan_SensitivePropertyIsFlagged()
    {
        var body = ParseJson("""
            {
                "properties": {
                    "name": "demo",
                    "secret": "top-secret"
                }
            }
            """);
        var sensitivity = ParseJson("""
            {
                "properties": {
                    "secret": true
                }
            }
            """);

        var plan = AzApiBodyRenderPlanner.BuildCreateDeletePlan(body, sensitivity, showSensitive: false);

        plan.TableProperties.Should().ContainSingle(property => property.DisplayPath == "name" && !property.IsSensitive);
        plan.TableProperties.Should().ContainSingle(property => property.DisplayPath == "secret" && property.IsSensitive);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Parses JSON into an object graph suitable for AzApi planning tests.
    /// </summary>
    /// <param name="json">JSON text.</param>
    /// <returns>The parsed object graph.</returns>
    private static object ParseJson(string json)
    {
        return JsonSerializer.Deserialize<object>(json)!;
    }
}
