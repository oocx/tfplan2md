using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Provides source-generated JSON metadata for Terraform plan parsing and auxiliary mapping files.
/// Related feature: docs/features/037-aot-trimmed-image/specification.md
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TerraformPlan))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class TfPlanJsonContext : JsonSerializerContext
{
}
