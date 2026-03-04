namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Represents the template selection decision made for a specific Terraform resource type.
/// Related feature: docs/features/038-debug-output/specification.md.
/// </summary>
/// <param name="ResourceType">The Terraform resource type (e.g., "azurerm_virtual_network").</param>
/// <param name="TemplateSource">Description of which renderer/template mode was used (e.g., "Built-in template: default", "Built-in template: summary", or "C# resource renderer").</param>
/// <remarks>
/// This record captures template selection decisions to help users understand which
/// render decisions are being applied to different resource types. This is particularly useful
/// when troubleshooting resource-specific rendering behavior.
/// </remarks>
internal record TemplateResolution(string ResourceType, string TemplateSource);
