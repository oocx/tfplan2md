module.exports = {
  flowSteps: [
    { icon: "📄", title: "Input", description: "Terraform Plan JSON", command: "terraform show -json" },
    { icon: "⚙️", title: "tfplan2md", description: "Parse · Transform · Render" },
    { icon: "📝", title: "Output", description: "Markdown Report", command: "GitHub / Azure DevOps PR" }
  ],
  qualityGoals: [
    { className: "quality-1", priority: "Priority 1", title: "Security", description: "Mask sensitive values by default · FROM scratch container · AOT-compiled static binary · Non-root user (UID 1654) · No shell · Minimal attack surface · Zero third-party runtime dependencies in the main CLI" },
    { className: "quality-2", priority: "Priority 2", title: "Reliability", description: "Handle malformed JSON gracefully · Validate all markdown output · Comprehensive test coverage" },
    { className: "quality-3", priority: "Priority 3", title: "Usability", description: "Simple CLI · Sensible defaults · Clear error messages · Zero configuration needed" },
    { className: "quality-4", priority: "Priority 4", title: "Maintainability", description: "Clean architecture · Immutable models · Pure functions · Modern C# patterns" },
    { className: "quality-5", priority: "Priority 5", title: "Extensibility", description: "Built-in templates · Resource-specific renderers · Provider-specific handling" },
    { className: "quality-6", priority: "Priority 6", title: "Performance", description: "Fast startup for CI/CD · Handle large plans efficiently · 2.1 MB Docker image · AOT compilation with aggressive trimming" }
  ],
  coreComponents: [
    { icon: "🎯", title: "CLI", description: "Command-line parsing and orchestration. Handles user input, loads configuration, coordinates workflow.", files: ["CliParser.cs", "HelpTextProvider.cs"] },
    { icon: "📦", title: "Parsing", description: "Terraform plan JSON parsing into immutable domain models using System.Text.Json.", files: ["TerraformPlan.cs", "TerraformPlanParser.cs"] },
    { icon: "🔄", title: "Model Building", description: "Transform domain models into report models. Build resource summaries, group by module, apply inline diffing.", files: ["ReportModel.cs", "Summaries/*.cs"] },
    { icon: "✍️", title: "Rendering", description: "Pure C# renderers generate markdown in a single pass. Built-in report, summary, and resource-specific renderers replace runtime template loading.", files: ["MarkdownRenderer.cs", "ReportRenderer.cs", "ResourceRendererRegistry.cs"] },
    { icon: "☁️", title: "Azure Utilities", description: "Azure-specific functionality: principal ID mapping, resource ID parsing, role assignment formatting.", files: ["PrincipalMapper.cs", "azurerm/* templates"] },
    { icon: "🔒", title: "Security", description: "Sensitive value detection and masking. AOT-compiled static binary in a FROM scratch container with no third-party runtime dependencies in the main CLI.", files: ["--show-sensitive flag", "FROM scratch + UPX-compressed binary"] }
  ],
  technologyStack: [
    { component: "Compilation", name: "NativeAOT (linux-musl-x64)", purpose: "Ahead-of-time compilation to native executable with aggressive trimming" },
    { component: "Language", name: "C# 13", purpose: "Modern language features: records, pattern matching, file-scoped namespaces" },
    { component: "JSON Parser", name: "System.Text.Json", purpose: "Parse Terraform plan JSON with built-in .NET library" },
    { component: "Rendering", name: "Pure C# Renderers", purpose: "Render markdown through built-in default, summary, and resource-specific renderer classes" },
    { component: "Container Base", name: "FROM scratch", purpose: "Scratch base with a single UPX-compressed NativeAOT binary (2.1 MB) — no shell, no runtime, non-root user" },
    { component: "Test Framework", name: "TUnit 1.9.26", purpose: "Unit and integration tests with comprehensive coverage, async-first design, and real-time progress reporting" },
    { component: "Markdown Linter", name: "markdownlint-cli2 0.20.0", purpose: "Validate markdown output for GitHub/Azure DevOps compatibility" }
  ],
  patterns: [
    { title: "Immutability", description: "All data models are immutable records. No mutable shared state. Pure functions for transformations.", benefit: "✅ Thread-safe, predictable, easier to reason about" },
    { title: "Renderer-Driven", description: "Built-in renderers generate markdown directly in C#. Resource-specific overrides plug into a shared registry.", benefit: "✅ Compile-time safety, simpler debugging, consistent output" },
    { title: "Separation of Concerns", description: "Clear boundaries: Parsing → Model Building → Rendering. Each component has single responsibility.", benefit: "✅ Testable, maintainable, modular" },
    { title: "Security by Default", description: "Sensitive values masked unless explicitly shown. FROM scratch containers with AOT-compiled static binaries. Non-root user, no shell, zero third-party runtime dependencies in the main CLI.", benefit: "✅ Safe for CI/CD, minimal attack surface, sub-second startup" }
  ],
  decisions: [
    { title: "Pure C# Rendering", description: "Rendering moved out of Scriban templates and into statically typed C# renderer classes. The user-facing template surface is now limited to built-in `default` and `summary` modes.", detailLabel: "Decision", detail: "Use direct C# rendering for markdown generation. This removes runtime template loading, improves compile-time safety, and keeps formatting behavior in one debuggable code path." },
    { title: "NativeAOT with FROM scratch", description: "Ahead-of-time compilation produces static native executables for containers and release assets. The main Docker image is now 2.1 MB while keeping the fastest possible startup time.", detailLabel: "Benefits", detail: "Minimal attack surface, no shell, non-root user (UID 1654), sub-second startup in CI/CD, and compact distribution for both containers and standalone binaries." },
    { title: "Modern C# 13 Patterns", description: "Records for immutable data models, file-scoped namespaces, nullable reference types, pattern matching. Comprehensive XML documentation (including private members) for AI-assisted development.", detailLabel: "Benefits", detail: "Thread-safe, predictable, easier to reason about. Supports 100% AI-assisted development workflow with GitHub Copilot multi-model approach." },
    { title: "CSS Layers for Website Examples", description: "CSS @layer isolates rendered example styles from website page styles. Examples use @layer examples with lower cascade priority than website styles.", detailLabel: "Benefits", detail: "Prevents specificity conflicts. Examples remain readable while website styles always win conflicts. Clean separation between content and presentation layers." },
    { title: "Resource-Specific Renderers with ViewModels", description: "Complex resources (firewall rules, NSG rules, role assignments, variable groups, build definitions) use specialized renderers with ViewModel pattern. C# factories precompute semantic diffs, match items by key, and format before/after comparisons.", detailLabel: "Benefits", detail: "Testable C# logic for complex merging and matching. Renderers iterate preformatted rows and deliver semantic diffs without runtime template limitations." },
    { title: "Single-Pass Report Rendering", description: "Direct renderer dispatch replaces render-then-replace flows. The main report renderer orchestrates headers, summaries, resources, outputs, and provider overrides in one pass.", detailLabel: "Benefits", detail: "No anchors or regex replacement. Explicit, debuggable renderer selection. No wasted computation. Easier testing and clearer ownership of rendering behavior." },
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