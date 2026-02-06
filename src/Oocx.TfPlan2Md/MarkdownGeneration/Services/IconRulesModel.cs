using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Represents the JSON document containing icon rules.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class IconRulesModel
{
    /// <summary>
    /// Gets or sets the list of icon rules.
    /// </summary>
    /// <value>The icon rules defined in the configuration file.</value>
    [JsonPropertyName("rules")]
    public List<IconRule>? Rules { get; set; }
}
