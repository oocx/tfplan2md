# Fix crash when `after_unknown` is an object in output changes

This patch fixes a `DeserializeUnableToConvertValue` exception that occurred when
parsing Terraform plan JSON where `output_changes[*].after_unknown` is a non-boolean
value (for example `{}`).

## 🐛 Bug fixes

### Fixed parser crash for output changes with non-boolean `after_unknown`

**Problem:** When a Terraform plan contains an output change whose `after_unknown` field
is an object (`{}`) instead of a plain boolean (`true` / `false`), `tfplan2md` crashed
with a deserialization exception:

```
System.Text.Json.JsonException: DeserializeUnableToConvertValue
```

This can happen with certain provider output types — for example, when an entire
computed output value is unknown at plan time and Terraform encodes that state as an
object rather than `true`.

**Root cause:** `OutputChange.AfterUnknown` was typed as `bool` in the internal plan
model. The `Change` record — used for resource attribute changes — was already typed
as `object?` and handled via `AfterUnknownHelper.IsWholeResourceUnknownAfterApply()`,
but `OutputChange` was missed when output-change support was added.

**Fix:** Changed `OutputChange.AfterUnknown` from `bool` to `object?`, matching the
existing `Change` pattern. The consumer (`ReportModelBuilder.Outputs.cs`) now uses
`AfterUnknownHelper.IsWholeResourceUnknownAfterApply()` to derive the `IsComputed`
flag, so behaviour for `true` / `false` values is unchanged and `{}` is correctly
treated as "not entirely unknown".

**Impact:**
- Plans that previously caused a crash now parse and render correctly.
- Output changes with `after_unknown: true` still render with a "(computed)" indicator.
- Output changes with `after_unknown: {}` or `after_unknown: false` render normally,
  with no crash and no change to the displayed output value.

## 🔗 Commits

- [`1ca12c9`](https://github.com/oocx/tfplan2md/commit/1ca12c98653fbf8ea4a1a679d37971b31fdf7bb7) fix: change OutputChange.AfterUnknown from bool to object?
