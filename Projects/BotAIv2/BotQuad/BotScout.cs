using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A captain taking a paid party out to ground nobody has ever stood in.
///
/// <para>
/// <b>This is the one errand on the shard whose product is knowledge rather than goods.</b> Everything else
/// a bot does ends in coin, in a made thing or in a dead monster; this ends in a square on the map changing
/// from "never stood in" to a reading. The population cannot hunt where it has not been, cannot be sent
/// where nothing is known, and — because <see cref="BotQuad"/> credits ground for being walked — cannot even
/// tell safe ground from unvisited ground without somebody going and looking.
/// </para>
///
/// <para>
/// <b>Paid, and paid out of the captain's own pocket, by order.</b> Fifty gold split between whoever comes.
/// It is a small sum on purpose: scouting is not meant to compete with hunting on takings, it is meant to be
/// worth doing when nothing better is going. What the money really buys is a reason for the other bots to
/// come at all — the auction weighs every want in gold a minute, and an unpaid walk into the unknown scores
/// exactly nothing against a hunt. The captain is kept from ruining himself by a floor on his own purse: see
/// <see cref="Solvent"/>.
/// </para>
/// </summary>
public sealed class BotScout : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotScout));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "scout";

    /// <summary>What the captain pays the party, all told, however many come.</summary>
    public static int Wage { get; set; } = 50;

    /// <summary>
    /// What the captain must still have after paying, on pocket and account together.
    ///
    /// <para>
    /// Three hundred, by order, and it is what stops this office from beggaring the bot that holds it. A
    /// captain earns by teaching and spends on his own kit like anybody else; an errand that pays out
    /// unconditionally would be a slow leak with no floor, which is the shape of defect this project has
    /// already found in three other places under the name "a multiplier with no floor".
    /// </para>
    /// </summary>
    public static int Solvent { get; set; } = 300;

    /// <summary>How far a captain will take a party to look at somewhere new.</summary>
    /// <summary>
    /// How far out the frontier is looked for.
    ///
    /// <para>
    /// <b>Deliberately not clamped to <see cref="BotHunter.Walkable"/>, and it was for an hour on
    /// 08.09.2026.</b> That clamp is a price rule, not a reachability one — measured the same afternoon,
    /// raising it from 240 to 900 left the count of refused roads flat at one or two a minute while the
    /// searching bill went from 3.36ms a search and 8% partial to 13.36ms and 51%. Distance costs clock; it
    /// does not make a road stop existing.
    /// </para>
    ///
    /// <para>
    /// A price rule has to be applied where the price is paid, and this is the one errand on the shard where
    /// it is not: forty-seven bots throw darts for prowls, and exactly one Baron scouts, a few times an
    /// hour. Clamped, the frontier vanished — everything within 240 tiles has been walked for days — and the
    /// Baron strolled round the town instead. Unclamped, he took scouting three times in three minutes.
    /// </para>
    /// </summary>
    public static int Range
    {
        get => _range > 0 ? _range : BotPopulation.Roam;
        set => _range = value;
    }

    private static int _range;

    /// <summary>How near the captain a volunteer has to be to be called on.</summary>
    public static int Reach { get; set; } = 30;

    /// <summary>Fewest bodies worth going with, the captain included.</summary>
    public static int Least { get; set; } = 2;

    /// <summary>Longest a party may be out before it turns for home.</summary>
    public static int CapMs { get; set; } = 600000;

    /// <summary>What scouting is reckoned at per minute before experience corrects it.</summary>
    public static double Prior { get; set; } = 40.0;

    /// <summary>How long the walk is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 6.0;

    /// <summary>Parties that actually formed and set out.</summary>
    public static long Parties { get; private set; }

    /// <summary>Captains who won the errand and could not raise a party for it.</summary>
    public static long Undermanned { get; private set; }

    /// <summary>Squares actually reached and read.</summary>
    public static long Surveyed { get; private set; }

    /// <summary>Parties that ran out of time before arriving.</summary>
    public static long Timedout { get; private set; }

    /// <summary>Gold actually handed over, and to how many.</summary>
    public static long Wages { get; private set; }

    public static long Paid { get; private set; }

    private readonly Map _map;

    /// <summary>
    /// The square being walked to now. Not readonly: a Baron's rounds move on to the next one on arrival.
    ///
    /// <para>
    /// <b>One square per errand is what made the Baron look stuck, and it was not a bug in the walking.</b>
    /// He reached his square, the errand ended, and the next one had to win an auction — which it could not,
    /// because winning the last one had left him leading a company, and a Baron leading a company is refused
    /// his rounds before anything is scored. Seven of ten answers in one window were exactly that. Rounds
    /// are a route rather than a destination, so the route belongs inside one errand.
    /// </para>
    /// </summary>
    private Point3D _where;

    /// <summary>
    /// What this particular party is worth, all told. Nought for a Baron's own rounds.
    ///
    /// <para>
    /// <b>Held on the deed rather than read off the static, because two offices walk into the unknown for
    /// different reasons.</b> A captain pays a party because he is buying their time away from work that
    /// pays; the Baron pays nobody, by order, and goes whether or not anybody follows — walking his ground
    /// is what the office <em>is</em>. Same errand, same walk, same map entry: only the wage and how few
    /// bodies will do differ, so they are the two things the caller names.
    /// </para>
    /// </summary>
    private readonly int _wage;

    private readonly int _least;

    /// <summary>How many squares this errand walks before it is finished. One for a paid party.</summary>
    private readonly int _rounds;

    /// <summary>
    /// Whether only the caller's own kind may fall in.
    ///
    /// <para>
    /// For the King's Rangers, who are a standing company rather than a party raised on the spot. A captain
    /// takes whoever is free and that is the point of paying them; a ranger company that swept up a passing
    /// miner would be walking into unread ground with a body in it that has no armour, no orders and a trade
    /// to get back to. Their formation is the whole of why five of them survive where one would not.
    /// </para>
    /// </summary>
    private readonly bool _kin;

    /// <summary>Ground this errand will not walk past, or an empty rectangle for anywhere.</summary>
    private readonly Rectangle2D _bounds;

    /// <summary>How many have been read so far.</summary>
    private int _read;

    private BotSquad _squad;

    private int _called;

    private long _began;

    public BotScout(Map map, Point3D where) : this(map, where, Wage, Least, 1)
    {
    }

    public BotScout(Map map, Point3D where, int wage, int least, int rounds)
        : this(map, where, wage, least, rounds, false, default)
    {
    }

    public BotScout(Map map, Point3D where, int wage, int least, int rounds, bool kin, Rectangle2D bounds)
    {
        _map = map;
        _where = where;
        _wage = Math.Max(0, wage);
        _least = Math.Max(1, least);
        _rounds = Math.Max(1, rounds);
        _kin = kin;
        _bounds = bounds;
        _began = Core.TickCount;
    }

    public override string Kind => Trade;

    /// <summary>Goes to see what is out there, danger included. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => _wage;

    public override double Coin => 1.0;

    /// <summary>
    /// Paid nothing on purpose, like the rounds — and the flag was put on the rounds and not on this.
    ///
    /// <para>
    /// <b>Scouting cannot show a profit at all, by construction.</b> It pays a wage out of the Baron's own
    /// purse to whoever walks with him (<see cref="Outlay"/>) and brings back <em>ground</em>: quadrants
    /// nobody had stood in, which is what every hunting ground, every seam and every forge on this shard is
    /// eventually found through. The ledger measures money, so it measured this at a loss and
    /// <c>BotAppraisal</c>'s "expected to pay nothing" veto threw it away.
    /// </para>
    ///
    /// <para>
    /// Found on 08.09.2026, an afternoon after the identical defect was fixed on <c>BotRounds</c> — same
    /// office, same shard, same flag, one file along. The Baron was being offered scouting nine times out of
    /// nine and taking his 6-a-minute stroll round the town instead, while the island's frontier went
    /// unwalked. See BotDeed.Unpaid for why this is a flag and not a floor under the estimate.
    /// </para>
    /// </summary>
    public override bool Unpaid => true;

    public override bool Alongside => true;

    public override string Stage =>
        _squad == null
            ? $"raising a party for the ground at ({_where.X}, {_where.Y})"
            : _rounds > 1
                ? $"scouting ({_where.X}, {_where.Y}) with {_called} of us, {_read} of {_rounds} squares read"
                : $"scouting ({_where.X}, {_where.Y}) with {_called} of us";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (bot is not IBotSquadMember member)
        {
            return BotDoing.Failed("not the sort of thing that leads parties");
        }

        return _squad == null ? Calling(member, body) : Walking(member, body);
    }

    /// <summary>Calls for volunteers where the captain stands, and sets out if enough came.</summary>
    private BotDoing Calling(IBotSquadMember member, Mobile body)
    {
        var squad = member.Squad;

        // <b>A company already has a leader, and it is not everybody who was offered the errand.</b> Three of
        // the five rangers won this in the same beat: the first raised the company, the other two joined it,
        // and then each of them read `member.Squad` back, decided it was *their* company, and set out for a
        // different square. "Ser Alric is taking 5 of them to (1425, 1425)" and "Fletcher Wyn is taking 5 of
        // them to (1395, 1455)" in the same second, on the same five bots — three orders on one company, so
        // it stood still. Falling in behind somebody is not the same as leading, and only the leader walks
        // the route.
        if (squad != null && !ReferenceEquals(squad.Leader, member))
        {
            return BotDoing.Done("somebody else is leading this company");
        }

        // <b>A standing company is not raised here, and must not be.</b> The King's Rangers are mustered once
        // for their whole lives — see BotRangers.Muster — so this errand finds their squad already made,
        // already charged and already holding all five. Calling one together per sweep is what produced a
        // company that dissolved after every skirmish and five bots with nothing to do.
        if (_kin)
        {
            if (squad == null)
            {
                return BotDoing.Failed("the company has not been mustered");
            }
        }
        else
        {
            squad ??= BotSquads.Form(member);

            if (squad == null)
            {
                return BotDoing.Failed("could not call a party together");
            }

            // The usual party, not the companies' ceiling (twenty since 01.10.2026), and every one of it pressed to the enlistment
            // so it walks with the party rather than standing Bound with its own errand set aside. See BotSquads.Enrol.
            foreach (var mobile in _map.GetMobilesInRange<Mobile>(body.Location, Reach))
            {
                if (squad.Count >= Math.Min(squad.Ceiling, BotSquad.Usual))
                {
                    break;
                }

                if (mobile == body || mobile is not IBotSquadMember { Squad: null } other)
                {
                    continue;
                }

                if (mobile is not IBotAlly { AbleToFight: true })
                {
                    continue;
                }

                BotSquads.Enrol(squad, other, _map, body.Location, $"called to {body.Name}'s party to look at ({_where.X}, {_where.Y})");
            }
        }

        _called = squad.Count;

        if (_called < _least)
        {
            Undermanned++;

            BotSquads.Leave(member);

            return BotDoing.Failed($"only {_called} would come to look at ({_where.X}, {_where.Y})");
        }

        _squad = squad;

        // Without this the party is dissolved on the next squad beat for having nothing to fight — which is
        // precisely what a scouting party is meant to have. See BotSquad.Charged.
        squad.Charged = true;

        Parties++;

        // Stamped when the party forms rather than when the deed was built: the auction builds one of these
        // every time it asks and throws most of them away.
        _began = Core.TickCount;

        BotVoice.Aloud(body, _wage > 0 ? "Ground nobody has walked. Who is coming? There is coin in it." : "Ground nobody has walked. I am going to look at it.");

        logger.Information(
            "{Name} is taking {Count} of them to look at ({X}, {Y}), which nobody has stood in",
            body.Name,
            _called,
            _where.X,
            _where.Y
        );

        return BotDoing.Walk(_map, _where, BotArrival.Within(BotQuad.Side / 3), $"scouting ({_where.X}, {_where.Y})");
    }

    /// <summary>On the road, and paying out when the ground is reached.</summary>
    private BotDoing Walking(IBotSquadMember member, Mobile body)
    {
        var squad = member.Squad;

        if (squad == null || !ReferenceEquals(squad, _squad))
        {
            return BotDoing.Done("the party broke up on the road");
        }

        _called = squad.Count;

        // The crown's surgeon left on his own. See BotRangers.SurgeonAlone: a healer by himself is not a
        // company, and the round ends rather than walking him into whatever killed the other four.
        if (Core.TickCount - _began >= CapMs)
        {
            Timedout++;

            // <b>Marked here rather than in Drop, because Disband clears the squad on the line below and
            // Drop's first test is that the squad exists.</b> So the one ending that reliably reaches this
            // errand's own time limit was the one ending that recorded nothing, and Baldric was offered
            // (1065, 1095) at 14:32, gave up on it at 14:42, and would have been offered it again. The square
            // is known now — known to be further than a party can walk in the time it is given — which is the
            // same fact Bend records about a square it steps past.
            BotQuad.Seen(_map, _where);
            Unreached++;

            Disband(member);

            return BotDoing.Done($"gave up on reaching ({_where.X}, {_where.Y})");
        }

        // Arrival is judged by where the bodies are and not by what the journey says: the ground is the
        // point, and a party standing in the square has done the errand whether or not the walk agrees.
        if (!body.InRange(_where, BotQuad.Side / 2))
        {
            return BotDoing.Walk(_map, _where, BotArrival.Within(BotQuad.Side / 3), $"scouting ({_where.X}, {_where.Y})");
        }

        Surveyed++;
        _read++;

        // Walking into it is what makes it known — every bot in the party is doing that right now, and
        // BotMobile.Cross has already told the map about each of them. Said here so the square is marked even
        // if the party arrived by some route that never crossed a boundary.
        // A ranger sweep marks the ground read and leaves the reading where it was; anybody else's arrival
        // is an ordinary first footfall. See BotQuad.Swept for why elite survival proves less, not more.
        if (_kin)
        {
            BotQuad.Swept(_map, body.Location);
        }
        else
        {
            BotQuad.Seen(_map, body.Location);
        }

        // <b>On to the next square, for as many as this errand was given.</b> The frontier is asked again
        // from where the party is standing rather than from where it started, so the route walks outwards
        // instead of returning to re-plan from home. A round that finds nothing left unknown ends the errand
        // honestly — the island within reach has been read, which is the errand succeeding, not failing.
        if (_read < _rounds)
        {
            var next = NextSquare(body);

            if (next != Point3D.Zero)
            {
                _where = next;

                // The clock restarts with the leg. It is there to catch a party that cannot reach the square
                // in front of it, not to punish one that is walking twenty of them as ordered — measured
                // across the whole route it would end every round of a Baron's rounds as a failure to arrive.
                _began = Core.TickCount;

                return BotDoing.Walk(_map, _where, BotArrival.Within(BotQuad.Side / 3), $"scouting ({_where.X}, {_where.Y})");
            }
        }

        var paid = _wage > 0 ? Pay(body, squad, _wage) : 0;

        Disband(member);

        return BotDoing.Done(
            paid > 0
                ? $"read {_read} squares and paid {paid}gp for it"
                : $"read {_read} squares, the last at ({_where.X}, {_where.Y})"
        );
    }

    /// <summary>
    /// Splits the wage between whoever came, out of the captain's own money.
    ///
    /// <para>
    /// The captain's share is not taken out and handed back to him — he keeps what is left by not paying it
    /// away, which is the same thing and cannot go wrong halfway. Coin comes out of the pack first and the
    /// account second, in that order, because that is the order every other purchase on this shard uses and
    /// a bot's pocket is what its own errands are paid from.
    /// </para>
    /// </summary>
    private static int Pay(Mobile captain, BotSquad squad, int wage)
    {
        var members = squad?.Members;

        if (members == null || captain?.Backpack == null)
        {
            return 0;
        }

        List<Mobile> owed = [];

        for (var i = 0; i < members.Count; i++)
        {
            var body = members[i]?.Self;

            if (body != null && body != captain && !body.Deleted && body.Backpack != null)
            {
                owed.Add(body);
            }
        }

        if (owed.Count == 0)
        {
            return 0;
        }

        var each = Math.Max(1, wage / owed.Count);
        var total = each * owed.Count;

        if (BotYield.Spendable(captain) - total < Solvent)
        {
            return 0;
        }

        var pack = captain.Backpack;
        var carried = pack.GetAmount(typeof(Gold));
        var fromPack = Math.Min(carried, total);
        var fromBank = total - fromPack;

        if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
        {
            return 0;
        }

        if (fromBank > 0 && !Banker.Withdraw(captain, fromBank))
        {
            // Put back what was already taken. Any other ordering here is a way to lose a bot's money.
            if (fromPack > 0)
            {
                pack.DropItem(new Gold(fromPack));
            }

            return 0;
        }

        for (var i = 0; i < owed.Count; i++)
        {
            owed[i].Backpack.DropItem(new Gold(each));
        }

        Wages += total;
        Paid += owed.Count;

        logger.Information(
            "{Name} paid {Total}gp to {Count} of them for walking into the unknown, {Each}gp apiece",
            captain.Name,
            total,
            owed.Count,
            each
        );

        return total;
    }

    /// <summary>
    /// The square in front cannot be reached. Take the next one instead of failing the whole round.
    ///
    /// <para>
    /// <b>One unwalkable square was ending entire sweeps, and the ground is full of them.</b> The Baron read
    /// sixteen squares of twenty, met one on a hilltop at height thirty that no route could close on, and
    /// the round failed — leaving him standing in a field with nothing to do, which from outside is a Baron
    /// who has simply stopped. A frontier is a list, not a promise: any one square on it may turn out to be
    /// a cliff, a lake or a walled yard, and the correct answer is the next square rather than going home.
    /// </para>
    ///
    /// <para>
    /// The square is marked read before moving on. It genuinely is known now — known to be unreachable from
    /// here — and leaving it unread would offer it again on the next beat, which is the loop this shard has
    /// paid for under the name "the same square offered for ever".
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _squad == null)
        {
            return false;
        }

        BotQuad.Seen(_map, _where);
        Baulked++;

        if (_read >= _rounds)
        {
            return false;
        }

        var next = NextSquare(body);

        if (next == Point3D.Zero || next == _where)
        {
            return false;
        }

        _where = next;
        _began = Core.TickCount;

        return true;
    }

    /// <summary>Squares given up on as unreachable and stepped past. A named nought, not a failed round.</summary>
    public static long Baulked { get; private set; }

    /// <summary>Squares the party never stood in, marked read as the errand ended however it ended.</summary>
    public static long Unreached { get; private set; }

    /// <summary>
    /// The errand is over, whichever way it ended, and the square in front was never stood in.
    ///
    /// <para>
    /// <b>Two exits from the same situation, and until now only one of them wrote the lesson down.</b>
    /// <see cref="Bend"/> marks a square read before stepping past it, and says why: leaving it unread offers
    /// it again on the next beat. But a round that runs out of <see cref="CapMs"/>, and a round taken off the
    /// bot by <c>BotStall</c> for having stopped getting anywhere, end the same errand at the same square and
    /// went through neither. The log says what that costs. Aldric the Captain took a party of six to
    /// (1605, 2115) at 09:40 on 03.09.2026, gave up after seven minutes, took the same party to the same
    /// square at 09:58, gave up again, and took it a third time one minute after the shard was restarted —
    /// because the square was still the nearest ground nobody had stood in, and nothing anywhere recorded
    /// that six bots had spent a quarter of an hour failing to get to it.
    /// </para>
    ///
    /// <para>
    /// Marking it read is the same small lie <see cref="Bend"/> already tells, for the same reason: the square
    /// genuinely is known now — known to be unreachable from where the population lives — and that is the fact
    /// worth keeping. It costs the square nothing else. <c>BotQuad.Seen</c> sets a flag and a timestamp and
    /// does not touch the danger reading, so a square retired this way is taken off the frontier without being
    /// called safe.
    /// </para>
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        var body = bot?.Self;

        // No party was ever raised, so nobody tried to walk anywhere and there is nothing to conclude.
        if (body == null || _map == null || _squad == null)
        {
            LetGo(bot);

            return;
        }

        // Arrival is judged the way Walking judges it: by where the bodies are, not by what the journey says.
        if (body.InRange(_where, BotQuad.Side / 2))
        {
            LetGo(bot);

            return;
        }

        BotQuad.Seen(_map, _where);
        Unreached++;

        logger.Information(
            "{Name} never stood in ({X}, {Y}) and it has been marked read, so it will not be offered again",
            body.Name,
            _where.X,
            _where.Y
        );

        LetGo(bot);
    }

    /// <summary>
    /// The party, which is the largest thing this errand holds and was the one thing it never let go of.
    ///
    /// <para>
    /// <b>A raised party is exactly what Drop's summary means by "whatever was being held for it", and it
    /// outlived every ending but two.</b> Disband was called when the round finished and when it ran out of
    /// CapMs, and not when BotStall took the errand off the bot for having stopped getting anywhere - so a
    /// captain whose scout was abandoned went on leading a company, and a bot in a company sits on the Bound
    /// rung with the auction switched off. Six bots with no work of their own, per abandoned round, until
    /// something else happened to them.
    /// </para>
    ///
    /// <para>
    /// The shard was saying so all day and in the plainest terms: "3 squads standing holding 15 bots" in
    /// every window on 03.09.2026, which is 44 per cent of the population, against 4 formed and 1 disbanded.
    /// Aldric the Captain stood at (1240, 2003) for ten minutes with its errand reading "nothing", five
    /// hundred and fifty tiles from home, until the rescue carried it back.
    /// </para>
    /// </summary>
    private void LetGo(IBotWilful bot)
    {
        if (_squad != null && bot is IBotSquadMember member)
        {
            Disband(member);
        }
    }

    /// <summary>Whether this square is inside the ground this errand was confined to. Anywhere, when it was not.</summary>
    private bool Within(Point3D at) =>
        _bounds.Width <= 0 || _bounds.Height <= 0 || _bounds.Contains(new Point2D(at.X, at.Y));

    /// <summary>Whether the ground between here and there is not already known to be closed.</summary>
    private static bool Reachable(Map map, Point3D from, Point3D at) =>
        BotReach.Ask(map, from, at, BotArrival.Within(BotQuad.Side / 3)) != BotReachVerdict.Sealed;

    /// <summary>
    /// The longest road from home a scouting party is sent down. Six hundred, which is what a prowl walks.
    ///
    /// <para>
    /// <b>The frontier had moved across the river, and the rounds followed it square by square.</b> By 22.09.2026 the
    /// island read "3566 quadrants, 3553 of them stood in": what was left unread lay beyond the banks west of Britain and
    /// north past the ford, reachable only round by the south. Each next square is vetted from where the party stands,
    /// so a round could walk twenty squares out along that detour — Faron "taking 4 of them to look at (1305, 735)",
    /// stations at (1018, 814), (994, 732), (1029, 797) — and disband there. Leofric was left at (1073, 1388) at 12:50
    /// with every walk home "no way through"; Fenna and Gerda before him. The road map from home answers the question
    /// the vet from the party's own tile cannot: whether anybody sent there can walk back.
    /// </para>
    /// </summary>
    public static int RoadLimit { get; set; } = 600;

    /// <summary>Squares passed over for lying beyond <see cref="RoadLimit"/> of road from home, or off the road map altogether.</summary>
    public static long Roadless { get; private set; }

    /// <summary>Whether the road map says a party sent here could walk home. Before the map is drawn, the old answer: yes.</summary>
    internal static bool Roadworthy(Map map, Point3D at)
    {
        if (!BotRoads.Ready)
        {
            return true;
        }

        // Off the map's square is past its reach as the crow flies, and RoadLimit is shorter than that: the first
        // round after this was written went to (795, 645), one square outside the square, on "unknown ground".
        if (!BotRoads.Covers(map, at.X, at.Y))
        {
            Roadless++;

            return false;
        }

        var road = BotRoads.FromHome(map, at.X, at.Y);

        if (road >= 0 && road <= RoadLimit)
        {
            return true;
        }

        Roadless++;

        return false;
    }

    /// <summary>
    /// The next square on the frontier that this party could actually walk to.
    ///
    /// <para>
    /// <b>BotScoutmaster looks for a way before it commits a party; this is the same question asked again
    /// every time the route moves on, and it was not being asked at all.</b> A Baron's rounds walk twenty
    /// squares and pick each one here, so the vet at the gate covers the first and none of the other
    /// nineteen. Baldric stood at (1108, 806) for four minutes on the first square of a round on 03.09.2026
    /// and at (1190, 750) for nine on the seventh, and two of the nine stalls in that three-quarters of an
    /// hour were his rounds alone.
    /// </para>
    ///
    /// <para>
    /// A square with no way to it is marked read before the next is asked for, so the frontier advances
    /// instead of handing back the same answer — the loop this file has now been fixed against three times.
    /// A search handed less clock than it asked for concludes nothing and the square is left alone: striking
    /// one off is permanent, and a starved search has not looked at the island.
    /// </para>
    /// </summary>
    private Point3D NextSquare(Mobile body)
    {
        for (var tries = 0; tries < Vets; tries++)
        {
            var next = BotQuad.Frontier(
                _map,
                body.Location,
                Range,
                at => Within(at) && Roadworthy(_map, at) && Reachable(_map, body.Location, at)
            );

            if (next == Point3D.Zero)
            {
                return Point3D.Zero;
            }

            var cramped = BotPath.Starved;

            if (BotPath.CanReach(_map, body.Location, next, BotArrival.Within(BotQuad.Side / 3), BotScoutmaster.VetMs))
            {
                return next;
            }

            if (BotPath.Starved != cramped)
            {
                return Point3D.Zero;
            }

            BotQuad.Seen(_map, next);
            Unreached++;
        }

        return Point3D.Zero;
    }

    /// <summary>
    /// How many squares to look for a way to before giving this beat up.
    ///
    /// Three, because each one costs a real search and the round comes back in a few seconds anyway. The
    /// ones struck off stay struck off, so a coast of unreachable squares is worked through a few at a time
    /// rather than all at once inside a single tick.
    /// </summary>
    public static int Vets { get; set; } = 3;

    private void Disband(IBotSquadMember member)
    {
        // <b>A standing company outlives its errands.</b> The rangers are one unit for their whole lives; the
        // sweep ending is not the company ending, and taking it apart here is what left them scattered and
        // idle after every fight. Only a party raised for this errand is let go by it.
        if (_kin)
        {
            _squad = null;

            return;
        }

        // <b>The whole party, and it used to be the leader alone.</b> Dropping the charge was meant to let
        // the company end itself on its next beat, and it cannot: a company dissolves at nought members or at
        // one uncharged, and this leaves five. They inherit a leader with no errand and sit on the Bound rung,
        // where the auction is switched off, so not one of them has any work of its own to walk away to.
        // Every bot the stall watch caught standing still on 03.09.2026 was "in a company"; the captain
        // beside them was "on its own", having been the one bot Leave took out.
        //
        // Update's own rule is that whoever charged a company owns ending it. This errand charged it.
        var squad = _squad ?? member?.Squad;

        if (squad != null)
        {
            squad.Charged = false;

            BotSquads.Disband(squad, "the errand that raised it is over");
        }

        BotSquads.Leave(member);

        _squad = null;
    }

    public static string Describe() =>
        $"{Parties} scouting parties set out, {Undermanned} could not raise {Least} bodies, {Surveyed} squares read, {Baulked} stepped past as unreachable, "
        + $"{Unreached} never stood in and retired off the frontier, {Roadless} passed over for lying past {RoadLimit} tiles of road from home, {Timedout} ran out of time; {Wages}gp paid to {Paid} volunteers";

    public static void Forget()
    {
        Parties = 0;
        Undermanned = 0;
        Surveyed = 0;
        Baulked = 0;
        Unreached = 0;
        Roadless = 0;
        Timedout = 0;
        Wages = 0;
        Paid = 0;
    }
}
