# UAT Test Plan: Known-After-Apply Rendering

## Goal

Verify that tfplan2md correctly renders computed (known-after-apply) attributes and summaries in GitHub and Azure DevOps PR comments. Before this feature, all computed attributes were silently dropped from reports. This UAT confirms they now appear with clear `(known after apply)` labels and meaningful configuration-reference context.

---

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the specific known-after-apply rendering changes. This artifact MUST be real `tfplan2md` output generated from the plan below.

**Source Plan Path:** `docs/features/102-known-after-apply-rendering/uat-plan.json`

**Rendered Output Path:** `docs/features/102-known-after-apply-rendering/uat-plan.md`

**Plan Requirements:**

The UAT plan exercises five key scenarios from the specification:

| Resource | Scenario |
|---|---|
| `azuread_group_member.all_unknown` | Scenario 1 — all IDs computed, no config refs → bare `(known after apply)` labels |
| `azuread_group_member.platform_admin_member` | Scenario 2 — static config refs → reference labels in summary and table |
| `azuread_group_member.platform_reader_member` | Scenario 4 — mixed: `group_object_id` computed, `member_object_id` known |
| `azurerm_resource_group.demo` | Scenario 6a — generic resource with computed `id` in `after` |
| `azurerm_storage_account.data` | Scenario 7 — update, sensitive + computed → `🔒(known after apply)` |
| `null_resource.app_config` | Scenario 8 — whole-resource unknown (`after_unknown: true` boolean) |

**Example Generation Command:**
```bash
tfplan2md docs/features/102-known-after-apply-rendering/uat-plan.json \
  > docs/features/102-known-after-apply-rendering/uat-plan.md
```

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other areas.

**Artifact Paths:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

---

## Test Steps

1. Developer generates `uat-plan.md` from `uat-plan.json` using the command above.
2. Code Reviewer validates both `uat-plan.json` and `uat-plan.md` exist and are non-empty before approving.
3. UAT Tester posts two separate PR comments:
   - **Feature-Specific Report** (labeled "🎯 Feature Test"): contents of `uat-plan.md`
   - **Comprehensive Demo** (labeled "🔄 Regression Test"): contents of the appropriate comprehensive demo artifact
4. Maintainer verifies both comments on GitHub and Azure DevOps.

---

## Validation Instructions (Test Description)

Use this section verbatim as the PR description for the UAT PR.

---

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"), verify the following in both GitHub and Azure DevOps PR rendering:

---

#### 1. `azuread_group_member.all_unknown` — All IDs Computed, No Config Block

**What to look for — summary line:**

```
➕ azuread_group_member all_unknown — (known after apply) → (known after apply)
```

The summary line must NOT be blank or show just ` → `. It must show `(known after apply)` for both group and member.

**What to look for — attribute table:**

| Attribute | Value |
| --- | --- |
| `group_object_id` | `` `(known after apply)` `` |
| `id` | `` `(known after apply)` `` |
| `member_object_id` | `` `(known after apply)` `` |

**Before this feature:** the attribute table showed `_No attribute changes._`. The summary line showed only ` — ` (blank).

---

#### 2. `azuread_group_member.platform_admin_member` — Static Config References

**What to look for — summary line:**

```
➕ azuread_group_member platform_admin_member — azuread_group.platform_engineers → azuread_user.admin
```

The summary line shows the Terraform resource reference path (e.g., `azuread_group.platform_engineers`) as a meaningful label instead of `(known after apply)`.

**What to look for — attribute table:**

| Attribute | Value |
| --- | --- |
| `group_object_id` | `` `(known after apply: azuread_group.platform_engineers)` `` |
| `id` | `` `(known after apply)` `` (no config ref available for `id`) |
| `member_object_id` | `` `(known after apply: azuread_user.admin)` `` |

**Before this feature:** attribute table showed `_No attribute changes._`. Summary was blank.

---

#### 3. `azuread_group_member.platform_reader_member` — Mixed Known/Computed

**What to look for — summary line:**

```
➕ azuread_group_member platform_reader_member — azuread_group.platform_engineers → user-201
```

The group side shows the config reference; the member side shows the known concrete value `user-201`.

**What to look for — attribute table:**

| Attribute | Value |
| --- | --- |
| `group_object_id` | `` `(known after apply: azuread_group.platform_engineers)` `` |
| `id` | `` `(known after apply)` `` |
| `member_object_id` | `` `user-201` `` |

---

#### 4. `azurerm_resource_group.demo` — Generic Resource With Computed `id`

**What to look for — attribute table:**

| Attribute | Value |
| --- | --- |
| `id` | `` `(known after apply)` `` |
| `location` | `🌍 eastus` (or similar icon-formatted value) |
| `name` | `🆔 rg-demo` (or similar icon-formatted value) |

The `id` row appears because it is present in `after` as `null`. This demonstrates that the fix is universal — not just for AzureAD types.

**Before this feature:** `id` was silently dropped from the table.

---

#### 5. `azurerm_storage_account.data` — Update With Sensitive + Computed Attribute

**What to look for — attribute table (diff view with Before/After columns):**

| Attribute | Before | After |
| --- | --- | --- |
| `account_replication_type` | `LRS` | `GRS` |
| `primary_access_key` | `(sensitive)` | `🔒(known after apply)` |

**Key checks:**
- The `🔒` lock icon appears in the After column to signal the before value was sensitive.
- The before value `abc123` (or any real secret) MUST NOT appear anywhere in the rendered output.
- The `primary_access_key` row MUST be present (the change must be visible to reviewers).
- The summary line should show `2 🔧 account_replication_type, primary_access_key` (both attributes counted).

**Before this feature:** `primary_access_key` was silently dropped from the diff table. Key rotations were invisible to reviewers.

---

#### 6. `null_resource.app_config` — Whole-Resource Unknown

**What to look for:**

The resource block renders as a create entry with the note `_(all values known after apply)_`. No attribute table rows are present.

**Critical check:** `_No attribute changes._` must NOT appear for this resource. The note `_(all values known after apply)_` must appear instead.

**Before this feature:** `_No attribute changes._` was shown, misleadingly suggesting the resource had no configuration.

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

- No unintended changes to existing resources that do not involve `after_unknown`.
- Resources with fully known `after` values continue to render without any `(known after apply)` rows.
- All sections render correctly (summaries, details, static analysis, tag badges).

---

## Success Criteria

- [ ] `azuread_group_member.all_unknown` summary line shows `(known after apply) → (known after apply)` (not blank)
- [ ] `azuread_group_member.platform_admin_member` summary shows `azuread_group.platform_engineers → azuread_user.admin`
- [ ] `azuread_group_member.platform_admin_member` attribute table shows `(known after apply: azuread_group.platform_engineers)` for `group_object_id`
- [ ] `azuread_group_member.platform_reader_member` shows mixed known/computed correctly
- [ ] `azurerm_resource_group.demo` attribute table includes `id` row with `(known after apply)`
- [ ] `azurerm_storage_account.data` shows `🔒(known after apply)` in After column for `primary_access_key`
- [ ] `azurerm_storage_account.data` Before column for `primary_access_key` shows `(sensitive)`, NOT the actual value
- [ ] `null_resource.app_config` does NOT show `_No attribute changes._`; shows `_(all values known after apply)_` instead
- [ ] All items render correctly in GitHub Markdown
- [ ] All items render correctly in Azure DevOps Markdown
- [ ] Regression: no unintended changes in the comprehensive demo
