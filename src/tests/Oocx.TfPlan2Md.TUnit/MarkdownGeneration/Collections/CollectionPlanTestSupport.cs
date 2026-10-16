using System.Globalization;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Builds Terraform plan JSON and renders it for compact collection diff tests.
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
internal static class CollectionPlanTestSupport
{
    /// <summary>
    /// Describes one resource change in a test plan; each value is raw JSON or null when the key is omitted.
    /// </summary>
    internal sealed record Resource(
        string Address,
        string Type,
        string[] Actions,
        string? Before,
        string? After,
        string? AfterUnknown = null,
        string? BeforeSensitive = null,
        string? AfterSensitive = null,
        string Provider = "registry.terraform.io/example/example");

    /// <summary>
    /// Creates a resource description with the default example type.
    /// </summary>
    internal static Resource Res(
        string[] actions,
        string? before,
        string? after,
        string address = "example_resource.main",
        string type = "example_resource",
        string? afterUnknown = null,
        string? beforeSensitive = null,
        string? afterSensitive = null,
        string provider = "registry.terraform.io/example/example")
        => new(address, type, actions, before, after, afterUnknown, beforeSensitive, afterSensitive, provider);

    /// <summary>
    /// Serialises a resource into a <c>resource_changes</c> / <c>resource_drift</c> array entry.
    /// </summary>
    internal static string ToJson(Resource resource)
    {
        var name = resource.Address[(resource.Address.LastIndexOf('.') + 1)..];
        var change = new StringBuilder();
        change.Append(CultureInfo.InvariantCulture, $"\"actions\":[{string.Join(',', resource.Actions.Select(a => $"\"{a}\""))}]");
        Append(change, "before", resource.Before);
        Append(change, "after", resource.After);
        Append(change, "after_unknown", resource.AfterUnknown);
        Append(change, "before_sensitive", resource.BeforeSensitive);
        Append(change, "after_sensitive", resource.AfterSensitive);
        return $"{{\"address\":{JsonSerializer.Serialize(resource.Address)},\"mode\":\"managed\",\"type\":\"{resource.Type}\",\"name\":{JsonSerializer.Serialize(name)},\"provider_name\":\"{resource.Provider}\",\"change\":{{{change}}}}}";
    }

    /// <summary>
    /// Builds a plan JSON document with planned changes and optional drift.
    /// </summary>
    internal static string PlanJson(IEnumerable<Resource> changes, IEnumerable<Resource>? drift = null, string? outputChanges = null)
    {
        var builder = new StringBuilder("{\"format_version\":\"1.2\",\"terraform_version\":\"1.14.0\",\"resource_changes\":[");
        builder.AppendJoin(',', changes.Select(ToJson)).Append(']');
        if (drift is not null)
        {
            builder.Append(",\"resource_drift\":[").AppendJoin(',', drift.Select(ToJson)).Append(']');
        }

        if (outputChanges is not null)
        {
            builder.Append(",\"output_changes\":").Append(outputChanges);
        }

        return builder.Append('}').ToString();
    }

    /// <summary>
    /// Parses plan JSON and builds the report model.
    /// </summary>
    internal static ReportModel BuildModel(string planJson, ReportModelBuilderOptions? options = null)
    {
        var plan = new TerraformPlanParser().Parse(planJson);
        return new ReportModelBuilder(options: options ?? new ReportModelBuilderOptions()).Build(plan);
    }

    /// <summary>
    /// Renders a plan JSON document to Markdown.
    /// </summary>
    internal static string Render(string planJson, ReportModelBuilderOptions? options = null)
        => new MarkdownRenderer().Render(BuildModel(planJson, options));

    /// <summary>
    /// Renders a single-resource plan to Markdown.
    /// </summary>
    internal static string Render(Resource resource, ReportModelBuilderOptions? options = null)
        => Render(PlanJson([resource]), options);

    /// <summary>
    /// Renders a JSON array of strings.
    /// </summary>
    internal static string L(params string[] values)
        => "[" + string.Join(',', values.Select(v => JsonSerializer.Serialize(v))) + "]";

    private static void Append(StringBuilder builder, string key, string? value)
    {
        if (value is not null)
        {
            builder.Append(CultureInfo.InvariantCulture, $",\"{key}\":{value}");
        }
    }
}
