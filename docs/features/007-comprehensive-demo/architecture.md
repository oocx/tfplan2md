# Architecture: Comprehensive Demo

## Status

Proposed

## Context

We need a comprehensive demonstration of `tfplan2md` capabilities that serves multiple purposes:
1.  **Documentation**: Showcases all features in the README and docs.
2.  **Testing**: Acts as a manual test fixture for verifying rendering logic.
3.  **User Trial**: Allows users to try the tool immediately without needing their own Terraform plan.

The current "comprehensive-demo" specification outlines the requirements for the content of this demo. This architecture document defines how it will be integrated into the repository and the distribution artifact (Docker image).

## Options Considered

### Option 1: Generate from Real Terraform
- **Description**: Include `.tf` files and run `terraform plan` during build or CI to generate the JSON.
- **Pros**: Guarantees valid JSON structure; ensures sync with Terraform behavior.
- **Cons**: Requires Terraform installation in CI/build; requires Azure provider initialization (might need dummy credentials); slow; complex to maintain specific edge cases (like "destroy" actions) without real state.

### Option 2: Handcrafted/Static JSON (Selected)
- **Description**: Maintain a static `plan.json` file that is manually crafted (or generated once and modified).
- **Pros**: No build-time dependencies; total control over the data (can simulate any edge case easily); fast; simple.
- **Cons**: JSON might become invalid if manually edited incorrectly; requires discipline to update when features change.

## Decision

We will use **Option 2: Handcrafted/Static JSON**.

We will create a dedicated directory `examples/comprehensive-demo/` containing:
- `plan.json`: The static plan file.
- `demo-principals.json`: A sample principal mapping file.
- `README.md`: Documentation for the demo.

We will also **include these files in the Docker image** to enable immediate user testing.

## Rationale

- **Simplicity**: We avoid adding Terraform as a build dependency.
- **Flexibility**: We can easily simulate complex scenarios (like specific diffs, sensitive values, various resource types) that might be hard to reproduce with a real Terraform run against a dummy state.
- **User Experience**: Users can run `docker run oocx/tfplan2md /examples/comprehensive-demo/plan.json` immediately.

## Implementation Notes

### Directory Structure
```
examples/comprehensive-demo/
├── plan.json
├── demo-principals.json
├── README.md
├── report.md (generated)
├── report-with-sensitive.md (generated)
└── report-summary.md (generated)
```

### Docker Integration
Modify `Dockerfile` to copy the example directory into the runtime image.

```dockerfile
# In the runtime stage
COPY examples/comprehensive-demo /examples/comprehensive-demo
```

### JSON Structure
The `plan.json` must adhere to the Terraform JSON output schema.
Key sections to populate:
- `format_version`: "1.2"
- `terraform_version`: "1.x.x"
- `resource_changes`: Array of resource change objects.
  - `address`: "module.x.resource.y"
  - `type`: "azurerm_..."
  - `change`:
    - `actions`: ["create"], ["update"], ["delete"], etc.
    - `before`: {...}
    - `after`: {...}
    - `after_unknown`: {...}

### Principal Mapping
`demo-principals.json` format:
```json
{
  "00000000-0000-0000-0000-000000000000": "Jane Doe (User)",
  "11111111-1111-1111-1111-111111111111": "DevOps Team (Group)"
}
```

## Consequences

### Positive
- Users have an immediate "try it now" experience with Docker.
- We have a stable test fixture that doesn't change unless we want it to.
- No external dependencies for the demo.
- The demo output serves as a markdown quality validation target in CI.

### Negative
- The `plan.json` is a large file that must be kept valid manually.
- Risk of `plan.json` drifting from what real Terraform produces (e.g., if Terraform changes its JSON schema).

