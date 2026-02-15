# Feature: Enhanced Azure AD Resource Display

## Overview

Improve the markdown output for Azure AD resources by adding semantic icons and more informative summaries. This makes Azure AD infrastructure changes easier to scan and understand in pull request reviews.

## User Goals

- Quickly identify Azure AD users, groups, and service principals in Terraform plan reports
- See key identity attributes (email addresses, principal names, member relationships) at a glance without expanding details
- Understand group membership composition through visual member counts
- Leverage consistent icon patterns across all Azure AD resource types

## Scope

### In Scope

Enhanced display for the following Azure AD resources:

1. **azuread_user**
   - Summary shows: display name, user principal name (UPN), and email address with icons
   - Table attributes use semantic icons for name, UPN, and email fields

2. **azuread_invitation**
   - Summary shows: email address and user type with icons
   - Table attributes use semantic icons for email field

3. **azuread_group_member**
   - Summary shows: group name → member name relationship with type-specific icons
   - Icons adapt based on member type (user, group, or service principal)
   - Gracefully handles missing `member_object_id` attribute

4. **azuread_group**
   - Summary shows: group name, optional description, and member count by type
   - Member counts display as: `N 👤 N 👥 N 💻` (users, groups, service principals)

5. **azuread_service_principal**
   - Summary shows: display name, application ID, and optional description with icons

6. **azuread_group_without_members**
   - Summary shows: group name and optional description with icons (no member counts)

### Out of Scope

- Other Azure AD resources not listed above (may be addressed in future work)
- Changes to principal mapping file format or behavior
- Integration with external Azure AD APIs to fetch additional metadata
- Custom templates beyond summary and table icon enhancements

## User Experience

### Summary Line Examples

**azuread_user:**
```markdown
➕ azuread_user <b><code>jane</code></b> — `👤 Jane Doe` (`🆔 jane.doe@example.com`) `📧 jane.doe@example.com`
```

**azuread_invitation:**
```markdown
➕ azuread_invitation <b><code>external_user</code></b> — `📧 contractor@external.com` (`Guest`)
```

**azuread_group_member:**
```markdown
➕ azuread_group_member <b><code>devops_jane</code></b> — `👥 DevOps Team` (`group-abc-123`) → `👤 Jane Doe` (`member-def-456`)
```

**azuread_group:**
```markdown
➕ azuread_group <b><code>platform_team</code></b> — `👥 Platform Team` (`🆔 Platform Engineering`) `Core platform engineering team` | `5 👤 1 👥 0 💻`
```

**azuread_service_principal:**
```markdown
➕ azuread_service_principal <b><code>terraform_spn</code></b> — `💻 terraform-spn` (`🆔 app-123-456`) `Terraform automation service principal`
```

**azuread_group_without_members:**
```markdown
➕ azuread_group_without_members <b><code>external_group</code></b> — `👥 External Partners` (`🆔 External Partners Group`)
```

### Table Icon Examples

When attributes appear in resource detail tables, icons are applied consistently:

| Attribute | Value |
|-----------|-------|
| display_name | `👤 Jane Doe` |
| user_principal_name | `🆔 jane.doe@example.com` |
| mail | `📧 jane.doe@example.com` |

### Icon Mapping

| Icon | Meaning | Used For |
|------|---------|----------|
| 👤 | User | User principals, user display names |
| 👥 | Group | Group principals, group display names |
| 💻 | Service Principal | Service principal display names |
| 🆔 | Identifier | User principal names, application IDs, group names |
| 📧 | Email | Email addresses (mail, user_email_address) |

### Edge Cases

1. **Missing optional attributes:**
   - If `description` is missing, omit it from summary (no placeholder text)
   - If `mail` is missing from azuread_user, omit the email portion from summary
   - If `member_object_id` is missing from azuread_group_member, show only what's available

2. **Empty member counts:**
   - If azuread_group has zero members, show: `0 👤 0 👥 0 💻`

3. **Principal mapping resolution:**
   - If principal mapping provides a display name, use it with appropriate icon
   - If no mapping exists, show the GUID without decoration

## Success Criteria

- [ ] azuread_user summary displays display_name, user_principal_name, and mail with correct icons
- [ ] azuread_user table attributes show icons for display_name (👤), user_principal_name (🆔), and mail (📧)
- [ ] azuread_invitation summary displays user_email_address and user_type with correct formatting
- [ ] azuread_invitation table shows 📧 icon for user_email_address
- [ ] azuread_group_member summary displays group → member relationship with type-appropriate icons
- [ ] azuread_group_member gracefully handles missing member_object_id without errors
- [ ] azuread_group summary shows group name, description (if present), and member counts by type
- [ ] azuread_group member counts are accurate for each principal type (user, group, service principal)
- [ ] azuread_service_principal summary shows display name, application ID, and description (if present)
- [ ] azuread_group_without_members summary shows group name and description (if present) without member counts
- [ ] All icon usage follows the report style guide patterns (inside code formatting for data values)
- [ ] Missing optional attributes (description, mail) are handled gracefully without error messages or placeholders
- [ ] Existing snapshot tests are updated to reflect the new icon-enhanced output
- [ ] Documentation is updated to reflect the new Azure AD resource display patterns

## Open Questions

None - all requirements are clearly defined.
