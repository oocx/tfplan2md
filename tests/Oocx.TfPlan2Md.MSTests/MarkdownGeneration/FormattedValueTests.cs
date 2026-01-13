using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Scriban;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

[TestClass]
public class FormattedValueTests
{
    [TestMethod]
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

    [TestMethod]
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
