using Server.BotAI.V2;

namespace Server.BotAI.Mind;

/// <summary>
/// The guilds' bouts before Argus (07.10.2026, officers20): a herald's challenge, and its end, called at the place by Argus's herald
/// and in the world channel (<see cref="BotVenues.Herald"/>, <see cref="BotVenues.Announce"/>), as the events' are. Filled into
/// <c>BotGuildBout.Witness</c> from <c>BotDebugModule.Start</c> and emptied on a reload: the bots' assembly never references this one.
/// </summary>
public static class BotOfficersArgus
{
    public static void Open() => BotGuildBout.Witness = Witness;

    public static void Close() => BotGuildBout.Witness = null;

    private static void Witness(Map map, Point3D at, string line)
    {
        if (map != null && map != Map.Internal && at != Point3D.Zero)
        {
            BotVenues.Herald(BotVenues.Exact(BotVenues.Use.Field, map, at), BotVenues.HeraldHoldMs);
        }

        BotVenues.Announce(line);
    }
}
