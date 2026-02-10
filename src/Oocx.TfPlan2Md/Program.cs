// Top-level statements entry point for tfplan2md command-line application.
// Delegates to ProgramEntry.RunAsync for all application logic.
// Related feature: docs/spec.md § Application Architecture.
//
// Note: Top-level statements don't support XML documentation comments.
// This file serves as the minimal entry point, with all logic in ProgramEntry class.
//
// Baseline suppression for code-quality metrics rollout.
// Related feature: docs/features/046-code-quality-metrics-enforcement/.
#pragma warning disable CA1506
#pragma warning disable SA1634 // File header copyright - not applicable to top-level statements

using Oocx.TfPlan2Md;

return await ProgramEntry.RunAsync(args);

#pragma warning restore SA1634
#pragma warning restore CA1506
