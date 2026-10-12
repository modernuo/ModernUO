using System;
using Server.Mobiles;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Falling in with a company that is already fighting, rather than starting a fight of your own beside it.
///
/// <para>
/// <b>A company could only ever be joined at the moment it was formed, and after that it was closed.</b>
/// <c>BotBand</c> and <c>BotSweep</c> both sweep up whoever is standing nearby when they call the roll, and
/// neither ever looks again — so a bot that walked into a graveyard thirty seconds later found five of its
/// own people fighting a troll, four of them in a formation with room for a fifth, and had no way at all to
/// go and stand with them. What it did instead was pick its own quarry two tiles away and fight it alone,
/// which is the one thing a company exists to stop.
/// </para>
///
/// <para>
/// <b>It is an offer to the newcomer, not a summons from the company.</b> Written the other way round — the
/// squad reaching out and pulling people in — it would be the squad deciding what somebody else's afternoon
/// is for, and this shard has refused that shape since its first week. The bot weighs falling in against
/// everything else it could do, in the same auction, and a miner with a good vein is entitled to walk past.
/// </para>
/// </summary>
public sealed class BotEnlist : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEnlist));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "enlist";

    /// <summary>
    /// What falling in is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// A little under a muster's, because it is the same work with the walk already half done by somebody
    /// else — the fight is found, the company is formed, and what is being offered is a place in it. It has
    /// to be worth more than prowling an empty field or nobody would ever come, and less than a hunt already
    /// in hand or bots would abandon fights to join fights.
    /// </para>
    ///
    /// <para>
    /// <b>Twenty-four since 22.09.2026, and the reason it could come down is that the musters never needed it.</b>
    /// The alarm had named this trade for days: the auction read it at a quarter of seventy, 18/min, and it paid 2–4
    /// over some fifty outcomes. Measured before touching it (session 21:58 on 21.09): 19 calls, 19 marched, 108 of
    /// 108 called standing in the square — the levy fills its companies through the muster itself — while the auction
    /// took enlist 24 times in three hours, 14 of them failing "company had no room by the time it arrived". At 24
    /// (floor 6) the dial halved those walks and the musters still marched 18 of 18. What this number buys is the
    /// bot that would otherwise stand about; what it cost was walks to companies already full.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 24.0;

    public static double WorkMinutes { get; set; } = 3.0;

    /// <summary>
    /// How far a bot will go to fall in with a company.
    ///
    /// <para>
    /// <b>Thirty-two, down from forty (twenty-four sent nobody at all in the first ten minutes: 3,164 asked, 0 sent), and the twelve hours of 13.09.2026 are the reason: 4,787 sent to
    /// fall in, 64 arrived.</b> A company disbands eight seconds after its fight ends (<c>BotSquad.IdleCapMs</c>)
    /// and forty tiles is eight seconds of walking at the very best, so nearly every bot offered a place
    /// arrived at an empty field — 4,419 of them, "the company was gone before it got there", six a minute,
    /// a fifth of every failure on the shard. Reach and the idle clock are one decision, and they had been
    /// made on different days.
    /// </para>
    /// </summary>
    public static int Reach { get; set; } = 32;

    /// <summary>
    /// How much of the quarry has to be left for a company's fight to be worth walking to.
    ///
    /// Two fifths. A fight nearly won is a fight that will be over before anybody arrives, and a company
    /// of three or more finishing one off does not need a fourth. Under three it is offered whatever is
    /// left, because a company that small is the one that gets killed.
    /// </summary>
    public static double Left { get; set; } = 0.4;

    /// <summary>Companies passed over because their fight was nearly won already. See <see cref="Left"/>.</summary>
    public static long Ending { get; private set; }

    /// <summary>Counts one such.</summary>
    public static void Passed() => Ending++;

    /// <summary>Companies of one passed over: the errand would refuse them on arrival, so the offer refuses them first.</summary>
    public static long Lonely { get; private set; }

    public static void Alone() => Lonely++;

    /// <summary>Companies passed over for being led by somebody of a guild hostile to ours (at war, or below the war line either way).</summary>
    public static long Hostile { get; private set; }

    /// <summary>
    /// How many times its enemy's strength a company of the usual size or more must have before no more are offered a place in
    /// it. Three: above the call's own two (BotSquad.CallOdds), so a company a call left short is still topped up.
    /// </summary>
    public static double Odds { get; set; } = 3.0;

    /// <summary>Companies passed over for being strong enough already. See <see cref="Odds"/>.</summary>
    public static long Overmanned { get; private set; }

    public static void Manned() => Overmanned++;

    public static void Foe() => Hostile++;

    /// <summary>
    /// Companies passed over because striking what they are fighting would be a crime for this bot: a guild's war company
    /// fighting the enemy guild, to a bot of neither. See <see cref="BotSquad.Strike"/>.
    /// </summary>
    public static long Unlawful { get; private set; }

    public static void Crime() => Unlawful++;

    /// <summary>Companies passed over because their leader stood further off than <see cref="BotSquads.JoinReach"/>, which the door refuses.</summary>
    public static long Remote { get; private set; }

    public static void Far() => Remote++;

    /// <summary>
    /// <b>Companies passed over for fighting something they and this bot together cannot beat (03.10.2026).</b> The enlistment
    /// offered a place in any fight with room. Rare — six bots walked over to fall in during the hour of build 395 measured for
    /// it, against 95 deaths "doing enlist" of members the companies had called themselves — and asked by the same rule that
    /// withdraws the company (BotSquad.Beyond), so the offer and the company never disagree about one fight.
    /// </summary>
    public static long Doomed { get; private set; }

    public static void Hopeless() => Doomed++;

    public static void Forget()
    {
        Ending = 0;
        Lonely = 0;
        Hostile = 0;
        Overmanned = 0;
        Unlawful = 0;
        Remote = 0;
        Doomed = 0;
        Trailing = 0;
        Struck = 0;
        Unserved = 0;
        MoonFollowed = 0;
        BindKept = 0;
    }

    private readonly BotSquad _squad;

    private readonly Map _map;

    private readonly Point3D _where;

    private bool _joined;

    public BotEnlist(BotSquad squad, Map map, Point3D where) : this(squad, map, where, false)
    {
    }

    /// <summary>
    /// <paramref name="levied"/>: pressed on a bot the company has already taken in (a delve's or a sweep's levy), as opposed to a
    /// bot walking over to offer itself. A levied member's service ends when the company ends or lets it go.
    /// </summary>
    public BotEnlist(BotSquad squad, Map map, Point3D where, bool levied)
    {
        _squad = squad;
        _map = map;
        _where = where;
        _pressedIn = levied;
    }

    /// <summary>Levied members let go by their company (left behind on a march), which ended the service rather than failing it.</summary>
    public static long Released { get; private set; }

    public override string Kind => Trade;

    /// <summary>A company of ours already fighting, close by. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    /// <summary>
    /// A bot in a company stands where the company stands. The stall watch read five levied bots at a muster that
    /// waits its five minutes as stuck on 16.09.2026 — the leader's case of build 81 seen from the members' side —
    /// and the company's own quiet clock is what disbands one that really has stopped.
    /// </summary>
    public override bool Still => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override bool Alongside => true;

    /// <summary>The company is still being raised after a flight, or it is full and the first beat back says so. See <see cref="BotDeed.Resumes"/>.</summary>
    public override bool Resumes => true;

    /// <summary>
    /// Work put down for this is kept for as long as the company lasts (12.10.2026, w2). A company runs as long as its fight
    /// and its march — 2 000 of 10 181 enlistments on 11.10 outlasted the ten minutes of <c>BotWill.AsideCapMs</c>, 416 ran past
    /// thirty — and the bot called into it had no say in that. See <see cref="BotDeed.Keeps"/>.
    /// </summary>
    public override bool Keeps => true;

    /// <summary>
    /// <b>An enlistment is not bid away (29.09.2026).</b> On build 308 two parties of five stood at a forward muster and
    /// marched, and within two minutes "Lorandil dropped enlist: outbid by acquire at 152/min", "Kael dropped enlist:
    /// outbid by peddle", "Dunstan dropped enlist: outbid by hunt" — the enlistment is worth twenty-four a minute and
    /// the review sold it to the first errand that paid more once its hold ran out. A summons is kept until the company
    /// breaks up; a rescue or a flight still jumps it, which is the mutual aid the company exists for.
    /// </summary>
    public override bool Committed => true;

    /// <summary>
    /// Urgent, and for the same reason a rescue is: a fight that is happening now will not be happening in
    /// a minute, and a company that is one body short is short of it at this moment or not at all.
    /// </summary>
    public override bool Pressing(IBotWilful bot) => Standing();

    public override string Stage =>
        _joined || _pressedIn
            ? $"with company {_squad?.Id}, {_squad?.Count} of them"
            : $"falling in with company {_squad?.Id}, {_squad?.Count} of them";

    /// <summary>Whether the member was already in the company when the enlistment was pressed (a levy), so the stage reads right.</summary>
    private bool _pressedIn;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (bot is not IBotSquadMember member)
        {
            return BotDoing.Failed("not the sort of thing that joins companies");
        }

        // <b>A levied member's service ends with the company (29.09.2026).</b> Between 16:37 and 19:00, 35 enlistments ended
        // "the company was gone before it got there" and 22 "had no room by the time it arrived", nearly all of them members
        // a delve or a sweep had levied and then left behind on the march: released, the member walked back to rejoin a
        // company that had closed its ranks or broken up, and failed. Service to a company that is over, or that let the
        // member go, is finished service; only a bot walking over of its own accord can fail to arrive.
        if (!Standing())
        {
            if (_joined || _pressedIn)
            {
                // <b>Said for what it was (01.10.2026, build next).</b> A company that dissolved left this member's Squad empty
                // while the company object still counted its roster, so the line below read "company N went on without it" for
                // a company that was no more: 3495 of the 3658 enlistments finished on build 384. Service that never began is
                // kept out of the share (KeptOut).
                _letGo = !_served;

                if (_letGo)
                {
                    Unserved++;
                }

                return BotDoing.Done(_served ? "the company broke up" : $"company {_squad?.Id} broke up before it stood with it");
            }

            return BotDoing.Failed("the company was gone before it got there");
        }

        // Already in one, possibly this one. Either way there is nothing further to do here, and the fighting
        // itself is the squad's business from now on.
        if (member.Squad != null)
        {
            if (member.Squad == _squad)
            {
                _pressedIn = true;

                var doing = Holding(member);

                // Standing with the company, or in its fight, is service; walking to it is not yet. See KeptOut.
                if (doing.Kind == BotDoingKind.Work && !_served)
                {
                    var now = Core.TickCount;

                    if (_workTick != 0)
                    {
                        _with += Math.Min(now - _workTick, 1000);
                    }

                    _workTick = now;
                    _served = _inFight || _with >= ServeMs;
                }

                return doing;
            }

            return BotDoing.Done("fell in with another company on the way");
        }

        // Levied, and no longer in the company: it let this member go (left behind on a march). Not walked back to.
        if (_pressedIn || _joined)
        {
            Released++;
            _letGo = !_served;

            if (_letGo)
            {
                Unserved++;

                return BotDoing.Done($"company {_squad.Id} let it go before it stood with it");
            }

            return BotDoing.Done($"company {_squad.Id} went on without it");
        }

        var anchor = _squad.Anchor;

        if (!body.InRange(anchor, BotSquad.PressReach))
        {
            return BotDoing.Walk(_map, anchor, BotArrival.Within(BotSquad.PressReach - 1), $"falling in with company {_squad.Id}");
        }

        if (!BotSquads.Join(_squad, member))
        {
            // Filled up on the way, or moved to another facet. An honest ending: somebody else got there.
            return BotDoing.Failed($"company {_squad.Id} had no room by the time it arrived");
        }

        _joined = true;

        logger.Information("{Name} fell in with company {Id}, now {Count} strong", body.Name, _squad.Id, _squad.Count);

        return BotDoing.Work($"fell in with company {_squad.Id}");
    }

    /// <summary>
    /// In the company and staying in it.
    ///
    /// <para>
    /// <b>Work, and the fence around it is the squad's own life rather than a clock here.</b> A member is
    /// Bound, so its own auction is skipped and this undertaking is what stands between it and having
    /// nothing at all; it must therefore last exactly as long as the company does and not one beat longer.
    /// The squad disbands after eight quiet seconds, which ends this the same second.
    /// </para>
    /// </summary>
    private BotDoing Holding(IBotSquadMember member)
    {
        if (member.Squad == null)
        {
            return BotDoing.Done("the company broke up");
        }

        // Runes (29.09.2026): while the company goes through a Gate Travel, the passage says where a member stands. See BotPassage.
        if (BotPassage.Steer(member, _squad, out var steered))
        {
            return steered;
        }

        // <b>A member that has fallen back closes up on its own (29.09.2026).</b> The stations are handed out by
        // BotSquad.Station only when the anchor has moved a reform's distance or the company changed, so a member whose
        // walk to its station ended short stood where it ended: on build 299 Daeron's march to the Orc Caves held for
        // ninety seconds for Katrin and then for Dunstan, both twenty-seven tiles back and neither moving, and left
        // them. Beyond PressReach of the anchor and not walking, the member walks to the anchor itself.
        var self = member.Self;
        var anchor = _squad.Anchor;

        // <b>While the company forms up short of its enemy, the member comes to the rally, not to the enemy (06.10.2026,
        // company16).</b> The anchor is the rally (BotSquad.Mustering) and the company's stations are the line there; a member too
        // far for a station (BotSquad.StationReach) or standing still beyond the field is walked to it. Nobody walks on to the thing
        // alone: that is what the muster is for.
        if (_squad.Mustering && _squad.Focus is { Deleted: false, Alive: true } mustered && self is { Deleted: false, Alive: true }
            && anchor != Point3D.Zero)
        {
            _closeTries = 0;
            _stillUntil = 0;
            _inFight = true;

            if (self.InRange(anchor, BotSquad.FieldReach) || member.Journey is { Active: true })
            {
                return BotDoing.Work($"forming up with company {_squad.Id} against {mustered.Name}");
            }

            if (!ReferenceEquals(_toFight, mustered))
            {
                _toFight = mustered;
                ToTheFight++;
            }

            return BotDoing.Walk(_map, anchor, BotArrival.Within(CloseUp), $"to company {_squad.Id}'s muster against {mustered.Name}");
        }

        // <b>In a fight the member goes to the fight, not to the leader (30.09.2026, build 338).</b> In the fighting stance the
        // anchor is the creature (BotSquad.Anchor), and the close-up below walked every member standing further than CloseUp
        // from it to the leader's body — which is where a caster's or a healer's own place is not (seven tiles out), where an
        // archer shooting from eight is not, and, with a leader that marched on, away from the fight altogether: Norbert on
        // 336 "could not get one tile closer … (closing up on Durgan, company 261)" four times while the company fought
        // round him. Within the squad's own reach of the thing the company's places and its press are the formation (the
        // stranded are sent again by BotSquad.Station); further off, the member walks to the fight itself.
        if (_squad.Stance == BotSquadStance.Fighting && _squad.Focus is { Deleted: false, Alive: true } foe
            && self is { Deleted: false, Alive: true } && foe.Map == self.Map)
        {
            _closeTries = 0;
            _stillUntil = 0;

            // <b>A member on its way to bind one of ours, or winding the bandage, keeps that walk (01.10.2026, build next).</b> The
            // company sends a member with cloth to a dying member beyond its reach (BotSquad.Approach) by writing the walk into
            // the same slot this errand writes its own into; a patient that had run more than PressReach from the enemy put the
            // binder's target outside "going to the fight" below, and this walked the binder back to the enemy on its next beat
            // while its claim on the patient stood for eight seconds. 88 walks to bind on build 386 against 773 members under
            // half their health. The company's station already passes such a member over (BindExcused); so does this now.
            if (_squad.Binding(self) || BotMend.Winding(self))
            {
                _inFight = true;
                BindKept++;

                return BotDoing.Work($"binding one of company {_squad.Id}'s own in its fight with {foe.Name}");
            }

            // Already walking to a place in it — the company's own station round the thing — is going to the fight: the two
            // are not made to take turns at the bottom of the road.
            var going = member.Journey is { Active: true } road && road.Target != Point3D.Zero
                        && Math.Max(Math.Abs(road.Target.X - foe.X), Math.Abs(road.Target.Y - foe.Y)) <= BotSquad.PressReach;

            if (going || self.InRange(foe.Location, BotSquad.PressReach))
            {
                _inFight = true;

                return BotDoing.Work($"in company {_squad.Id}'s fight with {foe.Name}");
            }

            // <b>A fight this member's walker has just found no road to is not walked to again (06.10.2026, work15).</b> The walker
            // gives a chase up after ChasePlans roads that none reached (BotWalk.Chasing) and this errand sent the same walk again in
            // the next beat: Morgrim and Bardryn of company 94, 100 drops in 25 minutes after one skeleton north of Cove. The
            // company is told: with none of its members at the thing it lets the fight go as hopeless (BotSquad.Unreachable,
            // which shuns the creature), and otherwise this member, who cannot get there, is let go to its own work.
            if (foe is BaseCreature && BotWalk.Unreached(self, foe))
            {
                if (_squad.Unreachable(foe))
                {
                    return BotDoing.Work($"company {_squad.Id} let {foe.Name} go: no road any of us drew reached it");
                }

                UnreachedFight++;

                return BotDoing.Failed($"no road reached company {_squad.Id}'s fight with {foe.Name} at {foe.Location}");
            }

            if (!ReferenceEquals(_toFight, foe))
            {
                _toFight = foe;
                ToTheFight++;
            }

            return BotDoing.Walk(_map, foe, BotArrival.Within(BotSquad.PressReach - 2), $"to company {_squad.Id}'s fight with {foe.Name}");
        }

        // <b>And a member walking to a station the anchor has left behind re-aims (29.09.2026, build 311).</b> The leave-behind
        // line of 310 read, three times over, "in squad N, holding enlist, journey moving towards (x, y) (station)" with
        // the station forty to sixty tiles from the leader: the stations are handed out on the squad's own cadence and
        // a running leader outpaces them, so a member was always walking to where the leader had been. Beyond CloseUp
        // of the anchor, a member whose walk is not itself bound for the anchor walks to the anchor.
        if (self is { Deleted: false, Alive: true } && anchor != Point3D.Zero && self.InRange(anchor, CloseUp))
        {
            _closeTries = 0;
            _closeAt = Point3D.Zero;
        }

        if (self is { Deleted: false, Alive: true } && anchor != Point3D.Zero && !self.InRange(anchor, CloseUp))
        {
            // <b>Across a moongate from its leader, a member standing on the gate goes through (01.10.2026, build next).</b> The
            // leader of a march steps through a moongate on the road (BotDelve), and a member closing up on it from the near side
            // is walked to the gate's tile by its own route; standing there, it is put through to the side its leader is on.
            if (_squad.Leader?.Self is { Deleted: false, Alive: true } across
                && Math.Max(Math.Abs(self.X - across.X), Math.Abs(self.Y - across.Y)) > BotDelve.GateApart
                && BotGates.MoongateAt(self.Location, across.Location) is { } moon && BotGates.Travel(self, moon))
            {
                MoonFollowed++;
                member.Journey?.Finish();

                return BotDoing.Work($"through the moongate after company {_squad.Id}");
            }

            // And walks to that gate first when one near it shortens the way to its leader by more than the gate's own detour.
            if (_squad.Leader?.Self is { Deleted: false, Alive: true } beyond
                && Math.Max(Math.Abs(self.X - beyond.X), Math.Abs(self.Y - beyond.Y)) > BotDelve.GateApart
                && BotGates.MoongateNear(self.Location, beyond.Location, MoonReach) is { } near)
            {
                return BotDoing.Walk(_map, near.From, BotArrival.Exactly, $"to the moongate after company {_squad.Id}");
            }

            var journey = member.Journey;
            var bound = journey is { Moving: true } && journey.Target != Point3D.Zero
                && Math.Max(Math.Abs(journey.Target.X - anchor.X), Math.Abs(journey.Target.Y - anchor.Y)) <= CloseUp;

            if (!bound)
            {
                // <b>A close-up that never closes is not repeated for ever (29.09.2026, build 332).</b> Miriel and Liftrasa asked
                // the walk to Aelith of company 45 every six seconds for twenty minutes on 330, each one "could not get one tile
                // closer … in 16 plans": the captain stood where the ground would not let them nearer, and nothing counted the
                // tries. The nearest the member has come is kept; eight tries without beating it, and a member within
                // HoldNear of the anchor stands where it is for HoldStillMs — near enough to be the company's — while one
                // further off is let go, honestly failed.
                var off = Math.Max(Math.Abs(self.X - anchor.X), Math.Abs(self.Y - anchor.Y));

                if (Core.TickCount - _stillUntil < 0)
                {
                    return BotDoing.Work($"with company {_squad.Id}, {off} tiles off and no nearer to be had");
                }

                // <b>A close-up the walker has just given up on is an answer, not a try to repeat (30.09.2026, build 345).</b>
                // The valve below counts tries by the clock, and a fight on the march resets it, so a member whose walk ended
                // "could not get one tile closer" asked the same walk again on its next beat: on 344 the close-ups of four
                // companies burned more of the walker's search ceiling than all the fighting ("closing up on Maud, company 61
                // 1584 … Quillathe, company 17 1486"), 342 legs given up as hopeless — Quillathe's members aimed at the same
                // few tiles by (1510, 1330) again and again (`night-r22-health.md`). Within HoldNear the member stands with its
                // company for HoldStillMs at once; further off the count goes on as before.
                if (member is BotMobile { LastWalk: BotWalkResult.GaveUp } && off <= HoldNear)
                {
                    GaveUpNear++;
                    HeldNear++;
                    _closeTries = 0;
                    _stillUntil = Core.TickCount + HoldStillMs;

                    return BotDoing.Work($"with company {_squad.Id}, {off} tiles off and no nearer to be had");
                }

                // <b>Counted walk by walk, not against the best distance (build 336).</b> The anchor moves, so "nearer than
                // ever" was met by the captain turning back, and the count reset: on 334 Oriel and Joran followed Liriel's
                // company 29 for eight minutes and the close-ups burned the path budget ("closing up on Liriel, company 29
                // 696") while the valve almost never tripped (`night-r8-paths.md`). A close-up is asked again only when the
                // last one ended short; the count starts again only when the member stands within CloseUp.
                // At most one try every CloseTryMs, so a walk still under way is not counted beat by beat.
                //
                // <b>And never while the leader is itself walking (01.10.2026, build 382).</b> The count was kept by the clock,
                // and a member following a leader on the move is beyond CloseUp of it on nearly every beat: eight ticks of the
                // clock, forty seconds, and a member more than HoldNear back was let go — "could not close on company 2: 231
                // tiles off after 8 walks that came no nearer" (Aelith, 23:58:53 on 381), 27 such in the first fifteen minutes
                // of 381 over seven companies, every one a member build 381 had just pressed to the enlistment (BotSquads.Enrol,
                // the delve's levy on the call, the harrowing's since 380) and thrown out again while its leader walked to the
                // muster or the ground. A walk that gains no ground on a receding leader says nothing about the ground between;
                // a try is a walk that came no nearer to a leader that stood. Followed instead, and counted.
                var leadWalking = _squad.Leader?.Journey is { Moving: true };

                if (leadWalking)
                {
                    Trailing++;
                }
                else if (Core.TickCount - _closeTick >= CloseTryMs || _closeTries == 0)
                {
                    _closeTick = Core.TickCount;
                    _closeTries++;
                }

                if (!leadWalking && _closeTries >= CloseTries)
                {
                    _closeTries = 0;

                    if (off <= HoldNear)
                    {
                        HeldNear++;
                        _stillUntil = Core.TickCount + HoldStillMs;

                        return BotDoing.Work($"with company {_squad.Id}, {off} tiles off and no nearer to be had");
                    }

                    Unclosed++;
                    BotSquads.Leave(member);

                    return BotDoing.Failed($"could not close on company {_squad.Id}: {off} tiles off after {CloseTries} walks that came no nearer");
                }

                ClosedUp++;

                // The leader's body is followed, not the point it stood on: a walk to a point is re-planned at every beat
                // the leader moves, and on build 311 a member closing up on a running captain was still sixty tiles
                // back after forty-eight seconds. Following a mobile is the walker's own trade (BotDoing.Walk(Mobile)).
                var lead = _squad.Leader?.Self;

                // <b>To where the leader stands, kept until it has moved on, not after the moving body (30.09.2026, build 346).</b>
                // A walk after a body that moves as fast as the walker is never "closer", so every plan of it asked the walker's
                // whole ceiling and after twelve it was dropped as hopeless, and asked again: Daeron "could not get one tile closer
                // … (closing up on Quillathe, company 16)" every four seconds on 345 while Quillathe marched from (1423, 801) to
                // (1462, 840), and the close-ups of two companies burned 2514 of the ceiling in fifteen minutes against 58 for
                // every fight. A point is one plan, drawn fresh; it is renewed only when the leader has gone CloseUp past it, so
                // the order stays the same from beat to beat (a changed order is a new plan — see BotWill.Advance's Matches).
                var arrive = CloseUp > 2 ? CloseUp - 2 : 1;

                if (lead is { Deleted: false, Alive: true } && lead.Map == _map && !ReferenceEquals(lead, self))
                {
                    // <b>A close-up that has arrived and is still not close is aimed again (30.09.2026, build 359).</b> The point
                    // above is renewed only when the leader has gone CloseUp (6) past it, and the walk to it ends CloseUp − 2 (4)
                    // short: a member standing where its walk ended can be ten tiles from a leader that never went six past the
                    // point. A marching leader holds for a member past MarchSpread (8), and stands; the member stands too, arrived,
                    // and neither moves until the hold runs out. From build 346, which brought the point in, to 357 every one of the
                    // 176 members a march left behind (50 on delves, 126 on sweeps) was left so, nine or ten tiles back after the
                    // whole 90 s hold — "leaves Elenwe behind on the march to the Orc Caves: 9 tiles back after 90s — … journey
                    // active, not moving towards (1164, 1858, 0) (closing up on Naevys, company 16)" — and every delve of 355–356 that
                    // marched was turned back by it ("nobody is left who could hold a line", "2 left of the 3 that set out once
                    // Rhiannel was left behind"). On 29.09 the members left were 23 to 119 tiles back. A member at its point and still
                    // beyond CloseUp of the anchor walks to where the leader stands now: one new order per arrival, not one per beat.
                    var stale = _closeAt != Point3D.Zero && self.InRange(_closeAt, arrive);

                    if (_closeAt == Point3D.Zero || !lead.InRange(_closeAt, CloseUp) || stale)
                    {
                        if (stale && lead.InRange(_closeAt, CloseUp))
                        {
                            Reaimed++;
                        }

                        _closeAt = lead.Location;
                        Repointed++;
                    }

                    return BotDoing.Walk(_map, _closeAt, BotArrival.Within(arrive), $"closing up on {lead.Name}, company {_squad.Id}");
                }

                return BotDoing.Walk(_map, anchor, BotArrival.Within(arrive), $"closing up on company {_squad.Id}");
            }
        }

        return BotDoing.Work($"with company {_squad.Id}, {_squad.Count} of us");
    }

    /// <summary>Walks a member took on its own to close up on the company. See <see cref="Holding"/>.</summary>
    public static long ClosedUp { get; private set; }

    /// <summary>Walks a member out of the squad's reach of its fight took to the fight rather than to the leader. See <see cref="Holding"/>.</summary>
    public static long ToTheFight { get; private set; }

    /// <summary>Beats a member on its way to bind one of ours, or winding the bandage, was left to it rather than walked to the fight. See <see cref="Holding"/>.</summary>
    public static long BindKept { get; private set; }

    /// <summary>The fight this member last walked to, so a walk is counted once and not every beat.</summary>
    private Mobile _toFight;

    /// <summary>Close-up walks in a row that came no nearer before the member stops asking. See the note in <see cref="Holding"/>.</summary>
    public static int CloseTries { get; set; } = 8;

    /// <summary>Within this of the anchor a member that cannot close stands where it is; further off it is let go.</summary>
    public static int HoldNear { get; set; } = 24;

    /// <summary>How long a member that cannot close stands before it tries again.</summary>
    public static int HoldStillMs { get; set; } = 30000;

    /// <summary>Members that stood near their company for want of a nearer tile, and members let go for want of any.</summary>
    public static long HeldNear { get; private set; }

    /// <summary>Of those, members held at once because the walker had just given the close-up up. See <see cref="Holding"/>.</summary>
    public static long GaveUpNear { get; private set; }

    /// <summary>Where this member is closing up to: the leader's place when the walk was set, kept until the leader moves on.</summary>
    private Point3D _closeAt;

    /// <summary>Close-up points set afresh because the leader had moved past the last one. See <see cref="Holding"/>.</summary>
    public static long Repointed { get; private set; }

    /// <summary>
    /// Of those, points set afresh because the member stood at its point still beyond <see cref="CloseUp"/> of a leader that
    /// had not moved past it (30.09.2026, build 359). See <see cref="Holding"/>.
    /// </summary>
    public static long Reaimed { get; private set; }

    public static long Unclosed { get; private set; }

    /// <summary>Close-up beats on which no try was counted because the leader was itself walking (build 382). See <see cref="Holding"/>.</summary>
    public static long Trailing { get; private set; }

    private long _closeTick;

    /// <summary>How often a close-up that has not closed counts as another try. See <see cref="CloseTries"/>.</summary>
    public static int CloseTryMs { get; set; } = 5000;

    private int _closeTries;

    private long _stillUntil;

    /// <summary>
    /// How far from the anchor a holding member may stand before it walks in on its own. Six: under the leader's
    /// <c>MarchSpread</c> of eight, so the company closes up before the leader has to stop for it.
    /// </summary>
    public static int CloseUp { get; set; } = 6;

    /// <summary>
    /// <b>An enlistment that ends takes the member out of its company (01.10.2026, build next).</b> A member whose enlistment
    /// failed ("no way through", a walk that could not close) or was dropped stayed on the roster with nothing in hand, and the
    /// company waited for it: at 10:23:32 on build 384 Thrain held the march into the Orc Caves ninety seconds for Lorandil, "9
    /// tiles back … holding nothing (), journey idle", two minutes after Lorandil's enlistment had failed "no way through to
    /// (5147, 1961, 0)". Put down (a flight, a bandage) is not ended, and Drop is not called for it. A member already in another
    /// company, or one the company has let go, is left alone.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        if (bot is IBotSquadMember member && member.Squad != null && ReferenceEquals(member.Squad, _squad)
            && !ReferenceEquals(_squad.Leader, member) && !_squad.Disbanded)
        {
            BotSquads.Leave(member);
            Struck++;
        }
    }

    /// <summary>How far from a member a moongate is looked for when its leader is across one. See <see cref="Holding"/>.</summary>
    public static int MoonReach { get; set; } = 120;

    /// <summary>Members put through a moongate after a leader on its far side. See <see cref="Holding"/>.</summary>
    public static long MoonFollowed { get; private set; }

    /// <summary>Members let go because their walker found no road to their company's fight while others of it were at it. See <see cref="Holding"/>.</summary>
    public static long UnreachedFight { get; private set; }

    /// <summary>Members struck off their company's roster when their enlistment ended. See <see cref="Drop"/>.</summary>
    public static long Struck { get; private set; }

    /// <summary>Levied members let go, or whose company broke up, before they ever stood with it. See <see cref="KeptOut"/>.</summary>
    public static long Unserved { get; private set; }

    /// <summary>How long a levied member stands with its company before its enlistment counts as service. Fifteen seconds.</summary>
    public static int ServeMs { get; set; } = 15000;

    /// <summary>Kept out of the share: a levied member let go before it served. See <see cref="BotDeed.KeptOut"/>.</summary>
    public override bool KeptOut => _letGo;

    private bool _served;

    private bool _inFight;

    private bool _letGo;

    private long _workTick;

    private long _with;

    /// <summary>Whether the company is still a company worth walking to.</summary>
    private bool Standing() =>
        _squad is { Count: >= 2, Disbanded: false } && _squad.Map == _map && _squad.Leader?.Self is { Deleted: false, Alive: true };
}

/// <summary>
/// Offers a free bot a place in whatever company is fighting within sight of it.
///
/// <para>
/// <b>Only companies that are actually in a fight, and only ones with room.</b> A company marching or
/// scouting has no need of a fifth body and would only be slowed by one arriving from behind; a company that
/// is <em>Fighting</em> is short-handed by definition, because that is what being in a fight means. And a
/// full one is not offered at all, so nobody ever walks forty tiles to be turned away.
/// </para>
/// </summary>
public sealed class BotEnlister : IBotProposer
{
    public string Name => "Enlist";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    public static long None { get; private set; }

    /// <summary>Companies passed over because there is no way through to where they are fighting.</summary>
    public static long Walled { get; private set; }

    public static long Sent { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || !BotSquads.Running)
        {
            return null;
        }

        Asked++;

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

        // A thief of The Shadow marches with nobody: a company is the island's business, and standing in one is how a
        // wanted bot is recognised. The championship is the exception Patrick allowed, and that is a duel, not a company.
        if (BotUnderworld.Member(body) && !BotFence.Is(body))
        {
            Unfit++;

            return null;
        }

        // Nor a trader: neutral in wars, it falls in with no company (01.10.2026, build 385). Asked here so the walk to a company
        // that would refuse it at the door (BotSquads.Join) is never offered. See BotTrading.Neutral.
        if (BotTrading.Passes(body))
        {
            return null;
        }

        var squad = Nearest(body, map);

        if (squad == null)
        {
            None++;

            return null;
        }

        Sent++;

        return new BotEnlist(squad, map, squad.Anchor);
    }

    /// <summary>The nearest company that is in a fight, has room, and is on this bot's own facet.</summary>
    private static BotSquad Nearest(Mobile body, Map map)
    {
        var squads = BotSquads.All;

        BotSquad best = null;
        var bestAway = double.MaxValue;

        for (var i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];

            if (squad.Stance != BotSquadStance.Fighting || squad.Count >= squad.Ceiling || squad.Map != map)
            {
                continue;
            }

            // <b>The same bar the errand holds it to, or the offer is a loop.</b> BotEnlist.Standing refuses
            // a company of one, and this offered one: Fenna was handed company 1, "1 of them", 2,966 times in
            // ten minutes on 13.09.2026, each errand failing "the company was gone before it got there" in the
            // same second it was taken. A proposer and its deed asking two different questions is an offer
            // that is not an errand.
            if (squad.Count < 2)
            {
                BotEnlist.Alone();

                continue;
            }

            var anchor = squad.Anchor;

            if (anchor == Point3D.Zero || !Utility.InRange(body.Location, anchor, BotEnlist.Reach))
            {
                continue;
            }

            // <b>The two door questions below are asked only of a fight within reach, so that what they count is a
            // company this bot could have walked to.</b> Asked before the distance, on build 49 the second one read 33,016
            // in forty-five minutes of peace: every fighting company on the island counted once for every free bot far
            // from its leader, a number about the loop and nothing about the shard. The first counted the same way.

            // Not a hostile guild's company (at war, or thought of below the war line: 01.10.2026, as the door asks). Join would
            // refuse it anyway; asked here so the walk is not made.
            if (squad.Leader?.Self is Mobile lead && BotRegard.Hostile(body, lead))
            {
                BotEnlist.Foe();

                continue;
            }

            // <b>Nor a fight that is not ours to fight.</b> The Crown declared war on The Blade at 07:00:58 on 24.09.2026, and
            // its war company took in Quenna of The Needle at 07:04:44, then Torvin, Faron and Lysa Ashdown: the offer asked
            // only whether the company was the enemy's. Its focus was Vesna and Cassia of The Blade — enemies to The Crown,
            // innocents to everybody else — so each of them was a criminal within half a minute of falling in ("the engine
            // counts helping it a crime"), and Quenna died by the guards' hand in Britain at 07:08:56. Asked of the engine:
            // whatever the company is fighting, a blow at it has to be lawful for this bot. See BotSquad.Strike.
            if (squad.Focus is { Deleted: false } fighting && body.IsHarmfulCriminal(fighting))
            {
                BotEnlist.Crime();

                continue;
            }

            // <b>And near enough its leader for the door to let it in, which the door has asked since build 43 and this
            // did not.</b> A company's anchor is whoever it is fighting, and in a war that can stand beside a bot while the
            // leader is across the island. On build 48, in the war declared at 04:41 on 15.09.2026, 35 enlistments failed
            // "had no room by the time it arrived", 29 of them in the minute 04:55 and 28 into company 53, The Hammer's war
            // company - Doran Ashdown ten times, Neriah, Piers and Ulwin six each - while the door counted a bot too far
            // from its leader every five seconds. The same predicate the door asks. See BotSquads.Reaches.
            if (!BotSquads.Reaches(squad, body))
            {
                BotEnlist.Far();

                continue;
            }

            // A fight that will be over before anybody arrives is not a place in a company, it is a walk to
            // an empty field. See BotEnlist.Left.
            if (squad.Count >= 3 && squad.Focus is { Deleted: false, HitsMax: > 0 } focus &&
                focus.Hits < focus.HitsMax * BotEnlist.Left)
            {
                BotEnlist.Passed();

                continue;
            }

            // <b>Nor one already strong enough for what it is fighting (01.10.2026).</b> With the ceiling at twenty (BotSquad.MaxSize)
            // every free bot within reach of a company of five fighting an orc would otherwise walk over to make it six, seven,
            // twenty — bots off their own work for a fight that did not need them. Past the usual five a company is offered only
            // while its strength is short of the enlistment's odds against its enemy. See BotEnlist.Odds.
            if (squad.Count >= BotSquad.Usual && squad.Focus is { Deleted: false } against
                && Strength(squad) >= BotThreat.Power(against) * BotEnlist.Odds)
            {
                BotEnlist.Manned();

                continue;
            }

            // <b>Nor a fight the company would withdraw from with this bot in it (03.10.2026).</b> See BotEnlist.Doomed. A caster's is
            // a fight the company stays in to the end (Patrick, 30.09), so help is still sent to it (review of march10, F1). Judged at
            // the withdrawal's odds, not the choice's (company16): the company is in this fight already, and a joiner is help.
            if (squad.Focus is { Deleted: false } doomed && !BotSquad.Caster(doomed)
                && (squad.Beyond(doomed, BotThreat.Power(body), BotSquad.WithdrawOdds) || BotSquad.Shuns(doomed, squad.Able() + BotThreat.Power(body))))
            {
                BotEnlist.Hopeless();

                continue;
            }

            // <b>Near is not the same as reachable, and a company fighting on a roof is both.</b> A fight
            // anchors wherever the fight is; this picked the nearest one by the crow's flight, so a company
            // dealing with something one storey up was the best offer going for everybody underneath it. The
            // reach ledger already knew — 33 enlist errands ended "no way through to (1361, 1483, 30)" in
            // ninety minutes on 03.09.2026, one tile from a pocket of 63 filed at (1362, 1482, 30) — and it
            // was being asked by the walker after the errand had been taken instead of by the chooser before.
            // Nor a fight this bot's walker has just given up a chase to (06.10.2026, work15). See BotWalk.Unreached.
            if (squad.Focus is BaseCreature unreached && BotWalk.Unreached(body, unreached))
            {
                Walled++;

                continue;
            }

            if (BotReach.Ask(map, body.Location, anchor, BotArrival.Within(BotEnlist.Reach)) == BotReachVerdict.Sealed)
            {
                Walled++;

                continue;
            }

            var away = body.GetDistanceToSqrt(anchor);

            if (away < bestAway)
            {
                bestAway = away;
                best = squad;
            }
        }

        return best;
    }

    /// <summary>The summed strength of a company's living members able to fight. See <see cref="Nearest"/>.</summary>
    private static double Strength(BotSquad squad)
    {
        var members = squad.Members;
        var sum = 0.0;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i] is IBotAlly { AbleToFight: true } && members[i].Self is { Deleted: false, Alive: true } self)
            {
                sum += BotThreat.Power(self);
            }
        }

        return sum;
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been offered a place in a company"
            : $"{Asked} asked: {Sent} sent to fall in, {Held} were already in a company, {Unfit} were too hurt to be any help, {None} had no company fighting within {BotEnlist.Reach} tiles with room in it, {Walled} passed one over for having no way through to it, {BotEnlist.Ending} passed one over whose fight was nearly won, {BotEnlist.Lonely} passed over a company of one, {BotEnlist.Hostile} passed over a hostile guild's, {BotEnlist.Overmanned} passed over one already {BotEnlist.Odds:F0} times its enemy's strength, {BotEnlist.Unlawful} passed over one whose fight would have made it a criminal, {BotEnlist.Remote} passed over one whose leader stood more than {BotSquads.JoinReach} tiles off, {BotEnlist.Doomed} passed over one fighting something reckoned past ×{BotSquad.WithdrawOdds:F1} it and this bot together; in the companies, {BotEnlist.ClosedUp} walks to close up ({BotEnlist.Repointed} points set afresh as the leader moved on, {BotEnlist.Reaimed} of them because the member stood at its point still more than {BotEnlist.CloseUp} tiles from the leader), {BotEnlist.ToTheFight} walks to the company's fight rather than to its leader ({BotEnlist.BindKept} beats a member binding one of ours was left to it instead), {BotEnlist.HeldNear} stood near their company for want of a nearer tile ({BotEnlist.GaveUpNear} at once, the walker having given the close-up up) and {BotEnlist.Unclosed} let go for want of any ({BotEnlist.Trailing} close-up beats behind a leader that was itself walking, no try counted), {BotEnlist.Released} levied members released when their company went on, {BotEnlist.Unserved} levied members let go or whose company broke up before they had stood with it {BotEnlist.ServeMs / 1000}s (kept out of the share), {BotEnlist.Struck} struck off their company's roster when their enlistment ended, {BotEnlist.UnreachedFight} let go for finding no road to their company's fight ({BotSquad.UnreachedLet} fights let go by a company none of whose members could reach)";

    public static void Forget()
    {
        Asked = 0;
        Walled = 0;
        Held = 0;
        Unfit = 0;
        None = 0;
        Sent = 0;
        BotEnlist.Forget();
    }
}
