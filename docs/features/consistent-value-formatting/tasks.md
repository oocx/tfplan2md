# Tasks: Consistent Value Formatting

## Implementation

- [ ] Update `ScribanHelpers.FormatDiff` signature and implementation
  - [ ] Add `format` parameter
  - [ ] Implement table-compatible `inline-diff` rendering (HTML with `<br>`)
  - [ ] Implement table-compatible `standard-diff` rendering (HTML with `<br>`)
- [ ] Update `default.sbn`
  - [ ] Reverse backticks in attribute tables (Name plain, Value code)
- [ ] Update `role_assignment.sbn`
  - [ ] Reverse backticks in attribute tables
  - [ ] Update summary lines to only code-format values
- [ ] Update `firewall_network_rule_collection.sbn`
  - [ ] Update header formatting
  - [ ] Update rule table formatting (code-format all data columns)
  - [ ] Pass `large_value_format` to `format_diff` calls
- [ ] Update `network_security_group.sbn`
  - [ ] Update header formatting
  - [ ] Update rule table formatting
  - [ ] Pass `large_value_format` to `format_diff` calls

## Testing

- [ ] Verify attribute tables in default output
- [ ] Verify role assignment output
- [ ] Verify firewall rule output (inline and standard diffs)
- [ ] Verify NSG rule output (inline and standard diffs)
- [ ] Verify `large_value_format` CLI option affects `format_diff` output
