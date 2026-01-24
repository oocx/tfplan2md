using System;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Registration methods for AzApi Scriban helpers.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Registers AzApi-specific Scriban helper functions with the script object.
    /// Related feature: docs/features/047-provider-code-separation/specification.md.
    /// </summary>
    /// <param name="scriptObject">The script object receiving helpers.</param>
    internal static void RegisterAzApiHelpers(ScriptObject scriptObject)
    {
        // AzApi helpers - Related feature: docs/features/040-azapi-resource-template/specification.md
        scriptObject.Import("flatten_json", new Func<object?, string, ScriptArray>(FlattenJson));
        scriptObject.Import("compare_json_properties", new Func<object?, object?, object?, object?, bool, bool, ScriptArray>(CompareJsonProperties));
        scriptObject.Import("parse_azure_resource_type", new Func<string?, ScriptObject>(ParseAzureResourceType));
        scriptObject.Import("azure_api_doc_link", new Func<string?, string?>(AzureApiDocLink));
        scriptObject.Import("extract_azapi_metadata", new Func<object?, ScriptObject>(ExtractAzapiMetadata));
        scriptObject.Import("render_azapi_body", new Func<object?, string, string, object?, object?, object?, bool, string, string>(RenderAzapiBody));
    }
}
