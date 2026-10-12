using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Calling a company together for something one bot cannot take, and seeing it through.
///
/// <para>
/// <b>The squad machinery has been complete and unused since it was written.</b> Formation, stations,
/// scouting, the share-out, yielding a tile to whoever outranks you on it — all of it works, and
/// <c>BotSquads.Form</c> had no caller at all. Its own note says so in as many words: joining will be an
/// undertaking once somebody proposes one. This is that undertaking, and what it proposes on is the one
/// fact that makes a company worth its cost — a creature the arithmetic says one bot must refuse and four
/// bots may have.
/// </para>
///
/// <para>
/// <b>It calls, and then it stops giving orders.</b> Everything after the moment the company agrees on a
/// target belongs to the squad's own beat: where each member stands, who is anchored on whom, who swings,
/// and how the corpse is divided. This undertaking holds on only so that the fight has an owner — somebody
/// whose ledger the takings land in, and who can say the word when it is over. It sends the bot nowhere,
/// which is the whole reason it is allowed to run alongside a squad at all; see
/// <see cref="BotDeed.Alongside"/>.
/// </para>
/// </summary>
public sealed class BotBand : BotDeed
{
    /// <summary>The ledger's key.</summary>
    public const string Trade = "band";

    /// <summary>
    /// What fighting something in company is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// Above a lone hunt's sixty, because the whole point of the thing is that it is bigger game: what a
    /// company can take is what a bot has been walking past all evening, and a creature that needs four
    /// bots carries a purse that reflects it. Not far above, though — the takings are divided, and a prior
    /// that promised each member a whole ogre would have the population forming companies for the arithmetic
    /// rather than for the ogre. The ledger settles it either way within a few fights, from what actually
    /// arrives in the pack.
    /// </para>
    /// </summary>
    // Lowered from 90 on 16.09.2026: the overstated alarm measured 2/min — the takings are divided and arrive in the
    // members' packs, and a band's own line reads empty. Patrick's decision, that evening.
    public static double Prior { get; set; } = 30.0;

    /// <summary>How long one of these is expected to take, calling and fighting together.</summary>
    public static double WorkMinutes { get; set; } = 4.0;

    private readonly BaseCreature _quarry;

    private readonly Map _map;

    private readonly Point3D _found;

    private BotSquad _squad;

    private int _called;

    private bool _engaged;

    /// <summary>The strength of the bot and its allies standing by when the band was called (BotThreat.OurPower).</summary>
    private readonly double _together;

    public BotBand(BaseCreature quarry, double together = 0.0)
    {
        _quarry = quarry;
        _map = quarry.Map;
        _found = quarry.Location;
        _together = together;
    }

    /// <summary>
    /// The company it was called with, until the squad stands: <c>BotMuster</c> called it because the bot and the allies
    /// by it together could take what one could not, and the auction weighs the ground against the same sum
    /// (<see cref="BotPeril.Overwhelms"/>).
    /// </summary>
    public override double Brings(Mobile body) => System.Math.Max(BotQuad.Strength(body), _together);

    public override string Kind => Trade;

    /// <summary>Goes where the fighting is on purpose. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    /// <summary>See <see cref="BotDeed.Resumes"/>.</summary>
    public override bool Resumes => true;

    /// <summary>What the band is on. See <see cref="BotDeed.Foe"/>.</summary>
    public override Mobile Foe => _quarry;

    /// <summary>
    /// Claims the quarry the moment the band is chosen, not when the company has been called.
    ///
    /// <para>
    /// <b>Eleven companies against one spectre.</b> At 02:03:05 on build 45, six seconds after the boot, Roderic, Ulric,
    /// Wulfric, Alaric, Faron, Joss, Merrick, Orin, Perri, Quill and Rowan each called a squad of five against the same
    /// spectre (4070) on the roof by Britain Graveyard: every bot chose in the same step, and the claim that makes
    /// <c>BotQuarry.Company</c> pass a creature over was taken in <see cref="Calling"/>, a beat after all of them had
    /// chosen. Taken here, the next bot asked already sees it held. See <see cref="BotDeed.Taken"/>.
    /// </para>
    /// </summary>
    public override void Taken(IBotWilful bot) => BotQuarry.Claim(bot?.Self, _quarry);

    public override Map Map => _map;

    /// <summary>Where it was found, so the ledger learns about the ground rather than about the chase.</summary>
    public override Point3D Where => _found;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>
    /// Nothing, and that is deliberate.
    ///
    /// The blows a member lands are the engine's and train whatever it swings, exactly as they would on any
    /// other fight — that credit is real and arrives without anybody claiming it here. Naming a skill on
    /// this as well would pay the caller twice for one fight and make calling companies a way of training.
    /// </summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Mostly coin: a share of what came off the corpse.</summary>
    public override double Coin => 1.0;

    public override int Made => 0;

    /// <summary>The one piece of work in the project that goes on while its bot is in a squad.</summary>
    public override bool Alongside => true;

    /// <summary>
    /// A company is worth calling now or not at all: the creature is standing there, and the bots who would
    /// come are standing there too. Half a minute later it is one bot's problem again.
    /// </summary>
    public override bool Pressing(IBotWilful bot)
    {
        var body = bot?.Self;

        return body != null && Standing() && body.InRange(_quarry.Location, BotMuster.Reach);
    }

    public override string Stage
    {
        get
        {
            if (!_engaged)
            {
                return _squad == null
                    ? $"calling a company against {_quarry?.Name ?? "something"}"
                    : $"bringing {_called} of us up to {_quarry?.Name ?? "something"}";
            }

            return $"{_called} of us on {_quarry?.Name ?? "something"}";
        }
    }

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

        if (!_engaged)
        {
            return _squad == null ? Calling(member, body) : Gathering(member, body);
        }

        return Fighting(member);
    }

    /// <summary>
    /// The whole of the calling, in one beat.
    ///
    /// <para>
    /// <b>It has to be one beat, and that is not tidiness.</b> The instant a squad exists its founder is on
    /// the <c>Bound</c> rung, where the squad owns the bot — so a calling spread over several beats would be
    /// frozen by the company it had half finished assembling. Everybody who is going to come is standing
    /// within sight already, which is the condition the proposer offered this on, so there is nothing to
    /// wait for.
    /// </para>
    /// </summary>
    private BotDoing Calling(IBotSquadMember member, Mobile body)
    {
        if (!Standing())
        {
            return BotDoing.Failed("it went before anybody could be called");
        }

        // It wandered off while the auction was being run. Chasing it would be a lone bot walking at
        // something it has already worked out it cannot take.
        if (!body.InRange(_quarry.Location, BotMuster.Reach))
        {
            return BotDoing.Failed($"{_quarry.Name} moved off before the company formed");
        }

        _squad = member.Squad ?? BotSquads.Form(member);

        if (_squad == null)
        {
            return BotDoing.Failed("could not call one together");
        }

        // <b>The nearest first, as many as the fight asks, and every one of them pressed to the enlistment (01.10.2026).</b> The
        // call used to take whoever the map handed out first, up to five, and only joined them: the member kept its own errand in
        // hand, set aside, and walked to the fight only if a station happened to be handed out. Now the nearest come first (they
        // are there soonest), a bot whose work the enlistment would throw away is left at it, and the call goes past the usual
        // five only while the company is short of twice the weight round the quarry (BotSquads.Enough) — up to twenty.
        _near.Clear();

        foreach (var mobile in _map.GetMobilesInRange<Mobile>(body.Location, BotMuster.Reach))
        {
            if (mobile == body || mobile is not IBotSquadMember { Squad: null })
            {
                continue;
            }

            // Only those who could actually take part. A bot on its last two hit points is not
            // reinforcement, and counting it is how the first version formed companies that disbanded in the
            // tick they were announced.
            if (mobile is not IBotAlly { AbleToFight: true })
            {
                continue;
            }

            _near.Add(mobile);
        }

        _near.Sort((a, b) => body.GetDistanceToSqrt(a).CompareTo(body.GetDistanceToSqrt(b)));

        var asked = Asked(body);
        var strength = BotQuad.Strength(body);

        for (var i = 0; i < _near.Count && !BotSquads.Enough(_squad, strength, asked); i++)
        {
            if (_near[i] is not IBotSquadMember other)
            {
                continue;
            }

            var past = _squad.Count >= BotSquad.Usual;

            if (BotSquads.Enrol(_squad, other, _map, _quarry.Location, $"called to {body.Name}'s company against {_quarry.Name}"))
            {
                strength += BotThreat.Power(_near[i]);

                if (past)
                {
                    BotSquads.Past();
                }
            }
        }

        _near.Clear();

        _called = _squad.Count;

        if (_called < 2)
        {
            // Nobody came. The squad would disband by itself on its own beat, but leaving the bot standing in
            // a company of one until then is a bot on the Bound rung for no reason at all.
            BotSquads.Leave(member);
            _squad = null;

            return BotDoing.Failed($"nobody was free to come at {_quarry.Name}");
        }

        // <b>A company without a medic levies the nearest free healer (01.10.2026, build 388; the Opus dossier of 386, row 5).</b>
        // 43 % of the companies levied on 386 held neither a healer nor a mage, and of 773 members fallen under half their health
        // in a company's fight 101 were helped; build 387 let the blades bind (BindSkill 20) and the helped share rose to 50 %,
        // the died-unhelped to 27 %. The one who binds and casts on the dying is levied here, after the fighters and before the
        // company is judged, so it is in the company from its first beat and walks up with it. See LevyMedic.
        if (LevyMedic(_squad, body, _map, _quarry.Location, $"called to {body.Name}'s company against {_quarry.Name}, as its medic"))
        {
            _called = _squad.Count;
        }

        // <b>The company that came is judged, not the crowd that was counted (01.10.2026, build next; the Sonnet analyst of 386:
        // "a number-promise against a number-wager").</b> The call was offered because everybody able within 120 tiles would
        // together outweigh the quarry (BotQuarry.Company, BotThreat.Tolerance); the levy takes whoever of them is free and not
        // at work the enlistment would cost it, and on 386 that was a median of three: companies of two or three put their
        // quarry down 52 % of the time and broke off "its health has not moved" a third, against 85 % at five or six; 52 bands
        // died, and ogres were taken 3 of 39 times, whipping vines 0 of 22, reapers 1 of 15. A company that does not clear the
        // same bar the crowd was weighed against does not engage: the quarry is left alone for ShunMs, so the same few do not
        // call the same short company again in the next beat, and the members are let go to their work.
        var brought = Brought(_squad);

        // <b>Nor a company that would withdraw from it on sight (03.10.2026, the re-review of build 398).</b> With the shun after a
        // withdrawal cut to two minutes, nothing stopped a band being raised at a creature its own company withdraws from — three
        // on 395, at whipping vines and a troll, which do not move — so its members walked up to 120 tiles to it and back again on
        // every raise. Judged here, before the walk, and the quarry left alone for as long as a hopeless break-off leaves it.
        var beyond = _squad.Beyond(_quarry);

        if (Rejudged && brought * BotThreat.Tolerance < asked || beyond)
        {
            Short++;
            BotQuarry.Shun(_quarry, beyond ? BotQuarry.HopelessMs : BotQuarry.ShunMs);

            // The whole company, not the leader alone (Fable's review of 387): unlike "nobody came" above, one to four members
            // were just levied into this squad. Leave would detach the leader and nothing promotes another, so the levied
            // members would stand Bound under a leader that had gone back to its work until the squad's own clock. Disbanded
            // here, their enlistments end as "company broke up" (Unserved, kept out of the share), the way BotRest and
            // BotScout release a company whose leader cannot go on.
            BotSquads.Disband(_squad, $"only {_called} came against {_quarry.Name}, short of the fight");
            _squad = null;

            // Said as what it was: the company's own price, or the bar it was asked against (06.10.2026, work15). A band refused
            // by Beyond printed "10377 of strength to the 5811 it asks", a passing sum on a failure.
            return BotDoing.Failed(
                beyond
                    ? $"only {_called} came against {_quarry.Name}: the company prices it at {BotSquad.Reckon(_quarry):F0}, past what {brought:F0} of strength can beat"
                    : $"only {_called} came against {_quarry.Name}: {brought:F0} of strength to the {asked:F0} it asks"
            );
        }

        // Nobody else goes for it while the company has it. The claim is the same one a lone hunter makes,
        // and it is renewed for as long as this undertaking is held.
        BotQuarry.Claim(body, _quarry);

        // <b>The company comes up to its quarry together, and engages when it is there (01.10.2026, build 388).</b> Until now the
        // company engaged in the beat it was levied: the members, pressed from up to BotMuster.Reach (120 tiles) off, walked to
        // their stations round the quarry one by one and the first blows came one by one — on 387 a band's first blow came a mean
        // of 25.1 s after the company engaged (44 fights; 79 of 101 past 10 s), against 8.9 s for a prowl's company, which walks
        // to its ground already gathered. A company that trickles in fights the quarry two at a time, which is how bands of
        // three to five died on 386. Now the leader walks up to EngageTiles of the quarry as a march (OnTheMarch: paced to its
        // members behind it, BotSquad.Lagging) and stands there until every member is within GatherTiles of it, or GatherMs is
        // up; the company engages then, with everybody in reach. Set upon on the way, it engages at once (BotSquads.Note). The
        // squad's own finder is stilled meanwhile (Quarry) and the charge keeps it from ageing (Charged); both are given back
        // at the engagement and at Drop.
        if (Gathers)
        {
            _gatherTick = Core.TickCount;
            _squad.Charged = true;
            _squad.OnTheMarch = true;
            _squad.Quarry = Nothing;

            return BotDoing.Work($"bringing {_called} of us up to {_quarry.Name}");
        }

        _squad.Engage(_quarry, member);
        Engaged(_squad);

        return BotDoing.Work($"{_called} of us on {_quarry.Name}");
    }

    /// <summary>
    /// <b>Walking up together, and engaging when there (01.10.2026, build 388).</b> See the note in <see cref="Calling"/>.
    /// </summary>
    private BotDoing Gathering(IBotSquadMember member, Mobile body)
    {
        var squad = member.Squad;

        if (squad == null || !ReferenceEquals(squad, _squad))
        {
            Unfinished++;

            return BotDoing.Failed($"the company broke up before it came up to {_quarry?.Name ?? "it"}");
        }

        if (!Standing())
        {
            WentBefore++;

            return BotDoing.Done($"{_quarry?.Name ?? "it"} went before {_called} of us came up to it");
        }

        // Set upon on the way: the company's fight is the band's fight, as in Fighting; the quarry waits under its claim.
        if (squad.Focus is { Deleted: false, Alive: true })
        {
            SetUpon++;
            Engaged(squad);

            return BotDoing.Work($"{_called} of us on {squad.Focus.Name}, {_quarry.Name} after");
        }

        if (!body.InRange(_quarry.Location, BotMuster.Reach))
        {
            MovedOff++;
            BotSquads.Disband(_squad, $"{_quarry.Name} moved off before the company came up to it");
            _squad = null;

            return BotDoing.Failed($"{_quarry.Name} moved off before the company came up to it");
        }

        if (!body.InRange(_quarry.Location, EngageTiles))
        {
            if (!_walkedUp)
            {
                _walkedUp = true;
                WalkedUp++;
            }

            return BotDoing.Walk(_map, _quarry, BotArrival.Within(EngageTiles), $"up to {_quarry.Name} with the company");
        }

        var alive = 0;
        var near = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (ReferenceEquals(members[i], member) || members[i]?.Self is not { Deleted: false, Alive: true } other)
            {
                continue;
            }

            alive++;

            if (other.Map == _map && other.InRange(body.Location, GatherTiles))
            {
                near++;
            }
        }

        if (near < alive && Core.TickCount - _gatherTick < GatherMs)
        {
            return BotDoing.Work($"waiting for {alive - near} of {_called} to come up to {_quarry.Name}");
        }

        if (near >= alive)
        {
            Gathered++;
        }
        else
        {
            GatheredLate++;

            // <b>On the clock with members behind, the company engages only if what came up is enough (06.10.2026, company16).</b>
            // The band was offered on everybody within 120 tiles and judged on everybody levied (Calling); what stands at the
            // quarry after thirty seconds is what fights it. Judged at the company's choice odds on the strength within
            // BotSquad.FieldReach of the leader; short, the quarry is left alone for ShunMs and the members let go, as a short levy is.
            var afield = squad.Afield(body.Location, BotSquad.FieldReach);
            var reckoned = BotSquad.Reckon(_quarry);

            if (reckoned > 0.0 && reckoned > afield * BotSquad.ChoiceOdds)
            {
                GatheredShort++;
                BotQuarry.Shun(_quarry, BotQuarry.ShunMs);
                BotSquads.Disband(_squad, $"only {afield:F0} of strength came up to {_quarry.Name} in {GatherMs / 1000}s");
                _squad = null;

                return BotDoing.Failed($"only {afield:F0} of strength came up to {_quarry.Name} in {GatherMs / 1000}s, which the company prices at {reckoned:F0}");
            }
        }

        squad.Engage(_quarry, member);
        Engaged(squad);

        return BotDoing.Work($"{_called} of us on {_quarry.Name}");
    }

    /// <summary>The company has its enemy: the band fights from here, and the squad is given back its own finder and its ageing.</summary>
    private void Engaged(BotSquad squad)
    {
        _killsAtCall = squad.Kills;
        _engaged = true;
        Release(squad);
    }

    /// <summary>Gives the squad back what the gathering took: the charge, the march and the stilled finder. See <see cref="Calling"/>.</summary>
    private static void Release(BotSquad squad)
    {
        if (squad == null)
        {
            return;
        }

        squad.Charged = false;
        squad.OnTheMarch = false;

        if (ReferenceEquals(squad.Quarry, Nothing))
        {
            squad.Quarry = null;
        }
    }

    /// <summary>The squad's own finder while the band's company gathers: nothing. See <see cref="Calling"/>.</summary>
    private static readonly Func<Mobile, BaseCreature> Nothing = _ => null;

    /// <summary>Whether a band's company walks up to its quarry and gathers before it engages. A switch. See <see cref="Calling"/>.</summary>
    public static bool Gathers { get; set; } = true;

    /// <summary>How near its quarry the leader brings the company before it engages. Ten: inside a bow's reach, outside a blow's.</summary>
    public static int EngageTiles { get; set; } = 10;

    /// <summary>Within this of the leader a member counts as come up. Twelve.</summary>
    public static int GatherTiles { get; set; } = 12;

    /// <summary>The longest the leader waits at its quarry for the members behind it before engaging with whoever is there. Thirty seconds.</summary>
    public static int GatherMs { get; set; } = 30000;

    private long _gatherTick;

    private bool _walkedUp;

    /// <summary>Bands whose leader walked up to its quarry before engaging; engaged with everybody come up; engaged on the clock with members behind; set upon on the way; whose quarry went or moved off before they came up.</summary>
    public static long WalkedUp { get; private set; }

    public static long Gathered { get; private set; }

    public static long GatheredLate { get; private set; }

    /// <summary>Bands let go at the gather's clock because the strength that came up to the quarry was short of it at the company's choice odds (06.10.2026, company16).</summary>
    public static long GatheredShort { get; private set; }

    public static long SetUpon { get; private set; }

    public static long WentBefore { get; private set; }

    public static long MovedOff { get; private set; }

    /// <summary>
    /// What the company is to meet: the quarry in full and whatever else menaces round it at <c>BotThreat.Secondary</c>, as the
    /// threat round a bot is reckoned. See <see cref="BotSquads.Enough"/>. Static since build 388, so the muster asks the same
    /// question before it offers the band (<c>BotMuster.Propose</c>).
    /// </summary>
    private double Asked(Mobile body) => Asked(_map, _quarry, body);

    public static double Asked(Map map, BaseCreature quarry, Mobile body)
    {
        if (map == null || quarry == null)
        {
            return 0.0;
        }

        var total = BotThreat.Power(quarry);

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(quarry.Location, AskedReach))
        {
            if (!ReferenceEquals(creature, quarry) && BotThreat.Menacing(body, creature))
            {
                total += BotThreat.Power(creature) * BotThreat.Secondary;
            }
        }

        return total;
    }

    /// <summary>How far round the leader a free healer is looked for when the company it levied has no medic. See <see cref="LevyMedic"/>.</summary>
    public static int MedicReach { get; set; } = 150;

    /// <summary>Whether a company with no medic levies one. A switch.</summary>
    public static bool Medics { get; set; } = true;

    /// <summary>Medics levied into a company that had none; companies that wanted one and found no free healer within reach.</summary>
    public static long MedicsLevied { get; private set; }

    public static long MedicsWanting { get; private set; }

    /// <summary>
    /// Levies the nearest free healer within <see cref="MedicReach"/> into a company that holds no medic. Returns whether one was
    /// taken. Asked by the band's call and the prowl's company (<c>BotProwl</c>). A medic is a member whose class role is
    /// <c>BotRole.Medic</c> (<c>BotFormation.RoleOf</c>); a mage is not one, since its heals wait on reagents (<c>BotSquad.Dry</c>).
    /// </summary>
    public static bool LevyMedic(BotSquad squad, Mobile body, Map map, Point3D where, string why)
    {
        if (!Medics || squad == null || body == null || map == null || squad.Count >= squad.Ceiling)
        {
            return false;
        }

        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (BotFormation.RoleOf(members[i]) == BotRole.Medic)
            {
                return false;
            }
        }

        IBotSquadMember medic = null;
        var nearest = double.MaxValue;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(body.Location, MedicReach))
        {
            if (mobile == body || mobile is not IBotSquadMember { Squad: null, Class.Role: BotRole.Medic } other
                || mobile is not IBotAlly { AbleToFight: true } || mobile is not { Deleted: false, Alive: true })
            {
                continue;
            }

            // Not a healer keeping its guild's open infirmary: the next nearest is levied (07.10.2026, life18b). See BotInfirmaries.Keeping.
            if (BotInfirmaries.Keeping(mobile))
            {
                continue;
            }

            var distance = body.GetDistanceToSqrt(mobile);

            if (distance < nearest)
            {
                nearest = distance;
                medic = other;
            }
        }

        if (medic != null && BotSquads.Enrol(squad, medic, map, where, why))
        {
            MedicsLevied++;

            return true;
        }

        MedicsWanting++;

        return false;
    }

    /// <summary>How far round its quarry a band weighs what else it will meet. See <see cref="Asked"/>.</summary>
    public static int AskedReach { get; set; } = 8;

    /// <summary>Whether the company that answered a call is judged against the fight before it engages. A switch. See <see cref="Calling"/>.</summary>
    public static bool Rejudged { get; set; } = true;

    /// <summary>Bands not engaged because the company that came fell short of the fight. See <see cref="Rejudged"/>.</summary>
    public static long Short { get; private set; }

    /// <summary>The strength of the company's living members, summed. See <see cref="Calling"/>.</summary>
    private static double Brought(BotSquad squad)
    {
        var sum = 0.0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is { Deleted: false, Alive: true } body)
            {
                sum += BotThreat.Power(body);
            }
        }

        return sum;
    }

    /// <summary>Scratch for the call: one game thread, so one list is enough.</summary>
    private static readonly List<Mobile> _near = [];

    /// <summary>What the company had put down when it was called, so the band counts its own. See <see cref="BotSquad.Kills"/>.</summary>
    private int _killsAtCall;

    /// <summary>
    /// Watching it happen, and seeing it through.
    ///
    /// <para>
    /// <b>The company's fight is the band's fight, however it turns (01.10.2026).</b> Until now the band ended "the company broke
    /// off" the moment the company's enemy was anything but the band's quarry — finished, at nought — and on 30.09 that was 49 of
    /// the 83 bands that finished: 25 of them because the company had turned to something else that was attacking it, 13 because
    /// it had given the quarry up as hopeless. The first is not an ending: the company is fighting what set upon it, and the
    /// quarry is still there for after. The second is not a finish. So the band holds while its company fights anything; when
    /// the company is free again and the quarry still stands within reach, the company is turned back to it; it finishes when
    /// the quarry is down, and fails, saying so, when the company gave the quarry up or it got away with nothing down.
    /// </para>
    /// </summary>
    private BotDoing Fighting(IBotSquadMember member)
    {
        var squad = member.Squad;
        var kills = _squad == null ? 0 : _squad.Kills - _killsAtCall;

        if (squad == null || !ReferenceEquals(squad, _squad))
        {
            // Disbanded under it, or the bot was taken out of it. What went down is counted; a company that came apart with
            // nothing down has not finished anything.
            if (kills > 0 || !Standing())
            {
                return BotDoing.Done($"the company broke up with {Down(kills)}");
            }

            Unfinished++;

            return BotDoing.Failed($"the company broke up round {_quarry?.Name ?? "it"} with nothing down");
        }

        if (!Standing())
        {
            return BotDoing.Done($"{_quarry?.Name ?? "it"} went down to {_called} of us{(kills > 1 ? $" ({kills} down in all)" : "")}");
        }

        if (ReferenceEquals(squad.Focus, _quarry))
        {
            BotQuarry.Claim(member.Self, _quarry);

            return BotDoing.Work($"{_called} of us on {_quarry.Name}");
        }

        // Given up on by the squad's own judgement — four minutes, health that will not move, never got near it.
        if (ReferenceEquals(squad.Broken, _quarry))
        {
            Unfinished++;

            return BotDoing.Failed($"the company broke off {_quarry.Name}: {squad.BrokenWhy}{(kills > 0 ? $" ({Down(kills)})" : "")}");
        }

        // <b>Or pulled back from, having lost two inside a minute (06.10.2026, company16).</b> The company stands; the band's quarry is
        // not fought again by it, so the band is over. The quarry is left alone for ShunMs so the muster does not raise another at once.
        if (squad.Bleeding(_quarry))
        {
            PulledBack++;
            BotQuarry.Shun(_quarry, BotQuarry.ShunMs);

            return BotDoing.Failed($"the company pulled back from {_quarry.Name} after losing {BotSquad.BleedDeaths} inside a minute{(kills > 0 ? $" ({Down(kills)})" : "")}");
        }

        // The company is fighting something else that set upon it. The quarry waits; the claim holds it for us.
        if (squad.Focus is { Deleted: false, Alive: true } other)
        {
            if (!ReferenceEquals(_aside, other))
            {
                _aside = other;
                Turned++;
            }

            BotQuarry.Claim(member.Self, _quarry);

            return BotDoing.Work($"{_called} of us on {other.Name}, {_quarry.Name} after");
        }

        // Free again, and the quarry still stands within reach: back to it. A chosen fight: the company musters short of it when
        // its strength is spread (company16, BotSquad.Engage).
        if (member.Self is { } body && body.InRange(_quarry.Location, BotMuster.Reach) && !BotQuarry.Shunned(_quarry))
        {
            Returned++;
            squad.Engage(_quarry, member, true);
            BotQuarry.Claim(body, _quarry);

            return BotDoing.Work($"{_called} of us back on {_quarry.Name}");
        }

        if (kills > 0)
        {
            return BotDoing.Done($"{Down(kills)}; {_quarry.Name} got away");
        }

        Unfinished++;

        return BotDoing.Failed($"{_quarry.Name} got away with nothing down");
    }

    private static string Down(int kills) => kills == 1 ? "1 down" : $"{kills} down";

    /// <summary>The last other enemy the company was seen fighting while the band held, so a turn is counted once.</summary>
    private Mobile _aside;

    /// <summary>
    /// Times a band's company turned to something else attacking it while the band held; times the band turned it back to its
    /// quarry; bands that ended without their quarry down and nothing else either. For the Companies line.
    /// </summary>
    public static long Turned { get; private set; }

    public static long Returned { get; private set; }

    public static long Unfinished { get; private set; }

    /// <summary>Bands ended because their company pulled back from the quarry after losing two inside a minute (company16). See <c>BotSquad.Bleed</c>.</summary>
    public static long PulledBack { get; private set; }

    public static string Describe() =>
        $"bands: {Turned} turns of a band's company to something else attacking it while the band held, {Returned} turned back to their quarry after, {Unfinished} ended with their quarry standing and nothing down, {PulledBack} ended because the company pulled back from the quarry after losing {BotSquad.BleedDeaths} inside a minute, {Short} not engaged because the company that came fell short of the fight ({(Rejudged ? "judged after the levy" : "off")}); "
        + $"{WalkedUp} companies walked up to their quarry before engaging ({(Gathers ? "on" : "off")}): {Gathered} engaged with everybody within {GatherTiles} tiles of the leader, {GatheredLate} on the {GatherMs / 1000}s clock with members still behind ({GatheredShort} of them let go for the strength come up being short at ×{BotSquad.ChoiceOdds:F1}), {SetUpon} set upon on the way, {WentBefore} found the quarry gone and {MovedOff} gone off past {BotMuster.Reach} tiles; "
        + $"{MedicsLevied} medics levied into a company that had none and {MedicsWanting} companies that found no free healer within {MedicReach} tiles";

    public static void Forget()
    {
        Turned = 0;
        Returned = 0;
        Unfinished = 0;
        Short = 0;
        WalkedUp = 0;
        Gathered = 0;
        GatheredLate = 0;
        GatheredShort = 0;
        PulledBack = 0;
        SetUpon = 0;
        WentBefore = 0;
        MovedOff = 0;
        MedicsLevied = 0;
        MedicsWanting = 0;
    }

    /// <summary>Over, however it ended. The claim goes back; the squad lives its own life from here, with what the gathering took given back.</summary>
    public override void Drop(IBotWilful bot)
    {
        BotQuarry.Release(_quarry);

        if (_squad is { Disbanded: false })
        {
            Release(_squad);
        }
    }

    /// <summary>Whether the quarry is still something to fight.</summary>
    private bool Standing() =>
        _quarry is { Deleted: false, Alive: true } && _quarry.Map == _map;
}
