# Bug Fixes Release

This release fixes 5 bugs discovered during systematic code review: uncaught exception types, silent data loss, misordered guard clauses, and dead code. All fixes include regression tests.

## 🐛 Bug fixes

- **Parser exception handling**: `TerraformPlanParser.Parse(null!)` and `ParseAsync(null!)` now throw the documented `TerraformPlanParseException` instead of `ArgumentNullException`. Added explicit null guards with clear error messages for both methods.

- **CLI argument validation**: Multiple positional arguments (`tfplan2md a.json b.json`) no longer silently overwrite the input file. The CLI now throws `CliParseException` with message "Unexpected argument: {arg}. Only one input file can be specified."

- **Code formatting edge case**: Fixed escaped backtick removal (`\`` → empty) that was stripping content from plain text values. The stripping now only occurs inside `<code>` tags where it belongs (fixing FormatDiff output like `<code>\`value\`</code>`).

- **Principal mapper performance**: Both `GetPrincipalName` overloads now check `IsNullOrWhiteSpace` *before* calling `GetName`, avoiding unnecessary dictionary lookups on invalid input.

- **Dead code elimination**: Removed redundant ternary expression `return name is not null ? name : null` in `ResourceSummaryBuilder.BuildDeleteSummary` (simplified to `return name`).

## 🔗 Commits

- [`93df991`](https://github.com/oocx/tfplan2md/commit/93df9918b743c13439657974dbfbf4907be91fec) fix: CLI parser now rejects multiple positional arguments
- [`aaa7435`](https://github.com/oocx/tfplan2md/commit/aaa74350a9cec2efe023d4576cc18d6db211c2d9) fix: escaped backtick stripping only applies to HTML code tags
- [`19b2be0`](https://github.com/oocx/tfplan2md/commit/19b2be01f2ea99a048ddffc611893e2cec0f470c) fix: remove redundant ternary in ResourceSummaryBuilder
- [`252ac13`](https://github.com/oocx/tfplan2md/commit/252ac13dae4aba6bc1330a13963cc671016f5993) fix: parser throws TerraformPlanParseException on null input
- [`c466bb4`](https://github.com/oocx/tfplan2md/commit/c466bb46ef483c3a7e5b783bf2eb83d2fdef81d7) fix: principal mapper checks null/whitespace before dictionary lookup
- [`76a802a`](https://github.com/oocx/tfplan2md/commit/76a802ad496ebd44fa8a8a933690540bf006e872) test: add CLI parser test for multiple positional arguments
- [`e1226ce`](https://github.com/oocx/tfplan2md/commit/e1226ceec02a159df3edc66498ee1a8b8448467e) test: add comprehensive tests for code formatting helpers
- [`05889df`](https://github.com/oocx/tfplan2md/commit/05889df2fb59af3b1a2a8675999e310d59c515f4) test: add ResourceSummaryBuilder test for null display name handling
- [`8de2c99`](https://github.com/oocx/tfplan2md/commit/8de2c995db36768b694901173c98985739bf7f7e) test: add TerraformPlanParser null stream test
- [`ec1dbc4`](https://github.com/oocx/tfplan2md/commit/ec1dbc4800643eac8c934715a80d2f58daf9bb61) test: add TerraformPlanParser null input test
- [`8e60a0b`](https://github.com/oocx/tfplan2md/commit/8e60a0b8fb7f28a550703371ba75658d73cecdcd) test: add PrincipalMapper tests for null/empty/whitespace input handling
