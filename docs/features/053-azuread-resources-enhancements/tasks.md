# Tasks: Enhanced Azure AD Resource Display

## Overview

Improve the markdown output for Azure AD resources by adding semantic icons and informative summaries as defined in [specification.md](specification.md) and [architecture.md](architecture.md).

## Tasks

### Task 1: Infrastructure - Azure AD Provider Module

**Priority:** High

**Description:**
Establish the foundation for Azure AD resource-specific rendering by creating the provider module and registering it in the application.

**Acceptance Criteria:**
- [x] New class `AzureADModule` created in `src/Oocx.TfPlan2Md/Providers/AzureAD/`.
- [x] `AzureADModule` implements `IProviderModule` with `TemplateResourcePrefix = "azuread"`.
- [x] `AzureADModule` is registered in `src/Oocx.TfPlan2Md/Program.cs`.
- [x] Empty template directory `src/Oocx.TfPlan2Md/Providers/AzureAD/Templates/azuread/` created.

**Dependencies:** None

---

### Task 2: Infrastructure - Type Inference and Icon Helpers

**Priority:** High

**Description:**
Add the necessary C# helpers to Scriban to support principal type inference from the mapping file and consistent icon+value formatting.

**Acceptance Criteria:**
- [x] `TryGetPrincipalType` added to Scriban helpers (via `IPrincipalMapper`).
- [x] `FormatIconValue` or similar helper ensures non-breaking space (U+00A0) and `<code>` wrapping.
- [x] Semantic formatting rules added for `user_principal_name` (🆔), `mail` (📧), and `user_email_address` (📧).

**Dependencies:** Task 1

---

### Task 3: Template - azuread_user

**Priority:** Medium

**Description:**
Implement the resource-specific template for `azuread_user`.

**Acceptance Criteria:**
- [x] Summary shows `👤 Display Name`, `🆔 UPN`, and `📧 Email` (if present).
- [x] Summary values are wrapped in `<code>`.
- [x] Table attributes `display_name`, `user_principal_name`, and `mail` use correct icons.

**Dependencies:** Task 2

---

### Task 4: Template - azuread_invitation

**Priority:** Medium

**Description:**
Implement the resource-specific template for `azuread_invitation`.

**Acceptance Criteria:**
- [x] Summary shows `📧 Email` and User Type.
- [x] Table attribute `user_email_address` uses 📧 icon.

**Dependencies:** Task 2

---

### Task 5: Template - azuread_service_principal

**Priority:** Medium

**Description:**
Implement the resource-specific template for `azuread_service_principal`.

**Acceptance Criteria:**
- [x] Summary shows `💻 Display Name`, `🆔 App ID`, and Description (if present).

**Dependencies:** Task 2

---

### Task 6: Template - azuread_group_without_members

**Priority:** Medium

**Description:**
Implement the resource-specific template for `azuread_group_without_members`.

**Acceptance Criteria:**
- [x] Summary shows `👥 Group Name`, `🆔 Display Name`, and Description (if present).
- [x] No member counts are displayed.

**Dependencies:** Task 2

---

### Task 7: Template - azuread_group_member

**Priority:** High

**Description:**
Implement the resource-specific template for `azuread_group_member` with relationship visualization.

**Acceptance Criteria:**
- [x] Summary shows `👥 Group` → `[Icon] Member`.
- [x] Member icon adapts based on inferred type (User, Group, ServicePrincipal).
- [x] Gracefully handles missing `member_object_id`.

**Dependencies:** Task 2

---

### Task 8: Template - azuread_group

**Priority:** High

**Description:**
Implement the resource-specific template for `azuread_group` with member counts.

**Acceptance Criteria:**
- [x] Summary shows `👥 Group Name`, `🆔 Display Name`, Description (if present).
- [x] Member counts displayed as `N 👤 N 👥 N 💻`.
- [x] Unknown member types displayed as `N ❓`.

**Dependencies:** Task 2

---

### Task 9: Cleanup and Integration

**Priority:** Medium

**Description:**
Remove legacy hardcoded Azure AD mappings and verify the new provider takes over.

**Acceptance Criteria:**
- [ ] Hardcoded `azuread_*` entries removed from `ResourceSummaryMappings.cs`.
- [ ] Verify that Azure AD resources now use the new resource-specific templates for summaries.

**Dependencies:** Tasks 3-8

---

### Task 10: Verification and Documentation

**Priority:** Medium

**Description:**
Finalize the feature with tests, UAT artifacts, and documentation updates.

**Acceptance Criteria:**
- [ ] Unit tests for each new template in `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/`.
- [ ] `examples/azuread-resources-demo.json` and `examples/principal-mapping-azuread.json` created.
- [ ] `artifacts/azuread-enhancements-demo.md` generated.
- [ ] Existing snapshot tests updated (`SNAPSHOT_UPDATE_OK`).
- [ ] Documentation updated to reflect Azure AD provider support.

**Dependencies:** Task 9

## Implementation Order

1. **Infrastructure (Tasks 1-2)**: Essential for everything else.
2. **High-Impact Templates (Tasks 7-8)**: Group and membership enhancements are the most complex.
3. **Identity Templates (Tasks 3-6)**: Users, SPNs, and invitations.
4. **Integration and Verification (Tasks 9-10)**: Clean up and final UAT.

## Open Questions

None.
