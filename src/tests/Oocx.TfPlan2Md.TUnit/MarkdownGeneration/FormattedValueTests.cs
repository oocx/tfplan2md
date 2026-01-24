using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Scriban;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests formatted value wrappers used by Scriban templates.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
public class FormattedValueTests
{
    /// <summary>
    /// Ensures formatted values expose raw and formatted fields to Scriban.
    /// </summary>
    [Test]
    public void FormattedValue_ExposesRawAndFormattedToScriban()
    {
        var model = new
        {
            value = new FormattedValue<string>("raw-value", "formatted-value")
        };

        var template = Template.Parse("{{ value.raw }}|{{ value.formatted }}", "formatted-value-test");
        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: member => ToSnakeCase(member.Name));
        var context = new TemplateContext
        {
            MemberRenamer = member => ToSnakeCase(member.Name)
        };
        context.PushGlobal(scriptObject);

        var result = template.Render(context);

        result.Should().Be("raw-value|formatted-value");
    }

    /// <summary>
    /// Ensures formatted lists expose raw, formatted, count, and empty state to Scriban.
    /// </summary>
    [Test]
    public void FormattedList_ExposesRawFormattedCountAndIsEmpty()
    {
        var list = new FormattedList<string>(new List<string> { "a", "b" }, "formatted-list");
        var model = new { list };

        var template = Template.Parse("{{ list.formatted }}|{{ list.count }}|{{ list.is_empty }}|{{ for item in list.raw }}{{ item }}{{ end }}", "formatted-list-test");
        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: member => ToSnakeCase(member.Name));
        var context = new TemplateContext
        {
            MemberRenamer = member => ToSnakeCase(member.Name)
        };
        context.PushGlobal(scriptObject);

        var result = template.Render(context);

        result.Should().Be("formatted-list|2|false|ab");
    }

    /// <summary>
    /// Ensures null formatted values throw for <see cref="FormattedValue{T}"/>.
    /// </summary>
    [Test]
    public async Task FormattedValue_WithNullFormatted_Throws()
    {
        var action = () => new FormattedValue<string>("raw", null!);

        action.Should().Throw<ArgumentNullException>();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures null raw values throw for <see cref="FormattedList{T}"/>.
    /// </summary>
    [Test]
    public async Task FormattedList_WithNullRaw_Throws()
    {
        var action = () => new FormattedList<string>(null!, "formatted");

        action.Should().Throw<ArgumentNullException>();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures null formatted values throw for <see cref="FormattedList{T}"/>.
    /// </summary>
    [Test]
    public async Task FormattedList_WithNullFormatted_Throws()
    {
        var action = () => new FormattedList<string>([], null!);

        action.Should().Throw<ArgumentNullException>();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures empty lists report correct count and empty state.
    /// </summary>
    [Test]
    public async Task FormattedList_WithNoItems_IsEmpty()
    {
        var list = new FormattedList<string>([], "formatted");

        list.Count.Should().Be(0);
        list.IsEmpty.Should().BeTrue();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures non-empty lists report correct count and empty state.
    /// </summary>
    [Test]
    public async Task FormattedList_WithItems_IsNotEmpty()
    {
        var list = new FormattedList<string>(["one"], "formatted");

        list.Count.Should().Be(1);
        list.IsEmpty.Should().BeFalse();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Converts identifiers to snake_case for Scriban member access.
    /// </summary>
    /// <param name="name">The input name.</param>
    /// <returns>The snake_case version.</returns>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];
            if (char.IsUpper(character))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
