# Contributing to tfplan2md

Thank you for your interest in contributing to tfplan2md! This document provides guidelines and instructions for contributing.

## Development Workflow

### Branch Strategy

1. **Main branch** (`main`) — Always in a releasable state
2. **Feature branches** — Created from `main` for new features or fixes

### Creating a Feature Branch

```bash
git checkout main
git pull origin main
git checkout -b feat/your-feature-name
```

Use these branch prefixes:
- `feat/` — New features
- `fix/` — Bug fixes
- `docs/` — Documentation changes
- `refactor/` — Code refactoring
- `chore/` — Maintenance tasks

## Commit Messages

This project uses [Conventional Commits](https://www.conventionalcommits.org/) to automate versioning and changelog generation.

### Commit Message Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description | Version Bump |
|------|-------------|--------------|
| `feat` | A new feature | Minor (0.x.0) |
| `fix` | A bug fix | Patch (0.0.x) |
| `docs` | Documentation only changes | None |
| `style` | Code style changes (formatting, etc.) | None |
| `refactor` | Code refactoring without feature changes | None |
| `perf` | Performance improvements | Patch (0.0.x) |
| `test` | Adding or modifying tests | None |
| `build` | Build system or dependency changes | None |
| `ci` | CI configuration changes | None |
| `chore` | Other maintenance tasks | None |
| `revert` | Reverting a previous commit | Depends |

### Breaking Changes

For breaking changes, add `BREAKING CHANGE:` in the commit footer:

```
feat(parser): change output format to JSON

BREAKING CHANGE: The output format has changed from plain text to JSON.
```

Or use `!` after the type:

```
feat(parser)!: change output format to JSON
```

Breaking changes trigger a **major** version bump (x.0.0).

### Examples

```bash
# Feature
git commit -m "feat(cli): add --output flag for custom output path"

# Bug fix
git commit -m "fix(parser): handle empty resource changes array"

# Documentation
git commit -m "docs: update installation instructions"

# Breaking change
git commit -m "feat(api)!: rename TerraformPlan to PlanResult"
```

## Pull Request Process

1. **Create a feature branch** from `main`
2. **Make your changes** following the coding guidelines
3. **Ensure all checks pass**:
   ```bash
   dotnet format --verify-no-changes
   dotnet build
   dotnet test
   ```
4. **Push your branch** and create a Pull Request
5. **Wait for review** — PR validation will run automatically

### PR Requirements

- All CI checks must pass (build, test, format, vulnerability scan)
- Code follows the project's style guidelines (enforced by `.editorconfig`)
- Commit messages follow Conventional Commits format

## Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- [Docker](https://www.docker.com/) (for running integration tests)

### Getting Started

```bash
# Clone the repository
git clone https://github.com/oocx/tfplan2md.git
cd tfplan2md

# Restore tools (including Husky for git hooks)
dotnet tool restore

# Install git hooks
dotnet husky install

# Build and test
dotnet build
dotnet test
```

### Pre-commit Hooks

This project uses [Husky.Net](https://github.com/alirezanet/Husky.Net) for git hooks:

- **pre-commit**: Runs `dotnet format --verify-no-changes` and `dotnet build`
- **commit-msg**: Validates commit message follows Conventional Commits format

If your commit is rejected:
1. **Format issues**: Run `dotnet format` to fix formatting
2. **Build errors**: Fix the build errors before committing
3. **Commit message**: Ensure your message follows the format `type: description`

## Release Process

Releases are automated via GitHub Actions:

1. When commits are pushed to `main`, the CI workflow runs Versionize
2. If there are `feat:`, `fix:`, or `BREAKING CHANGE` commits, Versionize:
   - Bumps the version in `.csproj`
   - Updates `CHANGELOG.md`
   - Creates a git tag (e.g., `v0.2.0`)
3. The tag push triggers the Release workflow which:
   - Creates a GitHub Release with changelog notes
   - Builds and pushes the Docker image to Docker Hub

## Questions?

If you have questions, feel free to open an issue for discussion.
