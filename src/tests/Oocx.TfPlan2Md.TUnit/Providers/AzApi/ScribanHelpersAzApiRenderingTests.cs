using System.Text.Json;
using AwesomeAssertions;
using TUnit.Core;
using AzApiHelpers = Oocx.TfPlan2Md.Providers.AzApi.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for azapi_resource body rendering helpers.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
/// </summary>
public class ScribanHelpersAzApiRenderingTests
{
    /// <summary>
    /// Verifies null body content renders the empty body marker.
    /// </summary>
    [Test]
    public void RenderAzapiBody_NullBody_ReturnsEmptyMessage()
    {
        var result = AzApiHelpers.RenderAzapiBody(null, "Body", "create");

        result.Should().Contain("*Body: (empty)*");
    }

    /// <summary>
    /// Verifies create mode renders a body table.
    /// </summary>
    [Test]
    public void RenderAzapiBody_CreateMode_RendersTable()
    {
        using var document = JsonDocument.Parse("""
        {
          "name": "example",
          "location": "westeurope"
        }
        """);

        var result = AzApiHelpers.RenderAzapiBody(document.RootElement, "Body", "create");

        result.Should().Contain("#### Body");
        result.Should().Contain("| Property | Value |");
        result.Should().Contain("name");
    }

    /// <summary>
    /// Verifies update mode renders a change table.
    /// </summary>
    [Test]
    public void RenderAzapiBody_UpdateMode_RendersChanges()
    {
        using var beforeDoc = JsonDocument.Parse("""
        {
          "name": "before",
          "location": "westeurope"
        }
        """);
        using var afterDoc = JsonDocument.Parse("""
        {
          "name": "after",
          "location": "westeurope"
        }
        """);

        var result = AzApiHelpers.RenderAzapiBody(
            afterDoc.RootElement,
            "Body Changes",
            "update",
            beforeJson: beforeDoc.RootElement);

        result.Should().Contain("#### Body Changes");
        result.Should().Contain("| Property | Before | After |");
        result.Should().Contain("name");
    }
}
