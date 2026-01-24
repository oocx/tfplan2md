# Firewall Rules Demo

This example demonstrates tfplan2md's semantic diffing for Azure Firewall network rule collections.

## Changes Demonstrated

1. **Priority change**: 100 → 150
2. **Rule added**: `allow-dns` (TCP/UDP port 53)
3. **Rule modified**: `allow-http` (source address and destination address changes)
4. **Rule removed**: `allow-ssh-old` (was in initial state)
5. **Rule unchanged**: `allow-https` (no changes)

## Generate Plan

```bash
# Initial plan
terraform init
terraform plan -out=plan.tfplan

# Save as binary
terraform show -json plan.tfplan > plan-before.json

# Update main.tf to main-updated.tf content
cp main-updated.tf main.tf

# Generate updated plan
terraform plan -out=plan.tfplan
terraform show -json plan.tfplan > plan-after.json

# Create diff plan (this is what tfplan2md processes)
# In a real workflow, this would be a single plan showing before→after
```

## Generate tfplan2md Output

```bash
# Generate markdown report
docker run --rm -v "$(pwd):/data" ghcr.io/oocx/tfplan2md:latest \
  --input /data/plan.json \
  --output /data/firewall-rules.md

# Generate HTML for website
dotnet run --project ../../src/tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input firewall-rules.md \
  --flavor azdo \
  --output firewall-rules.azdo.html
```
