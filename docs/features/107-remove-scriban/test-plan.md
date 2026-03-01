# Test Plan: Remove Scriban and Replace with Pure C# Rendering

## Overview

This test plan covers feature 107 — removing the Scriban template engine and replacing all
`.sbn` templates with pure C# rendering. The feature is a **pure internal refactoring**: from
the user's perspective nothing changes. The plan has two goals:

1. **Structural guard**: Verify that every Scriban artifact is removed and no new references
   exist.
2. **Behavioral guard + 100% branch coverage**: Verify that every new type and method
   introduced produces the same output as the Scriban templates it replaces, with all
   decision branches exercised.

Reference: [`docs/features/107-remove-scriban/specification.md`](specification.md)

---

## Test Coverage Matrix

### Structural / Architecture Acceptance Criteria

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `Scriban` NuGet package removed | TC-S01 | Architecture / reflection |
| All 27 `.sbn` template files deleted | TC-S02 | Build (CI) |
| `AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver` deleted | TC-S02 | Build (CI) |
| `TrimmerRootDescriptor.xml` has no Scriban entries | TC-S03 | File assertion |
| No C# file imports `using Scriban` or `Scriban.*` | TC-S04 | Architecture (NetArchTest) |
| All existing snapshot tests pass without snapshot modification | TC-S05 | Snapshot (existing) |
| NativeAOT binary builds successfully | TC-S06 | CI build |
| Zero third-party NuGet references | TC-S01 | Architecture / reflection |
| Architecture boundary exemptions for Scriban types removed | TC-S09 | Architecture (existing) |
| Provider modules implement `IResourceRenderer`, not `RegisterHelpers` | TC-S07 | Architecture / reflection |
| Existing `ArchitectureBoundaryTests` still pass | TC-S08 | Architecture (existing) |

### New Type Coverage Matrix

| New Type | Test Cases |
|----------|-----------|
| `MarkdownWriter` | TC-MW-01 to TC-MW-10 |
| `ResourceRendererRegistry` | TC-RRR-01 to TC-RRR-03 |
| `RenderContext` | TC-RC-01 |
| `ReportRenderer` | TC-RR-01 to TC-RR-07 |
| `HeaderRenderer` | TC-HR-01 to TC-HR-03 |
| `SummaryRenderer` | TC-SR-01 to TC-SR-03 |
| `DefaultResourceRenderer` | TC-DR-01 to TC-DR-08 |
| `ChildResourceRenderer` | TC-CR-01 to TC-CR-02 |
| `CodeAnalysisRenderer` | TC-CA-01 to TC-CA-06 |
| `RefactoringRenderer` | TC-RF-01 to TC-RF-04 |
| `OutputRenderer` | TC-OR-01 to TC-OR-04 |
| `RoleAssignmentRenderer` | TC-ARM-01 to TC-ARM-04 |
| `NsgRenderer` | TC-ARM-05 to TC-ARM-08 |
| `FirewallNetworkRuleRenderer` | TC-ARM-09 to TC-ARM-12 |
| `FirewallAppRuleRenderer` | TC-ARM-13 to TC-ARM-16 |
| `AzApiResourceRenderer` | TC-API-01 to TC-API-05 |
| `AzApiUpdateResourceRenderer` | TC-API-06 to TC-API-09 |
| `AzApiOutputValuesRenderer` | TC-API-10 to TC-API-14 |
| `UserRenderer` | TC-AD-01 to TC-AD-03 |
| `GroupRenderer` | TC-AD-04 to TC-AD-06 |
| `GroupWithoutMembersRenderer` | TC-AD-07 to TC-AD-09 |
| `GroupMemberRenderer` | TC-AD-10 to TC-AD-12 |
| `ServicePrincipalRenderer` | TC-AD-13 to TC-AD-15 |
| `InvitationRenderer` | TC-AD-16 to TC-AD-18 |
| `VariableGroupRenderer` | TC-ADO-01 to TC-ADO-04 |
| `BuildDefinitionRenderer` | TC-ADO-05 to TC-ADO-10 |

---

## No UAT Required

This feature produces **zero changes to user-facing output**. The specification states:
*"The Markdown output is byte-for-byte identical to the current output (verified by snapshot
tests)"*. There is no visual or CLI behaviour to validate in a PR environment, so no UAT test
plan is required.

---

## Section 1: Structural / Architecture Tests

---

### TC-S01: Zero Third-Party NuGet Dependencies

**Type:** Architecture (reflection unit test)

**Description:**
The production assembly must reference no Scriban assembly. This test loads the assembly via
reflection and checks all `GetReferencedAssemblies()` entries.

**Test Steps:**
1. Call `typeof(TerraformPlan).Assembly.GetReferencedAssemblies()`
2. Assert no entry has a `Name` equal to `"Scriban"` (case-insensitive)

**Expected Result:** Zero referenced assemblies matching `Scriban`.

**Test Name:** `Assembly_ProductionAssembly_HasNoScribanReference`

---

### TC-S02: Scriban Infrastructure Types Are Absent

**Type:** Build / Compilation (CI gate)

**Description:**
`AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver`, and `ScribanHelperException`
are deleted. A green build is the test — no compilation errors caused by lingering references.

**Expected Result:** `dotnet build` exits with code 0.

---

### TC-S03: TrimmerRootDescriptor.xml Contains No Scriban Entries

**Type:** File-content assertion

**Description:**
Either `TrimmerRootDescriptor.xml` is deleted, or it contains no text matching `Scriban`.

**Test Name:** `TrimmerRootDescriptor_DoesNotContain_ScribanEntries`

---

### TC-S04: No Production Code Imports Scriban Namespaces

**Type:** Architecture (NetArchTest)

**Description:**
Uses `NetArchTest.Rules` to assert that no type in the production assembly depends on any
`Scriban.*` namespace.

**Test Steps:**
```csharp
var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
    .That().ResideInNamespace("Oocx.TfPlan2Md")
    .ShouldNot().HaveDependencyOn("Scriban")
    .GetResult();
result.IsSuccessful.Should().BeTrue();
```

**Expected Result:** Zero failing types.

**Test Name:** `ProductionCode_ShouldNotDependOn_Scriban`

**Location:** Add to `Architecture/ArchitectureBoundaryTests.cs`.

---

### TC-S05: All Existing Snapshot Tests Pass Without Snapshot Modification

**Type:** Snapshot (existing tests — no implementation changes)

**Description:**
Every snapshot file in `TestData/Snapshots/` must match the output of the new pure C#
renderers **without any snapshot file modification**. Any snapshot diff is a defect.

**Covered test classes:**
`MarkdownSnapshotTests`, `AzapiSnapshotTests`, `AzureAdSnapshotTests`,
`AzureDevOpsSnapshotTests`, `ParentChildUatSnapshotTests`, `EphemeralSnapshotTests`,
`KnownAfterApplySnapshotTests`, `OutputsSnapshotTests`,
`ParentChildConditionalColumnSnapshotTests`

**Expected Result:** All snapshot tests pass; zero snapshot files modified.

---

### TC-S06: NativeAOT Binary Builds Successfully

**Type:** CI build

**Description:**
The `publish-nativeaot` CI step succeeds without `TrimmerRootDescriptor.xml` and without the
Scriban package. Verified by the existing CI workflow.

---

### TC-S07: All Provider Modules Expose IResourceRenderer Implementations

**Type:** Architecture (reflection unit test)

**Description:**
1. At least one `IResourceRenderer` implementation exists per provider namespace
   (`AzureRM`, `AzApi`, `AzureAD`, `AzureDevOps`).
2. No public method named `RegisterHelpers` accepting a `ScriptObject`-typed parameter exists.

**Test Names:**
- `IResourceRenderer_HasImplementationsForAllProviders`
- `ProviderModules_DoNotExpose_RegisterHelpersWithScriptObject`

---

### TC-S08: Existing Architecture Boundary Tests Still Pass

**Type:** Architecture (existing tests)

**Description:**
All rules in `ArchitectureBoundaryTests.cs` pass after the refactoring. In particular,
`MarkdownGeneration_ShouldNotDependOn_Providers` must pass with **zero exemptions** after
the removal of `AotScriptObjectMapper` and the 3 `ScriptObject` mapper files.

---

### TC-S09: Scriban Exemptions Removed from Architecture Tests

**Type:** Architecture (code review)

**Description:**
Exemption clauses in `ArchitectureBoundaryTests.cs` for `AotScriptObjectMapper` and related
types must be deleted. The rules pass without exclusions.

**Expected Result:** Zero `// EXEMPTION` or exclusion-list comments referencing
`AotScriptObjectMapper`, `ScriptObject`, or Scriban.

---

## Section 2: New Type Unit Tests

These tests provide 100% branch coverage for every new class introduced by the migration.

---

## 2.1 MarkdownWriter

### TC-MW-01: Heading — Each Level Produces Correct Markdown Prefix

**Branches:** Levels 1–6

**Test Names:**
- `Heading_Level1_RendersHashPrefix`
- `Heading_Level3_RendersTripleHash`
- `Heading_Level6_RendersSixHashes`

---

### TC-MW-02: Paragraph and BlankLine

**Branches:**
- `Paragraph` with non-empty text
- `Paragraph` with empty string
- `BlankLine` after other content

**Test Names:**
- `Paragraph_NonEmptyText_RendersText`
- `Paragraph_EmptyString_RendersEmptyParagraph`

---

### TC-MW-03: TableHeader and TableRow

**Branches:**
- Single-column header
- Multi-column header
- Row cell containing `|` (must be escaped)
- Row cell that is empty

**Test Names:**
- `TableHeader_MultipleColumns_RendersCorrectSeparatorRow`
- `TableRow_CellContainingPipe_EscapedInOutput`
- `TableRow_EmptyCell_RendersEmptyCell`

---

### TC-MW-04: DetailsOpen and DetailsClose

**Branches:**
- `DetailsOpen` with `open = true` — produces `open` attribute
- `DetailsOpen` with `open = false` — no `open` attribute
- Matching `DetailsClose` produces `</details>` tag

**Test Names:**
- `DetailsOpen_OpenTrue_ContainsOpenAttribute`
- `DetailsOpen_OpenFalse_NoOpenAttribute`
- `DetailsClose_AfterDetailsOpen_ProducesClosingTag`

---

### TC-MW-05: Code and InlineCode

**Branches:**
- `Code` with single-line content
- `Code` with multi-line content
- `InlineCode` with non-empty text
- `InlineCode` with empty string

**Test Names:**
- `Code_SingleLine_RendersInFencedBlock`
- `Code_MultiLine_RendersAllLinesInBlock`
- `InlineCode_EmptyContent_RendersEmptyBackticks`

---

### TC-MW-06: Raw

**Branches:**
- Non-empty string appended verbatim
- Empty string has no effect on output

**Test Names:**
- `Raw_NonEmptyString_AppendedVerbatim`
- `Raw_EmptyString_OutputUnchanged`

---

### TC-MW-07: Build Normalization — Blank Lines Between Table Rows Removed

**Description:**
A blank line between two table rows is stripped by `Build()`.

**Test Name:** `Build_BlankLineBetweenTableRows_LineRemoved`

---

### TC-MW-08: Build Normalization — Indentation Stripped from Table Rows

**Test Name:** `Build_IndentedTableRow_IndentationStripped`

---

### TC-MW-09: Build Normalization — Multiple Blank Lines Collapsed

**Description:**
Three or more consecutive blank lines are collapsed to one.

**Test Name:** `Build_ThreeConsecutiveBlankLines_CollapsedToOne`

---

### TC-MW-10: Build Normalization — Blank Lines Inserted Around Headings

**Branches:**
- Heading without preceding blank line → blank line inserted before
- Heading without following blank line → blank line inserted after
- Heading already surrounded by blank lines → no duplication

**Test Names:**
- `Build_HeadingWithoutPrecedingBlankLine_BlankLineInsertedBefore`
- `Build_HeadingWithoutFollowingBlankLine_BlankLineInsertedAfter`
- `Build_HeadingAlreadySurrounded_NoExtraBlankLines`

---

## 2.2 ResourceRendererRegistry

### TC-RRR-01: GetRenderer — Registered Type Returns Renderer

**Test Name:** `GetRenderer_RegisteredResourceType_ReturnsRegisteredRenderer`

---

### TC-RRR-02: GetRenderer — Unregistered Type Returns Null

**Test Name:** `GetRenderer_UnregisteredResourceType_ReturnsNull`

---

### TC-RRR-03: Register — Duplicate Registration Behavior Is Pinned

**Description:**
Registering a second renderer for the same resource type either replaces the first
(last-writer-wins) or throws. Test pins the chosen behavior to prevent regression.

**Test Name:**
- `Register_DuplicateResourceType_ReplacesExistingRenderer` *(if last-writer-wins)*
- OR `Register_DuplicateResourceType_ThrowsServiceRegistrationException` *(if throw)*

---

## 2.3 RenderContext

### TC-RC-01: Construction — All Properties Stored Correctly

**Branches:**
- `ShowSensitive = true` and `= false`
- `ShowUnchangedValues = true` and `= false`
- `IgnoreAzureIdCaseChanges = true` and `= false`
- Each `RenderTarget` value
- Each `DetailsDisplayMode` value

**Test Names:**
- `RenderContext_ShowSensitiveTrue_PropertyIsTrue`
- `RenderContext_ShowSensitiveFalse_PropertyIsFalse`
- `RenderContext_RenderTargetGitHub_PropertyIsGitHub`
- `RenderContext_RenderTargetAzureDevOps_PropertyIsAzureDevOps`

---

## 2.4 ReportRenderer

### TC-RR-01: Empty Model — Header and Summary Only

**Description:** A `ReportModel` with zero changes produces only header and summary — no
resource rows, no code analysis, no outputs.

**Test Name:** `Render_EmptyModel_ContainsOnlyHeaderAndSummary`

---

### TC-RR-02: Root Module — No Module Heading

**Description:** Resources in the root module are rendered without a "📦 Module: …" heading.

**Test Name:** `Render_RootModule_NoModuleHeadingInOutput`

---

### TC-RR-03: Named Module — Module Heading Present

**Test Name:** `Render_NamedModule_ModuleHeadingInOutput`

---

### TC-RR-04: Model with Code Analysis — Code Analysis Sections Rendered

**Test Name:** `Render_ModelWithCodeAnalysis_CodeAnalysisSectionsPresent`

---

### TC-RR-05: Model without Code Analysis — No Code Analysis Content

**Test Name:** `Render_ModelWithoutCodeAnalysis_NoCodeAnalysisContent`

---

### TC-RR-06: Model with Refactoring Operations — Refactoring Section Rendered

**Test Name:** `Render_ModelWithRefactoringOperations_RefactoringSectionPresent`

---

### TC-RR-07: Unknown Resource Type — Falls Back to DefaultResourceRenderer

**Description:** When `GetRenderer` returns `null`, `DefaultResourceRenderer` is used.
No exception is thrown.

**Test Name:** `Render_UnknownResourceType_FallsBackToDefaultRenderer`

---

## 2.5 HeaderRenderer

### TC-HR-01: HideMetadata False — All Metadata Fields Present

**Test Name:** `Render_HideMetadataFalse_AllMetadataFieldsPresent`

---

### TC-HR-02: HideMetadata True — No Metadata Fields

**Test Name:** `Render_HideMetadataTrue_NoMetadataFields`

---

### TC-HR-03: Custom vs. Default Report Title

**Branches:**
- `model.ReportTitle` non-null → custom title used
- `model.ReportTitle` null → default title used

**Test Names:**
- `Render_CustomReportTitle_CustomTitleInOutput`
- `Render_NullReportTitle_DefaultTitleInOutput`

---

## 2.6 SummaryRenderer

### TC-SR-01: All Action Counts Non-Zero — All Rows Present

**Test Name:** `Render_AllActionsNonZero_AllCountRowsPresent`

---

### TC-SR-02: Zero-Count Row Behavior Is Pinned

**Description:** Documents whether zero-count action rows are shown or omitted, preventing
accidental behavior changes.

**Test Name:** `Render_ZeroActionCount_BehaviorMatchesSpec`

---

### TC-SR-03: Summary-Only Mode — No Resource Detail Sections Follow

**Test Name:** `Render_SummaryOnlyMode_NoResourceDetailContent`

---

## 2.7 DefaultResourceRenderer

### TC-DR-01: Create — After Attributes in Table

**Test Name:** `Render_Create_AfterAttributesRendered`

---

### TC-DR-02: Delete — Before Attributes in Table

**Test Name:** `Render_Delete_BeforeAttributesRendered`

---

### TC-DR-03: Update — Before/After Diff Columns

**Test Name:** `Render_Update_BeforeAndAfterColumnsPresent`

---

### TC-DR-04: Replace — Action Badge Shows Replace

**Test Name:** `Render_Replace_ActionBadgeIsReplace`

---

### TC-DR-05: Sensitive Attribute — Masked as "(sensitive)"

**Test Name:** `Render_SensitiveAttribute_DisplaysAsSensitivePlaceholder`

---

### TC-DR-06: Unknown-After-Apply Attribute — Known-After-Apply Indicator

**Test Name:** `Render_UnknownAfterApplyAttribute_KnownAfterApplyIndicatorShown`

---

### TC-DR-07: ShowUnchangedValues False — Unchanged Attributes Omitted

**Test Name:** `Render_ShowUnchangedValuesFalse_UnchangedAttributesOmitted`

---

### TC-DR-08: ShowUnchangedValues True — Unchanged Attributes Included

**Test Name:** `Render_ShowUnchangedValuesTrue_UnchangedAttributesIncluded`

---

## 2.8 ChildResourceRenderer

### TC-CR-01: Resource with Child Groups — One Table per Group

**Test Name:** `Render_WithChildGroups_OneTablePerGroup`

---

### TC-CR-02: Resource with No Child Groups — No Child Section

**Test Name:** `Render_NoChildGroups_NoChildSection`

---

## 2.9 CodeAnalysisRenderer

### TC-CA-01: RenderSummary — With Code Analysis

**Test Name:** `RenderSummary_WithCodeAnalysis_SummarySectionPresent`

---

### TC-CA-02: RenderSummary — Null Code Analysis Produces No Output

**Test Name:** `RenderSummary_NullCodeAnalysis_NoOutput`

---

### TC-CA-03: RenderFindings — Resource with Findings

**Test Name:** `RenderFindings_ResourceWithFindings_FindingsSubSectionRendered`

---

### TC-CA-04: RenderFindings — Resource with No Findings Produces No Output

**Test Name:** `RenderFindings_ResourceWithNoFindings_NoOutput`

---

### TC-CA-05: RenderFindings — Other (Unmapped) Findings Section

**Test Name:** `RenderFindings_OtherFindings_OtherFindingsSectionPresent`

---

### TC-CA-06: RenderFindings — Tool Metadata Section

**Test Name:** `RenderFindings_WithToolMetadata_MetadataSectionPresent`

---

## 2.10 RefactoringRenderer

### TC-RF-01: Import Operations — Import Section Rendered

**Test Name:** `Render_ImportOperation_ImportSectionRendered`

---

### TC-RF-02: Move Operations — Move Section Rendered

**Test Name:** `Render_MoveOperation_MoveSectionRendered`

---

### TC-RF-03: Already Applied — Not Rendered

**Test Name:** `Render_AlreadyAppliedOperation_SectionNotRendered`

---

### TC-RF-04: Empty Refactoring Operations — No Output

**Test Name:** `Render_EmptyRefactoringOperations_NoOutput`

---

## 2.11 OutputRenderer

### TC-OR-01: Module Outputs with Changes — Action Rows Present

**Test Name:** `Render_ModuleOutputs_AllActionRowsPresent`

---

### TC-OR-02: Empty Module Outputs — No Outputs Section

**Test Name:** `Render_EmptyModuleOutputs_NoOutputsSection`

---

### TC-OR-03: Global Outputs — Global Heading Present

**Test Name:** `RenderGlobal_WithOutputs_GlobalHeadingPresent`

---

### TC-OR-04: Sensitive Output — Value Masked

**Test Name:** `Render_SensitiveOutput_ValueMasked`

---

## Section 3: Provider Renderer Unit Tests

Each renderer is tested with a synthetic `ResourceChangeModel` constructed inline. Branches
focus on decision points NOT fully isolated by existing snapshot tests.

---

## 3.1 RoleAssignmentRenderer

### TC-ARM-01 to TC-ARM-04

| # | Test Name |
|---|-----------|
| TC-ARM-01 | `Render_RoleAssignment_Create_AllFieldsRendered` |
| TC-ARM-02 | `Render_RoleAssignment_Update_DiffColumnPresent` |
| TC-ARM-03 | `Render_RoleAssignment_Delete_BeforeValuesShown` |
| TC-ARM-04a | `Render_RoleAssignment_WithPrincipalMapping_DisplaysName` |
| TC-ARM-04b | `Render_RoleAssignment_WithoutPrincipalMapping_DisplaysRawId` |

---

## 3.2 NsgRenderer

### TC-ARM-05 to TC-ARM-08

| # | Test Name |
|---|-----------|
| TC-ARM-05 | `Render_Nsg_Create_AllRulesInTable` |
| TC-ARM-06 | `Render_Nsg_Update_ModifiedRulesShowDiff` |
| TC-ARM-07 | `Render_Nsg_Delete_BeforeStateRulesShown` |
| TC-ARM-08 | `Render_Nsg_EmptyRulesCollection_NoException` |

---

## 3.3 FirewallNetworkRuleRenderer

### TC-ARM-09 to TC-ARM-12

| # | Test Name |
|---|-----------|
| TC-ARM-09 | `Render_FirewallNetworkRule_Create_AllRulesPresent` |
| TC-ARM-10 | `Render_FirewallNetworkRule_Update_DiffPresent` |
| TC-ARM-11 | `Render_FirewallNetworkRule_Delete_BeforeRulesShown` |
| TC-ARM-12 | `Render_FirewallNetworkRule_EmptyCollection_NoException` |

---

## 3.4 FirewallAppRuleRenderer

### TC-ARM-13 to TC-ARM-16

| # | Test Name |
|---|-----------|
| TC-ARM-13 | `Render_FirewallAppRule_Create_AllRulesPresent` |
| TC-ARM-14 | `Render_FirewallAppRule_Update_DiffPresent` |
| TC-ARM-15 | `Render_FirewallAppRule_Delete_BeforeRulesShown` |
| TC-ARM-16 | `Render_FirewallAppRule_EmptyCollection_NoException` |

---

## 3.5 AzApiResourceRenderer

### TC-API-01 to TC-API-05

| # | Test Name |
|---|-----------|
| TC-API-01 | `Render_AzApiResource_Create_BodyGroupedIntoSections` |
| TC-API-02 | `Render_AzApiResource_Update_DiffTablePerChangedAttribute` |
| TC-API-03 | `Render_AzApiResource_Delete_BeforeBodyShown` |
| TC-API-04 | `Render_AzApiResource_WithOutputValues_OutputValuesSectionPresent` |
| TC-API-05 | `Render_AzApiResource_WithoutOutputValues_NoOutputValuesSection` |

---

## 3.6 AzApiUpdateResourceRenderer

### TC-API-06 to TC-API-09

| # | Test Name |
|---|-----------|
| TC-API-06 | `Render_AzApiUpdateResource_Update_DiffTableRendered` |
| TC-API-07 | `Render_AzApiUpdateResource_NoBodyChanges_OnlyMetadataSection` |
| TC-API-08 | `Render_AzApiUpdateResource_WithOutputValues_SectionPresent` |
| TC-API-09 | `Render_AzApiUpdateResource_WithoutOutputValues_NoSection` |

---

## 3.7 AzApiOutputValuesRenderer

### TC-API-10 to TC-API-14

| # | Test Name |
|---|-----------|
| TC-API-10 | `Render_AzApiOutputValues_CleanValues_BeforeAfterRendered` |
| TC-API-11 | `Render_AzApiOutputValues_AfterUnknownTrue_KnownAfterApplyNotice` |
| TC-API-12 | `Render_AzApiOutputValues_BeforeSensitiveTrue_BeforeValueMasked` |
| TC-API-13 | `Render_AzApiOutputValues_AfterSensitiveTrue_AfterValueMasked` |
| TC-API-14 | `Render_AzApiOutputValues_NoOutputValuesPresent_SectionOmitted` |

---

## 3.8 AzureAD Renderers

### UserRenderer — TC-AD-01 to TC-AD-03

| # | Test Name |
|---|-----------|
| TC-AD-01 | `Render_AzureAdUser_Create_UserFieldsRendered` |
| TC-AD-02 | `Render_AzureAdUser_Update_DiffPresent` |
| TC-AD-03 | `Render_AzureAdUser_Delete_BeforeFieldsShown` |

### GroupRenderer — TC-AD-04 to TC-AD-06

| # | Test Name |
|---|-----------|
| TC-AD-04 | `Render_AzureAdGroup_Create_GroupAndMembersRendered` |
| TC-AD-05 | `Render_AzureAdGroup_Update_DiffPresent` |
| TC-AD-06 | `Render_AzureAdGroup_Delete_BeforeFieldsShown` |

### GroupWithoutMembersRenderer — TC-AD-07 to TC-AD-09

| # | Test Name |
|---|-----------|
| TC-AD-07 | `Render_AzureAdGroupWithoutMembers_Create_GroupFieldsRendered` |
| TC-AD-08 | `Render_AzureAdGroupWithoutMembers_Update_DiffPresent` |
| TC-AD-09 | `Render_AzureAdGroupWithoutMembers_Delete_BeforeFieldsShown` |

### GroupMemberRenderer — TC-AD-10 to TC-AD-12

| # | Test Name |
|---|-----------|
| TC-AD-10 | `Render_AzureAdGroupMember_Create_MemberFieldsRendered` |
| TC-AD-11 | `Render_AzureAdGroupMember_Update_DiffPresent` |
| TC-AD-12 | `Render_AzureAdGroupMember_Delete_BeforeFieldsShown` |

### ServicePrincipalRenderer — TC-AD-13 to TC-AD-15

| # | Test Name |
|---|-----------|
| TC-AD-13 | `Render_AzureAdServicePrincipal_Create_FieldsRendered` |
| TC-AD-14 | `Render_AzureAdServicePrincipal_Update_DiffPresent` |
| TC-AD-15 | `Render_AzureAdServicePrincipal_Delete_BeforeFieldsShown` |

### InvitationRenderer — TC-AD-16 to TC-AD-18

| # | Test Name |
|---|-----------|
| TC-AD-16 | `Render_AzureAdInvitation_Create_InvitationFieldsRendered` |
| TC-AD-17 | `Render_AzureAdInvitation_Update_DiffPresent` |
| TC-AD-18 | `Render_AzureAdInvitation_Delete_BeforeFieldsShown` |

---

## 3.9 VariableGroupRenderer

### TC-ADO-01 to TC-ADO-04

| # | Test Name |
|---|-----------|
| TC-ADO-01 | `Render_VariableGroup_Create_AllVariablesRendered` |
| TC-ADO-02 | `Render_VariableGroup_Update_DiffPresent` |
| TC-ADO-03 | `Render_VariableGroup_Delete_BeforeVariablesShown` |
| TC-ADO-04 | `Render_VariableGroup_SensitiveVariable_ValueMasked` |

---

## 3.10 BuildDefinitionRenderer

### TC-ADO-05 to TC-ADO-10

| # | Test Name |
|---|-----------|
| TC-ADO-05 | `Render_BuildDefinition_Create_AllSectionsRendered` |
| TC-ADO-06 | `Render_BuildDefinition_Update_DiffPresent` |
| TC-ADO-07 | `Render_BuildDefinition_Delete_BeforeStateShown` |
| TC-ADO-08 | `Render_BuildDefinition_NoVariables_VariablesSectionOmitted` |
| TC-ADO-09 | `Render_BuildDefinition_NoTriggers_TriggersSectionOmitted` |
| TC-ADO-10 | `Render_BuildDefinition_NoOtherBlocks_OtherBlocksSectionOmitted` |

---

## Test Data Requirements

No new `.json` test data files are required. All new renderer tests use synthetic
`ResourceChangeModel` instances constructed inline. Where setup is repeated across tests, a
shared builder helper may be added to `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`.

---

## Test File Structure

New test files under `src/tests/Oocx.TfPlan2Md.TUnit/`:

```
MarkdownGeneration/Rendering/
    MarkdownWriterTests.cs              # TC-MW-01 to TC-MW-10
    ResourceRendererRegistryTests.cs    # TC-RRR-01 to TC-RRR-03
    RenderContextTests.cs               # TC-RC-01
    ReportRendererTests.cs              # TC-RR-01 to TC-RR-07
    HeaderRendererTests.cs              # TC-HR-01 to TC-HR-03
    SummaryRendererTests.cs             # TC-SR-01 to TC-SR-03
    DefaultResourceRendererTests.cs     # TC-DR-01 to TC-DR-08
    ChildResourceRendererTests.cs       # TC-CR-01 to TC-CR-02
    CodeAnalysisRendererTests.cs        # TC-CA-01 to TC-CA-06
    RefactoringRendererTests.cs         # TC-RF-01 to TC-RF-04
    OutputRendererTests.cs              # TC-OR-01 to TC-OR-04
Providers/AzureRM/
    RoleAssignmentRendererTests.cs      # TC-ARM-01 to TC-ARM-04
    NsgRendererTests.cs                 # TC-ARM-05 to TC-ARM-08
    FirewallNetworkRuleRendererTests.cs # TC-ARM-09 to TC-ARM-12
    FirewallAppRuleRendererTests.cs     # TC-ARM-13 to TC-ARM-16
Providers/AzApi/
    AzApiResourceRendererTests.cs       # TC-API-01 to TC-API-05
    AzApiUpdateResourceRendererTests.cs # TC-API-06 to TC-API-09
    AzApiOutputValuesRendererTests.cs   # TC-API-10 to TC-API-14
Providers/AzureAD/
    AzureAdRendererTests.cs             # TC-AD-01 to TC-AD-18
Providers/AzureDevOps/
    VariableGroupRendererTests.cs       # TC-ADO-01 to TC-ADO-04
    BuildDefinitionRendererTests.cs     # TC-ADO-05 to TC-ADO-10
```

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|----------|
| Scriban assembly still present in output | TC-S01 catches assembly reference | TC-S01 |
| Any snapshot file requires modification | Blocking defect — stop and fix the renderer | TC-S05 |
| `TrimmerRootDescriptor.xml` not deleted but still has Scriban entry | TC-S03 catches it | TC-S03 |
| `RenderContext.ShowSensitive = true` exposes real values | Existing `SensitivityHierarchyTests` cover this | TC-S05 |
| `ModuleChangeGroup` with empty `Changes` list | `ReportRenderer` renders no resource rows | TC-RR-01 |
| `NsgRenderer` with no security rules | Clean output, no null-ref exception | TC-ARM-08 |
| `AzApiOutputValuesRenderer` with both before and after sensitive | Both cells masked independently | TC-API-12 + TC-API-13 |
| `BuildDefinitionRenderer` with none of the 3 optional sections | No section headings rendered, no exception | TC-ADO-08 to TC-ADO-10 |
| `ResourceRendererRegistry.GetRenderer` returns null for unknown type | `ReportRenderer` falls back to `DefaultResourceRenderer` | TC-RRR-02 + TC-RR-07 |

---

## Non-Functional Tests

### Compile-Time Safety

*"All rendering logic is statically typed"* is verified implicitly: a misnamed property is a
compiler error caught by TC-S02 (build succeeds) combined with TC-S05 (snapshots green).

### Binary Size (Informational)

The developer should note the NativeAOT binary size delta in the PR description
(~1.8 MB reduction expected). No automated assertion required.

---

## Open Questions

None. The specification and architecture document are fully resolved. All acceptance criteria
are testable and all new types are explicitly enumerated.
