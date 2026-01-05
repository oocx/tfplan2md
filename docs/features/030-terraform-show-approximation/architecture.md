# Architecture: Terraform Show Output Approximation Tool

## Status

Proposed

## Context

Feature spec: [specification.md](specification.md)

The website needs “before tfplan2md” examples that look like real terminal output from `terraform show` for a plan, including ANSI colors. The repository primarily stores Terraform plan JSON (from `terraform show -json`), not the binary `.tfplan` that `terraform show` typically consumes.

This feature adds a **standalone development tool** that:
- Reads Terraform plan JSON (`format_version` 1.2+)
- Produces text output approximating `terraform show`’s human output format
- Optionally includes Terraform-like ANSI color codes and styles
- Writes to stdout or a file

## Fidelity Baseline (Concrete Examples)

The primary fidelity target for this feature is the real `terraform show` output captured in:

- [examples/azuredevops/terraform_plan.txt](../../../examples/azuredevops/terraform_plan.txt)
- [examples/azuredevops/terraform_plan2.txt](../../../examples/azuredevops/terraform_plan2.txt) (includes replacement `-/+`)

The input plan JSON for those outputs is:

- [examples/azuredevops/terraform_plan.json](../../../examples/azuredevops/terraform_plan.json)
- [examples/azuredevops/terraform_plan2.json](../../../examples/azuredevops/terraform_plan2.json)

The renderer should aim to match that output’s:
- section ordering
- indentation
- per-line prefixes (`+`, `~`, `-`, `<=`) and where prefixes appear
- ANSI colors and text styles
- “unchanged attributes/elements hidden” comment lines
- “Changes to Outputs” section

Non-goals (per spec):
- No Terraform binary invocation
- No `.tfplan` support
- No Docker distribution / CI integration
- No interactive UI

## Existing Architecture & Patterns to Reuse

- The repo already parses Terraform plan JSON in `Oocx.TfPlan2Md.Parsing` using `TerraformPlanParser` and immutable record types.
- Existing `tools/*` projects follow a consistent pattern:
  - `Program` delegates to an `*App` class
  - `CLI/` contains a simple, explicit `CliParser` and `CliOptions`
  - File IO and validation live in the `*App`
  - Errors are printed as `Error: ...` to stderr

This tool should follow the same conventions for consistency and low dependency footprint.

## Key Architectural Decisions

### Decision 1: Implement as a separate tool project under `tools/`

Create a new console project at:
- `tools/Oocx.TfPlan2Md.TerraformShowRenderer/`

Rationale:
- Matches the spec (“standalone development tool”).
- Matches established patterns from `tools/Oocx.TfPlan2Md.HtmlRenderer/` and `tools/Oocx.TfPlan2Md.ScreenshotGenerator/`.
- Keeps the main `tfplan2md` CLI focused on Markdown reporting (see [docs/spec.md](../../spec.md)).

### Decision 2: Reuse the existing Terraform plan JSON model (`TerraformPlanParser`)

Use `Oocx.TfPlan2Md.Parsing.TerraformPlanParser` and `TerraformPlan` / `ResourceChange`.

Rationale:
- Minimizes duplication of Terraform JSON parsing.
- Ensures consistent handling of `resource_changes`, including `after_unknown` and `*_sensitive` fields.

Implementation note:
- `Change.Before`/`After`/`AfterUnknown` currently deserialize into `object?` (typically `JsonElement`). The renderer should treat them as `JsonElement` and never convert to `Dictionary<string,object>` to preserve property ordering.

Additional note:
- The baseline includes `resource_changes[*].action_reason` and top-level `output_changes`, which are not currently represented by `TerraformPlan` / `ResourceChange`.
- The tool should parse these fields inside the renderer tool (e.g., with `JsonDocument`) without requiring changes to the main `Oocx.TfPlan2Md.Parsing` model.

### Decision 3: Use a small internal ANSI abstraction (no external console libraries)

Output must include ANSI color codes, and also be able to strip them for `--no-color`.

Rationale:
- Keeps the tool dependency-light.
- Easier to write deterministic tests (rendering is just strings).

Proposed approach:
- Internal `AnsiStyle` enum (e.g., `Add`, `Change`, `Remove`, `Dim`, `Reset`).
- A writer that can either:
  - emit raw escape sequences (default)
  - emit plain text (when `--no-color` is set)

## Decision: Header Contents

The tool will **not** print an explicit “Terraform version: …” header line.

Rationale:
- Terraform’s human plan output typically begins with `Terraform will perform the following actions:`.
- Adding a custom version header would reduce “looks like real Terraform” fidelity.

The renderer may still use the JSON `terraform_version` internally (for future compatibility toggles), but it will not be emitted as a dedicated header line.

## Decision: ANSI / Styling Contract

The fidelity baseline includes ANSI styling beyond green/yellow/red. The tool should emit (unless `--no-color` is used):

- **Bold**: `\u001b[1m` … `\u001b[0m`
- **Green (create / added attribute marker)**: `\u001b[32m`
- **Yellow (update marker and update arrow)**: `\u001b[33m`
- **Red (destroy marker / destroyed emphasis)**: `\u001b[31m`
- **Cyan (read marker `<=`)**: `\u001b[36m`
- **Dim / gray** (hidden attributes/elements and `-> null` suffix): `\u001b[90m`
- **Replacement marker `-/+`**: a red `-`, then `/`, then green `+`

Notes:
- The output uses frequent resets; the renderer may normalize resets as long as the visible terminal rendering is equivalent.
- `--no-color` removes all `\u001b[` escape sequences and outputs the same text.

## Options Considered

### Option 1: Render from `TerraformPlanParser` + `resource_changes` ✅ Recommended

- Parse the plan JSON using existing types.
- Render output primarily from `resource_changes[*].change`.

Pros:
- Reuses existing parsing.
- Closest fit to the spec’s scope.
- Straightforward to support all action types.

Cons:
- Fidelity depends on implementing a reasonable diff renderer (Terraform’s exact formatting is nuanced).

### Option 2: Parse JSON with `JsonDocument` directly to mirror Terraform’s ordering everywhere

- Avoid typed model and traverse JSON directly.

Pros:
- Maximum control over ordering and formatting.

Cons:
- Duplicates parsing logic already present in the repo.
- Higher implementation and maintenance cost.

### Option 3: Invoke Terraform and/or synthesize `.tfplan`

Pros:
- Potentially exact output.

Cons:
- Explicitly out of scope (no Terraform dependency, no `.tfplan`).

## Recommended Approach

Choose **Option 1**.

Within Option 1, maximize authenticity by:
- Preserving property order from the JSON (`JsonElement.EnumerateObject()` preserves input order).
- Rendering the same top-level section flow as the fidelity baseline:
  - provider/action legend
  - actions header
  - resource blocks (excluding `no-op`)
  - plan summary line
  - output changes (when present)
- Implementing a stable, structural diff strategy that renders values using Terraform-like conventions.

## High-Level Design

### CLI surface

Match established tool conventions (see HtmlRenderer/ScreenshotGenerator):

- Required:
  - `--input` / `-i` (path to plan JSON)
- Optional:
  - `--output` / `-o` (defaults to stdout)
  - `--no-color` (emit plain text)
  - `--help` / `-h`
  - `--version` / `-v`

Exit codes (per spec):
- `0` success
- `1` invalid arguments / usage
- `2` file I/O error
- `3` JSON parsing error
- `4` unsupported format version

### Components

Suggested (conceptual) structure:

- `Program` → `TerraformShowRendererApp`
- `CLI/`
  - `CliParser`, `CliOptions`, `CliParseException`, `HelpTextProvider`
- `Rendering/`
  - `TerraformShowRenderer` (main renderer)
  - `AnsiTextWriter` (ANSI/no-color abstraction)
  - `ValueRenderer` (Terraform-ish value formatting)
  - `DiffRenderer` (before/after, after_unknown, sensitive)

### Rendering pipeline

1. Parse arguments
2. Handle `--help` and `--version`
3. Validate input file existence
4. Read JSON input
5. Parse via `TerraformPlanParser`
6. Validate `format_version >= 1.2`
7. Render:
   - Legend section (see below)
   - Blank line
   - `Terraform will perform the following actions:`
   - Blank line
   - For each `resource_changes` entry in input order:
     - Skip `no-op`.
     - Render the bold resource header comment line(s).
     - Render the Terraform-like block line and its attributes.
     - Add a blank line between resources.
   - Render `Plan:` summary line with bold `Plan:` label.
   - When `output_changes` exists and contains any non-`no-op` outputs:
     - Blank line
     - Render `Changes to Outputs:` section.
8. Write to stdout or to `--output`

### Legend section

To match real `terraform show`, output this section before the actions listing:

- `Terraform used the selected providers to generate the following execution`
- `plan. Resource actions are indicated with the following symbols:`
- Four indented legend lines:
  - green `+` create
  - yellow `~` update in-place
  - red `-` destroy
  - red `-` / green `+` destroy and then create replacement
  - cyan `<=` read (data resources)

This section is static text (not derived from the plan), but it is important for “authentic terminal output” screenshots.

### Plan action mapping

Map `change.actions` to Terraform-style action kinds:
- `["create"]` → **create** (`+`)
- `["update"]` → **update** (`~`)
- `["delete"]` → **delete** (`-`)
- `["delete","create"]` → **replace** (`-/+`)
- `["create","delete"]` → **replace** (be tolerant)
- `["read"]` → **read** (display like Terraform data source read)
- `["no-op"]` → **no-op** (supported, but typically not emitted in Terraform’s change listing)

### Resource header comment lines

Before each resource block, Terraform prints one or two comment lines:

- Line 1 (bold): `  # <address> will be <action phrase>`
- Line 2 (optional): explanatory reason in parentheses

Action phrases to match the fidelity baseline:
- create: `will be created`
- update: `will be updated in-place`
- delete: `will be destroyed` (the word `destroyed` is emphasized in red + bold)
- read: `will be read during apply`
- replace: `must be replaced` (the word `replaced` is emphasized in red + bold)

Reason line mapping (from `resource_changes[*].action_reason`) observed in the baseline JSON:
- `read_because_dependency_pending` → `  # (depends on a resource or a module with changes pending)`
- `delete_because_no_resource_config` → `  # (because <address> is not in configuration)`

If the action reason is unknown or missing, omit the second line.

### Block line format and indentation

The fidelity baseline places the action marker on the block line itself (not on the `# ...` comment header line).

Block line shape:
- Managed resource: `  <marker> resource "<type>" "<name>" {`
- Data resource: `  <marker> data "<type>" "<name>" {`

Where `<marker>` is one of:
- green `+` (create)
- yellow `~` (update)
- red `-` (delete)
- cyan `<=` (read)
- red `-` / green `+` (replace)

Indentation conventions (approximate, based on the baseline):
- Block line begins with two spaces.
- Attribute lines are indented further and include their own per-line marker when applicable.
- Nested blocks are separated by blank lines and use additional indentation.

### Diff strategy

The fidelity baseline uses Terraform’s “inline arrow” formatting and hides most unchanged values.

- **Create**:
  - Render the block with a green `+` marker at the block line.
  - Each attribute line is prefixed with a green `+`.
  - For unknown values, render `(known after apply)`.

- **Read**:
  - Render the block line with a cyan `<=` marker.
  - Attribute lines appear with green `+` markers, similar to create.

- **Delete**:
  - Render the block with a red `-` marker at the block line.
  - Each attribute line is prefixed with red `-`.
  - For scalar values, append a dim `-> null` suffix.
  - For nested blocks, print block headers with `-`, and keep nested formatting.

- **Update**:
  - Render the block line with a yellow `~` marker.
  - For changed attributes and changed nested elements, prefix with yellow `~`.
  - For changed scalar values, render `value_before -> value_after` where the arrow is yellow.
  - Unchanged attributes are frequently omitted, replaced with dim comment lines:
    - `# (<n> unchanged attributes hidden)`
    - `# (<n> unchanged elements hidden)`
    - `# (<n> unchanged blocks hidden)`

- **Sensitive blocks**:
  - When a nested block has sensitive content, Terraform prints a fixed 2-line comment:
    - `# At least one attribute in this block is (or was) sensitive,`
    - `# so its contents will not be displayed.`
  - The renderer should produce the same comment instead of showing values for that block.

- **Replace**:
  - Render the block line with the replacement marker `-/+`.
  - Inside the block, Terraform may mix markers:
    - `~` for attributes that conceptually change
    - `-` and `+` for removed/added nested blocks and list elements
  - When an attribute change forces replacement, Terraform appends a red comment at end-of-line:
    - `# forces replacement`
  - The renderer should derive this from `change.replace_paths`:
    - When a rendered attribute path is in `replace_paths`, append `# forces replacement` in red.

Represent unknown and sensitive markers path-wise:
- Treat `after_unknown` / `after_sensitive` as a tree mirroring the shape of `after`.
- Unknown leaf `true` indicates “known after apply”.

### Output changes

The baseline includes an additional section at the end:

- `Changes to Outputs:`
  - One line per changed output.

This is represented by `output_changes` in the plan JSON.

Rendering rules (baseline-aligned):
- Only include outputs where `actions` is not `no-op`.
- For an updated output, use a yellow `~` marker and print `before -> after` where unknown after is rendered as `(known after apply)`.

Note: The main `TerraformPlan` model does not currently include `output_changes`. The show renderer tool should parse it separately.

### Counting summary totals

Compute plan summary counts from `resource_changes`:
- add: `create` + `replace`
- change: `update`
- destroy: `delete` + `replace`
- exclude `read` and `no-op` from the `Plan: ...` counts (matching Terraform plan output)

## Testing Guidance

Keep tests deterministic and string-based:
- CLI parsing (required/optional flags, errors)
- Summary counting for each action kind
- Rendering of a small synthetic plan:
  - create with `after_unknown`
  - update with a nested change
  - replace
  - sensitive value placeholder
- ANSI on/off snapshots:
  - `--no-color` should not emit `\u001b[` sequences

## Documentation Guidance

Per spec, document usage for contributors and website maintainers:
- website documentation (`website/code-examples.md` per spec)
- optionally a short note in `README.md` describing the dev-tool purpose and how to run it
