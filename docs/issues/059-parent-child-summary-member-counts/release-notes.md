# Fix Azure AD Group Member Count Summary

Bug fix release. Corrects icon counts in Azure AD group summaries to include both inline members and separate `azuread_group_member` child resources.

## 🐛 Bug fixes

**Fixed: Azure AD group summaries show incorrect member counts ([#447](https://github.com/oocx/tfplan2md/issues/447))**

When Azure AD groups had members defined both inline (via the `members` attribute) and as separate `azuread_group_member` child resources, the summary line only counted inline members. Separate child resources appeared in the member table but weren't reflected in the icon counts.

**Before:**
```
🔄 azuread_group platform_engineers | 0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members
```
- Icon counts showed zeros or undercounted members
- Member table had more rows than the icon summary indicated
- Separate `azuread_group_member` resources were missing from icon counts

**After:**
```
🔄 azuread_group platform_engineers | 3 👤 1 👥 1 💻 | ➕ 5 members
```
- Icon counts now include ALL members (inline + separate child resources)
- Counts correctly show member types: 👤 users, 👥 groups, 💻 service principals
- Icon counts match the number of rows in the member table

**Technical details:**

The fix adds a post-merge update step that runs after parent-child relationship merging completes. It extracts all member IDs from the merged child resource groups, recounts members by type using the existing principal mapper, and updates the icon counts in the summary HTML.

This approach maintains the simple architecture without introducing new interfaces or registry patterns.

## 🔗 Commits

- [`82a4533`](https://github.com/oocx/tfplan2md/commit/82a4533fae929706b38de5490c1127c84efa2e1e) fix: update Azure AD group member counts after parent-child merging
- [`2bd97e2`](https://github.com/oocx/tfplan2md/commit/2bd97e2e9c765f5de4e4635622ed8d23cec8994f) refactor: move Azure AD group summary logic to provider layer
