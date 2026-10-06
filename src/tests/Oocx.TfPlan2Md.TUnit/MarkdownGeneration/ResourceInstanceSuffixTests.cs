using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ResourceInstanceSuffixTests
{
    [Test]
    public void Build_ResourceHeadersPreserveExactSuffixForAllActions()
    {
        var addressCases = new (string Address, string ModuleAddress, string ExpectedHeader, string Label)[]
        {
            (
                "module.environment[\"blue.east\"].module.apps[\"api[0]\"].example_resource.worker[\"key.with.[brackets].\\\"quoted\\\".\\\\path\"]",
                "module.environment[\"blue.east\"].module.apps[\"api[0]\"]",
                "<code>worker[\"key.with.[brackets].\\\"quoted\\\".\\\\path\"]</code>",
                "string key"),
            (
                "module.environments[2].module.apps[1].example_resource.worker[0]",
                "module.environments[2].module.apps[1]",
                "<code>worker[0]</code>",
                "numeric key")
        };
        var actions = new (string Label, string[] Actions, bool Import, bool Move)[]
        {
            ("create", ["create"], false, false),
            ("update", ["update"], false, false),
            ("replace delete-create", ["delete", "create"], false, false),
            ("replace create-delete", ["create", "delete"], false, false),
            ("delete", ["delete"], false, false),
            ("import", ["create"], true, false),
            ("move", ["create"], false, true)
        };

        foreach (var addressCase in addressCases)
        {
            foreach (var scenario in actions)
            {
                var report = BuildReport(
                [
                    CreateChange(
                        addressCase.Address,
                        "example_resource",
                        "worker",
                        scenario.Actions,
                        beforeJson: scenario.Actions.Contains("delete") || scenario.Actions.Contains("update") ? "{\"marker\":\"before\"}" : null,
                        afterJson: scenario.Actions.Contains("create") || scenario.Actions.Contains("update") ? "{\"marker\":\"after\"}" : null,
                        importId: scenario.Import ? "resource-id" : null,
                        previousAddress: scenario.Move ? "module.old.example_resource.worker" : null,
                        moduleAddress: addressCase.ModuleAddress)
                ]);

                var renderedChange = report.Changes.Count > 0 ? report.Changes[0] : null;
                renderedChange.Should().NotBeNull($"{scenario.Label} with {addressCase.Label}");
                renderedChange!.SummaryHtml.Should().Contain(addressCase.ExpectedHeader, $"{scenario.Label} with {addressCase.Label}");
                renderedChange.ModuleAddress.Should().Be(addressCase.ModuleAddress);
                report.ModuleChanges.Should().ContainSingle(group => group.ModuleAddress == addressCase.ModuleAddress);
            }
        }
    }

    [Test]
    public void Build_ResourceHeadersKeepNumericIndexesAndIgnoreIndexedModuleNames()
    {
        var indexedResource = BuildReport(
        [
            CreateChange(
                "module.environments[2].example_resource.worker[0]",
                "example_resource",
                "worker",
                ["create"],
                afterJson: "{}",
                moduleAddress: "module.environments[2]")
        ]).Changes.Single();
        var indexedModuleOnly = BuildReport(
        [
            CreateChange(
                "module.environments[2].example_resource.worker",
                "example_resource",
                "worker",
                ["create"],
                afterJson: "{}",
                moduleAddress: "module.environments[2]")
        ]).Changes.Single();

        indexedResource.SummaryHtml.Should().Contain("<code>worker[0]</code>");
        indexedResource.ModuleAddress.Should().Be("module.environments[2]");
        indexedModuleOnly.SummaryHtml.Should().Contain("<code>worker</code>");
        indexedModuleOnly.SummaryHtml.Should().NotContain("environments[2]");
    }

    [Test]
    public void Build_ProviderOwnedSummaryHeaderPreservesInstanceSuffix()
    {
        var report = BuildReport(
        [
            CreateChange(
                "module.api.azurerm_api_management_api_operation.list[2]",
                "azurerm_api_management_api_operation",
                "list",
                ["create"],
                afterJson: "{}",
                moduleAddress: "module.api")
        ]);

        report.Changes.Single().SummaryHtml.Should().Contain("<b><code>list[2]</code></b>");
    }

    [Test]
    public void Render_RefactoringLabelsUseFullAddressAndSharedVisibleIdentity()
    {
        const string genericAddress = "module.identity.example_resource.policy[\"one.two\"]";
        const string mappedAddress = "module.identity.msgraph_resource.policy[2]";
        const string sensitiveAddress = "module.identity.example_resource.secret[0]";
        var report = BuildReport(
        [
            CreateChange(
                genericAddress,
                "example_resource",
                "policy",
                ["no-op"],
                afterJson: "{\"displayName\":\"Friendly <policy> & owners | ops\"}",
                importId: "generic-id",
                previousAddress: "module.old.example_resource.policy[\"one.two\"]",
                moduleAddress: "module.identity"),
            CreateChange(
                mappedAddress,
                "msgraph_resource",
                "policy",
                ["no-op"],
                afterJson: "{\"url\":\"users\",\"body\":{\"displayName\":\"Mapped policy\",\"title\":\"Ignored override\"}}",
                importId: "mapped-id",
                previousAddress: "module.old.msgraph_resource.policy[2]",
                moduleAddress: "module.identity"),
            CreateChange(
                sensitiveAddress,
                "example_resource",
                "secret",
                ["no-op"],
                afterJson: "{\"displayName\":\"must-not-leak\"}",
                afterSensitiveJson: "{\"displayName\":true}",
                importId: "sensitive-id",
                moduleAddress: "module.identity")
        ]);

        var markdown = new ReportRenderer().Render(
            report,
            new RenderContext(
                showSensitive: false,
                showUnchangedValues: false,
                ignoreAzureIdCaseChanges: true,
                renderTarget: RenderTarget.GitHub,
                detailsDisplayMode: DetailsDisplayMode.Auto));

        markdown.Should().Contain($"`{genericAddress}` — <code>Friendly &lt;policy&gt; &amp; owners &#124; ops</code>");
        markdown.Should().Contain($"`{mappedAddress}` — <code>Mapped policy</code>");
        report.RefactoringOperations.Where(operation => operation.Address == mappedAddress)
            .Should()
            .OnlyContain(operation => operation.DisplayIdentity == "Mapped policy");
        markdown.Should().Contain($"`{sensitiveAddress}`");
        markdown.Should().NotContain("must-not-leak");
        report.RefactoringOperations.Should().HaveCount(5);
        report.ModuleChanges.Should().ContainSingle(group => group.ModuleAddress == "module.identity");
    }

    private static ReportModel BuildReport(IReadOnlyList<ResourceChange> changes)
    {
        var plan = new TerraformPlan("1.0", "1.0", changes);
        return new ReportModelBuilder().Build(plan);
    }

    private static ResourceChange CreateChange(
        string address,
        string type,
        string name,
        IReadOnlyList<string> actions,
        string? beforeJson = null,
        string? afterJson = null,
        string? afterSensitiveJson = null,
        string? importId = null,
        string? previousAddress = null,
        string? moduleAddress = null)
    {
        var change = new Change(
            actions,
            before: ParseJson(beforeJson),
            after: ParseJson(afterJson),
            afterUnknown: null,
            beforeSensitive: null,
            afterSensitive: ParseJson(afterSensitiveJson),
            importing: importId is null ? null : new Importing { Id = importId });
        return new ResourceChange(
            address,
            moduleAddress,
            "managed",
            type,
            name,
            "registry.terraform.io/example/provider",
            change,
            PreviousAddress: previousAddress);
    }

    private static JsonElement? ParseJson(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
