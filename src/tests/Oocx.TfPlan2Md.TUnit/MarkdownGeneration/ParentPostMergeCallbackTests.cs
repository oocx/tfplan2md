using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for the parent post-merge callback mechanism.
/// Related issue: docs/issues/070-parent-child-summary-member-counts/analysis.md.
/// </summary>
public class ParentPostMergeCallbackTests
{
    /// <summary>
    /// Sample parent resource type for test data.
    /// </summary>
    private const string ParentType = "test_parent";

    /// <summary>
    /// Sample child resource type for test data.
    /// </summary>
    private const string ChildType = "test_child";

    /// <summary>
    /// Verifies that a single callback is invoked after parent-child merging.
    /// </summary>
    [Test]
    public void Build_WithRegisteredCallback_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        ParentPostMergeCallback callback = (changes, mapper) => { callbackInvoked = true; };

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder();
        builder.RegisterPostMergeCallback(callback);

        // Act
        builder.Build(plan);

        // Assert
        callbackInvoked.Should().BeTrue("callback should be invoked after parent-child merging");
    }

    /// <summary>
    /// Verifies that multiple callbacks are invoked in order of registration.
    /// </summary>
    [Test]
    public void Build_WithMultipleCallbacks_InvokesInOrder()
    {
        // Arrange
        var invocationOrder = new List<int>();
        ParentPostMergeCallback callback1 = (changes, mapper) => { invocationOrder.Add(1); };
        ParentPostMergeCallback callback2 = (changes, mapper) => { invocationOrder.Add(2); };
        ParentPostMergeCallback callback3 = (changes, mapper) => { invocationOrder.Add(3); };

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder();
        builder.RegisterPostMergeCallback(callback1);
        builder.RegisterPostMergeCallback(callback2);
        builder.RegisterPostMergeCallback(callback3);

        // Act
        builder.Build(plan);

        // Assert
        invocationOrder.Should().Equal([1, 2, 3], "callbacks should be invoked in order of registration");
    }

    /// <summary>
    /// Verifies that callbacks receive the correct list of resource changes after merging.
    /// </summary>
    [Test]
    public void Build_CallbackReceivesCorrectChanges_AfterMerging()
    {
        // Arrange
        List<ResourceChangeModel>? receivedChanges = null;
        ParentPostMergeCallback callback = (changes, mapper) => { receivedChanges = changes; };

        var plan = BuildPlanWithParentAndChild();
        var providerRegistry = CreateProviderRegistryWithParentChildRelationship();
        var builder = new ReportModelBuilder(providerRegistry: providerRegistry);
        builder.RegisterPostMergeCallback(callback);

        // Act
        _ = builder.Build(plan);

        // Assert
        receivedChanges.Should().NotBeNull("callback should receive resource changes");
        receivedChanges!.Should().HaveCount(1, "child should be merged into parent");
        receivedChanges[0].Type.Should().Be(ParentType, "only parent should remain after merging");
        receivedChanges[0].ChildResourceGroups.Should().ContainSingle("parent should have child resources merged");
    }

    /// <summary>
    /// Verifies that callbacks are only invoked for parents with children.
    /// </summary>
    [Test]
    public void Build_WithoutChildren_DoesNotInvokeCallbackForResourcesWithoutChildren()
    {
        // Arrange
        var callbackInvokedForParentsWithChildren = false;
        ParentPostMergeCallback callback = (changes, mapper) =>
        {
            var hasParentsWithChildren = changes.Any(c => c.ChildResourceGroups.Count > 0);
            if (hasParentsWithChildren)
            {
                callbackInvokedForParentsWithChildren = true;
            }
        };

        // Plan with only standalone parent (no children)
        var plan = BuildPlanWithStandaloneParent();
        var builder = new ReportModelBuilder();
        builder.RegisterPostMergeCallback(callback);

        // Act
        _ = builder.Build(plan);

        // Assert - Callback is always invoked, but no parents have children
        callbackInvokedForParentsWithChildren.Should().BeFalse("callback should not see parents with children when none exist");
    }

    /// <summary>
    /// Verifies that callback exceptions do not prevent other callbacks from executing.
    /// </summary>
    [Test]
    public void Build_CallbackThrowsException_DoesNotBreakOtherCallbacks()
    {
        // Arrange
        var callback1Invoked = false;
        var callback3Invoked = false;

        ParentPostMergeCallback callback1 = (changes, mapper) => { callback1Invoked = true; };
        ParentPostMergeCallback callback2 = (changes, mapper) => { throw new InvalidOperationException("Test exception"); };
        ParentPostMergeCallback callback3 = (changes, mapper) => { callback3Invoked = true; };

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder();
        builder.RegisterPostMergeCallback(callback1);
        builder.RegisterPostMergeCallback(callback2);
        builder.RegisterPostMergeCallback(callback3);

        // Act
        builder.Build(plan);

        // Assert
        callback1Invoked.Should().BeTrue("first callback should execute");
        callback3Invoked.Should().BeTrue("third callback should execute despite second callback throwing");
    }

    /// <summary>
    /// Verifies that callbacks receive the principal mapper instance.
    /// </summary>
    [Test]
    public void Build_CallbackReceivesPrincipalMapper()
    {
        // Arrange
        IPrincipalMapper? receivedMapper = null;
        ParentPostMergeCallback callback = (changes, mapper) => { receivedMapper = mapper; };

        var principals = new Dictionary<string, string> { ["user-1"] = "Alice" };
        var principalTypes = new Dictionary<string, string> { ["user-1"] = "User" };
        var principalMapper = new PrincipalMapper(principals, principalTypes);

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder(principalMapper: principalMapper);
        builder.RegisterPostMergeCallback(callback);

        // Act
        builder.Build(plan);

        // Assert
        receivedMapper.Should().NotBeNull("callback should receive principal mapper");
        receivedMapper.Should().BeOfType<PrincipalMapper>("callback should receive the actual mapper instance");
    }

    /// <summary>
    /// Verifies that callbacks work when no principal mapper is provided.
    /// </summary>
    [Test]
    public void Build_WithoutPrincipalMapper_CallbackReceivesNullPrincipalMapper()
    {
        // Arrange
        IPrincipalMapper? receivedMapper = null;
        ParentPostMergeCallback callback = (changes, mapper) => { receivedMapper = mapper; };

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder(principalMapper: null);
        builder.RegisterPostMergeCallback(callback);

        // Act
        builder.Build(plan);

        // Assert
        receivedMapper.Should().NotBeNull("callback should receive a non-null mapper instance");
        receivedMapper.Should().BeOfType<NullPrincipalMapper>("callback should receive NullPrincipalMapper when none is provided");
    }

    /// <summary>
    /// Verifies that building without any callbacks registered works correctly.
    /// </summary>
    [Test]
    public void Build_WithoutCallbacks_CompletesSuccessfully()
    {
        // Arrange
        var plan = BuildPlanWithParentAndChild();
        var providerRegistry = CreateProviderRegistryWithParentChildRelationship();
        var builder = new ReportModelBuilder(providerRegistry: providerRegistry);
        // No callbacks registered

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Should().NotBeNull("build should complete successfully without callbacks");
        model.Changes.Should().ContainSingle("child should be merged even without callbacks");
        model.Changes[0].ChildResourceGroups.Should().ContainSingle("parent-child merging should work without callbacks");
    }

    /// <summary>
    /// Verifies that callbacks can modify resource change models.
    /// </summary>
    [Test]
    public void Build_CallbackModifiesResourceChange_ChangesAreReflected()
    {
        // Arrange
        const string customSuffix = " [MODIFIED]";
        ParentPostMergeCallback callback = (changes, mapper) =>
        {
            foreach (var change in changes)
            {
                change.SummaryHtml += customSuffix;
            }
        };

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder();
        builder.RegisterPostMergeCallback(callback);

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Changes.Should().OnlyContain(c => c.SummaryHtml!.EndsWith(customSuffix),
            "callback modifications should be reflected in the final model");
    }

    /// <summary>
    /// Verifies that callbacks are invoked even when there are no parent-child relationships registered.
    /// </summary>
    [Test]
    public void Build_WithoutParentChildRelationships_StillInvokesCallbacks()
    {
        // Arrange
        var callbackInvoked = false;
        ParentPostMergeCallback callback = (changes, mapper) => { callbackInvoked = true; };

        var plan = BuildPlanWithStandaloneParent();
        var builder = new ReportModelBuilder(); // No provider registry, so no parent-child relationships
        builder.RegisterPostMergeCallback(callback);

        // Act
        builder.Build(plan);

        // Assert
        callbackInvoked.Should().BeTrue("callbacks should be invoked even without parent-child relationships");
    }

    /// <summary>
    /// Verifies that callbacks registered via provider registry are invoked.
    /// </summary>
    [Test]
    public void Build_CallbacksRegisteredViaProviderRegistry_AreInvoked()
    {
        // Arrange
        var callbackInvoked = false;
        var testModule = new TestProviderModule((builder) =>
        {
            builder.RegisterPostMergeCallback((changes, mapper) => { callbackInvoked = true; });
        });

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(testModule);

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder(providerRegistry: providerRegistry);

        // Act
        builder.Build(plan);

        // Assert
        callbackInvoked.Should().BeTrue("callbacks registered via provider registry should be invoked");
    }

    /// <summary>
    /// Verifies that multiple providers can register callbacks independently.
    /// </summary>
    [Test]
    public void Build_MultipleProvidersRegisterCallbacks_AllAreInvoked()
    {
        // Arrange
        var callback1Invoked = false;
        var callback2Invoked = false;

        var testModule1 = new TestProviderModule((builder) =>
        {
            builder.RegisterPostMergeCallback((changes, mapper) => { callback1Invoked = true; });
        });

        var testModule2 = new TestProviderModule((builder) =>
        {
            builder.RegisterPostMergeCallback((changes, mapper) => { callback2Invoked = true; });
        });

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(testModule1);
        providerRegistry.RegisterProvider(testModule2);

        var plan = BuildPlanWithParentAndChild();
        var builder = new ReportModelBuilder(providerRegistry: providerRegistry);

        // Act
        builder.Build(plan);

        // Assert
        callback1Invoked.Should().BeTrue("first provider's callback should be invoked");
        callback2Invoked.Should().BeTrue("second provider's callback should be invoked");
    }

    /// <summary>
    /// Builds a Terraform plan with a parent resource and a separate child resource.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithParentAndChild()
    {
        var parentAfter = JsonDocument.Parse("""
        {
            "id": "parent-1",
            "name": "Test Parent"
        }
        """).RootElement;

        var childAfter = JsonDocument.Parse("""
        {
            "parent_id": "parent-1",
            "name": "Test Child"
        }
        """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    $"{ParentType}.parent1",
                    null,
                    "managed",
                    ParentType,
                    "parent1",
                    "test",
                    new Change(["create"], null, parentAfter, null, null, null)),
                new ResourceChange(
                    $"{ChildType}.child1",
                    null,
                    "managed",
                    ChildType,
                    "child1",
                    "test",
                    new Change(["create"], null, childAfter, null, null, null))
            });
    }

    /// <summary>
    /// Builds a Terraform plan with a standalone parent resource (no children).
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithStandaloneParent()
    {
        var parentAfter = JsonDocument.Parse("""
        {
            "id": "parent-1",
            "name": "Test Parent"
        }
        """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    $"{ParentType}.parent1",
                    null,
                    "managed",
                    ParentType,
                    "parent1",
                    "test",
                    new Change(["create"], null, parentAfter, null, null, null))
            });
    }

    /// <summary>
    /// Creates a provider registry with a parent-child relationship configured.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistryWithParentChildRelationship()
    {
        var testModule = new TestProviderModule(null, (registry) =>
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = ParentType,
                ChildResourceType = ChildType,
                ChildReferenceAttribute = "parent_id",
                ParentIdAttribute = "id",
                ChildGroupLabel = "Children",
                TableColumns = [new ChildTableColumn("Name", "name")],
                RowExtractor = new TestChildRowExtractor()
            });
        });

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(testModule);
        return providerRegistry;
    }

    /// <summary>
    /// Test provider module for callback testing.
    /// </summary>
    private sealed class TestProviderModule : IProviderModule
    {
        private readonly Action<ReportModelBuilder>? _callbackRegistration;
        private readonly Action<IParentChildRelationshipRegistry>? _relationshipRegistration;

        public TestProviderModule(
            Action<ReportModelBuilder>? callbackRegistration = null,
            Action<IParentChildRelationshipRegistry>? relationshipRegistration = null)
        {
            _callbackRegistration = callbackRegistration;
            _relationshipRegistration = relationshipRegistration;
        }

        public string ProviderName => "test";
        public string TemplateResourcePrefix => "Test.";

        public void RegisterHelpers(Scriban.Runtime.ScriptObject scriptObject) { }
        public void RegisterFactories(IResourceViewModelFactoryRegistry registry) { }
        public void RegisterValueFormatters(ValueFormatterRegistry registry) { }
        public void RegisterIconProviders(IconProviderRegistry registry) { }
        public void RegisterResourceModelMappers(ResourceModelMapperRegistry registry) { }

        public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
        {
            _relationshipRegistration?.Invoke(registry);
        }

        public void RegisterPostMergeCallbacks(ReportModelBuilder builder)
        {
            _callbackRegistration?.Invoke(builder);
        }
    }

    /// <summary>
    /// Test row extractor for parent-child testing.
    /// </summary>
    private sealed class TestChildRowExtractor : IChildRowExtractor
    {
        public IReadOnlyDictionary<string, string> ExtractDiffRow(object? beforeState, object? afterState, string providerName, ValueFormatterRegistry? valueFormatterRegistry, IconProviderRegistry? iconProviderRegistry, LargeValueFormat largeValueFormat) => ExtractRow(afterState, providerName, valueFormatterRegistry, iconProviderRegistry);

        public IReadOnlyDictionary<string, string> ExtractRow(
            object? childState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry)
        {
            var name = childState is JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Object
                && element.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() ?? "Unknown"
                : "Unknown";

            return new Dictionary<string, string> { ["name"] = name };
        }
    }
}
