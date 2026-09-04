# Test Plan: Configurable, Aggregated Drift Rendering

## Overview

This feature changes the selection and shape of rendered Terraform drift while
preserving existing display filtering. Tests prove selection happens after filtering
and before grouping, grouping uses normalized display values, and every grouped
address remains available without rendering an empty drift section. All automated
tests use TUnit and the `MethodName_Scenario_ExpectedResult` naming convention.

## Test Coverage Matrix

| Criterion | Test case(s) | Type |
| --- | --- | --- |
| Matching type, path, and normalized transition render as one entry | DRIFT-01, DRIFT-02 | Model unit; renderer unit |
| Different normalized transitions remain separate | DRIFT-03 | Model unit |
| Summary identifies type, path, count, and transition | DRIFT-02 | Renderer unit |
| Every address is in a collapsed details list | DRIFT-02, DRIFT-04 | Renderer unit; snapshot |
| `all` includes all displayable drift; omission is equivalent | DRIFT-05, CLI-01 | Model unit; CLI parser unit |
| `relevant` uses only displayable planned changes, excluding no-ops | DRIFT-06, DRIFT-07 | Model unit |
| `none` omits the complete drift section | DRIFT-08 | Model and renderer unit |
| Selection happens before grouping | DRIFT-06 | Model unit |
| No-op and fully suppressed drift stay absent | DRIFT-07, DRIFT-09 | Model integration |
| Invalid `--drift` identifies all accepted values | CLI-02, CLI-03 | CLI parser unit |
| No displayable drift has no section in any mode | DRIFT-08, DRIFT-10 | Model and renderer unit |
| Modes, grouping/details, and filtering have automated coverage | CLI-01–03; DRIFT-01–10 | TUnit suite |

## Test Cases

### CLI-01: CliParser_DriftOptionOmittedOrValidValue_SetsExpectedDisplayMode

Parse a plan path with no `--drift`, then `all`, `relevant`, and `none`, including
mixed case. Assert omission and `all` select `All`; each explicit option selects its
matching enum value; and the input path is preserved.

### CLI-02: CliParser_DriftOptionInvalidValue_ThrowsErrorListingAcceptedValues

Parse `--drift unexpected plan.json`. Assert the parser error declares the value
invalid and names `all`, `relevant`, and `none`.

### CLI-03: CliParser_DriftOptionWithoutValue_ThrowsErrorListingAcceptedValues

Parse a command ending in `--drift`. Assert a clear error names all three valid
values; the parser must not treat the missing value as a path or silently default.

### DRIFT-01: ReportModelBuilder_MatchingNormalizedCandidates_CreatesOneGroup

Build two displayable drift resources of the same Terraform type and changed path
with the same normalized before and after values. Assert one group, count two, and
exactly the two source addresses.

### DRIFT-02: ReportRenderer_GroupWithMultipleAddresses_RendersCollapsedDetailsSummary

Render one group with two addresses. Assert exactly one `<details>` and `<summary>`;
the summary includes the drift icon, `2`, type, code-formatted path, and code-formatted
before and after values; and the body contains both addresses as code-formatted
bullets. Assert the details element is not pre-opened.

### DRIFT-03: ReportModelBuilder_CandidatesDifferingInAnyKeyPart_CreatesSeparateGroups

Use candidates differing one dimension at a time: type, path, normalized before, and
normalized after. Assert each variation makes a distinct group. Include raw values
that normalize to the same display strings to prove grouping uses normalized values.

### DRIFT-04: ReportModelBuilder_DuplicateAndUnorderedAddresses_DeduplicatesAndOrdersOrdinally

Provide repeated candidates for one address and non-ordinal address/group input.
Assert an address appears once, addresses use ordinal order, and groups are ordered by
type, path, before, then after.

### DRIFT-05: ReportModelBuilder_AllMode_SelectsEveryDisplayableDriftEntry

Build mixed displayable drift with `All` and default options. Assert equivalent groups
that include every displayable candidate, whether or not it has a planned change.

### DRIFT-06: ReportModelBuilder_RelevantMode_FiltersBeforeGroupingUsingDisplayChanges

Build drift candidates that otherwise share a group, but give only one address a
displayable planned change. Assert `Relevant` retains only that address and count one,
proving excluded addresses cannot leak into a group or address list.

### DRIFT-07: ReportModelBuilder_RelevantMode_NoOpOrSuppressedPlannedChangeDoesNotMakeDriftRelevant

Pair drift addresses with respectively a displayable planned change, a Terraform
no-op planned change, and a fully attribute-suppressed planned change. Assert
`Relevant` retains only the first. Also assert no-op and fully suppressed drift are
absent from both `All` and `Relevant` output.

### DRIFT-08: ReportRenderer_NoneModeOrNoGroups_OmitsDriftHeadingAndContent

Build displayable drift using `None`, then render it. Assert groups are empty and the
markdown contains neither `## 🌀 Drift Detected` nor drift details. Repeat for an
explicitly empty group list to verify the renderer's empty behavior.

### DRIFT-09: ReportModelBuilder_DriftAttributeFilteringSuppressesEveryAttribute_OmitsResource

Use a provider attribute filter that suppresses every attribute of a drift resource.
Assert it produces no candidate or group in `All` or `Relevant` modes.

### DRIFT-10: ReportModelBuilder_NoDisplayableDrift_AllModesProduceNoGroups

For `All`, `Relevant`, and `None`, use plans where drift is absent, no-op, or fully
suppressed. Assert empty groups and no rendered drift heading in every case.

## Edge Cases and Error Conditions

- A resource with multiple changed paths creates a candidate per path; only matching
  paths can group.
- Values, paths, resource types, and addresses containing HTML delimiters, backticks,
  or line breaks are escaped using existing markdown helpers; test they cannot break
  the summary or details list.
- Sensitive values remain masked before grouping; raw secrets never render, and equal
  masked display values may group.
- Relevant address comparison is ordinal: case-distinct Terraform addresses must not
  correlate.
- A single-member group uses the same details structure with count `1`.
- Missing and invalid CLI values are parser failures, never fallback behavior.
