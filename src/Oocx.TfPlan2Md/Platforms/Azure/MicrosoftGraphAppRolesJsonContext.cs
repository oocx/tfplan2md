using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// JSON serialization context for Microsoft Graph app roles to support native AOT compilation.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class MicrosoftGraphAppRolesJsonContext : JsonSerializerContext
{
}
