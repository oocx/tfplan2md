using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureAD;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureAD;

/// <summary>
/// Integration tests for Azure AD group member computed known-after-apply scenarios.
/// </summary>
/// <remarks>
/// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
/// Related test plan: docs/features/102-known-after-apply-rendering/test-plan.md (TC-12 to TC-16).
/// </remarks>
public class AzureAdGroupMemberComputedTests
{
    private const string KnownAfterApply = "(known after apply)";
    private const string GroupObjectIdAttribute = "group_object_id";
    private const string MemberObjectIdAttribute = "member_object_id";

    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Scenario1_AllUnknown_NoConfiguration_UsesKnownAfterApplyFallback()
    {
        var model = BuildAzureAdModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azuread_group_member.all_unknown",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azuread_group_member",
                  "name": "all_unknown",
                  "provider_name": "registry.terraform.io/hashicorp/azuread",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "group_object_id": null, "member_object_id": null, "id": null },
                    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """);

        var change = model.Changes.Single();
        change.SummaryHtml.Should().Contain($"<code>{KnownAfterApply}</code> → <code>{KnownAfterApply}</code>");
        change.AttributeChanges.Should().Contain(a => a.Name == GroupObjectIdAttribute && a.After == KnownAfterApply);
        change.AttributeChanges.Should().Contain(a => a.Name == MemberObjectIdAttribute && a.After == KnownAfterApply);
        change.AttributeChanges.Should().Contain(a => a.Name == "id" && a.After == KnownAfterApply);
    }

    [Test]
    public void Scenario2_StaticReferences_AppearInSummaryAndTable()
    {
        var model = BuildAzureAdModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azuread_group_member.platform_admin_member",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azuread_group_member",
                  "name": "platform_admin_member",
                  "provider_name": "registry.terraform.io/hashicorp/azuread",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "group_object_id": null, "member_object_id": null, "id": null },
                    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ],
              "configuration": {
                "root_module": {
                  "resources": [
                    {
                      "address": "azuread_group_member.platform_admin_member",
                      "mode": "managed",
                      "type": "azuread_group_member",
                      "name": "platform_admin_member",
                      "expressions": {
                        "group_object_id": { "references": ["azuread_group.platform_engineers.object_id", "azuread_group.platform_engineers"] },
                        "member_object_id": { "references": ["azuread_user.admin.object_id", "azuread_user.admin"] }
                      }
                    }
                  ]
                }
              }
            }
            """);

        var change = model.Changes.Single();
        change.SummaryHtml.Should().Contain("<code>azuread_group.platform_engineers</code> → <code>azuread_user.admin</code>");
        change.AttributeChanges.Should().Contain(a => a.Name == GroupObjectIdAttribute && a.After == "(known after apply: azuread_group.platform_engineers)");
        change.AttributeChanges.Should().Contain(a => a.Name == MemberObjectIdAttribute && a.After == "(known after apply: azuread_user.admin)");
        change.AttributeChanges.Should().Contain(a => a.Name == "id" && a.After == KnownAfterApply);
    }

    [Test]
    public void Scenario3_ForEachStringKeyWithoutStaticRef_UsesInstanceKeySummary()
    {
        var model = BuildAzureAdModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azuread_group_member.user_groups[\"team-example - user@example.de\"]",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azuread_group_member",
                  "name": "user_groups",
                  "provider_name": "registry.terraform.io/hashicorp/azuread",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "group_object_id": null, "member_object_id": null, "id": null },
                    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ],
              "configuration": {
                "root_module": {
                  "resources": [
                    {
                      "address": "azuread_group_member.user_groups",
                      "mode": "managed",
                      "type": "azuread_group_member",
                      "name": "user_groups",
                      "expressions": {
                        "group_object_id": { "references": ["each.value.group_object_id", "each.value"] },
                        "member_object_id": { "references": ["each.value.user_object_id", "each.value"] }
                      }
                    }
                  ]
                }
              }
            }
            """);

        var change = model.Changes.Single();
        change.SummaryHtml.Should().Contain("<code>\"team-example - user@example.de\"</code> → <code>\"team-example - user@example.de\"</code>");
        change.AttributeChanges.Should().Contain(a => a.Name == GroupObjectIdAttribute && a.After == "(known after apply: each.value.group_object_id)");
        change.AttributeChanges.Should().Contain(a => a.Name == MemberObjectIdAttribute && a.After == "(known after apply: each.value.user_object_id)");
    }

    [Test]
    public void Scenario4_MixedKnownAndComputed_UsesKnownAndComputedSummary()
    {
        var model = BuildAzureAdModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azuread_group_member.platform_admin_member",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azuread_group_member",
                  "name": "platform_admin_member",
                  "provider_name": "registry.terraform.io/hashicorp/azuread",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "group_object_id": null, "member_object_id": "user-200", "id": null },
                    "after_unknown": { "group_object_id": true, "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ]
            }
            """);

        var change = model.Changes.Single();
        change.SummaryHtml.Should().Contain($"<code>{KnownAfterApply}</code> → <code>user-200</code>");
        change.AttributeChanges.Should().Contain(a => a.Name == GroupObjectIdAttribute && a.After == KnownAfterApply);
        change.AttributeChanges.Should().Contain(a => a.Name == MemberObjectIdAttribute && a.After == "user-200");
    }

    [Test]
    public void Scenario5_NumericInstanceKeyAppendedToStaticGroupReference()
    {
        var model = BuildAzureAdModel("""
            {
              "format_version": "1.2",
              "terraform_version": "1.14.0",
              "resource_changes": [
                {
                  "address": "azuread_group_member.members[0]",
                  "module_address": null,
                  "mode": "managed",
                  "type": "azuread_group_member",
                  "name": "members",
                  "provider_name": "registry.terraform.io/hashicorp/azuread",
                  "change": {
                    "actions": ["create"],
                    "before": null,
                    "after": { "group_object_id": null, "member_object_id": null, "id": null },
                    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true },
                    "before_sensitive": {},
                    "after_sensitive": {}
                  }
                }
              ],
              "configuration": {
                "root_module": {
                  "resources": [
                    {
                      "address": "azuread_group_member.members",
                      "mode": "managed",
                      "type": "azuread_group_member",
                      "name": "members",
                      "expressions": {
                        "group_object_id": { "references": ["azuread_group.admins.object_id", "azuread_group.admins"] },
                        "member_object_id": { "references": ["count.index", "var.users"] }
                      }
                    }
                  ]
                }
              }
            }
            """);

        var change = model.Changes.Single();
        change.SummaryHtml.Should().Contain("<code>azuread_group.admins[0]</code> → <code>(known after apply)</code>");
        change.AttributeChanges.Should().Contain(a => a.Name == GroupObjectIdAttribute && a.After == "(known after apply: azuread_group.admins)");
        change.AttributeChanges.Should().Contain(a => a.Name == MemberObjectIdAttribute && a.After == "(known after apply: var.users)");
    }

    private ReportModel BuildAzureAdModel(string json)
    {
        var plan = _parser.Parse(json);

        var principalMapper = new PrincipalMapper(
            new Dictionary<string, string>(),
            new Dictionary<string, string>());

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());

        return new ReportModelBuilder(
            principalMapper: principalMapper,
            providerRegistry: providerRegistry).Build(plan);
    }
}
