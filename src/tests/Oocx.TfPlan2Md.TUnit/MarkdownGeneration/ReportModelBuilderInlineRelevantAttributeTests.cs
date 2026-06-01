using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderInlineRelevantAttributeTests
{
    [Test]
    public void Build_ReplacePathMatchingRelevantAttribute_CreatesForcedReplacementAnnotation()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { network_interface_ids = new List<string> { "nic-old" } },
                    new { network_interface_ids = new List<string> { "nic-new" } },
                    [["network_interface_ids", 0]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["network_interface_ids"] = ["example_resource.nic.id", "example_resource.nic"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.nic", ["id"])
            ]);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.ForcedReplacementAnnotations.Should().ContainSingle();
        change.ForcedReplacementAnnotations[0].LocalAttribute.Should().Be("network_interface_ids");
        change.ForcedReplacementAnnotations[0].UpstreamResource.Should().Be("example_resource.nic");
        change.ForcedReplacementAnnotations[0].UpstreamAttributePath.Should().Be("id");
        change.ForcedReplacementAnnotations[0].IsChangingInThisPlan.Should().BeFalse();
    }

    [Test]
    public void Build_ForcedReplacementAnnotation_UsesLocalAttributeUpstreamResourceAndAttributePath()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.api",
                    ["delete", "create"],
                    new { app_settings = new { KV_URI = "old" } },
                    new { app_settings = new { KV_URI = "new" } },
                    [["app_settings"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.api", new Dictionary<string, string[]>
                {
                    ["app_settings"] = ["example_resource.kv.vault_uri"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.kv", ["vault_uri"])
            ]);

        var annotation = model.Changes.Should().ContainSingle().Subject.ForcedReplacementAnnotations.Should().ContainSingle().Subject;
        annotation.LocalAttribute.Should().Be("app_settings");
        annotation.UpstreamResource.Should().Be("example_resource.kv");
        annotation.UpstreamAttributePath.Should().Be("vault_uri");
    }

    [Test]
    public void Build_UpstreamReplacement_SetsIsChangingInThisPlanTrue()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { network_interface_ids = new List<string> { "nic-old" } },
                    new { network_interface_ids = new List<string> { "nic-new" } },
                    [["network_interface_ids", 0]]),
                MakeResourceChange(
                    "example_resource.nic",
                    ["delete", "create"],
                    new { id = "nic-old" },
                    new { id = "nic-new" })
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["network_interface_ids"] = ["example_resource.nic.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.nic", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle().Which.IsChangingInThisPlan.Should().BeTrue();
    }

    [Test]
    public void Build_UpstreamDelete_SetsIsChangingInThisPlanTrue()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { input_id = "old" },
                    new { input_id = "new" },
                    [["input_id"]]),
                MakeResourceChange(
                    "example_resource.input",
                    ["delete"],
                    new { id = "old" },
                    null)
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["input_id"] = ["example_resource.input.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.input", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle().Which.IsChangingInThisPlan.Should().BeTrue();
    }

    [Test]
    public void Build_UpstreamUpdate_DoesNotSetIsChangingInThisPlan()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { input_id = "old" },
                    new { input_id = "new" },
                    [["input_id"]]),
                MakeResourceChange(
                    "example_resource.input",
                    ["update"],
                    new { id = "old" },
                    new { id = "new" })
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["input_id"] = ["example_resource.input.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.input", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle().Which.IsChangingInThisPlan.Should().BeFalse();
    }

    [Test]
    public void Build_CorrelatedNonReplacePathEntries_GoToDependsOnAnnotations()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.api",
                    ["delete", "create"],
                    new { app_settings = new { uri = "old" }, identity = new { tenant_id = "old" } },
                    new { app_settings = new { uri = "new" }, identity = new { tenant_id = "new" } },
                    [["app_settings"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.api", new Dictionary<string, string[]>
                {
                    ["app_settings"] = ["example_resource.kv.vault_uri"],
                    ["identity"] = ["data.example_data.current.tenant_id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.kv", ["vault_uri"]),
                new RelevantAttribute("data.example_data.current", ["tenant_id"])
            ]);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.ForcedReplacementAnnotations.Should().ContainSingle().Which.UpstreamResource.Should().Be("example_resource.kv");
        change.DependsOnAnnotations.Should().ContainSingle().Which.UpstreamResource.Should().Be("data.example_data.current");
        change.DependsOnAnnotations[0].UpstreamAttributePath.Should().Be("tenant_id");
    }

    [Test]
    public void Build_AllRelevantAttributesCorrelated_LeavesFallbackEmpty()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.api",
                    ["delete", "create"],
                    new { app_settings = new { uri = "old" }, identity = new { tenant_id = "old" } },
                    new { app_settings = new { uri = "new" }, identity = new { tenant_id = "new" } },
                    [["app_settings"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.api", new Dictionary<string, string[]>
                {
                    ["app_settings"] = ["example_resource.kv.vault_uri"],
                    ["identity"] = ["data.example_data.current.tenant_id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.kv", ["vault_uri"]),
                new RelevantAttribute("data.example_data.current", ["tenant_id"])
            ]);

        model.RelevantAttributes.Should().BeEmpty();
    }

    [Test]
    public void Build_ResourceWithForcedAndDependency_PopulatesBothLists()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.api",
                    ["delete", "create"],
                    new { app_settings = new { uri = "old" }, identity = new { tenant_id = "old" } },
                    new { app_settings = new { uri = "new" }, identity = new { tenant_id = "new" } },
                    [["app_settings"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.api", new Dictionary<string, string[]>
                {
                    ["app_settings"] = ["example_resource.kv.vault_uri"],
                    ["identity"] = ["data.example_data.current.tenant_id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.kv", ["vault_uri"]),
                new RelevantAttribute("data.example_data.current", ["tenant_id"])
            ]);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.ForcedReplacementAnnotations.Should().NotBeEmpty();
        change.DependsOnAnnotations.Should().NotBeEmpty();
    }

    [Test]
    public void Build_UpdateResource_DoesNotReceiveAnnotations()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.api",
                    ["update"],
                    new { identity = new { tenant_id = "old" } },
                    new { identity = new { tenant_id = "new" } })
            ],
            configuration: MakeConfiguration(
                ("example_resource.api", new Dictionary<string, string[]>
                {
                    ["identity"] = ["data.example_data.current.tenant_id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("data.example_data.current", ["tenant_id"])
            ]);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.ForcedReplacementAnnotations.Should().BeEmpty();
        change.DependsOnAnnotations.Should().BeEmpty();
        model.RelevantAttributes.Should().ContainSingle().Which.Resource.Should().Be("data.example_data.current");
    }

    [Test]
    public void Build_DriftResource_DoesNotReceiveAnnotations()
    {
        var model = BuildModel(
            changes: [],
            drift:
            [
                MakeResourceChange(
                    "example_resource.drifted",
                    ["delete", "create"],
                    new { input_id = "old" },
                    new { input_id = "new" },
                    [["input_id"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.drifted", new Dictionary<string, string[]>
                {
                    ["input_id"] = ["example_resource.source.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.source", ["id"])
            ]);

        model.Drift.Should().ContainSingle();
        model.Drift[0].ForcedReplacementAnnotations.Should().BeEmpty();
        model.Drift[0].DependsOnAnnotations.Should().BeEmpty();
        model.RelevantAttributes.Should().ContainSingle().Which.Resource.Should().Be("example_resource.source");
    }

    [Test]
    public void Build_UncorrelatedRelevantAttributes_RemainInFallback()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.api",
                    ["delete", "create"],
                    new { input = "old" },
                    new { input = "new" },
                    [["input"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.api", new Dictionary<string, string[]>
                {
                    ["input"] = ["example_resource.matched.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.matched", ["id"]),
                new RelevantAttribute("example_resource.other", ["name"])
            ]);

        model.RelevantAttributes.Should().ContainSingle();
        model.RelevantAttributes[0].Resource.Should().Be("example_resource.other");
    }

    [Test]
    public void Build_NoRelevantAttributes_LeavesAnnotationsAndFallbackEmpty()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.api",
                    ["delete", "create"],
                    new { input = "old" },
                    new { input = "new" },
                    [["input"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.api", new Dictionary<string, string[]>
                {
                    ["input"] = ["example_resource.matched.id"]
                })));

        var change = model.Changes.Should().ContainSingle().Subject;
        change.ForcedReplacementAnnotations.Should().BeEmpty();
        change.DependsOnAnnotations.Should().BeEmpty();
        model.RelevantAttributes.Should().BeEmpty();
    }

    [Test]
    public void Build_ManagedResourceReferenceWithAttributeSuffix_Correlates()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { source_id = "old" },
                    new { source_id = "new" },
                    [["source_id"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["source_id"] = ["example_resource.source.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.source", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle();
    }

    [Test]
    public void Build_DataSourceReferenceWithAttributeSuffix_Correlates()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { tenant_id = "old" },
                    new { tenant_id = "new" },
                    [["tenant_id"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["tenant_id"] = ["data.example_data.current.tenant_id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("data.example_data.current", ["tenant_id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle();
    }

    [Test]
    public void Build_CaseInsensitiveMatches_CorrelateSuccessfully()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { source_id = "old" },
                    new { source_id = "new" },
                    [["source_id"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["source_id"] = ["EXAMPLE_RESOURCE.SOURCE.ID"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.source", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle();
    }

    [Test]
    public void Build_MultipleReplacePaths_CreateMultipleForcedAnnotations()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { source_id = "old", backup_id = "old2" },
                    new { source_id = "new", backup_id = "new2" },
                    [["source_id"], ["backup_id"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["source_id"] = ["example_resource.source.id"],
                    ["backup_id"] = ["example_resource.backup.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.source", ["id"]),
                new RelevantAttribute("example_resource.backup", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().HaveCount(2);
    }

    [Test]
    public void Build_DeleteResource_TreatedLikeReplaceForAnnotations()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete"],
                    new { source_id = "old" },
                    null,
                    [["source_id"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["source_id"] = ["example_resource.source.id"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.source", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle();
    }

    [Test]
    public void Build_ExactResourceAddressReference_CorrelatesWithoutAttributeSuffix()
    {
        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "example_resource.web",
                    ["delete", "create"],
                    new { source = "old" },
                    new { source = "new" },
                    [["source"]])
            ],
            configuration: MakeConfiguration(
                ("example_resource.web", new Dictionary<string, string[]>
                {
                    ["source"] = ["example_resource.source"]
                })),
            relevantAttributes:
            [
                new RelevantAttribute("example_resource.source", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle().Which.UpstreamResource.Should().Be("example_resource.source");
    }

    [Test]
    public void Build_ModulePrefixedReference_CorrelatesToModuleScopedResource()
    {
        var configuration = JsonSerializer.SerializeToElement(new
        {
            root_module = new
            {
                module_calls = new Dictionary<string, object>
                {
                    ["app"] = new
                    {
                        module = new
                        {
                            resources = new object[]
                            {
                                new
                                {
                                    address = "example_resource.web",
                                    expressions = new Dictionary<string, object>
                                    {
                                        ["source_id"] = new { references = new List<string> { "example_resource.source.id" } }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });

        var model = BuildModel(
            changes:
            [
                MakeResourceChange(
                    "module.app.example_resource.web",
                    ["delete", "create"],
                    new { source_id = "old" },
                    new { source_id = "new" },
                    [["source_id"]])
            ],
            configuration: configuration,
            relevantAttributes:
            [
                new RelevantAttribute("module.app.example_resource.source", ["id"])
            ]);

        model.Changes[0].ForcedReplacementAnnotations.Should().ContainSingle().Which.UpstreamResource.Should().Be("module.app.example_resource.source");
    }

    private static ReportModel BuildModel(
        IReadOnlyList<ResourceChange> changes,
        JsonElement? configuration = null,
        IReadOnlyList<RelevantAttribute>? relevantAttributes = null,
        IReadOnlyList<ResourceChange>? drift = null)
    {
        var plan = new TerraformPlan(
            "1.2",
            "1.14.0",
            changes,
            Configuration: configuration,
            ResourceDrift: drift,
            RelevantAttributes: relevantAttributes);

        return new ReportModelBuilder().Build(plan);
    }

    private static ResourceChange MakeResourceChange(
        string address,
        IReadOnlyList<string> actions,
        object? before,
        object? after,
        IReadOnlyList<IReadOnlyList<object>>? replacePaths = null)
    {
        var segments = address.Split('.');
        var type = segments[^2];
        var name = segments[^1];

        return new ResourceChange(
            address,
            null,
            "managed",
            type,
            name,
            "registry.terraform.io/example/example",
            new Change(
                actions,
                before is null ? null : JsonSerializer.SerializeToElement(before),
                after is null ? null : JsonSerializer.SerializeToElement(after),
                new { },
                new { },
                new { },
                replacePaths));
    }

    private static JsonElement MakeConfiguration(params (string Address, Dictionary<string, string[]> Expressions)[] resources)
    {
        return JsonSerializer.SerializeToElement(new
        {
            root_module = new
            {
                resources = resources.Select(resource => new
                {
                    address = resource.Address,
                    expressions = resource.Expressions.ToDictionary(
                        expression => expression.Key,
                        expression => (object)new { references = expression.Value })
                }).ToArray()
            }
        });
    }
}
