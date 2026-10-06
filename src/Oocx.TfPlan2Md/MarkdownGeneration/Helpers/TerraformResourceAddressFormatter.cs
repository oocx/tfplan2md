using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Extracts Terraform resource instance suffixes without splitting quoted keys.
/// </summary>
internal static class TerraformResourceAddressFormatter
{
    private enum AddressScanState
    {
        OutsideIndex,
        InsideIndex,
        InsideQuotedKey,
        EscapedQuotedCharacter,
        Invalid
    }

    /// <summary>
    /// Appends the final resource instance suffix to the existing local-name label.
    /// </summary>
    /// <param name="resourceName">Terraform's local resource name.</param>
    /// <param name="address">The full Terraform resource address.</param>
    /// <returns>The local name with its exact instance suffix, when present.</returns>
    internal static string AppendInstanceSuffix(string resourceName, string address)
    {
        var suffix = GetFinalResourceInstanceSuffix(address);
        var label = string.IsNullOrWhiteSpace(resourceName)
            ? GetLocalName(address, suffix)
            : resourceName;

        return suffix.Length == 0 || label.EndsWith(suffix, StringComparison.Ordinal)
            ? label
            : label + suffix;
    }

    /// <summary>
    /// Gets the exact index suffix from the final resource segment, excluding indexed module segments.
    /// </summary>
    /// <param name="address">The full Terraform resource address.</param>
    /// <returns>The final resource's exact bracketed index, or an empty string.</returns>
    internal static string GetFinalResourceInstanceSuffix(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return string.Empty;
        }

        var finalSeparator = FindLastAddressSeparator(address);
        if (finalSeparator < 0)
        {
            return string.Empty;
        }

        var resourceNameStart = finalSeparator + 1;
        return TryGetTrailingIndexStart(address, resourceNameStart, out var suffixStart)
            ? address[suffixStart..]
            : string.Empty;
    }

    /// <summary>
    /// Finds the final resource-name separator while ignoring dots inside instance keys.
    /// </summary>
    /// <param name="address">The full Terraform resource address.</param>
    /// <returns>The separator position, or <c>-1</c> for malformed addresses.</returns>
    private static int FindLastAddressSeparator(string address)
    {
        var lastSeparator = -1;
        var state = AddressScanState.OutsideIndex;

        for (var position = 0; position < address.Length; position++)
        {
            var character = address[position];
            if (state == AddressScanState.OutsideIndex && character == '.')
            {
                lastSeparator = position;
            }

            state = AdvanceState(state, character);
            if (state == AddressScanState.Invalid)
            {
                return -1;
            }
        }

        return state == AddressScanState.OutsideIndex ? lastSeparator : -1;
    }

    /// <summary>
    /// Finds a single balanced index that closes at the end of the resource address.
    /// </summary>
    /// <param name="address">The full Terraform resource address.</param>
    /// <param name="resourceNameStart">The first character after the final resource-name separator.</param>
    /// <param name="suffixStart">The opening bracket when an index is found.</param>
    /// <returns><see langword="true"/> when the final resource segment has a trailing index.</returns>
    private static bool TryGetTrailingIndexStart(string address, int resourceNameStart, out int suffixStart)
    {
        suffixStart = -1;
        var state = AddressScanState.OutsideIndex;

        for (var position = resourceNameStart; position < address.Length; position++)
        {
            var character = address[position];
            if (state == AddressScanState.OutsideIndex && character == '[')
            {
                suffixStart = position;
            }

            var nextState = AdvanceState(state, character);
            if (nextState == AddressScanState.Invalid)
            {
                return false;
            }

            if (state == AddressScanState.InsideIndex && character == ']' && position != address.Length - 1)
            {
                return false;
            }

            state = nextState;
        }

        return suffixStart >= 0 && state == AddressScanState.OutsideIndex;
    }

    /// <summary>
    /// Advances the address scanner through one character while honoring quoted-key escapes.
    /// </summary>
    /// <param name="state">The scanner state before the character.</param>
    /// <param name="character">The character to process.</param>
    /// <returns>The scanner state after the character, or <see cref="AddressScanState.Invalid"/>.</returns>
    private static AddressScanState AdvanceState(AddressScanState state, char character)
    {
        return state switch
        {
            AddressScanState.OutsideIndex when character == '[' => AddressScanState.InsideIndex,
            AddressScanState.OutsideIndex when character == ']' => AddressScanState.Invalid,
            AddressScanState.InsideIndex when character == '[' => AddressScanState.Invalid,
            AddressScanState.InsideIndex when character == '"' => AddressScanState.InsideQuotedKey,
            AddressScanState.InsideIndex when character == ']' => AddressScanState.OutsideIndex,
            AddressScanState.InsideQuotedKey when character == '\\' => AddressScanState.EscapedQuotedCharacter,
            AddressScanState.InsideQuotedKey when character == '"' => AddressScanState.InsideIndex,
            AddressScanState.EscapedQuotedCharacter => AddressScanState.InsideQuotedKey,
            AddressScanState.Invalid => AddressScanState.Invalid,
            _ => state
        };
    }

    /// <summary>
    /// Reads the final local resource segment when the plan model has no resource name.
    /// </summary>
    /// <param name="address">The full Terraform resource address.</param>
    /// <param name="suffix">The parsed resource instance suffix.</param>
    /// <returns>The resource segment without its index, or an empty string.</returns>
    private static string GetLocalName(string address, string suffix)
    {
        var finalSeparator = FindLastAddressSeparator(address);
        if (finalSeparator < 0)
        {
            return string.Empty;
        }

        var segmentEnd = address.Length - suffix.Length;
        return segmentEnd < finalSeparator + 1
            ? string.Empty
            : address[(finalSeparator + 1)..segmentEnd];
    }
}
