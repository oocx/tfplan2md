# Tasks: Configurable, Aggregated Drift Rendering

## Tasks

### Task 1: Add and propagate the drift display mode
**Priority:** High
**Description:** Add the provider-neutral `DriftDisplayMode` (`All`, `Relevant`, `None`) and carry it through `CliOptions`, the main CLI parser, validation/help or error reporting, and the composition root into `ReportModelBuilderOptions`. Preserve `All` as the default when the option is omitted. Parse values case-insensitively and reject missing or invalid values with an error that lists `all`, `relevant`, and `none`.
**Acceptance Criteria:**
- [x] `CliParser` returns `All` for an omitted `--drift` and for explicit `all`, and returns the matching mode for `relevant` and `none`, including mixed-case input; the plan path remains intact (CLI-01).
- [x] `--drift unexpected` and a trailing `--drift` both fail parsing, identify the invalid or missing value, and name all accepted values (CLI-02, CLI-03).
- [x] The parsed mode reaches `ReportModelBuilder` without changing unrelated CLI options or default behavior.
- [x] Automated parser tests cover omission, all three explicit modes, mixed case, invalid input, and missing input.
**Dependencies:** None

### Task 2: Introduce deterministic drift groups and grouping logic
**Priority:** High
**Description:** Replace the resource-card-shaped drift report property with a dedicated `DriftGroupModel` containing resource type, attribute path, normalized before and after display values, and affected addresses. Implement provider-neutral flattening and grouping of already display-filtered drift attribute changes. Group only on the complete tuple (type, path, before, after), deduplicate addresses, and order groups and addresses with ordinal comparisons.
**Acceptance Criteria:**
- [x] Two matching candidates produce one group with count two and exactly their addresses (DRIFT-01).
- [x] A difference in resource type, path, normalized before value, or normalized after value produces a separate group; candidates whose raw values normalize identically group together (DRIFT-03).
- [x] A resource with multiple changed paths contributes one candidate/group per path, and duplicate addresses are emitted once (edge cases, DRIFT-04).
- [x] Groups sort by type, path, before, then after, and addresses sort ordinally regardless of input or dictionary order (DRIFT-04).
- [x] Grouping consumes the canonical normalized, masked display strings from the existing attribute-change pipeline; no provider-specific renderer registry is consulted.
- [x] Model-level TUnit tests cover matching keys, each differing key component, normalization, multiple paths, deduplication, and deterministic ordering.
**Dependencies:** Task 1

### Task 3: Apply drift mode selection before grouping and preserve filtering
**Priority:** High
**Description:** Integrate mode-aware drift selection into report model building after existing attribute and display filtering, then pass the resulting groups through report assembly. `All` selects all displayable drift, `Relevant` intersects exact ordinal addresses with `displayChanges`, and `None` produces no groups. Ensure no-op and fully suppressed planned or drift changes cannot make or appear in relevant output.
**Acceptance Criteria:**
- [x] `All` and the default builder options include every displayable drift resource, whether or not it has a planned change (DRIFT-05).
- [x] `Relevant` filters candidates before grouping and retains only drift addresses with displayable planned changes; excluded addresses do not appear in counts or lists (DRIFT-06).
- [x] A Terraform no-op or fully attribute-suppressed planned change does not make drift relevant, while the displayable planned change does; no-op and fully suppressed drift remain absent from both rendering modes (DRIFT-07).
- [x] A drift resource whose attributes are all suppressed produces no candidate/group in `All` or `Relevant` (DRIFT-09).
- [x] Plans with absent, no-op, or fully suppressed drift produce no groups in `All`, `Relevant`, or `None` (DRIFT-10).
- [x] Model/integration TUnit tests prove selection precedes grouping, relevant membership uses `displayChanges` with ordinal address equality, and existing filtering is retained.
**Dependencies:** Task 2

### Task 4: Render grouped drift details and empty states
**Priority:** High
**Description:** Update `ReportRenderer` to render each `DriftGroupModel` as one collapsed `<details>` element with the icon, count, resource type, code-formatted path, and code-formatted before-to-after values, followed by all code-formatted address bullets. Use existing Markdown escaping helpers for every interpolated value and omit the whole section when there are no groups.
**Acceptance Criteria:**
- [x] A two-address group renders exactly one non-opened `<details>` and `<summary>`, with all required summary fields and both code-formatted address bullets (DRIFT-02).
- [x] A single-member group uses the same collapsed details structure and reports count `1` (edge cases).
- [x] Type, paths, values, and addresses containing HTML delimiters, backticks, or line breaks remain safely escaped and cannot break the summary/details markup; sensitive values remain masked (edge cases).
- [x] `None`, an empty group list, and plans with no displayable drift render neither the drift heading nor drift details (DRIFT-08, DRIFT-10).
- [x] Renderer tests assert exact collapsed structure, summary content, complete address lists, escaping, and empty-section behavior.
**Dependencies:** Task 3

### Task 5: Update default-output regression coverage and snapshots
**Priority:** Medium
**Description:** Add or update end-to-end and snapshot coverage for the new default grouped layout while retaining existing planned-change, refactoring, relevant-attribute, and plan-status output. Regenerate only the snapshots intentionally changed by the drift layout using the repository snapshot script and document the intentional update in the commit message.
**Acceptance Criteria:**
- [x] Default output is equivalent in drift selection to `--drift all` and uses grouped summaries even for a single displayable drift entry.
- [x] Existing non-drift sections and provider-specific planned resource rendering remain unchanged.
- [x] Regression/snapshot coverage exercises grouped addresses, separate value-transition groups, all three modes, and no-heading empty output; all automated tests pass.
- [x] Snapshot files are regenerated with `scripts/update-test-snapshots.sh` and the committing message includes `SNAPSHOT_UPDATE_OK` with the reason.
**Dependencies:** Task 4

## Implementation Order

Implement Task 1 first so the mode is a stable input at every composition boundary. Build the standalone group model and deterministic algorithm in Task 2 before integrating selection in Task 3; this keeps key semantics independently testable and makes it possible to verify that filtering precedes grouping. Task 4 can then focus solely on the new report shape and escaping. Finish with Task 5 to update broad regression snapshots after the model and renderer contracts are settled.
