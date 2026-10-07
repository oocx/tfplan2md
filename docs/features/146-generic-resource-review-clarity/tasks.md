# Tasks: Generic Resource Review Clarity

## Planning Basis

This plan implements the approved [specification](specification.md) through the
boundaries in [architecture.md](architecture.md). SC numbers refer to the 17
success criteria in specification order; T numbers refer to [test-plan.md](test-plan.md).
Tasks 1–9 belong to the Developer; Task 10 belongs to the Technical Writer.
Each implementation task includes its focused tests and can be verified separately.
No task requires live provider access. Platform UAT belongs to the UAT Tester after
code review, following [uat-test-plan.md](uat-test-plan.md).

## Tasks

### Task 1: Parse and propagate the two invocation options

**Priority:** High

**Description:** Add ordered summary-name paths and the opt-in unchanged-output
flag to CLI parsing, help, composition, and `ReportModelBuilderOptions`. Use
immutable lists and compatible defaults. This task exposes configuration; Tasks 2
and 8 implement its report behavior.

**Acceptance Criteria:**

- [x] `--summary-name-attributes` preserves supplied order, trims entry whitespace,
  and replaces the default candidate list without appending defaults (SC4; T05).
- [x] Missing values, empty/whitespace-only lists, leading/trailing commas, and
  consecutive commas produce clear existing-style CLI errors naming the option
  (SC4; T06).
- [x] Nonexistent paths parse successfully; options reach model construction and
  `--hide-unchanged-outputs` defaults to false (SC4, SC14–SC15; T05, T15).
- [x] Help states replacement semantics and opt-in output filtering (T06).

**Dependencies:** None.

### Task 2: Resolve and share safe generic display identity

**Priority:** High

**Description:** Add an internal resolver under `MarkdownGeneration/Summaries/`
that returns optional raw scalar identity and path. Store its result on the
resource model and replace independent generic HTML/text selection. Determine
fallback eligibility by explicit resource-mapping membership. Existing mappings
retain identifying choices and provide context through the shared model where
possible; provider fallback keys cannot override generic candidate order.

**Acceptance Criteria:**

- [x] Unmapped `review_object` selects the first usable candidate from all six
  documented defaults or the replacement list; zero/false are usable, while
  null, empty string, objects, and collections are skipped (SC1–SC2; T01, T05).
- [x] Nested path traversal distinguishes nested properties from literal dotted
  keys and does not parse string-valued JSON bodies (SC1, SC4; T01, T05).
- [x] Desired state wins for create/update/replace and prior state for delete;
  the other side at the same path is tried before the next candidate, including
  known prior fallback for unknown desired values (SC1–SC3; T02, T04).
- [x] Leaf, ancestor, and whole-resource sensitivity/unknown markers respect
  existing sensitivity union and authorized sensitive mode; masked or unknown
  values never become identity placeholders or leak into summaries (SC3; T03–T04).
- [x] Raw identity is escaped at HTML/text boundaries; no usable identity leaves
  a valid summary. Competing overrides preserve azapi, both msgraph types,
  azurerm, and provider-owned identifying content (SC2, SC5, SC15; T02, T07).

**Dependencies:** Task 1.

### Task 3: Preserve instance suffixes and stabilize refactoring labels

**Priority:** High

**Description:** Extract the exact final resource instance suffix with a
bracket/quote-aware scan and combine it with the existing local-name style.
Consume it in default and provider-owned headers. Use full addresses and shared
optional identifying context for refactoring operations, replacing report
assembly's independent name/url selection.

**Acceptance Criteria:**

- [x] Create, update, both replacement orders, delete, import, and move headers
  preserve string/numeric suffixes, including dots, brackets, escaped quotes,
  backslashes, and indexed modules; unindexed names gain no suffix (SC6; T08).
- [x] Existing module grouping and mapped identifying content remain intact in
  default cards and provider-owned headers (SC5–SC6, SC15; T07–T08).
- [x] Every import/move label keeps its full address, appending optional visible
  identity; supported combined import/move metadata uses the same rule (SC9; T11).
- [x] Resource/refactoring labels preserve output escaping and sensitivity policy,
  with no secret exposure or malformed markup (SC3, SC9; T02–T03, T11).

**Dependencies:** Task 2.

### Task 4: Suppress non-informative computed reference hints

**Priority:** High

**Description:** Classify references in `ReferenceSelector` after consuming
recognized module context. Apply pseudo exclusions before static-resource
selection; preserve existing meaningful-reference priorities and formatting.

**Acceptance Criteria:**

- [x] Bare and nested-module-qualified `each.key`, `each.value`, `count.index`,
  and `self` alone produce plain `(known after apply)` without colon/hint
  (SC7; T09).
- [x] Static resource, variable, local, and `each.value.group_object_id` hints
  retain existing priorities, including mixed pseudo/useful lists (SC8; T09).
- [x] Sensitive computed values retain existing masking/authorized-mode policy;
  pseudo suppression introduces no expression/source tracing (SC8; T09–T10).

**Dependencies:** None.

### Task 5: Classify empty imported prior state and render its context

**Priority:** High

**Description:** Add a provider-neutral import-context model flag and reusable
meaningful-state walker accepting excluded paths. Use existing factories to
supply housekeeping exclusions; keep MsGraph-specific declarations in its
provider directory. Render the note immediately before desired attributes in
both default and provider-owned cards.

**Acceptance Criteria:**

- [x] Null/empty state, housekeeping-only root values, `body: {}`, and nested
  null/empty strings/empty collections trigger the import note (SC10; T12).
- [x] Substantive root/nested values, including zero, false, non-empty arrays,
  and `body.id`, suppress the note; root exclusions do not suppress same-named
  nested data (SC10; T12).
- [x] Imported cards explain full desired state before attributes and make no
  newly-created claim solely from missing history; ordinary creates show no
  import note and sensitive desired values retain protection (SC10; T12).
- [x] Default and provider-owned renderers consume the shared flag without
  provider decisions in the reusable walker (SC10; T12).

**Dependencies:** None.

### Task 6: Classify MsGraph settings before primary counts

**Priority:** High

**Description:** Register `Providers/MsGraph/MsGraphModule` through existing
composition and contribution interfaces. Add factories/provider view models
classifying settings from raw before/after state. Remove secondary rows before
primary summary/count construction. Keep resource types and setting paths in
`Providers/MsGraph/`.

**Acceptance Criteria:**

- [x] Both msgraph types place unchanged settings, plus desired settings for
  creates/empty imports, in secondary models with no primary row/count entries
  in either unchanged-value mode (SC11–SC13; T13).
- [x] Against meaningful prior state, settings added, removed, or changed remain
  primary and count once, including populated imports; unchanged companions
  remain secondary (SC12–SC13; T14).
- [x] Missing settings produce no phantom rows. Secondary models preserve
  masking/computed presentation and mapped friendly context (SC3, SC11; T13).
- [x] Non-msgraph resources retain ordinary presentation for identical attribute
  names; no new provider rule is added to core mappings (SC15; T14, T17).

**Dependencies:** Tasks 2 and 5.

### Task 7: Render accessible collapsed MsGraph provider settings

**Priority:** High

**Description:** Use a MsGraph renderer sharing default rendering operations to
insert a collapsed `Provider settings` area inside each card. Extract a small
provider-neutral hook if required; do not copy the renderer or replace the
normal diff path.

**Acceptance Criteria:**

- [x] Secondary settings remain readable and accessible inside the card for
  updates, creates, and empty imports, including `--show-unchanged-values`
  (SC11–SC13; T13).
- [x] Changed settings remain in ordinary primary diffs/counts exactly once;
  import-note position and selected identity are preserved (SC10–SC13; T12–T14).
- [x] GitHub, Azure DevOps, and Bitbucket output uses established collapse/fallback
  conventions; annotations, inline actions, large values, masking, and card/table
  structure retain existing rendering behavior (SC15, SC17; T17–T18).

**Dependencies:** Task 6.

### Task 8: Filter effective unchanged outputs before grouping

**Priority:** Medium

**Description:** Filter effective no-op outputs in `BuildOutputModels` after
existing action determination and before root/module grouping. Keep the existing
value-selection and formatting path for retained outputs.

**Acceptance Criteria:**

- [x] The flag omits only effective no-ops at root and module scope, preserving
  existing absent-action handling, create/update/delete markers, before/after
  values, sensitive masking, and computed presentation (SC14; T15).
- [x] Empty output sections and output-only module headings disappear; modules
  retaining resources and non-empty section order remain valid, including
  no-output plans (SC14; T16).
- [x] Without the flag, existing output selection remains unchanged
  (SC15; T17).

**Dependencies:** Task 1.

### Task 9: Integrate acceptance coverage and prepare review artifacts

**Priority:** High

**Description:** Add complete rendered acceptance fixtures across the feature,
close test-plan coverage gaps, and prepare Developer-owned UAT artifacts to the
Quality Engineer's inventory. Regenerate intentional snapshots using the
repository skill and run focused/full checks through `run-dotnet-tests`.

**Acceptance Criteria:**

- [x] Automated tests cover every T01–T18 scenario and SC1–SC17, using synthetic
  unmapped resources separately from mapped msgraph/azapi compatibility cases;
  tests use TUnit and prescribed naming (SC16; test-plan coverage matrix).
- [x] Full-render fixtures cover actions, instances, fallback/override naming,
  sensitive/unknown identities, meaningful/pseudo hints, refactoring labels,
  empty/populated imports, settings modes/counts, and root/module filtering
  (SC16; T02–T17).
- [x] GitHub, Azure DevOps, and Bitbucket deterministic assertions check escaping,
  note placement, settings access, counts/diffs, and output selection; existing
  fixture changes are limited to approved clarity behavior (SC15–SC17; T17–T18).
- [x] `uat-plan.json`, default rendered `uat-plan.md`, documented variants/options,
  and the root-only-no-op variant match `uat-test-plan.md`; no platform UAT is
  claimed from snapshots or assigned to the Developer (SC17; UAT inventory).
- [x] Intentional snapshots use `update-test-snapshots`; their commit includes
  `SNAPSHOT_UPDATE_OK` with rationale. Record check results and remaining
  limitations for Code Reviewer and UAT Tester handoff (SC15–SC17; T17–T18).

**Dependencies:** Tasks 3, 4, 5, 7, and 8.

### Task 10: Document invocation and review behavior

**Priority:** Medium

**Description:** The Technical Writer updates global user documentation in the
same feature change, using implemented behavior and prepared examples. Developer
handoff supplies option defaults/errors and concrete output examples.

**Acceptance Criteria:**

- [x] Documentation explains ordered defaults, replacement override semantics,
  nested paths, unusable candidates, mapped-summary compatibility, and exact
  indexed labels (SC1–SC6).
- [x] Documentation explains pseudo-hint suppression, stable refactoring labels,
  empty-import context, secondary versus changed settings, and unchanged-value
  mode (SC7–SC13).
- [x] Documentation describes opt-in root/module no-op output removal and preserved
  defaults, with examples of both new options; `CHANGELOG.md` is untouched
  (SC14–SC15).

**Dependencies:** Task 9.

## Implementation Order

1. Task 1 establishes invocation configuration; Task 2 resolves shared identity;
   Task 3 uses it in instance and refactoring labels.
2. Tasks 4 and 5 are independently verifiable reference/import changes.
3. Task 6 builds MsGraph classification on identity and import state, then Task 7
   renders those classified rows and verifies primary counts/presentation together.
4. Task 8 consumes the invocation flag and can be implemented after Task 1.
5. Task 9 integrates all paths, checks defaults/targets, and prepares review evidence.
6. Task 10 runs in the Technical Writer workflow stage before code review and UAT.

This order keeps selection and classification testable before renderer integration.
Developer tasks are completed and committed before the workflow advances; the
workflow stages remain authoritative for review, UAT, and release.

## Reverse Coverage Mapping

| Specification criteria | Tasks |
| --- | --- |
| SC1–SC2: ordered fallback and unusable values | 2, 9, 10 |
| SC3: sensitive/unknown identity | 2, 3, 6, 9 |
| SC4: override and validation | 1, 2, 9, 10 |
| SC5: mapped compatibility | 2, 3, 9, 10 |
| SC6: all-action suffixes | 3, 9, 10 |
| SC7–SC8: pseudo/useful computed hints | 4, 9, 10 |
| SC9: full-address refactoring | 3, 9, 10 |
| SC10: import context | 5, 7, 9, 10 |
| SC11–SC13: settings availability, counts, modes | 6, 7, 9, 10 |
| SC14: root/module filtering | 1, 8, 9, 10 |
| SC15: compatible defaults | 1, 2, 3, 6, 7, 8, 9, 10 |
| SC16: automated acceptance breadth | 9 |
| SC17: supported targets | 7, 9; UAT Tester validates platform legibility |

| Test-plan scenarios | Tasks |
| --- | --- |
| T01–T04: candidates, states, sensitivity, unknowns | 2; 3 extends labels; 9 integrates |
| T05–T06: CLI override/errors | 1, 2, 9 |
| T07–T08: mappings and exact instances | 2, 3, 9 |
| T09–T10: references and computed masking | 4, 9 |
| T11: refactoring labels | 3, 9 |
| T12: meaningful imported prior state | 5, 7, 9 |
| T13–T14: secondary/changed settings | 6, 7, 9 |
| T15–T16: filtering and empty sections | 1, 8, 9 |
| T17: default regression | 2, 3, 6, 7, 8, 9 |
| T18: target acceptance and UAT preparation | 7, 9; UAT Tester executes platform UAT |
