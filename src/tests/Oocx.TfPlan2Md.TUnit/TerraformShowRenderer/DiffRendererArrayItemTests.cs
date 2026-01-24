using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Tests array item rendering branches in the Terraform show diff renderer.
/// </summary>
public class DiffRendererArrayItemTests
{
    /// <summary>
    /// Ensures unknown array items render the known-after-apply placeholder.
    /// </summary>
    [Test]
    public async Task RenderAddedArrayItem_WithUnknownPath_WritesKnownAfterApply()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderAddedArrayItem");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var elementDoc = JsonDocument.Parse("\"value\"");
        using var unknownDoc = JsonDocument.Parse("{\"items\":[true]}");

        var path = new List<string> { "items", "0" };
        method.Invoke(renderer, new object?[]
        {
            ansi,
            elementDoc.RootElement,
            string.Empty,
            "+",
            AnsiStyle.Green,
            unknownDoc.RootElement,
            null,
            path
        });

        writer.ToString().Should().Contain("known after apply");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures sensitive array items render the sensitive placeholder comment.
    /// </summary>
    [Test]
    public async Task RenderAddedArrayItem_WithSensitivePath_WritesSensitivePlaceholder()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderAddedArrayItem");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var elementDoc = JsonDocument.Parse("\"value\"");
        using var sensitiveDoc = JsonDocument.Parse("{\"items\":[true]}");

        var path = new List<string> { "items", "0" };
        method.Invoke(renderer, new object?[]
        {
            ansi,
            elementDoc.RootElement,
            string.Empty,
            "+",
            AnsiStyle.Green,
            null,
            sensitiveDoc.RootElement,
            path
        });

        writer.ToString().Should().Contain("sensitive");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures updated array items marked unknown render the known-after-apply placeholder.
    /// </summary>
    [Test]
    public async Task RenderUpdatedArrayItem_WithUnknownPath_WritesKnownAfterApply()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderUpdatedArrayItem");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var beforeDoc = JsonDocument.Parse("\"before\"");
        using var afterDoc = JsonDocument.Parse("\"after\"");
        using var unknownDoc = JsonDocument.Parse("{\"items\":[true]}");

        var path = new List<string> { "items", "0" };
        method.Invoke(renderer, new object?[]
        {
            ansi,
            beforeDoc.RootElement,
            afterDoc.RootElement,
            string.Empty,
            path,
            unknownDoc.RootElement,
            null,
            new HashSet<string>()
        });

        writer.ToString().Should().Contain("known after apply");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets a private DiffRenderer method via reflection.
    /// </summary>
    /// <param name="name">Method name to fetch.</param>
    /// <returns>The method info.</returns>
    [SuppressMessage("Major Code Smell", "S3011", Justification = "Testing private diff rendering branches via reflection.")]
    private static MethodInfo GetMethod(string name)
    {
        var method = typeof(DiffRenderer).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return method!;
    }
}
