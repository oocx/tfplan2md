module.exports = {
  installationMethods: [
    {
      title: "🐳 Docker (Recommended)",
      description: "The easiest way to get started. Docker automatically pulls the image when you run it.",
      codeTitle: "Run directly",
      code: "terraform show -json plan.tfplan | docker run -i oocx/tfplan2md",
      noteLabel: "✅ Benefits",
      noteText: "One command to run, no setup required, works everywhere Docker runs"
    },
    {
      title: "⚙️ From Source",
      description: "Build from source if you need the latest development version or want to contribute.",
      codeTitle: "Clone and build",
      code: "git clone https://github.com/oocx/tfplan2md.git\ncd tfplan2md\ndotnet build",
      noteLabel: "📝 Requirements",
      noteText: ".NET 10 SDK"
    }
  ],
  quickStartSteps: [
    { number: "1", title: "Create a Terraform Plan", code: "terraform plan -out=plan.tfplan" },
    { number: "2", title: "Convert to JSON", code: "terraform show -json plan.tfplan > plan.json" },
    { number: "3", title: "Generate Markdown Report", code: "cat plan.json | docker run -i oocx/tfplan2md > plan.md" }
  ],
  usagePatterns: [
    { title: "Pipe directly from Terraform", code: "terraform show -json plan.tfplan | docker run -i oocx/tfplan2md" },
    { title: "Read from file with mounted volume", code: "docker run -v $(pwd):/data oocx/tfplan2md /data/plan.json" },
      { title: "Write output to file", code: "terraform show -json plan.tfplan | \\\n  docker run -i -v $(pwd):/data oocx/tfplan2md --output /data/plan.md" },
      { title: "Generate summary-only report", code: "terraform show -json plan.tfplan | \\\n  docker run -i oocx/tfplan2md --template summary" }
  ],
  securityIntro: "tfplan2md natively supports SARIF 2.1.0 format, enabling you to integrate security findings from tools like Checkov, TfLint, and Trivy directly into your Terraform plan reports.",
    securityCode: "# Generate plan\nterraform show -json plan.tfplan > plan.json\n\n# Run security scans\ncheckov -d terraform --framework terraform --output sarif -o checkov.sarif\ntflint --format sarif > tflint.sarif\ntrivy config terraform --format sarif --output trivy.sarif\n\n# Generate unified report\ndocker run -v $(pwd):/data -i oocx/tfplan2md \\\n  /data/plan.json \\\n  --code-analysis-results \"/data/*.sarif\" \\\n  --output /data/report.md",
  securityOptions: [
    {
      title: "--code-analysis-results",
      description: "File path or wildcard pattern for SARIF files. Supports wildcards like *.sarif or **/*.sarif for recursive search.",
      example: "--code-analysis-results \"/data/**/*.sarif\""
    },
    {
      title: "--code-analysis-minimum-level",
      description: "Minimum severity level to include in the report. Options: none, note, warning, error. Default is note.",
      example: "--code-analysis-minimum-level warning"
    },
    {
      title: "--fail-on-static-code-analysis-errors",
      description: "Exit with non-zero status if high or critical security findings are detected. Useful for blocking PRs with critical issues.",
      example: "--fail-on-static-code-analysis-errors"
    }
  ],
  securityBenefits: [
    "Security findings mapped to specific resources and attributes",
    "Unified view of infrastructure changes and security issues",
    "Support for multiple security tools in one report",
    "Summary view showing overall security posture"
  ],
  nextSteps: [
    { href: "features/index.html", icon: "🔍", title: "Explore Features", description: "Learn about inline diffs, module grouping, Azure optimizations, and more." },
    { href: "examples.html", icon: "📊", title: "View Examples", description: "See real-world examples of generated Markdown reports with screenshots." },
    { href: "docs.html", icon: "📖", title: "Read Documentation", description: "Dive into CLI options, custom templates, and advanced configuration." }
  ]
};