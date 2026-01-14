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
- Documentation subfolders under `/docs/features`, `/docs/issues`, and `/docs/workflow` use a global numeric prefix: `NNN-<topic-slug>`.
  - **Parallel work rule:** If two branches chose the same next `NNN`, the first PR to merge keeps it; later PRs must renumber before merge.
- The testing strategy is described in /docs/testing-strategy.md
- Features of tfplan2md (from a user perspective) are described in /docs/features.md
- Contribution guidelines are in /CONTRIBUTING.md

## Coding Standards

### Access Modifiers

**This is NOT a class library** - tfplan2md is a standalone CLI tool that is not referenced by other .NET projects. Therefore:

- **Use the most restrictive access modifier that works**
  - Prefer `private` for class members whenever possible
  - Use `internal` for types and members that need cross-assembly visibility within the solution
  - Avoid `public` unless there is a clear justification

- **Valid reasons for `public` access:**
  - Main entry points (e.g., `Program.cs` top-level statements or `Main` method)
  - Types/members that must be visible to test projects

- **Test Access Strategy:**
  - Use `InternalsVisibleTo` attribute to expose `internal` members to test projects
  - Do NOT make members `public` solely for testing purposes
  - Add this to the main project's `.csproj` or `AssemblyInfo.cs`:
    ```csharp
    [assembly: InternalsVisibleTo("Oocx.TfPlan2Md.TUnit")]
    [assembly: InternalsVisibleTo("Oocx.TfPlan2Md.Tests")]
    [assembly: InternalsVisibleTo("Oocx.TfPlan2Md.MSTests")]
    ```
  - **Note**: TUnit is the primary framework; legacy test projects (xUnit and MSTest) are preserved for compatibility but not actively used

- **Why this matters:**
  - Agents were considering backwards compatibility and breaking changes for `public` methods even though no external consumers exist
  - Restrictive access modifiers clearly communicate that members are internal implementation details
  - This prevents false concerns about API stability and breaking changes

### Code Comments

- **All class members must have XML documentation comments** (including private members)
- Comments should explain "why" something was done, not just repeat what the code shows
- Follow the comprehensive guidelines in [docs/commenting-guidelines.md](commenting-guidelines.md)
- Key requirements:
  - Use `<summary>`, `<param>`, `<returns>`, `<remarks>` tags appropriately
  - Reference related features/specifications for traceability
  - Keep comments synchronized with code changes
  - Provide examples for complex methods using `<example>` and `<code>` tags

## CI/CD and Versioning

### Versioning Strategy
- Use [Semantic Versioning](https://semver.org/) (SemVer)
- Automate versioning with [Versionize](https://github.com/versionize/versionize) based on [Conventional Commits](https://www.conventionalcommits.org/)
- Version tags use `v` prefix (e.g., `v1.0.0`)
- Docker images are tagged with full version. Stable releases also include minor version, major version, and `latest` tags.

### Commit Message Format
- Follow [Conventional Commits](https://www.conventionalcommits.org/) specification
- Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
- Breaking changes: Use `BREAKING CHANGE:` footer or `!` after type
- Pre-commit hooks enforce commit message format

### GitHub Actions Workflows

| Workflow | File | Trigger | Purpose |
|----------|------|---------|----------|
| PR Validation | `pr-validation.yml` | Pull requests to `main` | Format check, build, test, markdown lint, vulnerability scan |
| CI | `ci.yml` | Push to `main` | Build, test, markdown lint, run Versionize to bump version and create tag |
| Release | `release.yml` | Version tags (`v*`) | Create GitHub Release with cumulative changelog, build and push Docker image |

**Markdown Quality:** Both PR validation and CI workflows generate the comprehensive demo report and validate it with `markdownlint-cli2` to ensure templates produce valid markdown.

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

**Branch Protection Limitation (Private Repos):**
- GitHub branch protection rules (requiring status checks) require GitHub Pro for private repositories
- Until the repository is made public, PRs CAN be merged before the "PR Validation" workflow completes
- **CRITICAL**: Agents and maintainers must manually verify "PR Validation" shows ✅ success before merging
- The Release Manager agent enforces this requirement by monitoring PR status checks (prefer GitHub chat tools; `gh pr checks --watch` is a fallback)
- Once the repository is public, configure branch protection to require the "PR Validation" workflow as a required status check
