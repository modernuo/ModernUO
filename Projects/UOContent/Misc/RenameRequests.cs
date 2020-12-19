using System;

namespace Server.Misc
{
    public static class RenameRequests
    {
        public static void Initialize()
        {
            EventSink.RenameRequest += EventSink_RenameRequest;
        }

        private static void EventSink_RenameRequest(Mobile from, Mobile targ, string name)
        {
            if (from.CanSee(targ) && from.InRange(targ, 12) && targ.CanBeRenamedBy(from))
            {
                name = name.Trim();

                if (NameVerification.Validate(
                    name,
                    1,
                    16,
                    true,
                    false,
                    true,
                    0,
                    NameVerification.Empty,
                    NameVerification.StartDisallowed,
                    Core.ML ? NameVerification.Disallowed : Array.Empty<string>()
                ))
                {
                    if (Core.ML)
                    {
                        var disallowed = ProfanityProtection.Disallowed;

                        for (var i = 0; i < disallowed.Length; i++)
                        {
                            if (name.IndexOfOrdinal(disallowed[i]) != -1)
                            {
                                from.SendLocalizedMessage(1072622); // That name isn't very polite.
                                return;
                            }
                        }

                        from.SendLocalizedMessage(
                            1072623,
                            $"{targ.Name}\t{name}"
                        ); // Pet ~1_OLDPETNAME~ renamed to ~2_NEWPETNAME~.
                    }

                    targ.Name = name;
                }
                else
                {
                    from.SendMessage("That name is unacceptable.");
                }
            }
        }
    }
}
