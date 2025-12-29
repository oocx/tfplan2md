# Formatting Examples: Before and After

This document shows concrete examples of how the formatting changes will look.

## 1. Attribute Change Tables

### CREATE Action

**Before (current):**

| Attribute | Value |
|-----------|-------|
| `location` | eastus |
| `name` | rg-tfplan2md-demo |
| `account_tier` | Standard |
| `account_replication_type` | LRS |

**After (new):**

| Attribute | Value |
|-----------|-------|
| location | `eastus` |
| name | `rg-tfplan2md-demo` |
| account_tier | `Standard` |
| account_replication_type | `LRS` |

### UPDATE Action

**Before (current):**

| Attribute | Before | After |
|-----------|--------|-------|
| `account_replication_type` | LRS | GRS |
| `account_tier` | Standard | Standard |
| `location` | eastus | eastus |
| `name` | sttfplan2mddata | sttfplan2mddata |

**After (new):**

| Attribute | Before | After |
|-----------|--------|-------|
| account_replication_type | `LRS` | `GRS` |
| account_tier | `Standard` | `Standard` |
| location | `eastus` | `eastus` |
| name | `sttfplan2mddata` | `sttfplan2mddata` |

### DELETE Action

**Before (current):**

| Attribute | Value |
|-----------|-------|
| `account_replication_type` | LRS |
| `account_tier` | Standard |
| `location` | eastus |
| `name` | sttfplan2mdlegacy |

**After (new):**

| Attribute | Value |
|-----------|-------|
| account_replication_type | `LRS` |
| account_tier | `Standard` |
| location | `eastus` |
| name | `sttfplan2mdlegacy` |

---

## 2. Firewall Network Rule Collections

### Collection Header

**Before (current):**

**Collection:** `public-egress` | **Priority:** 110 | **Action:** Allow

**After (new):**

**Collection:** `public-egress` | **Priority:** `110` | **Action:** `Allow`

### Rule Tables - CREATE

**Before (current):**

#### Rules

| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
|-----------|-----------|------------------|----------------------|-------------------|-------------|
| allow-http | TCP | 10.1.1.0/24 | * | 80 | Allow outbound HTTP |
| allow-https | TCP | 10.1.1.0/24 | * | 443 | Allow outbound HTTPS |

**After (new):**

#### Rules

| Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
|-----------|-----------|------------------|----------------------|-------------------|-------------|
| `allow-http` | `TCP` | `10.1.1.0/24` | `*` | `80` | `Allow outbound HTTP` |
| `allow-https` | `TCP` | `10.1.1.0/24` | `*` | `443` | `Allow outbound HTTPS` |

### Rule Tables - Modified Rules (with enhanced diff)

**Before (current):**

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
|--------|-----------|-----------|------------------|----------------------|-------------------|-------------|
| 🔄 | allow-dns | UDP | - 10.1.1.0/24<br>+ 10.1.1.0/24, 10.1.2.0/24 | 168.63.129.16 | 53 | DNS to Azure |
| 🔄 | allow-api | TCP | 10.1.1.0/24 | - 10.1.2.0/24<br>+ 10.2.2.0/24 | - 8443<br>+ 8443, 9443 | API tier |

**After (new) - Standard Diff Format:**

> Note: Triple backticks shown as ` ``` ` below - these create code fences in the actual output

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
|--------|-----------|-----------|------------------|----------------------|-------------------|-------------|
| 🔄 | `allow-dns` | `UDP` | ` ```diff`<br>`- 10.1.1.0/24`<br>`+ 10.1.1.0/24, 10.1.2.0/24`<br>` ``` ` | `168.63.129.16` | `53` | `DNS to Azure` |
| 🔄 | `allow-api` | `TCP` | `10.1.1.0/24` | ` ```diff`<br>`- 10.1.2.0/24`<br>`+ 10.2.2.0/24`<br>` ``` ` | ` ```diff`<br>`- 8443`<br>`+ 8443, 9443`<br>` ``` ` | `API tier` |

**After (new) - Inline Diff Format (HTML with character-level highlighting):**

#### Rule Changes

| Change | Rule Name | Protocols | Source Addresses | Destination Addresses | Destination Ports | Description |
|--------|-----------|-----------|------------------|----------------------|-------------------|-------------|
| 🔄 | `allow-dns` | `UDP` | <pre style="font-family: monospace;"><code><span style="background-color: #ffe0e0;">10.1.1.0/24</span><br><span style="background-color: #e0ffe0;">10.1.1.0/24, 10.1.2.0/24</span></code></pre> | `168.63.129.16` | `53` | `DNS to Azure` |
| 🔄 | `allow-api` | `TCP` | `10.1.1.0/24` | <pre style="font-family: monospace;"><code><span style="background-color: #ffe0e0;">10.1.2.0/24</span><br><span style="background-color: #e0ffe0;">10.2.2.0/24</span></code></pre> | <pre style="font-family: monospace;"><code><span style="background-color: #ffe0e0;">8443</span><br><span style="background-color: #e0ffe0;">8443, 9443</span></code></pre> | `API tier` |

---

## 3. Network Security Group Rules

### NSG Header

**Before (current):**

**Network Security Group:** `nsg-app`

**After (new):**

**Network Security Group:** nsg-app

### Security Rules - CREATE

**Before (current):**

#### Security Rules

| Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
|------|----------|-----------|--------|----------|------------------|--------------|----------------------|-------------------|-------------|
| allow-https | 100 | Inbound | Allow | Tcp | * | * | * | 443 | HTTPS inbound |

**After (new):**

#### Security Rules

| Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
|------|----------|-----------|--------|----------|------------------|--------------|----------------------|-------------------|-------------|
| `allow-https` | `100` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `*` | `443` | `HTTPS inbound` |

### Security Rules - Modified Rules (with enhanced diff)

**Before (current):**

| Change | Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
|--------|------|----------|-----------|--------|----------|------------------|--------------|----------------------|-------------------|-------------|
| 🔄 | allow-web | 100 | Inbound | Allow | Tcp | * | * | * | - 80<br>+ 443 | - HTTP<br>+ HTTPS |

**After (new) - Standard Diff Format:**

| Change | Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
|--------|------|----------|-----------|--------|----------|------------------|--------------|----------------------|-------------------|-------------|
| 🔄 | `allow-web` | `100` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `*` | ` ```diff`<br>`- 80`<br>`+ 443`<br>` ``` ` | ` ```diff`<br>`- HTTP`<br>`+ HTTPS`<br>` ``` ` |

**After (new) - Inline Diff Format:**

| Change | Name | Priority | Direction | Access | Protocol | Source Addresses | Source Ports | Destination Addresses | Destination Ports | Description |
|--------|------|----------|-----------|--------|----------|------------------|--------------|----------------------|-------------------|-------------|
| 🔄 | `allow-web` | `100` | `Inbound` | `Allow` | `Tcp` | `*` | `*` | `*` | <pre style="font-family: monospace;"><code><span style="background-color: #ffe0e0;">80</span><br><span style="background-color: #e0ffe0;">443</span></code></pre> | <pre style="font-family: monospace;"><code><span style="background-color: #ffe0e0;">HTTP</span><br><span style="background-color: #e0ffe0;">HTTPS</span></code></pre> |

---

## 4. Role Assignments

### CREATE

**Before (current):**

| Attribute | Value |
|-----------|-------|
| `scope` | **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012** |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

**After (new):**

| Attribute | Value |
|-----------|-------|
| scope | **`rg-tfplan2md-demo`** in subscription **`12345678-1234-1234-1234-123456789012`** |
| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |
| principal_id | `Jane Doe` (`User`) [`00000000-0000-0000-0000-000000000001`] |

### UPDATE

**Before (current):**

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account **sttfplan2mdlogs** in resource group **rg-tfplan2md-demo** | Storage Account **sttfplan2mddata** in resource group **rg-tfplan2md-demo** |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-...) | Storage Blob Data Contributor (ba92f5b4-...) |

**After (new):**

| Attribute | Before | After |
|-----------|--------|-------|
| scope | Storage Account **`sttfplan2mdlogs`** in resource group **`rg-tfplan2md-demo`** | Storage Account **`sttfplan2mddata`** in resource group **`rg-tfplan2md-demo`** |
| role_definition_id | `Storage Blob Data Reader` (`2a2b9908-...`) | `Storage Blob Data Contributor` (`ba92f5b4-...`) |

---

## 5. No Changes to Resource Summaries

Resource summaries already have appropriate formatting and remain unchanged:

**Current (stays the same):**

**Summary:** `sttfplan2mdlogs` in `rg-tfplan2md-demo` (eastus) | Standard LRS

**Summary:** `vnet-spoke` | Changed: address_space[1]

**Summary:** recreate `snet-db` (address_prefixes[0] changed: force replacement)

---

## Visual Comparison

### Current State Issues
- ❌ Attribute names are emphasized (backticks) even though they don't change
- ❌ Values are plain text even though they're the actual data
- ❌ Inconsistent: some places format names, others format values
- ❌ Diffs are basic text with `-`/`+`, no styling

### New State Benefits
- ✅ Values are emphasized (backticks) because they're what changes
- ✅ Attribute names are plain because they're just labels
- ✅ Consistent: all data values are code-formatted everywhere
- ✅ Diffs have rich styling with character-level highlighting
