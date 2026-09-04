---
name: context-pack
description: Assemble the context a role needs without reading whole files - structural search with ast-grep, and what to delegate instead of reading.
---

# Context Pack

Token cost is dominated by what enters context, not by what you do with it. This skill
is about reading less while knowing more.

## Read known paths, search for unknown ones

A role usually needs specific artifacts — `specification.md`, `architecture.md`,
`tasks.md`, `analysis.md`. Read those directly; there is nothing to discover.

Reach for search only when the question is "where is X". Then make the search
structural rather than textual.

## ast-grep for code questions

`ast-grep` matches syntax, not strings, so it returns the construct you asked about
instead of every line mentioning it.

```bash
# Every class implementing an interface, without reading any file whole
ast-grep --lang csharp -p 'class $NAME : $$$ IResourceRenderer $$$'

# Every call to a method, with its arguments
ast-grep --lang csharp -p 'RegisterRenderer($$$)'

# Every method with a given attribute
ast-grep --lang csharp -p '[Test] $RET $NAME($$$) { $$$ }'
```

**Always invoke `ast-grep` by its full name.** On Linux, `sg` is util-linux's setgid
binary; a script that calls `sg` expecting ast-grep runs the wrong program silently.

Fall back to `rg` for prose, configuration and markdown, where there is no syntax tree
worth matching.

## Delegate what you do not need verbatim

Spawn a **cheap-tier** subagent for anything whose output you need conclusions from
rather than content:

- "Where is the display-name logic for azurerm?" — the answer is a path, not a file.
- Build and test runs where only pass/fail matters.
- "Does anything else already do X?" before writing something new.

Do not delegate a single file read or one `rg` — the spawn costs more than the call.

## Command output is compressed for you

`rtk` sits in front of Bash calls and compresses the output of git, test runners,
linters, `gh` and docker before it reaches context. You do not invoke it; the hook
rewrites the command.

Two consequences worth knowing:

- Its compression is **lossy** — dedup, truncation, grouping. Where you need the exact
  bytes, say so; `tee` mode preserves full output on failure.
- The Code Reviewer's diff is excluded, because a truncated diff produces a confident
  review of code nobody saw.

## What not to do

- Do not `cat` a 300-line file to answer a one-line question.
- Do not read a file "for context" before knowing what you are looking for.
- Do not paste a role file into a subagent prompt — the subagent reads it itself.
