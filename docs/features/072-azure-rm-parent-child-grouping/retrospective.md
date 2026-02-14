# Feature 072: Retrospective

## What Went Well

1. **Iterative Development**: Multiple rounds of feedback and fixes led to a robust solution
2. **Comprehensive Testing**: UAT validation on both GitHub and Azure DevOps platforms
3. **Code Quality**: All tests passing, markdownlint compliant, thorough code reviews
4. **Documentation**: Detailed release notes with visual examples

## Challenges & Solutions

### Challenge 1: HTML Tags Appearing as Literal Text
**Problem**: HTML `<span>` tags were appearing as escaped text in diffs.
**Solution**: Extract raw values (icons only, no backticks) before passing to `FormatDiff`.

### Challenge 2: Backticks on Bare Dash Placeholder
**Problem**: The bare dash `-` for null values was being wrapped in code tags.
**Solution**: Added special case in `FormatDiff` to return bare `-` when both values are the dash placeholder.

### Challenge 3: Literal `<br>` Tags in GitHub Diffs
**Problem**: Line breaks showed as literal `<br>` text.  
**Initial Fix**: Changed to `\n` - broke markdown tables.
**Final Solution**: Keep `<br>` tags, add pattern detection in `FormatChildValue` to pass through simple diff format without wrapping.

### Challenge 4: Markdownlint Trailing Spaces
**Problem**: Template had trailing spaces when conditional was false.
**Initial Fix**: Used `{{~ end ~}}` - collapsed all table rows into one line.
**Final Solution**: Use `{{ end ~}}{{~ if}}` pattern to strip right whitespace while preserving newlines.

## Lessons Learned

1. **Scriban Whitespace Control**: `{{~` strips LEFT, `~}}` strips RIGHT. Be careful not to strip newlines.
2. **Template Testing**: Always test both conditional branches (true/false).
3. **Markdown Linting**: Automated validation catches subtle issues early.
4. **UAT Platforms**: Test on both GitHub and Azure DevOps as they handle HTML differently.

## Metrics

- **Total Commits**: 66
- **Code Reviews**: 6
- **UAT Iterations**: 5
- **Test Coverage**: 1007 tests, all passing
- **Markdownlint Errors**: 0 (after fixes)
