### Summary

Describe the change and motivation in one or two sentences.

---

### Checklist

- [ ] All checks pass (build, test, lint)
- [ ] Commits follow Conventional Commits
- [ ] PR description uses the standard template (Problem / Change / Verification)

**Merge method:** Use **Rebase and merge** to maintain a linear history. The repository enforces rebase-only merges by default.

**Create & merge guidance:** Use `scripts/pr-github.sh create` to create PRs, and `scripts/pr-github.sh create-and-merge` to perform the merge (this script is the authoritative, repo-preferred tool for PR creation and merges). If you need to inspect/check the PR, use GitHub chat tools (`github/*`) as needed.