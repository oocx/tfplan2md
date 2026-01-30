using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Tests helper branches in the Terraform show diff renderer.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
public class DiffRendererHelperTests
{
    /// <summary>
    /// Verifies added arrays of primitives render with array syntax.
    /// </summary>
    [Test]
    public async Task RenderAddedValue_ArrayOfPrimitives_WritesArrayLines()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderAddedValue");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var valueDoc = JsonDocument.Parse("[1,2]");

        method.Invoke(renderer, new object?[]
        {
            ansi,
            valueDoc.RootElement,
            "values",
            string.Empty,
            "+",
            AnsiStyle.Green,
            null,
            null,
            new List<string> { "values" },
            0
        });

        var output = writer.ToString();
        output.Should().Contain("values");
        output.Should().Contain("[");
        output.Should().Contain("1");
        output.Should().Contain("2");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies removed primitive arrays include the null suffix.
    /// </summary>
    [Test]
    public async Task RenderRemovedValue_PrimitiveArray_WritesNullSuffix()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderRemovedValue");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var valueDoc = JsonDocument.Parse("[\"a\", \"b\"]");

        method.Invoke(renderer, new object?[]
        {
            ansi,
            valueDoc.RootElement,
            "tags",
            string.Empty,
            null,
            new List<string> { "tags" },
            0
        });

        writer.ToString().Should().Contain("-> null");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies added objects render nested properties.
    /// </summary>
    [Test]
    public async Task RenderAddedValue_Object_WritesNestedProperties()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderAddedValue");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var valueDoc = JsonDocument.Parse("{ \"name\": \"example\" }");

        method.Invoke(renderer, new object?[]
        {
            ansi,
            valueDoc.RootElement,
            "metadata",
            string.Empty,
            "+",
            AnsiStyle.Green,
            null,
            null,
            new List<string> { "metadata" },
            0
        });

        writer.ToString().Should().Contain("name");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies added array items render object blocks.
    /// </summary>
    [Test]
    public async Task RenderAddedArrayItem_Object_WritesBlock()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderAddedArrayItem");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var valueDoc = JsonDocument.Parse("{ \"name\": \"example\" }");

        method.Invoke(renderer, new object?[]
        {
            ansi,
            valueDoc.RootElement,
            string.Empty,
            "+",
            AnsiStyle.Green,
            null,
            null,
            new List<string> { "items", "0" }
        });

        writer.ToString().Should().Contain("{");
        writer.ToString().Should().Contain("name");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies added array items render nested arrays.
    /// </summary>
    [Test]
    public async Task RenderAddedArrayItem_Array_WritesNestedItems()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderAddedArrayItem");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var valueDoc = JsonDocument.Parse("[1]");

        method.Invoke(renderer, new object?[]
        {
            ansi,
            valueDoc.RootElement,
            string.Empty,
            "+",
            AnsiStyle.Green,
            null,
            null,
            new List<string> { "items", "0" }
        });

        writer.ToString().Should().Contain("[");
        writer.ToString().Should().Contain("1");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies updated array items render changed object properties.
    /// </summary>
    [Test]
    public async Task RenderUpdatedArrayItem_Object_WritesChanges()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderUpdatedArrayItem");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var beforeDoc = JsonDocument.Parse("{ \"name\": \"old\", \"count\": 1 }");
        using var afterDoc = JsonDocument.Parse("{ \"name\": \"new\", \"added\": true }");

        method.Invoke(renderer, new object?[]
        {
            ansi,
            beforeDoc.RootElement,
            afterDoc.RootElement,
            string.Empty,
            new List<string> { "items", "0" },
            null,
            null,
            new HashSet<string>()
        });

        var output = writer.ToString();
        output.Should().Contain("name");
        output.Should().Contain("added");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies sensitive removals render placeholder text.
    /// </summary>
    [Test]
    public async Task RenderRemovedValue_SensitiveValue_WritesPlaceholder()
    {
        var renderer = new DiffRenderer();
        var method = GetMethod("RenderRemovedValue");
        using var writer = new StringWriter(new StringBuilder());
        using var ansi = new AnsiTextWriter(writer, useColor: false);
        using var valueDoc = JsonDocument.Parse("\"secret\"");
        using var sensitiveDoc = JsonDocument.Parse("{ \"secret\": true }");

        method.Invoke(renderer, new object?[]
        {
            ansi,
            valueDoc.RootElement,
            "secret",
            string.Empty,
            sensitiveDoc.RootElement,
            new List<string> { "secret" },
            0
        });

        writer.ToString().Should().Contain("sensitive");
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
