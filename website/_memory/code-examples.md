# Website Code Examples Inventory

This document lists the code examples shown on the website and how to (re)generate them.

## Rules

- Every code example/snippet referenced by the website must have an entry here.
- Prefer sourcing examples from existing documentation (README.md, docs/) unless explicitly instructed otherwise.
- Code examples should be copy/paste ready.

## Current State

**Status:** All pages now display real tfplan2md output from comprehensive demo artifacts.

### Pages Updated with Real Examples

1. **index.html (Homepage)** - NSG replacement example (real output)
2. **examples.html** - Three interactive examples (real output) 
3. **features/firewall-rules.html** - Firewall rule changes table (real output)
4. **features/nsg-rules.html** - NSG security rules table (real output)
5. **features/module-grouping.html** - Module hierarchy structure (real output)
6. **features/sensitive-masking.html** - Key vault secret masking (real output)
7. **features/azure-optimizations.html** - Role assignment display (real output)
8. **features/large-values.html** - Large value diff example (real output)
9. **features/value-formatting.html** - Formatting rules (conceptual, no changes needed)
10. **features/custom-templates.html** - Template syntax (conceptual, no changes needed)
11. **features/misc.html** - Simple summaries (conceptual, no changes needed)

### Extraction Commands

#### Homepage NSG Example

```bash
# Extract NSG replacement section
cd /home/mathias/git/tfplan2md
sed -n '180,195p' artifacts/comprehensive-demo.md > .tmp/homepage-nsg-example.md

# Generate Azure DevOps HTML
dotnet run --no-build --project src/tools/Oocx.TfPlan2Md.HtmlRenderer/Oocx.TfPlan2Md.HtmlRenderer.csproj \
  --input .tmp/homepage-nsg-example.md \
  --flavor azdo > .tmp/homepage-nsg-example.azdo.html
```

### Generated HTML Examples

The rendered HTML examples use the HtmlRenderer tool to convert markdown artifacts:

```bash
# Generate Azure DevOps flavor HTML from comprehensive-demo
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor azdo \
  --output artifacts/comprehensive-demo.azdo.html

# Generate GitHub flavor HTML from comprehensive-demo
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor github \
  --output artifacts/comprehensive-demo.github.html

# Generate firewall rules demo HTML
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input examples/firewall-rules-demo/firewall-rules.md \
  --flavor azdo \
  --output examples/firewall-rules-demo/firewall-rules.azdo.html
```

### Terraform show approximation outputs

Use the TerraformShowRenderer tool to generate authentic "before tfplan2md" examples for website comparisons.

```bash
# Plan 1 (baseline, colored)
dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.json \
  --output src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.txt

# Plan 1 (plain text)
dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.json \
  --no-color \
  --output src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.nocolor.txt

# Plan 2 (replacement-focused, colored)
dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.json \
  --output src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.txt

# UAT artifact (ANSI, used for feature screenshots)
dotnet run --project src/tools/Oocx.TfPlan2Md.TerraformShowRenderer -- \
  --input src/tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.json \
  --output artifacts/uat-terraform-show-approximation.txt
```

**Source artifacts:**
- `artifacts/comprehensive-demo.md` - Full comprehensive demo markdown
- `artifacts/comprehensive-demo.azdo.html` - Azure DevOps rendered HTML
- `artifacts/comprehensive-demo.github.html` - GitHub rendered HTML
- `examples/firewall-rules-demo/firewall-rules.md` - Firewall rules demo markdown (generated from plan.json)
- `examples/firewall-rules-demo/firewall-rules.azdo.html` - Firewall rules demo HTML

**How firewall-rules demo was created:**
1. Created minimal Terraform configuration in `examples/firewall-rules-demo/main.tf`
2. Hand-crafted realistic `plan.json` showing firewall rule changes (priority change, rule add/modify/remove)
3. Generated markdown: `dotnet run --project src/Oocx.TfPlan2Md -- examples/firewall-rules-demo/plan.json --output examples/firewall-rules-demo/firewall-rules.md`
4. Generated HTML using HtmlRenderer (command above)
5. Extracted firewall rule `<details>` section for use in website examples

**How module grouping / role assignment / sensitive value examples were created:**
1. Extracted relevant sections from `artifacts/comprehensive-demo.md` into temporary markdown files:
   - `.tmp/module-grouping-example.md` - Sections showing root, module.network, module.security, module.network.module.monitoring
   - `.tmp/role-assignment-example.md` - Single azurerm_role_assignment resource
   - `.tmp/sensitive-masked-example.md` - azurerm_key_vault_secret with `(sensitive)` value
   - `.tmp/sensitive-shown-example.md` - azurerm_key_vault_secret with actual value `super-secret-value`
2. Generated HTML using HtmlRenderer (azdo flavor):
   ```bash
  dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- --input .tmp/module-grouping-example.md --flavor azdo --output .tmp/module-grouping-example.azdo.html
  dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- --input .tmp/role-assignment-example.md --flavor azdo --output .tmp/role-assignment-example.azdo.html
  dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- --input .tmp/sensitive-masked-example.md --flavor azdo --output .tmp/sensitive-masked-example.azdo.html
  dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer -- --input .tmp/sensitive-shown-example.md --flavor azdo --output .tmp/sensitive-shown-example.azdo.html
   ```
3. Extracted HTML from generated files and embedded into examples.html

The `/features/index.html` page shows feature descriptions but no code snippets. Code examples will be needed for:
- Getting Started page (installation, first usage)
- CI/CD integration examples
- Custom template examples

## Planned Code Examples

| ID | Used on Page | Type | Source-of-Truth | Status |
|----|--------------|------|-----------------|--------|
| 1 | `/getting-started.html` | Docker pull | README.md | ⬜ Not added |
| 2 | `/getting-started.html` | Basic usage | README.md | ⬜ Not added |
| 3 | `/getting-started.html` | GitHub Actions workflow | README.md / examples/ | ⬜ Not added |
| 4 | `/getting-started.html` | Azure DevOps pipeline | README.md / examples/azuredevops/ | ⬜ Not added |
| 5 | `/docs.html` | CLI options reference | docs/spec.md | ⬜ Not added |
| 6 | `/features/custom-templates.html` | Custom template example | docs/features.md | ⬜ Not added |

## Source Locations

### README.md Quick Start

The README contains copy/paste ready examples:

```bash
# Docker pull
docker pull oocx/tfplan2md:latest

# Basic usage
terraform show -json plan.tfplan | docker run -i oocx/tfplan2md
```

### CI/CD Examples

- **GitHub Actions**: See README.md "CI/CD Integration" section
- **Azure DevOps**: See `examples/azuredevops/` directory

### CLI Reference

Full CLI options documented in `docs/spec.md` and can be retrieved via:

```bash
docker run oocx/tfplan2md --help
```

## Regeneration

Most code examples are static and sourced from documentation. If documentation changes:

1. Update the source file (README.md, docs/*)
2. Copy the updated snippet to the website page
3. Update this inventory to reflect any changes

## Decision Log

- 2026-01-03: Initial inventory created. No code examples on website yet.
- 2026-01-03: Documented planned examples and source locations.
- 2026-01-04: Replaced hand-crafted examples with real comprehensive-demo output for Module Grouping, Role Assignment Display, and Sensitive Value Masking. All feature examples on examples.html now use authentic tfplan2md formatting.
