using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Server.Misc;

public static class NameVerification
{
    public static readonly SearchValues<char> AlphaNumeric = SearchValues.Create(
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
        'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
        'u', 'v', 'w', 'x', 'y', 'z'
    );

    public static readonly SearchValues<char> Alphabetic = SearchValues.Create(
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
        'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
        'u', 'v', 'w', 'x', 'y', 'z'
    );

    public static readonly SearchValues<char> Numeric = SearchValues.Create(
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    );

    public static readonly SearchValues<char> SpaceDashPeriodQuote = SearchValues.Create(' ', '-', '.', '\'');

    public static readonly SearchValues<string> StartDisallowed = SearchValues.Create(
        [
            "seer",
            "counselor",
            "gm",
            "admin",
            "lady",
            "lord"
        ],
        StringComparison.OrdinalIgnoreCase
    );

    public static readonly string[] Disallowed =
    [
        ..ProfanityProtection.Disallowed,
        "tailor",
        "smith",
        "scholar",
        "rogue",
        "novice",
        "neophyte",
        "merchant",
        "medium",
        "master",
        "mage",
        "lb",
        "journeyman",
        "grandmaster",
        "fisherman",
        "expert",
        "chef",
        "carpenter",
        "british",
        "blackthorne",
        "blackthorn",
        "beggar",
        "archer",
        "apprentice",
        "adept",
        "gamemaster",
        "frozen",
        "squelched",
        "invulnerable",
        "osi",
        "origin"
    ];

    public static readonly SearchValues<string> DisallowedSearchValues = SearchValues.Create(
        Disallowed,
        StringComparison.OrdinalIgnoreCase
    );

    public static void Configure()
    {
        CommandSystem.Register("ValidateName", AccessLevel.Administrator, ValidateName_OnCommand);
    }

    [Usage("ValidateName"), Description("Checks the result of NameValidation on the specified name.")]
    public static void ValidateName_OnCommand(CommandEventArgs e)
    {
        if (Validate(e.ArgString, 2, 16, true, false, true, 1, SpaceDashPeriodQuote))
        {
            e.Mobile.SendMessage(0x59, "That name is considered valid.");
        }
        else
        {
            e.Mobile.SendMessage(0x22, "That name is considered invalid.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ValidatePlayerName(ReadOnlySpan<char> name) =>
        Validate(name, 2, 16, true, false, true, 1, SpaceDashPeriodQuote);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ValidatePetName(ReadOnlySpan<char> name) => Validate(name, 1, 16, true, false, exceptions: null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ValidateVendorName(ReadOnlySpan<char> name) => Validate(name, 1, 20, true, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Validate(
        ReadOnlySpan<char> name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
        bool noExceptionsAtStart = true, int maxExceptions = 0, SearchValues<char> exceptions = null
    ) => Validate(
        name,
        minLength,
        maxLength,
        allowLetters,
        allowDigits,
        noExceptionsAtStart,
        maxExceptions,
        exceptions,
        Disallowed,
        DisallowedSearchValues,
        StartDisallowed
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Validate(
        ReadOnlySpan<char> name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
        bool noExceptionsAtStart, int maxExceptions, SearchValues<char> exceptions, ReadOnlySpan<string> disallowed,
        SearchValues<string> disallowedSV
    ) => Validate(
        name,
        minLength,
        maxLength,
        allowLetters,
        allowDigits,
        noExceptionsAtStart,
        maxExceptions,
        exceptions,
        disallowed,
        disallowedSV,
        null
    );

    public static bool Validate(
        ReadOnlySpan<char> name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
        bool noExceptionsAtStart, int maxExceptions, SearchValues<char> exceptions, ReadOnlySpan<string> disallowed,
        SearchValues<string> disallowedSV, SearchValues<string> startDisallowedSV
    )
    {
        if (name.Length == 0 || name.Length < minLength || name.Length > maxLength)
        {
            return false;
        }

        if (exceptions == null)
        {
            // We don't have exceptions, so we might be limited to letters or numbers
            var allowed = allowLetters switch
            {
                // If we don't allow exceptions, then non-alphanumeric is not allowed
                true when allowDigits && maxExceptions == 0 => AlphaNumeric,
                true when !allowDigits => Alphabetic,
                false when allowDigits => Numeric,
                // Everything has been allowed! Use `Utility.FixHtml()` to stop weird behavior
                _                      => null
            };

            if (allowed != null && name.ContainsAnyExcept(allowed))
            {
                return false;
            }
        }
        else
        {
            // We have exceptions, and at least one of the letters/digits flag is false:
            var notAllowed = allowLetters switch
            {
                true when !allowDigits  => Numeric,
                false when allowDigits => Alphabetic,
                _                      => null
            };

            if (notAllowed != null && name.ContainsAny(notAllowed))
            {
                return false;
            }

            if (ContainsExceptions(name, exceptions, noExceptionsAtStart, maxExceptions))
            {
                return false;
            }
        }

        if (disallowedSV != null && disallowed.Length > 0 && ContainsDisallowedWord(name, disallowed, disallowedSV))
        {
            return false;
        }

        return startDisallowedSV == null || name.IndexOfAny(startDisallowedSV) != 0;
    }

    public static bool ContainsExceptions(
        ReadOnlySpan<char> name, SearchValues<char> exceptions, bool noExceptionsAtStart, int maxExceptions
    )
    {
        if (!noExceptionsAtStart && maxExceptions is <= -1 or >= int.MaxValue)
        {
            return false;
        }

        var index = name.IndexOfAny(exceptions);
        var exceptionCount = 0;

        while (index != -1)
        {
            if (noExceptionsAtStart)
            {
                if (index == 0)
                {
                    return true;
                }

                noExceptionsAtStart = false;
            }

            if (exceptionCount++ >= maxExceptions)
            {
                return true;
            }

            if (index + 1 < name.Length)
            {
                name = name[(index + 1)..];
                index = name.IndexOfAny(exceptions);
                if (index != 0)
                {
                    exceptionCount = 0;
                }
            }
            else
            {
                index = -1;
            }
        }

        return false;
    }

    public static bool ContainsDisallowedWord(ReadOnlySpan<char> name, ReadOnlySpan<string> disallowed, SearchValues<string> disallowedSV)
    {
        var index = name.IndexOfAny(disallowedSV);

        while (index != -1)
        {
            var isStartBoundary = index == 0 || !char.IsLetterOrDigit(name[index - 1]);

            if (isStartBoundary)
            {
                for (var i = 0; i < disallowed.Length; i++)
                {
                    var word = disallowed[i].AsSpan();
                    if (index + word.Length > name.Length || !name.Slice(index, word.Length).InsensitiveEquals(word))
                    {
                        continue;
                    }

                    // End boundary
                    if (index + word.Length == name.Length || !char.IsLetterOrDigit(name[index + word.Length]))
                    {
                        return true;
                    }
                }
            }

            if (index + 1 < name.Length)
            {
                name = name[(index + 1)..];
                index = name.IndexOfAny(disallowedSV);
            }
            else
            {
                index = -1;
            }
        }

        return false;
    }
}
