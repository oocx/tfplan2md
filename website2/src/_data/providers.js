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
          "<strong>Firewall Network Rule Collections</strong> — Inline diff of rules (added/removed/unchanged)",
          "<strong>Network Security Groups</strong> — Rule-level diffing with priority awareness",
          "<strong>Role Assignments</strong> — Principal name mapping and readable scope display"
        ]
      },
      {
        title: "Global Enhancements",
        items: [
          "<strong>Resource ID Formatting</strong> — Long Azure IDs broken into readable scopes",
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
    slug: "azuredevops",
    title: "Azure DevOps",
    icon: "🔧",
    status: "Partial Support",
    statusClass: "provider-status-success",
    description: "Specialized support for Azure DevOps variable groups with semantic diffing and secret protection.",
    sections: [
      {
        title: "Implemented Resources",
        items: [
          "<strong>Variable Groups</strong> — Variable-level diffing with secret value protection"
        ]
      },
      {
        title: "Planned Resources",
        items: [
          "Projects and project settings",
          "Build and release pipelines",
          "Service connections"
        ]
      }
    ],
    note: "<strong>Status:</strong> Variable groups implemented. Additional resources in planning phase.",
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
          "<strong>Groups</strong> — Member counts, group memberships with readable names",
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
          "<strong>Contribute templates</strong> — Submit provider-specific templates for your use case",
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