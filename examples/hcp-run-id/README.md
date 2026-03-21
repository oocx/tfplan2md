# HCP Run-ID Example

This example shows how to generate a tfplan2md report directly from an HCP Terraform run ID.

## Prerequisites

- `TFE_TOKEN` set to a user or team token with workspace admin access.
- Optional `TFE_ADDRESS` override for Terraform Enterprise (default is `https://app.terraform.io`).

## Usage

```bash
# HCP Terraform (default address)
TFE_TOKEN="<your-token>" \
  dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  --hcp-run-id "run-abc123" \
  --output artifacts/hcp-run-id-example.md

# Terraform Enterprise (custom address)
TFE_TOKEN="<your-token>" \
TFE_ADDRESS="https://tfe.example.com" \
  dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  --hcp-run-id "run-abc123" \
  --output artifacts/hcp-run-id-example.md
```

## Notes

- Input modes are mutually exclusive: use either `--hcp-run-id`, a positional `plan.json` file, or stdin.
- If `TFE_TOKEN` is missing, tfplan2md exits with an actionable error.
