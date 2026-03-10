module.exports = {
  quickLinks: [
    { href: "https://github.com/oocx/tfplan2md", label: "GitHub Repository", variant: "primary", external: true },
    { href: "https://github.com/oocx/tfplan2md/issues", label: "Issue Tracker", variant: "secondary", external: true }
  ],
  branchPrefixes: [
    { prefix: "feat/", purpose: "New features", example: "feat/semantic-diff-firewall-rules" },
    { prefix: "fix/", purpose: "Bug fixes", example: "fix/template-rendering-escape" },
    { prefix: "docs/", purpose: "Documentation changes", example: "docs/update-installation-guide" },
    { prefix: "refactor/", purpose: "Code refactoring", example: "refactor/parser-immutable-models" },
    { prefix: "chore/", purpose: "Maintenance tasks", example: "chore/update-dependencies" },
    { prefix: "workflow/", purpose: "Agent/workflow changes", example: "workflow/add-uat-skill" },
    { prefix: "website/", purpose: "Website changes", example: "website/add-feature-page" }
  ],
  testTypes: [
    "Unit Tests - Test individual components in isolation",
    "Integration Tests - Test end-to-end workflows, including Docker-based tests",
    "Invariant Tests - Property-based tests that verify markdown invariants",
    "Snapshot Tests - Golden file tests that detect unexpected output changes",
    "Template Isolation Tests - Test each template independently",
    "Fuzz Tests - Test with edge-case inputs (special characters, Unicode)",
    "Markdownlint Integration - Docker-based linting with markdownlint-cli2"
  ],
  markdownRequirements: [
    "Pass markdownlint validation (MD012 and other rules)",
    "Parse correctly with Markdig",
    "Render correctly on GitHub and Azure DevOps",
    "Have proper table structure (no blank lines between rows)",
    "Have proper heading spacing (blank lines before/after)",
    "Have balanced HTML tags (<details>, <summary>)"
  ],
  commitTypes: [
    { type: "feat", description: "A new feature", versionBump: "Minor (0.x.0)" },
    { type: "fix", description: "A bug fix", versionBump: "Patch (0.0.x)" },
    { type: "docs", description: "Documentation only", versionBump: "None" },
    { type: "style", description: "Code style changes", versionBump: "None" },
    { type: "refactor", description: "Code refactoring", versionBump: "None" },
    { type: "test", description: "Adding or modifying tests", versionBump: "None" },
    { type: "chore", description: "Other maintenance", versionBump: "None" }
  ],
  pullRequestSteps: [
    { lead: "Create a feature branch", text: "from main" },
    { lead: "Make your changes", text: "following the coding guidelines" },
    { lead: "Ensure all checks pass", text: "", showChecks: true },
    { lead: "Push your branch", text: "and create a Pull Request" },
    { lead: "Wait for review", text: "PR validation will run automatically" },
    { lead: "Merge using Rebase and merge", text: "This project requires a linear history" }
  ],
  prRequirements: [
    "All CI checks must pass (build, test, format, vulnerability scan)",
    "Code follows the project's style guidelines (enforced by .editorconfig)",
    "Commit messages follow Conventional Commits format",
    "If conflicts occur, rebase onto main: git pull --rebase origin main",
    "Force-push your branch: git push --force-with-lease",
    "Do NOT use Squash and merge or Create a merge commit"
  ],
  accessModifiers: [
    "private - Default for class members",
    "internal - For cross-assembly visibility within the solution",
    "public - Only for main entry points or when absolutely necessary"
  ],
  codeComments: [
    "All members (public, internal, private) require XML doc comments",
    "Comments must explain why not just what",
    "Use standard XML tags: <summary>, <param>, <returns>, <remarks>",
    "Reference related features/specifications for traceability"
  ],
  prerequisites: [
    { href: "https://dotnet.microsoft.com/download", label: ".NET 10 SDK", external: true },
    { href: "https://git-scm.com/", label: "Git", external: true },
    { href: "https://www.docker.com/", label: "Docker", external: true, suffix: " (for running integration tests)" }
  ],
  preCommitHooks: [
    "pre-commit: Runs dotnet format --verify-no-changes and dotnet build",
    "commit-msg: Validates commit message follows Conventional Commits format"
  ],
  hookFailureSteps: [
    "Format issues: Run dotnet format to fix formatting",
    "Build errors: Fix the build errors before committing",
    "Commit message: Ensure your message follows the format type: description"
  ],
  releaseSteps: [
    {
      text: "When commits are pushed to main, the CI workflow runs Versionize"
    },
    {
      text: "Versionize only runs when Docker-relevant files changed (runtime code, examples, build config)"
    },
    {
      text: "If there are feat:, fix:, or BREAKING CHANGE commits, Versionize:",
      items: [
        "Bumps the version in .csproj",
        "Updates CHANGELOG.md",
        "Creates a git tag (for example, v0.2.0)"
      ]
    },
    {
      text: "The tag push triggers the Release workflow which:",
      items: [
        "Creates a GitHub Release with changelog notes",
        "Builds and pushes the Docker image to Docker Hub"
      ]
    }
  ]
};