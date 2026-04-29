using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Provides source-generated JSON metadata for Terraform plan parsing and auxiliary mapping files.
/// Related feature: docs/features/037-aot-trimmed-image/specification.md.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TerraformPlan))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(PrincipalMappingFile))]
[JsonSerializable(typeof(MappingEntry))]
[JsonSerializable(typeof(ActionInvocation))]
[JsonSerializable(typeof(LifecycleActionTrigger))]
[JsonSerializable(typeof(InvokeActionTrigger))]
[JsonSerializable(typeof(RelevantAttribute))]
[JsonSerializable(typeof(IReadOnlyList<ActionInvocation>))]
[JsonSerializable(typeof(IReadOnlyList<RelevantAttribute>))]
internal partial class TfPlanJsonContext : JsonSerializerContext
{
}
