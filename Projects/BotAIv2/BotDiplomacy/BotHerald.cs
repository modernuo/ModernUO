namespace Server.BotAI.V2;

/// <summary>
/// Offers a free member of a guild that has a meeting called the walk as its envoy, or as its answerer when the other guild's
/// envoy is near. See <see cref="BotEnvoy"/>.
///
/// <para>
/// <b>Asked of every free bot at every auction, so it answers the common case with a count</b>: no meeting standing is no
/// offer, before anything about the bot is read. A member goes on the terms a traveller does, less the strength the road asks
/// — a guild of one sends its founder whatever the road — and not while it is red or grey, because a town's guards would kill
/// an envoy at the door.
/// </para>
/// </summary>
public sealed class BotHerald : IBotProposer
{
    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long OfferedHost { get; private set; }

    public static long Outlawed { get; private set; }

    public static long Unfit { get; private set; }

    public static long Far { get; private set; }

    public static long Sealed { get; private set; }

    public string Name => "Herald";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (BotParley.Meetings.Count == 0 || bot?.Self is not BotMobile who)
        {
            return null;
        }

        var map = who.Map;

        if (map == null || map == Map.Internal || !who.Alive || who.Tired)
        {
            return null;
        }

        if (bot is IBotSquadMember { Squad: not null } || BotDelveParty.Delving(who) || BotDungeon.Under(who.Location))
        {
            return null;
        }

        var m = BotParley.Wanting(who, out var asHost);

        if (m == null)
        {
            return null;
        }

        Asked++;

        if (who.Murderer || who.Criminal)
        {
            Outlawed++;

            return null;
        }

        if (who.HitsMax <= 0 || who.Hits < who.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        if (m.Map != map || !BotGates.Joined(map, who.Location, m.Place))
        {
            Far++;

            return null;
        }

        if (BotReach.Ask(map, who.Location, m.Place, BotArrival.Within(BotParley.ArriveTiles)) == BotReachVerdict.Sealed)
        {
            Sealed++;

            return null;
        }

        if (asHost)
        {
            OfferedHost++;
        }
        else
        {
            Offered++;
        }

        return new BotEnvoy(m, asHost, who.Location);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been asked to go to a meeting"
            : $"{Asked} members asked to go to a meeting: {Offered} offered the envoy's walk and {OfferedHost} the answerer's, {Outlawed} outlawed, {Unfit} hurt, {Far} with no walk there, {Sealed} with the place sealed off";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        OfferedHost = 0;
        Outlawed = 0;
        Unfit = 0;
        Far = 0;
        Sealed = 0;
        BotEnvoy.Forget();
    }
}
