using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Pirates: The Shadow waits on the quay for a colony's loaded ship and boards it as it comes in.
///
/// <para>
/// <b>Patrick, 07.10.2026: "it can be lost in war or to pirates: The Shadow at sea intercepts loaded ships (sea ambushes are honest:
/// a loaded ship does not turn)."</b> And the order's own simplification: the ambush at the quay on arrival, outside the guards.
/// A run home with goods aboard (<see cref="BotColonyRun"/>) is watched while it sails; when it is within
/// <see cref="LayAheadMs"/> of the quay it makes for, that quay lies where no town's guards act, and two or three of The Shadow
/// stand on that land within <see cref="PartyReach"/> and are a match for the captain and the hands aboard, the band is laid on
/// the quay. The ship holds its course — it does not turn — and lands into them.
/// </para>
///
/// <para>
/// <b>The ambush is the highway's own</b> (<see cref="BotHighway.Lay"/>): the band pressed with <see cref="BotLieInWait"/>, the
/// strike, the robbery fought by <see cref="BotBrawl"/>, the captain's choice at a quarter (<see cref="BotQuarter"/>), the goods
/// taken marked hot for the fence, the withdrawal when the field turns, the report. What this adds is the sea: the choice of the
/// ship and the moment, and the crew. As the captain steps ashore with a band lying in wait, its hands form a company round it
/// (<see cref="BotSquads.Form"/>), and the company fights by the companies' rules (company16): a blow on a member turns the
/// company on whoever struck it, and it pulls back on results.
/// </para>
/// </summary>
public static class BotPiracy
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPiracy));

    /// <summary>Whether The Shadow waits for colony ships at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long after one boarding before another is laid. Half an hour.</summary>
    public static int EveryMs { get; set; } = 1800000;

    /// <summary>How near its quay, in sailing time, a ship must be for the band to be laid: long enough to walk there, short enough to wait.</summary>
    public static int LayAheadMs { get; set; } = 150000;

    /// <summary>How far from the quay a member of The Shadow may be and still be called to it.</summary>
    public static int PartyReach { get; set; } = 400;

    public static int LeastParty { get; set; } = 2;

    public static int MostParty { get; set; } = 3;

    /// <summary>The band's strength against the captain's and the hands' aboard, at least: the companies' choosing rule.</summary>
    public static double ChooseOdds { get; set; } = 1.0;

    /// <summary>The least a ship's goods must be worth, by the market, to be worth boarding.</summary>
    public static int LeastWorth { get; set; } = 150;

    public static long Heard { get; private set; }

    public static long Light { get; private set; }

    public static long Guarded { get; private set; }

    public static long Busy { get; private set; }

    public static long Short { get; private set; }

    public static long Outmatched { get; private set; }

    public static long Laid { get; private set; }

    public static long Struck { get; private set; }

    public static long Missed { get; private set; }

    public static long TakenUnits { get; private set; }

    public static long TakenWorth { get; private set; }

    public static long CrewFormed { get; private set; }

    private static readonly Dictionary<BotSailing, int> _watched = [];

    private static BotHighway.Party _party;

    private static BotSailing _for;

    private static long _laid;

    /// <summary>A loaded run put to sea: watched until it is near its quay. From <see cref="BotColonyRun"/>.</summary>
    public static void Sailing(BotSailing passage, int units, int worth)
    {
        if (!Running || passage == null || units <= 0)
        {
            return;
        }

        Heard++;

        if (worth < LeastWorth)
        {
            Light++;

            return;
        }

        _watched[passage] = worth;
    }

    /// <summary>From the colonies' clock: a watched ship near its quay gets its band; a band that is over is counted.</summary>
    public static void Look()
    {
        if (_party != null && _party.Closed)
        {
            Settle();
        }

        if (_watched.Count == 0)
        {
            return;
        }

        List<BotSailing> done = null;

        foreach (var (passage, worth) in _watched)
        {
            if (passage.State != BotSailing.Tide.Sailing || BotHelm.Of(passage.Captain) is not { } voyage || voyage.Boat == null)
            {
                (done ??= []).Add(passage);

                continue;
            }

            var left = voyage.Left(voyage.Boat.Location);
            var eta = (long)left * BotHelm.StrokeMs / Math.Max(1, BotHelm.Speed);

            if (eta > LayAheadMs)
            {
                continue;
            }

            (done ??= []).Add(passage);
            Lay(passage, worth);
        }

        for (var i = 0; done != null && i < done.Count; i++)
        {
            _watched.Remove(done[i]);
        }
    }

    private static readonly List<BotMobile> _band = [];

    private static void Lay(BotSailing passage, int worth)
    {
        var captain = passage.Captain;
        var map = passage.Map;
        var quay = passage.To?.First?.Shore ?? passage.To?.Shore ?? Point3D.Zero;

        if (!Running || captain == null || map == null || quay == Point3D.Zero)
        {
            return;
        }

        if (BotOutlaw.Guarded(map, quay.X, quay.Y, quay.Z))
        {
            Guarded++;

            return;
        }

        var now = Core.TickCount;

        if (_party is { Closed: false } || BotHighway.Now != null || _laid != 0 && now - _laid < EveryMs || !BotHighway.Running || !BotCrime.Running)
        {
            Busy++;

            return;
        }

        _band.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false, Alive: true, Squad: null } bot || bot.Map != map || !BotUnderworld.Member(bot) || BotFence.Is(bot)
                || BotOutlaw.Jailed(bot) || BotDungeon.Under(bot.Location) || bot.Tired || !Utility.InRange(bot.Location, quay, PartyReach)
                || !BotColonies.SameLand(map, bot.Location, quay) || !BotSquads.Pressable(bot))
            {
                continue;
            }

            _band.Add(bot);
        }

        if (_band.Count < Math.Max(1, LeastParty))
        {
            Short++;

            return;
        }

        _band.Sort((a, b) => a.GetDistanceToSqrt(quay).CompareTo(b.GetDistanceToSqrt(quay)));

        var party = new BotHighway.Party
        {
            Carrier = captain,
            Map = map,
            Cargo = worth,
            Formed = now,
            Spot = quay,
            Goal = quay,
            Quay = true,
            Town = BotCrime.TownAt(quay)
        };

        var power = 0.0;

        for (var i = 0; i < _band.Count && party.Members.Count < Math.Max(1, MostParty); i++)
        {
            party.Members.Add(_band[i]);
            power += BotThreat.Now(_band[i]);
        }

        var against = BotThreat.Now(captain);

        for (var i = 0; i < passage.Aboard.Count; i++)
        {
            if (passage.Aboard[i] is { Deleted: false, Alive: true } hand)
            {
                against += BotThreat.Now(hand);
            }
        }

        if (power < against * ChooseOdds)
        {
            Outmatched++;

            logger.Information(
                "The Shadow lets {Guild}'s ship into {Quay} with {Worth}gp of goods: {Count} of the band, {Power:F0} of strength, against {Against:F0} aboard",
                passage.Guild?.Name,
                passage.To?.Name,
                worth,
                party.Members.Count,
                power,
                against
            );

            return;
        }

        if (!BotHighway.Lay(party, $"a ship of {passage.Guild?.Name} coming into {passage.To?.Name} with {worth}gp of goods"))
        {
            Busy++;

            return;
        }

        _party = party;
        _for = passage;
        _laid = now;
        Laid++;

        logger.Information(
            "The Shadow waits on the quay at {Quay} ({X}, {Y}) for {Guild}'s ship under {Captain}, {Worth}gp of the colony's goods aboard and {Hands} hands: {Count} of the band, {Power:F0} of strength against {Against:F0}",
            passage.To?.Name,
            quay.X,
            quay.Y,
            passage.Guild?.Name,
            captain.Name,
            worth,
            passage.Aboard.Count,
            party.Members.Count,
            power,
            against
        );
    }

    /// <summary>
    /// The captain is ashore at a quay where the band lies in wait: how many of its hands stepped ashore with it, counted. From
    /// <see cref="BotColonyRun"/>. The company is formed when the band strikes (<see cref="Boarded"/>), not before: a company with
    /// no enemy goes looking for one, and the crew would be off hunting the dunes.
    /// </summary>
    public static void Landing(BotSailing passage)
    {
        if (passage == null || _for != passage || _party is not { Closed: false } || passage.Captain is not { Deleted: false, Alive: true } captain)
        {
            return;
        }

        var near = 0;

        for (var i = 0; i < passage.Aboard.Count; i++)
        {
            if (passage.Aboard[i] is { Deleted: false, Alive: true } hand && hand.InRange(captain.Location, 12))
            {
                near++;
            }
        }

        Landings++;
        HandsAshore += near;

        logger.Information("{Captain} lands at {Quay} where The Shadow lies in wait, {Near} hands ashore with it", captain.Name, passage.To?.Name, near);
    }

    public static long Landings { get; private set; }

    public static long HandsAshore { get; private set; }

    /// <summary>The band struck: from <c>BotHighway.Strike</c>. Two seconds on, when the first blow has made the band the aggressor, the crew forms up.</summary>
    public static void Boarded(BotHighway.Party party)
    {
        if (party == null || !ReferenceEquals(party, _party) || _for == null)
        {
            return;
        }

        var passage = _for;

        Timer.DelayCall(TimeSpan.FromSeconds(2), () => FormUp(party, passage));
    }

    /// <summary>
    /// The crew forms a company round its captain and turns on the band (<see cref="BotSquad.Engage"/>): from here it fights by the
    /// companies' rules (company16) - a blow on a member turns it, whoever can reach what is killing a member fights it, and it pulls
    /// back on results.
    /// </summary>
    private static void FormUp(BotHighway.Party party, BotSailing passage)
    {
        if (party.Closed || passage.Captain is not { Deleted: false, Alive: true } captain || captain.Map != party.Map)
        {
            return;
        }

        var squad = BotSquads.Form(captain);

        if (squad == null)
        {
            return;
        }

        var joined = 0;

        for (var i = 0; i < passage.Aboard.Count; i++)
        {
            if (passage.Aboard[i] is BotMobile { Deleted: false, Alive: true } hand && BotSquads.Join(squad, hand))
            {
                joined++;
            }
        }

        Mobile foe = null;

        for (var i = 0; i < party.Members.Count && foe == null; i++)
        {
            if (party.Members[i] is { Deleted: false, Alive: true } bandit && bandit.Map == captain.Map && bandit.InRange(captain.Location, 15))
            {
                foe = bandit;
            }
        }

        if (foe != null)
        {
            squad.Engage(foe, captain);
        }

        CrewFormed++;

        BotVoice.Say(captain, "local", joined > 0 ? "Pirates! Stand together and guard the goods!" : "Pirates! Keep back!", true);

        logger.Information(
            "{Captain}'s crew forms a company of {Count} against The Shadow's band at {Quay}{Foe}",
            captain.Name,
            joined + 1,
            passage.To?.Name,
            foe != null ? $", set on {foe.Name}" : ""
        );
    }

    /// <summary>A band that is over: what it came to, against the colony.</summary>
    private static void Settle()
    {
        var party = _party;
        var passage = _for;

        _party = null;
        _for = null;

        if (party == null)
        {
            return;
        }

        if (party.Hit)
        {
            Struck++;
            TakenUnits += party.Things;
            TakenWorth += party.Worth;

            if (passage?.Colony != null)
            {
                passage.Colony.Plundered += party.Things;
                passage.Colony.Last = $"boarded by The Shadow at {passage.To?.Name}: {party.Things} things worth {party.Worth}gp taken";
            }
        }
        else
        {
            Missed++;
        }

        logger.Information(
            "The Shadow's band on the quay at {Quay} for {Guild}'s ship is over: {Outcome}; {Things} things worth {Worth}gp taken",
            passage?.To?.Name,
            passage?.Guild?.Name,
            party.Hit ? party.CarrierDown ? "the captain killed" : party.Outcome ?? "struck" : "it never struck",
            party.Things,
            party.Worth
        );
    }

    public static string Describe() =>
        !Running
            ? "pirates are off"
            : Heard == 0
                ? "pirates: no loaded ship has sailed"
                : $"pirates: {Heard} loaded ships heard of ({Light} too light), {Guarded} making for a guarded quay, {Busy} with a band already out or too soon after the last, {Short} with too few of The Shadow near, {Outmatched} too strong to board, {Laid} bands laid on a quay, {Struck} struck and {Missed} never did, {TakenUnits} things worth {TakenWorth}gp taken, {Landings} ships landed into a band with {HandsAshore} hands, {CrewFormed} crews formed up";

    public static void Forget()
    {
        Heard = 0;
        Light = 0;
        Guarded = 0;
        Busy = 0;
        Short = 0;
        Outmatched = 0;
        Laid = 0;
        Struck = 0;
        Missed = 0;
        TakenUnits = 0;
        TakenWorth = 0;
        CrewFormed = 0;
        Landings = 0;
        HandsAshore = 0;
        _watched.Clear();
        _party = null;
        _for = null;
        _laid = 0;
    }
}
