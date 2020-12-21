using System;

namespace Server.Misc
{
    public static class NameVerification
    {
        public static readonly char[] SpaceDashPeriodQuote =
        {
            ' ', '-', '.', '\''
        };

        public static readonly char[] Empty = Array.Empty<char>();

        public static string[] StartDisallowed { get; } =
        {
            "seer",
            "counselor",
            "gm",
            "admin",
            "lady",
            "lord"
        };

        public static string[] Disallowed { get; } =
        {
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
        };

        public static void Initialize()
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

        public static bool Validate(
            string name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
            bool noExceptionsAtStart, int maxExceptions, char[] exceptions
        ) =>
            Validate(
                name,
                minLength,
                maxLength,
                allowLetters,
                allowDigits,
                noExceptionsAtStart,
                maxExceptions,
                exceptions,
                Disallowed,
                StartDisallowed
            );

        public static bool Validate(
            string name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
            bool noExceptionsAtStart, int maxExceptions, char[] exceptions, string[] disallowed, string[] startDisallowed
        )
        {
            if (name == null || name.Length < minLength || name.Length > maxLength)
            {
                return false;
            }

            var exceptCount = 0;

            name = name.ToLower();

            if (!allowLetters || !allowDigits ||
                exceptions.Length > 0 && (noExceptionsAtStart || maxExceptions < int.MaxValue))
            {
                for (var i = 0; i < name.Length; ++i)
                {
                    var c = name[i];

                    if (c >= 'a' && c <= 'z')
                    {
                        if (!allowLetters)
                        {
                            return false;
                        }

                        exceptCount = 0;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        if (!allowDigits)
                        {
                            return false;
                        }

                        exceptCount = 0;
                    }
                    else
                    {
                        var except = false;

                        for (var j = 0; !except && j < exceptions.Length; ++j)
                        {
                            if (c == exceptions[j])
                            {
                                except = true;
                            }
                        }

                        if (!except || i == 0 && noExceptionsAtStart)
                        {
                            return false;
                        }

                        if (exceptCount++ == maxExceptions)
                        {
                            return false;
                        }
                    }
                }
            }

            for (var i = 0; i < disallowed.Length; ++i)
            {
                var indexOf = name.IndexOfOrdinal(disallowed[i]);

                if (indexOf == -1)
                {
                    continue;
                }

                var badPrefix = indexOf == 0;

                for (var j = 0; !badPrefix && j < exceptions.Length; ++j)
                {
                    badPrefix = name[indexOf - 1] == exceptions[j];
                }

                if (!badPrefix)
                {
                    continue;
                }

                var badSuffix = indexOf + disallowed[i].Length >= name.Length;

                for (var j = 0; !badSuffix && j < exceptions.Length; ++j)
                {
                    badSuffix = name[indexOf + disallowed[i].Length] == exceptions[j];
                }

                if (badSuffix)
                {
                    return false;
                }
            }

            for (var i = 0; i < startDisallowed.Length; ++i)
            {
                if (name.StartsWithOrdinal(startDisallowed[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
