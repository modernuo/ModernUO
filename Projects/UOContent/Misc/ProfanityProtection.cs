using System;
using System.Buffers;

namespace Server.Misc;

public enum ProfanityAction
{
    None,           // no action taken
    Disallow,       // speech is not displayed
    Criminal,       // makes the player criminal, not killable by guards
    CriminalAction, // makes the player criminal, can be killed by guards
    Disconnect,     // player is kicked
    Other           // some other implementation
}

public static class ProfanityProtection
{
    private static bool Enabled;
    private static ProfanityAction Action;

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetSetting("profanityProtection.enabled", false);
        Action = ServerConfiguration.GetSetting("profanityProtection.action", ProfanityAction.Disallow);

        if (Enabled)
        {
            EventSink.Speech += EventSink_Speech;
        }
    }

    // Used by the guild system
    public static readonly SearchValues<char> Exceptions = SearchValues.Create(
        ' ', '-', '.', '\'', '"', ',', '_', '+', '=', '~', '`', '!', '^', '*', '\\', '/', ';', ':', '<', '>', '[', ']',
        '{', '}', '?', '|', '(', ')', '%', '$', '&', '#', '@'
    );

    public static readonly string[] Disallowed =
    [
        "jigaboo",
        "chigaboo",
        "wop",
        "kyke",
        "kike",
        "tit",
        "spic",
        "prick",
        "piss",
        "lezbo",
        "lesbo",
        "felatio",
        "dyke",
        "dildo",
        "chinc",
        "chink",
        "cunnilingus",
        "cum",
        "cocksucker",
        "cock",
        "clitoris",
        "clit",
        "ass",
        "hitler",
        "penis",
        "nigga",
        "nigger",
        "klit",
        "kunt",
        "jiz",
        "jism",
        "jerkoff",
        "jackoff",
        "goddamn",
        "fag",
        "blowjob",
        "bitch",
        "asshole",
        "dick",
        "pussy",
        "snatch",
        "cunt",
        "twat",
        "shit",
        "fuck",
    ];

    public static readonly SearchValues<string> DisallowedSearchValues = SearchValues.Create(
        Disallowed,
        StringComparison.OrdinalIgnoreCase
    );

    private static bool OnProfanityDetected(Mobile from, string speech)
    {
        switch (Action)
        {
            case ProfanityAction.None:     return true;
            case ProfanityAction.Disallow: return false;
            case ProfanityAction.Criminal:
                from.Criminal = true;
                return true;
            case ProfanityAction.CriminalAction:
                from.CriminalAction(false);
                return true;
            case ProfanityAction.Disconnect:
                {
                    from.NetState?.Disconnect("Using profanity.");

                    return false;
                }
            default:
            case ProfanityAction.Other: // TODO: Provide custom implementation if this is chosen
                {
                    return true;
                }
        }
    }

    public static bool ContainsProfanity(ReadOnlySpan<char> speech) =>
        speech.Length > 0 &&
        !NameVerification.Validate(
            speech,
            1,
            int.MaxValue,
            true,
            true,
            true,
            int.MaxValue, // allow all non-alphanumeric characters
            null,
            Disallowed,
            DisallowedSearchValues
        );

    private static void EventSink_Speech(SpeechEventArgs e)
    {
        var from = e.Mobile;

        if (from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        if (ContainsProfanity(e.Speech))
        {
            e.Blocked = !OnProfanityDetected(from, e.Speech);
        }
    }
}
