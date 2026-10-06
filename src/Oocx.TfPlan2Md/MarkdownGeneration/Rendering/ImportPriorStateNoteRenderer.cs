namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Renders the shared explanation for imported resources with no meaningful prior state.
/// </summary>
internal static class ImportPriorStateNoteRenderer
{
    /// <summary>
    /// Adds the full desired-state note when an imported resource has no substantive prior values.
    /// </summary>
    /// <param name="writer">The report writer.</param>
    /// <param name="change">The resource change model.</param>
    internal static void Render(MarkdownWriter writer, ResourceChangeModel change)
    {
        if (!change.IsImportedWithEmptyPriorState)
        {
            return;
        }

        writer.Paragraph("> 📥 Prior state from import is empty; the values below show the full desired state.");
        writer.BlankLine();
    }
}
