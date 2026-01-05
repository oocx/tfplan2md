using System.Text;
using Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Validates ANSI styling behavior for the Terraform show renderer writer.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
public sealed class AnsiTextWriterTests
{
    /// <summary>
    /// Ensures styled writes include start and reset escape sequences.
    /// Related acceptance: Task 2 ANSI support.
    /// </summary>
    [Fact]
    public void WriteStyled_WithGreen_WritesAnsiSequences()
    {
        var buffer = new StringBuilder();
        using var writer = new StringWriter(buffer);
        var ansi = new AnsiTextWriter(writer, useColor: true);

        ansi.WriteStyled("value", AnsiStyle.Green);

        var output = buffer.ToString();
        Assert.Contains("\u001b[32m", output, StringComparison.Ordinal);
        Assert.Contains("\u001b[0m", output, StringComparison.Ordinal);
        Assert.Contains("value", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ensures multiple styles are emitted together and reset once.
    /// Related acceptance: Task 2 ANSI support.
    /// </summary>
    [Fact]
    public void WriteStyled_WithBoldAndRed_EmitsBothStyles()
    {
        var buffer = new StringBuilder();
        using var writer = new StringWriter(buffer);
        var ansi = new AnsiTextWriter(writer, useColor: true);

        ansi.WriteStyled("critical", AnsiStyle.Bold, AnsiStyle.Red);

        var output = buffer.ToString();
        Assert.Contains("\u001b[1m", output, StringComparison.Ordinal);
        Assert.Contains("\u001b[31m", output, StringComparison.Ordinal);
        Assert.EndsWith("\u001b[0m", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ensures ANSI escape sequences are suppressed when color is disabled.
    /// Related acceptance: Task 2 no-color flag.
    /// </summary>
    [Fact]
    public void WriteStyled_NoColor_OmitsAnsiSequences()
    {
        var buffer = new StringBuilder();
        using var writer = new StringWriter(buffer);
        var ansi = new AnsiTextWriter(writer, useColor: false);

        ansi.WriteStyled("plain", AnsiStyle.Green);

        var output = buffer.ToString();
        Assert.DoesNotContain("\u001b[", output, StringComparison.Ordinal);
        Assert.Equal("plain", output);
    }
}
