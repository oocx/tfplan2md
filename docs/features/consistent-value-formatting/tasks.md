# Tasks: Consistent Value Formatting

## Implementation

- [ ] Update `ScribanHelpers.FormatDiff` signature and implementation
  - [ ] Change signature to `FormatDiff(string? before, string? after, string format)`
  - [ ] Implement table-compatible `inline-diff` rendering (HTML with `<br>`)
  - [ ] Implement table-compatible `standard-diff` rendering (HTML with `<br>`)
- [ ] Update `ScribanHelpers.RegisterHelpers`
  - [ ] Add `LargeValueFormat` parameter
  - [ ] Register `format_diff` as a closure capturing the format
- [ ] Update `MarkdownRenderer`
  - [ ] Pass `LargeValueFormat` to `RegisterHelpers`
  - [ ] Thread `LargeValueFormat` through `RenderResourceChange` and `RenderResourceWithTemplate`
- [ ] Update `default.sbn`
  - [ ] Reverse backticks in attribute tables (Name plain, Value code)
- [ ] Update `role_assignment.sbn`
  - [ ] Reverse backticks in attribute tables
  - [ ] Update summary lines to only code-format values
- [ ] Update `firewall_network_rule_collection.sbn`
  - [ ] Update header formatting
  - [ ] Update rule table formatting (code-format all data columns)
- [ ] Update `network_security_group.sbn`
  - [ ] Update header formatting
  - [ ] Update rule table formatting

## Testing

- [ ] Verify attribute tables in default output
- [ ] Verify role assignment output
- [ ] Verify firewall rule output (inline and standard diffs)
- [ ] Verify NSG rule output (inline and standard diffs)
- [ ] Verify `large_value_format` CLI option affects `format_diff` output
