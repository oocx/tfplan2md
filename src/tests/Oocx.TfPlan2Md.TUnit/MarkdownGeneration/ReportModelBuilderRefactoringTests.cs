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
    private const string NoOpAction = "no-op";

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
}
