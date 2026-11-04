using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Somebody of ours is being killed and has said so out loud.
///
/// <para>
/// <b>This is the one thing in the project that is genuinely a message.</b> Everything else a company does is
/// arithmetic every member repeats for itself — stations, patches, shares — precisely so that nothing has to
/// be sent, received or kept in step. A cry cannot be arithmetic: what makes it urgent is that it happened at
/// a moment, to one bot, and nobody else can derive it from where they are standing. The squad's own note
/// allows exactly two such events, and names this as one of them.
/// </para>
///
/// <para>
/// <b>It is a fact, not a summons.</b> Nothing here sends a bot anywhere. A cry is posted, it expires on its
/// own, and whoever is free enough to care picks it up through the ordinary auction as a piece of work worth
/// more than digging — see <see cref="BotRescuer"/>. That matters because the first version's help system was
/// an order: a bot posted a call, the call found nobody able, it disbanded in the same tick, and the bot
/// posted it again dozens of times over. An offer that nobody takes is silence; an order that nobody can obey
/// is a loop.
/// </para>
/// </summary>
public static class BotCry
{
    /// <summary>
    /// How long a cry is worth answering.
    ///
    /// Short. Help that arrives half a minute after a fight is help that arrives at a corpse, and a stale cry
    /// on the board sends bots across the map to stand in an empty field. Renewed every beat the bot is still
    /// in trouble, so a long fight keeps its cry alive without anybody repeating themselves.
    /// </summary>
    public static int HoldsMs { get; set; } = 20000;

    /// <summary>How far a cry carries. Wider than a company forms across: this is the whole point of it.</summary>
    public static int Carries { get; set; } = 60;

    /// <summary>Each crier's cry: about what, when last renewed, and whether the bout has turned dire (once dire, dire until it lapses).</summary>
    private static readonly Dictionary<Serial, (Mobile Who, Mobile What, long Tick, bool Dire)> _cries = [];

    /// <summary>Cries raised and cries answered. Both, because neither number means anything alone.</summary>
    public static long Raised { get; private set; }

    public static long Answered { get; private set; }

    /// <summary>
    /// Says that this bot is being set upon by that. Renewing an existing cry costs nothing and is expected.
    /// </summary>
    public static void Raise(Mobile who, Mobile what, bool dire = false)
    {
        if (who is not { Deleted: false, Alive: true } || what is not { Deleted: false, Alive: true })
        {
            return;
        }

        var stood = _cries.TryGetValue(who.Serial, out var old);

        if (!stood)
        {
            Raised++;
        }

        // Dire by the caller's word (refused its fight for the odds) or by its health; and a bout that has turned dire stays so
        // while the cry stands, for whoever reads it (BotRescuer passes over a steady crier's crowded thing, not a dire one's).
        dire = dire || who is BotMobile { HitsMax: > 0 } hurt && hurt.Hits < hurt.HitsMax * DireAt
                    || stood && old.Dire && Core.TickCount - old.Tick < HoldsMs;

        _cries[who.Serial] = (who, what, Core.TickCount, dire);

        // The stopwatch keeps the first cry of the bout: the stamp above is rewritten by every renewal. See BotReflex.
        BotReflex.Cried(who, what);

        if (!Summons || who is not BotMobile crier)
        {
            return;
        }

        // A cry from a bot that is losing, or has been refused its fight for the odds, is answered now. See Summon. A steady
        // cry — the crier whole and in a fight it judged it could win — has its nearest free neighbour sent (12.10.2026, w5). See
        // Nearby.
        if (dire)
        {
            Ask(crier, true);
        }
        else if (NearCalls)
        {
            Ask(crier, false);
        }
    }

    /// <summary>Whether this bot's standing cry is dire: raised under <see cref="DireAt"/> of its health or for a fight refused on the odds.</summary>
    public static bool IsDire(Mobile who) =>
        who != null && _cries.TryGetValue(who.Serial, out var cry) && cry.Dire && Core.TickCount - cry.Tick < HoldsMs;

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCry));

    /// <summary>Whether a dire cry sends the best placed of ours near it at once. A switch, for a running shard.</summary>
    public static bool Summons { get; set; } = true;

    /// <summary>
    /// The share of its health below which a crier is in real trouble, whatever it made of the odds: the line at which a bot
    /// is worth mending (<c>BotMend.Hurt</c>). A cry from a bot refused its fight for the odds is dire at any health.
    /// </summary>
    public static double DireAt { get; set; } = 0.7;

    /// <summary>The most of ours sent to one cry at one calling. Three: enough to turn most fights a lone bot is losing.</summary>
    public static int MostCalled { get; set; } = 3;

    /// <summary>How long after a calling a cry still dire may call again, and how many callings a bout of crying is given.</summary>
    public static int RecallMs { get; set; } = 5000;

    public static int MostRounds { get; set; } = 2;

    /// <summary>How long a bot that has been called to one cry is left alone by the next.</summary>
    public static int HelperRestMs { get; set; } = 45000;

    /// <summary>
    /// How near a dire crier a bot carrying another bot's goods (a delivery, a trader's haul) must stand to be called to it. Twelve:
    /// the fight is beside its road. Further off it is left at its walk (06.10.2026, deliver16).
    /// </summary>
    public static int CarrierReach { get; set; } = 12;

    /// <summary>Bots carrying another bot's goods left out of a dire cry's first calling for standing past <see cref="CarrierReach"/>.</summary>
    public static long CarriersSpared => _why[WhyCarrying];

    /// <summary>How many of the nearest of ours a calling weighs. Eight.</summary>
    public static int Shortlist { get; set; } = 8;

    /// <summary>Dire cries, and what came of each at its first calling. Each ends in exactly one of these.</summary>
    public static long Dire { get; private set; }

    /// <summary>Helpers pressed to a rescue by a calling, all told, and callings that sent at least one.</summary>
    public static long Called { get; private set; }

    public static long Callings { get; private set; }

    /// <summary>Dire cries from a member whose company already had what was on it for its enemy: the company's fight.</summary>
    public static long DireCompany { get; private set; }

    /// <summary>Dire cries from a criminal or a murderer: nobody is sent.</summary>
    public static long DireOutlaw { get; private set; }

    /// <summary>Dire cries from underground (the delve's company is the rescue) and about a creature no road reaches.</summary>
    public static long DireBelow { get; private set; }

    public static long DireShunned { get; private set; }

    /// <summary>Dire cries with nobody of ours near who was free, fit and friendly; and where those near could not have won it together.</summary>
    public static long DireNobody { get; private set; }

    public static long DireBeyond { get; private set; }

    /// <summary>Criers passed over by a free bot's own look for being of a guild hostile to it, or outlaws. See <see cref="Nearest"/>.</summary>
    public static long PassedHostile { get; private set; }

    public static long PassedOutlaw { get; private set; }

    private sealed class Calling
    {
        public long Tick;
        public int Rounds;
        public int Sent;
        public bool Pending;

        /// <summary>Whether this bout's callings are the dire summons or the steady near call. See <see cref="Ask"/>.</summary>
        public bool Dire;
    }

    private static readonly Dictionary<Serial, Calling> _callings = [];

    private static readonly List<Serial> _pending = [];

    /// <summary>When each bot was last called to somebody's aid. See <see cref="HelperRestMs"/>.</summary>
    private static readonly Dictionary<Serial, long> _helped = [];

    private static readonly List<(BotMobile Who, int Off)> _near = [];

    private static readonly List<BotMobile> _chosen = [];

    /// <summary>
    /// Puts a cry down to be answered at the clock's next look, at most <see cref="MostRounds"/> times a bout. A steady bout that
    /// turns dire is a new bout: the near call sent one neighbour at most and weighed nothing; the summons weighs the fight.
    /// </summary>
    private static void Ask(BotMobile crier, bool dire)
    {
        var now = Core.TickCount;

        if (!_callings.TryGetValue(crier.Serial, out var calling))
        {
            _callings[crier.Serial] = calling = new Calling();
        }
        else if (!calling.Pending && calling.Rounds > 0 && now - calling.Tick >= HoldsMs + RecallMs)
        {
            // A new bout, or one that has outlasted its callings by a cry's whole span: it may call again.
            calling.Rounds = 0;
            calling.Sent = 0;
            calling.Dire = false;
        }

        if (dire && !calling.Dire)
        {
            calling.Dire = true;

            if (!calling.Pending)
            {
                calling.Rounds = 0;
                calling.Sent = 0;
            }
        }

        // A second calling only when the first sent nobody: those already on their way are not on the crier's side of the
        // reckoning until they arrive, and a calling that could not see them would send as many again.
        if (calling.Pending || calling.Sent > 0 || calling.Rounds >= MostRounds || calling.Rounds > 0 && now - calling.Tick < RecallMs)
        {
            return;
        }

        calling.Pending = true;
        _pending.Add(crier.Serial);
    }

    /// <summary>
    /// Answers the cries that turned dire since the clock last looked. Called by <c>BotBeat</c> every look, a tenth of a second,
    /// and not from inside <see cref="Raise"/>: a cry is raised in the middle of the crier's own auction, and pressing work
    /// into three other bots' hands from there is a decision taken inside another decision.
    /// </summary>
    public static void Dispatch()
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;

        for (var i = 0; i < _pending.Count; i++)
        {
            var serial = _pending[i];

            if (!_callings.TryGetValue(serial, out var calling))
            {
                continue;
            }

            calling.Pending = false;

            if (!_cries.TryGetValue(serial, out var cry)
                || cry.Who is not BotMobile { Deleted: false, Alive: true } who
                || cry.What is not BaseCreature { Deleted: false, Alive: true } foe)
            {
                continue;
            }

            calling.Tick = now;
            calling.Rounds++;

            try
            {
                calling.Sent += calling.Dire ? Summon(who, foe, calling.Rounds == 1, now) : Nearby(who, foe, calling.Rounds == 1, now);
            }
            catch (Exception e)
            {
                logger.Error(e, "Calling help for {Name} threw; the cry stands for whoever looks up", who.Name);
            }
        }

        _pending.Clear();

        // Both are hints with their own clocks; emptied when grown, and refilled by the next cries.
        if (_callings.Count > 1024)
        {
            _callings.Clear();
        }

        if (_helped.Count > 1024)
        {
            _helped.Clear();
        }

        if (_tended.Count > 1024)
        {
            _tended.Clear();
        }
    }

    /// <summary>
    /// <b>A dire cry reaches the best placed of ours in the beat it is raised, and they go (06.10.2026, react14; Patrick: "the
    /// bots must make decisions about a fight and about help instantly").</b>
    ///
    /// <para>
    /// <b>What was there.</b> A cry is a fact on a board, and a bot read the board when its own review came round: every
    /// fifteen seconds with work in hand (<c>BotWill.ReviewMs</c>), every two with none, never in a company, and then only if
    /// the rescue outbid the work in hand past the dwell, the margin and the hold. Measured on build 400 (06.10, 00:17-00:44,
    /// `react14-tools/baseline_events.py`): of 244 bouts of crying 9 had a rescue taken for the crier inside a minute, at a
    /// median of 24 seconds, and 150 of the 235 criers nobody came to were dead inside that minute. The stopwatch on build 401
    /// (00:51): 130 first cries, 16 answered, median three seconds and a ninth decile of thirty; 85 of the hundred nobody
    /// answered had somebody of ours able within sixty tiles.
    /// </para>
    ///
    /// <para>
    /// <b>Who goes.</b> The nearest of ours within <c>BotRescuer.Reach</c> who are free to: fit (<c>BotRescuer.FitAt</c>), no
    /// novice and no healer (a healer's answer is the heal: <c>BotSurgeon.Call</c>), in no company and no fight, not underground,
    /// of no guild hostile to the crier's, not called in the last <see cref="HelperRestMs"/>, and holding either nothing or work
    /// that is put down and taken up again (<c>BotDeed.Resumes</c>) — a calling never throws work away. Of those, not one that
    /// is itself outmatched where it stands, and only as many as the fight asks: they are added nearest first until what the
    /// crier's side and they bring is within <c>BotThreat.Tolerance</c> of what stands round the foe, at least one and at most
    /// <see cref="MostCalled"/>. If the whole shortlist together would still be short, nobody is sent to die beside the crier
    /// (<see cref="DireBeyond"/>); the cry stands, and the muster calls a company for it as before.
    /// </para>
    ///
    /// <para>
    /// <b>Not sent for:</b> a criminal or a murderer; a member whose company already has the thing for its enemy; a fight
    /// underground; a creature no road reaches. Each has its own count. The rescue is pressed (<c>BotWill.Press</c>), as a
    /// company's call is: the work in hand is put down for it and taken up when it ends.
    /// </para>
    /// </summary>
    private static int Summon(BotMobile who, BaseCreature foe, bool first, long now)
    {
        var map = who.Map;

        if (map == null || map == Map.Internal || foe.Map != map)
        {
            return 0;
        }

        if (first)
        {
            Dire++;
        }

        // An outlaw by the shard's own book (red, wanted, jailed), not by the engine's two-minute flag: that flag is set by a
        // stray blow and 620 criers wore it on build 405 while the free bots passed them by. Counted apart. See Nearest.
        if (BotOutlaw.Outlaw(who) || BotOutlaw.Jailed(who) || who.Murderer)
        {
            Filed(first, 1);

            return 0;
        }

        if (who.Criminal && first)
        {
            DireCriminal++;
        }

        // One look at who is near, for everything below: the fighters free to come, the nearest healer free to, the nearest
        // company with no enemy whose leader stands where it can answer, and for everybody else the reason, counted.
        _near.Clear();
        _bandsSeen.Clear();
        _sparesSeen.Clear();

        BotSquad spare = null;
        var spareOff = int.MaxValue;

        var threat = BotThreat.ThreatAt(who, foe.Location, BotMobile.NoticeRange);

        BotMobile healer = null;
        var healerOff = int.MaxValue;
        BotSquad band = null;
        var bandOff = int.MaxValue;

        foreach (var m in map.GetMobilesInRange<BotMobile>(who.Location, BotRescuer.Reach))
        {
            if (m == who || m.Deleted || !m.Alive)
            {
                continue;
            }

            var off = Math.Max(Math.Abs(m.X - who.X), Math.Abs(m.Y - who.Y));
            var why = Unfree(m, who, now);

            if (why == WhyHealer && Healers && off < healerOff && (BotMend.Spell(m, who) >= 0 || BotMend.Cloth(m) > 0))
            {
                healer = m;
                healerOff = off;

                continue;
            }

            if (why == WhyCompany && Companies && m.Squad is { } theirs && Brings(theirs, m, who, foe, threat))
            {
                if (off < bandOff)
                {
                    band = theirs;
                    bandOff = off;
                }

                continue;
            }

            // A company in a fight well in hand, one of whose members stands near enough to the crier to be spared to it
            // (06.10.2026, react18). Asked once a company a calling. See BotSquad.Spare.
            if (why == WhyCompany && BotSquad.Spares && off <= BotSquad.SpareReach && m.Squad is { } busy && !_sparesSeen.Contains(busy))
            {
                _sparesSeen.Add(busy);

                if (off < spareOff && busy.WellInHand())
                {
                    spare = busy;
                    spareOff = off;
                }
            }

            if (why != 0)
            {
                if (first)
                {
                    _why[why]++;
                }

                continue;
            }

            var at = _near.Count;

            // A friend of the crier's before anybody else, the nearest friend first (07.10.2026, traits17). See BotTies.CryRank.
            var rank = BotTies.CryRank(m, who, off);

            while (at > 0 && BotTies.CryRank(_near[at - 1].Who, who, _near[at - 1].Off) > rank)
            {
                at--;
            }

            if (at >= Shortlist)
            {
                continue;
            }

            _near.Insert(at, (m, off));

            if (_near.Count > Shortlist)
            {
                _near.RemoveAt(_near.Count - 1);
            }
        }

        // <b>A healer comes as a healer (06.10.2026, react16; Patrick: a healer helps and heals every non-hostile, non-criminal
        // bot).</b> To a crier that is actually hurt, whoever it is and whatever its company is doing: the nearest healer free
        // to come is pressed to the mending itself (BotSalve walks to the patient and mends it until it is whole), its work put
        // down. One healer a crier in <see cref="HelperRestMs"/>.
        if (healer != null && who.HitsMax > 0 && who.Hits < who.HitsMax * DireAt && !BotDungeon.Under(who.Location)
            && (!_tended.TryGetValue(who.Serial, out var tended) || now - tended >= HelperRestMs))
        {
            var trains = BotMend.Spell(healer, who) >= 0 ? SkillName.Magery : SkillName.Healing;

            if (BotWill.Press(healer, new BotSalve(who, map, onSelf: false, trains), $"called to {who.Name} as a healer, {healerOff} tiles off", StackCalls))
            {
                _tended[who.Serial] = now;
                _helped[healer.Serial] = now;
                HealersCalled++;
            }
        }

        if (who.Squad is { } company && ReferenceEquals(company.Focus, foe))
        {
            Filed(first, 0);

            return 0;
        }

        if (BotDungeon.Under(foe.Location))
        {
            Filed(first, 2);

            return 0;
        }

        if (BotQuarry.Shunned(foe))
        {
            Filed(first, 3);

            return 0;
        }

        // Ours against this thing: a company member beside the crier but held by its own enemy is not on the crier's side (13.10.2026,
        // w5 review A; until then it was, and the need read as met by bots that would never turn).
        var ours = BotThreat.OurPower(who, BotMobile.NoticeRange, foe);
        var need = threat / Math.Max(0.1, BotThreat.Tolerance) - ours;

        _chosen.Clear();

        var brought = 0.0;

        for (var i = 0; i < _near.Count && _chosen.Count < MostCalled; i++)
        {
            if (_chosen.Count >= 1 && brought >= need)
            {
                break;
            }

            var m = _near[i].Who;

            // In trouble where it stands: it is no help to anybody else.
            if (BotThreat.Decide(m, BotMobile.NoticeRange) == BotStand.Outmatched)
            {
                if (first)
                {
                    _why[WhyFighting]++;
                }

                continue;
            }

            _chosen.Add(m);

            // One standing by the crier is already counted on its side.
            if (_near[i].Off > BotMobile.NoticeRange)
            {
                brought += BotThreat.Power(m);
            }
        }

        // <b>A company near, whose leader can bring it (react16).</b> When the free bots are none or not enough, the nearest
        // company with no enemy of its own takes this one on, exactly as it would had the thing struck one of its members.
        var bring = band != null && (_chosen.Count == 0 || brought < need);

        if (bring)
        {
            brought += band.Able();
        }

        // Nobody free, or not enough, and no idle company: a company near whose fight is well in hand spares its members nearest the
        // thing, as many as the thing asks and none when all it could spare would be short — the rule the free bots are sent by
        // (12.10.2026, w5; until then one or two went whatever stood round the crier, and only when no free bot at all was found).
        // See BotSquad.Spare.
        var spared = 0;

        if (!bring && brought < need && spare != null)
        {
            spared = spare.Spare(who, foe, now, need - brought, out var sparedPower);
            brought += sparedPower;
        }

        if (_chosen.Count == 0 && !bring && spared == 0)
        {
            Filed(first, 4);

            return 0;
        }

        if (brought < need)
        {
            Filed(first, 5);

            return 0;
        }

        var sent = spared;

        if (bring)
        {
            band.Engage(foe, band.Leader);
            CompaniesBrought++;
            sent++;

            logger.Information(
                "Squad {Id} of {Count} is brought to {Name}'s aid against {Foe}: no enemy of its own, its leader {Off} tiles off",
                band.Id,
                band.Count,
                who.Name,
                foe.Name,
                bandOff
            );
        }

        for (var i = 0; i < _chosen.Count; i++)
        {
            var m = _chosen[i];
            var trains = m.Bond?.Weapon?.Skill ?? SkillName.Wrestling;
            var rescue = new BotRescue(new BotSlay(foe, trains), who, foe, own: false, rescuer: m);

            if (BotWill.Press(m, rescue, $"called to {who.Name}'s aid against {foe.Name}, {_near.Count} of ours free within {BotRescuer.Reach} and {_chosen.Count} sent", StackCalls))
            {
                _helped[m.Serial] = now;
                Called++;
                sent++;

                // Counted as a friend's answer when it went as one. See BotTies.Answered.
                BotTies.Answered(m, who);
            }
        }

        if (sent > 0)
        {
            Callings++;
        }

        return sent;
    }

    /// <summary>
    /// <b>A steady cry sends the nearest free neighbour (12.10.2026, w5).</b>
    ///
    /// <para>
    /// A cry from a bot whole and in a fight it judged it could win was a fact on the board and nothing more: the summons is for
    /// a dire cry, and the board is read by a free bot at its review — two seconds with nothing in hand, fifteen with work, and
    /// then only past the dwell (half a minute to two) and only when the rescue at <c>BotRescue.Steady</c> out-prices the work.
    /// On the night of 12.10 (23:18–03:48) 5 142 bouts of crying began, 1 857 turned dire; of the 2 936 open to anybody 1 962
    /// were never answered and 1 142 of those had somebody of ours able within sixty tiles. Patrick's order of 08.09 ("a bot
    /// being chewed on by a rat with ten others standing about") is about exactly the cry that is not yet dire.
    /// </para>
    ///
    /// <para>
    /// So a steady cry is an interruption too, a small one: the nearest of ours within <see cref="NearReach"/> that is free to
    /// come by the summons' own rule with two easements of its own (05.11.2026, w5 — <see cref="Unfree"/>: in no company, no real
    /// fight, no mending, holding nothing or work put down and taken up again — or, only for a steady call, work that is not
    /// taken up again and a fight that is no danger to the neighbour, see <see cref="Trivial"/> — no carrier far off, no healer,
    /// no novice, not called lately) is pressed to the rescue, at most
    /// <see cref="MostNear"/>, nearest first, and only when what is on the crier is at least <see cref="NearOdds"/> of the
    /// crier's own strength — a rat on a warrior calls nobody. No company is brought and nobody is spared for a steady cry;
    /// the cry stands for the board as before, and a bout that turns dire is summoned for afresh (<see cref="Ask"/>).
    /// </para>
    /// </summary>
    public static bool NearCalls { get; set; } = true;

    /// <summary>How far a steady cry calls its nearest free neighbour from. Forty tiles: in sight, a short walk (05.11.2026, w5: twenty left 124 cries of 970 with nobody free, thirty still left 111 of 691 — and the earshot-never share is what the night's targets turn on, so the ring widens as the rule eases).</summary>
    public static int NearReach { get; set; } = 40;

    /// <summary>The least the thing on a steady crier must come to, as a share of the crier's own strength, to call a neighbour. A quarter (05.11.2026, w5: a half let 41.5% of steady cries be trivial, and the call almost never came). The same share read the other way — what a neighbour is fighting under this share of its own strength is no fight worth keeping it in (<see cref="Trivial"/>).</summary>
    public static double NearOdds { get; set; } = 0.25;

    /// <summary>The most neighbours a steady cry sends. One: the crier is winning, the neighbour makes it quick.</summary>
    public static int MostNear { get; set; } = 1;

    /// <summary>
    /// Steady bouts of crying that called, and what came of each <b>first</b> calling: one bucket each, every bucket counted on the first
    /// round only (13.10.2026, w5 review B: the callings counted every round for a day, and a bout whose first round found nobody and
    /// whose second sent one sat in two buckets). <see cref="NearCalled"/> counts bots, every round.
    /// </summary>
    public static long NearCries { get; private set; }

    public static long NearCallings { get; private set; }

    public static long NearCalled { get; private set; }

    public static long NearNobody { get; private set; }

    public static long NearTrivial { get; private set; }

    public static long NearCompanied { get; private set; }

    public static long NearBelow { get; private set; }

    public static long NearShunned { get; private set; }

    public static long NearOutlaw { get; private set; }

    /// <summary>Bots sent to a steady cry out of a fight of their own that was no danger to them (<see cref="Trivial"/>), and off work that is not taken up again, which the press dropped (05.11.2026, w5). Bots, every round, as <see cref="NearCalled"/>.</summary>
    public static long NearFromFight { get; private set; }

    public static long NearNoReturn { get; private set; }

    /// <summary>Bots within <see cref="NearReach"/> of a steady cry that were not free to come, by why. Bots, first callings only.</summary>
    private static readonly long[] _nearWhy = new long[14];

    /// <summary>The steady cry's calling. See <see cref="NearCalls"/>.</summary>
    private static int Nearby(BotMobile who, BaseCreature foe, bool first, long now)
    {
        var map = who.Map;

        if (map == null || map == Map.Internal || foe.Map != map)
        {
            return 0;
        }

        if (first)
        {
            NearCries++;
        }

        if (BotOutlaw.Outlaw(who) || BotOutlaw.Jailed(who) || who.Murderer)
        {
            if (first)
            {
                NearOutlaw++;
            }

            return 0;
        }

        // A member's steady cry is its company's: the company turns on what strikes it (BotSquads.Note) and its mates peel.
        if (who.Squad != null)
        {
            if (first)
            {
                NearCompanied++;
            }

            return 0;
        }

        if (BotDungeon.Under(foe.Location))
        {
            if (first)
            {
                NearBelow++;
            }

            return 0;
        }

        if (BotQuarry.Shunned(foe))
        {
            if (first)
            {
                NearShunned++;
            }

            return 0;
        }

        var threat = BotThreat.ThreatAt(who, foe.Location, BotMobile.NoticeRange);
        var own = BotThreat.Power(who);

        if (threat < NearOdds * own)
        {
            if (first)
            {
                NearTrivial++;
            }

            return 0;
        }

        _near.Clear();

        foreach (var m in map.GetMobilesInRange<BotMobile>(who.Location, NearReach))
        {
            if (m == who || m.Deleted || !m.Alive)
            {
                continue;
            }

            var why = Unfree(m, who, now, tally: false, dire: false);

            if (why != 0)
            {
                if (first)
                {
                    _nearWhy[why]++;
                }

                continue;
            }

            var off = Math.Max(Math.Abs(m.X - who.X), Math.Abs(m.Y - who.Y));
            var at = _near.Count;

            while (at > 0 && _near[at - 1].Off > off)
            {
                at--;
            }

            if (at >= MostNear)
            {
                continue;
            }

            _near.Insert(at, (m, off));

            if (_near.Count > MostNear)
            {
                _near.RemoveAt(_near.Count - 1);
            }
        }

        var sent = 0;

        for (var i = 0; i < _near.Count; i++)
        {
            var m = _near[i].Who;

            // In trouble where it stands: it is no help to anybody else.
            if (BotThreat.Decide(m, BotMobile.NoticeRange) == BotStand.Outmatched)
            {
                if (first)
                {
                    _nearWhy[WhyFighting]++;
                }

                continue;
            }

            var trains = m.Bond?.Weapon?.Skill ?? SkillName.Wrestling;
            var rescue = new BotRescue(new BotSlay(foe, trains), who, foe, own: false, rescuer: m);

            // What the neighbour held, before the press settles it: the steady easements are counted where they
            // actually send somebody, not where they merely stop refusing (05.11.2026, w5). See Trivial.
            var held = m.Resolve?.Deed;

            if (BotWill.Press(m, rescue, $"called to {who.Name} near by against {foe.Name}, {_near[i].Off} tiles off, the thing {threat:F0} against the crier's {own:F0}", StackCalls))
            {
                _helped[m.Serial] = now;
                NearCalled++;
                sent++;

                if (held?.Foe != null)
                {
                    NearFromFight++;
                }

                if (held != null && !held.Resumes)
                {
                    NearNoReturn++;
                }

                BotTies.Answered(m, who);
            }
        }

        if (sent == 0)
        {
            if (first)
            {
                NearNobody++;
            }

            return 0;
        }

        if (first)
        {
            NearCallings++;
        }

        return sent;
    }

    private static string DescribeNearWhy()
    {
        var say = "";

        for (var i = 1; i < _nearWhy.Length; i++)
        {
            if (_nearWhy[i] > 0)
            {
                say += $"{(say.Length > 0 ? ", " : "")}{_nearWhy[i]} {Whys[i]}";
            }
        }

        return say.Length == 0 ? "none" : say;
    }

    /// <summary>Whether a healer free to come is pressed to a hurt crier's mending. A switch, like the two below.</summary>
    public static bool Healers { get; set; } = true;

    /// <summary>Whether a company with no enemy is brought to a cry the free bots cannot answer.</summary>
    public static bool Companies { get; set; } = true;

    /// <summary>Whether a bot whose work in hand is already over work put down may be called (the work in hand is put down over it).</summary>
    public static bool StackCalls { get; set; } = true;

    /// <summary>Healers pressed to a crier's mending, and companies brought to a cry.</summary>
    public static long HealersCalled { get; private set; }

    public static long CompaniesBrought { get; private set; }

    /// <summary>When each crier last had a healer sent. See <see cref="HelperRestMs"/>.</summary>
    private static readonly Dictionary<Serial, long> _tended = [];

    private const int WhyCompany = 1;
    private const int WhyResting = 2;
    private const int WhyUnfit = 3;
    private const int WhyFighting = 4;
    private const int WhyCommitted = 5;
    private const int WhyNoReturn = 6;
    private const int WhyPutDown = 7;
    private const int WhyLately = 8;
    private const int WhyBelow = 9;
    private const int WhyHostile = 10;
    private const int WhyNovice = 11;
    private const int WhyHealer = 12;
    private const int WhyCarrying = 13;

    private static readonly string[] Whys =
    [
        "", "in a company", "played out or of a class that takes no offers", "under the fitness bar", "in a fight, a flight or a mending, or outmatched where it stood",
        "on work that may not be bid away", "on work that is not taken up again", "on work already over two put down", "called within the rest",
        "underground", "of a hostile guild", "novices", "healers with no means or not the nearest",
        "carrying another bot's goods further off than the carrier's reach"
    ];

    /// <summary>Bots within reach of a dire cry that were not free to come, by why. Bots, not cries; first callings only.</summary>
    private static readonly long[] _why = new long[14];

    /// <summary>
    /// Whether a company can be brought: this bot leads it, it has no enemy and is in no war, its leader stands where the
    /// company answers from (<c>BotSquad.AnswerReach</c>), it is not hostile to the crier, and the thing is neither beyond it
    /// nor one a company no stronger lately withdrew from — the rule a blow on one of its own members is answered by.
    /// </summary>
    private static bool Brings(BotSquad squad, BotMobile m, BotMobile who, BaseCreature foe, double threat)
    {
        // Any member in reach speaks for its company, once: the first one met files the reason, the rest are not counted.
        if (squad.Disbanded || _bandsSeen.Contains(squad))
        {
            return false;
        }

        _bandsSeen.Add(squad);

        if (squad.Leader?.Self is not BotMobile { Deleted: false, Alive: true } lead)
        {
            _whyBand[1]++;

            return false;
        }

        if (squad.Warring)
        {
            _whyBand[2]++;

            return false;
        }

        // Its own fight is kept unless what is on the crier is Switch times its enemy: then the crier's is the fight that matters.
        if (squad.Focus is { Deleted: false, Alive: true } focus && BotThreat.Power(focus) * BotSquads.Switch > threat)
        {
            _whyBand[3]++;

            return false;
        }

        // The leader's reach, or — with no enemy of its own — this member's: the company walks to wherever its enemy is, and a
        // member standing near the thing is what tells it that there is one (06.10.2026, react18; 58 companies on build 410 were
        // refused for "the leader beyond answering reach" in fifty minutes).
        var reaches = lead.InRange(foe.Location, BotSquad.AnswerReach)
                      || MemberReach && squad.Focus is not { Deleted: false, Alive: true } && m.InRange(foe.Location, BotSquad.AnswerReach);

        if (!reaches)
        {
            _whyBand[4]++;

            return false;
        }

        if (BotDungeon.Under(lead.Location) || BotRegard.Hostile(lead, who))
        {
            _whyBand[5]++;

            return false;
        }

        if (!BotSquad.Caster(foe) && (squad.Beyond(foe) || BotSquad.Shuns(foe, squad.Able())))
        {
            _whyBand[6]++;

            return false;
        }

        return true;
    }

    /// <summary>How near a dire cry must be for a fit bot to leave a chat by a fire, an envoy's or a watchman's errand for it. Nought: never.</summary>
    public static int CommittedReach { get; set; } = 20;

    /// <summary>Bots near a dire cry on committed work, by the kind of work. See <see cref="Unfree"/>.</summary>
    private static readonly Dictionary<string, long> _committed = [];

    private static string DescribeCommitted()
    {
        var say = "";

        foreach (var (kind, n) in _committed)
        {
            say += $"{(say.Length > 0 ? ", " : "")}{kind} {n}";
        }

        return say.Length == 0 ? "none" : say;
    }

    /// <summary>Whether a member near the thing speaks for its company's reach when the company has no enemy. A switch.</summary>
    public static bool MemberReach { get; set; } = true;

    private static readonly HashSet<BotSquad> _bandsSeen = [];

    private static readonly HashSet<BotSquad> _sparesSeen = [];

    /// <summary>Companies with a member near a dire cry that were not brought, by why. Companies, first callings only.</summary>
    private static readonly long[] _whyBand = new long[7];

    private static readonly string[] WhyBands =
        ["", "no leader standing", "at war", "in a fight of their own against something not weaker", "the leader beyond answering reach of the thing", "the leader underground or hostile", "the thing beyond them"];

    private static string DescribeBands()
    {
        var say = "";

        for (var i = 1; i < _whyBand.Length; i++)
        {
            if (_whyBand[i] > 0)
            {
                say += $"{(say.Length > 0 ? ", " : "")}{_whyBand[i]} {WhyBands[i]}";
            }
        }

        return say.Length == 0 ? "none" : say;
    }

    /// <summary>Dire criers wearing the engine's criminal flag, helped all the same; and such criers a free bot's look did not pass over.</summary>
    public static long DireCriminal { get; private set; }

    public static long PassedCriminal { get; private set; }

    /// <summary>One dire cry's first calling filed under what came of it; a second calling of the same bout is not counted again.</summary>
    private static void Filed(bool first, int what)
    {
        if (!first)
        {
            return;
        }

        switch (what)
        {
            case 0:
                DireCompany++;
                break;
            case 1:
                DireOutlaw++;
                break;
            case 2:
                DireBelow++;
                break;
            case 3:
                DireShunned++;
                break;
            case 4:
                DireNobody++;
                break;
            default:
                DireBeyond++;
                break;
        }
    }

    /// <summary>
    /// Why this bot may not be called to that crier now, or nought when it may. One reason each, the first that holds, and every
    /// clause a rule that was already somebody's. A healer otherwise free answers <see cref="WhyHealer"/>: it is not sent to
    /// fight, and <see cref="Summon"/> sends the nearest of them to mend. A steady call (<paramref name="dire"/> false) eases
    /// two of the rules: a fight that is no danger to the bot does not hold it (<see cref="Trivial"/>), and work that is not
    /// taken up again may be dropped for the rescue (counted, <see cref="NearNoReturn"/>). The summons keeps both refusals.
    /// </summary>
    private static int Unfree(BotMobile m, BotMobile who, long now, bool tally = true, bool dire = true)
    {
        if (m.Squad != null)
        {
            return WhyCompany;
        }

        if (m.Tired || m.Class is not { Bidding: true } klass)
        {
            return WhyResting;
        }

        if (m.HitsMax <= 0 || m.Hits < m.HitsMax * BotRescuer.FitAt)
        {
            return WhyUnfit;
        }

        if (m.Resolve is not { } resolve)
        {
            return WhyFighting;
        }

        var deed = resolve.Deed;

        // Hunted while holding no fight of its own is refused either way: it is being beaten with no plan, which is trouble
        // and not a neighbour to spare. A steady call may still take a bot out of a trivial fight of its own (Trivial).
        if (BotLadder.Hunted(resolve) && (dire || !Trivial(m, deed?.Foe)))
        {
            return WhyFighting;
        }

        if (deed != null)
        {
            if (deed is BotBolt || deed is BotSalve || deed is BotBrawl || deed is BotAccompany)
            {
                return WhyFighting;
            }

            if (deed.Foe != null && (dire || !Trivial(m, deed.Foe)))
            {
                return WhyFighting;
            }

            if (deed.Committed)
            {
                // <b>Which work, and whether a dire cry near still outranks it (06.10.2026, react18).</b> Committed work is work
                // that may not be bid away: a carrier with goods, a voyage, a shopkeeper at its front, a delve underground, an
                // outlaw's business, a company's enlistment — and a chat by a fire, an envoy's or a watchman's errand, which a
                // bot being killed within CommittedReach outranks. Counted by kind either way — for a dire calling; the steady
                // near call asks the same question and keeps its own count (DescribeNearWhy).
                if (tally)
                {
                    _committed[deed.Kind] = _committed.GetValueOrDefault(deed.Kind) + 1;
                }

                if (!(CommittedReach > 0 && (deed is BotChat || deed is BotEnvoy || deed is BotStandGuard) && m.InRange(who.Location, CommittedReach)))
                {
                    return WhyCommitted;
                }
            }

            // <b>Work that is not taken up again holds the summons but not a steady call (05.11.2026, w5).</b> The summons never
            // throws work away; a steady call may drop it — a rat on a neighbour is worth more than a finished errand's
            // convenience — and the drop is counted where it actually sends somebody (NearNoReturn).
            if (!deed.Resumes && dire)
            {
                return WhyNoReturn;
            }

            // <b>A carrier with another bot's goods comes only from close by (06.10.2026, deliver16).</b> The calling is for a dire cry
            // already; from further than CarrierReach the walk to the fight and back is the delivery's whole margin, and a walk put
            // down too long is dropped (47 deliveries on build 407, 46 of them for a company). See BotSquads.Pressable.
            if ((deed is BotDeliver || deed is BotHaul) && !m.InRange(who.Location, CarrierReach))
            {
                return WhyCarrying;
            }

            // Work in hand over work put down is put down over it (BotWill.Press, stack); over two, there is no third place.
            if (resolve.Paused != null && (!StackCalls || resolve.Under != null))
            {
                return WhyPutDown;
            }
        }

        if (_helped.TryGetValue(m.Serial, out var called) && now - called < HelperRestMs)
        {
            return WhyLately;
        }

        if (BotDungeon.Under(m.Location))
        {
            return WhyBelow;
        }

        if (BotRegard.Hostile(m, who))
        {
            return WhyHostile;
        }

        if (klass.Role == BotRole.Medic)
        {
            return WhyHealer;
        }

        return BotLadder.Novice(m) ? WhyNovice : 0;
    }

    /// <summary>
    /// Whether what this bot is fighting is too small to keep it from a steady cry: alive, standing, and under
    /// <see cref="NearOdds"/> of the bot's own strength — the same share that decides the crier's trouble is worth a
    /// neighbour, read the other way (05.11.2026, w5; the earshot-never bucket was 133 of 343 open cries, 79 of them
    /// bots busy in a fight, a flight or a mending). The summons never asks: for a dire cry a fight is a fight.
    /// </summary>
    private static bool Trivial(BotMobile m, Mobile foe) =>
        foe is { Deleted: false, Alive: true } && BotThreat.Power(foe) < NearOdds * BotThreat.Power(m);

    /// <summary>The reasons bots near a dire cry were not free, most first.</summary>
    private static string DescribeWhy()
    {
        var say = "";

        for (var i = 1; i < _why.Length; i++)
        {
            if (_why[i] > 0)
            {
                say += $"{(say.Length > 0 ? ", " : "")}{_why[i]} {Whys[i]}";
            }
        }

        return say.Length == 0 ? "none" : say;
    }

    /// <summary>The summons, for the <c>Reflexes:</c> line and for <c>Arms:</c>.</summary>
    public static string DescribeCalls() =>
        $"{Dire} cries turned dire (the crier under {DireAt:P0} of its health, or refused its fight for the odds): {Callings} called {Called} of ours to a rescue at once (work in hand put down, never dropped), "
        + $"{DireCompany} were a member's whose company already had the thing for its enemy, {DireNobody} had nobody free, fit and friendly within {BotRescuer.Reach} tiles (bots near a dire cry not free, by why: {DescribeWhy()}), {CompaniesBrought} had a company brought by its leader (companies near not brought, by why: {DescribeBands()}), {DireCriminal} wore the engine's criminal flag and were helped all the same, committed work near a dire cry by kind: {DescribeCommitted()}; {BotSquad.DescribeSpares()}, {HealersCalled} healers pressed to a hurt crier's mending, {DireBeyond} were beyond what those near could have brought together, "
        + $"{DireBelow} underground, {DireShunned} about a creature no road reaches, {DireOutlaw} from a criminal or a murderer; a free bot's own look passed over {PassedHostile} criers of a guild hostile to it and {PassedOutlaw} outlaws (red, wanted or jailed), {BotRescuer.PassedCrowded} steady criers for the crowd note on the thing, and did not pass over {PassedCriminal} wearing the engine's criminal flag; "
        + $"{NearCries} steady cries (the crier whole and able): {NearCallings} called {NearCalled} of ours within {NearReach} tiles at once ({NearFromFight} of them out of a fight of their own that was no danger, {NearNoReturn} off work not taken up again), {NearNobody} had nobody free there (bots near not free, by why: {DescribeNearWhy()}), {NearTrivial} were about a thing under {NearOdds:P0} of the crier's own strength, {NearCompanied} from a member of a company, {NearBelow} underground, {NearShunned} about a creature no road reaches, {NearOutlaw} from a criminal or a murderer{(NearCalls ? "" : " (off)")}";

    /// <summary>Over, one way or the other. Called when the bot is clear, or dead, or has been helped.</summary>
    /// <summary>Whether this bot's cry still stands: raised and renewed within <see cref="HoldsMs"/>. For a spare's end. See <c>BotSquad.Sparing</c>.</summary>
    public static bool Crying(Mobile who) =>
        who != null && _cries.TryGetValue(who.Serial, out var cry) && Core.TickCount - cry.Tick < HoldsMs;

    public static void Quiet(Mobile who)
    {
        if (who != null)
        {
            _cries.Remove(who.Serial);
        }
    }

    /// <summary>
    /// The nearest of ours who is calling for help within reach of this one, and what is on them.
    ///
    /// <para>
    /// Nearest rather than worst off, deliberately. Whoever is closest can be reached soonest, and a rescue
    /// that arrives is worth more than a better-chosen one that arrives too late — the same reasoning the
    /// mending subsystem uses about who a medic goes to.
    /// </para>
    /// </summary>
    public static (Mobile Who, BaseCreature What) Nearest(Mobile helper, int range)
    {
        if (helper?.Map is not { } map || map == Map.Internal)
        {
            return (null, null);
        }

        Mobile found = null;
        BaseCreature onThem = null;
        var closest = int.MaxValue;

        List<Serial> stale = null;

        foreach (var (serial, cry) in _cries)
        {
            var (who, what, tick, _) = cry;

            if (Core.TickCount - tick >= HoldsMs
                || who is not { Deleted: false, Alive: true }
                || what is not BaseCreature { Deleted: false, Alive: true } creature)
            {
                (stale ??= []).Add(serial);

                continue;
            }

            if (who == helper || who.Map != map)
            {
                continue;
            }

            // Nothing across hostile guilds, and nobody goes to a criminal's or a murderer's aid (06.10.2026). Counted: a gate
            // without a bucket is invisible.
            if (BotOutlaw.Outlaw(who) || BotOutlaw.Jailed(who) || who.Murderer)
            {
                PassedOutlaw++;

                continue;
            }

            if (who.Criminal)
            {
                PassedCriminal++;
            }

            if (BotRegard.Hostile(helper, who))
            {
                PassedHostile++;

                continue;
            }

            var away = (int)helper.GetDistanceToSqrt(who.Location);

            if (away > range || away >= closest)
            {
                continue;
            }

            closest = away;
            found = who;
            onThem = creature;
        }

        if (stale != null)
        {
            for (var i = 0; i < stale.Count; i++)
            {
                _cries.Remove(stale[i]);
            }
        }

        return (found, onThem);
    }

    /// <summary>Counted where the help is actually taken on, so the tally is of rescues and not of offers.</summary>
    public static void Noted() => Answered++;

    /// <summary>Milliseconds between cries being raised and somebody setting out, summed, and how many.</summary>
    public static long ReactionMs { get; private set; }

    public static long Reactions { get; private set; }

    /// <summary>
    /// Somebody is setting out to this cry: how long it stood is the reaction the night of 13.09.2026 is
    /// judged by. Counted once per cry - the first to go is the reaction; the second is company.
    /// </summary>
    public static void Answering(Mobile who)
    {
        if (who == null || !_cries.TryGetValue(who.Serial, out var cry))
        {
            return;
        }

        ReactionMs += Core.TickCount - cry.Tick;
        Reactions++;

        // Re-stamped so a second helper is not counted as a second reaction to the same cry.
        _cries[who.Serial] = (cry.Who, cry.What, long.MinValue / 2, cry.Dire);
    }

    public static string Describe() =>
        Raised == 0
            ? "nobody has called for help"
            : $"{Raised} cried for help, {Answered} were gone to, the first helper setting out {(Reactions > 0 ? ReactionMs / Reactions / 1000.0 : 0.0):F1}s after the cry on average over {Reactions} (rescues offered and the cry's last renewal, not rescues taken nor the wait: Reflexes: times the first cry of a bout to the first rescue taken); {DescribeCalls()}; {BotDefender.Already} blows not answered with a second errand because the work in hand was already that fight, {BotDefender.Outnumbered} not answered with a fight the odds round the bot already called off, {BotDefender.Leading} on a company's leader left to the company, {BotDefender.Below} fights underground left to the delve's company, {BotDefender.ToTown} answered with a run for a town by a bot outmatched where it stood";

    public static void Forget()
    {
        _cries.Clear();
        Raised = 0;
        Answered = 0;
        ReactionMs = 0;
        Reactions = 0;

        _callings.Clear();
        _pending.Clear();
        _helped.Clear();
        _tended.Clear();
        System.Array.Clear(_why);
        System.Array.Clear(_whyBand);
        System.Array.Clear(_nearWhy);
        _committed.Clear();
        NearCries = 0;
        NearCallings = 0;
        NearCalled = 0;
        NearNobody = 0;
        NearTrivial = 0;
        NearCompanied = 0;
        NearBelow = 0;
        NearShunned = 0;
        NearOutlaw = 0;
        NearFromFight = 0;
        NearNoReturn = 0;
        _sparesSeen.Clear();
        _bandsSeen.Clear();
        DireCriminal = 0;
        PassedCriminal = 0;
        HealersCalled = 0;
        CompaniesBrought = 0;
        Dire = 0;
        Called = 0;
        Callings = 0;
        DireCompany = 0;
        DireOutlaw = 0;
        DireBelow = 0;
        DireShunned = 0;
        DireNobody = 0;
        DireBeyond = 0;
        PassedHostile = 0;
        PassedOutlaw = 0;

        BotReflex.Forget();
    }
}
