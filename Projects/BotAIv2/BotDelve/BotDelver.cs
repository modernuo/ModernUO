using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers the maker of a guild a dungeon its band could actually come back out of.
///
/// <para>
/// <b>Only a guild's maker, by order.</b> A delve is called by the head of a band, which on this shard is
/// the crafter every guild is founded on and the one bot in it with a mind. It is the same office that
/// charges the band with a trade and decides who is in it — see <c>BotCharter</c> and <c>BotRoster</c> —
/// and a delve is the third thing that office can do.
/// </para>
///
/// <para>
/// <b>Which dungeon is a measurement, not a preference.</b> Patrick's order names the reason in one line:
/// a demon is plainly not a harpy. Every dungeon has been measured — see <see cref="BotDungeon"/>, which
/// builds one of each creature its spawners name and reads its strength off the engine — and what this
/// does with those numbers is pick the richest one whose <i>worst</i> inhabitant the party can still
/// answer. The worst rather than the average, because a party is killed by the strongest thing it meets
/// and not by the mean of what lives there.
/// </para>
///
/// <para>
/// <b>And the strength is the party's, not the leader's.</b> Asked of the bodies that would actually be
/// called up — the fighters standing within reach of the muster, best first, as many as would go down —
/// because a leader is a crafter and a crafter's own strength says nothing about what its band can take.
/// </para>
///
/// <para>
/// <b>Every refusal has a name.</b> A delve needs a maker, one not already leading a company, one fit to
/// walk into a cave, a surveyed dungeon and a band strong enough for one. Which of those was missing is
/// the only question worth being able to answer on an evening when nobody went anywhere.
/// </para>
/// </summary>
public sealed class BotDelver : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDelver));

    /// <summary>
    /// How much stronger than the worst thing in a dungeon a party wants to be before it goes down.
    ///
    /// <para>
    /// Half again, which is <c>BotThreat.Tolerance</c> said about a place instead of about one fight, and
    /// it is deliberately the same number: the shard already has a measured figure for "how much stronger
    /// than us the opposition may be before walking on beats standing and fighting", and inventing a second
    /// one here would be two thresholds on one shelf.
    /// </para>
    /// </summary>
    public static double Odds { get; set; } = 1.5;

    /// <summary>How far around the leader the band it could raise is looked for.</summary>
    public static int Reach { get; set; } = 200;

    /// <summary>
    /// How many creatures a dungeon must keep alive per party in it, which is how many parties it will hold.
    ///
    /// <para>
    /// <b>A dungeon's spawners are the ceiling on everybody in it, and the ceiling is low.</b> The Orc Caves
    /// keep thirty-six things alive; a party wants twenty corpses. Five parties went into them together on
    /// 11.09.2026 and killed seventy things between them in twenty minutes — not one of the five reached its
    /// quota, and there was nothing wrong with any of them. They had eaten the cave.
    /// </para>
    ///
    /// <para>
    /// So a dungeon holds <c>Heads / this</c> parties and no more, which is one for the Orc Caves, two for
    /// Wrong and six for Despise. Set to a party's quota, because that is the question being asked — how
    /// many bands can this place pay — and the answer is deliberately conservative: spawners refill over
    /// twenty minutes, so a dungeon at its cap is still not being stripped.
    /// </para>
    ///
    /// <para>
    /// Since build 359 also the most a party calls (<c>BotDelve.PartyFor</c>): a party bigger than the creatures its dungeon
    /// keeps alive for it is a party with nothing left to fight.
    /// </para>
    /// </summary>
    // 12 from 20 on 25.09.2026, with Patrick's order to open delving to everybody and BotDungeon.RestockMs: the Orc
    // Caves' thirty-six hold three parties at once instead of one, because they now fill again in minutes.
    public static int HeadsPerParty { get; set; } = 12;

    /// <summary>
    /// How long a leader leaves delving alone after one, whether it went well or not.
    ///
    /// Half an hour. A delve is twenty minutes of five bots doing nothing else; a band that goes down again
    /// the moment it surfaces is a band that has stopped being an economy.
    /// </summary>
    // Ten minutes from thirty on 25.09.2026, Patrick's order: "let everybody go more often". The fighters spent half
    // to three quarters of their time prowling, 77% of it finding "nothing here", while delves paid 1100 to 2000 a
    // time and went 45 times a day.
    public static int BetweenMs { get; set; } = 600000;

    public string Name => "Delver";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    /// <summary>Asked of somebody who leads no band. Not a refusal — nearly every answer is this.</summary>
    public static long NotALeader { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    /// <summary>Leaders turned away for want of their supplies. See <see cref="BotProvision"/>.</summary>
    public static long Unsupplied { get; private set; }

    public static long Resting { get; private set; }

    /// <summary>Asks that came too soon after this band's last delve.</summary>
    public static long TooSoon { get; private set; }

    /// <summary>Asks where the dungeons had not been surveyed, or held no room anybody could stand in.</summary>
    public static long Unsurveyed { get; private set; }

    /// <summary>Asks where no dungeon on the map was one this band could come back out of.</summary>
    public static long Outmatched { get; private set; }

    /// <summary>
    /// Asks where every dungeon this band could survive already had as many parties in it as it will feed.
    ///
    /// <b>Not a refusal to be fixed, on a shard with one dungeon in reach of five guilds.</b> It is the
    /// island saying its easiest cave is full, and the answer to it is bands that grow strong enough for
    /// the next rung up — see <see cref="HeadsPerParty"/> for what "full" means and why waiting beats
    /// crowding.
    /// </summary>
    public static long Crowded { get; private set; }

    /// <summary>Dungeons passed over because no gate joins them to where the band stands. See <see cref="BotGates"/> (29.09.2026).</summary>
    public static long Mouthless { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>When each band last went down.</summary>
    private static readonly Dictionary<string, long> _last = [];

    /// <summary>
    /// When each dungeon was last spoken for by a leader that had just been offered it.
    ///
    /// <para>
    /// <b>A party that is still calling has claimed its dungeon as surely as one already in it.</b>
    /// <c>BotDelveParty.Inside</c> only knows about parties that have gone down, and every maker on the
    /// island is asked in the same beat — so on the first try at spreading them out, all five were offered
    /// the Orc Caves inside one second, because at the moment each was asked nobody was in it yet. Four of
    /// them then spent two minutes failing to raise a party from bodies the fifth had already taken.
    /// </para>
    ///
    /// <para>
    /// A claim with a clock on it rather than one that has to be given back. It lasts as long as the call
    /// it stands for, so a delve that is outbid before it ever musters lets go of its dungeon by simply
    /// ageing — no teardown to get wrong, and nothing to leak if an errand ends in a way nobody thought of.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, long> _claims = [];

    private static bool _said;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Counted before anything else, so that "nobody here belongs to a band" and "members are never offered a
        // delve" stay different numbers instead of the same silence.
        // <b>Any member of a guild, from 25.09.2026.</b> Until then only the head (and before 21.09.2026 the maker) could
        // lead one, so a guild went down at most twice an hour and most bands never did. Patrick: "let everybody go
        // more often". One band a guild per BetweenMs still holds, and the dungeons' capacity (HeadsPerParty) still
        // decides how many go down together. See BotGuilds.Head for who led before.
        if (body is not BotMobile who || who.Guild is not Guild guild)
        {
            NotALeader++;

            return null;
        }

        Asked++;

        if (BotDelve.Resting(body))
        {
            Resting++;

            return null;
        }

        if (_last.TryGetValue(guild.Name, out var when) && Core.TickCount - (when + BetweenMs) < 0)
        {
            TooSoon++;

            return null;
        }

        if (!BotSquads.Running)
        {
            return null;
        }

        if (bot is not IBotSquadMember { Squad: null })
        {
            Held++;

            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        // Patrick's order of 26.09.2026: nobody goes down without its supplies, the leader first. See BotProvision.
        if (!BotProvision.Fit(body, out _))
        {
            Unsupplied++;

            return null;
        }

        // Done once, on the first ask, rather than at boot: the spawners have to be in the world, and this
        // is the first moment anything wants to know about them.
        BotDungeon.Survey(map);

        Gather(map, body);

        var deep = Deepest();

        if (deep == null)
        {
            if (BotDungeon.Rooms == 0)
            {
                Unsurveyed++;
            }
            else if (Deepest(alone: false, full: true) != null)
            {
                // Strong enough for somewhere, and somewhere is full. A different fact from "nowhere on
                // this map is survivable", and the two read identically until they are counted apart.
                Crowded++;
            }
            else
            {
                Outmatched++;

                // <b>How near (30.09.2026, build 341).</b> No delve has been called since build 330 weighed a weapon by its
                // wielder's skill, and "found nothing their band could come back out of" says only that the answer was no:
                // 2736 of 6707 asks on 339. The nearest any band came to what the easiest dungeon asks is kept, with whose
                // band it was, so the rule can be read against the bands the shard actually has.
                var easiest = Easiest();

                if (easiest is { Worst: > 0 })
                {
                    var have = Strength(easiest);
                    var need = easiest.Worst * Odds;

                    if (have / need > _nearest)
                    {
                        _nearest = have / need;
                        _nearestSaid = $"{(body as BotMobile)?.Guild?.Name ?? body.Name} with {_band.Count} in reach, {have:F0} of the {need:F0} {easiest.Name} asks";
                    }
                }

                // The formula would have sent this band somewhere the proving ground says it cannot come back from.
                if (BotProving.Judges && Deepest(alone: false, full: true, formula: true) != null)
                {
                    HeldBack++;
                }
            }

            return null;
        }

        // <b>Not a call that cannot fill (30.09.2026, build 361).</b> The party goes down only with more than the dungeon's worst
        // room holds (BotDelve.Least, 359), and on 360 every one of fourteen calls ended short of it — "only 6 of the 12 stood
        // up for the Orc Caves in 3 minutes" — each a leader's three to ten minutes spent calling bots that were not there to
        // come. Counted to the call's own reach (BotDelve.Reach), with the call's own tests (see Eligible), before it is offered.
        var least = BotDelve.Least(map, deep);

        // <b>Counted round the muster, where the call levies, not round the leader (01.10.2026).</b> See BotDelve.Plan: the
        // muster is the seat set forward on the road to the mouth, often hundreds of tiles from where the leader was asked.
        var delve = new BotDelve(map, deep);
        var muster = delve.Plan(body);

        var round = muster == Point3D.Zero ? body.Location : muster;

        // <b>Counted with the floor (12.10.2026, build 434).</b> The call fills its weak seats (BotDelve.WeakShare) and no more, and
        // the descent lets go whoever is beyond the seats of the party that stood up; a call that could raise bodies enough but
        // not enough holding the floor is a call that cannot fill, and is not offered — counted apart from Unraisable.
        var bodies = Raisable(map, body, round, BotDelve.Reach, deep, out var lines, out var all);

        if (bodies < least)
        {
            if (all >= least)
            {
                ShortOfFloor++;
            }
            else
            {
                Unraisable++;
            }

            return null;
        }

        // <b>And somebody to hold the line (01.10.2026).</b> 18 calls on 30.09 ended "nobody who could hold a line answered the
        // call for the Orc Caves" after the leader's walk and three minutes at the muster; the call looks for one within its reach
        // and then out to BotDelve.LineReach, and so does this, before the offer.
        if ((body as BotMobile)?.Class?.Role != BotRole.Melee && lines < BotDelve.Fighters
            && Lines(map, body, round, Math.Max(BotDelve.Reach, BotDelve.LineReach)) < BotDelve.Fighters)
        {
            Lineless++;

            return null;
        }

        // The guild's clock and the dungeon's claim start when a member takes it (Stamp), not here. See Stamp.
        // A call already gathering near here takes this bot instead of a second call beside it (build 376). See BotDelve.CallingNear.
        if (BotDelve.CallingNear(body, BotDelve.Reach))
        {
            Answering++;

            return null;
        }

        BotDelve.Offering(body);

        Offered++;

        var strength = Strength(deep);

        Compare(body, deep, strength);
        Once(body, deep, strength);

        return delve;
    }

    /// <summary>
    /// The bodies the party this leader could raise would be made of: the fighters within <see cref="Reach"/> who are fit,
    /// in no company and not already down a dungeon. Its own body is not among them — whoever leads is there to be
    /// protected, and a crafter's strength says nothing about what its band can take.
    /// </summary>
    /// <summary>Calls not offered because fewer than the party's floor could be raised within the call's reach. See Propose.</summary>
    public static long Unraisable { get; private set; }

    /// <summary>Delves not offered because another call was already gathering within its reach. See <c>BotDelve.CallingNear</c>.</summary>
    public static long Answering { get; private set; }

    /// <summary>Offers not made because, though bodies enough stood within the call's reach, too few of them hold the dungeon's floor to fill the party's seats. See <c>BotDelve.WeakShare</c>.</summary>
    public static long ShortOfFloor { get; private set; }

    /// <summary>
    /// How many the party could go down with, the leader included, within <paramref name="reach"/> of <paramref name="round"/> (its
    /// muster), by the tests <see cref="Gather"/> uses: every body that holds the dungeon's floor, and of those under it as many
    /// as the smallest party that may go down keeps seats for (<c>BotDelve.WeakSeats</c> of <c>BotDelve.Least</c>). That is the
    /// effective rule stated as a go/no-go: at least <c>Least − ⌊Least × WeakShare⌋</c> bodies over the floor must stand in reach
    /// (5 of the Orc Caves' 7 at a third, 6 at a quarter) — the same question the descent asks of the party that stood up, since a
    /// larger party keeps more seats and never fewer. <paramref name="all"/> is the count without the floor, the old answer.
    /// </summary>
    private static int Raisable(Map map, Mobile leader, Point3D round, int reach, BotDungeon.Deep deep, out int lines, out int all)
    {
        var strong = 0;
        var weak = 0;
        lines = 0;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(round, reach))
        {
            if (mobile != leader && mobile is BotMobile other && Eligible(other) && !BotRegard.Hostile(leader, other))
            {
                if (BotDelve.Weak(other, deep))
                {
                    weak++;
                }
                else
                {
                    strong++;
                }

                if (other.Class?.Role == BotRole.Melee)
                {
                    lines++;
                }
            }
        }

        if (BotDelve.Weak(leader, deep))
        {
            weak++;
        }
        else
        {
            strong++;
        }

        all = strong + weak;

        return strong + Math.Min(weak, BotDelve.WeakSeats(BotDelve.Least(map, deep)));
    }

    /// <summary>How many who could hold a line the call could raise within <paramref name="reach"/> of its muster. See <see cref="Propose"/>.</summary>
    private static int Lines(Map map, Mobile leader, Point3D round, int reach)
    {
        var lines = 0;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(round, reach))
        {
            if (mobile != leader && mobile is BotMobile { Class.Role: BotRole.Melee } other && Eligible(other)
                && !BotRegard.Hostile(leader, other))
            {
                lines++;
            }
        }

        return lines;
    }

    /// <summary>Delves not offered because nobody who could hold a line could be raised for the call. See <see cref="Propose"/>.</summary>
    public static long Lineless { get; private set; }

    /// <summary>Whether this bot is one a call could raise: able to fight, no producer, in no company, not down a dungeon, not
    /// holding work that answers to another, with its supplies.</summary>
    private static bool Eligible(BotMobile other) =>
        other is IBotAlly { AbleToFight: true } && other.Class is { } klass && klass.Role != BotRole.Producer && other.Squad == null
        && !BotDelveParty.Delving(other) && other.Resolve?.Deed is not { Alongside: true } && !BotProvision.Short(other);

    private static void Gather(Map map, Mobile leader)
    {
        _band.Clear();
        _leader = leader as BotMobile;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(leader.Location, Reach))
        {
            if (mobile == leader || mobile is not BotMobile other)
            {
                continue;
            }

            if (other is not IBotAlly { AbleToFight: true } || other.Class is not { } klass)
            {
                continue;
            }

            if (klass.Role == BotRole.Producer || other.Squad != null || BotDelveParty.Delving(other))
            {
                continue;
            }

            // Nor one that already answers to a company or a class — the call itself passes such a bot over (BotDelve.Levy).
            if (other.Resolve?.Deed is { Alongside: true })
            {
                continue;
            }

            // Nor one without its supplies, which the call turns away too (BotDelve.Levy, BotProvision).
            if (BotProvision.Short(other))
            {
                continue;
            }

            // Nor one of a guild hostile to the leader's, which the call passes over too (build 370, BotRegard.Hostile).
            if (BotRegard.Hostile(leader, other))
            {
                continue;
            }

            _band.Add(other);
        }

        // <b>In the order the call takes them, not strongest first.</b> The call puts guildmates at the front and the rest by
        // distance (BotDelve.Levy); summing the strongest four instead counted a Baron who beat the juka mage five times over
        // into every band within two hundred tiles of Britain — eight offers moved to Wrong in the first second of build 235,
        // and the party actually raised would have been the leader's guildmates.
        _band.Sort(
            (a, b) =>
            {
                var mine = (BotGuilds.Same(leader, b) ? 1 : 0).CompareTo(BotGuilds.Same(leader, a) ? 1 : 0);

                return mine != 0 ? mine : leader.GetDistanceToSqrt(a).CompareTo(leader.GetDistanceToSqrt(b));
            }
        );
    }

    private static readonly List<BotMobile> _band = [];

    /// <summary>The one asked to lead, reckoned beside the band. See <see cref="WeakestShare"/>.</summary>
    private static BotMobile _leader;

    /// <summary>The guild of a party being reckoned again before it goes down. See <see cref="Recheck"/>.</summary>
    private static string _partyGuild;

    /// <summary>
    /// The least a single member of a party — the leader included — may be worth against a dungeon's worst, as a share of
    /// it, for the party to go there.
    ///
    /// <para>
    /// <b>A fifth, from the first three delves into Wrong on 26.09.2026, all of which ended "whoever was leading it fell".</b>
    /// The band's sum cleared the bar every time, and the leaders were Delwyn the archer, bare of armour, who had lost to
    /// the Orc Caves' orcish mage at R 0.01, and Calla the crafter, never measured, in no armour at all: a delve ends when
    /// its leader falls, and the leader was never counted. A party is as far as its weakest member can go, not as far as
    /// its strongest four added up.
    /// </para>
    /// </summary>
    public static double WeakestShare { get; set; } = 0.2;

    /// <summary>Dungeons a band's sum would have reached and its weakest member could not. For the summary.</summary>
    public static long Weakest { get; private set; }

    /// <summary>
    /// Whether this body is worth at least the weakest share of this dungeon's worst. Not asked of the easiest dungeon there
    /// is: that is where a band learns, and the old formula sent everybody there.
    /// </summary>
    private static bool Holds(BotMobile member, BotDungeon.Deep deep) =>
        member == null || deep == Easiest() || BotProving.Against(member, deep) >= deep.Worst * WeakestShare;

    /// <summary>The nearest a refused band came to what the easiest dungeon asks, and whose it was. See the Outmatched count.</summary>
    private static double _nearest;

    private static string _nearestSaid = "no band refused yet";

    /// <summary>Dungeons a band was kept out of because its guild's company had not cleared their worst room lately.</summary>
    public static long Unproven { get; private set; }

    /// <summary>
    /// Whether a band of this guild may go down this dungeon by the company rule: the easiest always, anything deeper only where
    /// the guild's company has cleared the worst room on the proving ground lately (<c>BotProving.RoomCleared</c>). A leader in
    /// no guild has no company, and so the easiest only.
    /// </summary>
    private static bool Proven(string guild, BotDungeon.Deep deep)
    {
        if (!BotProving.Judges || !BotProving.RoomGate || deep == Easiest() || BotProving.RoomCleared(guild, deep))
        {
            return true;
        }

        Unproven++;

        return false;
    }

    /// <summary>The surveyed dungeon with the weakest worst inhabitant.</summary>
    private static BotDungeon.Deep Easiest()
    {
        BotDungeon.Deep easiest = null;
        var all = BotDungeon.All;

        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Ready && (easiest == null || all[i].Worst < easiest.Worst))
            {
                easiest = all[i];
            }
        }

        return easiest;
    }

    /// <summary>Which of the gathered the call's first pass took, so the second does not count them twice.</summary>
    private static readonly HashSet<int> _picked = [];

    /// <summary>
    /// What the gathered band is worth against this dungeon: its strongest bodies, as many as would go down, added up —
    /// because the company fights as one and the arithmetic the rest of the shard uses for "our side of a fight" is a sum.
    ///
    /// <para>
    /// <b>Each body counted at what it proved against this dungeon's worst creature, from 26.09.2026.</b> Until then every
    /// body was <c>BotThreat.Power</c> — health times the weapon's average hit, plus half the Magery — the same number for a
    /// novice in cloth and a veteran in plate with the same sword, and the bands, however they trained and dressed, never
    /// once read as strong enough for anything past the Orc Caves. See <see cref="BotProving.Against"/> for how a bot never
    /// measured on this creature is counted; <paramref name="formula"/> asks the old question, for the comparison.
    /// </para>
    /// </summary>
    private static double Strength(BotDungeon.Deep deep, bool formula = false)
    {
        // <b>The easiest dungeon is judged as it always was.</b> It is where a band learns, and the delves into it all evening
        // came back with twenty down and a thousand gold; with the creatures on the proving ground awake (build 249) a novice
        // loses to the orcish mage one against one, and the proven sum would have closed the Orc Caves to nearly everybody.
        // The proving ground decides what lies beyond it.
        if (deep == Easiest())
        {
            formula = true;
        }

        // The formula's question is the old one, asked as it always was: the strongest bodies in reach.
        if (formula)
        {
            _might.Clear();

            for (var i = 0; i < _band.Count; i++)
            {
                _might.Add(BotThreat.Power(_band[i]));
            }

            _might.Sort(static (a, b) => b.CompareTo(a));

            var sum = 0.0;

            // As many as the party for this dungeon calls, the leader apart (build 359): see BotDelve.PartyFor.
            var top = Math.Min(_might.Count, Math.Max(1, Want(deep) - 1));

            for (var i = 0; i < top; i++)
            {
                sum += _might[i];
            }

            return sum;
        }

        // The proven question is asked of the party the call would actually raise: the first line-holder in the call's order,
        // then the rest in that order, as many as would go down beside the leader (BotDelve.Levy, Take) — and of the leader
        // itself, who is counted and must hold its own (see WeakestShare).
        var want = Math.Max(1, Want(deep) - 1);
        var total = 0.0;
        var taken = 0;

        if (!Proven((_leader?.Guild as Guild)?.Name, deep))
        {
            return 0.0;
        }

        if (_leader != null)
        {
            if (!Holds(_leader, deep))
            {
                Weakest++;

                return 0.0;
            }

            total += BotProving.Against(_leader, deep);
        }

        _picked.Clear();

        for (var i = 0; i < _band.Count && taken < Math.Min(BotDelve.Fighters, want); i++)
        {
            if (_band[i].Class?.Role == BotRole.Melee)
            {
                if (!Holds(_band[i], deep))
                {
                    Weakest++;

                    return 0.0;
                }

                total += BotProving.Against(_band[i], deep);
                taken++;
                _picked.Add(i);
            }
        }

        for (var i = 0; i < _band.Count && taken < want; i++)
        {
            if (_picked.Contains(i))
            {
                continue;
            }

            if (!Holds(_band[i], deep))
            {
                Weakest++;

                return 0.0;
            }

            total += BotProving.Against(_band[i], deep);
            taken++;
        }

        return total;
    }

    private static readonly List<double> _might = [];

    /// <summary>How many the party for this dungeon calls, the leader included. See <c>BotDelve.PartyFor</c>.</summary>
    private static int Want(BotDungeon.Deep deep) => BotDelve.PartyFor(_leader?.Map, deep);

    /// <summary>
    /// Whether this dungeon is judged by its worst room on the proving ground rather than by <see cref="Odds"/>: every one past
    /// the easiest, while the proving ground judges and the room gate stands. See <see cref="Cored"/>.
    /// </summary>
    private static bool Ringed(BotDungeon.Deep deep) =>
        BotProving.Judges && BotProving.RoomGate && deep != Easiest();

    /// <summary>
    /// Whether the band gathered could go down this dungeon: its strength against <see cref="Odds"/> times the worst creature, or
    /// past the easiest the proving ground's room (<see cref="Cored"/>).
    /// </summary>
    private static bool Enough(BotDungeon.Deep deep, bool formula) =>
        !formula && Ringed(deep) ? Cored(deep) : Strength(deep, formula) >= deep.Worst * Odds;

    /// <summary>Dungeons whose worst room the guild's company had cleared, closed because fewer of that company stood in reach.</summary>
    public static long Uncompanied { get; private set; }

    /// <summary>
    /// <b>Past the easiest dungeon the proving ground's room is the bar, not the worst creature times Odds (30.09.2026, build
    /// 359).</b>
    ///
    /// <para>
    /// The sum of what each body proved against the worst creature alone sent four bands into Wrong on 26.09 and all four came
    /// back "whoever was leading it fell" — which is why the room gate was raised. With the gate standing, the sum was still
    /// asked on top of it: The Needle's eight cleared Wrong's worst room at 11:37:14 on 356 ("cleared lately: The Needle Wrong"
    /// in every <c>Proving:</c> line of 357), and no band of The Needle was offered Wrong once: "0 offers went deeper than the old
    /// formula would have sent them" in every <c>Delving:</c> line of 356 and 357. The room is the measurement of a company
    /// against a room; the sum is a guess at it from single fights, and the guess is the one that was wrong.
    /// </para>
    ///
    /// <para>
    /// <b>So the proof is asked of the party it was made by.</b> The ring fields a guild's own fighters, two for each creature
    /// of the room, as many as it has (<c>BotProving.NextRoom</c>); a band that goes down on that proof brings as many of the
    /// guild's own fighters — the leader counted if it fights — as the ring would field for it now
    /// (<see cref="RingCompany"/>), every one of them, the leader first, still worth a fifth of the worst (<see cref="WeakestShare"/>).
    /// Strangers may make up the rest of the party; they are not the proof.
    /// </para>
    /// </summary>
    private static bool Cored(BotDungeon.Deep deep)
    {
        var guild = _leader?.Guild as Guild;

        if (!Proven(guild?.Name, deep))
        {
            return false;
        }

        if (_leader != null && !Holds(_leader, deep))
        {
            Weakest++;

            return false;
        }

        var need = RingCompany(_leader?.Map, guild, deep);
        var mates = _leader is { Class.Role: not BotRole.Producer } ? 1 : 0;

        for (var i = 0; i < _band.Count && mates < need; i++)
        {
            if (!BotGuilds.Same(_leader, _band[i]))
            {
                continue;
            }

            if (!Holds(_band[i], deep))
            {
                Weakest++;

                return false;
            }

            mates++;
        }

        if (mates < need)
        {
            Uncompanied++;

            return false;
        }

        return true;
    }

    /// <summary>
    /// How many of its own fighters a guild's band brings to go down a dungeon on the proving ground's proof: as many as the ring
    /// fields for it against that dungeon's worst room — the party's size (<c>BotDelve.PartyFor</c>), or every fighter the guild
    /// has if it has fewer.
    /// </summary>
    public static int RingCompany(Map map, Guild guild, BotDungeon.Deep deep) =>
        guild == null ? BotDelve.PartyFor(map, deep) : Math.Min(BotDelve.PartyFor(map, deep), BotProving.Fighters(guild).Count);

    /// <summary>
    /// The richest dungeon whose worst inhabitant this band could still answer, and preferably an empty one.
    ///
    /// <para>
    /// <b>Emptiness comes before richness, and that order is the whole of this.</b> The measurement is the
    /// same for every band on the island, so without it every guild of about the same strength picks the
    /// same dungeon and they all arrive together: five parties into the Orc Caves at 22:03 on 11.09.2026,
    /// twenty-five bots in a cave of ten rooms, seventy kills between them in twenty minutes where one
    /// party on its own had taken twenty in eight. A dungeon's spawners are its ceiling, and six of the
    /// other eleven were sitting empty with nobody in them at all.
    /// </para>
    ///
    /// <para>
    /// So the choice is made twice: among the dungeons nobody is in, and if there are none, among all of
    /// them. Never a veto — a band that can only survive one dungeon and finds a party in it still goes,
    /// because "only with an empty one" is a rule that reads as "not at all" on a small island, and this
    /// project has paid for that shape twice.
    /// </para>
    /// </summary>
    private static BotDungeon.Deep Deepest()
    {
        var empty = Deepest(alone: true);

        return empty ?? Deepest(alone: false);
    }

    private static BotDungeon.Deep Deepest(bool alone, bool full = false, bool formula = false)
    {
        BotDungeon.Deep best = null;

        var all = BotDungeon.All;

        for (var i = 0; i < all.Count; i++)
        {
            var deep = all[i];

            if (!deep.Ready || !Enough(deep, formula))
            {
                continue;
            }

            // A dungeon the band cannot walk into is not offered: the descent by hand is kept only for a world with no
            // gates read at all. See BotGates (Patrick's order of 29.09.2026).
            if (BotGates.Ready && _leader?.Map is { } map && !BotGates.Reaches(map, _leader.Location, deep))
            {
                Mouthless++;

                continue;
            }

            if (!full)
            {
                var parties = BotDelveParty.Inside(deep) + (Spoken(deep) ? 1 : 0);

                if (parties >= (alone ? 1 : Holds(deep)))
                {
                    continue;
                }
            }

            if (best == null || deep.Power > best.Power)
            {
                best = deep;
            }
        }

        return best;
    }

    /// <summary>Delves a member took, as against offered. See <see cref="Stamp"/>.</summary>
    public static long Taken { get; private set; }

    /// <summary>
    /// A member took a delve: its guild's clock starts and the dungeon is spoken for. Called from <c>BotDelve.Taken</c>.
    ///
    /// <para>
    /// <b>At the taking, not at the offer, from build 229.</b> Both were written when the offer was made, which was
    /// harmless while only the head was asked — the head took it. Once any member could be asked (build 228), the first
    /// member asked was usually busy at a trade that paid better, turned it down, and the guild's ten minutes ran from
    /// an offer nobody took: 24 offers and 2 delves in the first 27 minutes, 7075 asks refused "too soon". The
    /// population's auctions run one bot at a time, so a delve taken here is seen by the very next member asked.
    /// </para>
    /// </summary>
    public static void Stamp(Mobile body, BotDungeon.Deep deep)
    {
        if (body is BotMobile { Guild: Guild guild })
        {
            _last[guild.Name] = Core.TickCount;
        }

        if (deep != null)
        {
            _claims[deep.Name] = Core.TickCount;
        }

        Taken++;
    }

    /// <summary>How many parties this dungeon will feed at once. See <see cref="HeadsPerParty"/>.</summary>
    private static int Holds(BotDungeon.Deep deep) =>
        Math.Max(1, deep.Heads / Math.Max(1, HeadsPerParty));

    /// <summary>Whether somebody is still calling a party for this dungeon. See <see cref="_claims"/>.</summary>
    private static bool Spoken(BotDungeon.Deep deep) =>
        _claims.TryGetValue(deep.Name, out var when) && Core.TickCount - (when + BotDelve.MusterMs) < 0;

    /// <summary>Parties sent somewhere other than where they were called for, once the call had raised them. See <see cref="Recheck"/>.</summary>
    public static long Rechosen { get; private set; }

    /// <summary>Everybody raised and living, the leader too: a delve ends when its leader falls. See WeakestShare.</summary>
    private static void Gather(BotSquad squad, Mobile leader)
    {
        _band.Clear();
        _leader = null;

        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is BotMobile { Deleted: false, Alive: true } member)
            {
                _band.Add(member);
            }
        }

        if (leader is BotMobile { Deleted: false, Alive: true } head && !_band.Contains(head))
        {
            _band.Add(head);
        }

        _partyGuild = (leader?.Guild as Guild)?.Name;
        _raisedGuild = leader?.Guild as Guild;
        _raisedMap = leader?.Map;
    }

    /// <summary>
    /// What the living members of a party are worth against the dungeon it is bound for, reckoned as <see cref="Recheck"/>
    /// reckons it and without choosing anywhere else. For a party on its way: it counts itself again whenever it loses a
    /// member and at the mouth before anybody steps through (<c>BotDelve.Short</c>, 30.09.2026, build 338). Full health, as
    /// every strength here is: a member hurt in a fight on the road is still a member.
    /// </summary>
    public static double Worth(BotSquad squad, Mobile leader, BotDungeon.Deep deep)
    {
        if (squad == null || deep == null)
        {
            return 0.0;
        }

        Gather(squad, leader);

        return Party(deep);
    }

    /// <summary>
    /// The party actually raised, reckoned again against the dungeon it was called for, just before it goes down; the dungeon
    /// it should go to instead when it is not strong enough, and the strength it was found to have.
    ///
    /// <para>
    /// <b>The offer is reckoned on who would probably answer, and the call raises whoever does.</b> With every body worth about
    /// the same under the old formula the two were the same party; with proven strength one body can be worth four, and a band
    /// offered Wrong because a Baron stood in reach might be raised without him (build 236, 26.09.2026). So the choice is made
    /// again on the members themselves: where they are strong enough, the richest such dungeon; where they are strong enough
    /// for nothing, the weakest there is, which is where the old formula sent everybody.
    /// </para>
    /// </summary>
    public static BotDungeon.Deep Recheck(BotSquad squad, Mobile leader, BotDungeon.Deep planned, out double strength, out string shortfall)
    {
        strength = 0.0;
        shortfall = null;

        if (squad == null || planned == null || !BotProving.Judges)
        {
            return planned;
        }

        Gather(squad, leader);

        if (Fits(planned, out strength, out shortfall))
        {
            return planned;
        }

        BotDungeon.Deep best = null;
        BotDungeon.Deep weakest = null;
        var all = BotDungeon.All;

        for (var i = 0; i < all.Count; i++)
        {
            var deep = all[i];

            if (!deep.Ready)
            {
                continue;
            }

            if (weakest == null || deep.Worst < weakest.Worst)
            {
                weakest = deep;
            }

            if (Fits(deep, out _, out _) && (best == null || deep.Power > best.Power))
            {
                best = deep;
            }
        }

        var chosen = best ?? weakest ?? planned;

        if (chosen != planned)
        {
            Rechosen++;
        }

        return chosen;
    }

    /// <summary>
    /// Whether the party raised may go down this dungeon, what it is worth there, and why not when it may not: its strength
    /// against <see cref="Odds"/> times the worst, or past the easiest the proving ground's room — cleared lately by the guild's
    /// company, every member worth a fifth of the worst, and as many of the guild's own fighters standing in it as the ring
    /// fields (<see cref="Cored"/>, build 359).
    /// </summary>
    private static bool Fits(BotDungeon.Deep deep, out double strength, out string shortfall)
    {
        strength = Party(deep);
        shortfall = null;

        if (!Ringed(deep))
        {
            if (strength >= deep.Worst * Odds)
            {
                return true;
            }

            shortfall = $"{deep.Worst * Odds:F0} wanted";

            return false;
        }

        // Nought when the guild's company has not cleared the room lately or somebody in the party is under a fifth. See Party.
        if (strength <= 0.0)
        {
            shortfall = "its guild's company has not cleared the worst room lately, or one of it is worth under a fifth of the worst";

            return false;
        }

        var need = RingCompany(_raisedMap, _raisedGuild, deep);
        var mates = 0;

        for (var i = 0; i < _band.Count; i++)
        {
            if (_band[i].Guild is Guild guild && guild.Name == _partyGuild && _band[i].Class is { Role: not BotRole.Producer })
            {
                mates++;
            }
        }

        if (mates >= need)
        {
            return true;
        }

        shortfall = $"{mates} of its guild's own fighters stood up of the {need} its company cleared the worst room with";

        return false;
    }

    /// <summary>The guild and map of a party being reckoned again before it goes down. See <see cref="Fits"/>.</summary>
    private static Guild _raisedGuild;

    private static Map _raisedMap;

    /// <summary>
    /// The whole of a party that has been raised, against one dungeon: every member and the leader, as proven — and nothing at
    /// all if any one of them cannot hold its own there (<see cref="WeakestShare"/>).
    /// </summary>
    private static double Party(BotDungeon.Deep deep)
    {
        var total = 0.0;

        // The easiest by the old rule, as the offer judged it. See Strength.
        if (deep == Easiest())
        {
            for (var i = 0; i < _band.Count; i++)
            {
                total += BotThreat.Power(_band[i]);
            }

            return total;
        }

        if (!Proven(_partyGuild, deep))
        {
            return 0.0;
        }

        for (var i = 0; i < _band.Count; i++)
        {
            if (!Holds(_band[i], deep))
            {
                return 0.0;
            }

            total += BotProving.Against(_band[i], deep);
        }

        return total;
    }

    /// <summary>Offers that went deeper than the old formula would have sent the same band. See <see cref="Compare"/>.</summary>
    public static long Lifted { get; private set; }

    /// <summary>Offers that went less deep than the old formula would have.</summary>
    public static long Lowered { get; private set; }

    /// <summary>Bands the formula would have sent down and the proving ground kept on the island.</summary>
    public static long HeldBack { get; private set; }

    private static int _compared;

    /// <summary>How many of the differences are said in the log, each session.</summary>
    public static int SayDifferences { get; set; } = 8;

    /// <summary>
    /// Whether the proving ground changed where this band goes, against the formula it replaced, counted and — the first
    /// few times — said. The one number that answers "did measuring the bots move anybody past the Orc Caves".
    /// </summary>
    private static void Compare(Mobile body, BotDungeon.Deep deep, double strength)
    {
        if (!BotProving.Judges)
        {
            return;
        }

        var proven = Deepest(alone: false, full: true);
        var formula = Deepest(alone: false, full: true, formula: true);
        var provenWorst = proven?.Worst ?? 0.0;
        var formulaWorst = formula?.Worst ?? 0.0;

        if (provenWorst > formulaWorst)
        {
            Lifted++;
        }
        else if (provenWorst < formulaWorst)
        {
            Lowered++;
        }
        else
        {
            return;
        }

        if (_compared++ >= SayDifferences)
        {
            return;
        }

        logger.Information(
            "Proving moved a delve offer: {Name}'s band of {Band} is {Proven} of strength against {Deep}'s worst by the proving ground and {Formula} by the old formula, so it can reach {Reach} where the formula said {Said}",
            body.Name,
            _band.Count,
            strength.ToString("F0"),
            deep.Name,
            Strength(deep, formula: true).ToString("F0"),
            proven?.Name ?? "nowhere",
            formula?.Name ?? "nowhere"
        );
    }

    /// <summary>Said the first time only. An offer is not a delve; what actually went down says so itself.</summary>
    private static void Once(Mobile body, BotDungeon.Deep deep, double strength)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} has been offered the first delve on this shard: {Deep}, worth {Power} a head and {Worst} at worst, against {Strength} of band",
            body.Name,
            deep.Name,
            deep.Power.ToString("F0"),
            deep.Worst.ToString("F0"),
            strength.ToString("F0")
        );
    }

    public static string Describe() =>
        Asked == 0
            ? $"no guild member has ever been offered a delve ({NotALeader} answers went to bots in no guild)"
            : $"{Asked} times a guild member was asked: {Offered} were offered a dungeon and {Taken} took one, {Held} were already leading a company, "
            + $"{Unfit} were too hurt, {Unsupplied} lacked their supplies, {Resting} came too soon after a call that failed, {TooSoon} too soon after their band's last delve, "
            + $"{Unsurveyed} found nothing surveyed, {Crowded} found every dungeon they could survive already full, {Unraisable} could not raise the party's floor within the call's reach of its muster, {ShortOfFloor} could raise bodies enough there but too few holding the dungeon's floor, {Lineless} could raise nobody there to hold a line, {Answering} left to a call already gathering near them, "
            + $"{Outmatched} found nothing their band could come back out of at ×{Odds:F2} the worst of it (the nearest came to {_nearest:P0} of it: {_nearestSaid}); "
            + $"judged by the proving ground: {Lifted} offers went deeper than the old formula would have sent them, {Lowered} less deep, "
            + $"{HeldBack} bands kept on the island that the formula would have sent down, {Rechosen} parties sent elsewhere once raised, "
            + $"{Weakest} dungeons closed to a band by its weakest member (under {WeakestShare:P0} of the worst), {Unproven} by its guild's company not having cleared the worst room lately, "
            + $"{Uncompanied} by fewer of that company in reach than cleared it (past the easiest the cleared room is the bar, not ×{Odds:F2} the worst); "
            + $"{BotDungeon.Describe()}; {BotDelve.Describe()}; {BotProvision.Describe()}";

    public static void Forget()
    {
        _said = false;
        Asked = 0;
        NotALeader = 0;
        Held = 0;
        Unfit = 0;
        Unsupplied = 0;
        Resting = 0;
        TooSoon = 0;
        Unsurveyed = 0;
        Outmatched = 0;
        Crowded = 0;
        Offered = 0;
        Taken = 0;
        Lifted = 0;
        Lowered = 0;
        HeldBack = 0;
        Rechosen = 0;
        Weakest = 0;
        Unproven = 0;
        Uncompanied = 0;
        Unraisable = 0;
        ShortOfFloor = 0;
        Lineless = 0;
        Answering = 0;
        _compared = 0;
        _band.Clear();
        _last.Clear();
        _claims.Clear();
        _might.Clear();

        BotDelve.Forget();
        BotDungeon.Forget();
        BotHalls.Forget();
    }
}
