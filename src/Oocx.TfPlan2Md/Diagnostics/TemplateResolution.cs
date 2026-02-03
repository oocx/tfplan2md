namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Represents the template selection decision made for a specific Terraform resource type.
/// Related feature: docs/features/038-debug-output/specification.md.
/// </summary>
/// <param name="ResourceType">The Terraform resource type (e.g., "azurerm_virtual_network").</param>
/// <param name="TemplateSource">Description of which template was used (e.g., "Built-in resource-specific template", "Default template", or "Custom template: /path/to/template.sbn").</param>
/// <remarks>
/// This record captures template selection decisions to help users understand which
/// templates are being applied to different resource types. This is particularly useful
/// when troubleshooting custom template behavior or verifying that resource-specific
/// templates are being used as expected.
/// </remarks>
internal record TemplateResolution(string ResourceType, string TemplateSource);
