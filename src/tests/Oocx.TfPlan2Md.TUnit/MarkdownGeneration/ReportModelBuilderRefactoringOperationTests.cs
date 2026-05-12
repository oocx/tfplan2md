using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Regression tests for refactoring operations after the report-pipeline extraction.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class ReportModelBuilderRefactoringOperationTests
{
    private const string ManagedMode = "managed";
    private const string ProviderName = "provider";

    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Build_NoRefactoring_RefactoringOperationsIsEmpty()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));

        var model = new ReportModelBuilder().Build(plan);

        model.RefactoringOperations.Should().BeEmpty();
    }

    [Test]
    public void Build_NoOpImport_IncludesChangeAndMarksAsReady()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/no-op-import.json"));

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.NoOp);
        change.ImportId.Should().NotBeNull();
        change.IsImportAlreadyApplied.Should().BeFalse();

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Operation.Should().Be("Import");
        operation.Status.Should().Be("Ready");
    }

    [Test]
    public void Build_RefactoringOperations_SortsImportsBeforeMovesAndWarningsFirst()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "type.move.ready",
                    null,
                    ManagedMode,
                    "type",
                    "move-ready",
                    ProviderName,
                    new Change([TerraformActions.Create]),
                    PreviousAddress: "module.old.type.move-ready"),
                new(
                    "type.import.ready",
                    null,
                    ManagedMode,
                    "type",
                    "import-ready",
                    ProviderName,
                    new Change([TerraformActions.Create]) { Importing = new Importing { Id = "id-ready" } }),
                new(
                    "type.import.noop",
                    null,
                    ManagedMode,
                    "type",
                    "import-noop",
                    ProviderName,
                    new Change([TerraformActions.NoOp]) { Importing = new Importing { Id = "id-noop" } }),
                new(
                    "type.move.noop",
                    null,
                    ManagedMode,
                    "type",
                    "move-noop",
                    ProviderName,
                    new Change([TerraformActions.NoOp]),
                    PreviousAddress: "module.old.type.move-noop")
            });

        var model = new ReportModelBuilder().Build(plan);

        model.RefactoringOperations
            .Select(operation => $"{operation.Operation}:{operation.Status}:{operation.Address}")
            .Should()
            .Equal(
                "Import:Ready:type.import.noop",
                "Import:Ready:type.import.ready",
                "Move:AlreadyApplied:type.move.noop",
                "Move:Ready:type.move.ready");
    }

    [Test]
    public void Build_ReadImport_IncludesChangeAndMarksAsReady()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/read-import.json"));

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Read);
        change.ImportId.Should().NotBeNull();
        change.IsImportAlreadyApplied.Should().BeFalse();

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Operation.Should().Be("Import");
        operation.Status.Should().Be("Ready");
    }

    [Test]
    public void Build_ImportWithReadAction_ActionIsRead()
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
                    new Change([TerraformActions.Read]) { Importing = new Importing { Id = "test-id" } })
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Read);
        change.IsImportAlreadyApplied.Should().BeFalse();
    }

    [Test]
    public void Build_ImportWithCreateAction_MarksAsReady()
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
                    new Change([TerraformActions.Create]) { Importing = new Importing { Id = "test-id" } })
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Create);
        change.IsImportAlreadyApplied.Should().BeFalse();
        model.RefactoringOperations.Should().ContainSingle().Which.Status.Should().Be("Ready");
    }

    [Test]
    public void Build_ImportWithUpdateAction_MarksAsReady()
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
                    new Change([TerraformActions.Update]) { Importing = new Importing { Id = "test-id" } })
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Update);
        change.IsImportAlreadyApplied.Should().BeFalse();
        model.RefactoringOperations.Should().ContainSingle().Which.Status.Should().Be("Ready");
    }

    [Test]
    public void Build_ImportWithNoOpAction_MarksAsReady()
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
                    new Change([TerraformActions.NoOp]) { Importing = new Importing { Id = "test-id" } })
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.NoOp);
        change.IsImportAlreadyApplied.Should().BeFalse();
        model.RefactoringOperations.Should().ContainSingle().Which.Status.Should().Be("Ready");
    }

    [Test]
    public void Build_MoveWithReadAction_MarksAsReady()
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
                    new Change([TerraformActions.Read]),
                    PreviousAddress: "azurerm_storage_account.old")
            ]);

        var model = new ReportModelBuilder().Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(TerraformActions.Read);
        change.IsMoveAlreadyApplied.Should().BeFalse();

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Operation.Should().Be("Move");
        operation.Status.Should().Be("Ready");
    }
}
