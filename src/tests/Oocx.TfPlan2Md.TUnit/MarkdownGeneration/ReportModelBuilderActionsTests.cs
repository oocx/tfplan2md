using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Builder tests covering Terraform 1.14+ action-invocation distribution across the
/// per-resource <c>Actions</c> list, the invoke-mode bucket, and the lifecycle-orphan
/// bucket of <see cref="Oocx.TfPlan2Md.MarkdownGeneration.Models.OtherActionsModel"/>.
/// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md (Task 9).
/// </summary>
public class ReportModelBuilderActionsTests
{
    private static ResourceChange MakeUpdateChange(string address)
    {
        return new ResourceChange(
            address,
            null,
            "managed",
            "example_resource",
            address.Split('.')[1],
            "registry.terraform.io/example/example",
            new Change(
                ["update"],
                JsonDocument.Parse("{\"name\":\"old\"}").RootElement,
                JsonDocument.Parse("{\"name\":\"new\"}").RootElement,
                null,
                null,
                null));
    }

    private static ActionInvocation MakeLifecycleAction(string actionAddress, string triggeringAddress)
    {
        return new ActionInvocation(
            actionAddress,
            "example_action",
            actionAddress.Split('.')[1],
            "registry.terraform.io/example/example",
            LifecycleActionTrigger: new LifecycleActionTrigger(triggeringAddress, "after_create"));
    }

    private static ActionInvocation MakeInvokeAction(string actionAddress)
    {
        return new ActionInvocation(
            actionAddress,
            "example_action",
            actionAddress.Split('.')[1],
            "registry.terraform.io/example/example",
            InvokeActionTrigger: new InvokeActionTrigger("explicit"));
    }

    private static TerraformPlan MakePlan(
        IReadOnlyList<ActionInvocation>? actionInvocations = null,
        IReadOnlyList<ActionInvocation>? deferredActionInvocations = null,
        params ResourceChange[] changes)
    {
        return new TerraformPlan(
            "1.2",
            "1.14.0",
            changes,
            ActionInvocations: actionInvocations,
            DeferredActionInvocations: deferredActionInvocations);
    }

    [Test]
    public void Build_LifecycleActionMatchingResource_AttachedInline()
    {
        var change = MakeUpdateChange("example_resource.kept");
        var action = MakeLifecycleAction("action.example_action.notify", "example_resource.kept");
        var plan = MakePlan(actionInvocations: new[] { action }, changes: change);

        var model = new ReportModelBuilder().Build(plan);

        var resource = model.Changes.Should().ContainSingle().Subject;
        resource.Actions.Should().HaveCount(1);
        resource.Actions[0].Invocation.Address.Should().Be("action.example_action.notify");
        resource.Actions[0].IsDeferred.Should().BeFalse();
        model.OtherActions.Should().BeNull();
    }

    [Test]
    public void Build_LifecycleActionWithoutMatchingResource_RoutedToOrphans()
    {
        var change = MakeUpdateChange("example_resource.kept");
        var action = MakeLifecycleAction("action.example_action.notify", "example_resource.missing");
        var plan = MakePlan(actionInvocations: new[] { action }, changes: change);

        var model = new ReportModelBuilder().Build(plan);

        model.Changes.Should().ContainSingle().Which.Actions.Should().BeEmpty();
        model.OtherActions.Should().NotBeNull();
        model.OtherActions!.LifecycleOrphanActions.Should().HaveCount(1);
        model.OtherActions.InvokeActions.Should().BeEmpty();
    }

    [Test]
    public void Build_InvokeAction_RoutedToInvokeBucket()
    {
        var change = MakeUpdateChange("example_resource.kept");
        var action = MakeInvokeAction("action.example_action.run");
        var plan = MakePlan(actionInvocations: new[] { action }, changes: change);

        var model = new ReportModelBuilder().Build(plan);

        model.OtherActions.Should().NotBeNull();
        model.OtherActions!.InvokeActions.Should().HaveCount(1);
        model.OtherActions.LifecycleOrphanActions.Should().BeEmpty();
    }

    [Test]
    public void Build_DeferredAction_FlaggedAsDeferred()
    {
        var change = MakeUpdateChange("example_resource.kept");
        var action = MakeLifecycleAction("action.example_action.notify", "example_resource.kept");
        var plan = MakePlan(deferredActionInvocations: new[] { action }, changes: change);

        var model = new ReportModelBuilder().Build(plan);

        var resource = model.Changes.Should().ContainSingle().Subject;
        resource.Actions.Should().HaveCount(1);
        resource.Actions[0].IsDeferred.Should().BeTrue();
    }

    [Test]
    public void Build_NoActionInvocations_LeavesOtherActionsNull()
    {
        var change = MakeUpdateChange("example_resource.kept");
        var plan = MakePlan(changes: change);

        var model = new ReportModelBuilder().Build(plan);

        model.OtherActions.Should().BeNull();
        model.Changes.Should().ContainSingle().Which.Actions.Should().BeEmpty();
    }
}
