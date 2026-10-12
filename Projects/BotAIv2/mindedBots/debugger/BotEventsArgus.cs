using Server.BotAI.V2;

namespace Server.BotAI.Mind;

/// <summary>
/// The events of the world, as Argus's squad serves them (07.10.2026, events17): the herald goes to the place and calls the line in
/// the world channel (<see cref="BotVenues.Herald"/>, <see cref="BotVenues.Announce"/>), the crown's purse fills a rumour's chest and
/// takes back what nobody found (<see cref="BotRevel.Lend"/>, <see cref="BotRevel.Repay"/>), and the fair asks for the championship
/// (<see cref="BotTourney.Start"/>, which keeps its hour). Filled into <c>BotSeasons</c>'s seams from <c>BotDebugModule.Start</c> and
/// emptied on a reload, as the revel's seams are: the bots' assembly never references this one.
/// </summary>
public static class BotEventsArgus
{
    public static void Open()
    {
        BotSeasons.Herald = Herald;
        BotSeasons.CrownTake = BotRevel.Lend;
        BotSeasons.CrownGive = BotRevel.Repay;
        BotSeasons.CallTourney = town => BotTourney.Start($"the fair at {town}");
    }

    public static void Close()
    {
        BotSeasons.Herald = null;
        BotSeasons.CrownTake = null;
        BotSeasons.CrownGive = null;
        BotSeasons.CallTourney = null;
    }

    private static void Herald(Map map, Point3D at, string line)
    {
        if (map != null && map != Map.Internal && at != Point3D.Zero)
        {
            BotVenues.Herald(BotVenues.Exact(BotVenues.Use.Field, map, at), BotVenues.HeraldHoldMs);
        }

        BotVenues.Announce(line);
    }
}
