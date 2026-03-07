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
    /// The default implementation is a no-op; override only when the factory enriches the model.
    /// </summary>
    /// <param name="context">All contextual data needed to enrich the view model.</param>
    void ApplyViewModel(ApplyViewModelContext context)
    {
        // Default no-op: factories that do not enrich the model skip this method.
    }
}
