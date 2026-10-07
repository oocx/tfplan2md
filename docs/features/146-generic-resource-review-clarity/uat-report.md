# UAT Report: Feature 146 Generic Resource Review Clarity

## Platform evidence

| Platform | PR | Publication result | Visual decision |
| --- | --- | --- | --- |
| GitHub | [UAT #127](https://github.com/oocx/tfplan2md-uat/pull/127) | Open; eight feature comments and one regression comment | Approved regarding this feature |
| Azure DevOps | [UAT #113](https://dev.azure.com/oocx/test/_git/test/pullrequest/113) | Active; eight feature comments and one regression comment | Approved regarding this feature |
| Bitbucket | No platform PR | Eight target reports generated locally | Real UI validation unavailable |

PR overview links: [GitHub](https://github.com/oocx/tfplan2md-uat/pulls) and
[Azure DevOps](https://dev.azure.com/oocx/test/_git/test/pullrequests?_a=mine).
Both UAT PRs remain open for the Maintainer. No cleanup has been run.
GitHub PR #127 is attached to the Codex chat. The app rejected attachment of the
Azure DevOps URL with: "attach_artifact requires a supported artifact type and
valid pull request URL." Its link remains available above.

## Artifacts and generation

Fixture: [uat-plan.json](uat-plan.json), containing 22 resource scenarios, and
[uat-plan-root-noop.json](uat-plan-root-noop.json), the root-only-no-op variant.
The fixture-only example provider supplies unmapped review_object resources;
existing azapi, msgraph, azurerm and Azure DevOps mappings exercise mapped content.
Generated 24 reports under repository scratch `.tmp/feature-146-uat/`, using eight
variants for each of github, azuredevops and bitbucket. Exact commands and
resource-specific instructions are retained in `manifest.json` in that folder.

Generation uses the current built Release binary:

```text
dotnet src/Oocx.TfPlan2Md/bin/Release/net10.0/tfplan2md.dll   --render-target TARGET [OPTIONS] FIXTURE --output REPORT
```

| Report suffix | Options / fixture |
| --- | --- |
| default | Default options, uat-plan.json |
| show-unchanged | --show-unchanged-values |
| hide-unchanged-outputs | --hide-unchanged-outputs |
| summary-overrides | --summary-name-attributes body.title,body.properties.title |
| combined-options | Both new options plus --show-unchanged-values |
| show-sensitive | --show-sensitive on synthetic fixture values |
| root-noop-default | Default options, uat-plan-root-noop.json |
| root-noop-filtered | --hide-unchanged-outputs, uat-plan-root-noop.json |

Files use `TARGET-SUFFIX.md`. Each real platform received its matching target's
reports. The wrappers automatically added `artifacts/comprehensive-demo-simple-diff.md`
on GitHub and `artifacts/comprehensive-demo.md` on Azure DevOps as regression comments.
All 24 feature reports and both regression demos passed repository freshness checks:
their embedded a65c84a source is unchanged by subsequent source-equivalent/docs commits.
The feature branch was fetched and rebased before UAT; it was already current.

Publication used repository wrappers via `.tmp/feature-146-uat/run-approved-uat.py`,
with platform-specific runs and separately preserved `github-state.json` and
`azuredevops-state.json` in the same scratch folder. The Maintainer explicitly
approved both exact proposed PR titles/bodies and feature/regression comment posting
before creation. Published titles are `UAT: github-default` and
`UAT: azuredevops-default`.

## Scenario coverage and review status

| Scenario group | Prepared evidence | Platform visual result |
| --- | --- | --- |
| Indexed identities, fallback order, unknown prior names, deletes/replaces and escaped text | Default reports; all required generic resource scenarios present | Approved regarding this feature |
| Full-address import/move refactoring labels and protected names | Default and authorized synthetic show-sensitive comparisons | Approved regarding this feature |
| Empty/housekeeping imported prior-state note versus populated import | Default reports contain the import note and both import classes | Approved regarding this feature |
| Accessible unchanged msgraph Provider settings and primary changed/added/removed settings | Default, show-unchanged and combined reports | Approved regarding this feature |
| Bare/module pseudo-reference suppression and retained useful computed hints | Root/module reference resources and sensitive computed markers | Approved regarding this feature |
| Ordered override affecting only generic fallback summaries | Summary-overrides and combined reports; mapped resources retained | Approved regarding this feature |
| Default and filtered root/module outputs, protection and section/group removal | Default/filtered reports and root-noop pair | Approved regarding this feature |
| Provider-owned diffs, tables, ordinary/large values and card structure | Matching-target feature reports plus comprehensive regression comments | Approved regarding this feature |

Local content inspection confirmed Provider settings and import notes are exercised,
output-only module.noop is present by default and absent under filtering,
root_unchanged disappears under filtering, and the root-only-no-op Outputs section
is fully removed by the option on all three targets. These content checks establish
coverage; they do not establish platform rendering acceptance.

## Problems and operational workarounds

- The wrapper parser requires --state-file before --platform; corrected the scratch
  runner after an initial pre-creation usage rejection.
- UAT gitlinks/checkouts were absent despite .gitmodules entries. Cloned dedicated
  clean UAT repositories under repository scratch and used command-scoped
  UAT_GITHUB_SUBMODULE_PATH and AZDO_SUBMODULE_PATH overrides.
- Wrapper tee logs hardcode /tmp paths. A command-scoped tee shim redirects only
  those two log paths into repository scratch; repository source is unchanged.
- Azure setup requires AZURE_DEVOPS_EXT_PAT despite functioning stored CLI login.
  Reused the existing authenticated Git credential in the child environment only;
  no token values were printed or persisted in UAT artifacts.
- Both wrappers consumed one backslash in the escaped resource-key instruction
  through shell substitution. Restored the exact approved PR bodies and verified
  both match; titles and nine comments/threads per platform were confirmed.
- Azure comment counting uses az devops invoke pullrequestthreads; az repos pr
  comment list is unsupported by the installed CLI.
- Azure DevOps app attachment is unsupported for this URL; Bitbucket real UI
  evidence is unavailable through repository UAT tooling.

The Maintainer approved the changes from this feature. The subsequent sensitive-output
question was investigated against all nine published GitHub comments: the synthetic
value ROOT-SENSITIVE-OUTPUT-DO-NOT-LEAK appears only in the explicitly authorized
[--show-sensitive comment](https://github.com/oocx/tfplan2md-uat/pull/127#issuecomment-6043712650).
The default report masks root_sensitive. This is the expected synthetic comparison
behavior; no default-output secret leak was found.

## Maintainer decision

The Maintainer's visual decision, verbatim:

> approved regarding the changes from this feature.

The driver recorded the UAT gate as approved in commit 441e8ef0. Earlier approval
of PR creation was separate from this visual decision. The Maintainer then asked
whether root_sensitive exposed ROOT-SENSITIVE-OUTPUT-DO-NOT-LEAK; investigation
confirmed exposure only in the authorized synthetic --show-sensitive variant,
with the default output masked. Bitbucket real UI validation remains unavailable.
