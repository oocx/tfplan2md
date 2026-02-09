# Release Trigger Fix

## Investigation Complete ✅

**Root Cause Found:** CI workflow pushes commit+tag together, preventing Release workflow from triggering.

**Fix Applied:** Added `--no-follow-tags` to ensure tag is pushed separately as a distinct event.

## Summary

### Problem
- v1.15.0 tag exists but no GitHub Release was created
- CI workflow pushed tag alongside commit, second push had no effect
- Release workflow never triggered

### Solution  
Changed `.github/workflows/ci.yml` line 201:
```diff
- git push origin HEAD
+ git push --no-follow-tags origin HEAD
```

### Manual Action Required
Trigger Release workflow for v1.15.0:
```bash
gh workflow run release.yml -f tag=v1.15.0
```

## Files Changed
- `.github/workflows/ci.yml` - Added --no-follow-tags flag to git push command

## Verification
- ✅ Root cause identified via CI workflow logs
- ✅ Minimal fix implemented (single flag addition)
- ✅ Fix committed to PR branch
- ⏳ Requires manual release trigger for v1.15.0
- ⏳ Will be validated on next PR merge

See detailed analysis in commit message.
