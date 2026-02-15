# Terraform `show` / human plan rendering — strict byte/ANSI specification (Terraform CLI)

This document specifies the **exact byte-level** output format (including **ANSI escape sequences**) of Terraform CLI’s **human-readable plan renderer** (as used by `terraform plan` output, and `terraform show` for plans when rendered in “human” mode), based on HashiCorp Terraform’s implementation under `internal/command/jsonformat` (notably `internal/command/jsonformat/plan.go` and `internal/command/jsonformat/renderer.go`).

**Scope**
- Focus: **human-readable plan output** rendered to console via Terraform’s plan renderer.
- Includes: whitespace, indentation, blank lines, ordering rules, and ANSI SGR sequences.
- Excludes: JSON output mode (`terraform show -json`).
- Notes: Some subcomponents are terminal-width dependent (`WordWrap`, `HorizontalRule`). A strict reimplementation must reproduce Terraform’s behavior for a given terminal width and the same `colorstring` palette settings.

**Compatibility goal**
A reimplementation in another language MUST produce output that is:
- **Byte-for-byte identical** to Terraform’s output for the same input plan data, schemas, options, and terminal width.
- Including exactly the same **ANSI SGR** sequences (in color mode) and exactly the same **line breaks** and spacing.

---

## 0. Definitions

### 0.1 Newlines
Terraform uses UNIX newlines only.

- `LF` means the single byte `0x0A` (`\n`).

### 0.2 ANSI escapes (colored mode)

Terraform expands color markup to ANSI escapes (via `github.com/mitchellh/colorstring`), producing output containing **SGR** control sequences.

Definitions (byte-level):

- `ESC` := `0x1B`
- `CSI` := `ESC` `[` (bytes: `0x1B 0x5B`)
- `SGR(params...)` := `CSI` `<params>` `m` where `<params>` is one or more decimal integers separated by `;`.

Regex definitions (bytes interpreted as UTF-8 text):
- `SGR` := `\x1b\[[0-9]+(?:;[0-9]+)*m`

Terraform output may contain **multiple SGR sequences** adjacent to each other.

### 0.3 ANSI “reset”
Terraform uses reset sequences as emitted by `colorstring`. In typical SGR, reset is `\x1b[0m`, but the exact emissions must match `colorstring` output for the given palette; do not assume hard-coded values unless you replicate the library.

### 0.4 No-color mode
In “no color” mode, the output MUST contain **no ANSI sequences**.

This spec is for strict colored mode. For no-color mode, interpret all ANSI terminals as empty.

---

## 1. Rendering inputs and configuration

### 1.1 Renderer configuration

The renderer is parameterized by:
- `RunningInAutomation` (boolean)
- `mode` in `{ NormalMode, DestroyMode, RefreshOnlyMode, ... }`
- `opts` containing zero or more of:
  - `Errored` (plan generation encountered an error)
  - `NoChanges` (quality flag affecting messaging with drift)
- Terminal width in columns (integer `C >= 1`)
- Color enablement (boolean). This spec assumes **color enabled**.

### 1.2 Plan data
Rendering uses:
- Planned resource changes
- Drift changes (refresh results)
- Deferred changes (partial plan)
- Output changes
- Action invocations (CLI-invoked and lifecycle-triggered)

A correct reimplementation must match Terraform’s logic for:
- which changes are displayed vs suppressed
- ordering and grouping
- summary counts

---

## 2. Grammar overview (byte-level EBNF with ANSI terminals)

This uses EBNF where terminals are literal bytes/strings and regex terminals are annotated.

### 2.1 Lexical terminals

- `NL` := `"\n"`
- `SP` := `" "`
- `SP2` := `"  "`
- `SP4` := `"    "`
- `SP8` := `"        "`

ANSI terminals:
- `ANSI` := `SGR` (regex `\x1b\[[0-9]+(?:;[0-9]+)*m`)
- `ANSI*` := zero or more `ANSI`.

Integer:
- `INT` := regex `[0-9]+`

Terraform Address:
- `ADDR` := regex `[^\n]+` (address strings are opaque to renderer; must be printed as-is)

Type/Name:
- `TYPE` := regex `[^\n\"]+`
- `NAME` := regex `[^\n\"]+`

---

## 3. Document structure

Top-level output:

```
PlanOutput :=
    (VersionWarningBlock)?,
    (DriftSection, DriftSeparator)?,
    (NoChangesBlock | ChangesBlock),
    (ActionsSection)?,
    (OutputsSection)?;
```

**Constraint A (drift separator):** If `DriftSection` is printed and rendering continues beyond drift, then `DriftSeparator` MUST be printed.

**Constraint B (no-changes early return):** In `RefreshOnlyMode`, if drift exists and Terraform deems there are no actionable changes, Terraform may return immediately after drift footer without printing separators or any other blocks.

---

## 4. Version warning block (optional)

Terraform may print a version warning if local JSON format versions are considered “incompatible” with the plan’s recorded versions.

**Shape:**

```
VersionWarningBlock :=
    WordWrapBlock(WarningText), NL;
```

Where `WordWrapBlock(...)` is the exact output of Terraform’s `format.WordWrap` at width `C`.

`WarningText` is a colorized string beginning with `"\n"` and containing “Warning: …”.

**Byte-level requirement:** word wrapping MUST match Terraform exactly for the same width `C`.

---

## 5. Drift section (“Objects have changed outside of Terraform”)

### 5.1 Drift section header

```
DriftSection :=
    NL,
    ANSI*, "Note:", ANSI*, SP, "Objects have changed outside of Terraform", NL,
    NL,
    WordWrapBlock(DriftIntroText),
    (NL, ResourceDiffBlock)*,
    WordWrapBlock(DriftFooterText), NL?;
```

`DriftIntroText` (exact text passed to WordWrap, including trailing `\n`):
- `Terraform detected the following changes made outside of Terraform since the last \"terraform apply\" which may have affected this plan:\n`

`DriftFooterText` depends on mode:
- Refresh-only footer:
  - `\n\nThis is a refresh-only plan, so Terraform will not take any actions to undo these. If you were expecting these changes then you can apply this plan to record the updated values in the Terraform state without changing any remote objects.`
- Other modes footer:
  - `\n\nUnless you have made equivalent changes to your configuration, or ignored the relevant attributes using ignore_changes, the following plan may include actions to undo or respond to these changes.`

### 5.2 Drift separator

```
DriftSeparator :=
    HorizontalRule(C), NL;
```

Where `HorizontalRule(C)` is exactly Terraform’s `format.HorizontalRule(colorize, C)`.

---

## 6. Deferred section (partial plan note)

If deferred changes exist:

```
DeferredSection :=
    NL,
    ANSI*, "Note:", ANSI*, SP,
    "This is a partial plan, parts can only be known in the next plan / apply cycle.",
    NL,
    NL,
    (DeferredDiffBlock, NL)*;
```

Deferred blocks are printed similarly to `ResourceDiffBlock` but have a two-line comment header explaining defer reason.

If `DeferredSection` prints and rendering continues, Terraform prints:

```
DeferredSeparator := HorizontalRule(C), NL;
```

---

## 7. No-changes blocks

A “no changes” plan is when:
- there are no renderable resource changes
- no outputs to display
- no action invocations to summarize

Terraform chooses among these based on flags and mode.

### 7.1 Planning failed (Errored)

```
PlanningFailedBlock :=
    (HorizontalRule(C), NL, NL)?,
    ANSI*, NL,
    "Planning failed.", SP, "Terraform encountered an error while generating this plan.",
    ANSI*, NL, NL;
```

### 7.2 No current changes but deferred exists

```
NoCurrentChangesDeferredBlock :=
    (HorizontalRule(C), NL, NL)?,
    ANSI*, NL,
    "No current changes.", SP, "This plan requires another plan to be applied first.",
    ANSI*, NL, NL;
```

### 7.3 Refresh-only mode: no changes

```
RefreshOnlyNoChangesBlock :=
    ANSI*, NL,
    "No changes.", SP, "Your infrastructure still matches the configuration.",
    ANSI*, NL, NL,
    WordWrapBlock(RefreshOnlyNoChangesText), NL?;
```

Text passed to WordWrap:
- `Terraform has checked that the real remote objects still match the result of your most recent changes, and found no differences.`

### 7.4 Destroy mode: no changes

```
DestroyModeNoChangesBlock :=
    (HorizontalRule(C), NL, NL)?,
    ANSI*, NL,
    "No changes.", SP, "No objects need to be destroyed.",
    ANSI*, NL, NL,
    WordWrapBlock(DestroyNoChangesText), NL?;
```

Text passed to WordWrap:
- `Either you have not created any objects yet or the existing objects were already deleted outside of Terraform.`

### 7.5 Normal mode: no changes

Always prints:

```
NormalModeNoChangesPreamble :=
    (HorizontalRule(C), NL, NL)?,
    ANSI*, NL,
    "No changes.", SP, "Your infrastructure matches the configuration.",
    ANSI*, NL, NL;
```

Then either (if drift was printed) a drift-specific message and **return**, or otherwise a generic no-drift message.

**Normal mode with drift printed and not `NoChanges` flag:**
- WordWrap text:
  - `Your configuration already matches the changes detected above, so applying this plan will only update the state to include the changes detected above and won't change any real infrastructure.`

**Normal mode with drift printed and `NoChanges` flag:**
- WordWrap text:
  - `Your configuration already matches the changes detected above. If you'd like to update the Terraform state to match, create and apply a refresh-only plan` + `Suggestion`
- `Suggestion` is:
  - `"."` if `RunningInAutomation = true`
  - or `":\n  terraform apply -refresh-only"` if `RunningInAutomation = false`

**Normal mode without drift:**
- WordWrap text:
  - `Terraform has compared your real infrastructure against your configuration and found no differences, so no changes are needed.`

---

## 8. Changes block (non-empty plan)

When there are changes and/or action invocations, Terraform prints (in order):

1) Optional deferred section + separator
2) Optional legend (execution plan intro + symbols)
3) Planned actions section (header + per-resource diffs + summary)

### 8.1 Execution plan intro + legend

Printed iff at least one non-NoOp action exists among counted resource changes.

```
LegendBlock :=
    WordWrapBlock("\nTerraform used the selected providers to generate the following execution plan. Resource actions are indicated with the following symbols:"),
    NL,
    (LegendLine)+;
```

LegendLine values appear in this exact order if present (each ends with `NL`):

- Create:  `SP2, ANSI*, "+", ANSI*, SP, "create", NL`
- Update:  `SP2, ANSI*, "~", ANSI*, SP, "update in-place", NL`
- Delete:  `SP2, ANSI*, "-", ANSI*, SP, "destroy", NL`
- DeleteThenCreate: `ANSI*, "-", ANSI*, "/", ANSI*, "+", ANSI*, SP, "destroy and then create replacement", NL`
- CreateThenDelete: `ANSI*, "+", ANSI*, "/", ANSI*, "-", ANSI*, SP, "create replacement and then destroy", NL`
- Read:    `SP, ANSI*, "<=", ANSI*, SP, "read (data resources)", NL`

(Exact spacing matches Terraform’s `actionDescription` strings.)

### 8.2 Planned actions section

```
PlannedActionsSection :=
    NL,
    PlannedActionsHeader,
    (NL, ResourceDiffBlock, NL)*,
    PlanSummaryLine;
```

Header is exactly one:

- Non-errored:
  - `Terraform will perform the following actions:\n`
- Errored:
  - `Terraform planned the following actions, but then encountered a problem:\n`

Summary:

```
PlanSummaryLine :=
    NL,
    ANSI*, "Plan:", ANSI*, SP,
    (INT, SP, "to import,", SP)?,
    INT, SP, "to add,", SP,
    INT, SP, "to change,", SP,
    INT, SP, "to destroy.",
    (SP, "Actions:", SP, INT, SP, "to invoke.")?,
    NL;
```

---

## 9. Resource diff blocks (core per-resource output)

### 9.1 Structure

A resource diff block is a concatenation:

```
ResourceDiffBlock :=
    ResourceCommentHeader,
    ActionSymbol, SP, ResourceHeader, SP, ComputedDiffText,
    (BeforeActionsSubsection)?,
    (AfterActionsSubsection)?;
```

Where:
- `ActionSymbol` is Terraform’s colored diff symbol (from `format.DiffActionSymbol`) including any ANSI.
- `ResourceHeader` is one:
  - `resource "TYPE" "NAME"`
  - `data "TYPE" "NAME"`
- `ComputedDiffText` is the output of Terraform’s computed diff engine, potentially multiline.

### 9.2 Resource comment header (exact-lines contract)

The comment header begins with a bold `#` line and ends with `NL`. It may include additional lines for moved/import notes and delete reasons.

**This header is not purely syntactic; it is a deterministic function** of:
- action type (`create`, `update`, `delete`, `replace`, `read`, `noop`)
- change cause (`drift` vs `change`)
- extra plan metadata (`PreviousAddress`, importing ID, action reasons, deposed objects, etc.)

At minimum:
- The header ALWAYS ends with at least one `NL`.
- Additional metadata lines (moved/imported/warnings) each end with `NL`.

A compliant reimplementation MUST reproduce:
- exact leading spaces before `#` on every line
- exact placement of `[reset]`, `[bold]`, color sequences around key terms like “destroyed”, “replaced”
- exact ordering of metadata lines
- exact quoting in imported-from line: `"(imported from \"ID\")"` uses a literal `"` in output (escaped by `\\\"` in source, but printed as `"`).

### 9.3 Embedded action invocation subsections (before/after)

If present:

```
BeforeActionsSubsection :=
    NL, NL,
    SP4, ANSI*, "# Actions to be invoked before this change in order:", ANSI*, NL,
    (ActionInvocationBlock)+;

AfterActionsSubsection :=
    NL, NL,
    SP4, ANSI*, "# Actions to be invoked after this change in order:", ANSI*, NL,
    (ActionInvocationBlock)+;
```

ActionInvocationBlock:

```
ActionInvocationBlock :=
    SP4, "action", SP, GoQuotedString, SP, GoQuotedString, SP, "{", NL,
    (SP8, "config", SP, ActionConfigDiff, NL)?,
    SP4, "}", NL;
```

- `GoQuotedString` MUST match Go `%q` escaping rules exactly.
- `ActionConfigDiff` is a computed diff text where **all lines after the first** are indented by exactly 8 spaces (indent-except-first-line rule).

---

## 10. Actions section (CLI-invoked actions)

If non-empty:

```
ActionsSection :=
    ANSI*, NL,
    "Terraform will invoke the following action(s):", NL, NL,
    (ActionSummaryBlock, (NL, ActionSummaryBlock)*)?,
    NL;
```

Each summary block:

```
ActionSummaryBlock :=
    SP2, ANSI*, "# ", ADDR, ANSI*, SP, "will be invoked", NL,
    ActionInvocationBlock;
```

---

## 11. Outputs section

If any output changes are rendered:

```
OutputsSection :=
    NL,
    "Changes to Outputs:", NL,
    OutputsBody, NL,
    (OutputsOnlyNote)?;
```

OutputsBody is one or more lines joined by `NL`:

```
OutputsBody :=
    OutputLine, (NL, OutputLine)*;
```

OutputLine:

```
OutputLine :=
    ActionSymbol, SP, EscapedNameAligned, SP, "=", SP, ComputedDiffText;
```

Where:
- `EscapedNameAligned` is the escaped output name padded with spaces to the max escaped length among rendered output keys.

If there were output changes but **no resource changes** (counts empty), Terraform prints a word-wrapped note:

```
OutputsOnlyNote :=
    WordWrapBlock("\nYou can apply this plan to save these new output values to the Terraform state, without changing any real infrastructure."),
    NL?;
```

---

## 12. Computed diff engine (placeholder contract)

`ComputedDiffText` is produced by Terraform’s computed diff renderers under:
- `internal/command/jsonformat/computed`
- `internal/command/jsonformat/computed/renderers`

A strict reimplementation MUST:
- replicate each renderer’s byte output exactly (including commas, brace placement, alignment, and ANSI color sequences)
- replicate helper functions and policies:
  - indentation (`formatIndent`)
  - diff symbol columns (`writeDiffActionSymbol`)
  - null suffix formatting (`nullSuffix`)
  - forces replacement suffix formatting (`forcesReplacement`)
  - “unchanged elements hidden” lines (`unchanged(...)`)
  - key escaping rules (`EnsureValidAttributeName`, HCL string escaping)
  - multiline string handling (trim rules, JSON pretty-diff logic, etc.)
  - sensitive and unknown renderers

Because the computed diff grammar is large, this document defines it as:

```
ComputedDiffText := ComputedDiffBytes(plan, schemas, opts, indent=0);
```

Where `ComputedDiffBytes(...)` MUST be byte-identical to Terraform’s `computed.Diff.RenderHuman(0, opts)`.

---

## 13. Terminal-width dependent functions

### 13.1 WordWrapBlock(text)
`WordWrapBlock(text)` MUST match Terraform’s `format.WordWrap(text, C)` exactly:
- same breaking positions
- same handling of leading `\n` and embedded newlines
- same indentation continuation rules (if any)
- same behavior for long tokens

### 13.2 HorizontalRule(C)
`HorizontalRule(C)` MUST match Terraform’s `format.HorizontalRule(colorize, C)` exactly, including:
- character choice
- length
- color resets (if any)

---

## 14. Conformance test requirements (strict)

A conforming implementation MUST pass golden tests where:

1) Terraform CLI is run with a controlled terminal width `C` and color enabled.
2) The same plan JSON + provider schema data are given to the reimplementation.
3) Output bytes are compared **exactly** (byte-for-byte), including all ANSI SGR sequences and newlines.

Recommended test matrix:
- widths: 78, 80, 120
- modes: normal, destroy, refresh-only
- flags: errored, nochanges, running-in-automation
- cases: drift-only, deferred-only, outputs-only, import, moved, replace, sensitive, unknown after apply

---

## Appendix A: ANSI token matching (strict)

To validate ANSI presence without hardcoding palette:
- Accept any `SGR` sequences in the exact positions where Terraform emits colored segments.
- However, to be truly byte-identical you MUST match Terraform’s concrete palette and emission behavior (including any redundant resets).

---

## Appendix B: Implementation references (Terraform source)

- `internal/command/jsonformat/renderer.go` (`RenderHumanPlan`)
- `internal/command/jsonformat/plan.go` (`Plan.renderHuman`, drift/deferred, summary)
- `internal/command/jsonformat/computed/diff.go`
- `internal/command/jsonformat/computed/renderers/*`
- `internal/command/format/*` (`WordWrap`, `HorizontalRule`, `DiffActionSymbol`, etc.)
