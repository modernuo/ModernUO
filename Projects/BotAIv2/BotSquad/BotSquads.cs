using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Every squad on the shard, and the four things that can happen to one: it forms, somebody joins, somebody
/// leaves, it dies.
///
/// <para>
/// <b>One way in.</b> <see cref="Join"/> is the only path into a squad, and the size cap is checked there and
/// nowhere else. That is not tidiness — the first version checked its cap on the recruiting path only, so a
/// bot that stumbled on the same target and called for help was added without any check at all, and companies
/// with a stated maximum of five were found holding twelve.
/// </para>
///
/// <para>
/// <b>The squad's own beat, not the bots'.</b> Rosters, stances and stations change over seconds, not
/// milliseconds, so this runs once a second on its own timer while the bots walk at their own pace. The work
/// per beat is proportional to the number of squads, and the expensive part — working out a station, which
/// ends in a path search — happens only when the anchor has actually moved.
/// </para>
/// </summary>
public static class BotSquads
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSquads));

    /// <summary>How often a squad reconsiders itself. Its life is slow; the bots inside it are not.</summary>
    public const int BeatMs = 1000;

    private static readonly List<BotSquad> _squads = [];

    private static SquadTimer _timer;

    private static int _next;

    public static IReadOnlyList<BotSquad> All => _squads;

    public static int Count => _squads.Count;

    public static long Formed { get; private set; }

    public static long Disbanded { get; private set; }

    public static long Rescues { get; private set; }

    public static long Yields { get; private set; }

    /// <summary>
    /// Bots turned away from a company that had already been taken apart.
    ///
    /// A named nought that should stay small rather than nought: companies really do dissolve in the same
    /// beat somebody decides to join one, and the whole point is that the answer is now "no" instead of a bot
    /// spending the rest of the shard's life in a company that does not exist.
    /// </summary>
    public static long Buried { get; private set; }

    /// <summary>Bots refused a place in a company for a guild hostile to its leader's (at war, or thought of below the war line). See <see cref="Join"/>.</summary>
    public static long Enemies { get; private set; }

    /// <summary>Blows from inside the company itself, passed over: a company never turns on one of its own.</summary>
    public static long Inside { get; private set; }

    public static bool Running => _timer != null;

    public static void Start()
    {
        if (_timer != null)
        {
            return;
        }

        // Seeded from a real tick rather than left at zero: these counters can start enormous and wrap.
        _saidTick = Core.TickCount;

        _timer = new SquadTimer(TimeSpan.FromMilliseconds(BeatMs));
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    public static void Reset()
    {
        for (var i = _squads.Count - 1; i >= 0; i--)
        {
            Dissolve(_squads[i], "the world was reloaded");
        }

        _squads.Clear();
        _next = 0;
        Formed = 0;
        Disbanded = 0;
        Rescues = 0;
        Steady = 0;
        Yields = 0;
        Buried = 0;
        Rebuffs = 0;
        Hurries = 0;
        Comings = 0;
        Protected = 0;
        ProtectedIdle = 0;
        _due.Clear();
        Afield = 0;
        CalledPast = 0;
        Sufficed = 0;
        LeftAtWork = 0;
        CarriersSpared = 0;
        FightersSpared = 0;
        ThievesSpared = 0;
        PressedIn = 0;
        Friendly = 0;
        Enemies = 0;
        Inside = 0;
        BotSquad.Forget();
        BotRaise.Forget();

        BotSpoils.Reset();
    }

    /// <summary>
    /// The companies in a sentence, and how many bots they are holding.
    ///
    /// <para>
    /// <b>The count of bots bound to a company was the one figure this line did not carry, and it is the one
    /// with a consequence.</b> A bound bot does not take part in the auction at all — the ladder answers for
    /// it — so a company standing about is not merely idle, it is a hole in the population's working time
    /// that nothing else on the shard reports. On 03.09.2026 at 08:13 the stall watch caught Faron 2 the
    /// Healer four minutes into "fell in with company 3", and answering how common that was meant grepping
    /// an hour of log for the phrase. Squads standing, formed and disbanded were all here; how many bots
    /// were inside them was not.
    /// </para>
    /// </summary>
    /// <summary>
    /// How many bots are held in companies this moment.
    ///
    /// <para>
    /// <b>Wanted outside this file the day a wider muster reach turned three quarters of the shard into one
    /// army.</b> A bot on the Bound rung is not offered work of its own — that is the whole point of the
    /// rung — so this number is also the count of bots that have stopped mining, sewing and trading. See
    /// BotMuster.MostBound.
    /// </para>
    /// </summary>
    public static int Bound
    {
        get
        {
            var held = 0;

            for (var i = 0; i < _squads.Count; i++)
            {
                held += _squads[i]?.Count ?? 0;
            }

            return held;
        }
    }

    public static string Describe()
    {
        var bound = Bound;

        return $"{Count} squads standing holding {bound} bots, {Formed} formed and {Disbanded} disbanded, {BotSquad.Sundered} stations withheld for lying across a dungeon's edge from the member, {BotSquad.StationRested} withheld from a member resting after a place it could not reach ({BotSquad.StationShorted} set resting for a search that came back short of its place, {BotWalk.ShortStations} as the short plan was drawn, {BotWalk.StationsLetGo} places let go at their second short plan), {BotSquad.StationFar} withheld in a fight from a member further than {BotSquad.StationReach} tiles from the anchor and left to its enlistment's walk,{Rescues} times a company with no enemy was set upon and took on the strongest thing attacking it ({Afield} blows on a member standing further than {BotSquad.AnswerReach} tiles from its leader left to the member), {Steady} blows that left the company on the enemy it was already fighting ({Friendly} more by a bot of a guild not at war with it, passed over as friendly fire, {Inside} from inside the company itself, {Enemies} bots refused a place for a guild hostile to the leader's, {Distant} for standing more than {JoinReach} tiles from it), {Yields} tiles given up to whoever belonged on them, {Buried} turned away from a company that no longer existed, {Duellists} called while in a duel or held at the arenas and left there, {BotSquad.Released} let go for doing nothing for a company that was doing nothing, {Rebuffs} times one of them was handed back something it had already given up on, {BotSquad.Unowned} charges taken back because the errand holding them had ended; {BotSquad.Blinded} beats stood near enough to fight with no line to the thing ({BotSquad.NearLeader} of them set on the strongest other attacker they could see, else the enemy nearest the leader), {BotSquad.Refused} refused the blow by the engine and {BotSquad.Unsteadied} were shooters that had moved too recently to fire, {BotSquad.Blindfights} fights given up because nobody could land one at all, {BotFormation.Unanchored} stations answered with standing fast because nothing round the enemy could be walked to ({BotFormation.Uncomponented} candidates passed over for lying in another part of the walkers' graph), {BotSquad.Conjured} spells thrown by the back ranks and {BotSquad.Mended} heals aimed by their medics and mages ({BotSquad.Tended} of them between fights), against {BotSquad.Dry} beats with nothing they could pay for; the enemy: {BotSquad.Engaged} taken on, {BotSquad.ToStrongest} of them the strongest thing attacking a company that had none and {BotSquad.ToStronger} a stronger attacker the company left its enemy for ({BotSquad.LeaderCalls} of these the leader's own combatant inside the {BotSquad.TieShare:P0} tie), {BotSquad.HeldFor} beats a stronger attacker waited out the {BotSquad.FocusHoldMs / 1000}s hold, {BotSquad.Switched} switches away from an enemy still standing ({BotSquad.SwitchedSoon} of them inside the hold), {BotSquad.Downed} went down, {BotSquad.Closing} beats the never-got-near clock was torn up for a company still closing on its enemy; withdrawals: {BotSquad.Withdrawn()}; {BotSquad.Afielded()}; {BotCompanyRecord.Describe()}; {BotSquad.Supported()}; {BotRaise.Describe()}; calling: {CalledPast} called past the usual {BotSquad.Usual} because the fight asked more, {Sufficed} calls that stopped short of the {BotSquad.MaxSize} for having {BotSquad.CallOdds:F1} times what they were to meet, {LeftAtWork} bots within a call left at work the enlistment would have cost them ({CarriersSpared} of them carrying another bot's goods; {FightersSpared} hunters at their fight left to it by a call, a harrowing or a delve's party; {ThievesSpared} thieves at their turn in a crowd left to it), {PressedIn} pressed to the enlistment on being called; {BotSquad.Defended} beats a member struck in melee was left on what struck it, {BotSquad.Excused} places not handed to a member running, binding its own wounds or raising one of ours, {BotSquad.RoadSpared} looks for a fight a delve's company skipped on its road, {BotSquad.Paced} turns a marching leader was slowed to for a member behind it (beats a member behind was not waited for, by what it held instead of the enlistment: {BotSquad.Unpaced()}); first blows on the company's enemy, by what the leader was doing when the company took it on: {BotSquad.Responsiveness()}; {BotSpoils.Describe()}";
    }

    /// <summary>
    /// Calls a squad together. Whoever calls it leads it.
    ///
    /// <para>
    /// A founder who cannot fight is refused, and that refusal is one of the first version's plainer lessons.
    /// Its rung order put "this is dangerous, call for help" <em>above</em> "I am running out of health", so a
    /// bot on its last few points would declare a company it could not itself take part in; the company
    /// counted only able fighters, found none, and disbanded in the same tick — and the same bot posted it
    /// again on the next. Whoever is in that state should be running, not recruiting.
    /// </para>
    /// </summary>
    public static BotSquad Form(IBotSquadMember leader)
    {
        if (leader?.Self is not { Deleted: false, Alive: true } || !leader.AbleToFight)
        {
            return null;
        }

        if (leader.Squad != null)
        {
            return leader.Squad;
        }

        // <b>A neutral class raises no company (01.10.2026, build 385).</b> Patrick: traders are neutral in wars, and a company is
        // the shape every war, levy and delve takes. See BotTrading.Neutral.
        if (BotTrading.Refuses(leader.Self))
        {
            return null;
        }

        var squad = new BotSquad(++_next, leader);

        _squads.Add(squad);
        Formed++;

        squad.Attach(leader);
        leader.Squad = squad;

        logger.Information("{Name} called a squad together", leader.Self.Name);

        return squad;
    }

    /// <summary>
    /// Puts a bot in a squad. The only way in, and therefore the only place the cap is enforced.
    /// </summary>
    /// <summary>Calls into a company refused because the bot was in a duel or held at the arenas (07.10.2026, arena27). See <see cref="Join"/>.</summary>
    public static long Duellists { get; private set; }

    public static bool Join(BotSquad squad, IBotSquadMember member)
    {
        if (squad == null || member?.Self is not { Deleted: false, Alive: true })
        {
            return false;
        }

        // <b>A company that has been taken apart is not a company, and every other test here passes for
        // one.</b> Dissolve drops the squad out of the list the timer walks and clears the Squad of everybody
        // in it, but the object goes on answering Count, Ceiling and Map exactly as before — through a leader
        // whose own Squad is now null. So a bot could fall in with a company that had ceased to exist one
        // second earlier, and then never get out of it, because nothing thinks about a squad that is not in
        // the list. See BotSquad.Disbanded for the two log lines that say so.
        if (squad.Disbanded)
        {
            Buried++;

            return false;
        }

        if (member.Squad == squad)
        {
            return true;
        }

        // <b>Nor does a duellist, or an entrant held at the arenas between its bouts (07.10.2026, arena27).</b> The championship's
        // 1v1 summons hit "with a company" 374 times on 06–07.10: the bracket ran an hour and companies took its entrants out of it.
        // The arena draws only free bots and holds them from the port to the end of their part; this door is how the hold is kept.
        // See BotDuel.Duelling.
        if (BotDuel.Duelling(member.Self))
        {
            Duellists++;

            return false;
        }

        // <b>Nor does a neutral class fall in with one (01.10.2026, build 385).</b> The one door in, so every caller — the hunt's
        // enlistment, a levy, a press, a delve's call, a war company — is answered here. See BotTrading.Neutral.
        if (BotTrading.Refuses(member.Self))
        {
            return false;
        }

        // The company's own ceiling rather than the shard's, because a harrowing was ordered six strong and
        // a muster is worth five. See BotSquad.Ceiling: it is MaxSize for everybody who does not ask.
        if (member.Squad != null || squad.Count >= squad.Ceiling || squad.Map != member.Self.Map)
        {
            return false;
        }

        // <b>And near the company, which this door never asked.</b> On build 42 Faron Ashdown, by Britain, was a member
        // of Squad 1 — Roderic's delve party, taken into the Orc Caves at 23:42:00 on 14.09.2026 — and was sent to a
        // station there ten times, "no way from here", until the population carried him home; when Roderic fell
        // underground the company passed to him. Which caller let him in is not in the log. Asked of the leader rather
        // than the anchor, because in a fight the anchor is the creature, and Squad 1's was a dire wolf on the surface.
        if (!Reaches(squad, member.Self))
        {
            Distant++;

            return false;
        }

        // <b>Nobody at war with the company's leader falls in with it.</b> The first war company on
        // 13.09.2026 (Squad 18, The Lantern's) took Isolde of The Blade in at 22:54:15 - the hunt's
        // enlistment offers any fighting company with room, and a war company is one - and thirty seconds
        // later the company was "dealing with Kerrin", its own founder, because Isolde inside it was being
        // hit by him. The one door in is the one place to ask.
        // <b>Nor anybody of a guild hostile to the leader's (01.10.2026, Patrick: «only friendly bots, never hostile ones»).</b>
        // At war, or thought of at or below the war line either way (BotRegard.Hostile, build 370) — the delve's call asked it
        // since 370 and every other company's door asked only the war. Asked here, the one door, it holds for every caller.
        if (squad.Leader?.Self is Mobile lead && BotRegard.Hostile(lead, member.Self))
        {
            Enemies++;

            return false;
        }

        squad.Attach(member);
        member.Squad = squad;

        return true;
    }

    /// <summary>
    /// <b>Whether a company that is calling has called enough (01.10.2026).</b> At its ceiling; or at least <c>BotSquad.Usual</c>
    /// strong with <paramref name="strength"/> — the sum of its members' <c>BotThreat.Power</c>, kept by the caller as it calls — at
    /// least <c>BotSquad.CallOdds</c> times <paramref name="asked"/>, what the company is to meet. With the ceiling at twenty, a
    /// band against a troll still calls the five it always did, and one against a whipping vine, an ettin and a giant spider
    /// calls on until it is twice their weight.
    /// </summary>
    public static bool Enough(BotSquad squad, double strength, double asked)
    {
        if (squad.Count >= squad.Ceiling)
        {
            return true;
        }

        if (squad.Count < BotSquad.Usual)
        {
            return false;
        }

        if (asked <= 0.0 || strength >= asked * BotSquad.CallOdds)
        {
            Sufficed++;

            return true;
        }

        return false;
    }

    /// <summary>Counts one member called past the usual size because the fight asked more. See <see cref="Enough"/>.</summary>
    public static void Past() => CalledPast++;

    /// <summary>Members called past <c>BotSquad.Usual</c> because the fight asked more, and calls that stopped short of the ceiling.</summary>
    public static long CalledPast { get; private set; }

    public static long Sufficed { get; private set; }

    /// <summary>
    /// <b>A bot a company calls falls in; it does not go on with its own errand beside it (01.10.2026).</b> The delve's levy has
    /// pressed the enlistment since build 299 and the sweep's since 311; the band, the prowl's company and the scouting party only
    /// joined — so the member stood Bound with its own errand set aside, walked nowhere on a march (a march hands out no stations,
    /// build 313) and read on the page as foraging, brewing or unloading while its leader waited: Patrick watched Cedric's
    /// harrowing at 23:11 on 379 read "1 of 6 standing in it, 6 in the company" for five minutes. The one way in for all of them:
    /// only a bot whose work in hand the enlistment puts down rather than throws away (<see cref="Pressable"/>) is called, it is
    /// joined, and the enlistment is pressed on it with the company's place. <paramref name="force"/>: the sweep's levy, which has
    /// pressed whatever the bot held since build 311, keeps doing so. Returns whether it was taken.
    /// </summary>
    public static bool Enrol(BotSquad squad, IBotSquadMember other, Map map, Point3D where, string why, bool force = false)
    {
        if (!force && other is IBotWilful wilful && !Pressable(wilful))
        {
            LeftAtWork++;

            if (Carrying(wilful))
            {
                CarriersSpared++;
            }
            else if (Fighting(wilful))
            {
                FightersSpared++;
            }
            else if (Thieving(wilful))
            {
                ThievesSpared++;
            }

            return false;
        }

        if (!Join(squad, other))
        {
            return false;
        }

        if (other is IBotWilful called && BotWill.Press(called, new BotEnlist(squad, map, where, true), why))
        {
            PressedIn++;
        }

        return true;
    }

    /// <summary>
    /// Whether pressing the enlistment on this bot would put its work down to take up again rather than drop it: nothing in hand,
    /// work that resumes with no other already waiting, or its own self-defence (kept out of the share, and the company fights
    /// what it fights). Never one whose work answers to a company or a class already.
    /// </summary>
    public static bool Pressable(IBotWilful bot)
    {
        var resolve = bot?.Resolve;

        if (resolve?.Deed is not { } deed)
        {
            return true;
        }

        // Answering to a company or a class already — an enlistment on its way to another company is one (BotEnlist.Alongside).
        if (deed.Alongside)
        {
            return false;
        }

        if (deed is BotRescue { Own: true })
        {
            return true;
        }

        // A healer keeping its guild's open infirmary is left at it; the company calls the next (07.10.2026, life18b). See BotInfirmaries.Spares.
        if (BotInfirmaries.Spares(deed))
        {
            return false;
        }

        // <b>A carrier with another bot's goods in its pack is not called (06.10.2026, deliver16).</b> A company's call lasts as
        // long as the company (18-32 minutes on build 407), and a walk put down past BotWill.AsideCapMs is dropped: 46 of the 47
        // deliveries dropped "not taken up again after enlist" were called to a hunt or a harrowing with the buyer's ribs, leather
        // or bandages in the pack, and 19 more carriers died while their walk was put down. The walk is minutes; the company calls
        // the next bot. The sweep's levy (force) still presses whatever the bot holds.
        if (Carrying(bot))
        {
            return false;
        }

        // <b>Nor a hunter at its fight (07.10.2026, work18).</b> On build 416 (01:16-10:52) 103 hunts were dropped "not taken up again
        // after enlist" (of 133 hunt drops): 163 hunters were pressed into a company, 87 of them "fighting" their quarry. The fight is
        // seconds and decides itself; the company is half an hour, and the quarry was dead or hundreds of tiles off when it ended.
        if (Fighting(bot))
        {
            return false;
        }

        // <b>Nor a thief at its turn in a crowd (07.10.2026, loops18).</b> The carriers' rule, for the same reason: the turn is a
        // few minutes and the company half an hour. On build 418 Faelyn's turn by the bank in Vesper was dropped "pressed to enlist:
        // called to Ulfgar's party for the Orc Caves". See BotPickpocket.
        if (Thieving(bot))
        {
            return false;
        }

        return BotWill.Resume && deed.Resumes && resolve.Paused == null;
    }

    /// <summary>Whether a hunter at its fight is left to it by a company's call, a harrowing and a delve's party (07.10.2026, work18).</summary>
    public static bool SpareFighters { get; set; } = true;

    /// <summary>Whether the work in hand is a hunt at its fight, the quarry within reach and standing. False with <see cref="SpareFighters"/> off.</summary>
    public static bool Fighting(IBotWilful bot) => SpareFighters && bot?.Resolve?.Deed is BotSlay { Engaged: true };

    /// <summary>Company calls, harrowings and delve parties that passed a hunter at its fight by. Of <see cref="LeftAtWork"/> for a company's call.</summary>
    public static long FightersSpared { get; private set; }

    /// <summary>A levy passed a hunter at its fight by.</summary>
    public static void SpareFighter() => FightersSpared++;

    /// <summary>Whether a thief at its turn in a crowd is left to it by a company's call, a harrowing and a delve's party (07.10.2026, loops18).</summary>
    public static bool SpareThieves { get; set; } = true;

    /// <summary>Whether the work in hand is a thief's turn in a crowd by a bank. False with <see cref="SpareThieves"/> off.</summary>
    public static bool Thieving(IBotWilful bot) => SpareThieves && bot?.Resolve?.Deed is BotPickpocket;

    /// <summary>Company calls, harrowings and delve parties that passed a thief at its turn by. Of <see cref="LeftAtWork"/> for a company's call.</summary>
    public static long ThievesSpared { get; private set; }

    /// <summary>A levy passed a thief at its turn by.</summary>
    public static void SpareThief() => ThievesSpared++;

    /// <summary>
    /// Whether a carrier holding another bot's goods is left at its walk by a company's call, a harrowing and a delve's party
    /// (06.10.2026, deliver16). A switch: off, carriers are pressed as before (the companies metric's guard, march10).
    /// </summary>
    public static bool SpareCarriers { get; set; } = true;

    /// <summary>
    /// Whether the work in hand carries goods somebody else has paid for: a delivery, a trader's haul, or a guild's supply run with
    /// its batch bought (12.10.2026, w2, review A — the run resumes now, so an ordinary call would walk the guild's goods into a
    /// thirty-minute company and could leave them on a corpse; before the purchase there is nothing to lose and the call wins).
    /// False with <see cref="SpareCarriers"/> off.
    /// </summary>
    public static bool Carrying(IBotWilful bot) => SpareCarriers && bot?.Resolve?.Deed is BotDeliver or BotHaul or BotSupply { Bought: > 0 };

    /// <summary>Company calls that passed a carrier holding another bot's goods by. Of <see cref="LeftAtWork"/>.</summary>
    public static long CarriersSpared { get; private set; }

    /// <summary>A levy (a harrowing, a delve's party) passed a carrier by. Counted with <see cref="CarriersSpared"/>.</summary>
    public static void Spare() => CarriersSpared++;

    /// <summary>Bots within a company's call passed over for work the enlistment would have cost them, and bots pressed to it.</summary>
    public static long LeftAtWork { get; private set; }

    public static long PressedIn { get; private set; }

    /// <summary>
    /// Takes a bot out. The road is not touched here, and the last station the company gave it stays at the bottom of
    /// it: a station is written with <c>BotJourney.Rebase</c> into the same slot as the bot's own walk, so there is no
    /// errand underneath to fall back to. <c>BotWill</c> sends the bot's own walk again when the bottom of the road is no
    /// longer that walk; until 14.09.2026 it did not, and a bot let go on a roof by Britain's stables stood on its old
    /// "sweep", one tile away and so arrived, for four minutes.
    /// </summary>
    public static void Leave(IBotSquadMember member)
    {
        var squad = member?.Squad;

        if (squad == null)
        {
            return;
        }

        squad.Detach(member);
        member.Squad = null;
    }

    /// <summary>The squad this bot is in, or null.</summary>
    public static BotSquad Of(IBotSquadMember member) => member?.Squad;

    /// <summary>Whether these two are in the same squad. Used wherever "one of ours" has to mean something.</summary>
    public static bool Together(IBotSquadMember a, IBotSquadMember b) =>
        a?.Squad != null && ReferenceEquals(a.Squad, b?.Squad);

    /// <summary>
    /// One of ours has been set upon. This is the whole of the shared mind, and it is one line of consequence:
    /// the squad now has a focus, and the formation now anchors on whoever was hit.
    ///
    /// Nobody is asked to come and help. Every station is derived from the anchor, so the moment the anchor
    /// moves onto the member under attack, every other member is already walking towards it — including the
    /// ones a hundred feet away on a sweep, whose patches were derived from that same anchor.
    /// </summary>
    public static void Note(IBotSquadMember member, Mobile attacker)
    {
        var squad = member?.Squad;

        if (squad == null || attacker is not { Deleted: false, Alive: true })
        {
            return;
        }

        // <b>A bot of a guild we are not at war with is not an enemy, whatever just hit us.</b> Splash from
        // an explosion potion and the edge of an area spell land on friends as readily as on the quarry,
        // and on 13.09.2026 company 19 — sixteen bots, five guilds, no war standing — was handed Leofric,
        // Rhiannon, Fendrel, Kestrel and Wynn to deal with inside a quarter of a minute, every one of them
        // a bot of ours. A company that turns on a friend for a stray blow is a company killing the
        // population it was raised to protect; the friend is passed over, and the count says how often.
        if (attacker is BotMobile other && member.Self is Mobile self && !BotRegard.AtWar(self, other))
        {
            Friendly++;

            return;
        }

        // And never one of its own, whatever the guilds say: a company is the one thing on this shard that
        // outranks a war, or it is not a company.
        if (attacker is IBotSquadMember { Squad: { } theirs } && ReferenceEquals(theirs, squad))
        {
            Inside++;

            return;
        }

        // <b>A company already fighting keeps its enemy; the squad's own beat decides whether a stronger attacker takes it off it
        // (01.10.2026).</b> Build 359 turned the company to whatever struck the leader in melee, and the rule below turned it to the
        // strongest thing round any member struck; the two fought each other — 760 of 1324 engagements on 30.09 were switches
        // away from an enemy still standing, 375 of them straight back to the one before. The company's enemy is now the strongest
        // thing attacking it, the leader's call breaking a tie, held against flicker: see BotSquad.Retarget, which runs every beat
        // over the members' aggressor lists. A blow here only answers the question when the company has no enemy at all.
        // <b>A blow on a member while the company forms up short of its enemy ends the muster: it engages now (06.10.2026,
        // company16).</b> Whether the striker is the enemy mustered against or another thing, the company is in a fight.
        if (squad.Mustering)
        {
            squad.Struck(attacker);
        }

        if (squad.Focus is { Deleted: false, Alive: true } focus)
        {
            // <b>Except to protect a member being killed by something stronger than its enemy (06.10.2026, react17).</b> The
            // beat's retargeting waits out the hold against flicker (FocusHoldMs) and its own beat; "its company turned on it"
            // answered a member's cry at a median of fifteen seconds on build 404. A member under BotCry.DireAt of its health
            // or failing, struck by a thing at least Switch times the company's enemy, has the company turn now. No flicker
            // can come of it: turning back would need the old enemy to be Switch times the new, which it is not.
            //
            // <b>Or by anything at all while the company's enemy is attacking nobody of ours (company16).</b> Of 181 deaths to a
            // thing other than the company's enemy on 405–409, the rule above covered 48 and fired three times: the member under
            // DireAt was mostly being killed by something priced under the enemy. An enemy that is on none of us can wait; one
            // that is on one of us keeps the company, and the member's own near turn on the striker (BotSquad.Peel). Judged at
            // the withdrawal's odds, not the choice's: this is defence.
            if (Protects && member.Self is { HitsMax: > 0 } hurt
                && (hurt.Hits < hurt.HitsMax * BotCry.DireAt || BotLadder.Failing(hurt))
                && (BotThreat.Power(attacker) >= BotThreat.Power(focus) * Switch || focus.Combatant is not IBotSquadMember { Squad: { } on } || !ReferenceEquals(on, squad))
                && squad.StrongestAttacking(attacker) is { } stronger && (ReferenceEquals(stronger, attacker) || BotThreat.Power(attacker) >= BotThreat.Power(stronger) * BotSquad.TieShare)
                && !BotQuarry.Shunned(attacker) && !squad.Bleeding(attacker)
                && (BotSquad.Caster(attacker) || !squad.Beyond(attacker, 0.0, BotSquad.WithdrawOdds) && !BotSquad.Shuns(attacker, squad.Able())))
            {
                if (BotThreat.Power(attacker) < BotThreat.Power(focus) * Switch)
                {
                    ProtectedIdle++;
                }

                squad.Engage(attacker, member);
                Protected++;

                return;
            }

            Steady++;

            return;
        }

        // The strongest thing attacking the company, not the thing that happened to land this blow. On a graveyard the nearest
        // hostile is always a skeleton, and the first version's companies formed against the nearest: six bots would declare a
        // band against a skeleton, commit at once because a skeleton is trivial, all go and kill it — while the lich that was
        // actually killing them carried on casting. The lich is an attacker of ours as soon as it casts at one of us.
        var pick = squad.StrongestAttacking(attacker);

        // A member set upon far from its leader (a levied one on its way to the muster) fights for itself; the company does not
        // walk off its muster or its road to it. See BotSquad.AnswerReach.
        if (pick == null)
        {
            Afield++;

            return;
        }

        // <b>The note the company had just written about this very creature, read by nobody.</b> Breaking
        // off as hopeless shuns the quarry for a quarter of an hour precisely so that nobody goes back to
        // it — and <c>BotQuarry.Company</c>, the finder <c>Hunt</c> uses, honours that. This path does not
        // go through a finder, so the creature the company gave up on two seconds ago hits somebody, and the
        // whole company is handed it again. Squad 17 on 03.09.2026: broke off from a troll with three of
        // three standing on it and its health untouched, re-engaged the same troll within the second, broke
        // off again, twice over, and every member of it was frozen for the duration of both.
        //
        // Passed over rather than fought: the members still defend themselves, because being hit is their
        // own business and always has been. What is refused here is committing the *company* to a fight it
        // has already proved it cannot win.
        if (BotQuarry.Shunned(pick))
        {
            Rebuffs++;

            return;
        }

        // <b>Nor one it cannot beat (03.10.2026).</b> Taken on here, a wandering plague beast was the company's enemy for the
        // minute it took to break off, and then nobody's. The squad's own beat withdraws from it. A caster is fought, as before
        // (Patrick, 30.09: fight it to the end). See BotSquad.Overmatched. Nor, caster or not, the thing the company has just pulled
        // back from (company16): the members defend themselves and the company holds on its leader. See BotSquad.Bleed.
        if (squad.Bleeding(pick) || !BotSquad.Caster(pick) && (squad.Beyond(pick, 0.0, BotSquad.WithdrawOdds) || BotSquad.Shuns(pick, squad.Able())))
        {
            squad.ShyOf(pick);

            return;
        }

        squad.Engage(pick, member);
        Rescues++;
    }

    /// <summary>Times a company was handed a creature it had already given up on, and passed. For the summary.</summary>
    /// <summary>Whether a company answers an event between its beats. A switch, for a running shard. See <see cref="Hurry"/>.</summary>
    public static bool AnswersAtOnce { get; set; } = true;

    /// <summary>Answers given between beats, and companies with no enemy that took on a creature seen coming for a member before its blow.</summary>
    public static long Hurries { get; private set; }

    public static long Comings { get; private set; }

    private static readonly List<BotSquad> _due = [];

    /// <summary>
    /// Asks a company to answer at the population's next look, a tenth of a second off, instead of at its own next beat. Asked
    /// when it takes on an enemy, when a member becomes a patient, and when a member falls under its failing line. Free to ask
    /// twice. See <see cref="BotSquad.AnswerNow"/>.
    /// </summary>
    public static void Hurry(BotSquad squad)
    {
        if (!AnswersAtOnce || squad == null || squad.Due || squad.Disbanded)
        {
            return;
        }

        squad.Due = true;
        _due.Add(squad);
    }

    /// <summary>The companies asked since the last look, answered. Called by <c>BotBeat.Look</c>.</summary>
    public static void Answer()
    {
        if (_due.Count == 0)
        {
            return;
        }

        // By index over a list that may grow under the loop: an answer may ask for another.
        for (var i = 0; i < _due.Count; i++)
        {
            var squad = _due[i];

            if (!squad.Due)
            {
                continue;
            }

            try
            {
                squad.AnswerNow();
                Hurries++;
            }
            catch (Exception e)
            {
                squad.Due = false;

                logger.Error(e, "Squad {Id} threw answering between its beats; it carries on at its next", squad.Id);
            }
        }

        _due.Clear();
    }

    /// <summary>
    /// <b>A creature is coming for a member and has not struck yet: the member tells its company in that beat (06.10.2026,
    /// react16).</b> <see cref="Note"/> is the same telling at the blow, and this is its rule a few seconds sooner: a company
    /// that has an enemy keeps it (its own beat weighs a stronger attacker against the hold); one that has none takes on the
    /// strongest thing attacking it, or this creature when it stands where the company can answer, unless the company has
    /// given it up or it is beyond the company and no caster. Of 88 first blows that landed on build 403 with the warning
    /// unused, the stopwatch could not say how many fell on a member; `Reflexes:` now does.
    /// </summary>
    public static void Coming(IBotSquadMember member, Mobile creature)
    {
        var squad = member?.Squad;

        if (!AnswersAtOnce || squad == null || creature is not { Deleted: false, Alive: true })
        {
            return;
        }

        // Coming for a member of a company forming up short of its enemy: the muster is over (company16). See BotSquad.Struck.
        if (squad.Mustering && squad.Focus is { Deleted: false, Alive: true })
        {
            squad.Struck(creature);

            return;
        }

        if (squad.Focus is { Deleted: false, Alive: true })
        {
            return;
        }

        var pick = squad.StrongestAttacking(creature);

        if (pick == null || BotQuarry.Shunned(pick) || squad.Bleeding(pick))
        {
            return;
        }

        // Defence, judged at the withdrawal's odds (company16): the thing is coming whether the company likes the odds or not.
        if (!BotSquad.Caster(pick) && (squad.Beyond(pick, 0.0, BotSquad.WithdrawOdds) || BotSquad.Shuns(pick, squad.Able())))
        {
            squad.ShyOf(pick);

            return;
        }

        squad.Engage(pick, member);
        Comings++;
    }

    /// <summary>The companies' answers between beats, for the <c>Reflexes:</c> line.</summary>
    public static string DescribeAnswers() =>
        $"{Hurries} answers by a company between its one-second beats (an enemy just taken on, a member become a patient or failing: its members turned and its medics asked at once), {Comings} companies with no enemy took on a creature seen coming for a member before its blow, {Protected} turned at a blow to protect a member under {BotCry.DireAt:P0} of its health ({Protected - ProtectedIdle} from a thing {Switch:F1} times their enemy, {ProtectedIdle} from a weaker thing while their enemy was on none of them); {BotSquad.Guests} wounded bots not of the company brought to its medics with no enemy standing, {BotSquad.GuestHeals} heals its medics chose one for";

    /// <summary>Whether a company turns at once to protect a dire member from something stronger than its enemy. A switch. See <see cref="Note"/>.</summary>
    public static bool Protects { get; set; } = true;

    /// <summary>Companies turned at a blow to protect a dire member. See <see cref="Note"/>.</summary>
    public static long Protected { get; private set; }

    /// <summary>Of those, turned on a thing weaker than the company's enemy because the enemy was attacking nobody of ours (company16).</summary>
    public static long ProtectedIdle { get; private set; }

    public static long Rebuffs { get; private set; }

    /// <summary>
    /// Blows on a member of a company with no enemy, from something standing further than <c>BotSquad.AnswerReach</c> from its
    /// leader: the member fights for itself and the company stays where it is. See <see cref="Note"/>.
    /// </summary>
    public static long Afield { get; private set; }

    /// <summary>
    /// How far from a company's leader a bot may stand and still be taken in.
    ///
    /// <para>
    /// <b>Above every radius a caller gathers from, and far below the distance to anywhere else.</b> A delve and a
    /// harrowing call bots in from two hundred tiles round the muster the leader stands at; a band, a sweep, a scouting
    /// party and an enlistment from forty or less. Every dungeon on this facet lies thousands of tiles from the island,
    /// on the same map, so the facet test in <see cref="Join"/> never told the two apart.
    /// </para>
    /// </summary>
    public static int JoinReach { get; set; } = 400;

    /// <summary>Bots refused a place for standing further than <see cref="JoinReach"/> from the leader.</summary>
    public static long Distant { get; private set; }

    /// <summary>
    /// Whether a bot stands near enough a company's leader to be let in: the question <see cref="Join"/> asks at the door
    /// about distance, and the one anything offering a walk to a company has to ask before it offers. See <c>BotFeud.Rally</c>.
    /// </summary>
    public static bool Reaches(BotSquad squad, Mobile body) =>
        body != null && (squad?.Leader?.Self is not Mobile head || body.InRange(head.Location, JoinReach));

    /// <summary>Blows from a bot of a guild not at war with ours, passed over as friendly fire. See <see cref="Note"/>.</summary>
    public static long Friendly { get; private set; }

    /// <summary>
    /// How far around a member the squad looks when working out what is attacking it. The whole company is
    /// counted at this range, so it has to be wide enough that the far knot of a sweep is included and narrow
    /// enough that the next field is not.
    /// </summary>
    public static int Reach { get; set; } = 12;

    /// <summary>
    /// How much stronger (by <c>BotThreat.Power</c>) an attacker must be than the enemy a company is already on, when that enemy
    /// is attacking it too, before the company leaves it for the attacker. See <c>BotSquad.Retarget</c> (01.10.2026; before, the
    /// same test was asked of the strongest thing round whichever member a blow landed on, in <see cref="Note"/>).
    /// </summary>
    public static double Switch { get; set; } = 1.5;

    /// <summary>Blows on a member that left the company on the enemy it was already fighting. For the summary.</summary>
    public static long Steady { get; private set; }

    /// <summary>
    /// Whether the holder of a tile should give it up to whoever is asking.
    ///
    /// <para>
    /// Two trees with a gap between them, a mage standing in the gap, something hostile on the far side. The
    /// mage dies there in seconds; the blade behind it would take minutes. So the blade asks, and the mage
    /// yields — not because anything recognised a chokepoint, but because the tile belongs to the rank that
    /// stands nearer the threat, and the formation already says which rank that is.
    /// </para>
    ///
    /// <para>
    /// It does not work the other way round. A mage cannot move a blade: the blade is where it belongs.
    /// </para>
    /// </summary>
    public static bool ShouldYield(IBotSquadMember holder, Mobile asker)
    {
        if (holder?.Self is not { Deleted: false, Alive: true } body || asker is not IBotSquadMember member)
        {
            return false;
        }

        // Inside one company the formation decides, because there the tiles mean something: a shield wall
        // that reshuffles itself every time somebody wants past is not a wall.
        //
        // <b>While it is a shield wall.</b> A company that is not fighting holds no wall — its stations are
        // only where everybody happens to be standing — so the rank rule there protects nothing and is four
        // bots refusing each other in a field. Edda, Faron, Gerda and Calla stood at (1298-1303, 1070-1073)
        // for eight minutes on 03.09.2026, every one of them reported "in a company", every one holding an
        // errand of its own that the company had no opinion about, and none outranking any other. In a fight
        // this branch still runs and rank still decides, exactly as before.
        if (holder.Squad is { Stance: BotSquadStance.Fighting } && ReferenceEquals(holder.Squad, member.Squad))
        {
            if (!BotFormation.OutranksFor(member, holder))
            {
                return false;
            }

            Yields++;

            return true;
        }

        // <b>And everybody else gets out of the way, which they flatly refused to do until now.</b> This
        // began and ended at "same company, and the asker outranks you" — so two bots from different
        // companies, or the twenty-odd in none at all, would stand facing each other for ever. With
        // thirty-four bots crowding one training field and one bank counter it showed up as a third of every
        // step on the shard being refused by the engine (11028 of 33015 in one window) and four bots at a
        // time reported stuck by the stall watch, none of them overloaded and none of them lost.
        //
        // The one standing still yields to the one who is going somewhere. That is the whole rule and it is
        // what a person would do in a doorway. It cannot deadlock only because the test below is motion
        // rather than intent: a bot that has not moved lately has nothing to lose by a step, and two that
        // have are not in each other's way for long.
        // <b>Moving, not Walking, and the difference is the whole of whether this rule can deadlock.</b>
        // Walking asks whether the bot holds a plan with tiles left in it, which a bot whose every step is
        // refused does for as long as it stands there. So the rule that reads "the one standing still yields
        // to the one who is going somewhere" was in fact "nobody with a plan yields to anybody", and four
        // bots with plans through each other's tiles held that position for four minutes at (1344, 878) on
        // 03.09.2026. Moving asks whether a step has actually been taken lately, which is what the rule
        // always meant and what makes its own argument true: two bots both moving are both moving.
        if (body is BotMobile { Journey.Moving: true })
        {
            return false;
        }

        Yields++;

        return true;
    }

    /// <summary>
    /// Which way somebody giving up a tile should step: away from whatever the squad is dealing with, or away
    /// from the asker when there is nothing.
    ///
    /// Not merely to clear the tile — to clear it in the direction the yielder wanted to go anyway. A caster
    /// pushed out of a gap should end up behind the line, which is its own station.
    /// </summary>
    public static Direction YieldAwayFrom(IBotSquadMember holder, Mobile asker)
    {
        var body = holder?.Self;

        if (body == null)
        {
            return Direction.North;
        }

        var from = holder.Squad?.Focus ?? asker;

        if (from == null)
        {
            return body.Direction;
        }

        // Four points round the compass from whatever it is backing away from.
        var towards = (int)(body.GetDirectionTo(from) & Direction.Mask);

        return (Direction)((towards + 4) & 0x7);
    }

    /// <summary>
    /// How often the state of companies is said out loud.
    ///
    /// <para>
    /// <b>"No squads formed" is not a measurement, and for two evenings it was all there was.</b> The count
    /// went from fifteen in twenty minutes to none, twice over, and nothing in the log distinguished a
    /// population with nothing worth ganging up on from one too scattered to gather two helpers — which want
    /// opposite fixes. This says both, beside each other, every five minutes.
    /// </para>
    /// </summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static long _saidTick;

    /// <summary>One beat: every squad reconsiders itself, and the ones that are finished are cleared away.</summary>
    public static void Update()
    {
        if (Core.TickCount - _saidTick >= SayEveryMs)
        {
            _saidTick = Core.TickCount;

            logger.Information("Companies: {Standing}; {Muster}; {Bands}; {Enlist}", Describe(), BotMuster.Describe(), BotBand.Describe(), BotEnlister.Describe());

            // Two facts about fighting that had nowhere else to be said, on the only clock in this assembly
            // that ticks slowly enough to say them: who came to whose aid, and who is swinging its fists.
            logger.Information(
                "Arms: {Cries}, {Outmatched} rescues passed over for the crowd round the foe; {Hands}; {Scrolls}; {Mending}; {Salve}; {Standing}; {Hire}; {Flight}; {Brawls}; {Outlaws}; {Robbers}; {Patrols}; {Assailed} times a bot set upon by a red hit back",
                BotCry.Describe(),
                BotRescuer.Outmatched,
                BotArms.Describe(),
                BotArmoury.Describe(),
                BotMedic.Describe(),
                BotSalve.Describe() + ", " + BotSurgeon.Describe() + "; " + BotMend.Describe(),
                BotAttendant.Describe() + "; " + BotHouseCalls.Describe(),
                BotRetainer.Describe(),
                BotFugitive.Describe() + "; " + BotBolt.Describe(),
                BotBrawl.Describe(),
                BotOutlaw.Describe(),
                BotRobber.Describe(),
                BotManhunt.Describe(),
                BotDefender.Assailed
            );

            // The island's criminal record and The Shadow. See BotUnderworld.
            logger.Information("Underworld: {Underworld}", BotUnderworld.Describe());

            // The bow's own line. It earns one because "the archer never kites" survived two nights of being
            // blamed on other things, and a decision nobody counts is a decision nobody can argue with.
            logger.Information("Bows: {Kites}", BotSlay.Bows());

            // The board, from the asking side. Counters were written for both of these and printed nowhere,
            // which is the same fault as having none: "the board is empty" and "nobody has looked at the
            // board" are different facts and were producing the same silence.
            logger.Information(
                "Needs: {Gear}; {Metal}; {Forge}; {Tinker}; {Thread}; {Arrows}; {Bottles}; {Skillet}; {Stores}; {Filled} things were kept in a pack at the counter for a carrier to take to somebody's want, {Shed} lots were put in the bank box on the spot by bots too heavy to walk, {Exposed} trips were begun because a pack was worth more than a bot should be carrying about, {Hoarding} because it held more stones of somebody else's goods than it should, {KitTrips} because capped kinds rode over their number and {BankTrips} because it stood at a bank with surplus (offers), {Stored} lots of {Stowed} things went into bank boxes ({Boxless} times the box would not take them; {Unlotted} things the porter left out of its count for having no room in the box; {Overheld} units put away after waiting their time for a want near; {Unbanked}gp the bank refused for a full box),{Dumped} things nobody would buy were left on the ground by bots that could not walk ({Stranded} of these errands were finished with no counter known at all, {Cornered} were offered to a bot with nothing the market wants so that the dropping could be reached, and {Immovable} of those found nothing to drop either; {Treadmills} stand-still passes refused for leaving a bot no lighter than the last), and {Sent} kills were chosen because the board wanted what the carcass carries",
                BotUpkeep.Describe(),
                BotBullion.Describe(),
                BotSmith.Describe(),
                BotTinkerer.Describe() + "; bows: " + BotBowyer.Describe() + "; weaving: " + BotWeaver.Describe() + $"; {BotTailor.OwnCloth} sewing stints on the weaver's own cloth, {BotProvision.Cut} bandages cut instead of bought",
                BotTailor.Describe(),
                BotFletcher.Describe(),
                BotAlchemist.Describe() + "; scrolls: " + BotScribes.Describe(),
                BotCook.Describe(),
                BotStores.Describe(),
                BotUnload.Filled,
                BotUnload.Shed,
                BotUnload.Exposed,
                BotUnload.Hoarding,
                BotUnload.KitTrips,
                BotUnload.BankTrips,
                BotUnload.Stored,
                BotUnload.Stowed,
                BotUnload.Boxless,
                BotUnload.Unlotted,
                BotUnload.Overheld,
                BotUnload.Unbanked,
                BotUnload.Dumped,
                BotUnload.Stranded,
                BotUnload.Cornered,
                BotUnload.Immovable,
                BotUnload.Treadmills,
                BotQuarry.Sent
            );

            // <b>The market's own state, which went to a gump nobody opens and to the world reload.</b> That is
        // the same "which is to say nowhere" as BotGround.Describe, and it hid a whole class of fault: a
        // stall whose price has ratcheted to its floor and still not sold is never removed, because only an
        // empty stall is ever forgotten. Nobody could see how many of those there were.
        logger.Information("Market: {What}", BotMarket.Describe());

        // What the population is actually carrying about, which nothing has ever printed. See
        // BotUnload.Weighed: every other weight instrument on this shard is about bots already in trouble,
        // and a pack filling with things nobody wants stays under the ceiling the whole time it is doing it.
        logger.Information("Packs: {What}", BotUnload.Weighed());

        // <b>The ground the whole economy is dug out of, and it has never once been printed.</b>
            // BotGround.Describe existed and went to exactly two places: a gump nobody has open, and the
            // world reload — which is to say nowhere. So how much rock this island has, how much of it is
            // behind a wall and how much turned out to be a mirage were facts nobody could read. It is the
            // same fault the market's own summary had, and BotBeat.Summarise carries the note about it.
            logger.Information(
                "The ground: {What}; {Wood}; {Stables}",
                BotGround.Describe(),
                BotWoodsman.Describe(),
                BotStable.Describe()
            );
        }

        for (var i = _squads.Count - 1; i >= 0; i--)
        {
            var squad = _squads[i];

            string over;

            try
            {
                over = squad.Update();

                if (over == null)
                {
                    continue;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Squad {Id} threw while thinking; it has been disbanded", squad.Id);

                over = "it threw while thinking";
            }

            Dissolve(squad, over);
        }
    }

    /// <summary>
    /// Takes a whole company apart, for whoever raised it.
    ///
    /// <para>
    /// <b>Update's own rule is that whoever set the charge owns ending the company, and there was no way for
    /// an owner to do it.</b> BotScout.Disband says in its summary that it lets the party go, and what it had
    /// was Leave, which detaches one bot — the leader. The other five inherited a new leader with no errand
    /// and stood there: a company only dissolves itself at nought members or at one uncharged, and five bots
    /// on the Bound rung have no work of their own to walk away to. On 03.09.2026 every bot the stall watch
    /// caught standing still was reported "in a company", and the captain beside them "on its own".
    /// </para>
    /// </summary>
    public static void Disband(BotSquad squad, string why)
    {
        if (squad == null || !_squads.Contains(squad))
        {
            return;
        }

        Dissolve(squad, why);
    }

    private static void Dissolve(BotSquad squad, string why)
    {
        // First, so that a Join arriving in the same beat is refused rather than attaching to a corpse.
        squad.Bury();

        var members = squad.Members;

        for (var i = members.Count - 1; i >= 0; i--)
        {
            members[i].Squad = null;
        }

        _squads.Remove(squad);
        Disbanded++;

        logger.Information("Squad {Id} is no more: {Why}", squad.Id, why);
    }

    private sealed class SquadTimer : Timer
    {
        public SquadTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Update();
    }
}
