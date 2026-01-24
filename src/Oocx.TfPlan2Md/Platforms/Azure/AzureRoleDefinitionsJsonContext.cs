using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// JSON serialization context for Azure role definitions to support native AOT compilation.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class AzureRoleDefinitionsJsonContext : JsonSerializerContext
{
}
