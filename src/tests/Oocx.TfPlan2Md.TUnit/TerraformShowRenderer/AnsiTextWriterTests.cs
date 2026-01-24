using System.Text;
using Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Validates ANSI styling behavior for the Terraform show renderer writer.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
public sealed class AnsiTextWriterTests
{
    /// <summary>
    /// Ensures styled writes include start and reset escape sequences.
    /// Related acceptance: Task 2 ANSI support.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WriteStyled_WithGreen_WritesAnsiSequences()
    {
        var buffer = new StringBuilder();
        using var writer = new StringWriter(buffer);
        var ansi = new AnsiTextWriter(writer, useColor: true);

        ansi.WriteStyled("value", AnsiStyle.Green);

        var output = buffer.ToString();
        await Assert.That(output).Contains("\u001b[32m");
        await Assert.That(output).Contains("\u001b[0m");
        await Assert.That(output).Contains("value");
    }

    /// <summary>
    /// Ensures multiple styles are emitted together and reset once.
    /// Related acceptance: Task 2 ANSI support.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WriteStyled_WithBoldAndRed_EmitsBothStyles()
    {
        var buffer = new StringBuilder();
        using var writer = new StringWriter(buffer);
        var ansi = new AnsiTextWriter(writer, useColor: true);

        ansi.WriteStyled("critical", AnsiStyle.Bold, AnsiStyle.Red);

        var output = buffer.ToString();
        await Assert.That(output).Contains("\u001b[1m");
        await Assert.That(output).Contains("\u001b[31m");
        await Assert.That(output).EndsWith("\u001b[0m");
    }

    /// <summary>
    /// Ensures ANSI escape sequences are suppressed when color is disabled.
    /// Related acceptance: Task 2 no-color flag.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WriteStyled_NoColor_OmitsAnsiSequences()
    {
        var buffer = new StringBuilder();
        using var writer = new StringWriter(buffer);
        var ansi = new AnsiTextWriter(writer, useColor: false);

        ansi.WriteStyled("plain", AnsiStyle.Green);

        var output = buffer.ToString();
        await Assert.That(output).DoesNotContain("\u001b[");
        await Assert.That(output).IsEqualTo("plain");
    }

    /// <summary>
    /// Verifies that WriteLineIfNotBlank prevents duplicate consecutive blank lines.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WriteLineIfNotBlank_PreventsDuplicateBlankLines()
    {
        var buffer = new StringBuilder();
        using var writer = new StringWriter(buffer);
        var ansi = new AnsiTextWriter(writer, useColor: false);

        ansi.Write("x");
        ansi.WriteLine();           // x\n
        ansi.WriteLineIfNotBlank(); // should be ignored (already a blank line)
        ansi.WriteLineIfNotBlank(); // still ignored

        ansi.Write("y");
        ansi.WriteLine();           // y\n

        var lines = buffer.ToString().Split(Environment.NewLine);

        // Expect sequence: "x", "", "y", "" (last trailing newline yields empty trailing entry)
        await Assert.That(lines.Length).IsEqualTo(4);
        await Assert.That(lines[0]).IsEqualTo("x");
        await Assert.That(lines[1]).IsEqualTo(string.Empty);
        await Assert.That(lines[2]).IsEqualTo("y");
        await Assert.That(lines[3]).IsEqualTo(string.Empty);
    }
}
