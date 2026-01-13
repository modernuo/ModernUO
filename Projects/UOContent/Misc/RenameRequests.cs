using System;

namespace Server.Misc;

public static class RenameRequests
{
    public static void RenameRequest(Mobile from, Mobile targ, string name)
    {
        if (!from.CanSee(targ) || !from.InRange(targ, 12) || !targ.CanBeRenamedBy(from))
        {
            return;
        }

        var span = name.AsSpan().Trim();

        if (NameVerification.ValidatePetName(span))
        {
            // Pet ~1_OLDPETNAME~ renamed to ~2_NEWPETNAME~.
            from.SendLocalizedMessage(1072623, $"{targ.Name}\t{span}");
            targ.Name = span.ToString();
        }
        else if (span.IndexOfAny(ProfanityProtection.DisallowedSearchValues) != -1)
        {
            from.SendLocalizedMessage(1072622); // That name isn't very polite.
        }
        else
        {
            from.SendMessage("That name is unacceptable.");
        }
    }
}
