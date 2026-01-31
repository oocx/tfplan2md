using System;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.RenderTargets;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Registers Scriban helper functions for template rendering.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Registers all custom helper functions with the given <see cref="ScriptObject"/>.
    /// Related feature: docs/features/026-template-rendering-simplification/specification.md.
    /// </summary>
    /// <param name="scriptObject">The script object receiving helpers.</param>
    /// <param name="principalMapper">Mapper used to resolve principal names.</param>
    /// <param name="diffFormatter">Formatter used for rendering before/after diffs.</param>
    internal static void RegisterHelpers(ScriptObject scriptObject, IPrincipalMapper principalMapper, IDiffFormatter diffFormatter)
    {
        scriptObject.Import("format_diff", new Func<string?, string?, string>((before, after) => diffFormatter.FormatDiff(before, after)));
        scriptObject.Import("diff_array", new Func<object?, object?, string, ScriptObject>(DiffArray));
        scriptObject.Import("escape_markdown", new Func<string?, string>(EscapeMarkdown));
        scriptObject.Import("escape_markdown_table_cell", new Func<string?, string>(EscapeMarkdownTableCell));
        scriptObject.Import("escape_heading", new Func<string?, string>(EscapeMarkdownHeading));
        scriptObject.Import("format_large_value", new Func<string?, string?, string, string>(FormatLargeValue));
        scriptObject.Import("format_value", new Func<string?, string?, string>(FormatValue));
        scriptObject.Import("format_code_summary", new Func<string?, string>(FormatCodeSummary));
        scriptObject.Import("format_code_table", new Func<string?, string>(FormatCodeTable));
        scriptObject.Import("format_icon_value_summary", new Func<string?, string>(FormatIconValueSummary));
        scriptObject.Import("format_icon_value_table", new Func<string?, string>(FormatIconValueTable));
        scriptObject.Import("format_attribute_value_summary", new Func<string?, string?, string?, string>(FormatAttributeValueSummary));
        scriptObject.Import("format_attribute_value_table", new Func<string?, string?, string?, string>(FormatAttributeValueTable));
        scriptObject.Import("format_attribute_value_plain", new Func<string?, string?, string?, string>(FormatAttributeValuePlain));
        scriptObject.Import("large_attributes_summary", new Func<object?, string>(LargeAttributesSummary));
        scriptObject.Import("is_large_value", new Func<string?, string?, bool>(IsLargeValue));
        scriptObject.Import("azure_role_name", new Func<string?, string>(AzureRoleDefinitionMapper.GetRoleName));
        scriptObject.Import("azure_scope", new Func<string?, string>(AzureScopeParser.ParseScope));
        scriptObject.Import("azure_principal_name", new Func<string?, string?, string?, string>((id, type, addr) => ResolvePrincipalName(id, type, principalMapper, addr)));
        scriptObject.Import("azure_scope_info", new Func<string?, ScriptObject>(GetScopeInfo));
        scriptObject.Import("azure_role_info", new Func<string?, string?, ScriptObject>(GetRoleInfo));
        scriptObject.Import("azure_principal_info", new Func<string?, string?, string?, ScriptObject>((id, type, addr) => GetPrincipalInfo(id, type, principalMapper, addr)));
        scriptObject.Import("try_get_principal_type", new Func<string?, ScriptObject>(id => TryGetPrincipalType(id, principalMapper)));
        scriptObject.Import("collect_attributes", new Func<object?, object?, ScriptArray>(CollectAttributes));
        scriptObject.Import("get_attribute_finding_indicator", new Func<string?, ScriptArray?, string>(GetAttributeFindingIndicator));
    }
}
