using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Who each guild at war has called its members onto, and how long the call stands.
///
/// <para>
/// <b>Written 10.09.2026 because a war meant nothing at all.</b> <c>BotRegard</c> could declare one, the
/// engine's own <c>Guild.IsWar</c> held it, and <c>BotRegard.AtWar</c> had not one caller anywhere in the
/// assembly — so five guilds could hate each other to the floor of the scale and go on trading. Patrick
/// watched a member of The Crown tell somebody "this is the land of The Crown, move along", get refused, and
/// walk away. His order: they are to call their guildmates and fight.
/// </para>
///
/// <para>
/// <b>A board rather than a squad, and that is the whole design decision.</b> The squad machinery is real
/// and complete, and every part of it is typed to a <c>BaseCreature</c> quarry — formation, stations, the
/// share-out of a corpse. Re-typing it for a bot would touch a dozen files to gain what one shared list of
/// "who we are all on" gains here: several members choosing the same enemy at the same time, which is what
/// calling your guildmates looks like from the outside. The squad's own work is unaffected and none of its
/// rules had to be loosened.
/// </para>
///
/// <para>
/// <b>Every call expires.</b> A quarrel that outlives the fight is a guild that hunts one bot for the rest
/// of the session; <see cref="CallMs"/> is short enough that the call has to be renewed by somebody actually
/// standing there, which is the only evidence that the fight is still happening.
/// </para>
/// </summary>
public static class BotFeud
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFeud));

    /// <summary>Whether a war means anybody swings at anybody. The rubber stamp for the whole idea.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How long a call on one enemy stands before it has to be renewed.
    ///
    /// Forty seconds: long enough for a mate two fields away to arrive, short enough that a guild whose
    /// quarry has walked off stops being a mob within the minute.
    /// </summary>
    public static int CallMs { get; set; } = 40000;

    /// <summary>
    /// How far a member will come when the guild calls.
    ///
    /// A hundred, up from sixty on 13.09.2026 with Patrick's order for more boldness: sixty was inside a
    /// single guild's yard and a half, so a member two fields away heard nothing. Still well inside a bot's
    /// ordinary roaming.
    /// </summary>
    public static int Answer { get; set; } = 100;

    /// <summary>
    /// How far a member will come to defend the guild's own ground. Most of the island.
    ///
    /// <para>
    /// <b>Patrick's order of 13.09.2026: if a guild's land is attacked, defending it comes first.</b> An
    /// enemy on the yard or on a claimed square, or claiming one, is a threat, and a threat is answered
    /// from further away than a quarrel a member happened to see — at a higher price too, see
    /// <c>BotQuarrel.DefendPrior</c>. A war fought entirely away from home is a war the home loses.
    /// </para>
    /// </summary>
    public static int Defend { get; set; } = 250;

    /// <summary>How long a sighting of an enemy on our ground stands before it has to be seen again.</summary>
    public static int ThreatMs { get; set; } = 60000;

    /// <summary>How often the wars are swept for enemies on anybody's ground.</summary>
    public static int WatchMs { get; set; } = 5000;

    /// <summary>Calls raised. One per enemy per guild, however many answer it.</summary>
    public static long Called { get; private set; }

    /// <summary>Members who took up somebody else's call rather than starting their own.</summary>
    public static long Answered { get; private set; }

    /// <summary>Threats seen: an enemy standing on our ground, or claiming it, while at war.</summary>
    public static long Threatened { get; private set; }

    /// <summary>Members sent to answer one.</summary>
    public static long Defended { get; private set; }

    /// <summary>What a guild is on, and when that was last renewed.</summary>
    private static readonly Dictionary<string, (BotMobile Whom, long When)> _calls = [];

    /// <summary>Who is on each guild's ground, and when that was last seen.</summary>
    private static readonly Dictionary<string, (BotMobile Whom, long When)> _threats = [];

    private static long _watched;

    /// <summary>
    /// The enemy standing on this guild's ground, or nothing.
    ///
    /// Freshness is the whole answer: a threat seen a minute ago and not since is an enemy who has walked
    /// off, and a guild chasing the memory of one is a guild not defending anything.
    /// </summary>
    public static BotMobile Threat(Guild ours)
    {
        if (ours == null || !_threats.TryGetValue(ours.Name, out var held))
        {
            return null;
        }

        if (Core.TickCount - (held.When + ThreatMs) >= 0 || held.Whom is not { Deleted: false, Alive: true })
        {
            _threats.Remove(ours.Name);

            return null;
        }

        return held.Whom;
    }

    /// <summary>
    /// The beat: for every war standing, whether anybody of either side is on the other's ground.
    ///
    /// <para>
    /// Asked of the wars rather than of every bot: two wars is four guild rolls of fifteen, a land lookup
    /// each, every five seconds — nothing. Asked of every bot on every beat it would be the bailiff's own
    /// area query eighty times a second for a rule that fires a few times an hour.
    /// </para>
    ///
    /// <para>
    /// <b>The ground is the yard and the claimed squares both</b>, because <c>BotLand.Holder</c> answers
    /// for both; and a claim being made on our square counts as a body on it, because the claimants are
    /// standing there by definition. See <c>BotClaim.On</c>.
    /// </para>
    /// </summary>
    public static void Watch()
    {
        if (!Running || !BotRegard.Warring)
        {
            return;
        }

        var now = Core.TickCount;

        if (now - (_watched + WatchMs) < 0)
        {
            return;
        }

        _watched = now;

        foreach (var war in BotWar.Standing)
        {
            Look(war.A, war.B, now);
            Look(war.B, war.A, now);
        }

        // The war companies: a company whose enemy is down is pointed at the next one on our ground or on
        // our call, and a company with nobody to fight and no war to fight in is let go - the charge comes
        // off, and the squad's own idle clock takes it apart within the minute.
        if (_companies.Count == 0)
        {
            return;
        }

        List<string> spent = null;

        foreach (var (name, company) in _companies)
        {
            if (!Standing(company))
            {
                (spent ??= []).Add(name);

                continue;
            }

            if (BaseGuild.FindByName(name) is not Guild ours)
            {
                (spent ??= []).Add(name);

                continue;
            }

            if (_musters.TryGetValue(company, out var muster))
            {
                if (!muster.Out)
                {
                    // An enemy on our own ground ends a muster at once: that is defence, and defence does not wait.
                    if (Threat(ours) != null)
                    {
                        muster.Out = true;
                        SetOut++;
                    }
                    else if (!Ready(ours, company, muster, now))
                    {
                        continue;
                    }
                }
                else if (company.Count < LeastToAttack && Threat(ours) == null
                         && company.Leader?.Self is { Deleted: false } head0 && !head0.InRange(muster.Where, Assembly))
                {
                    // <b>Too few left in the field to be a company: back to the muster, and out again together.</b> The dead
                    // rise at the seat and come back one at a time; a company of one or two in the field is where they
                    // were walking to, one at a time, and it is what the enemy was killing.
                    company.Disengage("too few left in the field; back to the muster");
                    muster.Out = false;
                    muster.Since = now;
                    FellBack++;

                    logger.Information(
                        "The war company of {Guild} falls back to muster again at ({X}, {Y}) with {Count} left",
                        name,
                        muster.Where.X,
                        muster.Where.Y,
                        company.Count
                    );

                    continue;
                }
            }

            if (company.Stance == BotSquadStance.Fighting && company.Focus is { Deleted: false, Alive: true })
            {
                continue;
            }

            var next = Threat(ours) ?? On(ours);

            // Nobody on our ground and nobody called: a company that has stood long enough goes and finds
            // the enemy. See Sortie.
            if (next == null && Sortie(ours) && company.Leader?.Self is { Deleted: false, Alive: true } head)
            {
                next = Foe(ours, head.Location, head.Map);

                if (next != null)
                {
                    Sorties++;
                }
            }

            if (next != null && next.Map == company.Map && Quarrel(company.Leader?.Self, next))
            {
                company.Engage(next, null);

                continue;
            }

            if (BotWar.Fighting(name) == 0)
            {
                (spent ??= []).Add(name);
            }
        }

        if (spent != null)
        {
            for (var i = 0; i < spent.Count; i++)
            {
                Disband(spent[i]);
            }
        }
    }

    /// <summary>Whether anybody of <paramref name="theirs"/> stands on the ground of <paramref name="ours"/>.</summary>
    private static void Look(string ours, string theirs, long now)
    {
        if (BaseGuild.FindByName(theirs) is not Guild enemy || enemy.Members == null)
        {
            return;
        }

        for (var i = 0; i < enemy.Members.Count; i++)
        {
            if (enemy.Members[i] is not BotMobile { Deleted: false, Alive: true } body || body.Map == null ||
                body.Map == Map.Internal)
            {
                continue;
            }

            var onOurs = BotLand.Holder(body.Map, body.Location) == ours;

            if (!onOurs)
            {
                // Standing in a square they are claiming off us counts the same: the claim is the attack.
                var bid = BotClaim.On(body.Map, body.Location);

                onOurs = bid != null && bid.Guild == theirs && bid.From == ours;
            }

            if (!onOurs)
            {
                continue;
            }

            if (!_threats.ContainsKey(ours))
            {
                Threatened++;
                _unanswered = now == 0 ? 1 : now;

                // <b>The guild is told at once, not at the next review.</b> A busy bot looks for work every
                // fifteen seconds (BotWill.ReviewMs); a threat that waited for that would be a threat
                // answered a quarter of a minute late by half the guild. Marking each member due costs one
                // flag apiece and is the same thing a finished job does to its own bot.
                if (BaseGuild.FindByName(ours) is Guild mine && mine.Members != null)
                {
                    for (var m = 0; m < mine.Members.Count; m++)
                    {
                        if (mine.Members[m] is IBotWilful { Resolve: { } resolve })
                        {
                            resolve.Due = true;
                        }
                    }
                }
            }

            _threats[ours] = (body, now);

            return;
        }
    }

    /// <summary>Scouts' warnings the war's muster took as the threat. See <see cref="Warn"/>.</summary>
    public static long Warned { get; private set; }

    /// <summary>
    /// A scout's warning (07.10.2026, officers20; see <c>BotGuildScout</c>): the leader of an enemy company seen gathering near the
    /// guild's seat is the threat the guild's war company musters against, before it is on the guild's ground. Only between two
    /// guilds at war; the threat is as fresh as any other (<see cref="ThreatMs"/>) and the members are told at once, as for a body
    /// on the ground. Returns whether the warning was taken.
    /// </summary>
    public static bool Warn(Guild ours, BotMobile whom)
    {
        if (!Running || ours?.Name == null || whom is not { Deleted: false, Alive: true } || whom.Guild is not Guild theirs
            || BotWar.Of(ours.Name, theirs.Name) == null)
        {
            return false;
        }

        var now = Core.TickCount;

        if (!_threats.ContainsKey(ours.Name))
        {
            Threatened++;
            _unanswered = now == 0 ? 1 : now;

            for (var m = 0; ours.Members != null && m < ours.Members.Count; m++)
            {
                if (ours.Members[m] is IBotWilful { Resolve: { } resolve })
                {
                    resolve.Due = true;
                }
            }
        }

        _threats[ours.Name] = (whom, now);
        Warned++;

        return true;
    }

    /// <summary>Counts a member sent to answer a threat, and the first one's reaction.</summary>
    public static void Stood()
    {
        Defended++;

        if (_unanswered != 0)
        {
            ReactionMs += Core.TickCount - _unanswered;
            Reactions++;
            _unanswered = 0;
        }
    }

    /// <summary>Milliseconds between a threat first being seen and the first member setting out, summed, and how many.</summary>
    public static long ReactionMs { get; private set; }

    public static long Reactions { get; private set; }

    /// <summary>When the newest threat was first seen and nobody has gone yet. Nought means nothing pending.</summary>
    private static long _unanswered;

    /// <summary>
    /// How many a war company may hold. A guild's worth, so a whole guild can fall in. Twenty since 01.10.2026, the companies'
    /// ceiling (BotSquad.MaxSize; Patrick: «companies may gather up to 20»).
    /// </summary>
    public static int CompanySize { get; set; } = 20;

    /// <summary>War companies formed, and members rallied to one.</summary>
    public static long Companies { get; private set; }

    public static long Rallied { get; private set; }

    /// <summary>Rallies not offered because the guild's company had its leader further off than <see cref="BotSquads.JoinReach"/>. See <see cref="Rally"/>.</summary>
    public static long Beyond { get; private set; }

    /// <summary>Rallies not offered because the company was fighting inside a pocket proved closed from where the bot stood. See <see cref="Rally"/>.</summary>
    public static long Pocketed { get; private set; }

    /// <summary>Each guild's war company, while it has one.</summary>
    private static readonly Dictionary<string, BotSquad> _companies = [];

    /// <summary>
    /// How long an attacking war company musters before it sets out with fewer than it wants, in milliseconds.
    ///
    /// <para>
    /// <b>Patrick's order of 24.09.2026: the attacking company gathers before the fight.</b> The first member to answer
    /// formed the company and pointed it at the enemy on the same beat; the leader walked at the enemy and everybody
    /// else walked at the leader from wherever they were, so a company arrived as a column of ones and twos. The five
    /// wars of 23–24.09 were lost that way: The Lantern's rally deaths came in "companies" of one thirteen times out of
    /// eighteen, and The Crown's dead rose at their own hall 110–150 tiles off and walked back alone, one at a time,
    /// into The Blade. An attacking company now forms up at the guild's seat — where its dead rise — and goes when it
    /// is whole.
    /// </para>
    /// </summary>
    public static int MusterMs { get; set; } = 90000;

    /// <summary>How near the muster point a member of the company must stand to count as gathered.</summary>
    public static int Assembly { get; set; } = 12;

    /// <summary>The fewest an attacking company sets out with, and the fewest it keeps in the field before it falls back to muster again.</summary>
    public static int LeastToAttack { get; set; } = 3;

    /// <summary>How far from a company already out a member must be to wait at the muster for the next wave rather than walk to it alone.</summary>
    public static int Straggle { get; set; } = 40;

    /// <summary>How many waiting at the muster make a wave.</summary>
    public static int Wave { get; set; } = 3;

    /// <summary>How long the first of them waits before a smaller wave goes, in milliseconds.</summary>
    public static int WaveMs { get; set; } = 60000;

    /// <summary>War companies that mustered before setting out, set out, fell back to muster again, and waves of stragglers sent from the muster.</summary>
    public static long Mustered { get; private set; }

    public static long SetOut { get; private set; }

    public static long SetOutWhole { get; private set; }

    public static long FellBack { get; private set; }

    public static long Waves { get; private set; }

    /// <summary>Where a war company forms up, since when, whether it has set out, and who is waiting there for the next wave.</summary>
    private sealed class Muster
    {
        private Point3D _where;

        /// <summary>
        /// Where the company forms up: its guild's hall as it stands now — Patrick's order of 26.09.2026, "the company may
        /// gather at the hall" — or, with no hall, where it was first set. Read afresh each time: The Lantern's company kept
        /// mustering at 1178,1500 for an hour after its hall had been carried to 1082,1402 (26.09.2026, 11:45–12:45).
        /// </summary>
        public Point3D Where
        {
            get => Guild != null && BotEstate.Held.TryGetValue(Guild, out var hall) && hall is { Deleted: false } ? hall.BanLocation : _where;
            set => _where = value;
        }

        public long Since;

        public bool Out;

        public string Guild;

        public readonly Dictionary<Serial, (long Arrived, long Seen)> Waiting = [];

        public long Released;

        public bool EverReleased;
    }

    private static readonly Dictionary<BotSquad, Muster> _musters = [];

    /// <summary>Whether this war company is still forming up and has not set out. See <see cref="MusterMs"/>.</summary>
    public static bool Mustering(BotSquad company) => company != null && _musters.TryGetValue(company, out var muster) && !muster.Out;

    /// <summary>Whether this war company has set out (or never had to muster). See <see cref="MusterMs"/>.</summary>
    public static bool Out(BotSquad company) => company == null || !_musters.TryGetValue(company, out var muster) || muster.Out;

    /// <summary>Where this war company musters, or nothing.</summary>
    public static Point3D MusterAt(BotSquad company) =>
        company != null && _musters.TryGetValue(company, out var muster) ? muster.Where : Point3D.Zero;

    /// <summary>Members of the company standing at its muster.</summary>
    public static int Gathered(BotSquad company)
    {
        if (company == null || !_musters.TryGetValue(company, out var muster))
        {
            return 0;
        }

        var here = 0;
        var members = company.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is { Deleted: false, Alive: true } body && body.Map == company.Map && body.InRange(muster.Where, Assembly))
            {
                here++;
            }
        }

        return here;
    }

    /// <summary>How many a mustering company waits for: what the war's score asks of the guild (BotWar.Wanted), never fewer than <see cref="LeastToAttack"/>.</summary>
    public static int Needed(Guild ours, BotSquad company) =>
        Math.Clamp(BotWar.Wanted(ours?.Name), LeastToAttack, Math.Max(LeastToAttack, company?.Ceiling ?? CompanySize));

    /// <summary>Stragglers waiting at this company's muster for the next wave.</summary>
    public static int Waiting(BotSquad company) =>
        company != null && _musters.TryGetValue(company, out var muster) ? muster.Waiting.Count : 0;

    /// <summary>
    /// Whether a straggler waiting at the muster goes now: a wave of <see cref="Wave"/> has gathered, or the first of those
    /// waiting has waited <see cref="WaveMs"/>, or a wave left after this one arrived. Called every beat by the waiting
    /// rally; each call says the bot is still there.
    /// </summary>
    public static bool WaveGoes(BotSquad company, Mobile body)
    {
        if (company == null || body == null || !_musters.TryGetValue(company, out var muster))
        {
            return true;
        }

        var now = Core.TickCount;
        var waiting = muster.Waiting;

        if (waiting.TryGetValue(body.Serial, out var mine))
        {
            mine = (mine.Arrived, now);
        }
        else
        {
            mine = (now, now);
        }

        waiting[body.Serial] = mine;

        if (muster.EverReleased && muster.Released - mine.Arrived >= 0)
        {
            waiting.Remove(body.Serial);

            return true;
        }

        List<Serial> stale = null;
        var oldest = now;

        foreach (var (serial, seen) in waiting)
        {
            if (now - seen.Seen > 5000)
            {
                (stale ??= []).Add(serial);

                continue;
            }

            if (seen.Arrived - oldest < 0)
            {
                oldest = seen.Arrived;
            }
        }

        for (var i = 0; stale != null && i < stale.Count; i++)
        {
            waiting.Remove(stale[i]);
        }

        if (waiting.Count < Wave && now - oldest < WaveMs)
        {
            return false;
        }

        muster.Released = now;
        muster.EverReleased = true;
        Waves++;

        logger.Information(
            "A wave of {Count} of {Guild}'s war company leaves the muster at ({X}, {Y}) for the company",
            waiting.Count,
            muster.Guild,
            muster.Where.X,
            muster.Where.Y
        );

        waiting.Remove(body.Serial);

        return true;
    }

    /// <summary>
    /// Where a war company musters: the guild's seat, where its members are born and its dead rise, wherever the founder
    /// happens to stand; the founder's own tile only for a guild with no seat.
    ///
    /// <para>
    /// <b>The founder's tile was the fallback beyond two hundred tiles, and it was the wrong place for everybody else.</b>
    /// In the first war on build 217 (The Blade on The Hammer, 17:33 on 24.09.2026) The Blade's company mustered at
    /// (1313, 2008) and The Needle's at (974, 1944), where Aric and Nessa Ashdown happened to be — five and six hundred
    /// tiles from their halls — and every straggler walked there to wait for a wave before walking to the fight.
    /// </para>
    /// </summary>
    public static Point3D MusterPoint(Guild ours, Mobile founder)
    {
        if (BotEstate.Hall(ours) is { Deleted: false } hall)
        {
            return hall.BanLocation;
        }

        var seat = BotSeat.Of(ours);

        return seat != Point3D.Zero ? seat : founder.Location;
    }

    /// <summary>
    /// Whether a mustering company is ready to set out: the whole of what is wanted stands at the muster, or the muster
    /// has run its clock and at least <see cref="LeastToAttack"/> stand there. Short at the bell, it keeps mustering.
    /// </summary>
    private static bool Ready(Guild ours, BotSquad company, Muster muster, long now)
    {
        var here = Gathered(company);
        var needed = Needed(ours, company);
        var waited = now - muster.Since;

        if (here < needed && (waited < MusterMs || here < LeastToAttack))
        {
            return false;
        }

        muster.Out = true;
        SetOut++;

        if (here >= needed)
        {
            SetOutWhole++;
        }

        logger.Information(
            "The war company of {Guild} sets out from ({X}, {Y}) with {Here} gathered of {Count} in it ({Needed} wanted) after {Waited:F0}s of mustering",
            ours.Name,
            muster.Where.X,
            muster.Where.Y,
            here,
            company.Count,
            needed,
            waited / 1000.0
        );

        return true;
    }

    /// <summary>
    /// How long a war may stand with no enemy seen before a company goes out to find one, in milliseconds.
    ///
    /// <para>
    /// <b>The fifth war of 14.09.2026 was declared between halls four hundred tiles apart, and nothing
    /// followed.</b> Every way into a war company was a sighting: an enemy on our ground, a guildmate's call,
    /// an enemy within twenty tiles. Two guilds that never meet are at war for three hours and then judged on
    /// whatever accidental blood there was. Patrick's order was a company and a real battle; this is the
    /// company going to find it.
    /// </para>
    /// </summary>
    public static int SortieMs { get; set; } = 180000;

    /// <summary>Times a company was pointed at an enemy nobody had seen. For the summary.</summary>
    public static long Sorties { get; private set; }

    /// <summary>Whether any war of this guild's has stood long enough, unanswered, for a sortie.</summary>
    public static bool Sortie(Guild ours)
    {
        if (ours == null || SortieMs <= 0)
        {
            return false;
        }

        var now = Core.TickCount;

        foreach (var war in BotWar.Standing)
        {
            if (war.Against(ours.Name) != null && now - (war.Began + SortieMs) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The nearest living enemy of this guild on the map, wherever it stands, or nothing.
    ///
    /// <para>
    /// Not one that is underground with a delving party: a company marching to a dungeon door would stand
    /// outside it, and the delve has its own clock. Towns are the caller's business, through <see cref="Quarrel"/>.
    /// </para>
    /// </summary>
    public static BotMobile Foe(Guild ours, Point3D from, Map map)
    {
        if (ours == null || map == null || map == Map.Internal)
        {
            return null;
        }

        BotMobile nearest = null;
        var best = int.MaxValue;

        foreach (var war in BotWar.Standing)
        {
            var theirs = war.Against(ours.Name);

            var enemy = theirs == null ? null : BotGuilds.Named(theirs) ?? BaseGuild.FindByName(theirs) as Guild;

            if (enemy?.Members == null)
            {
                continue;
            }

            for (var i = 0; i < enemy.Members.Count; i++)
            {
                if (enemy.Members[i] is not BotMobile { Deleted: false, Alive: true } foe || foe.Map != map)
                {
                    continue;
                }

                if (BotDelveParty.Delving(foe))
                {
                    continue;
                }

                var gap = Math.Max(Math.Abs(foe.X - from.X), Math.Abs(foe.Y - from.Y));

                if (gap < best)
                {
                    best = gap;
                    nearest = foe;
                }
            }
        }

        return nearest;
    }

    /// <summary>Counts one company sent out after an enemy nobody had seen.</summary>
    public static void Sortied() => Sorties++;

    /// <summary>
    /// Whether a company is still a company: not dissolved, and somebody still in it.
    ///
    /// <para>
    /// <b>Not "with a leader on its feet", which is what this asked until 03:00 on 14.09.2026.</b> A squad
    /// survives its leader's death — the strongest survivor inherits on the squad's next beat — but for the
    /// second or two between the death and that beat this said the company was gone: every member's rally
    /// ended "the war company is done", the heir was then holding whatever the auction gave it instead, the
    /// charge came off for that, the squad aged out inside ten seconds, and the founder, revived, formed
    /// the company again. In a war the leader dies about once a minute. Four formations in five minutes
    /// of the Blade–Lantern war, two of them of companies that had just been dissolved for this.
    /// </para>
    /// </summary>
    public static bool Standing(BotSquad company)
    {
        if (company == null || company.Count == 0)
        {
            return false;
        }

        var all = BotSquads.All;

        for (var i = 0; i < all.Count; i++)
        {
            if (ReferenceEquals(all[i], company))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The guild's war company, or nothing.</summary>
    public static BotSquad Company(Guild ours)
    {
        if (ours == null || !_companies.TryGetValue(ours.Name, out var company))
        {
            return null;
        }

        if (!Standing(company))
        {
            _companies.Remove(ours.Name);
            _musters.Remove(company);

            return null;
        }

        return company;
    }

    /// <summary>
    /// The errand for one member of a guild at war: form the war company if there is none, point it at the
    /// enemy, and fall in.
    ///
    /// <para>
    /// <b>Patrick's order of 13.09.2026: when a guild is attacked, a company forms and there is a real
    /// battle.</b> The company is the squad the hunts already use, charged so that it keeps its members
    /// through the lulls of a fight, with a ceiling of a whole guild, and its focus is the enemy bot; its own
    /// beat does the fighting. The first member forms it, the rest rally to it, and <see cref="Watch"/>
    /// points it at the next enemy and lets it go when there is none. Returns nothing when the bot cannot
    /// form or join one, and the caller falls back to a quarrel of its own.
    /// </para>
    /// </summary>
    public static BotDeed Rally(IBotWilful bot, Guild ours, BotMobile enemy, bool defending)
    {
        if (bot?.Self is not BotMobile body || bot is not IBotSquadMember member || ours == null || enemy == null)
        {
            return null;
        }

        var company = Company(ours);

        // <b>In some other company already — a hunt, most likely — whether or not the war company stands.</b>
        // It is not pulled out. Until 04:36 on 14.09.2026 this was asked only when there was no war company
        // yet; with one standing the rally was offered, taken, and ended on its first beat "fell in with
        // another company on the way" — twenty-two times in a minute for Lorcan under the sortie, which
        // proposes to everybody at war on every review.
        if (member.Squad != null && !ReferenceEquals(member.Squad, company))
        {
            return null;
        }

        if (company != null && company.Count >= company.Ceiling && !ReferenceEquals(member.Squad, company))
        {
            return null;
        }

        // <b>And near enough its leader to be let in, which the door has asked since build 43 and this did not.</b>
        // BotSquads.Join refuses a bot further than JoinReach from a company's leader; this offered the walk anyway, the
        // walk ended at the company's anchor - in a fight, whoever the company is on - and the door said no. On build 48,
        // between the summaries of 04:46 and 04:51 on 15.09.2026, the door counted 48 bots too far from their leader
        // against 1 in all the time before, and 48 rallies ended "the war company had no room by the time it arrived"
        // and were offered again at once: Sable six times inside a second, then Roderic, then Quenna, each until the
        // breaker rested them. One question, asked of one predicate in both places. A bot too far from its company
        // answers where it stands, which is the quarrel the caller already falls back to.
        // A company with a muster takes a member from anywhere: it walks to the muster, or waits there for a wave, rather
        // than quarrelling alone where it stands. See MusterMs.
        if (company != null && !ReferenceEquals(member.Squad, company) && !BotSquads.Reaches(company, body) && !_musters.ContainsKey(company))
        {
            Beyond++;

            return null;
        }

        // <b>And not to a company fighting inside ground the reach ledger has already proved closed from here, which the
        // enlistment asks and this did not.</b> The rally walks to the company's anchor, which in a fight is whoever the
        // company is on. On build 52, in the war of 08:25 on 15.09.2026, the roof at (1376, 1465, 30) by Britain Graveyard
        // was proved a pocket at 08:27:59, and from then on The Lantern's company fought on it: 32 of its stations were
        // dropped there, 6 walks "to the war company" and 8 rallies failed "no way through" - Jarek six times inside two
        // seconds until the breaker rested him. A dictionary lookup, the one BotEnlister.Nearest makes of the same anchor.
        if (company != null && !ReferenceEquals(member.Squad, company) && body.Map is { } map
            && BotReach.Ask(map, body.Location, company.Anchor, BotArrival.Within(BotSquad.PressReach - 1)) == BotReachVerdict.Sealed)
        {
            Pocketed++;

            return null;
        }

        Rallied++;

        return new BotRally(ours, enemy, body.Map, defending);
    }

    /// <summary>
    /// The guild's war company, formed round the member whose rally was actually taken, or the one that
    /// already stands.
    ///
    /// <para>
    /// <b>Formed here and not in <see cref="Rally"/>, and the difference was fifteen companies in ten
    /// minutes.</b> Until 02:45 on 14.09.2026 the proposer formed the company — the squad came into being
    /// while the auction was still being held, with the founder as its leader — and then the rally lost the
    /// auction to a reclaim or a stake at the founder, the founder was left leading a charged company while
    /// holding an errand that was not <c>Alongside</c>, the squad's own beat took the charge off it for
    /// that and dissolved a company of one, and every member already walking to it failed "the war company
    /// was gone before it got there" and proposed again. Hollis formed the Crown's company four times in
    /// six minutes. An offer is not an errand: nothing is formed until somebody is holding the rally.
    /// </para>
    /// </summary>
    public static BotSquad Form(IBotSquadMember member, Guild ours, BotMobile enemy, bool defending)
    {
        if (member?.Self is not BotMobile body || ours == null)
        {
            return null;
        }

        var company = Company(ours);

        if (company != null)
        {
            return company;
        }

        if (member.Squad != null)
        {
            return null;
        }

        company = BotSquads.Form(member);

        if (company == null)
        {
            return null;
        }

        company.Charged = true;
        company.Warring = true;
        company.Ceiling = CompanySize;
        _companies[ours.Name] = company;
        Companies++;

        // Defending our own ground does not wait; attacking does. Both keep the point, where stragglers wait for a wave.
        var muster = new Muster { Where = MusterPoint(ours, body), Since = Core.TickCount, Out = defending, Guild = ours.Name };
        _musters[company] = muster;

        if (!defending)
        {
            Mustered++;
        }

        logger.Information(
            "{Name} has formed the war company of {Guild} against {Enemy} of {Theirs}{Why}",
            body.Name,
            ours.Name,
            enemy?.Name ?? "nobody",
            (enemy?.Guild as Guild)?.Name ?? "nobody",
            defending ? ", on our own ground" : $", mustering it at ({muster.Where.X}, {muster.Where.Y})"
        );

        return company;
    }

    /// <summary>The guild's war is over, or its company has nothing left to fight: let the company go.</summary>
    public static void Disband(string guild)
    {
        if (guild == null || !_companies.TryGetValue(guild, out var company))
        {
            return;
        }

        _companies.Remove(guild);
        _musters.Remove(company);

        if (Standing(company))
        {
            company.Warring = false;
            company.Charged = false;
        }
    }

    /// <summary>
    /// Calls a guild onto one enemy, or renews the call if it is already on them.
    ///
    /// Renewing rather than replacing is deliberate: a second member arriving must not be able to swing the
    /// whole guild onto whoever it happens to have seen first, or a war becomes five bots each dragging the
    /// other four onto a different target and nobody ever reaching one.
    /// </summary>
    public static void Call(Guild ours, BotMobile whom)
    {
        if (ours == null || whom is not { Deleted: false })
        {
            return;
        }

        var now = Core.TickCount;

        if (_calls.TryGetValue(ours.Name, out var held) && held.Whom == whom && now - (held.When + CallMs) < 0)
        {
            _calls[ours.Name] = (whom, now);

            return;
        }

        _calls[ours.Name] = (whom, now);
        Called++;
    }

    /// <summary>Who this guild is on, or nothing if the call has run out or the enemy is gone.</summary>
    public static BotMobile On(Guild ours)
    {
        if (ours == null || !_calls.TryGetValue(ours.Name, out var held))
        {
            return null;
        }

        if (Core.TickCount - (held.When + CallMs) >= 0 || held.Whom is not { Deleted: false, Alive: true })
        {
            _calls.Remove(ours.Name);

            return null;
        }

        return held.Whom;
    }

    /// <summary>Counts a member that came to somebody else's call.</summary>
    public static void Came() => Answered++;

    /// <summary>The call is over, however it went.</summary>
    public static void Drop(Guild ours)
    {
        if (ours != null)
        {
            _calls.Remove(ours.Name);
        }
    }

    /// <summary>
    /// Whether these two may fight at all, which is four engine facts and not one.
    ///
    /// <para>
    /// A war is the only licence. The engine's notoriety makes a warring guild's member an <em>enemy</em>
    /// rather than an innocent, so the blow carries no criminal flag and no guard is called; strike anybody
    /// else and the attacker is a criminal, which on this shard means the town kills it and the population
    /// loses a bot to a rule nobody wrote down. A guarded region is refused for the same reason from the
    /// other side: even a lawful war is not to be fought under the guards' noses, where the engine's own
    /// rules about who may be struck get complicated and this population would be the one to find out how.
    /// </para>
    /// </summary>
    public static bool Quarrel(Mobile ours, Mobile theirs)
    {
        if (!Running || !BotRegard.Warring || ours == null || theirs == null || ours == theirs)
        {
            return false;
        }

        if (ours.Deleted || theirs.Deleted || !ours.Alive || !theirs.Alive || ours.Map != theirs.Map)
        {
            return false;
        }

        if (!BotRegard.AtWar(ours, theirs))
        {
            return false;
        }

        // A duellist, or an entrant held at the arenas, is the referee's until it is let go (07.10.2026, arena27): the waiting rings
        // hold entrants of guilds at war side by side, and a war's quarrel there would be a death at a bout that kills nobody.
        if (BotDuel.Duelling(ours) || BotDuel.Duelling(theirs))
        {
            return false;
        }

        if (ours.Region?.IsPartOf<Server.Regions.GuardedRegion>() == true)
        {
            return false;
        }

        return theirs.Region?.IsPartOf<Server.Regions.GuardedRegion>() != true && ours.CanBeHarmful(theirs, false);
    }

    /// <summary>Part of the estate's line.</summary>
    public static string Describe() =>
        Called + Threatened == 0
            ? "no guild has called anybody onto anybody"
            : $"{Called} calls raised against an enemy at war, {Answered} members came to one somebody else had raised, "
            + $"{Threatened} times an enemy was seen on a guild's own ground and {Defended} members went to it from up to {Defend} tiles, the first setting out {(Reactions > 0 ? ReactionMs / Reactions / 1000.0 : 0.0):F1}s after the sighting on average over {Reactions}; "
            + $"{Companies} war companies formed and {Rallied} members rallied to one, {Beyond} not called to one whose leader stood more than {BotSquads.JoinReach} tiles off, {Pocketed} not called to one fighting in a pocket proved closed from where they stood, {Sorties} times one was sent out after an enemy nobody had seen; "
            + $"{Mustered} attacking companies mustered before the fight (up to {MusterMs / 1000}s, setting out with what the score asks or at least {LeastToAttack}), {SetOut} set out ({SetOutWhole} whole), {FellBack} fell back to muster again with fewer than {LeastToAttack} in the field, {Waves} waves of stragglers sent from the muster ({Wave} or {WaveMs / 1000}s); {BotRally.Describe()}";

    public static void Forget()
    {
        _calls.Clear();
        _threats.Clear();
        _watched = 0;
        Called = 0;
        Answered = 0;
        Threatened = 0;
        Defended = 0;
        ReactionMs = 0;
        Reactions = 0;
        _unanswered = 0;
        Companies = 0;
        Rallied = 0;
        Beyond = 0;
        Pocketed = 0;
        _companies.Clear();
        _musters.Clear();
        Mustered = 0;
        SetOut = 0;
        SetOutWhole = 0;
        FellBack = 0;
        Waves = 0;
        Sorties = 0;
        BotRally.Forget();
    }
}
