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
      title: "Template Customization",
      children: [
        { id: "built-in-templates", title: "Built-in Templates" },
        { id: "custom-templates", title: "Custom Templates" },
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
    { name: "-t, --template <name|file>", description: "Use a built-in template by name (`default`, `summary`) or a custom Scriban template file path." },
    { name: "--report-title <title>", description: "Override the report title (level-1 heading) with a custom value." },
    { name: "-p, --principal-mapping <file>", description: "Map Azure principal IDs (GUIDs) to human-readable names using a JSON file. Applies to `azurerm_role_assignment` resources." },
    { name: "--render-target <platform>", description: "Target platform for rendering: `azuredevops` (default, alias: `azdo`) for styled HTML with line-by-line and character-level diff highlighting, or `github` for traditional diff format with +/- markers. *Note: Replaces deprecated `--large-value-format` flag.*" },
    { name: "--show-unchanged-values", description: "Include unchanged attribute values in tables. By default, only changed attributes are shown." },
    { name: "--code-analysis-results <pattern>", description: "Path or wildcard pattern for SARIF 2.1.0 files containing static analysis results, for example `*.sarif` or `**/*.sarif`. Security findings are mapped to resources and included in the report." },
    { name: "--code-analysis-minimum-level <level>", description: "Minimum severity level to include in the report. Options: `none`, `note`, `warning`, `error`. Default: `note`." },
    { name: "--fail-on-static-code-analysis-errors", description: "Exit with non-zero status code if high or critical severity findings are present. Useful for blocking CI/CD pipelines." },
    { name: "--show-sensitive", description: "Show sensitive values unmasked. By default, sensitive values are masked as `🔒 (sensitive value)`." },
    { name: "--hide-metadata", description: "Suppress tfplan2md version and generation timestamp from the report header." },
    { name: "--debug", description: "Append diagnostic information to the report for troubleshooting. Includes principal mapping diagnostics and template resolution details." },
    { name: "-h, --help", description: "Display help message." },
    { name: "-v, --version", description: "Display version information." }
  ],

  builtInTemplates: [
    { title: "default", description: "Full report with summary, resource changes grouped by module, and attribute details. Shows exactly what will change.", usage: "--template default" },
    { title: "summary", description: "Compact overview showing only action counts and resource type breakdown. Perfect for PR titles or large plans.", usage: "--template summary" }
  ],

  helperFunctions: [
    { title: "inline_diff", description: "Generate inline diffs showing added (+), removed (-), and unchanged items in collections.", example: "{{ inline_diff before.rules after.rules \"name\" }}" },
    { title: "format_azure_id", description: "Format long Azure resource IDs into readable, multi-line scoped paths.", example: "{{ format_azure_id change.after.id }}" },
    { title: "format_bool", description: "Format boolean values as ✅ (true) or ❌ (false) icons.", example: "{{ format_bool change.after.enabled }}" },
    { title: "format_large_value", description: "Format large values (>1000 chars) with inline diff or collapsible details.", example: "{{ format_large_value before.policy after.policy }}" },
    { title: "icon_* functions", description: "Add semantic icons for common value types: `icon_ip`, `icon_port`, `icon_protocol`, `icon_principal`.", example: "{{ icon_ip rule.source_address }}" },
    { title: "get_principal_name", description: "Resolve Azure principal IDs to friendly names (requires mapping file).", example: "{{ get_principal_name change.after.principal_id }}" }
  ],

  codeAnalysisOptions: [
    { name: "--code-analysis-results <pattern>", description: "Path or wildcard pattern for SARIF files. Supports both simple wildcards (`*.sarif`) and recursive patterns (`**/*.sarif`)." },
    { name: "--code-analysis-minimum-level <level>", description: "Minimum severity level to include. Options: `none`, `note`, `warning`, `error`. Default: `note`." },
    { name: "--fail-on-static-code-analysis-errors", description: "Exit with non-zero status if high or critical findings are present." }
  ],

  supportedTools: [
    {
      title: "Checkov",
      description: "Infrastructure security scanning with 1000+ built-in policies.",
      command: String.raw`checkov -d terraform \
  --framework terraform \\
  --output sarif \\
  -o checkov.sarif`
    },
    {
      title: "TfLint",
      description: "Terraform linter with pluggable rules for AWS, Azure, and GCP.",
      command: String.raw`tflint \
  --format sarif \\
  > tflint.sarif`
    },
    {
      title: "Trivy",
      description: "Comprehensive security scanner for IaC misconfigurations, vulnerabilities, and secrets.",
      command: String.raw`trivy config terraform \
  --format sarif \\
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
    }
  ],

  quickLinks: [
    { href: "getting-started.html", icon: "🚀", title: "Getting Started", description: "Installation and CI/CD integration guides" },
    { href: "examples.html", icon: "📋", title: "Examples", description: "Real tfplan2md output and usage scenarios" },
    { href: "features/index.html", icon: "✨", title: "Features", description: "Semantic diffing, role assignments, and more" },
    { href: "https://github.com/oocx/tfplan2md", icon: "💻", title: "GitHub Repository", description: "Source code, issues, and contributions", external: true }
  ]
};