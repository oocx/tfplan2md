using System.Reflection;
using System.Text;
using Scriban;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Renders Terraform plan reports to Markdown using Scriban templates.
/// </summary>
public class MarkdownRenderer
{
    private const string DefaultTemplateResourceName = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.default.sbn";

    /// <summary>
    /// Renders a report model to Markdown using the default embedded template.
    /// </summary>
    /// <param name="model">The report model to render.</param>
    /// <returns>The rendered Markdown string.</returns>
    public string Render(ReportModel model)
    {
        var templateText = LoadDefaultTemplate();
        return RenderWithTemplate(model, templateText);
    }

    /// <summary>
    /// Renders a report model to Markdown using a custom template file.
    /// </summary>
    /// <param name="model">The report model to render.</param>
    /// <param name="templatePath">Path to the custom template file.</param>
    /// <returns>The rendered Markdown string.</returns>
    public string Render(ReportModel model, string templatePath)
    {
        var templateText = File.ReadAllText(templatePath);
        return RenderWithTemplate(model, templateText);
    }

    /// <summary>
    /// Renders a report model to Markdown using a custom template file asynchronously.
    /// </summary>
    /// <param name="model">The report model to render.</param>
    /// <param name="templatePath">Path to the custom template file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The rendered Markdown string.</returns>
    public async Task<string> RenderAsync(ReportModel model, string templatePath, CancellationToken cancellationToken = default)
    {
        var templateText = await File.ReadAllTextAsync(templatePath, cancellationToken);
        return RenderWithTemplate(model, templateText);
    }

    private static string RenderWithTemplate(ReportModel model, string templateText)
    {
        var template = Template.Parse(templateText);
        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages);
            throw new MarkdownRenderException($"Template parsing failed: {errors}");
        }

        var result = template.Render(model, member => ToSnakeCase(member.Name));
        return result;
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string LoadDefaultTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(DefaultTemplateResourceName);

        if (stream is null)
        {
            throw new MarkdownRenderException($"Default template not found: {DefaultTemplateResourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
