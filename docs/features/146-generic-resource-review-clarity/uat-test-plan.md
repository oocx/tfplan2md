# UAT Test Plan: Generic Resource Review Clarity

## Goal

Verify readable indexed/refactoring identities, clear import context, accessible
provider settings, and changed-output filtering on all supported render targets.

## Required Artifacts

The Developer owns uat-plan.json and rendered uat-plan.md in this work-item folder.
Build a valid Terraform plan JSON fixture with resource changes, matching planned
and prior state as required, configuration references, unknown/sensitivity markers,
and root/module output data. Synthetic unmapped review_object resources exercise
fallback naming; document their example provider as fixture-only. Existing
azapi/msgraph mappings remain mapped.

### Resource inventory for uat-plan.json

| Resource scenario | Required values/edge case |
| --- | --- |
| review_object.defaults["first"], create | All six default paths distinct; name wins |
| review_object.fallback[0], create | name null, display_name empty, displayName object; nested body.displayName usable; literal dotted key differs |
| review_object.none[1], create | All candidates absent/unusable; suffix remains |
| module.demo.review_object.update["a.b[0]"], update | Desired name unknown, prior name known, later candidate known |
| review_object.deleted[2], delete | Prior display_name present |
| review_object.replaced, indexed replace | Desired displayName differs; key includes escaped quote/backslash |
| review_object.override["custom"], create | body.title/body.properties.title and competing defaults |
| review_object.masked["secret"], import/create | Synthetic sensitive name, safe later candidate; default summary/refactoring must omit secret |
| module.demo.review_object.imported["named"], import | Housekeeping-only before plus body:{}, friendly desired name |
| module.demo.review_object.moved["new"], move | Previous address differs in index; friendly identity present |
| msgraph_resource.policy["block_legacy_auth"], update | body.displayName; body.conditions.clientAppTypes change; all three settings unchanged |
| msgraph_update_resource.policy["require_mfa_admins"], update | body.displayName; substantive body prior state; url changed; other settings unchanged |
| msgraph_resource.created[0], create | Friendly body.displayName and all three desired settings |
| msgraph_update_resource.import_empty["empty"], import | Prior id/api_version/url/ignore_missing_property plus body:{}; full desired body/settings |
| msgraph_resource.import_full["full"], import | Prior body.displayName/body.enabled:false, changed body value; no empty note |
| msgraph_resource.settings_added["add"], update | Meaningful prior body, api_version added |
| msgraph_update_resource.settings_removed["remove"], update | Meaningful prior body, ignore_missing_property removed |
| azapi_resource.mapped[0], update | name/type/parent_id context and competing body.title override |
| azurerm_resource_group.mapped["rg"], create | name/location context and competing override |
| azuredevops_variable_group.mapped[0], update | Provider-owned summary and ordinary variable diff |

Include computed attributes referencing bare and module.demo-qualified each.key,
each.value, count.index, and self; specific each.value.group_object_id; static
resource, variable, local, and mixed pseudo/useful reference lists. Include one
sensitive computed attribute. Include friendly text with ampersand, quotes, HTML
delimiters, pipe, and backtick to inspect safe display.

### Output inventory

At root and module.demo include unchanged, created, updated, deleted, sensitive
changed, and unknown changed outputs, with distinct names. Add output-only
module.noop containing only no-op outputs. Prepare a root-only-no-op fixture
variant to exercise complete removal of the root Outputs section.

## Rendering Runs

Generate default uat-plan.md and labelled review variants in repository .tmp/ or
feature artifact locations. Run each variant for github, azuredevops, and bitbucket:

1. Default options.
2. --show-unchanged-values.
3. --hide-unchanged-outputs, also on the root-only-no-op variant.
4. --summary-name-attributes body.title,body.properties.title.
5. Both new options plus --show-unchanged-values.
6. Existing sensitive-value authorization option on synthetic masked values.

The UAT Tester obtains platform-rendered evidence through repository UAT skills.
If a supported platform is unavailable, record that limitation in uat-report.md;
do not claim platform visual validation from a Markdown snapshot.

## Test Steps and Review Expectations

1. Distinguish collapsed cards by exact string/numeric suffix and friendly identity.
   Inspect escaped keys/text for readable preserved identity. Overrides affect
   unmapped fallback only; mapped azapi/msgraph/azurerm/provider-owned content stays
   consistent with defaults.
2. Inspect imports/moves in Refactoring Summary: full addresses always remain,
   friendly context is secondary, and default masked secrets never appear.
3. Expand imports: empty/housekeeping-only before state explains full desired state
   before attributes; populated imports omit that note. Missing body history alone
   does not describe an import as newly created.
4. Expand msgraph cards and Provider settings: unchanged settings remain accessible
   and secondary in both unchanged-value modes. Creates/empty imports retain
   desired settings. Added/removed/changed settings appear in primary diff/counts
   once, without duplicate entries.
5. Inspect computed attributes: pseudo-only hints use the plain known-after-apply
   marker; specific each.value, static resource, variable, and local hints remain.
   Sensitive computed values preserve masking.
6. Compare default/filtered outputs: default retains no-ops, filtering retains
   changed/create/delete values and protection, and removes empty output sections
   and output-only module groups.
7. On every target inspect label readability, secondary settings access, intact
   card structure, import context, ordinary/large values, and tables. Record broken
   presentation or target degradation as failures.

## Acceptance Evidence

Record fixture paths, generation commands/options, platform evidence, per-scenario
results, and Maintainer feedback in uat-report.md. Link failures to test cases and
success criteria. The workflow driver owns the UAT gate decision.
