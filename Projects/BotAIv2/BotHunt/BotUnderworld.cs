using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Multis;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// The island's criminal record, and The Shadow: the sixth guild, of thieves and brigands, founded by murderers.
///
/// <para>
/// <b>Patrick's order of 17.09.2026, evening:</b> the most skilled and cunning murderers may found their own guild of
/// thieves and brigands, no more than five, whose thoughts and work are robbery, hiding, blackmail and killing; and the price
/// of it is prison and pursuit. His choices when asked: it is founded when two bots have killed from hiding, its master is
/// the better at hiding, and the master takes in other killers up to five; it has a secret hideout far outside the towns,
/// where its members rise and hide, and which the Baron raids once a caught thief gives it away (build 114, with the
/// blackmail); it is The Shadow [SHD].
/// </para>
///
/// <para>
/// <b>Not one of <see cref="BotGuilds"/>' guilds, and that is deliberate.</b> Those are dealt out round the crafters at every
/// boot, fifteen at most and five at least, led by their maker, and a guild with nothing to show loses a member a minute to
/// <c>BotGuilds.Review</c>; a band of five murderers with no hall and no crafter would be emptied by the machinery meant to
/// keep a trade guild honest. So The Shadow is an engine guild kept here: made at founding, re-made at every boot after the
/// muster from the names this keeps (<see cref="BotUnderworldStore"/>), its members taken out of whatever guild the muster
/// dealt them. The engine's guild is what makes them allies to each other and keeps a robber off its brother
/// (<c>BotRobber.Prey</c>).
/// </para>
///
/// <para>
/// <b>The record is kept by name</b>, because the population is raised afresh at every boot: murders, how many of them struck
/// from hiding, robberies carried through, times caught. Nothing else on the shard remembered a murder past its red hour.
/// </para>
/// </summary>
public static class BotUnderworld
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotUnderworld));

    public const string GuildName = "The Shadow";

    public const string GuildAbbreviation = "SHD";

    /// <summary>Whether the guild is founded, kept and grown at all. The record is kept either way.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How many bots that have killed from hiding it takes to found the guild. Patrick's choice: two.</summary>
    public static int Founders { get; set; } = 2;

    /// <summary>The most members the guild has. Patrick's number.</summary>
    public static int MostMembers { get; set; } = 5;

    /// <summary>
    /// The murders from hiding a bot must have on its record to found or be taken in, beside the one crime every bandit has.
    ///
    /// <para>
    /// <b>Nought from one, by Patrick's order of 26.09.2026: "the first bandits leave their guilds at once and form a new
    /// one".</b> A killing from hiding asked for Hiding 30 and a victim walking into the wait; between 21.09 and 25.09 there
    /// was one murderer on the island's record and no Shadow at all. A robbery or a murder is what makes a bandit now.
    /// </para>
    /// </summary>
    public static int LeastUnseen { get; set; } = 0;

    /// <summary>How often the guild is looked at: founding, the hideout, taking somebody in. A minute.</summary>
    public static int SweepMs { get; set; } = 60000;

    /// <summary>How long the master waits between taking in one member and the next. Half an hour.</summary>
    public static int RecruitEveryMs { get; set; } = 1800000;

    /// <summary>The fewest steps of road from home the hideout may lie.</summary>
    public static int HideoutLeastRoad { get; set; } = 250;

    /// <summary>The most steps of road from home the hideout may lie.</summary>
    public static int HideoutMostRoad { get; set; } = 520;

    /// <summary>The chance a member of The Shadow rolls the thought of robbery, every <c>BotRobber.EveryMs</c>; everybody else rolls <c>BotRobber.Chance</c>.</summary>
    // A half from a quarter, and every five minutes instead of ten (BanditEveryMs), by the same order: "from now on most of
    // their motivation is banditry".
    public static double MemberRobChance { get; set; } = 0.5;

    /// <summary>How often a bandit — of The Shadow, or out of every guild for its crimes — rolls the thought of robbery.</summary>
    public static int BanditEveryMs { get; set; } = 300000;

    /// <summary>Bandits who walked out of their guild at their first crime.</summary>
    public static long Outcasts { get; private set; }

    /// <summary>Guilds The Shadow was set against as an enemy.</summary>
    public static long Enmities { get; private set; }

    /// <summary>The share of a member's criminal thoughts that are blackmail rather than robbery. See <c>BotRob.Demand</c>.</summary>
    public static double ExtortShare { get; set; } = 0.5;

    /// <summary>
    /// How much stronger than its mark a blackmailer must be, as a ratio of powers. Below one: a mark may be stronger, and
    /// then it most likely refuses and the thief goes away. A robber wants <c>BotRobber.Edge</c>.
    /// </summary>
    public static double ExtortEdge { get; set; } = 0.6;

    /// <summary>How long a raid may stand before the Baron may raise another on the same hideout. Fifteen minutes.</summary>
    public static int RaidTimeoutMs { get; set; } = 900000;

    /// <summary>
    /// What the city pays for the first murder proved against a bot, and what each murder after it adds.
    ///
    /// <para>
    /// <b>Patrick's order of 17.09.2026, night.</b> "The more proven murders on a bot, the more is paid for taking it:
    /// say 1000gp for the first and +500 for each after. If the Baron killed it, nobody is paid. If an ordinary bot
    /// killed it, the whole reward is that bot's. If a band of bots killed it, the reward is split between them."
    /// Proved means what this record holds — a body found and the killing entered here, not a suspicion.
    /// </para>
    /// </summary>
    public static int HeadFirst { get; set; } = 1000;

    public static int HeadNext { get; set; } = 500;

    /// <summary>How often a member is sent back to practise hiding, at the hideout; everybody else waits <c>BotSkulk.EveryMs</c>.</summary>
    public static int MemberPracticeEveryMs { get; set; } = 900000;

    /// <summary>One bot's record.</summary>
    public sealed class Sheet
    {
        public string Name;

        public int Murders;

        public int Unseen;

        public int Robberies;

        public int Extortions;

        public int Jailed;
    }

    private static readonly Dictionary<string, Sheet> _sheets = new(StringComparer.Ordinal);

    private static readonly List<string> _members = [];

    private static bool _founded;

    private static Point3D _hideout = Point3D.Zero;

    private static long _stashedTick;

    private static readonly Dictionary<string, int> _paid = [];

    private static long _fetchedTick;

    private static Guild _guild;

    private static long _sweptTick;

    private static long _recruitedTick;

    private static bool _recruitClock;

    private static bool _hideoutKnown;

    private static string _givenAwayBy;

    private static bool _raiding;

    private static long _raidTick;

    /// <summary>Hideouts given away by a caught member.</summary>
    public static long GivenAway { get; private set; }

    /// <summary>Hideouts burned after a raid.</summary>
    public static long Burned { get; private set; }

    /// <summary>Whether the Baron knows where The Shadow hides.</summary>
    public static bool HideoutKnown => _hideoutKnown;

    /// <summary>Times the guild was founded.</summary>
    public static long Founded { get; private set; }

    /// <summary>Members taken in after the founding.</summary>
    public static long Recruited { get; private set; }

    /// <summary>Members put back into the guild at a boot.</summary>
    public static long Reformed { get; private set; }

    /// <summary>Looks for a hideout that found nowhere fit.</summary>
    public static long NoHideout { get; private set; }

    /// <summary>Whether The Shadow exists.</summary>
    public static bool Exists => _founded;

    /// <summary>Where The Shadow hides, or <see cref="Point3D.Zero"/>.</summary>
    public static Point3D Hideout => _hideout;

    /// <summary>Whether this one is of The Shadow, asked of the engine's own guild record.</summary>
    /// <summary>
    /// Whether this guild is The Shadow.
    ///
    /// <para>
    /// <b>The band is an engine guild, and everything that works off engine guilds finds it.</b> It was kept out of
    /// <c>BotGuilds</c> so the trade guilds' machinery would leave it alone, and that is true of the muster and the
    /// review — but the estate asks a bot what guild it is in, not what list it is on. At 01:42:37 on 18.09.2026 Orin
    /// Ashdown "raised the hall of The Shadow at 1438,1410 for 5000gp off 1 members", the band laid claim to a square
    /// in the same second, and a minute later its fence was standing on that square holding it: a secret band with a
    /// house, a signboard and a landholding, paid for out of what it had stolen. Its home is the hideout, and the
    /// hideout is the whole point of it.
    /// </para>
    /// </summary>
    public static bool Band(Guild guild) => _founded && _guild is { Disbanded: false } && ReferenceEquals(guild, _guild);


    public static bool Member(Mobile m) => _founded && _guild is { Disbanded: false } && m?.Guild != null && ReferenceEquals(m.Guild, _guild);

    /// <summary>
    /// Whether this one is a bandit: a robbery or a murder on its record. A bandit keeps no honest company — no guild takes it
    /// in (<c>BotGuilds.Outside</c>) — and robbing is most of what it thinks about (<c>BotRobber</c>). Patrick, 26.09.2026.
    /// </summary>
    public static bool Outlawed(Mobile m) => m != null && Of(m.Name) is { } sheet && sheet.Murders + sheet.Robberies > 0;

    /// <summary>
    /// A member of The Shadow within <paramref name="range"/> of this one that it can see — not hiding, not in a cell, alive —
    /// or null. Walks the band's own roll, five names, not the map. See <c>BotFeuder</c>.
    /// </summary>
    public static BotMobile Near(Mobile from, int range)
    {
        if (!_founded || _guild is not { Disbanded: false } || from?.Map == null || _members.Count == 0 || Member(from))
        {
            return null;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (Named(_members[i]) is not { Deleted: false, Alive: true, Hidden: false } one || one.Map != from.Map
                || !from.InRange(one.Location, range) || !from.CanSee(one) || BotOutlaw.Jailed(one))
            {
                continue;
            }

            return one;
        }

        return null;
    }

    /// <summary>Times the band was found with nobody on its roll and dissolved. See <see cref="Beat"/>.</summary>
    public static long Dissolved { get; private set; }

    /// <summary>Whether anybody on the band's roll still exists: in the world, or out of it resting.</summary>
    private static bool Anybody()
    {
        for (var i = 0; i < _members.Count; i++)
        {
            if (Named(_members[i]) != null)
            {
                return true;
            }

            var away = BotPopulation.Away;

            for (var k = 0; k < away.Count; k++)
            {
                if (away[k] is { Deleted: false } resting && resting.Name == _members[i])
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Bandits on record: a robbery or a murder against the name.</summary>
    private static int Bandits()
    {
        var count = 0;

        foreach (var (_, sheet) in _sheets)
        {
            if (sheet.Murders + sheet.Robberies > 0)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>This bot's record, or null when it has none.</summary>
    public static Sheet Of(string name) => name != null && _sheets.TryGetValue(name, out var sheet) ? sheet : null;

    /// <summary>One bandit as the dashboard shows it: what is proved against it, what it is worth, and where it stands.</summary>
    public readonly record struct Wanted(
        string Name,
        string Trade,
        int Murders,
        int Unseen,
        int Robberies,
        int Extortions,
        int Price,
        bool Member,
        bool Fence,
        bool Red,
        bool Hunted,
        bool Known,
        Point3D Where,
        Map Map
    );

    /// <summary>
    /// The bandits at large, worst first: Patrick's order of 17.09.2026, "only the uncaught and the current ones; the
    /// caught and those who have served their time come off the list."
    ///
    /// <para>
    /// At large means red, wanted, or of The Shadow, and not in a cell. A bot taken to the cells drops off the list
    /// while it serves; when it is let out it comes back only if it is still one of those three things, which is what
    /// "current" means — a bot that did its time and went back to its trade is nobody's business any more, whatever
    /// its rap sheet still remembers.
    /// </para>
    /// </summary>
    public static List<Wanted> AtLarge()
    {
        List<Wanted> roll = [];
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || BotOutlaw.Jailed(bot))
            {
                continue;
            }

            var member = Member(bot);

            if (!member && !BotOutlaw.Outlaw(bot))
            {
                continue;
            }

            var sheet = Of(bot.Name);

            roll.Add(
                new Wanted(
                    bot.Name,
                    bot.Class?.Name ?? "?",
                    sheet?.Murders ?? 0,
                    sheet?.Unseen ?? 0,
                    sheet?.Robberies ?? 0,
                    sheet?.Extortions ?? 0,
                    Price(bot),
                    member,
                    BotFence.Is(bot),
                    BotOutlaw.IsRed(bot),
                    BotOutlaw.IsWanted(bot),
                    BotOutlaw.Known(bot),
                    bot.Location,
                    bot.Map
                )
            );
        }

        roll.Sort((a, b) => b.Price != a.Price ? b.Price.CompareTo(a.Price) : b.Murders.CompareTo(a.Murders));

        return roll;
    }

    /// <summary>What the city will pay for this bot laid low: nothing unless a murder is proved against it. See <see cref="HeadFirst"/>.</summary>
    public static int Price(Mobile m)
    {
        var murders = Of(m?.Name)?.Murders ?? 0;

        // <b>Paid for the murders nobody has been paid for yet.</b> The price is a standing number, and a bot that dies
        // twice would be worth it twice: during the raid on the hideout at 01:50 on 18.09.2026 Nessa Ashdown was killed
        // there, rose there — a member's resurrection is its hideout — was found again a minute later and would have
        // been sold to the treasury again for the same single murder. What a head is worth is what it has done since
        // the last time somebody was paid for it.
        if (m?.Name != null && _paid.TryGetValue(m.Name, out var already) && murders <= already)
        {
            return 0;
        }

        return murders <= 0 ? 0 : HeadFirst + HeadNext * (murders - 1);
    }

    /// <summary>
    /// Books a head as paid for. Kept by name and only in memory: the store's shape cannot change without losing the
    /// whole record, and the worst a restart can cost is one head paid for twice.
    /// </summary>
    public static void Sold(Mobile m)
    {
        if (m?.Name is { } name)
        {
            _paid[name] = Of(name)?.Murders ?? 0;
        }
    }

    private static Sheet Sheeted(Mobile m)
    {
        if (m?.Name is not { } name)
        {
            return null;
        }

        if (!_sheets.TryGetValue(name, out var sheet))
        {
            _sheets[name] = sheet = new Sheet { Name = name };
        }

        return sheet;
    }

    /// <summary>A murder, and whether the blow was struck from hiding. From <see cref="BotRob"/>.</summary>
    public static void Murder(Mobile killer, bool unseen)
    {
        if (Sheeted(killer) is not { } sheet)
        {
            return;
        }

        sheet.Murders++;

        if (unseen)
        {
            sheet.Unseen++;
        }

        Outcast(killer);
    }

    /// <summary>A robbery carried through to the corpse. From <see cref="BotRob"/>.</summary>
    public static void Robbed(Mobile robber)
    {
        if (Sheeted(robber) is { } sheet)
        {
            sheet.Robberies++;

            Outcast(robber);
        }
    }

    /// <summary>
    /// A bandit's first crime takes it out of its guild at once, and into The Shadow if there is room, or founds it.
    ///
    /// <para>
    /// <b>Patrick's order of 26.09.2026: "the first bandits leave their current guilds at once and form a new one; from now on
    /// most of their motivation is banditry. If The Shadow is founded, it goes on the list of every guild" — as an enemy.</b>
    /// Until then a murderer stayed in its trade guild until two killings from hiding founded the band, and the sweep that
    /// looked for founders came once a minute; a bandit now walks out the moment it has done it, and the band is looked for
    /// then and there.
    /// </para>
    /// </summary>
    private static void Outcast(Mobile m)
    {
        if (!Running || m is not BotMobile { Deleted: false } bot || Member(bot))
        {
            return;
        }

        if (bot.Guild is Guild old && !ReferenceEquals(old, _guild))
        {
            Leave(bot);
            Outcasts++;

            logger.Information("{Bot} walks out of {Guild}: a bandit keeps no honest company", bot.Name, old.Name);
        }

        if (!_founded || !Anybody())
        {
            TryFound();

            return;
        }

        if (_guild is { Disbanded: false } && Thieves < MostMembers && Fit(bot))
        {
            Join(bot);
            Recruited++;

            logger.Information("{Bot} is taken into The Shadow at once, {Count} thieves of {Most}", bot.Name, Thieves, MostMembers);
        }
    }

    /// <summary>
    /// Sets The Shadow against every guild on the island: the engine's own enemy list, both ways, and never a war in the
    /// ledger — there is no score, no clock and no peace to this one. Called when the band is made, at every sweep, and when a
    /// guild is founded (<c>BotGuilds.Form</c>); a guild already its enemy is passed over, so it costs a list lookup a guild.
    /// </summary>
    public static void EnemyOfAll()
    {
        if (!Running || !_founded || _guild is not { Disbanded: false } band)
        {
            return;
        }

        var made = 0;

        foreach (var guild in BotGuilds.Standing)
        {
            if (guild == null || guild.Disbanded || ReferenceEquals(guild, band) || band.IsWar(guild))
            {
                continue;
            }

            band.AddEnemy(guild);
            made++;
        }

        if (made > 0)
        {
            Enmities += made;

            logger.Information("The Shadow is set against {Count} guilds as their enemy; every guild on the island now counts it one", made);
        }
    }

    /// <summary>A guild's chest broken into and emptied a little: a robbery on the record. From <c>BotBurgle</c>.</summary>
    public static void Burgled(Mobile burglar)
    {
        if (Sheeted(burglar) is { } sheet)
        {
            sheet.Robberies++;

            Outcast(burglar);
        }
    }

    /// <summary>Caught and put in a cell. From <see cref="BotOutlaw.Jail"/>.</summary>
    public static void Jailed(Mobile bot)
    {
        if (Sheeted(bot) is { } sheet)
        {
            sheet.Jailed++;
        }

        // <b>A caught thief gives the hideout away.</b> Patrick's order of 17.09.2026, and the word in it is thief: the
        // fence is the band's keeper and not one of its five.
        //
        // <para>
        // It matters because the fence is the one that keeps being caught. It is a Sage or an Architect, it must chase
        // the witnesses who see it, it loses those fights — "Hale died doing silence: after a witness" at 03:01:17 on
        // 18.09.2026 — and every catch burned the camp: three hideouts in four hours, and a chest that never held
        // anything long enough to be worth walking to. The hideout is the thieves' secret, and it is theirs to lose.
        // </para>
        if (Member(bot) && !BotFence.Is(bot) && _hideout != Point3D.Zero && !_hideoutKnown)
        {
            _hideoutKnown = true;
            _givenAwayBy = bot.Name;
            GivenAway++;

            logger.Information(
                "{Name}, caught, gives The Shadow's hideout at ({X}, {Y}) away to the Baron",
                bot.Name,
                _hideout.X,
                _hideout.Y
            );
        }
    }

    /// <summary>Whether the Baron has a hideout to raid, and where; not while a raid on it stands. From <c>BotHarrower</c>.</summary>
    public static bool RaidDue(out Point3D at)
    {
        at = _hideout;

        return Running && _founded && _hideoutKnown && _hideout != Point3D.Zero
               && !(_raiding && Core.TickCount - _raidTick < RaidTimeoutMs);
    }

    /// <summary>The Baron has taken the raid. From <see cref="BotRaid"/>.</summary>
    public static void RaidBegun()
    {
        _raiding = true;
        _raidTick = Core.TickCount;
    }

    /// <summary>The raid has searched the hideout: it is burned, and The Shadow finds another. From <see cref="BotRaid"/>.</summary>
    public static void RaidEnded(string by, int found)
    {
        logger.Information(
            "{By}'s raid searched The Shadow's hideout at ({X}, {Y}), given away by {Informer}, and found {Found} there; the hideout is burned, and The Shadow will find another",
            by,
            _hideout.X,
            _hideout.Y,
            _givenAwayBy ?? "somebody",
            found
        );

        _raiding = false;
        _hideoutKnown = false;
        _givenAwayBy = null;
        _hideout = Point3D.Zero;
        Burned++;
        BotLair.Burn();
    }

    /// <summary>A demand paid. From the blackmail, build 114.</summary>
    public static void Extorted(Mobile thief)
    {
        if (Sheeted(thief) is { } sheet)
        {
            sheet.Extortions++;
        }
    }

    /// <summary>
    /// Puts The Shadow back together after the boot's muster, from the names kept. From <c>BotPopulationModule</c>, straight
    /// after <see cref="BotGuilds.Muster"/>, because a guild dealt at the muster must be left before this one can be joined.
    /// </summary>
    /// <summary>
    /// Everything the underworld remembers, gone: the sheets, the band's roll and who has been paid off.
    ///
    /// <para>
    /// For <c>do reset</c>, which is Patrick's "they begin a new world, literally". A record of murders kept over
    /// a population that no longer exists is a list of strangers, and the band would re-form around names that
    /// were never in it.
    /// </para>
    /// </summary>
    public static void Forget()
    {
        _sheets.Clear();
        _members.Clear();
        _paid.Clear();

        // And the band itself: a wiped island has no Shadow until two of the new population found one. See Beat.
        _founded = false;
        _guild = null;

        BotBurgle.Forget();
    }

    public static void Reform()
    {
        if (!Running || !_founded || _members.Count == 0)
        {
            return;
        }

        List<BotMobile> band = [];

        for (var i = 0; i < _members.Count; i++)
        {
            if (Named(_members[i]) is { } bot)
            {
                band.Add(bot);
            }
        }

        if (band.Count == 0)
        {
            logger.Information("None of The Shadow's {Count} were raised this boot; the guild waits for them", _members.Count);

            return;
        }

        Form(band);
        Reformed += band.Count;

        logger.Information(
            "The Shadow is back together: {Members}, led by {Master}; its hideout at ({X}, {Y})",
            string.Join(", ", _members),
            band[0].Name,
            _hideout.X,
            _hideout.Y
        );
    }

    /// <summary>One look at the guild, from <c>BotOutlaw</c>'s clock.</summary>
    public static void Beat(long now)
    {
        if (!Running || now - _sweptTick < SweepMs)
        {
            return;
        }

        _sweptTick = now;

        if (!_founded)
        {
            TryFound();

            return;
        }

        // <b>A band with nobody left on its roll is no band.</b> The wipe of 25.09.2026 cleared the roll and the record
        // (Forget) and left the band founded: every sweep after it skipped the founding, the recruiting wanted a master off
        // an empty roll, and the island could never have a Shadow again — found at the first boot of 26.09.2026, reading
        // "The Shadow, the enemy of every guild: , hideout at (1521, 1102)". Nobody on the roll exists, in the world or at
        // rest: dissolved, and the next bandits found it afresh.
        if (!Anybody())
        {
            _founded = false;
            _guild = null;
            _members.Clear();
            Dissolved++;

            logger.Information("The Shadow has nobody left on its roll and is dissolved; the next {Founders} bandits found it again", Math.Max(1, Founders));

            TryFound();

            return;
        }

        EnemyOfAll();

        if (_hideout == Point3D.Zero)
        {
            ChooseHideout();
        }
        else if (BotLair.Fire == null && !BotLair.Rebind(BotPopulation.Home, _hideout))
        {
            // The camp is gone from the world but the ground is still The Shadow's: pitch it again.
            BotLair.Pitch(BotPopulation.Home, _hideout);
        }

        if (now - _stashedTick >= BotStash.EveryMs)
        {
            _stashedTick = now;
            Stash();
        }

        if (now - _fetchedTick >= BotFetch.EveryMs)
        {
            _fetchedTick = now;
            Fetch();
            Draw();
        }

        // The master's clock starts at the first look after a boot, so a restart is not a free recruit.
        if (!_recruitClock)
        {
            _recruitClock = true;
            _recruitedTick = now;

            return;
        }

        if (now - _recruitedTick >= RecruitEveryMs)
        {
            _recruitedTick = now;
            TryRecruit();
        }
    }

    /// <summary>
    /// One look for a member carrying more than it should: the takings go out to the chest by the fire rather than ride
    /// in a pack until a patrol takes them. See <c>BotStash</c>.
    /// </summary>
    /// <summary>Sweeps for the walk out to the chest, and every gate of them counted apart. See <see cref="Stash"/>.</summary>
    public static long Sweeps { get; private set; }

    public static long NoChest { get; private set; }

    public static long Unfit { get; private set; }

    public static long Busy { get; private set; }

    public static long Light { get; private set; }

    public static long Unready { get; private set; }

    public static long Sent { get; private set; }

    private static void Stash()
    {
        Sweeps++;

        if (BotLair.Chest is not { } chest || chest.Map is not { } map || map == Map.Internal)
        {
            NoChest++;

            return;
        }

        var at = chest.GetWorldLocation();
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Alive: true, Fallen: false } || bot.Map != map || !Member(bot)
                || BotOutlaw.Jailed(bot) || bot.Squad != null)
            {
                Unfit += Member(bot) ? 1 : 0;

                continue;
            }

            // Not in the middle of the band's own business: a robbery, a fight, a cell, lying low, or this very walk.
            if (bot.Resolve?.Deed is BotStash or BotRob or BotBrawl or BotManhunt or BotSentence or BotLieLow)
            {
                Busy++;

                continue;
            }

            // <b>The keeper walks out for what it bought as well as for what it took.</b> Its purse can be light and
            // its pack full of the band's bandages, and the coin test alone would keep it in town for ever. See
            // BotFence.Carrying.
            if ((bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) - BotPurse.Keeps(bot) < BotStash.Least
                && !BotFence.Carrying(bot))
            {
                Light++;

                continue;
            }

            if (BotLadder.Standing(bot) is not (BotStanding.Free or BotStanding.Busy))
            {
                Unready++;

                continue;
            }

            Sent++;

            BotWill.Press(bot, new BotStash(map, at), BotFence.Is(bot) ? "the band's order to put in the chest" : "the takings are heavy");
        }
    }

    /// <summary>
    /// One look at the fence: a chest with goods standing in it and a fence with an empty enough pack sends it out to
    /// carry them to market. See <c>BotFetch</c>.
    /// </summary>
    private static void Fetch()
    {
        if (BotLair.Chest is not { } chest || chest.Map is not { } map || map == Map.Internal)
        {
            return;
        }

        if (BotFence.Who is not { Deleted: false, Alive: true, Fallen: false } fence || fence.Map != map
            || fence.Squad != null || BotOutlaw.Jailed(fence))
        {
            return;
        }

        if (fence.Resolve?.Deed is BotFetch or BotStash or BotBrawl or BotSilence or BotHoleUp)
        {
            return;
        }

        // <b>A run to market is a walk into a town, and a wanted keeper is taken in towns.</b> This sweep presses, and
        // pressing replaces the work in hand whatever it is — so without this it would pull the fence off going to
        // ground every five minutes and send it to a counter with 5000gp on its head, while BotFence.Ground pressed it
        // straight back. Two sweeps taking turns at one bot is not a plan either of them meant.
        if (BotOutlaw.IsWanted(fence) || BotOutlaw.IsRed(fence))
        {
            return;
        }

        var goods = 0;

        for (var i = 0; i < chest.Items.Count; i++)
        {
            if (chest.Items[i] is { Deleted: false, Movable: true } and not Gold)
            {
                goods++;
            }
        }

        if (goods < BotFetch.Least || BotLadder.Standing(fence) is not (BotStanding.Free or BotStanding.Busy))
        {
            return;
        }

        BotWill.Press(fence, new BotFetch(map, chest.GetWorldLocation()), $"{goods} things of the band's to sell");
    }

    /// <summary>
    /// One look for a thief that is short of its kit and cannot shop for it: the chest is the only counter a red can
    /// reach. See <c>BotFetch</c>'s kit mode.
    /// </summary>
    private static void Draw()
    {
        if (BotLair.Chest is not { } chest || chest.Map is not { } map || map == Map.Internal || chest.Items.Count == 0)
        {
            return;
        }

        var at = chest.GetWorldLocation();
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Alive: true, Fallen: false } || bot.Map != map || !Member(bot)
                || BotFence.Is(bot) || BotOutlaw.Jailed(bot) || bot.Squad != null)
            {
                continue;
            }

            if (bot.Resolve?.Deed is BotFetch or BotStash or BotRob or BotBrawl or BotManhunt or BotSentence or BotLieLow)
            {
                continue;
            }

            if (BotLadder.Standing(bot) is not (BotStanding.Free or BotStanding.Busy))
            {
                continue;
            }

            var wanting = 0;
            var kept = BotUnload.Keeps(bot);
            var pack = bot.Backpack;

            if (pack == null)
            {
                continue;
            }

            for (var j = 0; j < chest.Items.Count && wanting == 0; j++)
            {
                if (chest.Items[j] is { Deleted: false, Movable: true } item && item is not Gold
                    && kept.TryGetValue(item.GetType(), out var wants) && pack.GetAmount(item.GetType()) < wants)
                {
                    wanting++;
                }
            }

            if (wanting == 0)
            {
                continue;
            }

            BotWill.Press(bot, new BotFetch(map, at, true), "short of its kit, and the towns are shut to it");
        }
    }

    /// <summary>Whether this bot could be of The Shadow: a killer from hiding, of a class that robs, not in a cell, and nobody's maker.</summary>
    private static bool Fit(BotMobile bot)
    {
        if (bot is not { Deleted: false } || bot.Class is not { } klass || Of(bot.Name) is not { } sheet
            || sheet.Murders + sheet.Robberies < 1 || sheet.Unseen < LeastUnseen)
        {
            return false;
        }

        // <b>The fence is known by its trade and nothing else.</b> The band's roll in the store is a list of names, and a
        // shape it cannot read loses the whole record; so the one member that keeps rather than robs is the one whose
        // class is a Sage's or an Architect's, and those two are therefore never taken for thieves. See BotFence.
        if (klass is BotSage or BotArchitect)
        {
            return false;
        }

        // <b>A cell does not disqualify a founder, or the band could never be founded at all.</b> On the night of
        // 17.09.2026 both of the island's killers from hiding were in cells within a minute of their kill — Orin
        // Ashdown at 23:41:51, Pell at 23:56:09, each hunted down and paid for inside half a minute of being seen — so
        // the founding sweep never once saw two free at the same moment, and the guild the order asks for could not
        // exist while the law works as it does. Prison and pursuit are the price Patrick set, not a bar to the band:
        // what founds The Shadow is a reputation, and a member serving its hour joins it on the way out.
        return klass.Role is not (BotRole.Producer or BotRole.Medic) && !klass.Unpaid && !klass.Leads
               && bot.Squad == null;
    }

    private static void TryFound()
    {
        List<BotMobile> fit = [];
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (Fit(bots[i]))
            {
                fit.Add(bots[i]);
            }
        }

        if (fit.Count < Math.Max(1, Founders))
        {
            return;
        }

        if (!ChooseHideout())
        {
            NoHideout++;

            return;
        }

        fit.Sort((a, b) => b.Skills.Hiding.Base.CompareTo(a.Skills.Hiding.Base));

        if (fit.Count > MostMembers)
        {
            fit.RemoveRange(MostMembers, fit.Count - MostMembers);
        }

        _founded = true;
        Founded++;
        Form(fit);

        List<string> founders = [];

        for (var i = 0; i < fit.Count; i++)
        {
            founders.Add($"{fit[i].Name} (Hiding {fit[i].Skills.Hiding.Base:F1}, {Of(fit[i].Name)?.Unseen} killed from hiding)");
        }

        logger.Information(
            "The Shadow is founded by {Founders}, led by {Master}; its hideout is at ({X}, {Y}), {Road} steps of road from home",
            string.Join(", ", founders),
            fit[0].Name,
            _hideout.X,
            _hideout.Y,
            BotRoads.FromHome(BotPopulation.Home, _hideout.X, _hideout.Y)
        );
    }

    private static void TryRecruit()
    {
        // <b>A master in a cell still runs its band.</b> Since build 124 a bot in a cell may found The Shadow, and on the
        // night of 18.09.2026 both founders were serving their hour within a minute of their kill — so this gate, which
        // refused to recruit while the master was jailed, meant the band could be founded and then never grow. Five
        // bots were on the record with four killings from hiding between them and "0 taken in" against them all. The
        // word from the cells is the oldest thing there is in this trade.
        if (Thieves >= MostMembers || Named(_members.Count > 0 ? _members[0] : null) is not { Alive: true } master)
        {
            return;
        }

        BotMobile best = null;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (Member(bot) || !Fit(bot))
            {
                continue;
            }

            if (best == null || bot.Skills.Hiding.Base > best.Skills.Hiding.Base)
            {
                best = bot;
            }
        }

        if (best == null)
        {
            return;
        }

        Join(best);
        Recruited++;

        logger.Information(
            "{Master} takes {Bot} into The Shadow (Hiding {Hiding:F1}, {Unseen} killed from hiding), {Count} thieves of {Most}",
            master.Name,
            best.Name,
            best.Skills.Hiding.Base,
            Of(best.Name)?.Unseen,
            Thieves,
            MostMembers
        );
    }

    /// <summary>Makes or revives the engine guild with this band, the first its master, each taken out of any other guild first.</summary>
    private static void Form(List<BotMobile> band)
    {
        for (var i = 0; i < band.Count; i++)
        {
            Leave(band[i]);
        }

        var master = band[0];
        var guild = BaseGuild.FindByName(GuildName) as Guild;

        if (guild == null || guild.Disbanded)
        {
            guild = new Guild(master, GuildName, GuildAbbreviation)
            {
                Type = GuildType.Regular,
                Charter = "Nobody's business."
            };
        }
        else
        {
            guild.Leader = master;

            for (var i = guild.Members.Count - 1; i >= 0; i--)
            {
                if (guild.Members[i] is not BotMobile { Deleted: false })
                {
                    guild.RemoveMember(guild.Members[i]);
                }
            }
        }

        // The engine disbands a guild with no guildstone the first time it is read back. See BotGuilds.Stone.
        BotGuilds.Stone(guild);

        _guild = guild;
        _members.Clear();

        for (var i = 0; i < band.Count; i++)
        {
            guild.AddMember(band[i]);
            band[i].DisplayGuildTitle = true;
            _members.Add(band[i].Name);
            Picks(band[i]);
        }

        // A guild made afresh has no enemies, and a disband cleared the last one's: set against everybody again.
        EnemyOfAll();
    }

    /// <summary>
    /// Takes a bot into The Shadow that is not a thief: the fence, and only the fence. The band's own five are counted
    /// by <see cref="Thieves"/>, so a keeper of goods is a sixth and not one of them.
    /// </summary>
    public static bool Take(BotMobile bot)
    {
        if (!_founded || _guild is not { Disbanded: false } || bot is not { Deleted: false, Alive: true }
            || Member(bot) || _members.Contains(bot.Name))
        {
            return false;
        }

        Join(bot);

        return true;
    }

    /// <summary>The thieves of the band: its members but for the fence.</summary>
    public static int Thieves
    {
        get
        {
            var count = _members.Count;

            return BotFence.Who != null ? Math.Max(0, count - 1) : count;
        }
    }

    private static void Join(BotMobile bot)
    {
        Leave(bot);
        _guild.AddMember(bot);
        bot.DisplayGuildTitle = true;
        _members.Add(bot.Name);
        Picks(bot);
    }

    /// <summary>The band's own kit: lockpicks, the day a bot joins. Patrick's second addition of 29.09.2026: a break-in wants picks in hand. See BotBurgle.</summary>
    private static void Picks(BotMobile bot)
    {
        if (bot?.Backpack == null || BotBurgle.PicksIssued <= 0 || BotBurgle.HasPick(bot))
        {
            return;
        }

        var picks = new Lockpick(BotBurgle.PicksIssued);

        if (!bot.Backpack.TryDropItem(bot, picks, false))
        {
            picks.Delete();
        }
    }

    /// <summary>Takes a bot out of whatever guild it is in, other than The Shadow.</summary>
    private static void Leave(BotMobile bot)
    {
        if (bot.Guild is Guild old && !ReferenceEquals(old, _guild))
        {
            old.RemoveMember(bot);
            bot.DisplayGuildTitle = false;
        }
    }

    /// <summary>
    /// Picks the hideout: standing ground between <see cref="HideoutLeastRoad"/> and <see cref="HideoutMostRoad"/> steps of
    /// road from home, in no town, house, dungeon or jail. Waits for the road map, which is what says it can be walked to.
    /// </summary>
    private static bool ChooseHideout()
    {
        if (_hideout != Point3D.Zero)
        {
            return true;
        }

        var map = BotPopulation.Home;

        if (map == null || map == Map.Internal || !BotRoads.Ready)
        {
            return false;
        }

        var home = BotPopulation.Where;

        for (var i = 0; i < 400; i++)
        {
            var angle = Utility.RandomDouble() * Math.PI * 2.0;
            var span = HideoutLeastRoad * 0.7 + Utility.RandomDouble() * (HideoutMostRoad - HideoutLeastRoad * 0.7);
            var x = home.X + (int)Math.Round(Math.Cos(angle) * span);
            var y = home.Y + (int)Math.Round(Math.Sin(angle) * span);

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height || !BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            var road = BotRoads.FromHome(map, x, y);

            if (road < HideoutLeastRoad || road > HideoutMostRoad)
            {
                continue;
            }

            var region = Region.Find(new Point3D(x, y, z), map);

            if (region.IsPartOf<GuardedRegion>() || region.IsPartOf<HouseRegion>() || region.IsPartOf<DungeonRegion>()
                || region.IsPartOf<JailRegion>())
            {
                continue;
            }

            _hideout = new Point3D(x, y, z);
            BotLair.Pitch(map, _hideout);

            return true;
        }

        return false;
    }

    /// <summary>The living bot of the population with this name, or null.</summary>
    private static BotMobile Named(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && bot.Name == name)
            {
                return bot;
            }
        }

        return null;
    }

    public static string Describe()
    {
        var murderers = 0;
        var unseen = 0;

        foreach (var (_, sheet) in _sheets)
        {
            if (sheet.Murders > 0)
            {
                murderers++;
            }

            if (sheet.Unseen > 0)
            {
                unseen++;
            }
        }

        var extortions = 0;

        foreach (var (_, sheet) in _sheets)
        {
            extortions += sheet.Extortions;
        }

        var guild = !_founded
            ? $"The Shadow is not founded ({Bandits()} of the {Math.Max(1, Founders)} bandits it wants)"
            : $"The Shadow, the enemy of every guild ({Enmities} set against it): {string.Join(", ", _members)}, hideout at ({_hideout.X}, {_hideout.Y}){(_hideoutKnown ? $", given away by {_givenAwayBy}" : "")}; founded {Founded} times, {Recruited} taken in, {Reformed} put back at a boot, {GivenAway} hideouts given away and {Burned} burned";

        return $"{_sheets.Count} bots on record, {murderers} of them murderers and {unseen} killers from hiding, {extortions} demands paid on record, {Outcasts} walked out of their guilds for it; {guild}; {BotFence.Describe()}; {BotSilence.Describe()}; {BotLair.Describe()}; {BotStash.Describe()}; {Sweeps} sweeps for a walk out ({NoChest} found no chest, {Busy} members at the band's own business, {Light} carrying nothing worth the walk, {Unready} not free to go, {Sent} sent); {BotFetch.Describe()}; {BotRaid.Describe()}; {BotBurgle.Describe()}; {BotCrime.Describe()}";
    }

    internal static void Save(IGenericWriter writer)
    {
        writer.Write(_founded);
        writer.Write(_hideout);
        writer.Write(_hideoutKnown);
        writer.Write(_givenAwayBy ?? "");
        writer.WriteEncodedInt(_members.Count);

        for (var i = 0; i < _members.Count; i++)
        {
            writer.Write(_members[i]);
        }

        writer.WriteEncodedInt(_sheets.Count);

        foreach (var (_, sheet) in _sheets)
        {
            writer.Write(sheet.Name);
            writer.WriteEncodedInt(sheet.Murders);
            writer.WriteEncodedInt(sheet.Unseen);
            writer.WriteEncodedInt(sheet.Robberies);
            writer.WriteEncodedInt(sheet.Extortions);
            writer.WriteEncodedInt(sheet.Jailed);
        }
    }

    internal static void Load(IGenericReader reader)
    {
        _founded = reader.ReadBool();
        _hideout = reader.ReadPoint3D();
        _hideoutKnown = reader.ReadBool();
        _givenAwayBy = reader.ReadString();

        if (string.IsNullOrEmpty(_givenAwayBy))
        {
            _givenAwayBy = null;
        }

        var members = reader.ReadEncodedInt();

        _members.Clear();

        for (var i = 0; i < members; i++)
        {
            _members.Add(reader.ReadString());
        }

        var sheets = reader.ReadEncodedInt();

        _sheets.Clear();

        for (var i = 0; i < sheets; i++)
        {
            var sheet = new Sheet
            {
                Name = reader.ReadString(),
                Murders = reader.ReadEncodedInt(),
                Unseen = reader.ReadEncodedInt(),
                Robberies = reader.ReadEncodedInt(),
                Extortions = reader.ReadEncodedInt(),
                Jailed = reader.ReadEncodedInt()
            };

            if (!string.IsNullOrEmpty(sheet.Name))
            {
                _sheets[sheet.Name] = sheet;
            }
        }
    }
}
