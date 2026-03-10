module.exports = {
  flowSteps: [
    { icon: "📄", title: "Input", description: "Terraform Plan JSON", command: "terraform show -json" },
    { icon: "⚙️", title: "tfplan2md", description: "Parse · Transform · Render" },
    { icon: "📝", title: "Output", description: "Markdown Report", command: "GitHub / Azure DevOps PR" }
  ],
  qualityGoals: [
    { className: "quality-1", priority: "Priority 1", title: "Security", description: "Mask sensitive values by default · FROM scratch container · AOT-compiled static binary · Non-root user (UID 1654) · No shell · Minimal attack surface" },
    { className: "quality-2", priority: "Priority 2", title: "Reliability", description: "Handle malformed JSON gracefully · Validate all markdown output · Comprehensive test coverage" },
    { className: "quality-3", priority: "Priority 3", title: "Usability", description: "Simple CLI · Sensible defaults · Clear error messages · Zero configuration needed" },
    { className: "quality-4", priority: "Priority 4", title: "Maintainability", description: "Clean architecture · Immutable models · Pure functions · Modern C# patterns" },
    { className: "quality-5", priority: "Priority 5", title: "Extensibility", description: "Custom templates · Resource-specific renderers · Provider-specific handling" },
    { className: "quality-6", priority: "Priority 6", title: "Performance", description: "Fast startup for CI/CD · Handle large plans efficiently · 14.7MB Docker image (89.6% reduction) · AOT compilation with aggressive trimming" }
  ],
  coreComponents: [
    { icon: "🎯", title: "CLI", description: "Command-line parsing and orchestration. Handles user input, loads configuration, coordinates workflow.", files: ["CliParser.cs", "HelpTextProvider.cs"] },
    { icon: "📦", title: "Parsing", description: "Terraform plan JSON parsing into immutable domain models using System.Text.Json.", files: ["TerraformPlan.cs", "TerraformPlanParser.cs"] },
    { icon: "🔄", title: "Model Building", description: "Transform domain models into report models. Build resource summaries, group by module, apply inline diffing.", files: ["ReportModel.cs", "Summaries/*.cs"] },
    { icon: "✍️", title: "Rendering", description: "Apply Scriban templates to generate markdown. Supports default templates, summary template, and custom user templates.", files: ["MarkdownRenderer.cs", "ScribanHelpers.cs", "Templates/*.sbn"] },
    { icon: "☁️", title: "Azure Utilities", description: "Azure-specific functionality: principal ID mapping, resource ID parsing, role assignment formatting.", files: ["PrincipalMapper.cs", "azurerm/* templates"] },
    { icon: "🔒", title: "Security", description: "Sensitive value detection and masking. AOT-compiled static binary in FROM scratch container. Non-root user, no shell, minimal dependencies.", files: ["--show-sensitive flag", "FROM scratch + 3 musl libs"] }
  ],
  technologyStack: [
    { component: "Compilation", name: "NativeAOT (linux-musl-x64)", purpose: "Ahead-of-time compilation to native executable with aggressive trimming" },
    { component: "Language", name: "C# 13", purpose: "Modern language features: records, pattern matching, file-scoped namespaces" },
    { component: "JSON Parser", name: "System.Text.Json", purpose: "Parse Terraform plan JSON with built-in .NET library" },
    { component: "Template Engine", name: "Scriban 6.5.2", purpose: "Render markdown from customizable templates" },
    { component: "Container Base", name: "FROM scratch", purpose: "Empty base with 3 musl libraries (14.7MB) - minimal attack surface, no shell, non-root user" },
    { component: "Test Framework", name: "TUnit 1.9.26", purpose: "Unit and integration tests with comprehensive coverage, async-first design, and real-time progress reporting" },
    { component: "Markdown Linter", name: "markdownlint-cli2 0.20.0", purpose: "Validate markdown output for GitHub/Azure DevOps compatibility" }
  ],
  patterns: [
    { title: "Immutability", description: "All data models are immutable records. No mutable shared state. Pure functions for transformations.", benefit: "✅ Thread-safe, predictable, easier to reason about" },
    { title: "Template-Driven", description: "Default templates embedded as resources. Custom templates from filesystem. Resource-specific overrides.", benefit: "✅ Flexible, customizable, user-controlled formatting" },
    { title: "Separation of Concerns", description: "Clear boundaries: Parsing → Model Building → Rendering. Each component has single responsibility.", benefit: "✅ Testable, maintainable, modular" },
    { title: "Security by Default", description: "Sensitive values masked unless explicitly shown. FROM scratch containers with AOT-compiled static binaries. Non-root user, no shell, minimal dependencies.", benefit: "✅ Safe for CI/CD, minimal attack surface, sub-second startup" }
  ],
  decisions: [
    { title: "Scriban for Templating", description: "Lightweight, text-focused template engine with familiar syntax. Better fit than Razor or Liquid for markdown generation. Embeddable, supports custom functions, and works seamlessly with AOT compilation.", detailLabel: "Decision", detail: "Use Scriban for all markdown generation. Enable user customization through filesystem templates. Provide composable partials for clean template organization." },
    { title: "NativeAOT with FROM scratch", description: "Ahead-of-time compilation produces static native executable (linux-musl-x64). Deployed in FROM scratch container with only 3 musl libraries. Achieves 14.7MB image size (89.6% reduction from baseline) with fastest possible startup time.", detailLabel: "Benefits", detail: "Minimal attack surface, no shell, non-root user (UID 1654), sub-second startup in CI/CD. Build-time resource embedding with custom ResourceManager avoids runtime reflection." },
    { title: "Modern C# 13 Patterns", description: "Records for immutable data models, file-scoped namespaces, nullable reference types, pattern matching. Comprehensive XML documentation (including private members) for AI-assisted development.", detailLabel: "Benefits", detail: "Thread-safe, predictable, easier to reason about. Supports 100% AI-assisted development workflow with GitHub Copilot multi-model approach." },
    { title: "CSS Layers for Website Examples", description: "CSS @layer isolates rendered example styles from website page styles. Examples use @layer examples with lower cascade priority than website styles.", detailLabel: "Benefits", detail: "Prevents specificity conflicts. Examples remain readable while website styles always win conflicts. Clean separation between content and presentation layers." },
    { title: "Resource-Specific Templates with ViewModels", description: "Complex resources (firewall rules, NSG rules, role assignments, variable groups) use specialized templates with ViewModel pattern. C# Factory precomputes semantic diffs, matches items by key, and formats before/after comparisons.", detailLabel: "Benefits", detail: "Testable C# logic for complex merging and matching. Clean templates iterate preformatted rows. Powerful semantic diffing without Scriban limitations." },
    { title: "Single-Pass Template Rendering", description: "Direct template dispatch replaces render-then-replace pattern. Main template orchestrates with composable partials and resource-specific overrides.", detailLabel: "Benefits", detail: "No anchors or regex replacement. Explicit, debuggable template selection. No wasted computation. Scriban include with custom template loader." },
    { title: "Markdig for Platform-Specific HTML", description: "Standalone HtmlRenderer converts markdown to HTML with GitHub and Azure DevOps flavors. GitHub strips style attributes; Azure DevOps preserves them for theme support.", detailLabel: "Use Case", detail: "Visual testing, website examples, and screenshot generation. Wrapper templates with a content placeholder provide a development-friendly approximation." },
    { title: "Playwright for Visual Regression", description: "ScreenshotGenerator uses Playwright and Chromium for automated screenshot capture with full-page and targeted element support.", detailLabel: "CI Integration", detail: "Cross-platform, headless-only, actionable error messages, and union bounding boxes for multiple matches enable visual regression pipelines." },
    { title: "DiagnosticContext for Debug Output", description: "Optional diagnostics are collected throughout the pipeline and appended with a single debug flag.", detailLabel: "Benefits", detail: "Non-intrusive when disabled, cleanly separated from business logic, and easy to extend with new diagnostic categories." },
    { title: "Report Metadata for Build Traceability", description: "Every report includes tfplan2md version, git commit hash, generation timestamp, and Terraform version metadata.", detailLabel: "Benefits", detail: "Provides an audit trail for debugging, deterministic tests through a mockable metadata provider, and optional suppression with --hide-metadata." },
    { title: "TUnit for Async-First Testing", description: "Modern test framework with true async support, real-time progress reporting, parallel execution, and comprehensive coverage.", detailLabel: "Benefits", detail: "Faster feedback than older frameworks, better diagnostics, and close alignment with modern C# and AI-assisted workflows." },
    { title: "Terraform Show Approximation", description: "Development scripts can generate synthetic plan JSON from config and state without running terraform plan.", detailLabel: "Use Case", detail: "Speeds up template development and experimentation, but remains an approximation that is not suitable for production use." }
  ],
  ctas: [
    { href: "https://github.com/oocx/tfplan2md/blob/main/docs/architecture.md", label: "View Full Architecture (arc42)", variant: "primary", external: true },
    { href: "https://github.com/oocx/tfplan2md/tree/main/docs", label: "Browse All Documentation", variant: "secondary", external: true }
  ]
};