# Firewall Rules with Static Analysis Example

This example demonstrates how firewall rule changes appear with static analysis findings in the markdown report.

## Contents

- **plan.json**: Terraform plan showing firewall rule collection changes
- **analysis.sarif**: SARIF file with a security warning about overly permissive firewall rule
- The firewall rule `allow-http` has a security finding: it uses wildcard destination (`*`) which allows unrestricted egress

## Generate Report

```bash
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  --code-analysis-results examples/firewall-with-static-analysis/analysis.sarif \
  --output examples/firewall-with-static-analysis/report.md \
  examples/firewall-with-static-analysis/plan.json
```

## Use for Homepage Screenshot

This example is specifically designed for the homepage screenshot, showing:
- Semantic diffs in firewall rules (Before/After comparison)
- Static analysis warning indicator on a specific rule
- Realistic security concern (overly broad destination addresses)
