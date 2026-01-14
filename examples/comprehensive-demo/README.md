# Comprehensive Demo

This directory contains a handcrafted Terraform plan JSON that demonstrates all supported features of the default `tfplan2md` template. It is meant for documentation, demos, and quick manual testing—no Terraform configuration files are included.

## Contents

- `plan.json` – Comprehensive demo plan (static JSON)
- `demo-principals.json` – Principal ID to display-name mapping for role assignments
- `report.md` – Generated sample using the default template
- `report-with-sensitive.md` – Generated sample with `--show-sensitive`
- `report-summary.md` – Generated sample using the summary template

## Feature Coverage Matrix

| Area | Covered By |
|------|------------|
| Module grouping (root, module.network, module.security, nested monitoring) | Multiple resources across modules |
| Action types (create, update, replace, destroy, no-op) | Resource mix in `plan.json` |
| Resource type breakdown | Diverse Azure resources (RG, VNet, Subnet, Storage, Key Vault, Role Assignments, Firewall, Log Analytics) |
| Sensitive handling | Storage access keys, Key Vault secret value |
| Attribute tables (create/delete/update/replace) | Resource mix with rich attributes |
| Role assignments (scope + principal mapping) | Role assignment resources using `demo-principals.json` |
| Firewall rule semantic diff | `azurerm_firewall_network_rule_collection.network_rules` update |
| Metadata (version, timestamp) | Top-level `format_version`, `terraform_version`, `timestamp` |
| Complex attribute types | Maps, lists, nested objects, computed values |
| Docker inclusion | Files copied into the published Docker image under `/examples/comprehensive-demo/` |

## Usage (local build)

```bash
# Render default report
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/comprehensive-demo/plan.json \
  --principals examples/comprehensive-demo/demo-principals.json

# Render with sensitive values
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/comprehensive-demo/plan.json \
  --principals examples/comprehensive-demo/demo-principals.json \
  --show-sensitive

# Render summary only
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/comprehensive-demo/plan.json \
  --template summary

# Render with debug information
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/comprehensive-demo/plan.json \
  --principals examples/comprehensive-demo/demo-principals.json \
  --debug
```

## Usage (Docker)

The demo files are packaged into the Docker image at `/examples/comprehensive-demo/`.

```bash
# Default report
docker run --rm oocx/tfplan2md /examples/comprehensive-demo/plan.json \
  --principals /examples/comprehensive-demo/demo-principals.json

# Report with sensitive values
docker run --rm oocx/tfplan2md /examples/comprehensive-demo/plan.json \
  --principals /examples/comprehensive-demo/demo-principals.json \
  --show-sensitive

# Summary report
docker run --rm oocx/tfplan2md /examples/comprehensive-demo/plan.json \
  --template summary

# Report with debug information
docker run --rm oocx/tfplan2md /examples/comprehensive-demo/plan.json \
  --principals /examples/comprehensive-demo/demo-principals.json \
  --debug
```

## Regenerating Sample Reports

```bash
# Default template
cd ../../
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/comprehensive-demo/plan.json \
  --principals examples/comprehensive-demo/demo-principals.json \
  > examples/comprehensive-demo/report.md

# With sensitive values
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/comprehensive-demo/plan.json \
  --principals examples/comprehensive-demo/demo-principals.json \
  --show-sensitive \
  > examples/comprehensive-demo/report-with-sensitive.md

# Summary template
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/comprehensive-demo/plan.json \
  --template summary \
  > examples/comprehensive-demo/report-summary.md
```
