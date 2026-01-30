# Code Analysis Example

This example shows how static code analysis findings appear in the markdown report.

## Files

- `plan.json`: Minimal Terraform plan with an AWS S3 bucket.
- `analysis.sarif`: Sample SARIF 2.1.0 output with a security finding.
- `report.md`: Generated report showing the code analysis summary and findings.

## Regenerate

```bash
./tfplan2md examples/code-analysis/plan.json \
  --code-analysis-results examples/code-analysis/analysis.sarif \
  --render-target github \
  --output examples/code-analysis/report.md
```
