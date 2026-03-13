module.exports = {
  installationMethods: [
    {
      title: "🍺 Homebrew",
      description: "Fastest setup on macOS and Linux if you already use Homebrew.",
      codeTitle: "Install and upgrade",
      code: "brew tap oocx/tfplan2md\nbrew install tfplan2md\n# Later: brew upgrade tfplan2md",
      noteLabel: "✅ Best for",
      noteText: "macOS and Linux workstations, repeat installs, and package-manager based upgrades"
    },
    {
      title: "🐳 Docker",
      description: "The easiest way to get started in CI/CD or container-friendly environments. Docker automatically pulls the image when you run it.",
      codeTitle: "Run directly",
      code: "terraform show -json plan.tfplan | \\\ndocker run -i oocx/tfplan2md",
      noteLabel: "✅ Benefits",
      noteText: "One command to run, no setup required, works everywhere Docker runs"
    },
    {
      title: "📦 Pre-built Binaries",
      description: "Download a self-contained NativeAOT binary from GitHub Releases when Docker or Homebrew is not available.",
      codeTitle: "Download and run",
      code: "VERSION=\"1.x.x\"\nPLATFORM=\"linux-x64\"\nwget https://github.com/oocx/tfplan2md/releases/download/v${VERSION}/tfplan2md_${VERSION}_${PLATFORM}.tar.gz\ntar -xzf tfplan2md_${VERSION}_${PLATFORM}.tar.gz\n./tfplan2md --help",
      noteLabel: "📝 Platforms",
      noteText: "Linux x64, Linux ARM64, Windows x64, macOS ARM64, plus musl targets for Alpine-focused deployments"
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
    { title: "Run the native binary", code: "terraform show -json plan.tfplan | ./tfplan2md --details auto > plan.md" },
    { title: "Read from file with mounted volume", code: "docker run -v $(pwd):/data oocx/tfplan2md /data/plan.json" },
    { title: "Write output to file", code: "terraform show -json plan.tfplan | \\\n+  docker run -i -v $(pwd):/data oocx/tfplan2md --output /data/plan.md" },
    { title: "Generate summary-only report", code: "terraform show -json plan.tfplan | \\\n+  docker run -i oocx/tfplan2md --template summary" }
  ],
  securityIntro: "tfplan2md natively supports SARIF 2.1.0 format, enabling you to integrate security findings from tools like Checkov, TfLint, and Trivy directly into your Terraform plan reports.",
  securityCode: "# Generate plan\nterraform show -json plan.tfplan > plan.json\n\n# Run security scans\ncheckov -d terraform --framework terraform --output sarif -o checkov.sarif\ntflint --format sarif > tflint.sarif\ntrivy config terraform --format sarif --output trivy.sarif\n\n# Generate unified report\ndocker run -v $(pwd):/data -i oocx/tfplan2md \\\n+  /data/plan.json \\\n+  --code-analysis-results \"/data/*.sarif\" \\\n+  --output /data/report.md",
  securityOptions: [
    {
      title: "--code-analysis-results",
      description: "File path or wildcard pattern for SARIF files. Supports wildcards like *.sarif or **/*.sarif for recursive search.",
      example: "--code-analysis-results \"/data/**/*.sarif\""
    },
    {
      title: "--code-analysis-minimum-level",
      description: "Minimum severity level to include in the report. Options: critical, high, medium, low, informational.",
      example: "--code-analysis-minimum-level high"
    },
    {
      title: "--fail-on-static-code-analysis-errors <level>",
      description: "Exit with code 10 when findings at or above the requested severity are detected. Useful for blocking PRs with critical issues.",
      example: "--fail-on-static-code-analysis-errors high"
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
    { href: "docs.html", icon: "📖", title: "Read Documentation", description: "Dive into CLI options, built-in templates, mappings, and advanced configuration." }
  ]
};