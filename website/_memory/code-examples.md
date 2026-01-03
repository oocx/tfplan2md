# Website Code Examples Inventory

This document lists the code examples shown on the website and how to (re)generate them.

## Rules

- Every code example/snippet referenced by the website must have an entry here.
- Prefer sourcing examples from existing documentation (README.md, docs/) unless explicitly instructed otherwise.
- Code examples should be copy/paste ready.

## Current State

**Status:** No code examples are currently displayed on the website.

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
