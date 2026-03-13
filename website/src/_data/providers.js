module.exports = [
  {
    slug: "azurerm",
    title: "Azure (azurerm)",
    icon: "☁️",
    status: "Implemented",
    statusClass: "provider-status-success",
    description: "Comprehensive support for Azure resources with inline diffing, resource ID formatting, and role assignment enhancements.",
    sections: [
      {
        title: "Specialized Resources",
        items: [
          "<strong>Firewall Application Rule Collections</strong> — FQDN and FQDN-tag aware tables for web access rules",
          "<strong>Firewall Network Rule Collections</strong> — Inline diff of rules (added/removed/unchanged)",
          "<strong>Network Security Groups</strong> — Rule-level diffing with priority awareness",
          "<strong>Role Assignments</strong> — Principal name mapping and readable scope display"
        ]
      },
      {
        title: "Global Enhancements",
        items: [
          "<strong>Resource ID Formatting</strong> — Long Azure IDs broken into readable scopes with subscription and resource-group context",
          "<strong>Parent-Child Grouping</strong> — Virtual networks, subnets, DNS records, routes, and NSG rules stay grouped together",
          "<strong>Semantic Icons</strong> — Visual indicators for IPs (🌐), ports (🔌), protocols (📨/🔗), principals (👤/👥/💻)",
          "<strong>Boolean Formatting</strong> — ✅/❌ for true/false values"
        ]
      }
    ],
    actions: [
      {
        href: "azurerm.html",
        label: "View Documentation",
        variant: "secondary"
      },
      {
        href: "../examples.html#firewall-rules",
        label: "See Examples",
        variant: "secondary"
      }
    ]
  },
  {
    slug: "azapi",
    title: "Azure API (azapi)",
    icon: "🧩",
    status: "Implemented",
    statusClass: "provider-status-success",
    description: "Specialized support for Azure API resources with body-aware rendering, output value tables, and Azure REST documentation links.",
    sections: [
      {
        title: "Implemented Resources",
        items: [
          "<strong>azapi_resource</strong> — Structured body rendering with semantic value formatting",
          "<strong>azapi_update_resource</strong> — Focused diffs for PATCH-style updates",
          "<strong>Output Values</strong> — Dedicated table for Azure API response fields separate from input body values"
        ]
      },
      {
        title: "Global Enhancements",
        items: [
          "<strong>Azure API Docs Links</strong> — Microsoft Learn REST API links for supported resource types",
          "<strong>Casing Noise Filter</strong> — Body-level Azure resource ID case-only changes filtered automatically",
          "<strong>Sensitive Handling</strong> — Known-after-apply and sensitive output values rendered safely"
        ]
      }
    ],
    actions: [
      {
        href: "azapi.html",
        label: "View Documentation",
        variant: "secondary",
        fullWidth: true
      }
    ]
  },
  {
    slug: "azuredevops",
    title: "Azure DevOps",
    icon: "🔧",
    status: "Implemented",
    statusClass: "provider-status-success",
    description: "Structured rendering for Azure DevOps variable groups and build definitions, with identity and repository mapping support.",
    sections: [
      {
        title: "Implemented Resources",
        items: [
          "<strong>Variable Groups</strong> — Variable-level diffing with secret value protection",
          "<strong>Build Definitions</strong> — Structured tables for variables, triggers, repository settings, schedules, and jobs"
        ]
      },
      {
        title: "Global Enhancements",
        items: [
          "<strong>Principal Mapping</strong> — Azure DevOps users, groups, and projects resolved to display names",
          "<strong>Repository Mapping</strong> — Repository IDs render with mapped names and 🗃️ / ⎇ icons",
          "<strong>Outputs Support</strong> — Terraform outputs from Azure DevOps plans appear in the report output table"
        ]
      }
    ],
    note: "<strong>Status:</strong> Variable groups and build definitions are implemented and documented.",
    noteClass: "provider-card-note-success",
    actions: [
      {
        href: "azuredevops.html",
        label: "View Documentation",
        variant: "secondary",
        fullWidth: true
      }
    ]
  },
  {
    slug: "azuread",
    title: "Azure AD (azuread)",
    icon: "🔐",
    status: "Implemented",
    statusClass: "provider-status-success",
    description: "Enhanced support for Azure Active Directory resources with specialized rendering for Groups, Service Principals, Invitations, and App Roles.",
    sections: [
      {
        title: "Specialized Resources",
        items: [
          "<strong>Groups</strong> — Member counts plus inline member tables with readable names",
          "<strong>Service Principals</strong> — App roles and OAuth2 permissions",
          "<strong>Invitations</strong> — Guest user invitation details",
          "<strong>Users</strong> — User profiles with attributes"
        ]
      },
      {
        title: "Global Enhancements",
        items: [
          "<strong>Principal Mapping</strong> — Resolves Object IDs to readable names in role assignments",
          "<strong>Group Hierarchies</strong> — Clear display of nested group memberships",
          "<strong>Application Integration</strong> — Links between applications and service principals"
        ]
      }
    ],
    actions: [
      {
        href: "azuread.html",
        label: "View Documentation",
        variant: "secondary",
        fullWidth: true
      }
    ]
  },
  {
    slug: "msgraph",
    title: "Microsoft Graph",
    icon: "📊",
    status: "Planned",
    statusClass: "provider-status-muted",
    description: "Planned support for Microsoft Graph resources including users, groups, and policies.",
    sections: [
      {
        title: "Planned Resources",
        items: [
          "Users and user settings",
          "Groups and group settings",
          "Policies and policy assignments"
        ]
      }
    ],
    note: "<strong>Status:</strong> In planning phase. Contributions welcome!",
    noteClass: "provider-card-note-primary",
    actions: [
      {
        href: "msgraph.html",
        label: "View Documentation",
        variant: "secondary",
        fullWidth: true
      }
    ]
  },
  {
    slug: "your-provider",
    title: "Your Provider",
    icon: "💡",
    status: "We Need You!",
    statusClass: "provider-status-primary",
    description: "Need support for a different Terraform provider? We'd love to hear from you!",
    sections: [
      {
        title: "How to Help",
        items: [
          "<strong>Request a provider</strong> — Open an issue describing which provider you need",
          "<strong>Contribute renderers</strong> — Submit provider-specific rendering improvements for your use case",
          "<strong>Share examples</strong> — Help us understand which resources need specialized rendering"
        ]
      }
    ],
    note: "<strong>Community-driven:</strong> Provider support grows based on user needs. Your input helps prioritize development!",
    noteClass: "provider-card-note-primary",
    actions: [
      {
        href: "https://github.com/oocx/tfplan2md/issues/new",
        label: "Request Provider",
        variant: "primary",
        external: true
      },
      {
        href: "../contributing.html",
        label: "Contribute",
        variant: "secondary"
      }
    ]
  }
];