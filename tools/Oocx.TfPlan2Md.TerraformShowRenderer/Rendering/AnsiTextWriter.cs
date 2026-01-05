using System.Globalization;
using System.Text;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

/// <summary>
/// Writes text with optional ANSI styling while supporting a no-color mode.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
internal sealed class AnsiTextWriter : IDisposable
{
    /// <summary>
    /// Underlying writer that receives rendered content.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md
    /// </summary>
    private readonly TextWriter _writer;

    /// <summary>
    /// Indicates whether ANSI escape sequences should be emitted.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md
    /// </summary>
    private readonly bool _useColor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiTextWriter"/> class.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md
    /// </summary>
    /// <param name="writer">Destination writer for output.</param>
    /// <param name="useColor">Determines whether ANSI escape codes are emitted.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> is null.</exception>
    public AnsiTextWriter(TextWriter writer, bool useColor)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _useColor = useColor;
    }

    /// <summary>
    /// Writes text without appending a newline.
    /// </summary>
    /// <param name="text">The text to write.</param>
    public void Write(string text)
    {
        _writer.Write(text);
    }

    /// <summary>
    /// Writes text and appends a newline.
    /// </summary>
    /// <param name="text">The text to write.</param>
    public void WriteLine(string text)
    {
        _writer.WriteLine(text);
    }

    /// <summary>
    /// Writes an empty line.
    /// </summary>
    public void WriteLine()
    {
        _writer.WriteLine();
    }

    /// <summary>
    /// Emits a reset escape sequence when ANSI coloring is enabled.
    /// </summary>
    public void WriteReset()
    {
        if (_useColor)
        {
            _writer.Write(GetEscapeSequence(AnsiStyle.Reset));
        }
    }

    /// <summary>
    /// Writes text wrapped in the specified ANSI styles.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="styles">Zero or more styles to apply.</param>
    public void WriteStyled(string text, params AnsiStyle[] styles)
    {
        WriteStyledInternal(text, styles, appendNewLine: false);
    }

    /// <summary>
    /// Writes text wrapped in the specified ANSI styles and appends a newline.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="styles">Zero or more styles to apply.</param>
    public void WriteLineStyled(string text, params AnsiStyle[] styles)
    {
        WriteStyledInternal(text, styles, appendNewLine: true);
    }

    /// <summary>
    /// Disposes the underlying writer.
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
    }

    /// <summary>
    /// Writes styled text, optionally appending a newline and applying ANSI sequences.
    /// </summary>
    /// <param name="text">Content to write.</param>
    /// <param name="styles">Styles to apply.</param>
    /// <param name="appendNewLine">Indicates whether to append a newline after writing.</param>
    private void WriteStyledInternal(string text, AnsiStyle[] styles, bool appendNewLine)
    {
        if (_useColor && styles.Length > 0)
        {
            foreach (var style in styles)
            {
                WriteEscape(style);
            }
        }

        _writer.Write(text);

        if (_useColor && styles.Length > 0)
        {
            WriteEscape(AnsiStyle.Reset);
        }

        if (appendNewLine)
        {
            _writer.WriteLine();
        }
    }

    /// <summary>
    /// Writes an ANSI escape sequence for the specified style when enabled.
    /// </summary>
    /// <param name="style">The ANSI style to emit.</param>
    private void WriteEscape(AnsiStyle style)
    {
        if (!_useColor)
        {
            return;
        }

        var escape = GetEscapeSequence(style);
        _writer.Write(escape);
    }

    /// <summary>
    /// Maps a style to its escape sequence.
    /// </summary>
    /// <param name="style">The ANSI style to map.</param>
    /// <returns>Escape sequence for the style.</returns>
    private static string GetEscapeSequence(AnsiStyle style)
    {
        return style switch
        {
            AnsiStyle.Bold => "\u001b[1m",
            AnsiStyle.Green => "\u001b[32m",
            AnsiStyle.Yellow => "\u001b[33m",
            AnsiStyle.Red => "\u001b[31m",
            AnsiStyle.Cyan => "\u001b[36m",
            AnsiStyle.Dim => "\u001b[90m",
            AnsiStyle.Reset => "\u001b[0m",
            _ => string.Empty
        };
    }
}
