# Test quality: replace fragmented Contain assertions with precise Be assertions

Improves test precision by converting chains of `.Should().Contain()` calls on single
string values into single exact `.Should().Be()` assertions across 15 test files.

## 🧹 Internal improvements

### Fragmented string assertions replaced with exact-match assertions

**Problem:** Multiple unit tests used two or more consecutive `.Should().Contain()` calls
on the same string value to verify its content piecemeal:

```csharp
// Before — passes even when surrounding content is wrong
viewModel.SummaryText.Should().Contain("remove");
viewModel.SummaryText.Should().Contain("🛡️");
viewModel.SummaryText.Should().Contain("subscription");
```

Each individual `Contain()` check only confirms that a fragment appears somewhere in
the string. Surrounding structure, order, non-breaking spaces, and exact formatting
characters were all left unchecked.

**Fix:** Each fragmented chain is replaced with a single exact `Be()` assertion:

```csharp
// After — fails immediately when any character differs
viewModel.SummaryText.Should().Be(
    "remove <code>🛡️\u00A0Contributor</code> on subscription <code>🔑\u00A0sub-id</code> from <code>👤\u00A0principal-1</code>");
```

The new assertions verify the complete output string, including icon non-breaking spaces
(`\u00A0`), HTML tags, parentheses, and order — properties the previous fragmented checks
ignored.

**Affected test files (15):**

- `AzureAdGroupSummaryMemberCountTests.cs` — 4 summary HTML assertions
- `AzureAdGroupSummaryRebuilderTests.cs` — 2 summary HTML assertions
- `RoleAssignmentManagementGroupFormattingTests.cs` — 2 scope/summary assertions
- `RoleAssignmentViewModelFactoryTests.cs` — 9 assertions across summary, scope, and principal fields
- `ScribanHelpersAzureScopeFormattingTests.cs` — 5 scope formatting assertions
- `ScribanHelpersAzureMetadataTests.cs` — 1 role info field assertion
- `ScribanHelpersAzdoTests.cs` — 1 group name assertion
- `ScribanHelpersAttributeCollectionTests.cs` — 1 collection equivalence assertion
- `ScribanHelpersLargeValueTests.cs` — 3 large-value format assertions
- `SensitivityHierarchyTests.cs` — hierarchy path assertions
- `ParentChildInlineDiffTests.cs` — simple diff assertions
- `AzureDevOpsDiffFormatterTests.cs` — 2 simple case assertions
- `AzureValueFormatterTests.cs` — 1 role definition format assertion
- `ProviderValueFormatterRegistryTests.cs` — 1 role definition format assertion

**Not converted:** Assertions that check individual lines within large multi-line rendered
templates, assertions that test CSS colour values within dynamically-generated HTML `<span>`
character-level diffs, and collection `.Contain()` calls using predicates — all of which are
the appropriate assertion form for their context.

## 🔗 Commits

- [`336fd8f`](https://github.com/oocx/tfplan2md/commit/336fd8f69c294a25f565963a8d5d28929d30ab06) test: replace fragmented Contain assertions with precise Be assertions
- [`25d96be`](https://github.com/oocx/tfplan2md/commit/25d96be39d59f0de68e1b0c0e47cdf9e8a9c64a8) test: convert remaining fragmented Contain assertions to Be in 3 more test files
