using System;
using System.Text;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Lightweight XML detection and pretty printing for markdown rendering.
/// Avoids using the full XML framework stack to keep NativeAOT output small.
/// </summary>
internal static class SimpleXmlFormatter
{
    /// <summary>
    /// Returns true when the value looks like XML based on simple structural heuristics.
    /// </summary>
    /// <param name="trimmed">The input value with leading and trailing whitespace removed.</param>
    /// <returns>True when the value likely represents XML.</returns>
    public static bool LooksLikeXml(string trimmed)
    {
        return trimmed.Length >= 3
            && trimmed.StartsWith('<')
            && trimmed.EndsWith('>')
            && trimmed.Contains('<')
            && trimmed.Contains('>');
    }

    /// <summary>
    /// Attempts to pretty-print XML using a best-effort tokenizer.
    /// </summary>
    /// <param name="trimmed">The input value with leading and trailing whitespace removed.</param>
    /// <param name="formatted">The formatted XML output when formatting succeeds.</param>
    /// <returns>True when formatting succeeds; otherwise false.</returns>
    public static bool TryPrettyPrint(string trimmed, out string formatted)
    {
        // Avoid trying to "pretty print" XML constructs that need a real parser.
        if (trimmed.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("<![CDATA[", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        var tokens = new SimpleTokenList();
        if (!TryTokenize(trimmed, ref tokens))
        {
            formatted = string.Empty;
            return false;
        }

        formatted = FormatTokens(tokens);
        return true;
    }

    private static bool TryTokenize(string input, ref SimpleTokenList tokens)
    {
        var index = 0;
        while (index < input.Length)
        {
            var lt = input.IndexOf('<', index);
            if (lt < 0)
            {
                tokens.AddText(input.AsSpan(index));
                return true;
            }

            if (lt > index)
            {
                tokens.AddText(input.AsSpan(index, lt - index));
            }

            var gt = input.IndexOf('>', lt + 1);
            if (gt < 0)
            {
                return false;
            }

            tokens.AddTag(input.AsSpan(lt, gt - lt + 1));
            index = gt + 1;
        }

        return true;
    }

    private static string FormatTokens(SimpleTokenList tokens)
    {
        var sb = new StringBuilder(capacity: tokens.EstimatedOutputLength);
        var indent = 0;

        var i = 0;
        while (i < tokens.Count)
        {
            if (tokens.Get(i).Kind == TokenKind.Text)
            {
                i = AppendTextToken(tokens, i, indent, sb);
                continue;
            }

            i = AppendTagToken(tokens, i, ref indent, sb);
        }

        // Trim trailing newline added by the formatter; callers add their own newlines.
        return sb.Length > 0 && sb[^1] == '\n' ? sb.ToString(0, sb.Length - 1) : sb.ToString();
    }

    private static int AppendTextToken(SimpleTokenList tokens, int index, int indent, StringBuilder sb)
    {
        var token = tokens.Get(index);
        if (!IsWhitespace(token.Value))
        {
            AppendIndent(sb, indent);
            sb.Append(token.Value.Trim());
            sb.Append('\n');
        }

        return index + 1;
    }

    private static int AppendTagToken(SimpleTokenList tokens, int index, ref int indent, StringBuilder sb)
    {
        var token = tokens.Get(index);
        var tagKind = ClassifyTag(token.Value);

        if (tagKind == TagKind.End)
        {
            indent = Math.Max(0, indent - 1);
            AppendIndent(sb, indent);
            sb.Append(token.Value);
            sb.Append('\n');
            return index + 1;
        }

        if (tagKind == TagKind.Start && TryAppendInlineTextElement(tokens, index, indent, sb, out var nextIndex))
        {
            return nextIndex;
        }

        AppendIndent(sb, indent);
        sb.Append(token.Value);
        sb.Append('\n');

        if (tagKind == TagKind.Start)
        {
            indent++;
        }

        return index + 1;
    }

    private static bool TryAppendInlineTextElement(
        SimpleTokenList tokens,
        int index,
        int indent,
        StringBuilder sb,
        out int nextIndex)
    {
        nextIndex = index;

        if (index + 2 >= tokens.Count)
        {
            return false;
        }

        if (tokens.Get(index + 1).Kind != TokenKind.Text || tokens.Get(index + 2).Kind != TokenKind.Tag)
        {
            return false;
        }

        if (ClassifyTag(tokens.Get(index + 2).Value) != TagKind.End)
        {
            return false;
        }

        var text = tokens.Get(index + 1).Value;
        if (IsWhitespace(text))
        {
            return false;
        }

        AppendIndent(sb, indent);
        sb.Append(tokens.Get(index).Value);
        sb.Append(text.Trim());
        sb.Append(tokens.Get(index + 2).Value);
        sb.Append('\n');
        nextIndex = index + 3;
        return true;
    }

    private static void AppendIndent(StringBuilder sb, int indent)
    {
        for (var i = 0; i < indent; i++)
        {
            sb.Append("  ");
        }
    }

    private static bool IsWhitespace(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsWhiteSpace(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static TagKind ClassifyTag(string tag)
    {
        if (tag.StartsWith("</", StringComparison.Ordinal))
        {
            return TagKind.End;
        }

        if (tag.StartsWith("<?", StringComparison.Ordinal) || tag.StartsWith("<!", StringComparison.Ordinal))
        {
            return TagKind.Special;
        }

        return tag.EndsWith("/>", StringComparison.Ordinal) ? TagKind.SelfClosing : TagKind.Start;
    }

    private enum TagKind
    {
        Start,
        End,
        SelfClosing,
        Special
    }

    private enum TokenKind
    {
        Tag,
        Text
    }

    private readonly struct SimpleToken(TokenKind kind, string value)
    {
        public TokenKind Kind { get; } = kind;
        public string Value { get; } = value;
    }

    private struct SimpleTokenList
    {
        private SimpleToken[] _items;
        private int _count;

        public SimpleTokenList()
        {
            _items = new SimpleToken[64];
            _count = 0;
        }

        public int Count => _count;

        public int EstimatedOutputLength => _count * 16;

        public SimpleToken Get(int index) => _items[index];

        public void AddTag(ReadOnlySpan<char> value) => Add(new SimpleToken(TokenKind.Tag, NormalizeTag(value.ToString())));

        public void AddText(ReadOnlySpan<char> value) => Add(new SimpleToken(TokenKind.Text, value.ToString()));

        private void Add(SimpleToken item)
        {
            if (_count >= _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[_count++] = item;
        }

        private static string NormalizeTag(string tag)
        {
            // Keep end tags and special constructs intact.
            if (tag.StartsWith("</", StringComparison.Ordinal)
                || tag.StartsWith("<?", StringComparison.Ordinal)
                || tag.StartsWith("<!", StringComparison.Ordinal))
            {
                return tag;
            }

            return NormalizeSingleQuotedAttributes(tag);
        }

        private static string NormalizeSingleQuotedAttributes(string tag)
        {
            // Best-effort normalization to match XDocument.ToString() output which uses double quotes.
            // Only rewrites an attribute if the single-quoted value does not contain a double quote.
            if (!tag.Contains("='", StringComparison.Ordinal))
            {
                return tag;
            }

            var sb = new StringBuilder(tag.Length);
            var i = 0;

            while (i < tag.Length)
            {
                var equalsIndex = tag.IndexOf('=', i);
                if (equalsIndex < 0 || equalsIndex + 1 >= tag.Length)
                {
                    sb.Append(tag, i, tag.Length - i);
                    break;
                }

                // Copy up to and including '='.
                sb.Append(tag, i, equalsIndex - i + 1);
                i = equalsIndex + 1;

                // Preserve whitespace between '=' and quote.
                while (i < tag.Length && char.IsWhiteSpace(tag[i]))
                {
                    sb.Append(tag[i]);
                    i++;
                }

                if (i >= tag.Length || tag[i] != '\'')
                {
                    continue;
                }

                var startQuoteIndex = i;
                var endQuoteIndex = tag.IndexOf('\'', startQuoteIndex + 1);
                if (endQuoteIndex < 0)
                {
                    // Malformed; give up and return original.
                    return tag;
                }

                var valueSpan = tag.AsSpan(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);
                if (valueSpan.Contains('"'))
                {
                    // Don't rewrite; we can't safely switch delimiters without escaping.
                    sb.Append(tag, startQuoteIndex, endQuoteIndex - startQuoteIndex + 1);
                    i = endQuoteIndex + 1;
                    continue;
                }

                sb.Append('"');
                sb.Append(valueSpan);
                sb.Append('"');
                i = endQuoteIndex + 1;
            }

            return sb.ToString();
        }
    }
}
