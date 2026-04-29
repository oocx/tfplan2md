using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Represents an attribute in one resource that influenced changes in another resource.
/// The attribute path is a heterogeneous array of strings (object keys) and integers (array indices),
/// parsed using the same pattern as replace_paths.
/// Related feature: docs/features/122-terraform-1-15-support/specification.md.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "This is the wire-format name from Terraform 1.14+ plan JSON; 'RelevantAttribute' is not a .NET attribute.")]
public record RelevantAttribute(
    [property: JsonPropertyName("resource")] string Resource,
    [property: JsonPropertyName("attribute")]
    [property: JsonConverter(typeof(RelevantAttributePathConverter))]
    IReadOnlyList<object> Attribute
);
