using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A company called together for a place rather than for a creature, and kept together until the place stops
/// killing people.
///
/// <para>
/// <b>This is the shard's second reason for a group to exist, and it is a different reason.</b>
/// <see cref="BotBand"/> musters against one thing that is too big for one bot: it forms when the thing is
/// in sight, it ends when the thing is dead, and everybody goes back to their own business — which is
/// exactly right, and is why the squads that ran all day were over in a minute or two. A patrol has no
/// quarry at all. It is dispatched to a square that has been hurting people, and its work is finished when
/// the square is quiet, which is a condition that cannot be met by killing any particular thing.
/// </para>
///
/// <para>
/// <b>Almost none of the marching is written here, and that is the point of putting it on a squad.</b> The
/// company follows its leader by arithmetic — <c>BotSquad.Station</c> re-forms whenever the anchor drifts,
/// and the stance falls out of whether the leader is walking: on the road they hold formation, and the
/// moment the captain stops in the middle of the square they scatter into scouting knots and cover it. Being
/// hit anywhere in that spread pulls the whole company onto the attacker through <c>BotSquads.Note</c>. So
/// "sweep a wood" needed no sweeping code: it needed somewhere to stand and a reason not to go home.
/// </para>
///
/// <para>
/// <b>The reason not to go home is the one piece of the squad this had to change.</b> A company disbands
/// after five quiet minutes, which is the right rule for a muster and the wrong one for a patrol — the whole
/// value of standing in a dangerous wood is being there before anything happens. See
/// <see cref="BotSquad.Charged"/>.
/// </para>
/// </summary>
public sealed class BotSweep : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSweep));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "sweep";

    /// <summary>
    /// Companies that actually formed and set out.
    ///
    /// <para>
    /// <b>Kept here rather than where the offer is made, because they are not the same event and the shard
    /// has paid for that confusion four times already.</b> <c>BotPatrol.Offered</c> counts offers handed to
    /// the auction, and most offers are thrown away unchosen — 22 of them against 2 real marches in one hour
    /// on 26.08.2026. A march is a captain who won the auction, called, and got enough bodies to go.
    /// </para>
    /// </summary>
    public static long Marches { get; private set; }

    /// <summary>Captains who won a patrol and then could not raise <see cref="Least"/> bodies for it.</summary>
    public static long Undermanned { get; private set; }

    /// <summary>
    /// What a patrol is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// <b>Low on purpose, and it is not meant to compete on money.</b> A sweep pays what its fights happen to
    /// drop, which is a hunt's takings spread over a much longer errand, so judged as trade it is poor and
    /// the ledger will say so within a session. It wins its place in the auction because only one bot on the
    /// shard is ever offered it and that bot has almost nothing else it would rather do — see
    /// <see cref="BotPatrol"/>. A number chosen to make it win would be a thumb on the scale, and the auction
    /// is the one thing on this shard that has never needed one.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 45.0;

    /// <summary>How long a patrol is expected to be out, walk and all.</summary>
    public static double WorkMinutes { get; set; } = 12.0;

    /// <summary>
    /// How long the company spends walking to one corner of its square before trying the next.
    ///
    /// Long enough to actually arrive across a third of a square, short enough that a patrol that has walked
    /// into a dead end gives up on that corner rather than on the afternoon.
    /// </summary>
    public static int RoundMs { get; set; } = 45000;

    /// <summary>Fewest bodies worth marching anywhere with. A captain and one other is an escort, not a company.</summary>
    public static int Least { get; set; } = 3;

    /// <summary>
    /// How many corners of the square may turn out to be unwalkable before the patrol gives the square up.
    ///
    /// <para>
    /// Four, which is the rest of the ring: a corner over a cliff edge or inside a wall is an ordinary fact
    /// about ground and says nothing about the other three. Counted from the last corner actually reached
    /// rather than over the whole patrol, or half an hour of honest walking accumulates the bends of a
    /// patrol that never worked at all.
    /// </para>
    /// </summary>
    public static int MaxBends { get; set; } = 4;

    /// <summary>How far around itself a captain calls for volunteers.</summary>
    public static int Reach { get; set; } = 40;

    /// <summary>
    /// The longest one patrol may last.
    ///
    /// <para>
    /// Half an hour, which is six times a muster's whole life and is meant to be. The order was for companies
    /// that live a long time; the cap exists because "until the square is quiet" is a condition a square full
    /// of respawning residents can refuse to meet all night, and a company that can never be wrong is the
    /// bot standing in a fight for twenty-two minutes wearing a different hat.
    /// </para>
    /// </summary>
    public static int CapMs { get; set; } = 1800000;

    /// <summary>
    /// How long the company stands in the square before "it is quiet here" is allowed to mean anything.
    ///
    /// <para>
    /// A square reads high because of what happened in it over the last twenty minutes, and a patrol that
    /// checked the reading on arrival would find the trouble it was sent for still on the board and the
    /// square still counted as dangerous — or, once the sweep knocks it down, would find it quiet the instant
    /// it got there and turn round. Neither of those is a patrol. The company holds the ground for this long
    /// before the question is asked at all.
    /// </para>
    /// </summary>
    public static int HoldMs { get; set; } = 300000;

    /// <summary>How far a member may fall behind the captain on the march to the square before the captain stops. Eight.</summary>
    public static int MarchSpread { get; set; } = 8;

    /// <summary>Behind this the member is left rather than waited for; a hold longer than <see cref="MarchHoldMs"/> ends the same way.</summary>
    public static int MarchLost { get; set; } = 60;

    public static int MarchHoldMs { get; set; } = 90000;

    /// <summary>
    /// <b>The company gathers on its captain before it sets off (29.09.2026, build 319; Patrick: "they gather in one place,
    /// before the dangerous squares, and go together").</b> The levy presses whoever is free within <see cref="Reach"/>,
    /// forty tiles, and the captain walked off on the same beat: on build 318 Aelith marched company 10 away from four
    /// members still coming in from their herbs, left Katra "61 tiles back after 15s" and Rhistel after 11s, and Piran and
    /// Valindra "could not get one tile closer" to a captain walking away as fast as they closed. The captain now stands
    /// until every member is within <see cref="MarchSpread"/>, or this long; whoever is still past <see cref="MarchLost"/>
    /// then is let go, and the march starts with the rest.
    /// </summary>
    public static int GatherMs { get; set; } = 90000;

    /// <summary>Companies gathered before the march, and members let go for not coming in. See <see cref="GatherMs"/>.</summary>
    public static long Gathered { get; private set; }

    public static long Ungathered { get; private set; }

    /// <summary>Holds for stragglers on the march, and members left behind. See <see cref="MarchSpread"/>.</summary>
    public static long Held { get; private set; }

    public static long LeftBehind { get; private set; }

    /// <summary>Fights the company stood to on the march, the captain with it. See <see cref="March"/>.</summary>
    public static long Stood { get; private set; }

    /// <summary>Whether the company is standing to a fight on the march. See <see cref="March"/>.</summary>
    private bool _stoodTo;

    private readonly Map _map;

    private readonly Point3D _square;

    private readonly double _read;

    private long _began;

    private long _stoodTick;

    /// <summary>When the captain began holding for a straggler; nought when it is not. See <see cref="MarchSpread"/>.</summary>
    private long _holdTick;

    /// <summary>Whether the company has gathered on its captain, and when the gathering began. See <see cref="GatherMs"/>.</summary>
    private bool _gathered;

    /// <summary>
    /// Whether the company has stood on its square. <b>After that the march's straggler rule is not asked (29.09.2026, build
    /// 320).</b> On 319 Brokk's company reached (1422, 1878), the captain went two tiles too far after something in a fight,
    /// the walk back was read as the march again, and a member at her station 63 tiles off was "left behind on the march" —
    /// "the company came apart", one fight into the patrol. Spread over the square is what a patrol is.
    /// </summary>
    private bool _reached;

    private long _gatherTick;

    private bool _standing;

    /// <summary>What the company was last seen fighting, so that a new focus can be reported to the will as progress.</summary>
    private Mobile _focus;

    private BotSquad _squad;

    private int _called;

    private int _fights;

    private long _steppedTick;

    private int _round;

    private Point3D _post;

    private int _bends;

    /// <summary>Whether the company was in contact last time it was asked. See <see cref="_fights"/>.</summary>
    private bool _fighting;

    /// <summary>A mage's gate taking the company to its rune near the square, while it lasts. See <see cref="BotPassage"/>.</summary>
    private BotPassage _passage;

    /// <summary>Who is standing near to be called, strongest first. Reused between calls. See <see cref="Calling"/>.</summary>
    private static readonly System.Collections.Generic.List<Mobile> _recruits = [];

    /// <summary>Sweeps not raised because the bots within call brought less than the square asks. See <see cref="Calling"/>.</summary>
    public static long Short { get; private set; }

    /// <summary>Sweeps that left their square when it was closed to the population while they were out for it. See <see cref="Patrolling"/>.</summary>
    public static long ClosedOn { get; private set; }

    public BotSweep(Map map, Point3D square, double reading)
    {
        _map = map;
        _square = square;
        _read = reading;
        _began = Core.TickCount;
    }

    public static string Describe() =>
        $"{Marches} companies actually marched ({Gathered} gathered on the captain first, {Ungathered} members let go for not coming in; {Held} holds for stragglers, {LeftBehind} left behind, {Stood} fights stood to on the road with the captain), {Undermanned} could not raise {Least} bodies once chosen, {Short} were not raised for the bots within call bringing less than the square asks, {ClosedOn} left their square when it was closed while they were out for it";

    public override string Kind => Trade;

    /// <summary>Goes where the fighting is on purpose. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    public override Map Map => _map;

    public override Point3D Where => _square;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>
    /// Nothing. A patrol trains whatever the fights in it happen to train, and claiming a skill here would
    /// be claiming it twice — the fights are the shard's ordinary combat and are already counted.
    /// </summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override bool Alongside => true;

    public override string Stage =>
        !_standing
            ? $"marching {_called} of us on the square at ({_square.X}, {_square.Y}), which reads {_read:F0}"
            : $"walking the square at ({_square.X}, {_square.Y}) with {_called} of us, {_fights} fights so far";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (bot is not IBotSquadMember member)
        {
            return BotDoing.Failed("not the sort of thing that leads companies");
        }

        if (_squad == null)
        {
            return Calling(member, body);
        }

        return Patrolling(member, body);
    }

    private BotDoing Calling(IBotSquadMember member, Mobile body)
    {
        // <b>As many as the square asks (01.10.2026).</b> The call took whoever stood within Reach up to the company's ceiling,
        // which was five; with the ceiling at twenty (BotSquad.MaxSize) it takes the usual five and goes on only while the
        // company is short of twice what the square asks (BotSquads.Enough). Whatever each held is put down for it, as since 311.
        // <b>The strongest first, and not raised at all short of what the square asks (03.10.2026).</b> What the square asks is the
        // record or the fight standing there now, whichever is more (BotPatrol.Asks), and the call is weighed before anybody is
        // pressed: the offer asked the same of the bots near when it was made (BotPatrol.Takes), and a company formed and let go
        // would have put down every member's work for nothing. As a prowl raises its company (BotProwl.Advance).
        var asked = BotPatrol.Asks(_map, _square);
        var strength = BotQuad.Strength(body);

        _recruits.Clear();

        foreach (var mobile in _map.GetMobilesInRange<Mobile>(body.Location, Reach))
        {
            if (BotPatrol.Callable(body, mobile))
            {
                _recruits.Add(mobile);
            }
        }

        _recruits.Sort((a, b) => BotThreat.Power(b).CompareTo(BotThreat.Power(a)));

        if (member.Squad == null && asked > 0.0)
        {
            var could = strength;

            for (var i = 0; i < _recruits.Count && i < BotSquad.MaxSize - 1; i++)
            {
                could += BotThreat.Power(_recruits[i]);
            }

            if (could < asked)
            {
                _recruits.Clear();
                Short++;

                return BotDoing.Failed($"the bots within call bring {could:F0} of the {asked:F0} of strength ({_square.X}, {_square.Y}) asks");
            }
        }

        var squad = member.Squad ?? BotSquads.Form(member);

        if (squad == null)
        {
            _recruits.Clear();

            return BotDoing.Failed("could not call a company together");
        }

        for (var i = 0; i < _recruits.Count; i++)
        {
            if (BotSquads.Enough(squad, strength, asked))
            {
                break;
            }

            var mobile = _recruits[i];

            if (mobile is not IBotSquadMember { Squad: null } other)
            {
                continue;
            }

            var past = squad.Count >= BotSquad.Usual;

            // As the delve's levy since 299: the errand in hand is put down for the company. See BotDelve.Take.
            if (BotSquads.Enrol(squad, other, _map, body.Location, $"called to {body.Name}'s company for ({_square.X}, {_square.Y})", force: true))
            {
                strength += BotThreat.Power(mobile);

                if (past)
                {
                    BotSquads.Past();
                }
            }
        }

        _recruits.Clear();

        _called = squad.Count;

        if (_called < Least)
        {
            Undermanned++;

            // Let go rather than held: a captain standing about with two volunteers is two bots not working,
            // and the auction will offer this again in fifteen seconds when somebody else is free.
            BotSquads.Leave(member);

            return BotDoing.Failed($"only {_called} were free to march on ({_square.X}, {_square.Y})");
        }

        _squad = squad;

        Marches++;

        // The one thing a patrol needs from the squad that a muster does not. See BotSquad.Charged.
        squad.Charged = true;

        // On the road until it stands on its square: no scattering while the captain holds, and nothing hunted but what stands
        // by the road (build 338). See BotSquad.OnTheMarch.
        squad.OnTheMarch = true;

        // Re-stamped here rather than trusted from the constructor: a deed is built every time the auction
        // asks and most of them are thrown away, so the clock has to start when the company does. The same
        // lesson the minds' own deed learned the hard way.
        _began = Core.TickCount;

        logger.Information(
            "{Name} is marching {Count} of them on the square at ({X}, {Y}), which reads {Read:F0}",
            body.Name,
            _called,
            _square.X,
            _square.Y,
            _read
        );

        // Not a step yet: the company gathers on the captain first. See GatherMs.
        return BotDoing.Work($"gathering {_called} of us to march on ({_square.X}, {_square.Y})");
    }

    /// <summary>The march to the square, holding together. See <see cref="MarchSpread"/>.</summary>
    private BotDoing March(BotSquad squad, Mobile body, long now)
    {
        // <b>The company answers an attack on any member as one (30.09.2026, build 338).</b> Every sweep that came apart on
        // 336–337 came apart in a fight the captain walked away from: Squad 7 on a plague spawn at 23:34:53 and Galathil "left
        // behind … 113 tiles back after 26s — journey active, not moving towards (1526, 769, 0) (station)"; Squad 136 on a
        // wraith at 00:14:22, Helja and Liesl left at 62 and 61; Squad 36 on two wraiths at 01:28:57–01:29:27, Dagna left at
        // 115 — each member standing at its place in the fight, the captain marching on to the square and the straggler rule
        // counting the distance. As the patrol on its square and the Baron's harrowing do (BotHarrow), and the delve's march
        // now (BotDelve.StandTo): while the company is on something the captain answers Work and is stationed and pressed
        // like everybody else, nobody is left behind while it lasts, and the march goes on when it is over. The walk in hand
        // is finished once, when the fight begins, so the company's places take the captain into it.
        if (squad.Stance == BotSquadStance.Fighting && squad.Focus is { Deleted: false, Alive: true } foe)
        {
            if (!_stoodTo)
            {
                _stoodTo = true;
                _holdTick = 0;
                Stood++;

                squad.Leader?.Journey?.Finish();

                logger.Information(
                    "{Name} halts the march on ({X}, {Y}): the company of {Count} turns on {Foe}, which set upon {Who}",
                    body.Name,
                    _square.X,
                    _square.Y,
                    squad.Count,
                    foe.Name,
                    squad.Contact?.Self?.Name ?? "one of us"
                );
            }

            // A new enemy is a new stage for the will's labour clock. See Patrolling.
            if (!ReferenceEquals(foe, _focus))
            {
                _focus = foe;

                if (squad.Leader is IBotWilful wilful && wilful.Resolve != null)
                {
                    wilful.Resolve.StirredTick = now;
                }
            }

            return BotDoing.Work($"standing with the company against {foe.Name} on the way to ({_square.X}, {_square.Y})");
        }

        _stoodTo = false;

        if (_reached)
        {
            return BotDoing.Walk(_map, _square, BotArrival.Within(BotPeril.Side / 3), $"back to the square at ({_square.X}, {_square.Y})");
        }

        if (!_gathered)
        {
            if (_gatherTick == 0)
            {
                _gatherTick = now;
            }

            var late = squad.Straggler(MarchSpread, out var off);

            if (late != null && now - _gatherTick < GatherMs)
            {
                // Held with the road kept, as the delve's holds are (BotJourney.Hold, 01.10.2026, build next). See Holding.
                return Holding(squad, $"gathering the company before the march on ({_square.X}, {_square.Y}): {late.Self?.Name ?? "a member"} is {off} tiles off");
            }

            _gathered = true;
            Gathered++;

            var let = 0;
            var members = squad.Members;

            for (var i = members.Count - 1; i >= 0; i--)
            {
                var other = members[i];

                if (ReferenceEquals(other, squad.Leader) || other?.Self is not { } self)
                {
                    continue;
                }

                if (!self.Alive || self.Map != body.Map || !self.InRange(body.Location, MarchLost))
                {
                    BotSquads.Leave(other);
                    let++;
                }
            }

            Ungathered += let;

            logger.Information(
                "{Name} gathered {Count} of them in {Seconds}s to march on ({X}, {Y}); {Let} did not come in and were let go",
                body.Name,
                squad.Count,
                (now - _gatherTick) / 1000,
                _square.X,
                _square.Y,
                let
            );

            if (squad.Count < Least)
            {
                return Finish(squad, $"only {squad.Count} came in to march on ({_square.X}, {_square.Y})");
            }

            _holdTick = 0;

            // Runes (29.09.2026): a mage of the gathered company holding a rune near the square opens a gate; the march goes on
            // from the far side. The company already stands round the captain, so the passage does not gather it again. Opened
            // once, here, where the gathering ends. See BotPassage.
            _passage = BotPassage.Open(squad, body, _map, _square, $"the square at ({_square.X}, {_square.Y})", false);

            if (_passage != null && _passage.Lead(body, out var gated))
            {
                return gated;
            }
        }

        // <b>The company holds together on the march (29.09.2026).</b> As BotDelve.Marching: a straggler within MarchLost is
        // waited for up to MarchHoldMs, beyond either it is left, and the march goes on while Least are still in it.
        var straggler = squad.Straggler(MarchSpread, out var behind);

        if (straggler != null)
        {
            if (_holdTick == 0)
            {
                _holdTick = now;
                Held++;
            }

            if ((behind <= MarchLost || BotSquad.Coming(straggler, behind)) && now - _holdTick < MarchHoldMs)
            {
                // A hold is a stop, the road kept. See BotDelve.Marching (314) and BotJourney.Hold (01.10.2026, build next).
                return Holding(squad, $"holding for {straggler.Self?.Name ?? "a member"}, {behind} tiles behind, on the way to ({_square.X}, {_square.Y})");
            }

            LeftBehind++;
            var stragglerDeed = (straggler as IBotWilful)?.Resolve?.Deed;
            var stragglerJourney = straggler.Journey;
            logger.Information(
                "{Name} leaves {Who} behind on the march to ({X}, {Y}): {Behind} tiles back after {Seconds}s — in squad {Squad}, holding {Deed} ({Stage}), journey {Moving} towards {Target} ({Reason})",
                body.Name,
                straggler.Self?.Name ?? "a member",
                _square.X,
                _square.Y,
                behind,
                (now - _holdTick) / 1000,
                straggler.Squad?.Id.ToString() ?? "none",
                stragglerDeed?.Kind ?? "nothing",
                stragglerDeed?.Stage ?? "",
                stragglerJourney is { Moving: true } ? "moving" : stragglerJourney is { Active: true } ? "active, not moving" : "idle",
                stragglerJourney?.Target ?? Point3D.Zero,
                stragglerJourney?.Reason ?? ""
            );
            BotSquads.Leave(straggler);
            _holdTick = 0;

            if (squad.Count < Least)
            {
                return Finish(squad, $"the company came apart on the way to ({_square.X}, {_square.Y}): {squad.Count} left");
            }
        }
        else
        {
            _holdTick = 0;
        }

        return BotDoing.Walk(_map, _square, BotArrival.Within(BotPeril.Side / 3), $"marching on ({_square.X}, {_square.Y})");
    }

    /// <summary>
    /// The march's own order with the captain's walk held where it stands (01.10.2026, build next): the same place and arrival as
    /// the march sends, so the Will matches it to the walk in hand instead of rebasing the road and drawing the route again on
    /// every held turn. See BotDelve.Holding and BotJourney.Hold.
    /// </summary>
    private BotDoing Holding(BotSquad squad, string why)
    {
        squad.Leader?.Journey?.Hold(BotDelve.HoldBeatMs);

        return BotDoing.Walk(_map, _square, BotArrival.Within(BotPeril.Side / 3), why);
    }

    private BotDoing Patrolling(IBotSquadMember member, Mobile body)
    {
        var squad = member.Squad;

        // <b>A company that withdrew from what it could not beat is not a patrol that ended (03.10.2026, review of march10, F2).</b>
        // Failed with the reason, as the prowl's company is; "broke up on the road" counted it finished. See BotSquad.Overmatched.
        if (_squad is { Disbanded: true, WithdrewFrom: { } withdrewFrom })
        {
            return BotDoing.Failed($"the company withdrew from {withdrewFrom.Name}, which it could not beat — {_fights} fights, {_called} of us");
        }

        if (squad == null || !ReferenceEquals(squad, _squad))
        {
            return BotDoing.Done("the company broke up on the road");
        }

        _called = squad.Count;

        if (_called < 2)
        {
            return Finish(squad, "there was nobody left to patrol with");
        }

        // <b>Fights, not beats spent fighting.</b> This counted up once per decision while the company was in
        // contact, so one skirmish that lasted a few minutes was reported as hundreds — "1064 fights" for a
        // single half-hour patrol on 25.08.2026, which is not a number anybody could act on. A fight is the
        // company going from not being in contact to being in contact, and nothing else.
        var fighting = squad.Stance == BotSquadStance.Fighting;

        if (fighting && !_fighting)
        {
            _fights++;
        }

        _fighting = fighting;

        var now = Core.TickCount;

        if (now - _began >= CapMs)
        {
            return Finish(squad, $"half an hour on ({_square.X}, {_square.Y}) was enough");
        }

        // <b>Closed ground is left, not walked into (03.10.2026).</b> A quarantine declared on the square after the sweep was
        // offered it — three of ours dead there in half an hour, or one to a plague beast — closes it to every chooser
        // (BotBarred), and the offer would not be made now (BotPatrol.Takes); the march and the patrol stop, and the square is
        // not counted swept. Who clears closed ground is the Baron's harrow and the clearing, sized to it.
        if (BotBarred.Holds(_map, _square))
        {
            ClosedOn++;

            return Abandon(squad, $"({_square.X}, {_square.Y}) was closed to the population while the company was out for it");
        }

        // Still on the road. The squad marches itself: the leader's journey is what puts it in Marching
        // stance, and everybody's station falls out of that.
        //
        // <b>Two different distances, and the gap between them is the whole of what makes a patrol stay
        // put.</b> Arriving is half a side — the company is in its square. <em>Leaving</em> is a whole side,
        // because a captain that walks a corner, or takes three steps after something that hit it, has not
        // left the errand and must not be treated as though it had. Judged on one number this flapped: the
        // patrol of 14:49 on 26.08.2026 crossed back and forth across twelve tiles about twice a second for
        // half an hour, and every crossing did three things — it reset the clock that decides whether the
        // ground has been held long enough for "it is quiet here" to mean anything, so no patrol could ever
        // end that way and all of them ran to the half-hour cap; it threw the corner walk back to the middle,
        // so the square the company was sent to walk was never actually walked; and it said "the company has
        // reached the square" 3007 times, which is how it was found.
        var away = _standing ? BotPeril.Side : BotPeril.Side / 2;

        // Runes: while the company goes through a mage's gate the passage has the captain. See BotPassage.
        if (_passage != null)
        {
            if (_passage.Lead(body, out var through))
            {
                return through;
            }

            _passage = null;
        }

        if (!body.InRange(_square, away))
        {
            _standing = false;

            return March(squad, body, now);
        }

        if (!_standing)
        {
            _standing = true;
            _reached = true;
            _stoodTick = now;
            squad.OnTheMarch = false;

            _steppedTick = now;
            _round = 0;
            _post = Post(_round);

            logger.Information(
                "{Name}'s company has reached the square at ({X}, {Y}) and is walking it",
                body.Name,
                _square.X,
                _square.Y
            );
        }

        if (now - _stoodTick >= HoldMs && BotPeril.Reading(_map, _square) < BotPeril.Worrying)
        {
            return Finish(squad, $"({_square.X}, {_square.Y}) has gone quiet");
        }

        // The captain fights with the company while it is on something — see BotHarrow, where the same two hands on
        // one journey had the Baron walking the corners while his levy fought. Patrick's order of 16.09.2026.
        if (fighting && squad.Focus is { Deleted: false, Alive: true } focus)
        {
            // A new focus is a new stage; the will's labour clock is told so. See BotHarrow for the reason.
            if (!ReferenceEquals(focus, _focus))
            {
                _focus = focus;

                if (member is IBotWilful wilful && wilful.Resolve != null)
                {
                    wilful.Resolve.StirredTick = now;
                }
            }

            return BotDoing.Work($"fighting {focus.Name} with the company on ({_square.X}, {_square.Y}), {_fights} fights so far");
        }

        // <b>It walks the square rather than standing in the middle of it, and that is two things at once.</b>
        //
        // The first is the order as given: a patrol patrols. A company parked on the centre tile covers
        // whatever wanders past the centre tile, and the squad's own scouting knots spread from wherever the
        // leader happens to be — so moving the leader moves the whole net across the ground.
        //
        // The second is a clock. <c>BotWill.LabourMs</c> fails any undertaking that has answered nothing but
        // <c>Work</c> for fifteen minutes, on the reasoning that such a thing is invisible and immortal —
        // and that reasoning is right, it was bought with three tailors frozen at their benches for two
        // hours. But this patrol's own cap is half an hour, so the two numbers could not both be obeyed: a
        // quiet patrol would have been failed at fifteen minutes, and a failure marks the ground with
        // caution, which would have taught the whole population to avoid the square the captain was sent to
        // make safe. Rather than tune one number against the other — two clocks on one shelf, which is the
        // defect this project keeps paying for — the work now does something a walk can be seen in.
        if (now - _steppedTick >= RoundMs || body.InRange(_post, 1))
        {
            _steppedTick = now;
            _bends = 0;
            _post = Post(++_round);
        }

        return BotDoing.Walk(
            _map,
            _post,
            BotArrival.Within(1),
            $"walking the square at ({_square.X}, {_square.Y}), {_fights} fights so far"
        );
    }

    /// <summary>
    /// The next place inside the square to walk to: the four corners and the middle, in turn.
    ///
    /// Corners rather than a random point, so that a watcher can tell a patrol from a wander and so that the
    /// whole square is actually covered rather than sampled. A third of a side out from the middle keeps the
    /// company inside the square it was sent to — a corner at half a side would put half the formation in the
    /// next square along.
    /// </summary>
    private Point3D Post(int round)
    {
        var reach = Math.Max(2, BotPeril.Side / 3);

        var (dx, dy) = (round % 5) switch
        {
            0 => (0, 0),
            1 => (-reach, -reach),
            2 => (reach, -reach),
            3 => (reach, reach),
            _ => (-reach, reach)
        };

        var x = _square.X + dx;
        var y = _square.Y + dy;

        // The corner's own ground, and not the middle's carried sideways eight tiles. A square is twenty-four
        // tiles across and a hillside crosses it: the middle at ten and the corner at thirty is ordinary
        // terrain, and a walk to a corner at the middle's height is a walk to nowhere — the same fault that
        // put (x, y, 0) on the whole map. See BotPeril.Worst.
        return BotStep.Settle(_map, x, y, out var z) ? new Point3D(x, y, z) : _square;
    }

    /// <summary>The patrol given up without the square swept: the charge put down, the reading left as it is. See <see cref="Patrolling"/>.</summary>
    private BotDoing Abandon(BotSquad squad, string why)
    {
        if (squad != null)
        {
            squad.Charged = false;
            squad.OnTheMarch = false;
        }

        return BotDoing.Failed($"{why} — {_fights} fights, {_called} of us");
    }

    private BotDoing Finish(BotSquad squad, string why)
    {
        // Knocked down rather than cleared, so that a square which is still full of trouble earns its way
        // back onto the board by hurting somebody rather than by never having been visited.
        BotPeril.Swept(_map, _square);

        if (squad != null)
        {
            squad.Charged = false;
            squad.OnTheMarch = false;
        }

        return BotDoing.Done($"{why} — {_fights} fights, {_called} of us");
    }

    /// <summary>
    /// The way to the next place in the square turned out not to exist.
    ///
    /// <para>
    /// <b>Somewhere else inside the same square, because the square is the errand and a corner is not.</b>
    /// A patrol that answered nothing here was given up whole the first time one of its five posts could not
    /// be walked to — and since the posts were being asked for at the middle's height, on any ground that is
    /// not flat that was the first one. Ten of eighteen patrols on the night of 25.08.2026 ended this way,
    /// on four squares between them, none of them swept.
    /// </para>
    ///
    /// <para>
    /// <b>On the road there is nowhere else to bend to, and that is a fact worth writing down.</b> The
    /// company has not arrived, so there is no square to try another corner of; the honest answer is to give
    /// up — and to say so on the map, or the captain is offered the identical unreachable square in the next
    /// beat and marches at it again. See <see cref="BotPeril.Baulked"/>.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (!_standing || ++_bends > MaxBends)
        {
            BotPeril.Baulked(_map, _square);

            return false;
        }

        _steppedTick = Core.TickCount;
        _post = Post(++_round);

        logger.Information(
            "{Name}'s company could not reach that corner of ({X}, {Y}) and is trying the next",
            bot?.Self?.Name,
            _square.X,
            _square.Y
        );

        return true;
    }

    /// <summary>
    /// The charge is given up whichever way the patrol ended, including the ways that never reach Finish.
    ///
    /// Left set, a company that lost its captain would stand in a field until the world was reloaded: the
    /// quiet clock is the only thing that ever disbands a squad nobody is fighting with, and the charge is
    /// precisely the thing that switches it off.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        // Runes: a gate half used is let go with the patrol. See BotPassage.
        _passage?.Abort("the patrol was let go");
        _passage = null;

        if (_squad != null)
        {
            _squad.Charged = false;
            _squad.OnTheMarch = false;
        }

        if (bot is IBotSquadMember member && member.Squad != null && ReferenceEquals(member.Squad, _squad))
        {
            BotSquads.Leave(member);
        }

        _squad = null;
    }
}
