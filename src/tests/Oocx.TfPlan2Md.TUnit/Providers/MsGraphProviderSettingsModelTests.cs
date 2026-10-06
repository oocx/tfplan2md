using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers.MsGraph;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.MsGraph;

/// <summary>
/// Verifies that Microsoft Graph metadata is classified before primary summaries and counts are built.
/// </summary>
public class MsGraphProviderSettingsModelTests
{
    /// <summary>
    /// Verifies desired provider settings are excluded from primary create rows in both unchanged-value modes.
    /// </summary>
    [Test]
    [Arguments("msgraph_resource", false)]
    [Arguments("msgraph_resource", true)]
    [Arguments("msgraph_update_resource", false)]
    [Arguments("msgraph_update_resource", true)]
    public void Build_MsGraphCreate_ExcludesDesiredSettingsFromPrimaryChanges(string resourceType, bool showUnchangedValues)
    {
        var model = BuildModel(
            resourceType,
            ["create"],
            before: null,
            after: CreateState("v1.0", "/groups/legacy", false, "New Group"),
            showUnchangedValues);

        var change = model.Changes.Single();

        change.AttributeChanges.Select(attribute => attribute.Name)
            .Should()
            .NotContain(["api_version", "url", "ignore_missing_property"]);
        change.AttributeChanges.Should().ContainSingle(attribute => attribute.Name == "body.displayName");
        change.SecondaryAttributeChanges.Select(attribute => attribute.Name)
            .Should()
            .BeEquivalentTo("api_version", "url", "ignore_missing_property");
    }

    /// <summary>
    /// Verifies unchanged settings do not inflate the primary changed-attribute count.
    /// </summary>
    [Test]
    [Arguments("msgraph_resource", false)]
    [Arguments("msgraph_resource", true)]
    [Arguments("msgraph_update_resource", false)]
    [Arguments("msgraph_update_resource", true)]
    public void Build_MsGraphUpdate_UnchangedSettingsDoNotInflatePrimaryChanges(string resourceType, bool showUnchangedValues)
    {
        var model = BuildModel(
            resourceType,
            ["update"],
            before: CreateState("v1.0", "/groups/legacy", false, "Before"),
            after: CreateState("v1.0", "/groups/legacy", false, "After"),
            showUnchangedValues);

        var change = model.Changes.Single();

        change.AttributeChanges.Select(attribute => attribute.Name)
            .Should()
            .BeEquivalentTo("body.displayName");
        change.SecondaryAttributeChanges.Select(attribute => attribute.Name)
            .Should()
            .BeEquivalentTo("api_version", "url", "ignore_missing_property");
    }

    /// <summary>
    /// Verifies changes to each setting remain primary while unchanged companions are omitted from the count.
    /// </summary>
    [Test]
    public void Build_PopulatedMsGraphImportsWithChangedSettings_CountEachChangedRowOnce()
    {
        var settings = new[] { "api_version", "url", "ignore_missing_property" };
        var resourceTypes = new[] { "msgraph_resource", "msgraph_update_resource" };

        foreach (var resourceType in resourceTypes)
        {
            foreach (var setting in settings)
            {
                foreach (var changeKind in new[] { "updated", "added", "removed" })
                {
                    var before = new Dictionary<string, object?>
                    {
                        ["api_version"] = "v1.0",
                        ["url"] = "/groups/legacy",
                        ["ignore_missing_property"] = false,
                        ["body"] = new { displayName = "Before" }
                    };
                    var after = new Dictionary<string, object?>(before)
                    {
                        ["body"] = new { displayName = "After" }
                    };

                    if (changeKind == "added")
                    {
                        before.Remove(setting);
                    }
                    else if (changeKind == "removed")
                    {
                        after.Remove(setting);
                    }
                    else
                    {
                        after[setting] = ChangedSettingValue(setting);
                    }

                    var model = BuildModel(resourceType, ["update"], before, after, showUnchangedValues: true, isImport: true);
                    var change = model.Changes.Single();
                    var changedNames = change.AttributeChanges.Select(attribute => attribute.Name).ToArray();

                    changedNames.Should().Contain(setting);
                    changedNames.Should().Contain("body.displayName");
                    changedNames.Should().HaveCount(2);
                    change.SecondaryAttributeChanges.Select(attribute => attribute.Name)
                        .Should()
                        .BeEquivalentTo(settings.Where(name => name != setting));
                }
            }
        }
    }

    /// <summary>
    /// Verifies provider housekeeping values do not make an imported Graph prior state meaningful.
    /// </summary>
    [Test]
    [Arguments("msgraph_resource")]
    [Arguments("msgraph_update_resource")]
    public void Build_MsGraphImportWithHousekeepingOnlyPriorState_IsClassifiedEmpty(string resourceType)
    {
        var model = BuildModel(
            resourceType,
            ["create"],
            before: """{"id":"existing-id","api_version":"v1.0","url":"/groups/legacy","ignore_missing_property":false,"body":{}}""",
            after: CreateState("v1.0", "/groups/legacy", false, "Imported Group"),
            showUnchangedValues: false,
            isImport: true);

        model.Changes.Single().IsImportedWithEmptyPriorState.Should().BeTrue();
        model.Changes.Single().SecondaryAttributeChanges.Select(attribute => attribute.Name)
            .Should()
            .BeEquivalentTo("api_version", "url", "ignore_missing_property");
    }

    /// <summary>
    /// Verifies matching attribute names on other resource types retain their ordinary primary presentation.
    /// </summary>
    [Test]
    public void Build_NonMsGraphResourceWithMatchingSettings_KeepsOrdinaryChanges()
    {
        var model = BuildModel(
            "example_resource",
            ["create"],
            before: null,
            after: CreateState("v1.0", "/groups/legacy", false, "New Group"),
            showUnchangedValues: false);

        model.Changes.Single().AttributeChanges.Select(attribute => attribute.Name)
            .Should()
            .Contain(["api_version", "url", "ignore_missing_property"]);
        model.Changes.Single().SecondaryAttributeChanges.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies absent provider settings do not create primary or secondary placeholder rows.
    /// </summary>
    [Test]
    [Arguments("msgraph_resource")]
    [Arguments("msgraph_update_resource")]
    public void Build_MsGraphCreateWithMissingSettings_DoesNotCreatePhantomRows(string resourceType)
    {
        var model = BuildModel(
            resourceType,
            ["create"],
            before: null,
            after: """{"body":{"displayName":"New Group"}}""",
            showUnchangedValues: false);

        var change = model.Changes.Single();

        change.AttributeChanges.Select(attribute => attribute.Name)
            .Should()
            .BeEquivalentTo("body.displayName");
        change.SecondaryAttributeChanges.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies secondary settings retain sensitivity masking and computed-value labels.
    /// </summary>
    [Test]
    public void Build_MsGraphCreateWithSensitiveAndUnknownSettings_PreservesValueProtection()
    {
        const string desired = """{"api_version":null,"url":"do-not-leak","ignore_missing_property":false,"body":{"displayName":"New Group"}}""";
        const string sensitive = """{"url":true}""";
        const string unknown = """{"api_version":true}""";

        var masked = BuildModel(
            "msgraph_resource",
            ["create"],
            before: null,
            after: desired,
            showUnchangedValues: false,
            afterSensitive: sensitive,
            afterUnknown: unknown).Changes.Single();

        masked.SecondaryAttributeChanges.Single(attribute => attribute.Name == "url").After.Should().Be("(sensitive)");
        masked.SecondaryAttributeChanges.Single(attribute => attribute.Name == "api_version").After.Should().Be("(known after apply)");
        JsonSerializer.Serialize(masked.SecondaryAttributeChanges).Should().NotContain("do-not-leak");

        var visible = BuildModel(
            "msgraph_resource",
            ["create"],
            before: null,
            after: desired,
            showUnchangedValues: false,
            showSensitive: true,
            afterSensitive: sensitive,
            afterUnknown: unknown).Changes.Single();

        visible.SecondaryAttributeChanges.Single(attribute => attribute.Name == "url").After.Should().Be("do-not-leak");
    }

    /// <summary>
    /// Builds a report model with the requested resource states and display options.
    /// </summary>
    /// <param name="resourceType">Terraform resource type.</param>
    /// <param name="actions">Terraform actions for the resource.</param>
    /// <param name="before">Prior state or its JSON text.</param>
    /// <param name="after">Desired state or its JSON text.</param>
    /// <param name="showUnchangedValues">Whether unchanged attributes are included in core change rows.</param>
    /// <param name="isImport">Whether Terraform marks the resource as imported.</param>
    /// <returns>The built report model.</returns>
    private static ReportModel BuildModel(
        string resourceType,
        IReadOnlyList<string> actions,
        object? before,
        object? after,
        bool showUnchangedValues,
        bool isImport = false,
        bool showSensitive = false,
        object? afterSensitive = null,
        object? afterUnknown = null)
    {
        var resourceChange = new ResourceChange(
            $"{resourceType}.sample",
            null,
            "managed",
            resourceType,
            "sample",
            "registry.terraform.io/microsoft/msgraph",
            new Change(
                actions,
                ParseState(before),
                ParseState(after),
                ParseState(afterUnknown),
                null,
                ParseState(afterSensitive),
                importing: isImport ? new Importing { Id = "existing-id" } : null));

        var registry = new ProviderRegistry();
        registry.RegisterProvider(new MsGraphModule());
        return new ReportModelBuilder(
            new ReportModelBuilderOptions(ShowSensitive: showSensitive, ShowUnchangedValues: showUnchangedValues),
            new ReportModelBuilderServices(ProviderRegistry: registry))
            .Build(new TerraformPlan("1.0", "1.0", [resourceChange]));
    }

    /// <summary>
    /// Converts a string or object state to an owned JSON element for plan construction.
    /// </summary>
    /// <param name="state">State text, dictionary, or null.</param>
    /// <returns>A cloned JSON element or null.</returns>
    private static JsonElement? ParseState(object? state)
    {
        if (state is null)
        {
            return null;
        }

        using var document = state is string json
            ? JsonDocument.Parse(json)
            : JsonDocument.Parse(JsonSerializer.Serialize(state));
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Creates Graph resource state containing provider metadata and a display-name body value.
    /// </summary>
    /// <param name="apiVersion">The API version setting.</param>
    /// <param name="url">The resource URL setting.</param>
    /// <param name="ignoreMissingProperty">The missing-property behavior setting.</param>
    /// <param name="displayName">The desired resource display name.</param>
    /// <returns>The serialized resource state.</returns>
    private static string CreateState(string apiVersion, string url, bool ignoreMissingProperty, string displayName)
    {
        return JsonSerializer.Serialize(new
        {
            api_version = apiVersion,
            url,
            ignore_missing_property = ignoreMissingProperty,
            body = new { displayName }
        });
    }

    /// <summary>
    /// Gets a changed value appropriate for one Graph provider setting.
    /// </summary>
    /// <param name="setting">The provider setting name.</param>
    /// <returns>The changed setting value.</returns>
    private static object ChangedSettingValue(string setting)
    {
        return setting switch
        {
            "api_version" => "v1.1",
            "url" => "/groups/renamed",
            _ => true
        };
    }
}
