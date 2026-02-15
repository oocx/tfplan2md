# Role Assignment Table Format Options

This document shows different table-based rendering options for Azure role assignments in both CREATE and UPDATE scenarios.

## Example Data

**Create scenario:**
- **Action:** ➕ Create
- **Resource:** `module.security.azurerm_role_assignment.rg_reader`
- **Scope:** Resource group `rg-tfplan2md-demo` in subscription `12345678-1234-1234-1234-123456789012`
- **Role:** Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **Principal:** Jane Doe (User) [00000000-0000-0000-0000-000000000001]

**Update scenario:**
- **Action:** 🔄 Change
- **Resource:** `module.security.azurerm_role_assignment.storage_reader`
- **Changes:**
  - Scope: Storage Account `sttfplan2mdlogs` → `sttfplan2mddata`
  - Role: Storage Blob Data Reader → Storage Blob Data Contributor
  - Principal: DevOps Team (Group) → Security Team (Group)

---

## Current Format (Bullet List - for comparison)

### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader (create)

- **scope**: **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012**
- **role_definition_id**: Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **principal_id**: Jane Doe (User) [00000000-0000-0000-0000-000000000001]
```

#### ➕ module.security.azurerm_role_assignment.rg_reader (create)

- **scope**: **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012**
- **role_definition_id**: Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **principal_id**: Jane Doe (User) [00000000-0000-0000-0000-000000000001]

### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader (update)

- **scope**: Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** → Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo**
- **role_definition_id**: Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) → Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe)
- **principal_id**: DevOps Team (Group) [00000000-0000-0000-0000-000000000002] → Security Team (Group) [00000000-0000-0000-0000-000000000003]
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader (update)

- **scope**: Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** → Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo**
- **role_definition_id**: Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) → Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe)
- **principal_id**: DevOps Team (Group) [00000000-0000-0000-0000-000000000002] → Security Team (Group) [00000000-0000-0000-0000-000000000003]

---

## Option 1: Vertical Key-Value Table with Details Wrapper

**Consistency:** Same format as other CREATE/UPDATE resources  
**Visibility:** Requires click to expand  

### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

## Option 2: Compact Horizontal Table

**Consistency:** Different from other resources  
**Visibility:** Always visible, very compact  

### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

| Scope | Role | Principal |
|-------|------|-----------|
| **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

| Scope | Role | Principal |
|-------|------|-----------|
| **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Scope | Role | Principal |
|-------|------|-----------|
| - Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo**<br>+ Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** | - Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1)<br>+ Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) | - DevOps Team (Group) [00000000-0000-0000-0000-000000000002]<br>+ Security Team (Group) [00000000-0000-0000-0000-000000000003] |
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Scope | Role | Principal |
|-------|------|-----------|
| - Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo**<br>+ Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** | - Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1)<br>+ Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) | - DevOps Team (Group) [00000000-0000-0000-0000-000000000002]<br>+ Security Team (Group) [00000000-0000-0000-0000-000000000003] |

---

## Option 3: Split Table with Friendly Headers

**Consistency:** Similar to other resources but without `details` wrapper  
**Visibility:** Always visible  

### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

| Property | Details |
|----------|---------|
| **Scope** | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| **Role** | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| **Principal** | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

| Property | Details |
|----------|---------|
| **Scope** | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| **Role** | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| **Principal** | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Property | Before | After |
|----------|--------|-------|
| **Scope** | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| **Role** | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| **Principal** | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Property | Before | After |
|----------|--------|-------|
| **Scope** | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| **Role** | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| **Principal** | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

---

## Option 4: Hybrid - Inline Summary + Details Table

**Consistency:** Extends standard format with summary line  
**Visibility:** Summary always visible, details collapsible  

### Styling Options

Below are different styling options for resource names and important values. Choose the one that feels most readable to you.

#### Style A: Bold (Current - but you find it too strong)

##### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on **rg-tfplan2md-demo**

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on **rg-tfplan2md-demo**

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

##### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account **sttfplan2mddata**

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account **sttfplan2mddata**

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

#### Style B: Italic

##### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on *rg-tfplan2md-demo*

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | *rg-tfplan2md-demo* in subscription *12345678-1234-1234-1234-123456789012* |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on *rg-tfplan2md-demo*

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | *rg-tfplan2md-demo* in subscription *12345678-1234-1234-1234-123456789012* |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

##### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account *sttfplan2mddata*

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account *sttfplan2mdlogs* in resource group *rg-tfplan2md-demo* of subscription *12345678-1234-1234-1234-123456789012* | Storage Account *sttfplan2mddata* in resource group *rg-tfplan2md-demo* of subscription *12345678-1234-1234-1234-123456789012* |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account *sttfplan2mddata*

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account *sttfplan2mdlogs* in resource group *rg-tfplan2md-demo* of subscription *12345678-1234-1234-1234-123456789012* | Storage Account *sttfplan2mddata* in resource group *rg-tfplan2md-demo* of subscription *12345678-1234-1234-1234-123456789012* |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

#### Style C: Code Backticks

##### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on `rg-tfplan2md-demo`

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | `rg-tfplan2md-demo` in subscription `12345678-1234-1234-1234-123456789012` |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on `rg-tfplan2md-demo`

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | `rg-tfplan2md-demo` in subscription `12345678-1234-1234-1234-123456789012` |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

##### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account `sttfplan2mddata`

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account `sttfplan2mdlogs` in resource group `rg-tfplan2md-demo` of subscription `12345678-1234-1234-1234-123456789012` | Storage Account `sttfplan2mddata` in resource group `rg-tfplan2md-demo` of subscription `12345678-1234-1234-1234-123456789012` |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account `sttfplan2mddata`

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account `sttfplan2mdlogs` in resource group `rg-tfplan2md-demo` of subscription `12345678-1234-1234-1234-123456789012` | Storage Account `sttfplan2mddata` in resource group `rg-tfplan2md-demo` of subscription `12345678-1234-1234-1234-123456789012` |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

#### Style D: Bold Italic (Combination)

##### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on ***rg-tfplan2md-demo***

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | ***rg-tfplan2md-demo*** in subscription ***12345678-1234-1234-1234-123456789012*** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on ***rg-tfplan2md-demo***

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | ***rg-tfplan2md-demo*** in subscription ***12345678-1234-1234-1234-123456789012*** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

##### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account ***sttfplan2mddata***

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account ***sttfplan2mdlogs*** in resource group ***rg-tfplan2md-demo*** of subscription ***12345678-1234-1234-1234-123456789012*** | Storage Account ***sttfplan2mddata*** in resource group ***rg-tfplan2md-demo*** of subscription ***12345678-1234-1234-1234-123456789012*** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account ***sttfplan2mddata***

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account ***sttfplan2mdlogs*** in resource group ***rg-tfplan2md-demo*** of subscription ***12345678-1234-1234-1234-123456789012*** | Storage Account ***sttfplan2mddata*** in resource group ***rg-tfplan2md-demo*** of subscription ***12345678-1234-1234-1234-123456789012*** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

#### Style E: No Special Formatting (Plain Text)

##### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on rg-tfplan2md-demo

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on rg-tfplan2md-demo

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

##### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account sttfplan2mddata

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account sttfplan2mdlogs in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 | Storage Account sttfplan2mddata in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account sttfplan2mddata

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account sttfplan2mdlogs in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 | Storage Account sttfplan2mddata in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

#### Style F: Mixed - Bold for Resource Names, Plain for IDs

##### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on **rg-tfplan2md-demo**

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** Jane Doe (User) → Reader on **rg-tfplan2md-demo**

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

##### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account **sttfplan2mddata**

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription 12345678-1234-1234-1234-123456789012 | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** Security Team (Group) → Storage Blob Data Contributor on Storage Account **sttfplan2mddata**

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription 12345678-1234-1234-1234-123456789012 | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

#### Style G: Backticks in Summary, Plain in Details (✅ Your Preference)

##### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** `Jane Doe` (User) → `Reader` on `rg-tfplan2md-demo`

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** `Jane Doe` (User) → `Reader` on `rg-tfplan2md-demo`

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>

##### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** `Security Team` (Group) → `Storage Blob Data Contributor` on Storage Account `sttfplan2mddata`

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account sttfplan2mdlogs in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 | Storage Account sttfplan2mddata in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** `Security Team` (Group) → `Storage Blob Data Contributor` on Storage Account `sttfplan2mddata`

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account sttfplan2mdlogs in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 | Storage Account sttfplan2mddata in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

</details>

---

## Option 5: Vertical Table Without Details Wrapper

**Consistency:** Similar structure to other resources, but always expanded  
**Visibility:** Always visible  

### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** of subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |

---

## Option 6: Semantic Table with Action Context

**Consistency:** Very different - uses natural language headers  
**Visibility:** Always visible  

### Create
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

| Who | Gets Role | Where |
|-----|-----------|-------|
| Jane Doe (User)<br/>[00000000-0000-0000-0000-000000000001] | Reader<br/>(acdd72a7-3385-48ef-bd42-f606fba81ae7) | **rg-tfplan2md-demo** in subscription<br/>**12345678-1234-1234-1234-123456789012** |
```

#### ➕ module.security.azurerm_role_assignment.rg_reader

| Who | Gets Role | Where |
|-----|-----------|-------|
| Jane Doe (User)<br/>[00000000-0000-0000-0000-000000000001] | Reader<br/>(acdd72a7-3385-48ef-bd42-f606fba81ae7) | **rg-tfplan2md-demo** in subscription<br/>**12345678-1234-1234-1234-123456789012** |

### Update
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Who | Gets Role | Where |
|-----|-----------|-------|
| - DevOps Team (Group) [00000000-0000-0000-0000-000000000002]<br>+ Security Team (Group) [00000000-0000-0000-0000-000000000003] | - Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1)<br>+ Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) | - Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo**<br>+ Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** |
```

#### 🔄 module.security.azurerm_role_assignment.storage_reader

| Who | Gets Role | Where |
|-----|-----------|-------|
| - DevOps Team (Group) [00000000-0000-0000-0000-000000000002]<br>+ Security Team (Group) [00000000-0000-0000-0000-000000000003] | - Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1)<br>+ Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) | - Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo**<br>+ Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** |

---

## Recommendation Summary

### Most Consistent with Existing Patterns
**Option 1** - Uses the same structure as all other resources (vertical table with details wrapper)

### Best Balance
**Option 3** - Always visible, clean headers, similar structure to other resources but without the collapsible wrapper (role assignments are typically important to see)

### Most Compact
**Option 2** - Horizontal layout, all info in one row

### Most Informative
**Option 4** - Provides both quick summary and detailed view

### Always Visible
**Options 2, 3, 5, 6** - No clicking required

### Most Readable
**Option 6** - Natural language headers make it easy to understand at a glance
