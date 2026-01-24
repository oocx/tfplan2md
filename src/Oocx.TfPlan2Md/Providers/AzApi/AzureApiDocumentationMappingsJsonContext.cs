using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// JSON serialization context for Azure API documentation mappings to support native AOT compilation.
/// Related feature: docs/features/048-azure-api-doc-mapping/specification.md.
/// </summary>
[JsonSerializable(typeof(AzureApiDocumentationMappingsModel))]
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Source-generated JSON serialization context has natural coupling to model types")]
internal partial class AzureApiDocumentationMappingsJsonContext : JsonSerializerContext
{
}
