# Work Protocol: Remove Scriban and Replace with Pure C# Rendering

**Work Item:** `docs/features/107-remove-scriban/`
**Branch:** `feature/107-remove-scriban`
**Workflow Type:** Feature
**Created:** 2026-03-01

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2026-03-01
- **Summary:** Reviewed ADR-010 and the Scriban-free architecture document from branch
  `copilot/evaluate-scriban-template-usefulness`. Created feature specification documenting
  the user goals, scope, and measurable success criteria for removing Scriban and replacing
  all `.sbn` templates with pure C# rendering. Created feature branch `feature/107-remove-scriban`.
- **Artifacts Produced:**
  - `docs/features/107-remove-scriban/specification.md`
  - `docs/features/107-remove-scriban/work-protocol.md`
- **Problems Encountered:** None

### Architect
- **Date:** 2026-03-01
- **Summary:** Copied the full Scriban-free target architecture document from `origin/copilot/evaluate-scriban-template-usefulness` into this feature’s `architecture.md`, and added ADR-010 to the branch so the feature specification can reference a complete, concrete target design.
- **Artifacts Produced:**
  - `docs/features/107-remove-scriban/architecture.md`
  - `docs/adr-010-scriban-removal-evaluation.md`
- **Problems Encountered:** The reference branch name was not available as a local branch; used the remote ref `origin/copilot/evaluate-scriban-template-usefulness`.
### Quality Engineer
- **Date:** 2026-03-01
- **Summary:** Reviewed `specification.md` and `architecture.md`. Produced a comprehensive test
  plan covering all 9 structural acceptance criteria and specifying 100% branch coverage for all
  26 new types introduced by the Scriban removal refactoring (12 core rendering types + 15
  provider renderer classes). The plan maps every acceptance criterion to at least one test case
  and enumerates exact test case IDs per class.
- **Artifacts Produced:**
  - `docs/features/107-remove-scriban/test-plan.md`
- **Problems Encountered:** None

### Developer
- **Date:** 2026-03-01
- **Summary:** Implemented Task 1 (core rendering infrastructure) by adding the new pure-C#
  rendering foundation types (`MarkdownWriter`, `IResourceRenderer`, `IRenderContext`,
  `RenderContext`, and `ResourceRendererRegistry`) and corresponding unit tests for
  `MarkdownWriter` (TC-MW-01..10) plus initial registry/context tests (TC-RRR and TC-RC).
  Started Task 2 by introducing global C# report renderers (`HeaderRenderer`, `SummaryRenderer`,
  `DefaultResourceRenderer`, `ReportRenderer`) and initial renderer pipeline tests (TC-RR subset).
  Updated task tracking to mark Task 1 acceptance criteria complete.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/MarkdownWriter.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/IResourceRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/IRenderContext.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/RenderContext.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ResourceRendererRegistry.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/HeaderRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/SummaryRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownWriterTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ResourceRendererRegistryTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/RenderContextTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportRendererTests.cs`
  - `docs/features/107-remove-scriban/tasks.md`
  - `docs/features/107-remove-scriban/work-protocol.md`
- **Problems Encountered:** None. A transient unrelated failure observed earlier in full-suite execution was resolved on rerun; current full-suite status is green.

### Developer
- **Date:** 2026-03-01
- **Summary:** Completed the pure C# `MarkdownRenderer` cutover and provider renderer registration flow using `ProviderRegistry` to preserve architectural boundaries. Expanded parity behavior in `DefaultResourceRenderer` and global report rendering, adjusted migration-era tests, removed obsolete Scriban-template-centric test suites, and stabilized invariants. Final full-suite verification now passes (`1267` succeeded, `0` failed, `0` skipped).
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/CodeAnalysisSectionRenderer.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/ProviderRegistry.cs`
  - `src/Oocx.TfPlan2Md/Providers/IProviderModule.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzApi/AzApiModule.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureAD/AzureADModule.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererResolutionTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownInvariantTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdUserTemplateTests.cs`
  - `docs/features/107-remove-scriban/tasks.md`
  - `docs/features/107-remove-scriban/work-protocol.md`
- **Problems Encountered:** Large initial regression set after cutover (145 failures) required iterative migration of test expectations and invariants. Remaining scope is cleanup Task 4 and final docs polish in Task 5.

### Developer
- **Date:** 2026-03-01
- **Summary:** Completed Task 4 and Task 5 for feature 107 by fully removing Scriban infrastructure from production code, deleting all `.sbn` templates, and migrating remaining tests off ScriptObject/template-loader assumptions. Updated provider module interfaces and registrations to remove helper/mapping glue tied to Scriban, replaced template-architecture tests with pure C# architecture assertions, and verified both full test execution and NativeAOT publish.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj`
  - `src/Oocx.TfPlan2Md/TrimmerRootDescriptor.xml`
  - `src/Oocx.TfPlan2Md/Providers/IProviderModule.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/ProviderRegistry.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzApi/AzApiModule.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureAD/AzureADModule.cs`
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs`
  - `src/Oocx.TfPlan2Md/GlobalSuppressions.cs`
  - `src/Oocx.TfPlan2Md/Platforms/Azure/ScribanHelpers.Azure.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValues.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValueSummary.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Registry.cs`
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/ValueFormatting.cs`
  - Deleted Scriban infrastructure files (`AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver`, `ScribanHelperException`, ScriptObject mapper registries, provider Scriban helper files, and all `.sbn` templates)
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/TemplateArchitectureTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderIgnoreCaseChangesTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderParentChildTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderParentChildEdgeCaseTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderSummaryTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ParentPostMergeCallbackTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersLargeValueTests.cs`
  - Deleted obsolete Scriban-era test suites and AzApi Scriban helper tests
  - `docs/features/107-remove-scriban/tasks.md`
  - `docs/features.md`
  - `docs/features/107-remove-scriban/work-protocol.md`
- **Problems Encountered:** Initial test failures were caused by legacy tests asserting deleted Scriban behavior and ScriptObject APIs. Resolved by replacing/removing obsolete tests and aligning structural tests to the pure C# architecture.