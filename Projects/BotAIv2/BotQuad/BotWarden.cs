using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The Baron walking his own ground: the nearest square nobody has stood in, alone if need be.
///
/// <para>
/// <b>The same errand the captain's scoutmaster offers, on two different terms, and both differences are
/// the office rather than the errand.</b> A captain buys a party — he is taking bots away from work that
/// pays, so he pays for it, and he does not go at all if too few will come. The Baron pays nobody and goes
/// regardless: whoever wants to walk with him may, for nothing, and if nobody does he walks alone. That is
/// what the office is. He already takes no share of any corpse and lives on a stipend — see
/// <c>BotStipend</c> — so a Baron who paid for company would be spending the crown's money to buy the
/// crown's own subjects, which is not a thing this shard should learn to do.
/// </para>
///
/// <para>
/// <b>And it is deliberately the humblest thing he does.</b> Reckoned low, so a great hunt or a rescue
/// outbids it every time: walking the frontier is what a Baron does when the island has nothing worse to
/// offer, which is most of the time and is exactly when the map most needs filling in.
/// </para>
/// </summary>
public sealed class BotWarden : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWarden));

    public string Name => "Warden";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>What a Baron pays whoever walks with him. Nothing, by order.</summary>
    public static int Wage { get; set; }

    /// <summary>How many bodies a Baron will set out with. One: himself.</summary>
    public static int Least { get; set; } = 1;

    /// <summary>
    /// How many squares one round covers before the errand ends.
    ///
    /// <para>
    /// Twenty, by order, and it is the difference between a Baron who walks the island and one who looks
    /// stuck. At one square the errand ended on arrival and the next had to win an auction he could not win,
    /// because holding a company is itself a refusal for his own offices — so he stood where he had arrived.
    /// A round is a route: each square read, the frontier is asked again from where he is standing, and he
    /// walks outward until twenty are behind him or there is nothing unknown left within reach.
    /// </para>
    /// </summary>
    public static int Rounds { get; set; } = 20;

    /// <summary>Asked of a bot that is not a Baron. Not a refusal — nearly every answer is this.</summary>
    public static long NotABaron { get; private set; }

    public static long Asked { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    /// <summary>Nothing unknown within reach: the island around the population has been walked.</summary>
    public static long Charted { get; private set; }

    /// <summary>Unknown ground with no way through to it that anybody has found.</summary>
    public static long Sealed { get; private set; }

    /// <summary>Rounds not offered because there is ground dire enough to want a great hunt instead.</summary>
    public static long Wanted { get; private set; }

    public static long Offered { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (body is not BotMobile { Class: BotBaron })
        {
            NotABaron++;

            return null;
        }

        Asked++;

        if (!BotSquads.Running)
        {
            return null;
        }

        // <b>Being in a company is only a refusal for those who are not leading it.</b> Written as "must have
        // no squad", this stranded the whole company the moment a fight ended: the sweep had been dropped for
        // the skirmish, the company outlived the skirmish, and then every one of them — the leader included —
        // was refused a fresh sweep for already being in it. Five bots with the errand "nothing", standing in
        // a field, which is exactly the shape of defect this keeps producing. A leader with no work in hand is
        // the one bot who must be offered some.
        if (bot is IBotSquadMember member && member.Squad != null
            && (!ReferenceEquals(member.Squad.Leader, member) || bot.Resolve?.Deed != null))
        {
            Held++;

            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        // <b>Dire ground outranks unknown ground, and it has to be said here rather than left to the
        // auction.</b> A Baron walking his rounds is leading a company, and a Baron leading a company is
        // refused a harrowing before anything is scored — see BotHarrower, which counts him as Held. So the
        // two offices cannot be allowed to compete on price: whichever is offered first simply wins, and the
        // rounds are offered constantly while a harrowing waits on the island going wrong. This is the same
        // shape as a squad member having no work of its own, which this project has already paid for once.
        // <b>And asked with the harrowing's own test, not with a bare "is there dire ground".</b> Standing
        // aside for ground the harrowing will then refuse leaves the Baron with neither office — twenty asks
        // on 08.09.2026, twenty stood aside here and twenty refused there, and a Baron who strolled round the
        // town all afternoon. See BotHarrower.Takeable: one rule, asked in both places.
        if (BotQuad.Direst(map, body.Location, BotHarrower.Range, BotHarrower.Takeable(map, body.Location)) != null)
        {
            Wanted++;

            return null;
        }

        // And within the road map's reach of home — the third chooser of a frontier square, and the one that took Faron's
        // rounds to (1035, 1395) at 15:17 on 22.09.2026, 1397 tiles by road, after the other two had been gated. See
        // BotScout.Roadworthy.
        var where = BotQuad.Frontier(map, body.Location, BotScout.Range, at => BotScout.Roadworthy(map, at) && Reachable(map, body.Location, at));

        if (where == Point3D.Zero)
        {
            Charted++;

            return null;
        }

        Offered++;

        return new BotScout(map, where, Wage, Least, Rounds);
    }

    /// <summary>Whether the ground between here and there is not already known to be closed.</summary>
    private static bool Reachable(Map map, Point3D from, Point3D at)
    {
        if (BotReach.Ask(map, from, at, BotArrival.Within(BotQuad.Side / 3)) != BotReachVerdict.Sealed)
        {
            return true;
        }

        Sealed++;

        return false;
    }

    public static string Describe() =>
        Asked == 0
            ? $"no Baron has ever been offered his rounds ({NotABaron} answers went to bots that are not Barons)"
            : $"{Asked} times a Baron was asked to walk his rounds: {Offered} were offered unknown ground, "
              + $"{Held} were already leading a company, {Unfit} were too hurt, "
              + $"{Charted} found everything within {BotScout.Range} tiles already walked, {Sealed} found no way through to it, {Wanted} stood aside for ground dire enough to harrow";

    public static void Forget()
    {
        Asked = 0;
        NotABaron = 0;
        Held = 0;
        Unfit = 0;
        Charted = 0;
        Sealed = 0;
        Wanted = 0;
        Offered = 0;
    }
}
