using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class GenericResourceDisplayIdentityTests
{
    [Test]
    public void Build_DefaultOrderCoversAllSixCandidatePaths()
    {
        var name = BuildResource("{\"name\":\"name\",\"display_name\":\"display_name\",\"displayName\":\"displayName\",\"body\":{\"displayName\":\"body.displayName\",\"properties\":{\"displayName\":\"body.properties.displayName\"},\"name\":\"body.name\"}}");
        var displayName = BuildResource("{\"name\":null,\"display_name\":\"display_name\",\"displayName\":\"displayName\",\"body\":{\"displayName\":\"body.displayName\",\"properties\":{\"displayName\":\"body.properties.displayName\"},\"name\":\"body.name\"}}");
        var camelCaseName = BuildResource("{\"name\":null,\"display_name\":null,\"displayName\":\"displayName\",\"body\":{\"displayName\":\"body.displayName\",\"properties\":{\"displayName\":\"body.properties.displayName\"},\"name\":\"body.name\"}}");
        var nestedDisplayName = BuildResource("{\"name\":null,\"display_name\":null,\"displayName\":null,\"body\":{\"displayName\":\"body.displayName\",\"properties\":{\"displayName\":\"body.properties.displayName\"},\"name\":\"body.name\"}}");
        var nestedPropertiesName = BuildResource("{\"name\":null,\"display_name\":null,\"displayName\":null,\"body\":{\"displayName\":null,\"properties\":{\"displayName\":\"body.properties.displayName\"},\"name\":\"body.name\"}}");
        var nestedName = BuildResource("{\"name\":null,\"display_name\":null,\"displayName\":null,\"body\":{\"displayName\":null,\"properties\":{\"displayName\":null},\"name\":\"body.name\"}}");

        name.Summary.Should().Contain("`name`");
        displayName.Summary.Should().Contain("`display_name`");
        camelCaseName.Summary.Should().Contain("`displayName`");
        nestedDisplayName.Summary.Should().Contain("`body.displayName`");
        nestedPropertiesName.Summary.Should().Contain("`body.properties.displayName`");
        nestedName.Summary.Should().Contain("`body.name`");
    }

    [Test]
    public void Build_DefaultCandidatesSkipInvalidValuesAndAcceptFalse()
    {
        var model = BuildResource(
            "{\"name\":null,\"display_name\":\"\",\"displayName\":{},\"body\":{\"displayName\":[],\"properties\":{\"displayName\":false},\"name\":99}}");

        model.Summary.Should().Contain("false");
        model.SummaryHtml.Should().Contain("false");
        model.Summary.Should().NotContain("99");
    }

    [Test]
    public void Build_DefaultCandidatesAcceptZero()
    {
        var model = BuildResource("{\"name\":0}");

        model.Summary.Should().Contain("0");
        model.SummaryHtml.Should().Contain("0");
    }

    [Test]
    public void Build_DottedCandidatesRequireNestedPropertiesAndDoNotParseStringBodies()
    {
        var model = BuildResource("{\"body.displayName\":\"literal dotted key\",\"body\":\"{\\\"displayName\\\":\\\"embedded JSON\\\"}\"}");

        model.Summary.Should().NotContain("literal dotted key");
        model.Summary.Should().NotContain("embedded JSON");
        model.SummaryHtml.Should().NotContain("literal dotted key");
        model.SummaryHtml.Should().NotContain("embedded JSON");
    }

    [Test]
    public void Build_CustomOrderReplacesDefaultsAndSupportsNestedPaths()
    {
        var model = BuildResource(
            "{\"name\":\"default name\",\"displayName\":\"camel name\",\"body\":{\"title\":\"custom title\"}}",
            new ReportModelBuilderOptions(SummaryNameAttributes: ["body.title", "displayName"]));

        model.Summary.Should().Contain("custom title");
        model.Summary.Should().NotContain("default name");
        model.SummaryHtml.Should().Contain("custom title");
        model.SummaryHtml.Should().NotContain("default name");
    }

    [Test]
    public void Build_CustomOrderWithNoMatchDoesNotUseDefaultCandidates()
    {
        var model = BuildResource(
            "{\"name\":\"default name\"}",
            new ReportModelBuilderOptions(SummaryNameAttributes: ["body.title"]));

        model.Summary.Should().NotContain("default name");
        model.SummaryHtml.Should().NotContain("default name");
    }

    [Test]
    public void Build_UpdateWithUnknownDesiredIdentityUsesPriorValueAtSamePath()
    {
        var model = BuildResource(
            "{\"displayName\":\"computed placeholder\"}",
            action: "update",
            beforeJson: "{\"displayName\":\"prior display name\"}",
            afterUnknownJson: "{\"displayName\":true}");

        model.Summary.Should().Contain("prior display name");
        model.Summary.Should().NotContain("computed placeholder");
        model.SummaryHtml.Should().Contain("prior display name");
        model.SummaryHtml.Should().NotContain("computed placeholder");
    }

    [Test]
    public void Build_UpdateWithUnknownIdentityTriesPriorAtSamePathBeforeNextCandidate()
    {
        var model = BuildResource(
            "{\"name\":null,\"display_name\":\"computed display\",\"body\":{\"name\":\"later candidate\"}}",
            action: "update",
            beforeJson: "{\"name\":null,\"display_name\":\"prior display\",\"body\":{\"name\":null}}",
            afterUnknownJson: "{\"display_name\":true}");

        model.Summary.Should().Contain("prior display").And.NotContain("later candidate");
        model.SummaryHtml.Should().Contain("prior display").And.NotContain("later candidate");
    }

    [Test]
    public void Build_DeletePrefersPriorIdentity()
    {
        var model = BuildResource(
            "{\"displayName\":\"desired name\"}",
            action: "delete",
            beforeJson: "{\"displayName\":\"prior name\"}");

        model.Summary.Should().Contain("prior name");
        model.Summary.Should().NotContain("desired name");
        model.SummaryHtml.Should().Contain("prior name");
        model.SummaryHtml.Should().NotContain("desired name");
    }

    [Test]
    public void Build_ReplacePrefersDesiredIdentity()
    {
        var model = BuildResource(
            "{\"name\":\"new identity\"}",
            beforeJson: "{\"name\":\"old identity\"}",
            actions: ["delete", "create"]);

        model.Summary.Should().Contain("new identity").And.NotContain("old identity");
        model.SummaryHtml.Should().Contain("new identity").And.NotContain("old identity");
    }

    [Test]
    public void Build_SkipsSensitiveIdentityUnlessSensitiveValuesAreShown()
    {
        const string after = "{\"displayName\":\"classified\",\"body\":{\"name\":\"public fallback\"}}";
        var hidden = BuildResource(after, beforeSensitiveJson: "{\"displayName\":true}");
        var visible = BuildResource(
            after,
            new ReportModelBuilderOptions(ShowSensitive: true),
            beforeSensitiveJson: "{\"displayName\":true}");

        hidden.Summary.Should().Contain("public fallback");
        hidden.Summary.Should().NotContain("classified");
        hidden.SummaryHtml.Should().Contain("public fallback");
        hidden.SummaryHtml.Should().NotContain("classified");
        visible.Summary.Should().Contain("classified");
        visible.SummaryHtml.Should().Contain("classified");
    }

    [Test]
    public void Build_AncestorAndWholeResourceMarkersPreventUnknownOrSensitiveIdentity()
    {
        var sensitive = BuildResource(
            "{\"body\":{\"displayName\":\"secret child\"}}",
            afterSensitiveJson: "{\"body\":true}");
        var unknownAncestor = BuildResource(
            "{\"body\":{\"displayName\":\"computed child\"}}",
            afterUnknownJson: "{\"body\":true}");
        var unknownWhole = BuildResource(
            "{\"name\":\"computed resource\"}",
            afterUnknownJson: "true");

        sensitive.Summary.Should().NotContain("secret child");
        unknownAncestor.Summary.Should().NotContain("computed child");
        unknownWhole.Summary.Should().NotContain("computed resource");
    }

    [Test]
    public void Build_EscapesIdentityAtTextAndHtmlBoundaries()
    {
        const string identity = "<script>alert(1)</script> & `quoted` | value";
        var model = BuildResource(JsonSerializer.Serialize(new { displayName = identity }));

        model.Summary.Should().Contain("&amp;").And.Contain("\\`").And.Contain("\\|");
        model.SummaryHtml.Should().Contain("&lt;script&gt;").And.Contain("&amp;");
        model.SummaryHtml.Should().NotContain("<script>");
    }

    [Test]
    public void Build_MappedResourceOverridesRetainExistingIdentityChoices()
    {
        var options = new ReportModelBuilderOptions(SummaryNameAttributes: ["body.title"]);
        var msgraphResource = BuildResource(
            "{\"url\":\"applications\",\"name\":\"competing name\",\"body\":{\"displayName\":\"resource app\",\"title\":\"override title\"}}",
            options,
            resourceType: "msgraph_resource");
        var msgraphUpdate = BuildResource(
            "{\"url\":\"applications\",\"body\":{\"displayName\":\"update app\",\"title\":\"override title\"}}",
            options,
            resourceType: "msgraph_update_resource");
        var azapi = BuildResource(
            "{\"name\":\"azapi object\",\"type\":\"Microsoft.Test/widgets@v1\",\"body\":{\"title\":\"override title\"}}",
            options,
            resourceType: "azapi_resource");
        var azureRm = BuildResource(
            "{\"name\":\"resource group\",\"location\":\"westeurope\",\"body\":{\"title\":\"override title\"}}",
            options,
            resourceType: "azurerm_resource_group");

        msgraphResource.Summary.Should().Be("`resource app` (applications)");
        msgraphUpdate.Summary.Should().Contain("update app").And.NotContain("override title");
        azapi.Summary.Should().Contain("azapi object").And.NotContain("override title");
        azureRm.Summary.Should().Contain("resource group").And.NotContain("override title");
        msgraphResource.SummaryDisplayIdentity.Should().Be("resource app");
        msgraphUpdate.SummaryDisplayIdentity.Should().Be("update app");
        azapi.SummaryDisplayIdentity.Should().Be("azapi object");
        azureRm.SummaryDisplayIdentity.Should().Be("resource group");
    }

    private static ResourceChangeModel BuildResource(
        string? afterJson,
        ReportModelBuilderOptions? options = null,
        string action = "create",
        string? beforeJson = null,
        string? afterUnknownJson = null,
        string? beforeSensitiveJson = null,
        string? afterSensitiveJson = null,
        string resourceType = "msgraph_review_object",
        string[]? actions = null)
    {
        var change = new Change(
            actions ?? [action],
            ParseJson(beforeJson),
            ParseJson(afterJson),
            ParseJson(afterUnknownJson),
            ParseJson(beforeSensitiveJson),
            ParseJson(afterSensitiveJson));
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [new ResourceChange(
                $"{resourceType}.demo",
                null,
                "managed",
                resourceType,
                "demo",
                "registry.terraform.io/example/custom",
                change)]);

        return new ReportModelBuilder(options: options).Build(plan).Changes.Single();
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
