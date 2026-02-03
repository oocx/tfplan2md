# Test Plan: Enhanced Azure AD Resource Display

## Overview

This test plan covers the enhancements for Azure AD (Entra) resource display in the Markdown report as specified in [specification.md](specification.md). The goal is to verify that identity resources render with semantic icons and richer summaries, improving readability for PR reviewers.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `azuread_user` summary & table icons | TC-01 | Unit (Template) |
| `azuread_invitation` summary & table icons | TC-02 | Unit (Template) |
| `azuread_group_member` relationships and icons | TC-03 | Unit (Template) |
| `azuread_group_member` missing object ID | TC-04 | Unit (Template) |
| `azuread_group` summary, description, and counts | TC-05 | Unit (Template) |
| `azuread_group` empty/missing attributes | TC-06 | Unit (Template) |
| `azuread_service_principal` summary & icons | TC-07 | Unit (Template) |
| `azuread_group_without_members` summary & icons | TC-08 | Unit (Template) |
| Global semantic icon formatting (U+00A0, code tags) | TC-09 | Unit |
| Handle missing optional attributes (no placeholders) | TC-06, TC-04 | Unit (Template) |
| Accurate member counts (User, Group, ServicePrincipal, Unknown) | TC-05 | Unit |

## User Acceptance Scenarios

> **Purpose**: Verify Markdown rendering in real-world PR environments (GitHub and Azure DevOps) using the UAT infrastructure.

### Scenario 1: Identity Resource Summary Scan

**User Goal**: Quickly scan identity changes in a PR to understand who is being added or what permissions are being changed.

**Test PR Context**:
- **GitHub**: Verify icons (👤, 🆔, 📧, etc.) render clearly within backticks in the summary line.
- **Azure DevOps**: Verify the same rendering in ADO markdown.

**Expected Output**:
- Summaries show display names with 👤 for users, 👥 for groups, 💻 for service principals.
- Member counts for groups display as `5 👤 1 👥 0 💻`.
- No empty segments or "unknown" placeholders for missing descriptions.

---

### Scenario 2: Principal Relationship Traversal

**User Goal**: Confirm that a group membership change correctly identifies both the group and the member by name using the principal mapping.

**Test PR Context**:
- **GitHub & ADO**: Review `azuread_group_member` resources in the report.

**Expected Output**:
- Displays as: `👥 DevOps Team` (`group-id`) → `👤 Jane Doe` (`user-id`).
- Icons match the principal type.

## Test Cases

### TC-01: azuread_user Rendering

**Type:** Unit (Template)

**Description:**
Verify `azuread_user` summary and table attributes render with correct icons.

**Test Steps:**
1. Provide a plan with `azuread_user` create/update.
2. Verify summary contains `👤 Display Name`, `🆔 UPN`, and `📧 Email`.
3. Verify table rows for `display_name`, `user_principal_name`, and `mail` have icons.

**Test Data:**
`azuread-user-sample.json`

---

### TC-02: azuread_invitation Rendering

**Type:** Unit (Template)

**Description:**
Verify `azuread_invitation` summary and table attributes.

**Expected Result:**
Summary shows `📧 Email` and user type. Table shows `📧` for `user_email_address`.

---

### TC-03: azuread_group_member Rendering

**Type:** Unit (Template)

**Description:**
Verify `azuread_group_member` displays the group-to-member relationship with resolved names and icons.

**Preconditions:**
- Principal mapping file containing IDs for the group and the member.

**Expected Result:**
`👥 Group Name` → `👤 Member Name`.

---

### TC-04: azuread_group_member Error Resistance

**Type:** Unit (Template)

**Description:**
Verify rendering when `member_object_id` is missing or unresolvable.

**Expected Result:**
Graceful degradation: show group name/ID and whatever is available for the member without failing or showing error text.

---

### TC-05: azuread_group Member Counts

**Type:** Unit (Template)

**Description:**
Verify `azuread_group` summary correctly counts and categorizes members from the `members` array.

**Test Data:**
A plan with an `azuread_group` having mixed member types: 3 users, 2 groups, 1 service principal, and 1 unknown ID.

**Expected Result:**
Summary includes: `3 👤 2 👥 1 💻 1 ❓`.

---

### TC-06: Azure AD Optional Attribute Handling

**Type:** Unit (Template)

**Description:**
Verify that optional attributes like `description` or `mail` are omitted from summaries when null/missing (no placeholders).

**Expected Result:**
Summary remains clean; no trailing spaces or empty parentheses.

---

### TC-07: azuread_service_principal Summary

**Type:** Unit (Template)

**Description:**
Verify `azuread_service_principal` summary icons and formatting.

**Expected Result:**
Shows `💻 Display Name`, `🆔 App ID`, and description.

---

### TC-08: azuread_group_without_members Summary

**Type:** Unit (Template)

**Description:**
Verify `azuread_group_without_members` summary.

**Expected Result:**
Shows group name and description with 👥 and 🆔 icons; ensures no member counts are shown (as they aren't available for this resource type).

---

### TC-09: Semantic Icon Formatting (Infrastructure)

**Type:** Unit

**Description:**
Verify the helper responsible for applying icons follows the report style guide:
- Icon followed by non-breaking space (U+00A0).
- Both icon and value wrapped in `<code>`.

**Expected Result:**
HTML segment: `<code>👤&nbsp;Jane Doe</code>`.

## Test Data Requirements

- `azuread-resources-demo.json`: A comprehensive plan containing all in-scope Azure AD resources with varying states (create, update, delete).
- `principal-mapping-azuread.json`: A mapping file to test name/type resolution for groups and members.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| All counts zero | `0 👤 0 👥 0 💻` | TC-06 |
| Unknown principal type | Display as `?` icon count segment | TC-05 |
| Missing mapping | Show raw GUID with ID icon | TC-03 |
| Very long description | Summary remains readable (Markdown wrap handled by browser) | TC-07 |

## Non-Functional Tests

- **Performance**: Ensure principal type inference from mapping file doesn't significantly slow down report generation for large plans.
- **Compatibility**: Verify that icons (which are standard Unicode emojis) render correctly in both GitHub (Web/Mobile) and Azure DevOps (Web).
