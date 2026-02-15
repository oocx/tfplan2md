# Code Review: NSG Rendering Issues

## Summary

This review covers the fix for three related NSG rendering issues: duplicate header line, incorrect column layout for create actions, and over-escaping of `>` characters. The implementation correctly addresses all three issues identified in the analysis document, but there are critical blockers related to test failures and missing snapshot updates.

## Verification Results

- Tests: **Fail** (3 failed, 812 passed)
- Coverage: Not measured in this review (requires separate coverage run)
- Build: **Success**
- Docker: **Builds successfully**
- Errors: **None** (no C# compilation errors)
- Markdownlint: **Pass** (0 errors on comprehensive demo)

## Review Decision

**Status:** Changes Requested

## Snapshot Changes

- Snapshot files changed: **Yes** (2 snapshots: `azapi-special-chars.md`, `comprehensive-demo.md`)
- Commit message token `SNAPSHOT_UPDATE_OK` present: **No** ❌
- Why the snapshot diff is correct: The changes are intentional and correct:
  1. `azapi-special-chars.md`: `>` is no longer escaped, so `\<tag\>content\</tag\>` becomes `\<tag>content\</tag>`. This is correct because the fix intentionally removes `>` escaping to improve readability in inline code spans (where the backslash becomes visible).
  2. `comprehensive-demo.md`: The redundant "Network Security Group: `nsg-app`" line has been removed. This is correct because the NSG name is already displayed in the `<summary>` element, making the duplicate line unnecessary.

## Issues Found

### Blockers

1. **Missing `SNAPSHOT_UPDATE_OK` token** ([commit 25baca6d](commit:25baca6d))
   - The snapshot changes are intentional and correct, but the commit message lacks the required `SNAPSHOT_UPDATE_OK` token.
   - **Fix:** Add a new commit with message including `SNAPSHOT_UPDATE_OK` and explanation of why the snapshot changes are correct.

2. **Test failure: `FormatDiff_EscapesValuesAndPreservesLineBreakTags`** ([ScribanHelpersFormatDiffTests.cs](../../src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersFormatDiffTests.cs#L57-L61))
   - The test expects `>` to be escaped in `FormatDiff` output, but the implementation now intentionally does not escape `>`.
   - The test expects: `"- \`\\<before\\>\`<br>+ \`\\<after\\>\`"`
   - But now produces: `"- \`\\<before>\`<br>+ \`\\<after>\`"`
   - **Fix:** Update the test expectation to match the new behavior (no `>` escaping):
     ```csharp
     .Should().Be("- `\\<before>`<br>+ `\\<after>`");
     ```

3. **Snapshot files not regenerated** ([TestData/Snapshots/](../../src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/))
   - Two snapshot files need to be regenerated:
     - `azapi-special-chars.md` 
     - `comprehensive-demo.md`
   - **Fix:** Run `scripts/update-test-snapshots.sh` to regenerate the snapshots after fixing the test above.

### Major Issues

None

### Minor Issues

None

### Suggestions

1. Consider adding a comment in the `EscapeMarkdown` method explaining why `>` is not escaped (to improve readability in inline code contexts where backslash escapes are preserved literally).

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ❌ (tests failing, snapshots need update) |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ❌ (1 test needs update, snapshots need regeneration) |
| Documentation | ✅ |

### Detailed Checklist

#### Correctness
- [x] Code implements all issues from the analysis (3 issues fixed)
- [x] All changes align with the analysis document
- [ ] Tests pass (3 failing - needs fixes)
- [ ] Snapshots have `SNAPSHOT_UPDATE_OK` token (missing)
- [x] No workspace problems (no C# compilation errors)
- [x] Docker image builds successfully
- [x] Comprehensive demo passes markdownlint

#### Code Quality
- [x] Follows C# coding conventions
- [x] Uses `_camelCase` for private fields (N/A - no private fields added)
- [x] Uses modern C# features appropriately
- [x] Files are under 300 lines (all files well under limit)
- [x] No unnecessary code duplication

#### Access Modifiers
- [x] Uses most restrictive access modifier (all changes to existing code)
- [x] No inappropriate `public` members (no new members added)

#### Code Comments
- [x] All members have XML doc comments
- [x] Comments explain "why" not just "what"
- [x] Required tags present: `<summary>`, `<param>`, `<returns>`
- [x] New test has `<summary>` explaining purpose
- [x] Comments are synchronized with code

#### Architecture
- [x] Changes align with the analysis document
- [x] No unnecessary new patterns or dependencies
- [x] Changes are focused on the task (3 related issues)

#### Testing
- [x] Tests are meaningful and test the right behavior
- [x] New test added for the `>` escaping behavior
- [x] Tests follow naming convention: `MethodName_Scenario_ExpectedResult`
- [ ] All tests pass (1 test needs update)

#### Documentation
- [x] Documentation is updated (analysis document exists)
- [x] No contradictions in documentation
- [x] CHANGELOG.md was NOT modified ✅
- [x] Comprehensive demo output passes markdownlint
- [x] Analysis document clearly explains the issues and fixes

## Next Steps

The following rework is required before this fix can be approved:

1. **Update failing test** ([ScribanHelpersFormatDiffTests.cs:59](../../src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersFormatDiffTests.cs#L59)):
   - Change expected value from `"- \`\\<before\\>\`<br>+ \`\\<after\\>\`"` to `"- \`\\<before>\`<br>+ \`\\<after>\`"`

2. **Regenerate snapshots**:
   - Run `scripts/update-test-snapshots.sh` to update the two affected snapshot files

3. **Add commit with `SNAPSHOT_UPDATE_OK` token**:
   - Create a commit message that includes `SNAPSHOT_UPDATE_OK` and explains why the snapshot changes are correct (e.g., "test: regenerate snapshots after fixing > escaping - SNAPSHOT_UPDATE_OK. Snapshots updated because: (1) > no longer escaped in inline code for better readability, (2) duplicate NSG header line removed")

4. **Verify all tests pass**:
   - Run `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` to confirm all tests pass

**Next**
- **Option 1:** Hand off to Developer agent to fix the blockers
- **Option 2:** Fix the issues yourself if you have the necessary context

**Recommendation:** Option 1, because the Developer agent should handle the rework to address the test failures and snapshot updates.
