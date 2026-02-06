using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// JSON serialization context for icon rules to support native AOT compilation.
/// </summary>
[JsonSerializable(typeof(IconRulesModel))]
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Source-generated JSON serialization context has natural coupling to model types")]
internal partial class IconRulesJsonContext : JsonSerializerContext
{
}
