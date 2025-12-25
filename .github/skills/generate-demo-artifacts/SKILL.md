---
name: generate-demo-artifacts
description: Generate the comprehensive demo markdown artifact from the current codebase. Use before UAT to ensure the test artifact reflects the latest code.
compatibility: Requires .NET SDK and access to the repository workspace.
---

# Generate Demo Artifacts

## Purpose
Regenerate the `artifacts/comprehensive-demo.md` file using the current code. This ensures UAT tests validate the actual behavior of the tool, not stale output.

## Hard Rules
### Must
- Run `dotnet build` before generating to ensure latest code is compiled.
- Use the canonical input files: `examples/comprehensive-demo/plan.json` and `demo-principals.json`.
- Overwrite the existing artifact file.
- Verify the output is valid markdown (non-empty, starts with `#`).

### Must Not
- Use cached or stale build outputs.
- Modify the input plan.json or demo-principals.json files.

## Actions

### 1. Build the Project
```bash
dotnet build src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -c Release
```

### 2. Generate the Artifact
```bash
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --output artifacts/comprehensive-demo.md \
  examples/comprehensive-demo/plan.json
```

### 3. Verify Output
```bash
# Check file exists and is non-empty
if [[ ! -s artifacts/comprehensive-demo.md ]]; then
  echo "ERROR: Generated artifact is empty or missing."
  exit 1
fi

# Check it starts with a markdown heading
if ! head -1 artifacts/comprehensive-demo.md | grep -q '^#'; then
  echo "ERROR: Generated artifact does not appear to be valid markdown."
  exit 1
fi

echo "SUCCESS: artifacts/comprehensive-demo.md generated."
```

## Golden Example
```bash
$ dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
    --input examples/comprehensive-demo/plan.json \
    --principal-mapping examples/comprehensive-demo/demo-principals.json \
    --output artifacts/comprehensive-demo.md
$ head -3 artifacts/comprehensive-demo.md
# Terraform Plan Report

## Summary
```
