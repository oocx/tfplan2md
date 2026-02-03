// Triggering release after CI fix
// Baseline suppression for code-quality metrics rollout.
// Related feature: docs/features/046-code-quality-metrics-enforcement/.
#pragma warning disable CA1506

using Oocx.TfPlan2Md;

return await ProgramEntry.RunAsync(args);

#pragma warning restore CA1506
