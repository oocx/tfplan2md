# AzAPI Attribute Grouping - Rendering Options

This document presents multiple rendering approaches for improving how azapi_resource attributes are displayed. Please review and select your preferred option for each problem.

---

## Problem 1: Prefix-Based Attribute Grouping

**Current Behavior:**
When multiple attributes share a common prefix (e.g., `siteConfig.connectionStrings[0].name`, `siteConfig.connectionStrings[0].connectionString`, `siteConfig.connectionStrings[0].type`), they are displayed as flat rows with full paths.

**Goal:**
Group attributes with common prefixes (≥3 attributes) into separate tables where the common prefix is removed.

### Example Data

Using the App Service example from `azapi-complex-demo.md`:

```
connectionStrings[0].name = "Database"
connectionStrings[0].connectionString = "Server=tcp:..."
connectionStrings[0].type = "SQLAzure"
connectionStrings[1].name = "Redis"
connectionStrings[1].connectionString = "myredis..."
connectionStrings[1].type = "RedisCache"
appSettings[0].name = "ASPNETCORE_ENVIRONMENT"
appSettings[0].value = "Production"
appSettings[1].name = "APPLICATIONINSIGHTS_CONNECTION_STRING"
appSettings[1].value = "InstrumentationKey=..."
cors.allowedOrigins[0] = "https://portal.azure.com"
cors.allowedOrigins[1] = "https://myapp.azurewebsites.net"
cors.supportCredentials = true
metadata[0].name = "CURRENT_STACK"
metadata[0].value = "dotnet"
```

---

### Option 1A: Nested Sections with Collapsed Common Prefix

Group attributes with ≥3 shared prefix components into subsections. Remove the common prefix from property names.

**Rendered Output:**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |

**connectionStrings[0]**

| Property | Value |
|----------|-------|
| name | `Database` |
| connectionString | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` |
| type | `SQLAzure` |

**connectionStrings[1]**

| Property | Value |
|----------|-------|
| name | `Redis` |
| connectionString | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` |
| type | `RedisCache` |

**appSettings[0]**

| Property | Value |
|----------|-------|
| name | `ASPNETCORE_ENVIRONMENT` |
| value | `Production` |

**appSettings[1]**

| Property | Value |
|----------|-------|
| name | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| value | `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/` |

**cors**

| Property | Value |
|----------|-------|
| allowedOrigins[0] | `https://portal.azure.com` |
| allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| supportCredentials | `✅ true` |

**metadata[0]**

| Property | Value |
|----------|-------|
| name | `CURRENT_STACK` |
| value | `dotnet` |

**Pros:**
- Clean, flat tables within each group
- Easy to scan individual items
- Clear visual separation between groups

**Cons:**
- Creates many small tables
- Array indices still appear in section headers
- Can feel fragmented for many grouped items

---

### Option 1B: Single Table with Visual Grouping (Bold Headers)

Keep all attributes in one table but add bold separator rows for groups.

**Rendered Output:**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |
| **connectionStrings[0]** | |
| ╰─ name | `Database` |
| ╰─ connectionString | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` |
| ╰─ type | `SQLAzure` |
| **connectionStrings[1]** | |
| ╰─ name | `Redis` |
| ╰─ connectionString | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` |
| ╰─ type | `RedisCache` |
| **appSettings[0]** | |
| ╰─ name | `ASPNETCORE_ENVIRONMENT` |
| ╰─ value | `Production` |
| **appSettings[1]** | |
| ╰─ name | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| ╰─ value | `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/` |
| **cors** | |
| ╰─ allowedOrigins[0] | `https://portal.azure.com` |
| ╰─ allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| ╰─ supportCredentials | `✅ true` |
| **metadata[0]** | |
| ╰─ name | `CURRENT_STACK` |
| ╰─ value | `dotnet` |

**Pros:**
- All data in one scannable table
- Visual hierarchy with tree characters (╰─)
- Compact representation

**Cons:**
- Still shows array indices in group headers
- Tree characters may not render consistently everywhere
- Can be harder to parse visually with many groups

---

### Option 1C: Collapsible Details for Groups

Use `<details>` for each grouped section (≥3 attributes with common prefix).

**Rendered Output:**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |

<details>
<summary><b>connectionStrings</b> (2 items)</summary>

**connectionStrings[0]**

| Property | Value |
|----------|-------|
| name | `Database` |
| connectionString | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` |
| type | `SQLAzure` |

**connectionStrings[1]**

| Property | Value |
|----------|-------|
| name | `Redis` |
| connectionString | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` |
| type | `RedisCache` |

</details>

<details>
<summary><b>appSettings</b> (2 items)</summary>

**appSettings[0]**

| Property | Value |
|----------|-------|
| name | `ASPNETCORE_ENVIRONMENT` |
| value | `Production` |

**appSettings[1]**

| Property | Value |
|----------|-------|
| name | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| value | `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/` |

</details>

<details>
<summary><b>cors</b> (3 properties)</summary>

| Property | Value |
|----------|-------|
| allowedOrigins[0] | `https://portal.azure.com` |
| allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| supportCredentials | `✅ true` |

</details>

<details>
<summary><b>metadata</b> (1 item)</summary>

**metadata[0]**

| Property | Value |
|----------|-------|
| name | `CURRENT_STACK` |
| value | `dotnet` |

</details>

**Pros:**
- Collapsible = less visual clutter by default
- Shows item counts in summary
- Groups arrays separately from nested objects

**Cons:**
- Requires clicking to see details
- More HTML/structure overhead
- May hide important information

---

### Option 1D: Hybrid - Separate Tables for Array Items

When ≥3 attributes share an array index prefix (e.g., `connectionStrings[0].*`), create a dedicated table per array item. Leave non-array grouped properties in main table.

**Rendered Output:**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |
| cors.allowedOrigins[0] | `https://portal.azure.com` |
| cors.allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| cors.supportCredentials | `✅ true` |

###### `connectionStrings` Array

**Item [0]**

| Property | Value |
|----------|-------|
| name | `Database` |
| connectionString | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` |
| type | `SQLAzure` |

**Item [1]**

| Property | Value |
|----------|-------|
| name | `Redis` |
| connectionString | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` |
| type | `RedisCache` |

###### `appSettings` Array

**Item [0]**

| Property | Value |
|----------|-------|
| name | `ASPNETCORE_ENVIRONMENT` |
| value | `Production` |

**Item [1]**

| Property | Value |
|----------|-------|
| name | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| value | `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/` |

###### `metadata` Array

**Item [0]**

| Property | Value |
|----------|-------|
| name | `CURRENT_STACK` |
| value | `dotnet` |

**Pros:**
- Clear distinction between arrays and nested objects
- Clean property names within each item
- Array structure is explicit
- Non-array grouped properties stay visible

**Cons:**
- More sections
- Requires special handling for array vs. object detection

---

## Problem 2: Array-Based Attribute Rendering

**Current Behavior:**
Array items are flattened as `array[0].property`, `array[1].property`, etc., which becomes repetitive and hard to scan.

**Goal:**
Find a better way to display indexed attributes that makes the array structure clear.

### Example Data

```
security[0].protocol = "TCP"
security[0].port = 443
security[0].source = "10.0.0.0/24"
security[1].protocol = "UDP"
security[1].port = 53
security[1].source = "0.0.0.0/0"
security[2].protocol = "ICMP"
security[2].port = null
security[2].source = "192.168.1.0/24"
```

---

### Option 2A: Index-Grouped Subsections

Create a subsection for each array index with properties as table rows.

**Rendered Output:**

**security[0]**

| Property | Value |
|----------|-------|
| protocol | `TCP` |
| port | `443` |
| source | `10.0.0.0/24` |

**security[1]**

| Property | Value |
|----------|-------|
| protocol | `UDP` |
| port | `53` |
| source | `0.0.0.0/0` |

**security[2]**

| Property | Value |
|----------|-------|
| protocol | `ICMP` |
| port | `-` |
| source | `192.168.1.0/24` |

**Pros:**
- Clean separation between array items
- Easy to see structure of each item
- Simple to implement

**Cons:**
- Creates many small tables
- Harder to compare across items (e.g., all protocols)

---

### Option 2B: Transposed Table (Array Items as Columns)

Display array items as columns with properties as rows.

**Rendered Output:**

**security** (3 items)

| Property | [0] | [1] | [2] |
|----------|-----|-----|-----|
| protocol | `TCP` | `UDP` | `ICMP` |
| port | `443` | `53` | `-` |
| source | `10.0.0.0/24` | `0.0.0.0/0` | `192.168.1.0/24` |

**Pros:**
- Compact representation
- Easy to compare values across array items
- Works well for homogeneous arrays (same properties)

**Cons:**
- Wide tables for many array items
- May not fit narrow displays
- Harder to read with many properties per item
- Breaks down with sparse/heterogeneous arrays

---

### Option 2C: Traditional Table (Array Items as Rows)

Display array items as table rows with properties as columns.

**Rendered Output:**

**security** (3 items)

| Index | protocol | port | source |
|-------|----------|------|--------|
| [0] | `TCP` | `443` | `10.0.0.0/24` |
| [1] | `UDP` | `53` | `0.0.0.0/0` |
| [2] | `ICMP` | `-` | `192.168.1.0/24` |

**Pros:**
- Most compact
- Natural table reading (top to bottom)
- Easy to scan items sequentially
- Familiar format

**Cons:**
- Wide tables with many properties per item
- Column headers are property names (data), breaking style guide preference
- Less suitable for nested objects within array items

---

### Option 2D: Collapsible Array Items

Wrap each array item in a `<details>` element.

**Rendered Output:**

**security** (3 items)

<details>
<summary><b>[0]</b> TCP:443 from 10.0.0.0/24</summary>

| Property | Value |
|----------|-------|
| protocol | `TCP` |
| port | `443` |
| source | `10.0.0.0/24` |

</details>

<details>
<summary><b>[1]</b> UDP:53 from 0.0.0.0/0</summary>

| Property | Value |
|----------|-------|
| protocol | `UDP` |
| port | `53` |
| source | `0.0.0.0/0` |

</details>

<details>
<summary><b>[2]</b> ICMP from 192.168.1.0/24</summary>

| Property | Value |
|----------|-------|
| protocol | `ICMP` |
| port | `-` |
| source | `192.168.1.0/24` |

</details>

**Pros:**
- Hides details by default
- Summary line can show key values
- Good for large arrays

**Cons:**
- Requires smart summary generation
- User must click to see full details
- Summary quality depends on property importance detection

---

### Option 2E: Hybrid - Smart Selection Based on Array Size

- **Small arrays (≤3 items):** Use traditional table format (Option 2C)
- **Medium arrays (4-10 items):** Use index-grouped subsections (Option 2A)
- **Large arrays (>10 items):** Use collapsible items (Option 2D)

**Pros:**
- Adapts rendering to data size
- Optimal readability for each scenario
- Small arrays stay compact

**Cons:**
- Inconsistent rendering style
- More complex implementation
- May be confusing if users expect consistency

---

## Recommendations

**For Problem 1 (Prefix Grouping):**
- **Recommended:** Option 1D (Hybrid - Separate Tables for Array Items)
  - Reason: Clear distinction between arrays and objects, clean property names, explicit array structure
  - Falls back gracefully for non-array grouped properties

**For Problem 2 (Array Rendering):**
- **Recommended:** Option 2C (Traditional Table) for arrays with ≤5 items AND ≤4 properties each
  - Reason: Most compact, easy to scan, familiar
- **Fallback:** Option 2A (Index-Grouped Subsections) for larger/complex arrays
  - Reason: Handles any array structure, always readable

**Combined Approach:**
Option 1D handles arrays through its grouping logic, which may naturally solve Problem 2 as well. Consider implementing 1D first and evaluating if Problem 2 still needs a separate solution.

---

---

## Combined Options: How Prefix Grouping + Array Rendering Work Together

**Selected Array Rendering Strategy:**
- **Option 2C** (Traditional Table) for arrays with ≤8 properties per item
- **Option 2A** (Index-Grouped Subsections) for arrays with >8 properties per item

Now let's see how each prefix grouping option would apply this array rendering strategy.

---

### Combined: 1A + (2C/2A)

**Option 1A** creates subsections for each prefix group. Arrays within those groups use 2C or 2A depending on property count.

**Example with ≤8 properties (uses 2C - traditional table):**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |

**connectionStrings**

| Index | name | connectionString | type |
|-------|------|------------------|------|
| [0] | `Database` | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` | `SQLAzure` |
| [1] | `Redis` | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` | `RedisCache` |

**appSettings**

| Index | name | value |
|-------|------|-------|
| [0] | `ASPNETCORE_ENVIRONMENT` | `Production` |
| [1] | `APPLICATIONINSIGHTS_CONNECTION_STRING` | `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/` |

**cors**

| Property | Value |
|----------|-------|
| allowedOrigins[0] | `https://portal.azure.com` |
| allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| supportCredentials | `✅ true` |

**metadata**

| Index | name | value |
|-------|------|-------|
| [0] | `CURRENT_STACK` | `dotnet` |

**Characteristics:**
- Clean sections per prefix group
- Arrays within sections use compact table format (2C)
- Non-array groups (like `cors`) keep dot notation
- Very readable, good visual separation

**Example with >8 properties (uses 2A - subsections):**

**connectionStrings[0]**

| Property | Value |
|----------|-------|
| name | `Database` |
| connectionString | `Server=...` |
| type | `SQLAzure` |
| pooling | `✅ true` |
| minPoolSize | `5` |
| maxPoolSize | `100` |
| connectionTimeout | `30` |
| commandTimeout | `60` |
| multipleActiveResultSets | `✅ true` |

**connectionStrings[1]**

| Property | Value |
|----------|-------|
| name | `Redis` |
| ... | ... |

---

### Combined: 1B + (2C/2A)

**Option 1B** keeps everything in one table with visual grouping. Arrays use 2C/2A inline.

**Rendered Output:**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |
| **connectionStrings** (array) | |

**Inline Array Table:**

| Index | name | connectionString | type |
|-------|------|------------------|------|
| [0] | `Database` | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` | `SQLAzure` |
| [1] | `Redis` | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` | `RedisCache` |

| Property | Value |
|----------|-------|
| **appSettings** (array) | |

**Inline Array Table:**

| Index | name | value |
|-------|------|-------|
| [0] | `ASPNETCORE_ENVIRONMENT` | `Production` |
| [1] | `APPLICATIONINSIGHTS_CONNECTION_STRING` | `InstrumentationKey=...` |

| Property | Value |
|----------|-------|
| **cors** | |
| ╰─ allowedOrigins[0] | `https://portal.azure.com` |
| ╰─ allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| ╰─ supportCredentials | `✅ true` |

**Characteristics:**
- Tries to keep single flow but becomes awkward with inline array tables
- Breaking out of the main table for array tables disrupts the visual grouping benefit
- Less compelling when combined with 2C

---

### Combined: 1C + (2C/2A)

**Option 1C** uses `<details>` for groups. Arrays inside details use 2C/2A.

**Rendered Output:**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |

<details>
<summary><b>connectionStrings</b> (2 items)</summary>

| Index | name | connectionString | type |
|-------|------|------------------|------|
| [0] | `Database` | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` | `SQLAzure` |
| [1] | `Redis` | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` | `RedisCache` |

</details>

<details>
<summary><b>appSettings</b> (2 items)</summary>

| Index | name | value |
|-------|------|-------|
| [0] | `ASPNETCORE_ENVIRONMENT` | `Production` |
| [1] | `APPLICATIONINSIGHTS_CONNECTION_STRING` | `InstrumentationKey=...` |

</details>

<details>
<summary><b>cors</b> (3 properties)</summary>

| Property | Value |
|----------|-------|
| allowedOrigins[0] | `https://portal.azure.com` |
| allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| supportCredentials | `✅ true` |

</details>

<details>
<summary><b>metadata</b> (1 item)</summary>

| Index | name | value |
|-------|------|-------|
| [0] | `CURRENT_STACK` | `dotnet` |

</details>

**Characteristics:**
- Collapsible groups hide complexity by default
- Array tables (2C) render compactly inside collapsed sections
- Good for large bodies with many arrays
- Requires clicking to see details (may hide important info)

---

### Combined: 1D + (2C/2A)

**Option 1D** creates dedicated sections for arrays. Arrays use 2C/2A based on property count.

**Rendered Output:**

###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |
| cors.allowedOrigins[0] | `https://portal.azure.com` |
| cors.allowedOrigins[1] | `https://myapp.azurewebsites.net` |
| cors.supportCredentials | `✅ true` |

###### `connectionStrings` Array

| Index | name | connectionString | type |
|-------|------|------------------|------|
| [0] | `Database` | `Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;` | `SQLAzure` |
| [1] | `Redis` | `myredis.redis.cache.windows.net:6380,password=***,ssl=True` | `RedisCache` |

###### `appSettings` Array

| Index | name | value |
|-------|------|-------|
| [0] | `ASPNETCORE_ENVIRONMENT` | `Production` |
| [1] | `APPLICATIONINSIGHTS_CONNECTION_STRING` | `InstrumentationKey=...` |

###### `metadata` Array

| Index | name | value |
|-------|------|-------|
| [0] | `CURRENT_STACK` | `dotnet` |

**Characteristics:**
- Arrays get dedicated sections with clear headings
- Array tables (2C) render in dedicated, labeled sections
- Non-array properties (cors.*) stay in main table with full paths
- Very clean separation between arrays and other properties
- Natural fit with 2C - arrays are explicitly called out and rendered as traditional tables

---

## Updated Recommendations

Given your preference for **2C (≤8 properties) / 2A (>8 properties)**:

**Best Combined Options:**

1. **Option 1A + (2C/2A)** ⭐ **Recommended**
   - Arrays render as compact tables within their own sections
   - Clean visual separation
   - Natural reading flow
   - Example: "connectionStrings" section contains a traditional table with 3 columns

2. **Option 1D + (2C/2A)** ⭐ **Also Recommended**
   - Arrays get dedicated `###### Array` sections
   - Very explicit about arrays vs. other properties
   - Array tables stand alone with clear context
   - Example: "###### `connectionStrings` Array" with table below

3. **Option 1C + (2C/2A)**
   - Good if you want collapsible groups
   - Hides complexity by default
   - Array tables render inside details (requires clicking)

4. **Option 1B + (2C/2A)**
   - Less compelling combination
   - Mixing single-table flow with inline array tables is awkward

**Key Difference Between 1A and 1D:**

- **1A**: Uses bold headers (`**connectionStrings**`) for array sections
- **1D**: Uses markdown subheadings (`###### \`connectionStrings\` Array`) for arrays, keeps non-arrays in main table

Both work well with 2C. Choose based on:
- **1A** if you want consistent formatting for all groups (arrays and non-arrays)
- **1D** if you want arrays to be MORE prominent than other grouped properties

---

## Next Steps

1. Choose between **1A** or **1D** (both work excellently with 2C/2A)
2. Validate choice against real-world examples
3. Define implementation approach in architecture document
4. Create test cases covering edge cases (empty arrays, deeply nested, mixed types)
