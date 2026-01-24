using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Factory interface for building resource-specific view models from Terraform plan data.
/// </summary>
/// <remarks>
/// This abstraction decouples ReportModelBuilder from specific factory implementations,
/// reducing class coupling and enabling easier extensibility for new resource types.
/// Related feature: docs/features/046-code-quality-metrics-enforcement/specification.md.
/// </remarks>
internal interface IResourceViewModelFactory
{
    /// <summary>
    /// Applies resource-specific view model to the provided model.
    /// </summary>
    /// <param name="model">The resource change model to populate.</param>
    /// <param name="resourceChange">The resource change data from the Terraform plan.</param>
    /// <param name="action">The determined action for this resource (create, update, delete, replace).</param>
    /// <param name="attributeChanges">Pre-computed attribute changes for the resource.</param>
    void ApplyViewModel(
        ResourceChangeModel model,
        ResourceChange resourceChange,
        string action,
        System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges);
}
