# AzAPI array rendering improvements

This patch improves the clarity and accuracy of markdown reports for AzAPI resources with nested array structures. Two key fixes: (1) array tables now show only changed items instead of all items, and (2) array diffs render correctly for new/removed elements with proper coloring in Azure DevOps.

## 🐛 Bug fixes

### 1. AzAPI array rendering now filters to show only changed items

**Problem:** When an AzAPI resource had a nested array structure (for example, an Azure Policy Definition's `body.properties.policyRule.if.allOf` array), and only ONE array item was modified, the markdown report would display ALL array items in the detailed change section. This created excessive visual clutter, making it harder to identify what actually changed.

**Before:** Modifying a single property in `allOf[4].in[3]` would show tables for **all** items (`allOf[0]` through `allOf[5]`), even though only `allOf[4]` contained changes.

**After:** The same change now shows **only** the modified array item (`allOf[4]`), significantly reducing report size and improving readability.

#### Example: Azure Policy Definition Update

**Before this fix** - A change to one nested array property showed all 6 array items:

```markdown
###### `body.properties.policyRule.if.allOf` Array

**Item [0]**
| Property | Before | After |
|----------|--------|-------|
| field | `type` | `type` |
| equals | `Microsoft.Sql/servers/databases` | `Microsoft.Sql/servers/databases` |

**Item [1]**
| Property | Before | After |
|----------|--------|-------|
| field | `Microsoft.Sql/servers/databases/sku.tier` | `Microsoft.Sql/servers/databases/sku.tier` |
| equals | `GeneralPurpose` | `GeneralPurpose` |

**Item [2]**
| Property | Before | After |
|----------|--------|-------|
| field | `Microsoft.Sql/servers/databases/sku.family` | `Microsoft.Sql/servers/databases/sku.family` |
| equals | `Gen5` | `Gen5` |

**Item [3]**
| Property | Before | After |
|----------|--------|-------|
| field | `Microsoft.Sql/servers/databases/sku.name` | `Microsoft.Sql/servers/databases/sku.name` |
| like | `GP_*` | `GP_*` |

**Item [4]** ← ONLY THIS ONE CHANGED
| Property | Before | After |
|----------|--------|-------|
| field | `Microsoft.Sql/servers/databases/sku.capacity` | `Microsoft.Sql/servers/databases/sku.capacity` |
| in[0] | `2` | `2` |
| in[1] | `4` | `4` |
| in[2] | `6` | `6` |
| in[3] | (not set) | `8` |

**Item [5]**
| Property | Before | After |
|----------|--------|-------|
| field | `requestedBackupStorageRedundancy` | `requestedBackupStorageRedundancy` |
| equals | `Local` | `Local` |
```

**After this fix** - The same change shows only the modified item:

```markdown
###### `body.properties.policyRule.if.allOf` Array

**Item [4]**
| Property | Before | After |
|----------|--------|-------|
| field | `Microsoft.Sql/servers/databases/sku.capacity` | `Microsoft.Sql/servers/databases/sku.capacity` |
| in[0] | `2` | `2` |
| in[1] | `4` | `4` |
| in[2] | `6` | `6` |
| in[3] | (not set) | `8` |
```

### Impact

- **Cleaner reports:** Resources with large nested arrays (Azure Policy Definitions, Network Security Groups, Firewall rules, etc.) produce much shorter, more readable reports
- **Easier code review:** Reviewers can immediately see what changed without scanning through unchanged array items
- **Consistent behavior:** The detailed change view now aligns with the change summary, which already correctly identified specific changed items
- **No information loss:** All changed items are still shown; only unchanged items are filtered out

### Compatibility

This change **improves** report accuracy and does not introduce breaking changes:

- If all array items changed, all items will still be displayed (same as before)
- If multiple items changed, all changed items will be displayed (improved from before, which would have shown all items anyway)
- The change only affects update operations where arrays have a mix of changed and unchanged items
- Reports will be shorter and more accurate than before

### Technical details

The fix modifies the array extraction logic in `ExtractArrayItems` (in `AzApi.Rendering.Array.cs`) to track which array items have changes and filter out items with no changes. This aligns the detailed change rendering with the change summary behavior and addresses user feedback on Feature 034 (AzAPI Attribute Grouping), which originally implemented an all-or-nothing approach as an MVP design decision.

### 2. Array diff rendering correctly handles new/removed elements

**Problem:** When an array element property was added (didn't exist in the "before" state), the diff was incorrectly showing `- <br>+ value` instead of just `value`. Additionally, Azure DevOps PR comments were not displaying colored diffs for array changes due to escaped diff markers.

**Root cause:** The `FormatUpdateCell` method in `AzApi.Rendering.Update.cs` was checking if a value was *empty* but not distinguishing between "property existed but was empty" vs. "property didn't exist at all (null)". Similarly, Azure DevOps diff formatting was escaping the `+` and `-` characters, preventing the platform from recognizing them as diff markers.

**Fix:** 
- Modified `FormatUpdateCell` to check if `beforeRaw` or `afterRaw` is null (property didn't exist) rather than just checking if the value is empty
- Added `BuildStyledDiffForTable` method that generates colored diffs using styled span elements with red/green backgrounds for Azure DevOps compatibility
- Azure DevOps now shows properly colored diffs in array matrix tables

**Impact:**
- **Cleaner diffs for new properties:** When a property is added to an array element, it now shows just the new value (e.g., `8`) instead of `- <br>+ 8`
- **Visual diff coloring in Azure DevOps:** Array changes now display with red (removed) and green (added) colored backgrounds, matching the behavior of other diff sections
- **Consistent with platform conventions:** Both GitHub and Azure DevOps now handle array diffs appropriately for their rendering engines

### Technical details (Fix #2)

The fix adds conditional logic in `FormatUpdateCell` to distinguish between null values (property didn't exist) and empty values (property existed but was empty). For Azure DevOps, a new `BuildStyledDiffForTable` method wraps diff content in styled `<span>` elements with background colors (`#ffeef0` for removed, `#e6ffed` for added) to provide visual diff highlighting in array matrix tables.

## 🔗 Commits

User-facing fixes included in this release:

- [`e4f77f6`](https://github.com/oocx/tfplan2md/commit/e4f77f6) fix: filter array items to show only changed items in update mode
- [`4a46c4b`](https://github.com/oocx/tfplan2md/commit/4a46c4b) fix: array diff rendering - skip markers when property didn't exist, add colored spans
- [`f286146`](https://github.com/oocx/tfplan2md/commit/f286146) fix: array diff rendering for new elements and Azure DevOps coloring

## 📚 Related Documentation

- **Issue analysis:** [docs/issues/090-nested-array-shows-all-items/analysis.md](../090-nested-array-shows-all-items/analysis.md)
- **Feature specification:** [docs/features/034-azapi-attribute-grouping/specification.md](../../features/034-azapi-attribute-grouping/specification.md)
- **Test coverage:** 3 new regression tests covering single-item changes, multiple-item changes, and all-items-changed scenarios
- **All tests:** Passing (50/50 AzAPI tests verified)
