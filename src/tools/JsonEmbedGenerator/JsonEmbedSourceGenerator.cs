using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace JsonEmbedGenerator;

/// <summary>
/// Generates Brotli-compressed embedded JSON classes from <c>AdditionalFiles</c> entries
/// marked with <c>EmbedAsJson=true</c> metadata.
/// </summary>
[Generator]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Source generation and Roslyn pipeline orchestration inherently couple compiler and IO APIs.")]
public sealed class JsonEmbedSourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Diagnostic emitted when a JSON additional file is missing required opt-in metadata.
    /// </summary>
    private static readonly DiagnosticDescriptor MissingEmbedMetadataDiagnostic = new(
        id: "JEG001",
        title: "JSON file missing EmbedAsJson metadata",
        messageFormat: "JSON additional file '{0}' is missing required metadata EmbedAsJson=true and will be ignored",
        category: "JsonEmbedding",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic emitted when a JSON additional file cannot be read.
    /// </summary>
    private static readonly DiagnosticDescriptor UnreadableJsonDiagnostic = new(
        id: "JEG002",
        title: "JSON additional file cannot be read",
        messageFormat: "JSON additional file '{0}' could not be read and will be ignored",
        category: "JsonEmbedding",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Initializes the incremental generator pipeline.
    /// </summary>
    /// <param name="context">Incremental generator context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var embeddingCandidates = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, cancellationToken) => GetEmbeddingInput(pair.Left, pair.Right, cancellationToken));

        var diagnostics = embeddingCandidates
            .Where(static candidate => candidate.Diagnostic is not null)
            .Select(static (candidate, _) => candidate.Diagnostic!);

        context.RegisterSourceOutput(diagnostics, static (productionContext, diagnostic) =>
        {
            productionContext.ReportDiagnostic(diagnostic);
        });

        var jsonFiles = embeddingCandidates
            .Where(static candidate => candidate.Input is not null)
            .Select(static (candidate, _) => candidate.Input!);

        context.RegisterSourceOutput(jsonFiles, static (productionContext, input) =>
        {
            var compressed = CompressUtf8(input.Content);
            var source = GenerateSource(input.ClassName, compressed);
            productionContext.AddSource($"EmbeddedJson.{input.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    /// <summary>
    /// Converts an additional file entry into embedding input when metadata opts it in.
    /// </summary>
    /// <param name="file">Additional file candidate.</param>
    /// <param name="optionsProvider">Analyzer config options provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Embedding input when valid; otherwise <c>null</c>.</returns>
    private static EmbeddingCandidate GetEmbeddingInput(
        AdditionalText file,
        AnalyzerConfigOptionsProvider optionsProvider,
        CancellationToken cancellationToken)
    {
        if (!file.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return new EmbeddingCandidate(null, null);
        }

        var options = optionsProvider.GetOptions(file);
        if (!TryGetEmbedAsJson(options, out var embedAsJson)
            || !string.Equals(embedAsJson, "true", StringComparison.OrdinalIgnoreCase))
        {
            var location = Location.Create(file.Path, default, default);
            return new EmbeddingCandidate(
                null,
                Diagnostic.Create(MissingEmbedMetadataDiagnostic, location, file.Path));
        }

        var text = file.GetText(cancellationToken)?.ToString();
        if (text is null)
        {
            var location = Location.Create(file.Path, default, default);
            return new EmbeddingCandidate(
                null,
                Diagnostic.Create(UnreadableJsonDiagnostic, location, file.Path));
        }

        var className = DeriveClassName(file.Path);
        return new EmbeddingCandidate(new EmbeddingInput(className, text), null);
    }

    /// <summary>
    /// Attempts to resolve the <c>EmbedAsJson</c> analyzer metadata key.
    /// </summary>
    /// <param name="options">Analyzer config options for one additional file.</param>
    /// <param name="value">Resolved metadata value.</param>
    /// <returns><c>true</c> when metadata key is present; otherwise <c>false</c>.</returns>
    private static bool TryGetEmbedAsJson(AnalyzerConfigOptions options, out string? value)
    {
        return options.TryGetValue("build_metadata.additionalfiles.embedasjson", out value);
    }

    /// <summary>
    /// Derives a class name from a file path according to the embedding rules.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <returns>Sanitized class name.</returns>
    private static string DeriveClassName(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrEmpty(fileName))
        {
            return "_";
        }

        var chars = fileName.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            if (!char.IsLetterOrDigit(chars[index]))
            {
                chars[index] = '_';
            }
        }

        var normalized = new string(chars);
        if (char.IsDigit(normalized[0]))
        {
            normalized = "_" + normalized;
        }

        if (char.IsLetter(normalized[0]))
        {
            normalized = char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
        }

        return normalized;
    }

    /// <summary>
    /// Compresses UTF-8 content with Brotli using the smallest-size compression level when available.
    /// </summary>
    /// <param name="content">Source JSON text.</param>
    /// <returns>Brotli-compressed payload bytes.</returns>
    private static byte[] CompressUtf8(string content)
    {
        var utf8Bytes = Encoding.UTF8.GetBytes(content);
        using var output = new MemoryStream();
        var level = ResolveSmallestSizeLevel();
        var brotliType = ResolveBrotliStreamType();

        using (var brotli = CreateCompressionStream(brotliType, output, level))
        {
            brotli.Write(utf8Bytes, 0, utf8Bytes.Length);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Resolves <see cref="CompressionLevel.SmallestSize"/> when available on the running SDK.
    /// </summary>
    /// <returns>The best available compression level.</returns>
    private static CompressionLevel ResolveSmallestSizeLevel()
    {
        return Enum.TryParse("SmallestSize", ignoreCase: false, out CompressionLevel parsed)
            ? parsed
            : CompressionLevel.Optimal;
    }

    /// <summary>
    /// Resolves the Brotli stream runtime type from the active build host.
    /// </summary>
    /// <returns>Brotli stream runtime type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Brotli stream type is unavailable.</exception>
    private static Type ResolveBrotliStreamType()
    {
        var type =
            Type.GetType("System.IO.Compression.BrotliStream, System.IO.Compression", throwOnError: false)
            ?? Type.GetType("System.IO.Compression.BrotliStream, System.IO.Compression.Brotli", throwOnError: false)
            ?? Type.GetType("System.IO.Compression.BrotliStream, System.Private.CoreLib", throwOnError: false);

        return type
            ?? throw new InvalidOperationException("BrotliStream runtime type is not available in the current build host.");
    }

    /// <summary>
    /// Creates a Brotli compression stream through runtime type activation.
    /// </summary>
    /// <param name="brotliType">Resolved Brotli stream runtime type.</param>
    /// <param name="output">Output stream receiving compressed bytes.</param>
    /// <param name="level">Compression level.</param>
    /// <returns>Writable compression stream.</returns>
    private static Stream CreateCompressionStream(Type brotliType, Stream output, CompressionLevel level)
    {
        var ctor = brotliType.GetConstructor(new[] { typeof(Stream), typeof(CompressionLevel), typeof(bool) });
        if (ctor is null)
        {
            throw new InvalidOperationException("Expected BrotliStream(Stream, CompressionLevel, bool) constructor was not found.");
        }

        var instance = ctor.Invoke(new object[] { output, level, true });
        return instance as Stream
            ?? throw new InvalidOperationException("Failed to create Brotli compression stream instance.");
    }

    /// <summary>
    /// Builds the generated class source code for one embedded JSON payload.
    /// </summary>
    /// <param name="className">Generated class name.</param>
    /// <param name="compressedBytes">Brotli-compressed data bytes.</param>
    /// <returns>C# source text.</returns>
    private static string GenerateSource(string className, byte[] compressedBytes)
    {
        var hex = BuildHexLiteral(compressedBytes);
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#pragma warning disable 1591");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.IO;");
        builder.AppendLine("using System.IO.Compression;");
        builder.AppendLine("using System.Text;");
        builder.AppendLine();
        builder.AppendLine("namespace EmbeddedJsonResources;");
        builder.AppendLine();
        builder.AppendLine($"public static class {className}");
        builder.AppendLine("{");
        builder.AppendLine($"    private static ReadOnlySpan<byte> CompressedData => new byte[] {{ {hex} }};");
        builder.AppendLine();
        builder.AppendLine("    public static string GetString()");
        builder.AppendLine("    {");
        builder.AppendLine("        var compressed = CompressedData.ToArray();");
        builder.AppendLine("        using var input = new MemoryStream(compressed);");
        builder.AppendLine("        using var brotli = new BrotliStream(input, CompressionMode.Decompress);");
        builder.AppendLine("        using var reader = new StreamReader(brotli, Encoding.UTF8);");
        builder.AppendLine("        return reader.ReadToEnd();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static byte[] GetBytes()");
        builder.AppendLine("    {");
        builder.AppendLine("        var compressed = CompressedData.ToArray();");
        builder.AppendLine("        using var input = new MemoryStream(compressed);");
        builder.AppendLine("        using var brotli = new BrotliStream(input, CompressionMode.Decompress);");
        builder.AppendLine("        using var output = new MemoryStream();");
        builder.AppendLine("        brotli.CopyTo(output);");
        builder.AppendLine("        return output.ToArray();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static Stream OpenStream()");
        builder.AppendLine("    {");
        builder.AppendLine("        var compressed = CompressedData.ToArray();");
        builder.AppendLine("        var input = new MemoryStream(compressed);");
        builder.AppendLine("        return new BrotliStream(input, CompressionMode.Decompress);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine("#pragma warning restore 1591");

        return builder.ToString();
    }

    /// <summary>
    /// Formats bytes as a hex literal list for C# array initialization.
    /// </summary>
    /// <param name="bytes">Payload bytes.</param>
    /// <returns>Comma-separated hex literal string.</returns>
    private static string BuildHexLiteral(byte[] bytes)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < bytes.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
#pragma warning disable S2583
                if ((index & 15) == 0)
                {
                    builder.AppendLine();
                    builder.Append("        ");
                }
                else
                {
                    builder.Append(' ');
                }
#pragma warning restore S2583
            }

            builder.Append("0x");
            builder.Append(bytes[index].ToString("X2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Immutable embedding input model.
    /// </summary>
    /// <param name="ClassName">Sanitized class name.</param>
    /// <param name="Content">JSON payload as text.</param>
    private sealed record EmbeddingInput(string ClassName, string Content);

    /// <summary>
    /// Carries a generator input and optional diagnostic for one additional file.
    /// </summary>
    /// <param name="Input">Embedding input when valid.</param>
    /// <param name="Diagnostic">Diagnostic when the file is skipped.</param>
    private sealed record EmbeddingCandidate(EmbeddingInput? Input, Diagnostic? Diagnostic);
}
