# UAT Test Plan: Terraform Import and Moved Blocks

## Goal
Verify that Terraform `import` and `moved` block metadata renders correctly and consistently in GitHub and Azure DevOps PR comments, including the new Refactoring Summary table and inline resource annotations.

## Artifacts
**Artifact to use:** `artifacts/refactoring-demo.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `examples/refactoring-comprehensive.json` (to be created by Developer)
- **Command:** `tfplan2md examples/refactoring-comprehensive.json --output artifacts/refactoring-demo.md`
- **Rationale:** This artifact covers all scenarios: active imports, active moves, and already-applied (unnecessary) blocks for both types.

## Test Steps
1. Run UAT using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

### 1. Refactoring Summary Table
**Location:** Near the end of the report, before the footer.

**Verify:**
- **Table Presence:** Section `## Refactoring Summary` appears only if imports/moves exist.
- **Sorting:** Imports must appear before Moves. Within each group, resources should be sorted alphabetically.
 - **Status Icons:** 
   - ✅ **Ready** status for active changes, including pending imports that Terraform has not yet applied.
   - ⚠️ **Already moved** status only for no-op moves that Terraform clearly indicates were already applied.
- **Formatting:** Resource addresses, import IDs, and previous addresses must be code-formatted (using backticks).

### 2. Resource-Level Annotations
**Location:** Inside each resource's `<summary>` tag.

**Specific Resources to check:**
- **Imported Resource:** Should show `📥 Imported | 🆔 <id>` (e.g., `azurerm_resource_group.existing`).
- **Moved Resource:** Should show `🔀 Moved from <old-address>` (e.g., `azurerm_virtual_network.hub`).
- **Already-applied status:** Should keep pending no-op imports as `📥 Imported` without a warning, while no-op moves may show `🔀 Moved from … (⚠️ already moved)` when Terraform clearly indicates the move was already applied.

  Note: Wording should be consistent with the refactoring summary table: pending imports remain `Ready`, while only already-applied moves use the warning wording.

**Expected Outcome:**
- Annotations are placed after the resource type/name and before other context (like location or ID).
- Icons (`📥`, `🔀`) and labels use non-breaking spaces and do not wrap.
- Inside `<summary>`, data is wrapped in `<code>` and labels use markdown emphasis (`*...*`).

**Before/After Context:**
- **Before:** Refactoring operations were invisible in the report; reviewers had to check HCL or the raw plan output to know if a resource was being imported or moved.
- **After:** Reviewers see the intent (Import/Move) explicitly in the summary line and a consolidated table for audit.
