using System;
using System.Collections.Generic;
using Server.Text;

namespace Server.Engines.AIConversation;

/// <summary>
/// Pure text helpers for AI NPC conversations: engagement phrase detection,
/// NPC name matching, model-output sanitization and overhead-speech chunking.
/// No game-state dependencies so the logic is unit-testable.
/// </summary>
public static class ConversationText
{
    private static readonly string[] _greetings = { "hi", "hello", "hail", "hey", "greetings", "salutations", "good day", "well met" };
    private static readonly string[] _farewells = { "bye", "goodbye", "farewell", "later", "good bye" };

    public static bool IsGreeting(ReadOnlySpan<char> said) => StartsWithAny(said, _greetings);

    public static bool IsFarewell(ReadOnlySpan<char> said) => StartsWithAny(said, _farewells);

    private static bool StartsWithAny(ReadOnlySpan<char> said, string[] phrases)
    {
        foreach (var phrase in phrases)
        {
            if (said.StartsWith(phrase, StringComparison.OrdinalIgnoreCase) &&
                (said.Length == phrase.Length || !char.IsLetter(said[phrase.Length])))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when the speech contains a whole-word mention of any part of the
    /// NPC's name that is at least three characters long ("Elric" matches
    /// "hail sage elric!", but "El" never matches and "elrics" does not).
    /// </summary>
    public static bool MentionsName(string said, string name)
    {
        if (string.IsNullOrEmpty(said) || string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (var part in name.AsSpan().Split(' '))
        {
            var word = name.AsSpan()[part];

            if (word.Length < 3)
            {
                continue;
            }

            var remaining = said.AsSpan();
            var offset = 0;

            while (true)
            {
                var index = remaining[offset..].IndexOf(word, StringComparison.OrdinalIgnoreCase);

                if (index < 0)
                {
                    break;
                }

                index += offset;

                var startOk = index == 0 || !char.IsLetter(remaining[index - 1]);
                var end = index + word.Length;
                var endOk = end >= remaining.Length || !char.IsLetter(remaining[end]);

                if (startOk && endOk)
                {
                    return true;
                }

                offset = index + 1;
            }
        }

        return false;
    }

    /// <summary>
    /// Cleans model output for overhead speech: control characters and
    /// newlines become spaces, runs of whitespace collapse, wrapping quotes
    /// are stripped, and text longer than maxLength is cut at a sentence
    /// boundary where a reasonable one exists.
    /// </summary>
    public static string Sanitize(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        using var sb = ValueStringBuilder.CreateMT(text.Length);
        var pendingSpace = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                sb.Append(' ');
                pendingSpace = false;
            }

            sb.Append(c);
        }

        var result = sb.ToString();

        if (result.Length >= 2 && result[0] == '"' && result[^1] == '"')
        {
            result = result[1..^1].Trim();
        }

        return result.Length > maxLength ? TruncateAtSentence(result, maxLength) : result;
    }

    private static string TruncateAtSentence(string text, int maxLength)
    {
        var slice = text[..maxLength];

        var lastSentence = Math.Max(
            slice.LastIndexOf(". ", StringComparison.Ordinal),
            Math.Max(slice.LastIndexOf("! ", StringComparison.Ordinal), slice.LastIndexOf("? ", StringComparison.Ordinal))
        );

        return lastSentence > maxLength / 3 ? slice[..(lastSentence + 1)] : slice;
    }

    /// <summary>
    /// Splits sanitized text into chunks of at most maxLength characters for
    /// overhead speech, preferring sentence boundaries, then word breaks.
    /// </summary>
    public static List<string> SplitIntoChunks(string text, int maxLength)
    {
        var chunks = new List<string>();
        var remaining = text.AsSpan().Trim();

        while (remaining.Length > maxLength)
        {
            var split = -1;

            // Look for the last sentence end inside the window, but not so
            // early that the chunk becomes a fragment.
            for (var i = maxLength - 1; i > maxLength / 3; --i)
            {
                var c = remaining[i];

                if (c is '.' or '!' or '?')
                {
                    split = i + 1;
                    break;
                }
            }

            if (split < 0)
            {
                split = remaining[..(maxLength + 1)].LastIndexOf(' ');

                if (split < maxLength / 3)
                {
                    split = maxLength;
                }
            }

            chunks.Add(remaining[..split].TrimEnd().ToString());
            remaining = remaining[split..].TrimStart();
        }

        if (remaining.Length > 0)
        {
            chunks.Add(remaining.ToString());
        }

        return chunks;
    }
}
