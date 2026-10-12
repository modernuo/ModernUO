namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot that has been marked to move house the walk to its new town, when it is free to go. See <see cref="BotResettle"/>.
///
/// <para>
/// <b>Asked of every free bot at every auction, so it answers the common case in one field read:</b> a bot with no move
/// marked (<see cref="BotRelocate"/>) is refused before anything is counted. A marked bot goes on the terms a traveller does
/// (<see cref="BotTraveller"/>): not in a company, not underground, not tired out, fit and supplied — a bot sent onto a road
/// with no bandages is the death the road rule exists to prevent.
/// </para>
/// </summary>
public sealed class BotMover : IBotProposer
{
    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Unfit { get; private set; }

    public static long Unsupplied { get; private set; }

    public string Name => "Mover";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile who || who.Residence?.Pending is not { } town)
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

        Asked++;

        if (who.HitsMax <= 0 || who.Hits < who.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        if (BotProvision.Short(who))
        {
            Unsupplied++;

            return null;
        }

        var rec = who.Residence;
        var from = BotTowns.Nearest(who.Location)?.Name ?? BotResidence.TownOf(rec)?.Name ?? "the wild";

        Offered++;

        return new BotResettle(map, town, from, who.Location, rec.PendingCause, rec.PendingWhy);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody marked to move has been free to go"
            : $"{Asked} marked bots free to go: {Offered} offered the walk, {Unfit} hurt, {Unsupplied} without supplies; {BotResettle.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Unfit = 0;
        Unsupplied = 0;
        BotResettle.Forget();
    }
}
