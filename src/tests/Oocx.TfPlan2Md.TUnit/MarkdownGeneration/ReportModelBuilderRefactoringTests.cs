using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderRefactoringTests
{
    private const string ManagedMode = "managed";
    private const string ProviderName = "provider";
    private const string CreateAction = "create";
    private const string ReadAction = "read";
    private const string ForgetAction = "forget";
    private const string UpdateAction = "update";
    private const string NoOpAction = "no-op";
    private const string UnknownAction = "unknown";

    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Build_NoRefactoring_RefactoringOperationsIsEmpty()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.RefactoringOperations.Should().BeEmpty();
    }

    [Test]
    public void Build_NoOpImport_IncludesChangeAndMarksAlreadyApplied()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-op-import.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(NoOpAction);
        change.ImportId.Should().NotBeNull();
        change.IsRefactoringAlreadyApplied.Should().BeTrue();

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Operation.Should().Be("Import");
        operation.Status.Should().Be("AlreadyApplied");
    }

    [Test]
    public void Build_RefactoringOperations_SortsImportsBeforeMovesAndWarningsFirst()
    {
        // Arrange
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
                    new Change([CreateAction]),
                    PreviousAddress: "module.old.type.move-ready"),
                new(
                    "type.import.ready",
                    null,
                    ManagedMode,
                    "type",
                    "import-ready",
                    ProviderName,
                    new Change([CreateAction]) { Importing = new Importing { Id = "id-ready" } }),
                new(
                    "type.import.noop",
                    null,
                    ManagedMode,
                    "type",
                    "import-noop",
                    ProviderName,
                    new Change([NoOpAction]) { Importing = new Importing { Id = "id-noop" } }),
                new(
                    "type.move.noop",
                    null,
                    ManagedMode,
                    "type",
                    "move-noop",
                    ProviderName,
                    new Change([NoOpAction]),
                    PreviousAddress: "module.old.type.move-noop")
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var order = model.RefactoringOperations
            .Select(op => $"{op.Operation}:{op.Status}:{op.Address}")
            .ToList();

        order.Should().Equal(
            "Import:AlreadyApplied:type.import.noop",
            "Import:Ready:type.import.ready",
            "Move:AlreadyApplied:type.move.noop",
            "Move:Ready:type.move.ready");
    }

    /// <summary>
    /// Tests the fix for issue #464: Import with "read" action should NOT show "Already imported" warning.
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md
    /// </summary>
    [Test]
    public void Build_ReadImport_IncludesChangeAndMarksAsReady()
    {
        // Arrange
        var json = File.ReadAllText("TestData/read-import.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(ReadAction);
        change.ImportId.Should().NotBeNull();
        change.IsRefactoringAlreadyApplied.Should().BeFalse("Import with 'read' action should be marked as Ready, not Already Applied");

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Operation.Should().Be("Import");
        operation.Status.Should().Be("Ready");
    }

    /// <summary>
    /// Unit test for DetermineAction to verify it correctly handles the "read" action.
    /// This ensures "read" doesn't fall through to NoOpAction.
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md
    /// </summary>
    [Test]
    public void Build_ImportWithReadAction_ActionIsRead()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([ReadAction]) { Importing = new Importing { Id = "test-id" } })
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(ReadAction, "DetermineAction should return 'read' for actions containing 'read'");
        change.IsRefactoringAlreadyApplied.Should().BeFalse("Read action with import ID should NOT be marked as already applied");
    }

    [Test]
    public void Build_ForgetAction_ActionIsForget()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([ForgetAction]))
            });

        var builder = new ReportModelBuilder();

        var model = builder.Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(ForgetAction, "DetermineAction should recognize Terraform state-removal actions");
        change.Action.Should().NotBe(NoOpAction, "forget must not be misclassified as no-op");
    }

    [Test]
    public void Build_UnknownAction_ActionIsUnknown()
    {
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change(["future-action"]))
            });

        var builder = new ReportModelBuilder();

        var model = builder.Build(plan);

        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(UnknownAction, "unknown action sets should be surfaced explicitly");
        change.Action.Should().NotBe(NoOpAction, "unknown action sets must not be treated as no-op");
    }

    /// <summary>
    /// Verifies that imports with "create" action are marked as Ready (not Already Applied).
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md
    /// </summary>
    [Test]
    public void Build_ImportWithCreateAction_MarksAsReady()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([CreateAction]) { Importing = new Importing { Id = "test-id" } })
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(CreateAction);
        change.IsRefactoringAlreadyApplied.Should().BeFalse("Create action with import ID should be marked as Ready");

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Status.Should().Be("Ready");
    }

    /// <summary>
    /// Verifies that imports with "update" action are marked as Ready (not Already Applied).
    /// This handles the case where an import will also apply configuration drift.
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md
    /// </summary>
    [Test]
    public void Build_ImportWithUpdateAction_MarksAsReady()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([UpdateAction]) { Importing = new Importing { Id = "test-id" } })
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(UpdateAction);
        change.IsRefactoringAlreadyApplied.Should().BeFalse("Update action with import ID should be marked as Ready");

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Status.Should().Be("Ready");
    }

    /// <summary>
    /// Verifies that only "no-op" imports are correctly marked as Already Applied.
    /// This is the positive test case for the "Already imported" warning.
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md
    /// </summary>
    [Test]
    public void Build_ImportWithNoOpAction_MarksAsAlreadyApplied()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([NoOpAction]) { Importing = new Importing { Id = "test-id" } })
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(NoOpAction);
        change.IsRefactoringAlreadyApplied.Should().BeTrue("No-op action with import ID should be marked as Already Applied");

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Status.Should().Be("AlreadyApplied");
    }

    /// <summary>
    /// Verifies that moved resources with "read" action are marked as Ready (not Already Applied).
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md
    /// </summary>
    [Test]
    public void Build_MoveWithReadAction_MarksAsReady()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "azurerm_storage_account.test",
                    null,
                    ManagedMode,
                    "azurerm_storage_account",
                    "test",
                    ProviderName,
                    new Change([ReadAction]),
                    PreviousAddress: "azurerm_storage_account.old")
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be(ReadAction);
        change.IsRefactoringAlreadyApplied.Should().BeFalse("Read action with move should be marked as Ready");

        var operation = model.RefactoringOperations.Should().ContainSingle().Subject;
        operation.Operation.Should().Be("Move");
        operation.Status.Should().Be("Ready");
    }
}
