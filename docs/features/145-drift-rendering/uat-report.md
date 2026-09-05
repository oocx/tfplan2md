# UAT Report: Drift Rendering

## GitHub

- Pull request: [oocx/tfplan2md-uat#126](https://github.com/oocx/tfplan2md-uat/pull/126)
- Posted reports: the feature-specific drift report and the comprehensive regression report.
- Result: passed, as confirmed by the Maintainer: “github uat passed”.

The feature report verifies grouped drift entries for matching values, separate entries
for differing transitions, omission of no-op entries in `relevant` mode, suppression
with `none`, and safe rendering of unsafe values.

## Azure DevOps

Azure DevOps UAT was not run. The Maintainer explicitly waived it after GitHub UAT
passed: “skip azdo uat”. The configured Azure DevOps UAT credentials were unavailable
in this environment.

## Decision

UAT passed with the Azure DevOps platform waived by the Maintainer.
