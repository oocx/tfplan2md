module.exports = {
  navSections: [
    {
      id: "cli-reference",
      title: "CLI Reference",
      children: [
        { id: "basic-usage", title: "Basic Usage" },
        { id: "options", title: "Options" },
        { id: "usage-examples", title: "Usage Examples" }
      ]
    },
    {
      id: "template-customization",
      title: "Templates and Mapping",
      children: [
        { id: "built-in-templates", title: "Built-in Templates" },
        { id: "rendering-engine", title: "Rendering Engine" },
        { id: "principal-mapping", title: "Principal Mapping" }
      ]
    },
    {
      id: "code-analysis",
      title: "Static Code Analysis",
      children: [
        { id: "code-analysis-overview", title: "Overview" },
        { id: "code-analysis-options", title: "CLI Options" },
        { id: "code-analysis-tools", title: "Supported Tools" }
      ]
    },
    {
      id: "azuredevops-integration",
      title: "Azure DevOps Integration",
      children: [
        { id: "pipeline-variable", title: "Pipeline Variable" }
      ]
    },
    {
      id: "render-targets",
      title: "Render Targets",
      children: [
        { id: "azuredevops", title: "azuredevops" },
        { id: "github", title: "github" }
      ]
    },
    {
      id: "troubleshooting",
      title: "Troubleshooting",
      children: [
        { id: "no-valid-json", title: "No valid JSON error" },
        { id: "template-not-found", title: "Template not found" },
        { id: "sensitive-values", title: "Sensitive values visible" },
        { id: "docker-permission", title: "Docker permission denied" }
      ]
    }
  ],

  cliOptions: [
    { name: "-o, --output <file>", description: "Write output to a file instead of stdout." },
    { name: "-t, --template <name>", description: "Use a built-in template by name: `default` or `summary`. Custom template files are no longer supported." },
    { name: "--report-title <title>", description: "Override the report title (level-1 heading) with a custom value." },
    { name: "-p, --principal-mapping <file>", description: "Map Azure principals, subscriptions, tenants, management groups, and Azure DevOps users, groups, projects, and repositories to readable names." },
    { name: "--render-target <platform>", description: "Target platform for rendering: `azuredevops` (default, alias: `azdo`) for styled HTML with line-by-line and character-level diff highlighting, or `github` for traditional diff format with +/- markers. *Note: Replaces deprecated `--large-value-format` flag.*" },
    { name: "--details <auto|open|closed>", description: "Control whether resource details start expanded. `auto` (default) opens resources with findings, `open` expands all, and `closed` collapses all." },
    { name: "--show-unchanged-values", description: "Include unchanged attribute values in tables. By default, only changed attributes are shown." },
    { name: "--code-analysis-results <pattern>", description: "Path or wildcard pattern for SARIF 2.1.0 files containing static analysis results, for example `*.sarif` or `**/*.sarif`. Security findings are mapped to resources and included in the report." },
    { name: "--code-analysis-minimum-level <level>", description: "Minimum severity level to include in the report. Options: `critical`, `high`, `medium`, `low`, `informational`." },
    { name: "--fail-on-static-code-analysis-errors <level>", description: "Exit with code 10 when findings at or above the requested severity exist." },
    { name: "--ignore-azure-id-case-changes", description: "Suppress casing-only Azure resource ID changes. Enabled by default and also applies to AzAPI body properties." },
    { name: "--show-sensitive", description: "Show sensitive values unmasked. By default, sensitive values are masked as `🔒 (sensitive value)`." },
    { name: "--hide-metadata", description: "Suppress tfplan2md version and generation timestamp from the report header." },
    { name: "--debug", description: "Append diagnostic information to the report for troubleshooting. Includes principal mapping diagnostics and renderer selection details." },
    { name: "-h, --help", description: "Display help message." },
    { name: "-v, --version", description: "Display version information." }
  ],

  builtInTemplates: [
    { title: "default", description: "Full report with summary, resource changes grouped by module, output tables, and attribute details. Shows exactly what will change.", usage: "--template default" },
    { title: "summary", description: "Compact overview showing only action counts and resource type breakdown. Perfect for PR titles or large plans.", usage: "--template summary" }
  ],

  codeAnalysisOptions: [
    { name: "--code-analysis-results <pattern>", description: "Path or wildcard pattern for SARIF files. Supports both simple wildcards (`*.sarif`) and recursive patterns (`**/*.sarif`)." },
    { name: "--code-analysis-minimum-level <level>", description: "Minimum severity level to include. Options: `critical`, `high`, `medium`, `low`, `informational`." },
    { name: "--fail-on-static-code-analysis-errors <level>", description: "Exit with code 10 if findings at or above the requested severity are present." }
  ],

  supportedTools: [
    {
      title: "Checkov",
      description: "Infrastructure security scanning with 1000+ built-in policies.",
      command: String.raw`checkov -d terraform \
  --framework terraform \
  --output sarif \
  -o checkov.sarif`
    },
    {
      title: "TfLint",
      description: "Terraform linter with pluggable rules for AWS, Azure, and GCP.",
      command: String.raw`tflint \
  --format sarif \
  > tflint.sarif`
    },
    {
      title: "Trivy",
      description: "Comprehensive security scanner for IaC misconfigurations, vulnerabilities, and secrets.",
      command: String.raw`trivy config terraform \
  --format sarif \
  --output trivy.sarif`
    }
  ],

  renderTargets: [
    {
      id: "azuredevops",
      title: "azuredevops (default)",
      description: "Styled HTML with line-by-line and character-level diff highlighting. Optimized for Azure DevOps PR comments.",
      usage: "--render-target azuredevops or --render-target azdo",
      code: `<pre style="font-family: monospace; line-height: 1.5;"><code>#!/bin/bash
<span style="background-color: #fff5f5; color: #24292e;">echo "v1.0"</span>
<span style="background-color: #f0fff4; color: #24292e;">echo "v2.0"</span>
apt-get update
</code></pre>`
    },
    {
      id: "github",
      title: "github",
      description: "Traditional diff format with +/- markers. Fully portable and works on both GitHub and Azure DevOps.",
      usage: "--render-target github",
      code: "```diff\n  #!/bin/bash\n- echo \"v1.0\"\n+ echo \"v2.0\"\n  apt-get update\n```"
    },
    {
      id: "bitbucket",
      title: "bitbucket",
      description: "Markdown-only output for Bitbucket PR comments. Uses simple diff blocks and rewrites HTML-only sections into plain markdown.",
      usage: "--render-target bitbucket",
      code: "```diff\n  #!/bin/bash\n- echo \"v1.0\"\n+ echo \"v2.0\"\n  apt-get update\n```"
    }
  ],

  quickLinks: [
    { href: "getting-started.html", icon: "🚀", title: "Getting Started", description: "Installation and CI/CD integration guides" },
    { href: "examples.html", icon: "📋", title: "Examples", description: "Real tfplan2md output and usage scenarios" },
    { href: "features/index.html", icon: "✨", title: "Features", description: "Semantic diffing, role assignments, and more" },
    { href: "https://github.com/oocx/tfplan2md", icon: "💻", title: "GitHub Repository", description: "Source code, issues, and contributions", external: true }
  ]
};