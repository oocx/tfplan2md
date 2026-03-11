module.exports = [
  {
    id: "high-impact",
    title: "What Sets Us Apart",
    description: "The features that make the biggest difference in your Terraform reviews",
    compact: false,
    cards: [
      {
        slug: "inline-diffs",
        title: "Inline Diffs",
        icon: "assets/icons/semantic-diffs.svg",
        href: "inline-diffs.html",
        linkLabel: "View examples",
        featured: true,
        description: "Character-level highlighting shows exactly what changed within a value. Added text in green, removed text in red-spot changes instantly."
      },
      {
        slug: "firewall-rules",
        title: "Firewall Rule Interpretation",
        icon: "assets/icons/firewall-rules.svg",
        href: "firewall-rules.html",
        linkLabel: "View examples",
        featured: true,
        description: "Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions clearly displayed."
      },
      {
        slug: "nsg-rules",
        title: "NSG Rule Interpretation",
        icon: "assets/icons/nsg-rules.svg",
        href: "nsg-rules.html",
        linkLabel: "View examples",
        featured: true,
        description: "Renders Network Security Group rules as readable tables, making security changes easy to audit at a glance."
      },
      {
        slug: "role-assignment-mapping",
        title: "Role Assignment Mapping",
        icon: "assets/icons/role-assignment.svg",
        href: "azure-optimizations.html#principal-mapping",
        linkLabel: "View examples",
        featured: true,
        description: "Resolves cryptic GUIDs to human-readable names: Principal IDs become \"Jane Doe\", Role Definition IDs become \"Reader\", and Scope IDs become \"rg-myresourcegroup\". Includes Azure AD Groups, Service Principals, and App Roles."
      },
      {
        slug: "large-values",
        title: "Large Value Formatting",
        icon: "assets/icons/large-values.svg",
        href: "large-values.html",
        linkLabel: "View examples",
        featured: true,
        description: "Handles large text blocks (like JSON policies or scripts) by showing computed diffs with inline highlighting instead of raw text walls."
      },
      {
        slug: "pr-rendering-optimization",
        title: "PR Rendering Optimization",
        icon: "assets/icons/pr-compatibility.svg",
        href: "../examples.html",
        linkLabel: "View examples",
        featured: true,
        description: "Designed and tested for rendering in pull request comments on Azure DevOps Services and GitHub. Reports look great where they matter most."
      },
      {
        slug: "friendly-resource-names",
        title: "Friendly Resource Names",
        icon: "assets/icons/friendly-names.svg",
        href: "misc.html#friendly-names",
        linkLabel: "Learn more",
        featured: true,
        description: "Displays friendly names for resources instead of complex resource ID strings. See \"kv-tfplan2md\" instead of a 200-character Azure resource ID."
      },
      {
        slug: "azdo-variable-groups",
        title: "Azure DevOps Variable Groups",
        icon: "assets/icons/firewall-rules.svg",
        href: "azdo-variable-groups.html",
        linkLabel: "View examples",
        featured: true,
        description: "Shows all variables (regular and secret) in variable group changes with full metadata. Secret values displayed as \"(sensitive / hidden)\" while preserving names and attributes."
      },
      {
        slug: "static-analysis",
        title: "Static Code Analysis Integration",
        icon: "assets/icons/static-analysis.svg",
        href: "static-analysis.html",
        linkLabel: "View examples",
        featured: true,
        description: "Native SARIF 2.1.0 support maps security findings from Checkov, TfLint, and Trivy directly to specific resources and attributes. Creates a unified report combining infrastructure changes with security insights."
      }
    ]
  },
  {
    id: "built-in",
    title: "Built-In Capabilities",
    description: "Solid capabilities that improve readability and usability",
    compact: false,
    sectionClass: "section section-alt",
    sectionStyle: "padding-top: 0; padding-bottom: 60px;",
    cards: [
      {
        slug: "plan-summary",
        title: "Plan Summary",
        icon: "assets/icons/plan-summary.svg",
        href: "misc.html#plan-summary",
        linkLabel: "Learn more",
        description: "High-level overview table showing counts of adds, changes, replaces, and destroys by resource type."
      },
      {
        slug: "module-grouping",
        title: "Module Grouping",
        icon: "assets/icons/module-grouping.svg",
        href: "module-grouping.html",
        linkLabel: "Learn more",
        description: "Groups resources logically by their Terraform module hierarchy (e.g., module.network.module.monitoring)."
      },
      {
        slug: "collapsible-details",
        title: "Collapsible Details",
        icon: "assets/icons/collapsible-details.svg",
        href: "misc.html#collapsible-details",
        linkLabel: "Learn more",
        description: "Hides verbose resource details inside expandable sections to keep PR comments readable and scannable."
      },
      {
        slug: "tag-visualization",
        title: "Tag Visualization",
        icon: "assets/icons/tag-visualization.svg",
        href: "semantic-icons.html#tags",
        linkLabel: "Learn more",
        description: "Renders resource tags with specific icons and formatting for easy scanning of metadata."
      },
      {
        slug: "smart-iconography",
        title: "Smart Iconography",
        icon: "assets/icons/smart-iconography.svg",
        href: "semantic-icons.html",
        linkLabel: "Learn more",
        description: "Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), Ports (🔌), and booleans."
      },
      {
        slug: "custom-templates",
        title: "Custom Templates",
        icon: "assets/icons/custom-templates.svg",
        href: "custom-templates.html",
        linkLabel: "Learn more",
        description: "Allows users to completely customize the markdown output using Scriban templates."
      },
      {
        slug: "cicd-integration",
        title: "CI/CD Integration",
        icon: "assets/icons/cicd-integration.svg",
        href: "../getting-started.html#cicd",
        linkLabel: "View integration guides",
        description: "Native support and examples for GitHub Actions, Azure DevOps, and GitLab CI. Just pipe terraform output to the Docker container."
      },
      {
        slug: "provider-agnostic-core",
        title: "Provider Agnostic Core",
        icon: "assets/icons/provider-agnostic.svg",
        href: "../providers/index.html",
        linkLabel: "View providers",
        description: "Works with any Terraform provider (AWS, GCP, etc.) using standard resource rendering. Azure gets specialized renderers."
      },
      {
        slug: "local-resource-names",
        title: "Local Resource Names",
        icon: "assets/icons/local-names.svg",
        href: "misc.html#local-names",
        linkLabel: "Learn more",
        description: "In modules, displays just the local resource name (e.g., \"hub\") instead of the full module path for cleaner summaries."
      }
    ]
  },
  {
    id: "also-included",
    title: "Also Included",
    description: "Security and quality-of-life improvements",
    compact: true,
    sectionStyle: "padding-top: 0; padding-bottom: 60px;",
    gridClass: "features-grid features-grid-compact",
    cards: [
      {
        slug: "sensitive-masking",
        title: "Sensitive Value Masking",
        icon: "assets/icons/sensitive-masking.svg",
        description: "Automatically masks values marked as sensitive in Terraform to prevent accidental exposure.",
        compact: true
      },
      {
        slug: "container-support",
        title: "Container Support",
        icon: "assets/icons/container-support.svg",
        description: "AOT-compiled native binary in 14.7MB FROM scratch container. Sub-second startup, minimal attack surface.",
        compact: true
      },
      {
        slug: "debug-output",
        title: "Debug Output",
        icon: "assets/icons/smart-iconography.svg",
        description: "Single --debug flag appends diagnostic info showing principal mapping status, template resolution, and failed ID lookups.",
        compact: true
      },
      {
        slug: "dark-light-mode",
        title: "Dark/Light Mode",
        icon: "assets/icons/dark-light-mode.svg",
        description: "Website supports dark and light theme toggle for comfortable viewing in any environment.",
        compact: true
      }
    ]
  }
];