# Test Plan: NBSP between icon and label (#033)

## Objective
Verify that all "icon + label" combinations in the generated markdown output use non-breaking spaces (U+00A0) instead of regular spaces (U+0020), ensuring icons and their labels stay together on the same line.

## Target Areas
1. Summary table rows (Add, Change, Replace, Destroy)
2. Module headers (📦 Module:)
3. Semantic formatting in resource attributes (Direction, Access, Protocol)

## Verification Steps
1. **Unit Tests**: Run `dotnet test` to verify that `MarkdownRendererTests` and `MarkdownToHtmlRendererTests` reflect the new spacing requirements.
2. **Snapshot Verification**: Run `scripts/update-test-snapshots.sh` and inspect the diff to confirm U+0020 has been replaced by U+00A0.
3. **Manual Inspection**: Use a hex editor or a diagnostic script to verify the exact codepoint (U+00A0) in `artifacts/comprehensive-demo.md`.
4. **Visual Verification**: Check rendered website examples in `website/features/nsg-rules.html` to ensure icons do not wrap.

## Automated Tests
- `src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownRendererTests.cs`
- `src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/ComprehensiveDemoTests.cs`
- `src/tests/Oocx.TfPlan2Md.HtmlRenderer.Tests/MarkdownToHtmlRendererTests.cs`
