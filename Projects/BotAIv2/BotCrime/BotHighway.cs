using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The ambush on the road: two or three of The Shadow lie in wait ahead of a lone carrier with goods worth taking, out of any
/// guarded town, and fall on it as it passes.
///
/// <para>
/// <b>Patrick, 07.10.2026: "ambushes on lone carriers with goods on the road … this gives a living reason for guards, escorts and
/// caravan guards."</b> A carrier is a bot on a delivery (<see cref="BotDeliver"/>) or a trader's haul (<see cref="BotHaul"/>)
/// whose goods are worth <see cref="LeastCargo"/> or more by the market's worth; lone means nobody of ours who could fight
/// within <see cref="EscortWithin"/> tiles, so an escort or a caravan guard makes it no target at all. One party at a time
/// (<see cref="EveryMs"/> between them), never in a guarded town, and only when the party is no weaker than the carrier
/// (<see cref="ChooseOdds"/>), the companies' own choosing rule.
/// </para>
///
/// <para>
/// <b>The fight is the engine's, and so is the carrier's answer.</b> The bandits close with <see cref="BotBrawl"/>'s robbery; the
/// carrier's reflexes (react14 on) fight, flee or cry for help as against anything; beaten to a quarter it chooses by
/// <see cref="BotQuarter"/>'s rule — help near or still a match: fight on; a ward in reach: run; otherwise throw half its pack
/// down — and the bandits take what is thrown and go. They withdraw as a company does, when what stands against them on the
/// field passes <c>BotSquad.WithdrawOdds</c> times their own strength. A carrier killed is a murder on the killer's record (red,
/// the Baron's patrol, the inquest), and its corpse is gone through. A carrier that lives reports the whole party: posted at once.
/// What is taken is stolen goods (<see cref="BotCrime.Heat"/>) and goes to the fence.
/// </para>
/// </summary>
public static class BotHighway
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHighway));

    /// <summary>Whether The Shadow lies in wait on the roads at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long after one ambush before another may be laid. Twenty minutes.</summary>
    public static int EveryMs { get; set; } = 1200000;

    /// <summary>How often the roads are looked over for a carrier.</summary>
    public static int SweepMs { get; set; } = 30000;

    /// <summary>
    /// The least a carrier's goods must be worth, by the market, to be worth an ambush — until the roads have been measured
    /// (<see cref="CargoSeen"/> carriers out of town), after which the bar is the worth that <see cref="CargoShare"/> of them carry
    /// or more, never under <see cref="CargoFloor"/>. life18a, 07.10.2026: the bar of 300gp let 73 of 1859 carriers on the roads
    /// through in nine hours (4 %); a haul's purchase is 87gp at the median and 149gp at the 80th percentile on this economy.
    /// </summary>
    public static int LeastCargo { get; set; } = 100;

    /// <summary>The share of the carriers on the roads whose cargo is worth an ambush: the bar is set at that quantile of what they carry.</summary>
    public static double CargoShare { get; set; } = 0.25;

    /// <summary>The bar never falls under this, however poor the roads.</summary>
    public static int CargoFloor { get; set; } = 40;

    /// <summary>Carriers out of town measured before the bar follows them; the last <see cref="CargoWindow"/> are kept.</summary>
    public static int CargoSeen { get; set; } = 40;

    public const int CargoWindow = 256;

    /// <summary>
    /// life18a: the spot is a point of the carrier's own route (the planner's, <c>BotErrand.Route</c>) where the carrier arrives
    /// after the band: its road there at least <see cref="Lead"/> times the nearest bandit's straight walk, plus
    /// <see cref="LeadSlack"/> tiles. Until now the spot lay <see cref="AheadTiles"/> ahead on the straight line to its goal, and
    /// the carrier was past it before the band had taken the call: three waits in three ambushes, the carrier never came by.
    /// </summary>
    public static double Lead { get; set; } = 1.3;

    public static int LeadSlack { get; set; } = 20;

    /// <summary>How near the carrier's road still ahead the spot must lie for the ambush to stand; farther, the carrier turned.</summary>
    public static int OnRoad { get; set; } = 12;

    /// <summary>How often an ambush is moved after its carrier before the band gives the carrier up.</summary>
    public static int MostRelays { get; set; } = 3;

    /// <summary>The fewest bandits an ambush is laid with.</summary>
    public static int LeastParty { get; set; } = 2;

    /// <summary>The most bandits in one ambush.</summary>
    public static int MostParty { get; set; } = 3;

    /// <summary>How far from the carrier a bandit may be to be called to the ambush.</summary>
    public static int PartyReach { get; set; } = 250;

    /// <summary>How far ahead of the carrier, along its road, the party lies in wait.</summary>
    public static int AheadTiles { get; set; } = 24;

    /// <summary>How long the party has to take the call before the ambush is called off.</summary>
    public static int MusterMs { get; set; } = 180000;

    /// <summary>How long the party lies in wait for the carrier to come past.</summary>
    public static int WaitMs { get; set; } = 240000;

    /// <summary>The longest an ambush stands, wait and fight together.</summary>
    public static int CapMs { get; set; } = 900000;

    /// <summary>How near the carrier must come to a hidden bandit for the party to strike (a route's points are walked to within 3).</summary>
    public static int StrikeWithin { get; set; } = 12;

    /// <summary>A fighter of ours this near the carrier makes it escorted, and no target.</summary>
    public static int EscortWithin { get; set; } = 12;

    /// <summary>How strong the party must be against the carrier to lay the ambush: the companies' choosing odds.</summary>
    public static double ChooseOdds { get; set; } = 1.0;

    /// <summary>
    /// The floor under what lying in wait is reckoned at a minute. life19: the price is the expected cargo now (the carrier's goods at
    /// the fence's share, split among the party, over <see cref="BotLieInWait.Minutes"/>); a floor of 120 claimed a take no ambush ever
    /// paid, and the review still sold it at 30–37/min to a delivery or a restock. The band is called by <see cref="Press"/> now, not
    /// by the auction, so the price is what the ledger reads, not what decides.
    /// </summary>
    public static double Prior { get; set; } = 1.0;

    /// <summary>
    /// <b>The band is mustered, as a company is (07.10.2026, life19).</b> On build 419 (2.3 h) three ambushes were laid and no bandit took
    /// the call: the deed was an offer in each bandit's auction and lost it ("over ambush: lying in wait for Norna at 37/min" to a delivery
    /// at 66/min; Joran's restock at 159/min over 30/min). With this on, the bandits within reach are pressed into the ambush the way a
    /// company's call presses the enlistment (<c>BotWill.Press</c>): work that resumes is put down to take up again, never dropped; a
    /// bandit at a fight, on a company's or a class's work, or carrying another bot's goods is passed over and counted by cause. The
    /// ambush is laid only when <see cref="LeastParty"/> are free to be pressed. Off: the old offer at auction.
    /// </summary>
    public static bool Press { get; set; } = true;

    /// <summary>
    /// The farthest along the carrier's road the spot may lie (life19): on 419 the spot was 561 and 703 tiles ahead, minutes of the
    /// carrier's walk, and both carriers were lost before they came near it.
    /// </summary>
    public static int MostAhead { get; set; } = 300;

    /// <summary>
    /// Whether a bandit strikes only when a second of the party is at hand (life19): the leader waits at the spot until two are
    /// committed, and does not fall on the carrier alone.
    /// </summary>
    public static bool StrikeTogether { get; set; } = true;

    /// <summary>
    /// <b>Cheap work is thrown away for the band's call (07.10.2026, life19b).</b> On build 420 (70 min) no ambush was laid: 16 calls had
    /// fewer than two free, and the commonest bar was "on work that would be thrown away" (14 of 31 passings over): an inscription, a
    /// restock, a hunt, a prowl — work that does not resume. Work reckoned under this many gp a minute is dropped for the ambush; dearer
    /// work still keeps its bandit. Zero: nothing is thrown away.
    /// </summary>
    public static double ThrowBelow { get; set; } = 40.0;

    /// <summary>
    /// <b>Hired hands (life19b).</b> Beside The Shadow's members and the thieves on the roll, a poor fighter (under <c>BotCrime.PoorBelow</c>)
    /// with the greedy or the reckless trait, of a class a thief may be, may be called to an ambush on a carrier not of its own guild or an
    /// ally's. Off: The Shadow and its thieves only.
    /// </summary>
    public static bool HireHands { get; set; } = true;

    public static long Sweeps { get; private set; }

    public static long Carriers { get; private set; }

    public static long Escorted { get; private set; }

    public static long Light { get; private set; }

    public static long InTown { get; private set; }

    public static long Short { get; private set; }

    public static long Outmatched { get; private set; }

    public static long Laid { get; private set; }

    public static long Joined { get; private set; }

    public static long Unmustered { get; private set; }

    public static long Struck { get; private set; }

    public static long Missed { get; private set; }

    public static long Paid { get; private set; }

    public static long Fled { get; private set; }

    public static long FoughtOn { get; private set; }

    public static long Killed { get; private set; }

    public static long Withdrawn { get; private set; }

    public static long BanditDeaths { get; private set; }

    public static long TakenGold { get; private set; }

    public static long TakenThings { get; private set; }

    public static long TakenWorth { get; private set; }

    /// <summary>life18a: carriers the band could not get ahead of on their road; ambushes moved after a carrier that turned; carriers lost.</summary>
    public static long NoCatch { get; private set; }

    public static long Relaid { get; private set; }

    public static long LostTrail { get; private set; }

    /// <summary>life18a: carriers that set their load down (delivered, dropped) while the band lay in wait.</summary>
    public static long Unloaded { get; private set; }

    /// <summary>life18a: bandits out of the recruits on the roll (not The Shadow's own) called to an ambush.</summary>
    public static long Recruits { get; private set; }

    /// <summary>life19: bandits pressed into an ambush; of them, thieves taken off a turn in a crowd; and the work put down for it.</summary>
    public static long Pressed { get; private set; }

    public static long FromTurn { get; private set; }

    public static long PutDown { get; private set; }

    /// <summary>life19b: work thrown away for the ambush (cheaper than <see cref="ThrowBelow"/>), by kind; and hired hands pressed in.</summary>
    public static long Thrown { get; private set; }

    public static long Hired { get; private set; }

    private static readonly Dictionary<string, long> _thrownKinds = [];

    /// <summary>life19: calls with a carrier worth it in reach and fewer than <see cref="LeastParty"/> bandits free to be pressed.</summary>
    public static long TooFew { get; private set; }

    /// <summary>life19: ambushes closed because fewer than two stayed committed before the carrier came; strikes held for a second bandit.</summary>
    public static long FellApart { get; private set; }

    public static long HeldStrike { get; private set; }

    /// <summary>life19: sweeps that kept an ambush whose carrier was off its drawn road but still bound for the same goal and short of the spot.</summary>
    public static long KeptOnGoal { get; private set; }

    /// <summary>life19: spots refused for lying more than <see cref="MostAhead"/> tiles along the carrier's road.</summary>
    public static long TooFarAhead { get; private set; }

    /// <summary>life19: bandits within reach of a call not pressed into it, by why (one count a bandit a call).</summary>
    private static readonly Dictionary<string, long> _refused = [];

    private static readonly int[] _cargo = new int[CargoWindow];

    private static int _cargoCount;

    private static int _cargoNext;

    private static readonly int[] _cargoSort = new int[CargoWindow];

    /// <summary>One ambush: who lies in wait, for whom, where, and how it has gone.</summary>
    public sealed class Party
    {
        public readonly List<BotMobile> Members = [];

        public BotMobile Carrier;

        public Map Map;

        public Point3D Spot;

        /// <summary>Bumped every time the spot moves after the carrier, so a bandit lying in wait starts its clock again.</summary>
        public int SpotStamp;

        public int Relays;

        public string Town;

        public int Cargo;

        public long Formed;

        public long StruckTick;

        public bool Hit;

        public bool Chosen;

        /// <summary>A strike was held for a second bandit at least once (life19).</summary>
        public bool HeldOnce;

        public bool Withdraw;

        /// <summary>Laid on a quay for a colony's ship by <see cref="BotPiracy"/>, not on a road: the zones paint it while it waits (08.10.2026).</summary>
        public bool Quay;

        public bool CarrierDown;

        public BotMobile Killer;

        public string Outcome;

        public long OddsTick;

        public bool Closed;

        public int Gold;

        public int Things;

        public int Worth;

        public readonly HashSet<Serial> Took = [];

        /// <summary>What each bandit took in goods, at what the fence pays: its work's takings beside the coin.</summary>
        public readonly Dictionary<Serial, int> Made = [];

        public readonly HashSet<Serial> Ended = [];

        /// <summary>Where the carrier was bound when the ambush was laid (life19): a carrier off its drawn road but bound there still is followed.</summary>
        public Point3D Goal;

        /// <summary>Bandits that took the call and are still on it, alive.</summary>
        public int Committed()
        {
            var count = 0;

            for (var i = 0; i < Members.Count; i++)
            {
                if (Members[i] is { Deleted: false, Alive: true } member && Took.Contains(member.Serial) && !Ended.Contains(member.Serial))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Committed bandits within <paramref name="range"/> of a point: who is at hand for the strike.</summary>
        public int AtHand(Point3D at, int range)
        {
            var count = 0;

            for (var i = 0; i < Members.Count; i++)
            {
                if (Members[i] is { Deleted: false, Alive: true } member && member.Map == Map && Took.Contains(member.Serial)
                    && !Ended.Contains(member.Serial) && member.InRange(at, range))
                {
                    count++;
                }
            }

            return count;
        }

        public bool Holds(Mobile m)
        {
            for (var i = 0; i < Members.Count; i++)
            {
                if (ReferenceEquals(Members[i], m))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static Party _party;

    private static long _sweptTick;

    private static long _laidTick;

    private static bool _clock;

    /// <summary>The ambush standing now, or null.</summary>
    public static Party Now => _party is { Closed: false } ? _party : null;

    /// <summary>One look at the roads, from <see cref="BotCrime.Beat"/>: the standing ambush watched, or a new one laid.</summary>
    public static void Sweep(long now)
    {
        if (now - _sweptTick < SweepMs)
        {
            return;
        }

        _sweptTick = now;

        if (_party is { Closed: false } party)
        {
            if (!Running)
            {
                Close(party, "the ambushes are switched off");
            }
            else if (now - party.Formed >= CapMs)
            {
                Close(party, "it ran to its cap");
            }
            else if (party.Took.Count == 0 && now - party.Formed >= MusterMs)
            {
                Unmustered++;
                Close(party, "nobody of the band took the call");
            }
            else if (party.Took.Count > 0 && party.Ended.Count >= party.Took.Count)
            {
                Close(party, party.Outcome ?? "every bandit is done");
            }
            else if (Press && !party.Hit && party.Took.Count > 0 && party.Committed() < Math.Max(1, LeastParty))
            {
                // life19: the leader waits at the spot only while two are committed; one left alone does not take the carrier on.
                FellApart++;
                Close(party, $"fewer than {Math.Max(1, LeastParty)} of the band were left on it before the carrier came");
            }
            else if (!party.Hit)
            {
                Follow(party);
            }

            return;
        }

        if (!Running || !BotCrime.Running || !BotUnderworld.Exists)
        {
            return;
        }

        // The first look after a boot starts the clock, so a restart is not a free ambush.
        if (!_clock)
        {
            _clock = true;
            _laidTick = now;

            return;
        }

        if (now - _laidTick < EveryMs)
        {
            return;
        }

        Sweeps++;
        Lay(now);
    }

    private static readonly List<BotMobile> _band = [];

    /// <summary>Looks for a lone carrier worth the trouble and a party near enough to take it, and lays the ambush.</summary>
    private static void Lay(long now)
    {
        var map = BotPopulation.Home;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        _band.Clear();
        _free.Clear();

        var bots = BotPopulation.Bots;

        // life19: with the press on, every bandit on the map is looked at, so the call can count who could not come and why; the carrier
        // is chosen by the nearest of them, and the party out of those free to be pressed (Callable).
        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false, Alive: true } bot && bot.Map == map && (Bandit(bot) || Press && HireHands && Hireling(bot)) && !BotFence.Is(bot)
                && !BotOutlaw.Jailed(bot) && !BotDungeon.Under(bot.Location) && (Press || bot.Squad == null && !bot.Tired))
            {
                _band.Add(bot);

                if (!Press || Callable(bot) == null)
                {
                    _free.Add(bot.Serial);
                }
            }
        }

        if (_band.Count < Math.Max(1, LeastParty))
        {
            Short++;

            return;
        }

        BotMobile carrier = null;
        var cargo = 0;
        var nearest = int.MaxValue;
        var bar = Bar();

        // life19: the carrier a free bandit is nearest; failing one, the carrier any bandit is nearest, so the call is made and
        // who could not come is counted by why.
        BotMobile busyCarrier = null;
        var busyCargo = 0;
        var busyNearest = int.MaxValue;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false, Alive: true, Squad: null } bot || bot.Map != map || Bandit(bot)
                || Goods(bot) is not { } goods || BotOutlaw.Outlaw(bot) || BotDungeon.Under(bot.Location))
            {
                continue;
            }

            Carriers++;

            if (BotOutlaw.Guarded(map, bot.X, bot.Y, bot.Z))
            {
                InTown++;

                continue;
            }

            var worth = Cargo(bot, goods);

            Measure(worth);

            if (worth < bar)
            {
                Light++;

                continue;
            }

            if (Escort(bot, map))
            {
                Escorted++;

                continue;
            }

            // The carrier nearest any bandit: the one the band can reach first.
            var any = Nearest(bot, false);

            if (any <= PartyReach && any < busyNearest)
            {
                busyNearest = any;
                busyCarrier = bot;
                busyCargo = worth;
            }

            var away = Press ? Nearest(bot, true) : any;

            if (away > PartyReach || away >= nearest)
            {
                continue;
            }

            nearest = away;
            carrier = bot;
            cargo = worth;
        }

        if (carrier == null)
        {
            carrier = busyCarrier;
            cargo = busyCargo;
        }

        if (carrier == null)
        {
            return;
        }

        _band.Sort((a, b) => a.GetDistanceToSqrt(carrier.Location).CompareTo(b.GetDistanceToSqrt(carrier.Location)));

        var party = new Party { Carrier = carrier, Map = map, Cargo = cargo, Formed = now, Goal = carrier.Resolve?.Deed?.Where ?? Point3D.Zero };
        var power = 0.0;
        _passed.Clear();
        _passedWho.Clear();

        for (var i = 0; i < _band.Count && party.Members.Count < Math.Max(1, MostParty); i++)
        {
            if (!Utility.InRange(_band[i].Location, carrier.Location, PartyReach))
            {
                break;
            }

            // life19b: a hired hand is not set on a carrier of its own guild or an ally's.
            if (!Bandit(_band[i]) && _band[i].Guild is Server.Guilds.Guild own && carrier.Guild is Server.Guilds.Guild theirs
                && (ReferenceEquals(own, theirs) || own.IsAlly(theirs)))
            {
                _passed.Add("a hired hand of the carrier's guild or an ally");
                _passedWho.Add($"{_band[i].Name} (of the carrier's guild or an ally)");

                continue;
            }

            // life19: only a bandit the call can press without throwing its work away; the rest are counted by why.
            if (Press && Callable(_band[i]) is { } why)
            {
                _passed.Add(why);
                _passedWho.Add($"{_band[i].Name} ({why})");

                continue;
            }

            party.Members.Add(_band[i]);
            power += BotThreat.Now(_band[i]);
        }

        if (party.Members.Count < Math.Max(1, LeastParty))
        {
            Short++;

            if (Press)
            {
                TooFew++;

                for (var i = 0; i < _passed.Count; i++)
                {
                    _refused[_passed[i]] = _refused.GetValueOrDefault(_passed[i]) + 1;
                }
            }

            return;
        }

        for (var i = 0; i < _passed.Count; i++)
        {
            _refused[_passed[i]] = _refused.GetValueOrDefault(_passed[i]) + 1;
        }

        var against = BotThreat.Now(carrier);

        if (power < against * ChooseOdds)
        {
            Outmatched++;

            logger.Information(
                "The Shadow lets {Carrier} pass with {Cargo}gp of goods: {Count} of the band, {Power:F0} of strength, against its {Against:F0}",
                carrier.Name,
                cargo,
                party.Members.Count,
                power,
                against
            );

            return;
        }

        party.Spot = Ahead(map, carrier, party.Members);

        if (party.Spot == Point3D.Zero)
        {
            NoCatch++;

            logger.Information(
                "The Shadow lets {Carrier} pass with {Cargo}gp of goods: no point of its road ahead, out of the guards, that it reaches after the band",
                carrier.Name,
                cargo
            );

            return;
        }

        for (var i = 0; i < party.Members.Count; i++)
        {
            if (!BotUnderworld.Member(party.Members[i]))
            {
                Recruits++;
            }
        }

        party.Town = BotCrime.TownAt(party.Spot);
        _party = party;
        _laidTick = now;
        Laid++;

        List<string> names = [];

        for (var i = 0; i < party.Members.Count; i++)
        {
            names.Add(party.Members[i].Name);
        }

        logger.Information(
            "The Shadow lays an ambush for {Carrier} the {Class}, carrying {Cargo}gp of goods alone (the bar {Bar}gp), at ({X}, {Y}) near {Town}, {Ahead} tiles ahead on its road: {Names}, {Power:F0} of strength against its {Against:F0}{Passed}",
            carrier.Name,
            carrier.Class?.Name,
            cargo,
            bar,
            party.Spot.X,
            party.Spot.Y,
            party.Town,
            Along(carrier, party.Spot),
            string.Join(", ", names),
            power,
            against,
            _passedWho.Count > 0 ? $"; passed over: {string.Join(", ", _passedWho)}" : ""
        );

        if (!Press)
        {
            return;
        }

        // life19: the call presses the ambush on each of them, as a company's call presses the enlistment. Taken() counts the taker.
        for (var i = 0; i < party.Members.Count; i++)
        {
            if (party.Members[i] is not IBotWilful wilful)
            {
                continue;
            }

            var turn = wilful.Resolve?.Deed is BotPickpocket;
            var held = wilful.Resolve?.Deed != null;
            var thrownKind = wilful.Resolve is { Deed: { } heldDeed } heldResolve
                             && (!BotWill.Resume || !heldDeed.Resumes || heldResolve.Paused != null)
                ? heldDeed.Kind
                : null;
            var hired = !Bandit(party.Members[i]);

            if (BotWill.Press(wilful, new BotLieInWait(party), $"called to lie in wait for {carrier.Name}'s {cargo}gp of goods"))
            {
                Pressed++;

                if (thrownKind != null)
                {
                    Thrown++;
                    _thrownKinds[thrownKind] = _thrownKinds.GetValueOrDefault(thrownKind) + 1;
                }
                else if (held)
                {
                    PutDown++;
                }

                if (hired)
                {
                    Hired++;
                }

                if (turn)
                {
                    FromTurn++;
                }

                BotVoice.Say(party.Members[i], "local", i == 0 ? $"{carrier.Name} is on the road with goods. With me." : "Coming.", true);
            }
        }

        if (party.Committed() < Math.Max(1, LeastParty))
        {
            FellApart++;
            Close(party, "the band could not be called together");
        }
    }

    /// <summary>
    /// A band laid by another's choosing (07.10.2026, sea20): The Shadow on a quay for a colony's loaded ship (<see cref="BotPiracy"/>).
    /// The party is made the standing ambush and pressed as <see cref="Lay(long)"/> presses its own, and everything after it — the wait,
    /// the strike, the robbery, the quarter, the loot, the report, the withdrawal — is this file's. False when an ambush stands already,
    /// the ambushes are off, or the band could not be called together.
    /// </summary>
    public static bool Lay(Party party, string why)
    {
        if (!Running || party == null || party.Members.Count == 0 || party.Carrier == null || _party is { Closed: false })
        {
            return false;
        }

        _party = party;
        _laidTick = Core.TickCount;
        _clock = true;
        Laid++;

        for (var i = 0; i < party.Members.Count; i++)
        {
            if (party.Members[i] is IBotWilful wilful && BotWill.Press(wilful, new BotLieInWait(party), $"called to lie in wait for {why}"))
            {
                Pressed++;

                BotVoice.Say(party.Members[i], "local", i == 0 ? "A ship's coming in heavy. To the quay, with me." : "Coming.", true);
            }
        }

        if (party.Committed() < Math.Max(1, LeastParty))
        {
            FellApart++;
            Close(party, "the band could not be called together on the quay");

            return false;
        }

        logger.Information("The Shadow lays a band on the quay at ({X}, {Y}) near {Town} for {Why}", party.Spot.X, party.Spot.Y, party.Town, why);

        return true;
    }

    /// <summary>
    /// A hired hand (life19b): not of the band, a poor fighter of a class a thief may be (crime17's rule), greedy or reckless, no guild's
    /// head, not wanted, not carrying goods of its own errand.
    /// </summary>
    private static bool Hireling(BotMobile bot) =>
        !Bandit(bot) && !BotOutlaw.Outlaw(bot) && Goods(bot) == null && bot.Class is { } klass
        && klass.Role is not (BotRole.Producer or BotRole.Medic) && !klass.Unpaid && !klass.Leads && !klass.Neutral && klass is not (BotSage or BotArchitect)
        && (BotTraits.Has(bot, BotTrait.Greedy) || BotTraits.Has(bot, BotTrait.Reckless)) && !BotGuilds.IsHead(bot)
        && BotYield.Wealth(bot) < BotCrime.PoorBelow;

    private static readonly List<string> _passed = [];

    private static readonly List<string> _passedWho = [];

    /// <summary>
    /// Why a bandit cannot be pressed into the ambush now, or null when it can (life19). The rule a company's call reads
    /// (<c>BotSquads.Pressable</c>) — work that resumes is put down, never thrown away; a fight, a company's or a class's work and another
    /// bot's goods are left alone — except that a thief's turn in a crowd is put down for it: the band's own call outranks its own errand.
    /// </summary>
    private static string Callable(BotMobile bot)
    {
        if (bot.Squad != null)
        {
            return "in a company";
        }

        if (bot.Tired)
        {
            return "tired";
        }

        if (bot.HitsMax > 0 && bot.Hits < bot.HitsMax * 0.7)
        {
            return "hurt";
        }

        if (bot.Combatant != null)
        {
            return "at a fight";
        }

        var resolve = bot.Resolve;

        if (resolve?.Deed is not { } deed)
        {
            return null;
        }

        if (deed is BotLieInWait)
        {
            return "on another ambush";
        }

        if (deed.Alongside)
        {
            return "answering a company or its class";
        }

        if (deed.Foe != null || BotSquads.Fighting(bot))
        {
            return "at a fight";
        }

        if (BotSquads.Carrying(bot))
        {
            return "carrying another's goods";
        }

        if (deed.Summons)
        {
            return "summoned";
        }

        if ((!BotWill.Resume || !deed.Resumes || resolve.Paused != null) && (ThrowBelow <= 0.0 || deed.Expects >= ThrowBelow || deed.Committed))
        {
            return "on work that would be thrown away";
        }

        return null;
    }

    /// <summary>A bandit: one of The Shadow, or a thief on the roll the band recruited (life18a: The Shadow alone was two bodies).</summary>
    public static bool Bandit(Mobile m) => BotUnderworld.Member(m) || BotCrime.IsThief(m);

    /// <summary>The kind a bot is carrying on the road for somebody: a delivery's, a haul's, or a caravan's (life18a); else null.</summary>
    private static Type Goods(BotMobile bot) =>
        bot.Resolve?.Deed switch
        {
            IBotWantCarrier carrying => carrying.Goods,
            BotCaravan caravan => caravan.Goods,

            // A colony's ship's captain carrying the store home (07.10.2026, sea20): what a band laid on its quay waits for. See BotPiracy.
            BotColonyRun run => run.Goods,
            _ => null
        };

    /// <summary>One carrier's cargo out of town, into the window the bar is read off. A carrier with nothing in its pack yet (on its way to buy) is not measured.</summary>
    private static void Measure(int worth)
    {
        if (worth <= 0)
        {
            return;
        }

        _cargo[_cargoNext] = Math.Max(0, worth);
        _cargoNext = (_cargoNext + 1) % CargoWindow;
        _cargoCount = Math.Min(CargoWindow, _cargoCount + 1);
    }

    /// <summary>What a carrier's cargo must come to: <see cref="LeastCargo"/> until measured, then the top <see cref="CargoShare"/>'s floor.</summary>
    public static int Bar()
    {
        if (_cargoCount < Math.Max(1, CargoSeen))
        {
            return Math.Max(CargoFloor, LeastCargo);
        }

        return Math.Max(CargoFloor, Quantile(1.0 - Math.Clamp(CargoShare, 0.01, 1.0)));
    }

    /// <summary>The cargo at this quantile of the window. A copy of 256 numbers sorted: every 30 seconds at most.</summary>
    private static int Quantile(double q)
    {
        if (_cargoCount == 0)
        {
            return 0;
        }

        Array.Copy(_cargo, _cargoSort, _cargoCount);
        Array.Sort(_cargoSort, 0, _cargoCount);

        return _cargoSort[Math.Clamp((int)(q * _cargoCount), 0, _cargoCount - 1)];
    }

    /// <summary>What the carrier's goods come to at the market's worth: the kind it carries, in its pack.</summary>
    private static int Cargo(BotMobile carrier, Type goods)
    {
        if (goods == null || carrier.Backpack is not { } pack)
        {
            return 0;
        }

        return pack.GetAmount(goods) * BotCrime.Worth(goods);
    }

    /// <summary>Whether a fighter of ours is near enough to the carrier to stand with it.</summary>
    private static bool Escort(BotMobile carrier, Map map)
    {
        foreach (var other in map.GetMobilesInRange<BotMobile>(carrier.Location, Math.Max(1, EscortWithin)))
        {
            if (other == carrier || other is not { Deleted: false, Alive: true } || Bandit(other) || BotOutlaw.Outlaw(other)
                || other.Class is not { } klass || klass.Role is BotRole.Producer || klass.Neutral)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static readonly HashSet<Serial> _free = [];

    /// <summary>The nearest bandit to the carrier, in tiles; of those free to be pressed only, when <paramref name="free"/>.</summary>
    private static int Nearest(BotMobile carrier, bool free)
    {
        var nearest = int.MaxValue;

        for (var i = 0; i < _band.Count; i++)
        {
            if (free && !_free.Contains(_band[i].Serial))
            {
                continue;
            }

            var away = (int)_band[i].GetDistanceToSqrt(carrier.Location);

            if (away < nearest)
            {
                nearest = away;
            }
        }

        return nearest;
    }

    private static readonly List<Point3D> _road = [];

    /// <summary>
    /// The carrier's road still ahead: the points of the route the planner drew for the errand it walks (from the leg in hand),
    /// then its goal — or, with no route held (a near goal walked straight, a fight on top), the straight line to its work's place,
    /// a point every 16 tiles. A route that ends on a gate stops there: what lies beyond it is another place.
    /// </summary>
    private static List<Point3D> Road(BotMobile carrier)
    {
        _road.Clear();

        var errand = carrier.Journey?.Current;

        if (errand != null && !errand.RouteSpent && errand.Route.Count > 0 && errand.Leg < errand.Route.Count)
        {
            for (var i = Math.Max(0, errand.Leg); i < errand.Route.Count; i++)
            {
                _road.Add(errand.Route[i]);
            }

            if (errand.Gate == Point3D.Zero && errand.Follow == null)
            {
                _road.Add(errand.Target);
            }

            return _road;
        }

        var goal = carrier.Resolve?.Deed?.Where ?? Point3D.Zero;

        if (goal == Point3D.Zero || carrier.Map == null)
        {
            return _road;
        }

        var from = carrier.Location;
        var dx = goal.X - from.X;
        var dy = goal.Y - from.Y;
        var pieces = Math.Max(1, (int)(Math.Sqrt(dx * (double)dx + dy * (double)dy) / 16));

        for (var k = 1; k <= pieces; k++)
        {
            var x = from.X + dx * k / pieces;
            var y = from.Y + dy * k / pieces;

            _road.Add(k == pieces ? goal : new Point3D(x, y, carrier.Map.GetAverageZ(x, y)));
        }

        return _road;
    }

    private static double Away(Point3D a, Point3D b) => Math.Sqrt((a.X - b.X) * (double)(a.X - b.X) + (a.Y - b.Y) * (double)(a.Y - b.Y));

    /// <summary>
    /// The first point of the carrier's road, out of the guards and at least <see cref="AheadTiles"/> along it, that the carrier
    /// reaches after the band does: its road there at least <see cref="Lead"/> times the nearest bandit's straight walk, plus
    /// <see cref="LeadSlack"/>. Nowhere when the band cannot get ahead of it before its road ends (in a town, at a gate).
    /// </summary>
    private static Point3D Ahead(Map map, BotMobile carrier, List<BotMobile> band)
    {
        var road = Road(carrier);
        var along = 0.0;
        var at = carrier.Location;

        for (var i = 0; i < road.Count; i++)
        {
            var point = road[i];

            along += Away(at, point);
            at = point;

            // life19: not minutes of the carrier's walk off; it turns or sets its load down long before it gets there.
            if (MostAhead > 0 && along > MostAhead)
            {
                TooFarAhead++;

                return Point3D.Zero;
            }

            if (along < AheadTiles || BotOutlaw.Guarded(map, point.X, point.Y, point.Z) || BotDungeon.Under(point))
            {
                continue;
            }

            var nearest = double.MaxValue;

            for (var b = 0; b < band.Count; b++)
            {
                if (band[b] is { Deleted: false, Alive: true } bandit && bandit.Map == map)
                {
                    nearest = Math.Min(nearest, Away(bandit.Location, point));
                }
            }

            if (nearest < double.MaxValue && along >= nearest * Math.Max(1.0, Lead) + LeadSlack)
            {
                return point;
            }
        }

        return Point3D.Zero;
    }

    /// <summary>The distance from a point to the stretch a–b.</summary>
    private static double Off(Point3D p, Point3D a, Point3D b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        var length = dx * dx + dy * dy;
        var t = length <= 0.0 ? 0.0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length, 0.0, 1.0);
        var x = a.X + t * dx - p.X;
        var y = a.Y + t * dy - p.Y;

        return Math.Sqrt(x * x + y * y);
    }

    /// <summary>How far along its road ahead the carrier comes nearest the spot, in tiles; -1 when its road does not pass within <see cref="OnRoad"/>.</summary>
    private static int Along(BotMobile carrier, Point3D spot)
    {
        var road = Road(carrier);
        var along = 0.0;
        var at = carrier.Location;

        for (var i = 0; i < road.Count; i++)
        {
            if (Off(spot, at, road[i]) <= OnRoad)
            {
                return (int)(along + Away(at, road[i]));
            }

            along += Away(at, road[i]);
            at = road[i];
        }

        return -1;
    }

    /// <summary>
    /// The standing ambush, every sweep until it is sprung (life18a): the carrier still carrying, and the spot still on its road
    /// ahead; a carrier that turned has the ambush moved after it, at most <see cref="MostRelays"/> times.
    /// </summary>
    private static void Follow(Party party)
    {
        var carrier = party.Carrier;

        if (carrier is not { Deleted: false, Alive: true } || carrier.Map != party.Map)
        {
            Close(party, "the carrier is gone");

            return;
        }

        if (Goods(carrier) == null)
        {
            Unloaded++;
            Close(party, $"{carrier.Name} set its load down before it came by");

            return;
        }

        if (carrier.InRange(party.Spot, StrikeWithin) || Along(carrier, party.Spot) >= 0)
        {
            return;
        }

        // life19: off the road the planner drew (a fresh plan, a step round a creature, the straight line while no route is held) but
        // still bound where it was and not yet past the spot: the band keeps its place. On 419 three carriers were "lost" 30-90 seconds
        // after the ambush was laid.
        if (party.Goal != Point3D.Zero && carrier.Resolve?.Deed?.Where is { } goal && goal == party.Goal
            && Away(carrier.Location, goal) > Away(party.Spot, goal) + StrikeWithin)
        {
            KeptOnGoal++;

            return;
        }

        var spot = party.Relays < Math.Max(0, MostRelays) ? Ahead(party.Map, carrier, party.Members) : Point3D.Zero;

        if (spot == Point3D.Zero)
        {
            LostTrail++;
            Close(party, $"{carrier.Name} left the road the band lay on, and the band lost it");

            return;
        }

        party.Spot = spot;
        party.Relays++;
        party.SpotStamp++;
        party.Town = BotCrime.TownAt(spot);
        Relaid++;

        logger.Information(
            "The Shadow moves its ambush for {Carrier}: it left the road the band lay on; now ({X}, {Y}) near {Town}, {Ahead} tiles ahead on its road (move {Relays} of {Most})",
            carrier.Name,
            spot.X,
            spot.Y,
            party.Town,
            Along(carrier, spot),
            party.Relays,
            MostRelays
        );
    }

    /// <summary>The ambush is over: what came of it said, and the carrier, if it lives, reports every bandit.</summary>
    private static void Close(Party party, string why)
    {
        if (party.Closed)
        {
            return;
        }

        party.Closed = true;

        if (party.Hit && party.Carrier is { Deleted: false, Alive: true } carrier && !party.CarrierDown)
        {
            for (var i = 0; i < party.Members.Count; i++)
            {
                if (party.Members[i] is { Deleted: false } bandit)
                {
                    var sheet = BotCrime.Of(bandit.Name, true);

                    sheet.Ambushes++;
                    BotCrime.Report(bandit, carrier, party.Town, "an ambush on the road", i == 0 ? party.Worth : 0, Math.Max(1, BotCrime.ReportsToPost));
                }
            }
        }

        logger.Information(
            "The ambush on {Carrier} near {Town} is over: {Why}; {Gold}gp and {Things} things worth {Worth}gp taken, the carrier {Fate}",
            party.Carrier?.Name,
            party.Town,
            why,
            party.Gold,
            party.Things,
            party.Worth,
            party.CarrierDown ? "dead" : party.Hit ? "alive and telling" : "never struck"
        );
    }

    /// <summary>A bot died: counted if it was the carrier or a bandit of the standing ambush. From <see cref="BotCrime.Fell"/>.</summary>
    public static void Fell(BotMobile fallen, Mobile killer)
    {
        var party = _party;

        if (party == null || party.Closed && Core.TickCount - party.Formed > CapMs)
        {
            return;
        }

        if (ReferenceEquals(fallen, party.Carrier) && party.Hit && !party.CarrierDown)
        {
            party.CarrierDown = true;
            Killed++;

            var by = killer as BotMobile;

            if (by == null || !party.Holds(by))
            {
                by = party.Members.Count > 0 ? party.Members[0] : null;
            }

            party.Killer = by;

            // A carrier killed on the road is murder, by the outlaws' own book: red, the Baron's patrol, the body to be found.
            if (by != null)
            {
                BotOutlaw.Murdered(by, fallen);
                BotUnderworld.Murder(by, false);
            }

            return;
        }

        if (party.Holds(fallen) && party.Hit)
        {
            BanditDeaths++;
        }
    }

    /// <summary>The rule every bandit's fight follows: over at a withdrawal, at the carrier's choice, or when the carrier is down.</summary>
    public static string Over(IBotWilful bot, BotBrawl brawl)
    {
        var party = _party;

        if (party == null || party.Closed)
        {
            return "the ambush is over";
        }

        if (party.Withdraw)
        {
            return "the band withdrew";
        }

        if (party.Outcome != null)
        {
            return party.Outcome;
        }

        var carrier = party.Carrier;

        if (carrier is not { Deleted: false, Alive: true })
        {
            return null;
        }

        var now = Core.TickCount;

        if (now - party.OddsTick >= 3000)
        {
            party.OddsTick = now;

            if (Overmatched(party, carrier))
            {
                party.Withdraw = true;
                Withdrawn++;

                if (bot?.Self is { } self)
                {
                    BotVoice.Say(self, "local", "Too many of them! Scatter!", true);
                }

                logger.Information("The ambush on {Carrier} is called off: too many stand with it, and the band withdraws", carrier.Name);

                return "the band withdrew";
            }
        }

        if (!party.Chosen && BotQuarter.Beaten(carrier) && bot?.Self is BotMobile robber)
        {
            party.Chosen = true;

            var before = BotCrime.Census(robber);
            var answer = BotQuarter.Choose(robber, carrier);

            switch (answer)
            {
                case BotQuarter.Answer.Pay:
                {
                    Paid++;
                    Took(party, robber, before);
                    party.Outcome = $"{carrier.Name} threw down its goods";

                    return party.Outcome;
                }
                case BotQuarter.Answer.Flee:
                {
                    Fled++;
                    party.Outcome = $"{carrier.Name} ran for it";

                    return party.Outcome;
                }
                default:
                {
                    FoughtOn++;

                    break;
                }
            }
        }

        return null;
    }

    /// <summary>Whether what stands with the carrier outweighs the band on the field, by the companies' withdrawal odds.</summary>
    private static bool Overmatched(Party party, BotMobile carrier)
    {
        var map = carrier.Map;

        if (map == null)
        {
            return false;
        }

        var theirs = 0.0;

        foreach (var other in map.GetMobilesInRange<BotMobile>(carrier.Location, Math.Max(1, EscortWithin)))
        {
            if (other is { Deleted: false, Alive: true } && !party.Holds(other) && !Bandit(other) && !BotOutlaw.Outlaw(other))
            {
                theirs += BotThreat.Now(other);
            }
        }

        var ours = 0.0;

        for (var i = 0; i < party.Members.Count; i++)
        {
            if (party.Members[i] is { Deleted: false, Alive: true } bandit && bandit.Map == map
                && bandit.InRange(carrier.Location, EscortWithin * 2))
            {
                ours += BotThreat.Now(bandit);
            }
        }

        return ours <= 0.0 || theirs > ours * Math.Max(1.0, BotSquad.WithdrawOdds);
    }

    /// <summary>What came into a bandit's pack since the census: counted, and made stolen goods.</summary>
    public static void Took(Party party, BotMobile bandit, Dictionary<Type, int> before)
    {
        List<(Type Kind, int Amount)> goods = [];
        var gold = BotCrime.Gained(before, BotCrime.Census(bandit), goods);
        var things = 0;
        var worth = gold;

        for (var i = 0; i < goods.Count; i++)
        {
            things += goods[i].Amount;
            worth += goods[i].Amount * BotCrime.Worth(goods[i].Kind);
            BotCrime.Heat(bandit, goods[i].Kind, goods[i].Amount, party.Town, party.Carrier?.Name);
        }

        party.Made[bandit.Serial] = party.Made.GetValueOrDefault(bandit.Serial) + (int)((worth - gold) * BotFencer.FenceShare);
        party.Gold += gold;
        party.Things += things;
        party.Worth += worth;
        TakenGold += gold;
        TakenThings += things;
        TakenWorth += worth;

        if (gold > 0 || things > 0)
        {
            BotUnderworld.Robbed(bandit);
            BotEvents.Post("crime", bandit, $"took {gold}gp and {things} things off {party.Carrier?.Name} on the road near {party.Town}");
        }
    }

    public static void Taking(Party party, Mobile bandit)
    {
        if (party.Took.Add(bandit.Serial))
        {
            Joined++;
        }
    }

    public static void Done(Party party, Mobile bandit) => party.Ended.Add(bandit.Serial);

    public static void Strike(Party party, BotMobile by)
    {
        if (party.Hit)
        {
            return;
        }

        party.Hit = true;
        party.StruckTick = Core.TickCount;
        Struck++;

        // A band on a quay for a colony's ship: the crew forms a company round its captain (07.10.2026, sea20). See BotPiracy.
        BotPiracy.Boarded(party);

        BotVoice.Say(by, "local", $"Stand and deliver, {party.Carrier?.Name}! The road is ours tonight.", true);
        BotEvents.Post("crime", by, $"springs an ambush on {party.Carrier?.Name} near {party.Town}");

        logger.Information(
            "{By} of The Shadow springs the ambush on {Carrier} at ({X}, {Y}) near {Town}",
            by.Name,
            party.Carrier?.Name,
            by.X,
            by.Y,
            party.Town
        );
    }

    public static void Miss() => Missed++;

    /// <summary>
    /// Whether enough of the band is at hand to fall on the carrier (life19): <see cref="LeastParty"/> of the committed within twice
    /// <see cref="StrikeWithin"/> of it, or all that are committed when fewer took the call. A strike held is counted once an ambush.
    /// </summary>
    public static bool Together(Party party, BotMobile carrier)
    {
        if (!Press || !StrikeTogether || party == null || carrier == null)
        {
            return true;
        }

        var need = Math.Min(Math.Max(1, LeastParty), Math.Max(1, party.Committed()));

        if (party.AtHand(carrier.Location, StrikeWithin * 2) >= need)
        {
            return true;
        }

        if (!party.HeldOnce)
        {
            party.HeldOnce = true;
            HeldStrike++;
        }

        return false;
    }

    public static string Describe() =>
        !Running
            ? "the roads: off"
            : $"the roads: {Sweeps} looks for a carrier ({Carriers} carriers seen, {InTown} in a guarded town, {Light} carrying under the bar, {Escorted} escorted, {Short} looks with the band too few, {Outmatched} let pass as too strong, {NoCatch} the band could not get ahead of on their road; the bar {Bar()}gp, {CargoBook()}), {Laid} ambushes laid ({Recruits} recruits called beside The Shadow), {Joined} bandits took the call and {Unmustered} ambushes nobody came to; {Mustered()}; {Relaid} moved after a carrier that turned, {LostTrail} carriers lost, {Unloaded} set their load down first; {Struck} sprung and {Missed} waits the carrier never came by; the carrier threw its goods down {Paid} times, ran {Fled}, fought on {FoughtOn} and died {Killed}; {Withdrawn} withdrawals at the odds; {BanditDeaths} bandits died at it; {TakenGold}gp and {TakenThings} things worth {TakenWorth}gp taken";

    /// <summary>"the muster: 6 pressed (2 off a turn in a crowd, 4 putting work down), 3 calls with too few free; passed over: in a company 4, …".</summary>
    private static string Mustered()
    {
        if (!Press)
        {
            return "the muster: off (the ambush offered at auction)";
        }

        var text = $"the muster: {Pressed} bandits pressed into an ambush ({Hired} of them hired hands, greedy or reckless and poor; {FromTurn} off a turn in a crowd, {PutDown} putting work down to take up again, {Thrown} throwing work under {ThrowBelow:F0}gp a minute away{ThrownKinds()}), {TooFew} calls with fewer than {Math.Max(1, LeastParty)} free in {PartyReach} tiles, {FellApart} ambushes closed with fewer than {Math.Max(1, LeastParty)} left on them, {HeldStrike} strikes held for a second bandit, {KeptOnGoal} sweeps kept the place for a carrier off its drawn road but bound the same way, {TooFarAhead} spots over {MostAhead} tiles along refused";

        if (_refused.Count == 0)
        {
            return $"{text}; no bandit within reach passed over";
        }

        List<string> parts = [];

        foreach (var (why, count) in _refused)
        {
            parts.Add($"{why} {count}");
        }

        return $"{text}; bandits within reach passed over by why: {string.Join(", ", parts)}";
    }

    private static string ThrownKinds()
    {
        if (_thrownKinds.Count == 0)
        {
            return "";
        }

        List<string> parts = [];

        foreach (var (kind, count) in _thrownKinds)
        {
            parts.Add($"{kind} {count}");
        }

        return $": {string.Join(", ", parts)}";
    }

    public static void Forget()
    {
        _party = null;
        _clock = false;
        Thrown = 0;
        Hired = 0;
        _thrownKinds.Clear();
        Pressed = 0;
        FromTurn = 0;
        PutDown = 0;
        TooFew = 0;
        FellApart = 0;
        HeldStrike = 0;
        KeptOnGoal = 0;
        TooFarAhead = 0;
        _refused.Clear();
        Sweeps = 0;
        Carriers = 0;
        Escorted = 0;
        Light = 0;
        InTown = 0;
        Short = 0;
        Outmatched = 0;
        Laid = 0;
        Joined = 0;
        Unmustered = 0;
        Struck = 0;
        Missed = 0;
        Paid = 0;
        Fled = 0;
        FoughtOn = 0;
        Killed = 0;
        Withdrawn = 0;
        BanditDeaths = 0;
        TakenGold = 0;
        TakenThings = 0;
        TakenWorth = 0;
        NoCatch = 0;
        Relaid = 0;
        LostTrail = 0;
        Unloaded = 0;
        Recruits = 0;
        _cargoCount = 0;
        _cargoNext = 0;
    }

    /// <summary>"cargo on the roads over the last 256: 12gp at the median, 60 at the 75th, 140 at the 90th".</summary>
    private static string CargoBook() =>
        _cargoCount == 0
            ? "no cargo measured on the roads yet"
            : $"cargo of the last {_cargoCount} carriers on the roads: {Quantile(0.5)}gp at the median, {Quantile(0.75)} at the 75th percentile, {Quantile(0.9)} at the 90th";
}

/// <summary>Offers a bandit called to the standing ambush its place in it. See <see cref="BotHighway"/>.</summary>
public sealed class BotHighwayman : IBotProposer
{
    public string Name => "Highwayman";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (BotHighway.Now is not { } party || bot?.Self is not BotMobile { Deleted: false, Alive: true } body || !party.Holds(body)
            || party.Took.Contains(body.Serial) || party.Ended.Contains(body.Serial) || body.Map != party.Map)
        {
            return null;
        }

        return new BotLieInWait(party);
    }
}

/// <summary>One bandit's part in an ambush: walk to the spot, hide, wait for the carrier, strike, and go through what is left.</summary>
public sealed class BotLieInWait : BotDeed
{
    public const string Trade = "ambush";

    private readonly BotHighway.Party _party;

    private BotBrawl _fight;

    private long _waitFrom;

    private long _hideTick;

    private bool _looted;

    private bool _over;

    private Mobile _body;

    private int _stamp;

    public BotLieInWait(BotHighway.Party party)
    {
        _party = party;
    }

    public override string Kind => Trade;

    public override Map Map => _party.Map;

    public override Point3D Where => _party.Spot;

    /// <summary>
    /// The expected cargo (life19): the carrier's goods at the fence's share, split among the party, over the deed's minutes. A 250gp
    /// cargo for two is some 10gp a minute — honest, and it no longer decides: the band is pressed (<see cref="BotHighway.Press"/>).
    /// </summary>
    public override double Expects =>
        Math.Max(BotHighway.Prior, _party.Cargo * BotFencer.FenceShare / Math.Max(1, _party.Members.Count) / Math.Max(1.0, Minutes));

    public override double Minutes => 6.0;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Braves => true;

    public override Mobile Foe => _fight != null ? _party.Carrier : null;

    public override SkillName? Trains => null;

    public override int Made => _body != null ? _party.Made.GetValueOrDefault(_body.Serial) : 0;

    public override string Stage =>
        _fight != null ? $"ambush: {_fight.Stage}" : $"lying in wait for {_party.Carrier?.Name} near {_party.Town}";

    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body)
        {
            _body = body;
            BotHighway.Taking(_party, body);
        }
    }

    public override void Drop(IBotWilful bot)
    {
        _fight?.Drop(bot);

        if (bot?.Self is { } body)
        {
            BotHighway.Done(_party, body);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map != _party.Map)
        {
            return End(bot, BotDoing.Failed("no body"));
        }

        if (_party.Closed)
        {
            return End(bot, BotDoing.Done("the ambush is over"));
        }

        var carrier = _party.Carrier;

        if (_fight == null)
        {
            if (carrier is not { Deleted: false, Alive: true } || carrier.Map != _party.Map)
            {
                return End(bot, BotDoing.Failed("the carrier is gone"));
            }

            if (!_party.Hit && body.InRange(carrier.Location, BotHighway.StrikeWithin) && body.InLOS(carrier) && !BotHighway.Together(_party, carrier))
            {
                // life19: not alone. The second is still on its way: hidden at the spot, the carrier let by rather than fought one to one.
                return BotDoing.Work($"lying in wait for {carrier.Name}, waiting for the rest of the band");
            }

            if (_party.Hit || body.InRange(carrier.Location, BotHighway.StrikeWithin) && body.InLOS(carrier))
            {
                BotHighway.Strike(_party, body);

                var trains = body.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

                _fight = new BotBrawl(carrier, BotBrawl.Robbery, trains, BotHighway.Over);
            }
            else if (_stamp != _party.SpotStamp || !body.InRange(_party.Spot, 3))
            {
                if (_stamp != _party.SpotStamp)
                {
                    // The carrier turned and the ambush moved after it: a new spot, a new wait.
                    _stamp = _party.SpotStamp;
                    _waitFrom = 0;
                }

                if (!body.InRange(_party.Spot, 3))
                {
                    return BotDoing.Walk(_party.Map, _party.Spot, BotArrival.Within(2), $"to lie in wait for {carrier.Name}");
                }

                return BotDoing.Work($"lying in wait for {carrier.Name}");
            }
            else
            {
                var now = Core.TickCount;

                if (_waitFrom == 0)
                {
                    _waitFrom = now;
                }

                if (now - _waitFrom >= BotHighway.WaitMs)
                {
                    BotHighway.Miss();

                    return End(bot, BotDoing.Failed($"{carrier.Name} never came by"));
                }

                if (!body.Hidden && now - _hideTick >= 10000)
                {
                    _hideTick = now;
                    BotShadow.Hide(body);
                }

                return BotDoing.Work($"lying in wait for {carrier.Name}");
            }
        }

        if (!_over)
        {
            var doing = _fight.Advance(bot);

            if (doing.Kind is not (BotDoingKind.Done or BotDoingKind.Failed))
            {
                return doing;
            }

            _over = true;

            if (!_party.CarrierDown || !ReferenceEquals(_party.Killer, body) && _party.Killer is { Deleted: false, Alive: true })
            {
                return End(bot, doing.Kind == BotDoingKind.Failed ? doing : BotDoing.Done($"ambush: {doing.Note}"));
            }
        }

        // The carrier is dead and this one goes through what it left: the leader, or whoever is left standing.
        return Loot(bot, body);
    }

    private BotDoing Loot(IBotWilful bot, BotMobile body)
    {
        if (_looted || _party.Carrier?.Remains is not { Deleted: false } corpse || corpse.Map != body.Map)
        {
            return End(bot, BotDoing.Done($"{_party.Carrier?.Name} is down"));
        }

        if (!body.InRange(corpse.GetWorldLocation(), 2))
        {
            return BotDoing.Walk(body.Map, corpse.GetWorldLocation(), BotArrival.Within(1), $"to {_party.Carrier.Name}'s body");
        }

        _looted = true;

        var before = BotCrime.Census(body);
        List<Item> lifted = [];

        for (var i = 0; i < corpse.Items.Count; i++)
        {
            if (corpse.Items[i] is { Deleted: false, Movable: true } item && (item is Gold || BotShopkeep.Merchandise(item, null)))
            {
                lifted.Add(item);
            }
        }

        for (var i = 0; i < lifted.Count; i++)
        {
            body.Backpack?.TryDropItem(body, lifted[i], false);
        }

        BotHighway.Took(_party, body, before);

        return End(bot, BotDoing.Done($"went through {_party.Carrier.Name}'s body"));
    }

    private BotDoing End(IBotWilful bot, BotDoing doing)
    {
        if (bot?.Self is { } body)
        {
            BotHighway.Done(_party, body);
        }

        return doing;
    }
}
