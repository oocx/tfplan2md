using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Regression tests for action classification in the refactored report pipeline.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class ReportModelBuilderActionClassificationTests
{
    private const string ManagedMode = "managed";
    private const string EphemeralMode = "ephemeral";
    private const string ProviderName = "provider";

    [Test]
    public void Build_ForgetAction_ActionIsForget()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([TerraformActions.Forget]))
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Forget);
        change.Action.Should().NotBe(TerraformActions.NoOp);
    }

    [Test]
    public void Build_ForgetAction_CountedInDestroySummary()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([TerraformActions.Forget]))
            ]);

        var model = new ReportModelBuilder().Build(plan);

        model.Summary.ToDestroy.Count.Should().Be(1);
        model.Summary.ToChange.Count.Should().Be(0);
    }

    [Test]
    public void Build_UnknownAction_ActionIsUnknown()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change(["future-action"]))
            ]);

        var model = new ReportModelBuilder(ignoreAzureIdCaseChanges: false).Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Unknown);
        change.Action.Should().NotBe(TerraformActions.NoOp);
    }

    [Test]
    public void Build_OpenAction_ActionIsOpen()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [
                new(
                    "ephemeral.vault_kv_secret_v2.test",
                    null,
                    EphemeralMode,
                    "vault_kv_secret_v2",
                    "test",
                    ProviderName,
                    new Change([TerraformActions.Open]))
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Open);
        change.Action.Should().NotBe(TerraformActions.Unknown);
        change.ActionSymbol.Should().Be(ActionIcons.Add);
    }

    [Test]
    public void Build_ForgetThenCreateAction_ClassifiedAsReplace()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([TerraformActions.Forget, TerraformActions.Create]))
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Replace);
        change.Action.Should().NotBe(TerraformActions.Create);
        change.ActionSymbol.Should().Be(ActionIcons.Replace);
    }

    [Test]
    public void Build_CreateThenForgetAction_ClassifiedAsReplace()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([TerraformActions.Create, TerraformActions.Forget]))
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Replace);
        change.Action.Should().NotBe(TerraformActions.Create);
        change.ActionSymbol.Should().Be(ActionIcons.Replace);
    }
}
