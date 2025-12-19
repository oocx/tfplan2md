# Project specification

## Project Overview
`tfplan2md` is a CLI tool that converts Terraform plan JSON files into human-readable markdown reports. It is built using modern .NET 10 and C# 13 features, emphasizing clean architecture, testability, and maintainability.

The goal of this tool is to help DevOps and infrastructure teams easily review Terraform plans by generating concise markdown summaries of proposed changes. The summaries must be customizable via template files, and provide a default template out of the box.

## Project Organization
- Use namespaces to organize the code. The root namespace is `Oocx.TfPlan2Md`
- Use a single project for the CLI tool; use separate projects for tests
- Organize files by feature (e.g., `Parsing`, `MarkdownGeneration`, `CLI`), not by type (e.g., `Models`, `Services`)
- Place all documentation in the /docs folder, except for the README.md at the root
- Key architecture decisions must be documented in separate files per decision. Place those files in /docs/adr-nnn-title.
- The testing strategy is described in /docs/testing-strategy.md
- Features of tfplan2md (from a user perspective) are described in /docs/features.md
- Contribution guidelines are in /CONTRIBUTING.md

## CI/CD and Versioning

### Versioning Strategy
- Use [Semantic Versioning](https://semver.org/) (SemVer)
- Automate versioning with [Versionize](https://github.com/versionize/versionize) based on [Conventional Commits](https://www.conventionalcommits.org/)
- Version tags use `v` prefix (e.g., `v1.0.0`)
- Docker images are tagged with full version, minor version, major version, and `latest`

### Commit Message Format
- Follow [Conventional Commits](https://www.conventionalcommits.org/) specification
- Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
- Breaking changes: Use `BREAKING CHANGE:` footer or `!` after type
- Pre-commit hooks enforce commit message format

### GitHub Actions Workflows

| Workflow | File | Trigger | Purpose |
|----------|------|---------|----------|
| PR Validation | `pr-validation.yml` | Pull requests to `main` | Format check, build, test, vulnerability scan |
| CI | `ci.yml` | Push to `main` | Build, test, run Versionize to bump version and create tag |
| Release | `release.yml` | Version tags (`v*`) or manual | Create GitHub Release with cumulative changelog, build and push Docker image |

**Release Notes:** The release workflow generates cumulative release notes that include all changes since the last GitHub release. This ensures Docker deployments contain complete change history even when intermediate versions are not released.

### Code Quality
- **Analyzers**: Microsoft.CodeAnalysis.NetAnalyzers with `TreatWarningsAsErrors`
- **Code Style**: Enforced via `.editorconfig` and `dotnet format`
- **Pre-commit Hooks**: [Husky.Net](https://github.com/alirezanet/Husky.Net) runs format check and build before commit
- **Dependency Updates**: Dependabot configured for NuGet, Docker, and GitHub Actions

### Branch Strategy
- `main` branch is always in a releasable state
- Feature branches created from `main` for new features or fixes
- Pull requests require passing validation checks before merge
