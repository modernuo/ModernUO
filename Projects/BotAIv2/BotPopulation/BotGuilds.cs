using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Who each bot belongs to, and what that belonging is worth.
///
/// <para>
/// <b>Ordered by Patrick on 08.09.2026: something that makes them pull together.</b> A guild on this shard
/// is not decoration — the engine already treats guildmates as <c>Notoriety.Ally</c>, so it is the one
/// standing statement the world itself understands about which bots are on the same side.
/// </para>
///
/// <para>
/// <b>Rewritten the same night, and the rewrite is the interesting half.</b> The first version sorted the
/// population by trade: every fighter into one guild, every maker into another. That is a caste system
/// rather than a society, and it produced exactly what a caste system produces — the Blade held thirty-five
/// of forty-nine bots and the Needle held one. Patrick's rules replace it:
/// </para>
///
/// <list type="bullet">
/// <item>a guild is formed only if it will have at least <see cref="Least"/> members;</item>
/// <item>at least one of them must be able to make things — see <see cref="Makers"/>;</item>
/// <item>no guild may hold more than <see cref="Most"/>.</item>
/// </list>
///
/// <para>
/// <b>What those three rules make is a band rather than a class.</b> Every guild ends up with fighters, a
/// healer or two and a maker of its own, because the roster is dealt out by role rather than by trade. That
/// is a social circle: a group small enough that everybody in it is somebody you keep meeting, and mixed
/// enough that it can look after itself. It is also the unit the rest of this project's plans need — a
/// guild that owns a hall, quarrels with its neighbours and equips its own can only be a mixed band.
/// </para>
///
/// <para>
/// <b>And it is a preference, never a condition.</b> That distinction is the one this project has paid for
/// most often — a rule that reads "only with my own" is a veto, and a veto here would split the population
/// into groups that cannot help each other, which is the same defect as a muster that could find nobody. So
/// guild membership multiplies what help is worth; it never refuses it. See <see cref="Kinship"/>.
/// </para>
///
/// <para>
/// <b>Patrick's order of 21.09.2026: anybody may found a guild.</b> Until then a guild existed only if the
/// boot's muster dealt one out around a crafter, and the only door in afterwards was a leader's mind taking
/// somebody on — and the minds were switched off on 18.09, so a bot outside a guild stayed outside for ever.
/// Founding is now something a bot does while the shard runs: see <see cref="Gather"/>. The rule of five and
/// the ceiling of fifteen stand; the rule that one of the five must be a maker is the one that was lifted,
/// and with it the guild's fixed point moved from its maker to its <see cref="Head"/>.
/// </para>
/// </summary>
public static class BotGuilds
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuilds));

    /// <summary>Whether the population is organised into guilds at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How much more a guildmate's cry, or a guildmate's company, is worth than a stranger's.
    ///
    /// <para>
    /// A multiplier and a small one. Loyalty should tilt a choice, not decide it: at two, a bot walks past a
    /// stranger bleeding in front of it to help somebody of its own three screens away, and that is not what
    /// was asked for. At <see cref="Kinship"/> it prefers its own among equals, which is what a guild is.
    /// </para>
    /// </summary>
    public static double Kinship { get; set; } = 1.35;

    /// <summary>
    /// The fewest a guild may be founded with. Patrick's number.
    ///
    /// Below five a guild is a pair of friends: it cannot hold a hall, cannot field a company, and cannot
    /// survive one of its members dying. The rule bites at formation — a guild that falls below five later is not
    /// disbanded for that alone, because disbanding it would take a hall off the island for a bad afternoon. Since
    /// 06.10.2026 a guild under five that also has next to nothing to show and nobody joining for two hours closes:
    /// see <see cref="Wane"/>.
    /// </summary>
    public static int Least { get; set; } = 5;

    /// <summary>
    /// The most a guild may hold. Patrick's number, and the reason for it was on the shard in front of him:
    /// the Blade held thirty-five of forty-nine bots and would have won any quarrel before it started.
    ///
    /// <para>
    /// <b>Thirty-five from 03.10.2026 (was fifteen).</b> Patrick: "now that we have 200 bots, guilds can be 35 people."
    /// At fifteen the eight names held 120 of 210 bots and the other 90 stood outside every guild. The halls'
    /// rooms moved up with it (<c>BotHallKind</c>), so each widening is still one size of hall.
    /// </para>
    /// </summary>
    public static int Most { get; set; } = 35;

    /// <summary>
    /// How many places one widening adds to a guild. Patrick's ten.
    ///
    /// <para>
    /// <b>Patrick's order of 24.09.2026: "the guild limit can be widened by ten bots for 10000gp".</b> A full guild that
    /// is asked to take somebody on — its leader recruiting, or a bot nobody else has room for — buys the widening if it
    /// can raise the price the way a hall is raised (<see cref="BotEstate.Levy"/>: the guild's chest first, then the
    /// richest members, each keeping <see cref="BotEstate.Keep"/>), and the guild's ceiling is <see cref="Ceiling"/> from
    /// then on. Kept across restarts (<see cref="BotGuildWidenStore"/>).
    /// </para>
    /// </summary>
    public static int WidenBy { get; set; } = 10;

    /// <summary>What one widening costs the guild. Patrick's 10000gp.</summary>
    public static int WidenPrice { get; set; } = 10000;

    /// <summary>
    /// How many times one guild may widen: three, thirty-five to sixty-five (fifteen to forty-five until 03.10.2026), each only once its hall has the room (from once,
    /// 26.09.2026, when the widening was tied to the size of the hall).
    /// </summary>
    public static int MostWidenings { get; set; } = 3;

    /// <summary>Widenings bought.</summary>
    public static long Widenings { get; private set; }

    /// <summary>Full guilds that would have widened and could not raise the price.</summary>
    public static long Unwidened { get; private set; }

    private static readonly Dictionary<string, int> _widened = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How many times this guild has widened.</summary>
    public static int Widened(Guild guild) =>
        guild?.Name != null && _widened.TryGetValue(guild.Name, out var times) ? times : 0;

    /// <summary>The most this guild may hold: <see cref="Most"/>, and <see cref="WidenBy"/> more for every widening.</summary>
    public static int Ceiling(Guild guild) => Math.Min(Most + Math.Max(0, WidenBy) * Widened(guild), Math.Max(Most, BotHallKind.Holds(guild)));

    /// <summary>
    /// Widenings refused because the guild's hall had no room for ten more. See <c>BotHallKind</c>: Patrick's order of
    /// 26.09.2026, "a small hall will not hold more than fifteen" — the guild moves into a bigger one (BotEnlarge) first.
    /// </summary>
    public static long Cramped { get; private set; }

    /// <summary>A full guild asked to take somebody on buys ten more places, when it may and when it can.</summary>
    private static bool Widen(Guild guild, string why)
    {
        if (guild?.Name == null || guild.Disbanded || WidenBy <= 0 || WidenPrice < 0 || Widened(guild) >= MostWidenings)
        {
            return false;
        }

        // Ten more places need a hall with room for them.
        if (Most + WidenBy * (Widened(guild) + 1) > BotHallKind.Holds(guild))
        {
            Cramped++;

            return false;
        }

        // Asked before anything is taken, so a guild that cannot raise the whole price is not asked for any of it. (A levy
        // that falls short hands the chest's share back itself since 29.09.2026 — BotEstate.Take — and Fund counts the
        // chest, which was added here by hand before that.)
        if (BotEstate.Fund(guild) < WidenPrice)
        {
            Unwidened++;

            return false;
        }

        var paid = new List<BotEstate.Contribution>();
        var got = BotEstate.Levy(guild, WidenPrice, paid);

        if (got < WidenPrice)
        {
            BotEstate.Refund(paid);
            Unwidened++;

            return false;
        }

        _widened[guild.Name] = Widened(guild) + 1;
        Widenings++;

        logger.Information(
            "{Guild} has widened to {Ceiling} places for {Price}gp raised off its chest and members — {Why}",
            guild.Name,
            Ceiling(guild),
            WidenPrice,
            why ?? "no reason given"
        );

        return true;
    }

    /// <summary>The widenings, for the store.</summary>
    internal static void SaveWidenings(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_widened.Count);

        foreach (var (name, times) in _widened)
        {
            writer.Write(name);
            writer.WriteEncodedInt(times);
        }
    }

    internal static int LoadWidenings(IGenericReader reader)
    {
        _widened.Clear();

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString();
            var times = reader.ReadEncodedInt();

            if (!string.IsNullOrEmpty(name) && times > 0)
            {
                _widened[name] = times;
            }
        }

        return _widened.Count;
    }

    /// <summary>
    /// The size a guild is aimed at when the roster is dealt out. Between <see cref="Least"/> and
    /// <see cref="Most"/>, and nearer the top of that range than the bottom: a band of ten can lose two
    /// members to a bad night and still be a guild.
    /// </summary>
    public static int Band { get; set; } = 10;

    /// <summary>
    /// The classes that count as being able to make things.
    ///
    /// <para>
    /// <b>A maker rather than a producer, and the difference matters.</b> <c>BotRole.Producer</c> also holds
    /// the gatherer, which digs ore and cuts wood and cannot turn either into a breastplate. Patrick's rule
    /// is that a guild must have somebody who can <em>equip</em> it, so the test is the smaller one: five
    /// bots on this shard qualify, which is what sets the ceiling on how many guilds there can be.
    /// </para>
    ///
    /// <para>
    /// <b>No longer a condition of founding, since 21.09.2026.</b> It still says who equips the guild — see
    /// <see cref="Maker"/> — and the summary still counts the guilds that have nobody to, because a guild
    /// without a maker buys its armour from somebody else's and that is worth seeing. The ceiling on guilds
    /// was <see cref="Names"/> until 06.10.2026 and is <see cref="MostGuilds"/> since.
    /// </para>
    /// </summary>
    public static readonly string[] Makers = ["Crafter"];

    /// <summary>
    /// Classes that may not be in a guild at all, by Patrick's order of 11.09.2026.
    ///
    /// <para>
    /// <b>They hold an office over the whole island rather than a place in a band.</b> The Baron levies
    /// companies for the crown and is the one bot on the shard that is given money; the Captain commands
    /// them. A crown officer wearing a guild's tag is a crown officer whose orders can be read as one
    /// guild's — and once guilds fight each other over ground, that is a war with the crown on one side of
    /// it. The Architect, the Sage and the Brawler are here on the same reading: one or two of each on the
    /// island, each doing something nobody else does, none of them anybody's rank and file.
    /// </para>
    ///
    /// <para>
    /// <b>And the Architect leaving the guilds is why <see cref="Makers"/> is now the Crafter alone.</b> A
    /// guild is founded on somebody who can make things and that somebody has to be in it; an architect
    /// barred from guilds cannot found one. Five crafters, five guilds — where it used to be seven.
    /// </para>
    ///
    /// <para>
    /// <b>And the Trader, by Patrick's answer of 30.09.2026: neutral in wars (01.10.2026, build 385).</b> A guild is what a war
    /// is fought between — the regard, the feud, the rally, a war company's attackers and the toll all ask a bot's guild — so a
    /// trader in none is outside all of them by construction rather than by a test at each. See <c>BotTrading.Neutral</c>.
    /// </para>
    /// </summary>
    public static readonly string[] Barred = ["Baron", "Captain", "Architect", "Sage", "Brawler", "Trader"];

    /// <summary>Bots left out of every guild because of their class. For the muster's own line.</summary>
    public static long Excused { get; private set; }

    /// <summary>Whether this bot's class is one that stands outside the bands. See <see cref="Barred"/>.</summary>
    public static bool Outside(BotMobile bot)
    {
        // A bandit keeps no honest company: out of its guild at its first crime, and no guild takes it back. See
        // BotUnderworld.Outcast (Patrick's order of 26.09.2026).
        if (BotUnderworld.Outlawed(bot))
        {
            return true;
        }

        // Nor a thief, a wanted bot, a prisoner, or one out of a cell and not yet repentant (07.10.2026, crime17). Asked only
        // of a bot in no guild: a member its own guild ransomed is not put out by this.
        if (BotCrime.Unrepentant(bot))
        {
            return true;
        }

        // A neutral class is outside whatever its name (01.10.2026, build 385): the flag, not the list, is what a trader is.
        return bot?.Class is { Neutral: true } || Bars(bot?.Class?.Name);
    }

    /// <summary>
    /// Whether a class of this name may not be in a guild. Asked by the newcomers' draw too, so a guild short of a producer is
    /// not sent a trader or an architect it can never take in (01.10.2026, build 385; see <c>BotGrowth.OfRole</c>).
    /// </summary>
    public static bool Bars(string klass)
    {
        if (klass == null)
        {
            return false;
        }

        for (var i = 0; i < Barred.Length; i++)
        {
            if (string.Equals(klass, Barred[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The names a guild may have, in the order they are used.
    ///
    /// <para>
    /// <b>A fixed pool rather than invented names, and a hall is the reason.</b> A hall is found again after
    /// a restart by the name written on its sign (<c>BotEstate.Adopt</c>), so a population that invented
    /// fresh names every start would orphan every building it has ever raised. The four at the head of the
    /// list are the four the old caste system used, so the halls already standing are inherited rather than
    /// abandoned.
    /// </para>
    ///
    /// <para>
    /// <b>Fifty more from 06.10.2026, by Patrick's order: "think up 50 more guild names; let them be taken from this
    /// list."</b> The eight stay at the head and are taken first, in order, because a hall standing under one of their
    /// signs is inherited by the next guild of that name (<c>BotEstate.Adopt</c>). Past the eight a founding draws one of
    /// the free fifty at random (<see cref="FreeName"/>): guilds named in list order read as a list, not a world. The pool
    /// is no longer the bound on how many guilds stand; <see cref="MostGuilds"/> is.
    /// </para>
    /// </summary>
    private static readonly (string Name, string Abbrev)[] Names =
    [
        ("The Hammer", "HAM"),
        ("The Blade", "BLD"),
        ("The Crown", "CRN"),
        ("The Needle", "NDL"),
        ("The Lantern", "LAN"),
        ("The Anchor", "ANC"),
        ("The Wolf", "WLF"),
        ("The Ash", "ASH"),
        ("The Raven", "RVN"),
        ("The Oak", "OAK"),
        ("The Stag", "STG"),
        ("The Forge", "FRG"),
        ("The Compass", "CMP"),
        ("The Tide", "TID"),
        ("The Ember", "EMB"),
        ("The Thorn", "THN"),
        ("The Falcon", "FAL"),
        ("The Chalice", "CHL"),
        ("The Gauntlet", "GNT"),
        ("The Lyre", "LYR"),
        ("The Sickle", "SCK"),
        ("The Mill", "MIL"),
        ("The Bell", "BEL"),
        ("The Scroll", "SCR"),
        ("The Loom", "LOM"),
        ("The Hearth", "HRT"),
        ("The Vine", "VIN"),
        ("The Griffin", "GRF"),
        ("The Serpent", "SRP"),
        ("The Boar", "BOR"),
        ("The Heron", "HRN"),
        ("The Otter", "OTR"),
        ("The Badger", "BDG"),
        ("The Hawk", "HWK"),
        ("The Bear", "BER"),
        ("The Rose", "ROS"),
        ("The Lily", "LIL"),
        ("The Star", "STR"),
        ("The Moon", "MON"),
        ("The Sun", "SUN"),
        ("The Comet", "CMT"),
        ("The Storm", "STM"),
        ("The Frost", "FRS"),
        ("The Flint", "FLN"),
        ("The Anvil", "ANV"),
        ("The Plough", "PLW"),
        ("The Sheaf", "SHF"),
        ("The Barrel", "BRL"),
        ("The Key", "KEY"),
        ("The Tower", "TWR"),
        ("The Bridge", "BRG"),
        ("The Gate", "GAT"),
        ("The Harp", "HRP"),
        ("The Horn", "HOR"),
        ("The Banner", "BNR"),
        ("The Shield", "SHL"),
        ("The Spear", "SPR"),
        ("The Arrow", "ARW")
    ];

    /// <summary>How many names at the head of <see cref="Names"/> are taken in order rather than drawn: the eight whose halls stand.</summary>
    private const int Inherited = 8;

    /// <summary>
    /// The most guilds that may stand at once (06.10.2026). Founding stops here; it does not close anybody.
    ///
    /// <para>
    /// <b>The pool of names was the only bound until the fifty were added, and it bit.</b> Founding asks for five bots
    /// without a guild and no guild under <see cref="Band"/> to join; on a new world of 180 bots that may join a guild it
    /// would found one guild per ten bots — eighteen — where eight names made eight. Ten is today's eight and two: past
    /// that a guild cannot even raise a hall (<c>BotEstate.MaxHalls</c> is eight in the file).
    /// </para>
    /// </summary>
    public static int MostGuilds { get; set; } = 10;

    /// <summary>Looks that would have founded a guild and found <see cref="MostGuilds"/> standing.</summary>
    public static long AtMost { get; private set; }

    /// <summary>Bots enrolled.</summary>
    public static long Enrolled { get; private set; }

    /// <summary>Bots the rules could not place: no room, or no guild to put them in.</summary>
    public static long Unplaced { get; private set; }

    /// <summary>Guilds standing.</summary>
    public static int Count => _guilds.Count;

    /// <summary>The guilds themselves, for anything that has business with all of them at once.</summary>
    public static IEnumerable<Guild> Standing => _guilds.Values;

    /// <summary>Whether this bot is the head of its guild. See BotPatrol, which lets a head call a company.</summary>
    public static bool Leads(BotMobile bot) => bot?.Guild is Guild guild && ReferenceEquals(Head(guild), bot);

    /// <summary>
    /// The name of a guild of at least <paramref name="least"/> bots with nobody of this role, or nothing. After the wipe of
    /// 29.09.2026 the fifty newcomers made four guilds and three of them had nobody to make anything: two crafters in
    /// the whole population, both in The Blade. See BotGrowth, which births into the gap.
    /// </summary>
    public static string Lacking(BotRole role, int least)
    {
        foreach (var guild in _guilds.Values)
        {
            var members = 0;

            for (var i = 0; i < guild.Members.Count; i++)
            {
                if (guild.Members[i] is BotMobile { Deleted: false })
                {
                    members++;
                }
            }

            if (members >= least && OfRole(guild, role) == 0)
            {
                return guild.Name;
            }
        }

        return null;
    }

    /// <summary>
    /// The guild this population holds under that name, or nothing. The live object, which is not always what
    /// <c>BaseGuild.FindByName</c> answers after a restart: the engine may still list the guild it disbanded at
    /// load beside the one this population raised in its place.
    /// </summary>
    public static Guild Named(string name) => name == null ? null : _guilds.GetValueOrDefault(name);

    /// <summary>
    /// The guild of this name, or null.
    ///
    /// Asked by name because a name is what survives: a hall's sign carries it across restarts, which is
    /// how <see cref="BotEstate"/> knows whose house it found. See <c>BotEstate.Adopt</c>.
    /// </summary>
    public static Guild Find(string name) =>
        !string.IsNullOrEmpty(name) && _guilds.TryGetValue(name, out var guild) && !guild.Disbanded
            ? guild
            : null;

    private static readonly Dictionary<string, Guild> _guilds = [];

    /// <summary>Whether the roster has been dealt out yet. Until it has, <see cref="Enrol"/> does nothing.</summary>
    private static bool _dealt;

    /// <summary>Whether this bot's class can make things. See <see cref="Makers"/>.</summary>
    public static bool IsMaker(BotMobile bot)
    {
        var klass = bot?.Class?.Name;

        if (klass == null)
        {
            return false;
        }

        for (var i = 0; i < Makers.Length; i++)
        {
            if (string.Equals(klass, Makers[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Takes back the guilds the world save returned. Called once, after the population has been raised.
    ///
    /// <para>
    /// <b>It used to deal the whole roster out, and since build 163 that was the wrong thing to do twice
    /// over.</b> Bots come back from the save with their guild, so a deal on every boot reshuffled a society
    /// that had spent a day forming. And the deal reused the saved guild by name, which is what exposed an
    /// engine rule nobody had met: see <see cref="Stone"/>. On 20.09.2026 the muster read fifteen members
    /// to each of five guilds, and the summary five minutes later read nought to each, with nobody having
    /// walked out — the engine had disbanded all five in the first slice of its timers.
    /// </para>
    ///
    /// <para>
    /// So the muster adopts and nothing else. Whoever the save returned inside a guild of this population's
    /// own names keeps its place; everybody else is somebody <see cref="Gather"/> will get to. On a new
    /// world that is everybody, and the guilds are founded in front of whoever is watching rather than dealt
    /// out before the first line of the log.
    /// </para>
    /// </summary>
    public static void Muster()
    {
        if (!Running)
        {
            return;
        }

        var roster = BotPopulation.Bots;
        var kept = 0;
        var free = 0;

        for (var i = 0; i < roster.Count; i++)
        {
            var bot = roster[i];

            if (bot is not { Deleted: false } || bot.Class == null)
            {
                continue;
            }

            if (Outside(bot))
            {
                Excused++;

                continue;
            }

            // Only a guild of this population's own names. The Shadow keeps its own roll, and a guild somebody
            // founded by hand from a deed is not this class's to manage.
            if (bot.Guild is not Guild { Disbanded: false } guild || !Ours(guild.Name))
            {
                free++;

                continue;
            }

            _guilds[guild.Name] = guild;

            Show(bot);
            Joined(bot);
            kept++;
        }

        // <b>And the guilds of the bots resting out of the world, which the roster above cannot see.</b> On 24.09.2026 at
        // 03:29 The Anchor, whose one member Gwendra was resting (see BotRest), was mustered by nobody: the boot listed
        // seven guilds where there had been eight, and she would have come back to a guild no part of this file knew
        // about. Registered and given its stone like any other; its members are not counted as present.
        var resting = 0;
        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is not { Deleted: false } bot || bot.Class == null || Outside(bot)
                || bot.Guild is not Guild { Disbanded: false } guild || !Ours(guild.Name))
            {
                continue;
            }

            _guilds[guild.Name] = guild;
            Joined(bot);
            resting++;
        }

        foreach (var guild in _guilds.Values)
        {
            Stone(guild);

            for (var i = guild.Members.Count - 1; i >= 0; i--)
            {
                if (guild.Members[i] is not { Deleted: false })
                {
                    guild.RemoveMember(guild.Members[i]);
                }
            }
        }

        _dealt = true;
        _gathered = Core.TickCount;
        _cursor = roster.Count == 0 ? 0 : Utility.Random(roster.Count);

        logger.Information(
            "Guilds mustered: {Guilds} came back from the world save holding {Kept} bots and {Resting} resting members, {Free} bots belong to none and {Excused} stand outside the bands by class — {What}",
            _guilds.Count,
            kept,
            resting,
            free,
            Excused,
            Describe()
        );

        // The rules about founding, staying, leaving and being taken on, in one line, because the only proof
        // of what a dial is actually set to on this shard is the boot log.
        logger.Information(
            "Guild membership: founding is {Founding} — every {Gather}s one bot without a guild joins the smallest guild under {Band}, or founds its own with the {Fellows} nearest bots without one if no guild is under {Band}, {Names} names to go round (the first {Inherited} in order, the rest drawn at random), at most {MostGuilds} guilds, {Most} the ceiling; a member gives a guild with nothing to show {Patience} minutes before it may leave, one a minute at most, {Defecting}, and is then barred from joining any guild for {Rejoin} hours, though not from founding one; a leader may put one member out and take one bot on every {Roster} minutes, and may not take back somebody who walked out of its own guild; the head of a guild neither leaves nor is put out; closing is {Closing}: a guild of fewer than {Least} with attractiveness under {LowAppeal} and nobody joining for {CloseAfter} minutes closes, its chest shared among its members, its name resting {Rest} hours ({Clocks} clocks and {Closed} closed names read back off the save)",
            Founding ? "ON" : "OFF",
            GatherMs / 1000,
            Band,
            Least - 1,
            Band,
            Names.Length,
            Inherited,
            MostGuilds,
            Most,
            Patience / 60000,
            Defecting ? "and only for a guild that has something to show and room" : "into no guild at all",
            RejoinMs / 3600000,
            BotRoster.EveryMs / 60000,
            Closing ? "ON" : "OFF",
            Least,
            LowAppeal,
            CloseAfterMs / 60000,
            NameRestHours,
            _low.Count,
            _closed.Count
        );

        logger.Information(
            "Guild splits: {Splitting} — a guild at its ceiling loses {Least} members to a new guild under one of the fifty names when {Least} of them, not its head, not in a company, not inside the walk-out bar, have a reason: under the {Keep}gp keep for {Unshared} minutes while its chest holds {Rich}gp or more, living in another town than its house or hall or out of {Reach} tiles of both, or a guild half the others hold at {Enmity} or under or a town that put it out; not while it is at war, not within {Cool} hours of its last split, not with {MostGuilds} guilds standing, not while a guild with room reads appeal {Bar} or more, not within {Gap} hours of any split; founders barred from their old guild and from crossing for {Rejoin} hours of running time ({Barred} inside the bar read back off the save), the new guild spared the closing clock for {Young} hours ({Spared} read back)",
            Splitting ? "ON" : "OFF",
            Least,
            Least,
            BotEstate.Keep,
            UnsharedMs / 60000,
            RichChest,
            BotSeat.Settled,
            BotRegard.Enmity,
            SplitParentMs / 3600000,
            MostGuilds,
            SplitBar,
            SplitGapMs / 3600000,
            RejoinMs / 3600000,
            InsideBar(),
            YoungMs / 3600000,
            _young.Count
        );
    }

    /// <summary>Whether this is one of the names this population gives its guilds. See <see cref="Names"/>.</summary>
    private static bool Ours(string name)
    {
        for (var i = 0; i < Names.Length; i++)
        {
            if (string.Equals(Names[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gives a guild the stone the engine will not let it live without.
    ///
    /// <para>
    /// <b>An engine rule found by its symptom, and it hid for ten days behind another decision.</b> This
    /// shard runs before the Samurai Empire guild system, and in that era <c>Guild.VerifyGuild_Callback</c>
    /// — a timer every guild starts when it is read back from the save — disbands any guild with no
    /// guildstone. A guild made in code has none. While bots were deleted at every load this could not be
    /// seen: the saved guild's leader was deleted, the guild counted as disbanded anyway, and the muster made
    /// a fresh one that had never been through a load. Once bots survived a restart the leader was alive,
    /// the saved guild was reused, and a moment later the engine's timer emptied it.
    /// </para>
    ///
    /// <para>
    /// The stone is not put down anywhere. It is an item on the internal map, which the engine saves like
    /// any other and its cleanup leaves alone, and it goes with the guild: disbanding deletes it. Setting it
    /// in the hall where a player could read the roll off it is a better home and a later build.
    /// </para>
    /// </summary>
    public static void Stone(Guild guild)
    {
        if (guild is not { Disbanded: false } || Guild.NewGuildSystem || guild.Guildstone is { Deleted: false })
        {
            return;
        }

        guild.Guildstone = new Guildstone(guild);
        Stones++;
    }

    /// <summary>Guildstones made, which is guilds the engine would otherwise have disbanded at the next load.</summary>
    public static long Stones { get; private set; }

    /// <summary>Whether bots without a guild found and join guilds while the shard runs. See <see cref="Gather"/>.</summary>
    public static bool Founding { get; set; } = true;

    /// <summary>
    /// How often one bot without a guild is looked at.
    ///
    /// Five seconds, and one bot a look. A new world of seventy-five is settled in about six minutes, which
    /// is slow enough to read each founding off the log and fast enough that nobody measures a guildless
    /// shard by mistake.
    /// </summary>
    public static int GatherMs { get; set; } = 5000;

    /// <summary>Guilds founded while the shard ran, and the bots that walked into one that was standing.</summary>
    public static long Founded { get; private set; }

    public static long Gathered { get; private set; }

    /// <summary>
    /// Looks that ended with a bot still outside, by reason. <b>These count looks, not bots</b>: one bot
    /// nobody can place is counted again every time its turn comes round.
    /// </summary>
    public static long TooFew { get; private set; }

    public static long Nameless { get; private set; }

    public static long Barred48 { get; private set; }

    private static long _gathered;

    private static int _cursor;

    /// <summary>
    /// The head of this guild: whoever the engine says leads it, if that is a bot.
    ///
    /// <para>
    /// <b>The guild's fixed point, which used to be its maker.</b> A guild was founded on a crafter, so the
    /// crafter was what could neither leave nor be put out, and what led its delves. A guild founded by
    /// anybody has no such member by construction; what it always has is a leader, and the engine elects
    /// another the moment one is deleted. Asked of the engine each time rather than kept here, for the same
    /// reason as <see cref="Same"/>.
    /// </para>
    /// </summary>
    public static BotMobile Head(Guild guild) =>
        guild is { Disbanded: false } && guild.Leader is BotMobile { Deleted: false } head ? head : null;

    /// <summary>Whether this bot leads the guild it is in.</summary>
    public static bool IsHead(BotMobile bot) =>
        bot?.Guild is Guild guild && ReferenceEquals(Head(guild), bot);

    /// <summary>
    /// Makes this member the guild's head: the engine's leader, every member's fealty sworn to it, and the guild's hall and
    /// outpost put in its name. Called by <c>BotElection</c> (07.10.2026), and the only door to a new head besides the engine's
    /// own vote when a head leaves or is deleted.
    ///
    /// <para>
    /// <b>The fealty is the half that makes it stick.</b> The engine counts its own vote once a day at a save
    /// (<c>Guild.CalculateGuildmaster</c>, by each member's <c>GuildFealty</c>), and a member's fealty is whoever led the guild
    /// on the day it joined: left alone, the next save would have elected the old head back over the members' choice. Sworn to
    /// the winner, the engine's own count agrees with the election.
    /// </para>
    /// </summary>
    public static bool Crown(Guild guild, BotMobile head)
    {
        if (guild is not { Disbanded: false } || head is not { Deleted: false } || head.Guild != guild)
        {
            return false;
        }

        if (!ReferenceEquals(guild.Leader, head))
        {
            guild.Leader = head;
        }

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is { Deleted: false } member)
            {
                member.GuildFealty = head;
            }
        }

        // The hall's owner and the outpost's are the head; every other member is a co-owner. See BotEstate.Take.
        if (BotEstate.Hall(guild) is { Deleted: false } hall && hall.Owner != head)
        {
            BotEstate.Take(guild, hall);
        }

        if (BotOutpost.Of(guild.Name) is { Deleted: false } outpost && outpost.Owner != head)
        {
            BotOutpost.Keep(guild, outpost);
        }

        return true;
    }

    /// <summary>Whether this bot is one <see cref="Gather"/> has business with: alive, in the world, of a class that may join, and in no guild.</summary>
    private static bool Loose(BotMobile bot) =>
        bot is { Deleted: false, Alive: true, Guild: null, Class: not null }
        && bot.Map != null
        && bot.Map != Map.Internal
        && !Outside(bot);

    /// <summary>
    /// One bot without a guild gets one: it joins a guild that is still small, or founds its own.
    ///
    /// <para>
    /// <b>The order of the three questions is the design.</b> A guild under <see cref="Band"/> is joined
    /// first, so a founding is followed by the guild filling rather than by four more foundings of five
    /// each. With no guild under <see cref="Band"/> the bot founds — it, and the <see cref="Least"/> − 1
    /// nearest bots that have no guild either, because the people a bot keeps meeting are the ones standing
    /// near it. Only when it can do neither (no names left, or too few outside) does it squeeze into a guild
    /// between <see cref="Band"/> and <see cref="Most"/>.
    /// </para>
    ///
    /// <para>
    /// <b>Whose turn it is decides who founds, and that is all "anybody" needs.</b> The cursor starts at a
    /// random place in the roster and walks it, so the founder is an archer one world and a gatherer the
    /// next. There is no price, no skill floor and no class test: each of those would be a gate that reads
    /// as a veto on a poor shard, which is this project's most expensive habit.
    /// </para>
    ///
    /// <para>
    /// A bot that walked out of a guild is still barred from <em>joining</em> one for <see cref="RejoinMs"/>;
    /// it may found, and it may be one of the fellows somebody else founds with. Leaving said something
    /// about the guild that was left, not about guilds — and on 18.09.2026 forty-six bots walked out of three
    /// guilds that had nothing to show and stood outside everything for the rest of the day, which is the
    /// population this door is for. Were the bar kept on fellows, five bots that had each walked out could
    /// never found together, and the door would be shut to exactly the bots standing at it.
    /// </para>
    /// </summary>
    public static void Gather()
    {
        if (!Running || !Founding || !_dealt)
        {
            return;
        }

        var now = Core.TickCount;

        if (now - (_gathered + GatherMs) < 0)
        {
            return;
        }

        _gathered = now;

        // A guild that died while the shard ran is taken off the books, so its name is free again and nobody
        // downstream is handed an empty shell. Its hall, if it had one, waits for the next guild of that name.
        List<string> dead = null;

        foreach (var (name, guild) in _guilds)
        {
            if (guild.Disbanded)
            {
                (dead ??= []).Add(name);
            }
        }

        if (dead != null)
        {
            for (var i = 0; i < dead.Count; i++)
            {
                _guilds.Remove(dead[i]);
                _young.Remove(dead[i]);

                logger.Information("{Guild} is no more: its last member is gone, and the name is free to be taken again", dead[i]);
            }
        }

        var roster = BotPopulation.Bots;
        BotMobile bot = null;

        for (var looked = 0; looked < roster.Count && bot == null; looked++)
        {
            var next = roster[_cursor++ % roster.Count];

            if (Loose(next))
            {
                bot = next;
            }
        }

        if (bot == null)
        {
            return;
        }

        var cooling = Cools(bot);

        if (!cooling && Roomiest(Band, bot) is { } small)
        {
            Admit(small, bot);

            return;
        }

        // The bound on how many guilds stand, now that the names are not it (06.10.2026). Counted apart from a name
        // shortage, which is a different fact about the shard.
        if (Living() >= MostGuilds)
        {
            AtMost++;
        }
        else if (FreeName(out var named))
        {
            var fellows = Nearest(bot, Least - 1);

            if (fellows.Count >= Least - 1)
            {
                List<BotMobile> band = [bot];

                band.AddRange(fellows);
                Form(named, band);
                Founded++;

                var names = new string[fellows.Count];

                for (var i = 0; i < fellows.Count; i++)
                {
                    names[i] = $"{fellows[i].Name} the {fellows[i].Class?.Name}";
                }

                logger.Information(
                    "{Founder} the {Class} has founded {Guild} [{Abbrev}] at ({X}, {Y}) with {Fellows} — {Count} guilds stand now",
                    bot.Name,
                    bot.Class?.Name,
                    named.Name,
                    named.Abbrev,
                    bot.X,
                    bot.Y,
                    string.Join(", ", names),
                    _guilds.Count
                );

                _foundedNames.Add(named.Name);
                BotAlarm.Note("guild", $"{named.Name} [{named.Abbrev}] was founded by {bot.Name} the {bot.Class?.Name} with {fellows.Count} others", Count, MostGuilds, "-");

                return;
            }

            TooFew++;
        }
        else
        {
            Nameless++;
        }

        if (cooling)
        {
            Barred48++;

            return;
        }

        if (Roomiest(0) is { } any)
        {
            Admit(any, bot);

            return;
        }

        // Every guild full: the one best able to pay widens for it, rather than the bot going without. See WidenBy.
        if (Widest() is { } widened && Widen(widened, $"{bot.Name} the {bot.Class?.Name} had nowhere else to go"))
        {
            Admit(widened, bot);

            return;
        }

        Unplaced++;
    }

    /// <summary>
    /// The smallest standing guild with fewer than <paramref name="under"/> members — or, at nought, with room under its own
    /// <see cref="Ceiling"/> — or nothing.
    /// </summary>
    private static Guild Roomiest(int under) => Roomiest(under, null);

    /// <summary>
    /// The guild under the band with the fewest of this bot's kind in it, the smallest among equals — so a smith goes where
    /// there is no smith and an archer where the line is thin.
    ///
    /// <para>
    /// <b>Patrick's fifth point of the night of 29.09.2026: balance the guilds.</b> On the wiped world the four guilds took
    /// whoever was nearest: The Blade got all three crafters, The Crown five of the seven gatherers, The Needle six archers
    /// and no producer, The Hammer five warrior-mages and no producer. Numbers were even; trades were not.
    /// </para>
    /// </summary>
    private static Guild Roomiest(int under, BotMobile bot)
    {
        Guild best = null;
        var bestShare = double.MaxValue;
        var bestCount = int.MaxValue;
        var role = bot?.Class?.Role;

        foreach (var guild in _guilds.Values)
        {
            if (guild.Disbanded || guild.Members.Count >= (under > 0 ? under : Ceiling(guild)))
            {
                continue;
            }

            var count = guild.Members.Count;
            var share = role == null || count == 0 ? 0.0 : (double)OfRole(guild, role.Value) / count;

            if (share < bestShare - 0.001 || Math.Abs(share - bestShare) <= 0.001 && count < bestCount)
            {
                best = guild;
                bestShare = share;
                bestCount = count;
            }
        }

        return best;
    }

    private static int OfRole(Guild guild, BotRole role)
    {
        var n = 0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } member && member.Class?.Role == role)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>The full guild best able to pay for a widening — the one with the most to raise — or nothing.</summary>
    private static Guild Widest()
    {
        Guild best = null;
        var bestFund = -1;

        foreach (var guild in _guilds.Values)
        {
            if (guild.Disbanded || Widened(guild) >= MostWidenings || guild.Members.Count < Ceiling(guild))
            {
                continue;
            }

            var fund = BotEstate.Fund(guild);

            if (fund > bestFund)
            {
                bestFund = fund;
                best = guild;
            }
        }

        return best;
    }

    /// <summary>
    /// A name for a new guild: the first of the eight inherited names that is free, else one of the free fifty drawn at
    /// random. Free means no standing guild holds it, the engine keeps no other guild under it, and it is not resting
    /// after a closing (<see cref="NameRestHours"/>).
    /// </summary>
    private static bool FreeName(out (string Name, string Abbrev) named) => FreeName(out named, false);

    /// <summary>
    /// The same, and for a split (<see cref="Hive"/>) only one of the fifty with nothing under it: no hall standing under the
    /// name's sign, no town house and no square held in it. An inherited name hands its guild the hall under its sign
    /// (<c>BotEstate.Adopt</c>), and a guild founded by a split starts with nothing — Patrick's "under one of the fifty names".
    /// </summary>
    private static bool FreeName(out (string Name, string Abbrev) named, bool split)
    {
        for (var i = 0; !split && i < Inherited && i < Names.Length; i++)
        {
            if (Free(Names[i].Name))
            {
                named = Names[i];

                return true;
            }
        }

        var free = 0;

        for (var i = Inherited; i < Names.Length; i++)
        {
            if (Free(Names[i].Name) && (!split || Bare(Names[i].Name)))
            {
                free++;
            }
        }

        if (free > 0)
        {
            var pick = Utility.Random(free);

            for (var i = Inherited; i < Names.Length; i++)
            {
                if (Free(Names[i].Name) && (!split || Bare(Names[i].Name)) && pick-- == 0)
                {
                    named = Names[i];

                    return true;
                }
            }
        }

        named = default;

        return false;
    }

    /// <summary>Whether a new guild may take this name. See <see cref="FreeName"/>.</summary>
    private static bool Free(string name)
    {
        if (_guilds.TryGetValue(name, out var guild) && !guild.Disbanded)
        {
            return false;
        }

        if (Resting(name))
        {
            return false;
        }

        // A living guild the engine lists under this name that this population does not hold (one founded by hand): not ours
        // to take over. A disbanded one is what Form revives, as it always has.
        return BaseGuild.FindByName(name) is not Guild { Disbanded: false };
    }

    /// <summary>Whether nothing stands under this name: no hall under its sign, no town house, no square. Asked by <c>FreeName</c> for a split.</summary>
    private static bool Bare(string name) =>
        !BotEstate.Held.ContainsKey(name) && BotGuildHouses.Of(name) == null && BotClaim.Holds(name) == 0;

    /// <summary>
    /// The nearest bots that could found a guild with this one: loose and on the same facet. Distance
    /// orders them and never refuses them — see the class note — and neither does having walked out of a
    /// guild before: see <see cref="Gather"/>.
    /// </summary>
    private static List<BotMobile> Nearest(BotMobile founder, int many)
    {
        List<BotMobile> found = [];
        var roster = BotPopulation.Bots;

        for (var i = 0; i < roster.Count; i++)
        {
            var other = roster[i];

            if (!ReferenceEquals(other, founder) && Loose(other) && other.Map == founder.Map)
            {
                found.Add(other);
            }
        }

        found.Sort((a, b) => founder.GetDistanceToSqrt(a.Location).CompareTo(founder.GetDistanceToSqrt(b.Location)));

        // <b>The nearest of each trade first, then the nearest (29.09.2026).</b> A band founded on whoever stood closest was
        // a band of one trade; a band with a smith, a gatherer, a line and a bow among its five is a guild. See Roomiest.
        List<BotMobile> band = [];
        List<BotRole> roles = [];

        if (founder.Class != null)
        {
            roles.Add(founder.Class.Role);
        }

        for (var i = 0; i < found.Count && band.Count < many; i++)
        {
            var role = found[i].Class?.Role;

            if (role != null && !roles.Contains(role.Value))
            {
                roles.Add(role.Value);
                band.Add(found[i]);
            }
        }

        for (var i = 0; i < found.Count && band.Count < many; i++)
        {
            if (!band.Contains(found[i]))
            {
                band.Add(found[i]);
            }
        }

        return band;
    }

    /// <summary>A bot walks into a standing guild through <see cref="Gather"/>.</summary>
    private static void Admit(Guild guild, BotMobile bot)
    {
        guild.AddMember(bot);
        Show(bot);
        Joined(bot);
        Gathered++;

        logger.Information(
            "{Bot} the {Class} has joined {Guild}, {Count} of {Most} now, under {Head}",
            bot.Name,
            bot.Class?.Name,
            guild.Name,
            guild.Members.Count,
            Ceiling(guild),
            Head(guild)?.Name ?? "nobody"
        );
    }

    /// <summary>
    /// Makes one guild and puts the band in it.
    ///
    /// <para>
    /// <b>An existing guild of the same name is reused, and that is the whole of surviving a restart.</b>
    /// Guilds are world objects: they outlive the population, and the population is rebuilt from nothing
    /// every start. Making a fresh "The Hammer" each time would leave a graveyard of empty guilds in the
    /// save, one per restart, for ever. The engine counts a guild disbanded when its leader is deleted — so
    /// the first bot of a returning guild is made its leader, which revives it.
    /// </para>
    /// </summary>
    private static void Form((string Name, string Abbrev) named, List<BotMobile> band)
    {
        if (band == null || band.Count == 0)
        {
            return;
        }

        var leader = band[0];
        var guild = BaseGuild.FindByName(named.Name) as Guild;

        if (guild == null || guild.Disbanded)
        {
            guild = new Guild(leader, named.Name, named.Abbrev)
            {
                Type = GuildType.Regular,
                Charter = "A company of the island's own."
            };
        }
        else
        {
            // Back from the save with a leader who no longer exists, and a membership list full of bots that
            // were deleted with the last population. Both are cleared out before the new band moves in.
            guild.Leader = leader;

            for (var i = guild.Members.Count - 1; i >= 0; i--)
            {
                if (guild.Members[i] is not BotMobile { Deleted: false })
                {
                    guild.RemoveMember(guild.Members[i]);
                }
            }
        }

        _guilds[named.Name] = guild;

        // Before anybody is put in it, and whichever branch made it. See Stone: without one the engine
        // disbands the guild the first time it is read back from a save.
        Stone(guild);

        // Where it lives: by another town, by BotSeat.AbroadShare, or at home. Patrick's order of 29.09.2026.
        BotSeat.Choose(named.Name, leader.Map);

        for (var i = 0; i < band.Count; i++)
        {
            guild.AddMember(band[i]);
            Show(band[i]);
            Joined(band[i]);
            Enrolled++;
        }

        // A guild founded after The Shadow counts it an enemy from the first minute, as every other guild does.
        BotUnderworld.EnemyOfAll();
    }

    /// <summary>
    /// Lets the band's mark be seen over a bot's head, which is not the same thing as being in the band.
    ///
    /// <para>
    /// <b>Patrick could not see the abbreviation in the world on 10.09.2026, and the reason is one engine
    /// flag.</b> <c>BotMobile.AddNameProperties</c> writes <c>[HAM] Alden</c> and that is the tooltip's
    /// line, which this era's client does not ask for; the label the client actually shows over a head comes
    /// from <c>Mobile.OnSingleClick</c>, and its guild line is gated on
    /// <c>m_DisplayGuildTitle || (m_Player &amp;&amp; guild.Type != GuildType.Regular)</c>. Bots are
    /// player-flagged and their guilds are of the ordinary type, so the second half is false and the first
    /// had never been set by anybody: the band existed and was invisible.
    /// </para>
    ///
    /// <para>
    /// Set on joining rather than at birth, because it is about the guild and a bot has none until here.
    /// </para>
    /// </summary>
    private static void Show(Mobile bot)
    {
        if (bot is { Deleted: false })
        {
            bot.DisplayGuildTitle = true;
        }
    }

    /// <summary>
    /// One bot arriving after the roster was dealt: it joins the smallest guild with room.
    ///
    /// <para>
    /// Does nothing at all until <see cref="Muster"/> has run, and that is deliberate: during the opening
    /// burst every bot in the population is born within a second of every other, and a rule about groups
    /// cannot be applied to the first of them.
    /// </para>
    /// </summary>
    public static void Enrol(BotMobile bot)
    {
        if (!Running || !_dealt || bot is not { Deleted: false } || bot.Guild != null)
        {
            return;
        }

        // <b>Asked here as well as in the muster, and both are needed.</b> The muster deals the roster once;
        // this is the door a bot born afterwards comes through, and a rule enforced on only one of the two
        // is a rule that holds until the first bot is replaced.
        if (Outside(bot))
        {
            Excused++;

            return;
        }

        // And the bar a bot earns by walking out. See RejoinMs: this is the only door back in, so this is
        // the only place the wait can be kept.
        if (Cools(bot))
        {
            Cooling++;

            return;
        }

        Guild smallest = null;

        foreach (var guild in _guilds.Values)
        {
            if (guild.Disbanded || guild.Members.Count >= Ceiling(guild))
            {
                continue;
            }

            if (smallest == null || guild.Members.Count < smallest.Members.Count)
            {
                smallest = guild;
            }
        }

        if (smallest == null)
        {
            Unplaced++;

            return;
        }

        smallest.AddMember(bot);
        Show(bot);
        Joined(bot);
        Enrolled++;
    }

    /// <summary>
    /// Whether a member may walk out of a guild that is getting nowhere.
    ///
    /// <b>Patrick's order of 11.09.2026: being in a guild is not an obligation.</b> Until this, a bot was
    /// dealt into a band at birth and stayed in it whatever the band did — which made a guild a caste rather
    /// than an association, and made every number about guild success unreadable, because nobody could vote
    /// on it with their feet.
    /// </summary>
    public static bool Leaving { get; set; } = true;

    /// <summary>
    /// How long a bot gives a new guild before it starts asking what it is getting.
    ///
    /// Thirty minutes. A guild raises its hall out of a levy on its members and that takes most of an hour
    /// on this shard; a bot that walks out at twenty minutes has left over something that had not had time
    /// to happen. This is the number that separates "getting nowhere" from "not there yet".
    /// </summary>
    public static int Patience { get; set; } = 1800000;

    /// <summary>
    /// How long a bot that has walked out must wait before any guild will take it.
    ///
    /// <para>
    /// <b>Two days, by order, and it is worth being plain about what that means here.</b> This shard's
    /// sessions run for hours and the population is rebuilt from nothing at every restart, so a bar of
    /// forty-eight hours is, in practice, "not again this session". That is the intended weight: leaving is
    /// a decision a bot makes once, not a door it swings through whenever the ledger dips.
    /// </para>
    /// </summary>
    public static int RejoinMs { get; set; } = 172800000;

    /// <summary>How often the guilds are looked at. One guild is reviewed per look, in turn.</summary>
    public static int ReviewMs { get; set; } = 60000;

    /// <summary>Members who have walked out.</summary>
    public static long Walked { get; private set; }

    /// <summary>Times a bot was refused a guild because it had walked out of one lately.</summary>
    public static long Cooling { get; private set; }

    /// <summary>
    /// When each bot last walked out, and of which guild. Absence means it never has.
    ///
    /// <b>The name is kept as well as the moment, and the reason is the roster.</b> A leader may now take
    /// bots in (see <see cref="BotRoster"/>), and the only rule that keeps that from becoming a revolving
    /// door is "not back into the one you walked out of" — which cannot be asked of a tick on its own.
    ///
    /// <para>
    /// <b>Kept across restarts since 06.10.2026</b> (<see cref="BotGuildSplitStore"/>, as running time elapsed): the five who
    /// found a guild out of a full one (<see cref="Hive"/>) are written here against the guild they left, and a bar forgotten at
    /// the next boot would have let them cross straight back into their old places.
    /// </para>
    /// </summary>
    private static readonly Dictionary<Serial, (long Tick, string Guild)> _quit = [];

    /// <summary>When each bot joined the guild it is in now, for <see cref="Patience"/>.</summary>
    private static readonly Dictionary<Serial, long> _joined = [];

    private static long _reviewed;

    private static int _turn;

    /// <summary>Whether this bot is inside the bar it earned by walking out. See <see cref="RejoinMs"/>.</summary>
    public static bool Cools(Mobile bot) =>
        bot != null && _quit.TryGetValue(bot.Serial, out var when) && Core.TickCount - (when.Tick + RejoinMs) < 0;

    /// <summary>
    /// Whether this bot walked out of <i>this</i> guild lately.
    ///
    /// <para>
    /// <b>The narrow half of <see cref="Cools"/>, and the one a leader is held to.</b> The wide bar exists
    /// because a bot that walks out has decided something about being in a guild at all, and it should not
    /// undo that on the next beat. A leader offering a place is a different event — somebody asked — so the
    /// bar it is held to is the one that stops a guild bleeding a member and hiring it straight back, which
    /// is the loop this shard has built by accident more often than any other.
    /// </para>
    /// </summary>
    public static bool Spurned(Mobile bot, Guild guild) =>
        bot != null
        && guild != null
        && _quit.TryGetValue(bot.Serial, out var when)
        && string.Equals(when.Guild, guild.Name, StringComparison.OrdinalIgnoreCase)
        && Core.TickCount - (when.Tick + RejoinMs) < 0;

    /// <summary>
    /// What a member can point to and say the guild did it.
    ///
    /// <para>
    /// <b>Three things, and every one of them is something the guild has that a lone bot could not get.</b>
    /// A hall, ground of its own, and money in the common purse. Not skills, not kills, not what its members
    /// happen to be worth — those are the members' own and would let a guild take credit for the work of the
    /// people thinking of leaving it.
    /// </para>
    /// </summary>
    public static int Worth(Guild guild)
    {
        if (guild == null)
        {
            return 0;
        }

        var worth = BotEstate.Hall(guild) is { Deleted: false } ? 2 : 0;

        worth += BotClaim.Holds(guild.Name);

        // A house in a town, with its door and sign, is something to show. See BotGuildHouses (29.09.2026, evening).
        if (BotGuildHouses.Held(guild))
        {
            worth++;
        }

        if (BotEstate.Fund(guild) >= BotEstate.Price / 2)
        {
            worth++;
        }

        return worth;
    }

    /// <summary>
    /// Whether a member that has had enough leaves <em>for another guild</em> rather than into nothing.
    ///
    /// <para>
    /// <b>Voting with your feet needs somewhere for the feet to go.</b> The order of 11.09.2026 was that
    /// being in a guild is not an obligation, and the first version read that as "walk out": the bot left,
    /// stood outside everything, and was barred from every guild for <see cref="RejoinMs"/>. Measured on
    /// 18.09.2026: forty-six walked out in four hours, thirty-six of them in the first hour it was allowed;
    /// The Hammer, The Blade and The Needle ended at two, two and one while the two guilds that already had
    /// halls kept fourteen and fifteen. And it is a remedy locked behind its own gate — a guild's purse is
    /// what its members can spare, so every member that walked out put the hall further off, which is the
    /// thing the next member was waiting for. The Hammer had 709gp of 5000 that day.
    /// </para>
    ///
    /// <para>
    /// So a member leaves only when some other guild has something to show and room to take it, and it
    /// crosses straight over. With nowhere better to go it stays, and the look is counted
    /// <see cref="Nowhere"/>. The two-day bar keeps its meaning: a bot crosses once and is then a member
    /// like any other until the bar runs out.
    /// </para>
    /// </summary>
    public static bool Defecting { get; set; } = true;

    /// <summary>Members who left one guild for another. A subset of <see cref="Walked"/>.</summary>
    public static long Crossed { get; private set; }

    /// <summary>Looks at a guild with nothing to show that found no other guild worth leaving it for.</summary>
    public static long Nowhere { get; private set; }

    /// <summary>
    /// The guild most worth leaving this one for: something to show, room, not at war with this one. The
    /// most to show first, the fewest members among equals.
    /// </summary>
    private static Guild Worthier(Guild than)
    {
        Guild best = null;
        var bestWorth = 0;

        foreach (var other in _guilds.Values)
        {
            if (other == null || ReferenceEquals(other, than) || other.Disbanded || other.Members.Count >= Ceiling(other))
            {
                continue;
            }

            if (than.Enemies != null && than.Enemies.Contains(other))
            {
                continue;
            }

            var worth = Worth(other);

            if (worth <= 0)
            {
                continue;
            }

            if (best == null || worth > bestWorth || (worth == bestWorth && other.Members.Count < best.Members.Count))
            {
                best = other;
                bestWorth = worth;
            }
        }

        return best;
    }

    /// <summary>
    /// Looks at one guild and lets at most one member walk out of it.
    ///
    /// <para>
    /// <b>One a look, and one guild a look, on purpose.</b> Fifteen bots reading the same empty ledger in the
    /// same beat would all leave in the same beat, and a rule that can empty a guild in one tick is not a
    /// decision anybody made — it is a stampede with a threshold. This way a guild that is going nowhere
    /// bleeds a member a minute, which is slow enough that the guild can fix itself and visible enough to
    /// read off the log.
    /// </para>
    ///
    /// <para>
    /// <b>The head never leaves.</b> Until 21.09.2026 this said the maker, because a guild was founded on
    /// one; now anybody founds, and the member whose walking out would be the guild dissolving by a side
    /// door is whoever leads it. If Patrick wants guilds to be able to die, that is its own order and its
    /// own code — and since 06.10.2026 it has both: a guild nobody wants closes as a whole, see <see cref="Wane"/>.
    /// </para>
    /// </summary>
    public static void Review()
    {
        if (!Running || _guilds.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;

        if (now - (_reviewed + ReviewMs) < 0)
        {
            return;
        }

        _reviewed = now;

        // Every guild's attractiveness clock on the same look, and at most one closing (06.10.2026). See Wane.
        Wane(now);

        // Five of a full guild found a new one, at most one split a look (06.10.2026). See Hive.
        Hive(now);

        if (!Leaving || _guilds.Count == 0)
        {
            return;
        }

        List<Guild> bands = [.. _guilds.Values];

        if (bands.Count == 0)
        {
            return;
        }

        var guild = bands[_turn++ % bands.Count];

        if (guild == null || guild.Disbanded || Worth(guild) > 0)
        {
            return;
        }

        // Somewhere to go, asked before anybody is asked whether it wants to. See Defecting.
        var better = Defecting ? Worthier(guild) : null;

        if (Defecting && better == null)
        {
            Nowhere++;

            return;
        }

        var members = guild.Members;

        for (var i = 0; i < members.Count; i++)
        {
            // The head, where this used to say the maker. See Head: a guild founded by anybody has no maker by
            // construction, and the one member whose leaving is the guild dissolving is whoever leads it.
            if (members[i] is not BotMobile { Deleted: false, Alive: true } bot || IsHead(bot))
            {
                continue;
            }

            if (!_joined.TryGetValue(bot.Serial, out var since) || now - (since + Patience) < 0)
            {
                continue;
            }

            // Leaving is a decision a bot makes once in RejoinMs, and with somewhere to go that has to be
            // asked here: a bot that crossed over an hour ago is a member like any other, and without this
            // it would cross again the first time its new guild had a bad half hour.
            if (Cools(bot))
            {
                continue;
            }

            if (better != null)
            {
                if (Spurned(bot, better))
                {
                    continue;
                }

                // AddMember takes the bot out of the guild it is in, so there is no moment outside both.
                better.AddMember(bot);
                Show(bot);

                _quit[bot.Serial] = (now, guild.Name);
                Joined(bot);
                Walked++;
                Crossed++;

                logger.Information(
                    "{Bot} the {Class} has left {Guild} for {Better}: it has no hall, no ground and no purse to speak of after {Minutes} minutes, and {Better} is {Showing} with {Count} of {Most}",
                    bot.Name,
                    bot.Class?.Name,
                    guild.Name,
                    better.Name,
                    (now - since) / 60000,
                    better.Name,
                    Worth(better) >= 2 ? "holding a hall" : "holding ground or a purse",
                    better.Members.Count,
                    Ceiling(better)
                );

                return;
            }

            guild.RemoveMember(bot);
            bot.DisplayGuildTitle = false;

            _quit[bot.Serial] = (now, guild.Name);
            _joined.Remove(bot.Serial);
            Walked++;

            logger.Information(
                "{Bot} has left {Guild}: it has no hall, no ground and no purse to speak of, and {Bot} had given it {Minutes} minutes",
                bot.Name,
                guild.Name,
                bot.Name,
                (now - since) / 60000
            );

            return;
        }
    }

    /// <summary>
    /// How much a guild is worth belonging to, in one number: <see cref="Worth(Guild)"/> — what it has to show (a hall 2,
    /// a square 1 each, a town house 1, a purse of half a hall's price 1) — plus its members over <see cref="Least"/>,
    /// so five members are one point and thirty-five are seven.
    ///
    /// <para>
    /// <b>Patrick's order of 06.10.2026: "if guilds are not in demand and their attractiveness is low, they may
    /// close."</b> The two halves are the two things the code already knew about being wanted. What a guild has is
    /// what a member crosses over for (<see cref="Worthier"/>); how many have stayed is the vote with the feet that the
    /// crossing counts. The crossings themselves are not added in: they are already in the members, and counting them
    /// again would count one departure twice.
    /// </para>
    /// </summary>
    public static double Appeal(Guild guild) =>
        guild is not { Disbanded: false } ? 0.0 : Worth(guild) + (double)guild.Members.Count / Math.Max(1, Least) + BotRelics.AppealOf(guild);

    /// <summary>Whether guilds nobody wants close. See <see cref="Wane"/>.</summary>
    public static bool Closing { get; set; } = true;

    /// <summary>
    /// The attractiveness under which a guild of fewer than <see cref="Least"/> counts as unwanted: two, so a hall alone
    /// keeps a guild open, and so does any two of a square, a house and a purse; one of them with four members or fewer
    /// does not.
    /// </summary>
    public static double LowAppeal { get; set; } = 2.0;

    /// <summary>
    /// How long a guild must stay unwanted, with nobody joining it, before it closes: two hours of the shard running.
    /// Kept across restarts (<see cref="BotGuildWidenStore"/>), so a shard restarted every hour still closes one.
    /// </summary>
    public static int CloseAfterMs { get; set; } = 7200000;

    /// <summary>
    /// How long a closed guild's name rests before a new guild may take it: a week. Its regard, its grievances and its
    /// standing in the towns are still on the books under that name (<c>BotRegard</c>, <c>BotDiplomacyStore</c>), unread
    /// while nobody holds it; a week is long enough that a new guild under the old name is not the old guild reborn.
    /// </summary>
    public static int NameRestHours { get; set; } = 168;

    /// <summary>Guilds closed, and the gold of their chests shared among their last members.</summary>
    public static long Closed { get; private set; }

    public static long ClosedGold { get; private set; }

    /// <summary>Members a closing put out of their guild for <see cref="Gather"/> to place.</summary>
    public static long Released { get; private set; }

    /// <summary>
    /// Looks that found a guild due to close and held it open, by reason. Looks, not guilds: a guild held back is counted
    /// again every minute it is held.
    /// </summary>
    public static long HeldAtWar { get; private set; }

    public static long HeldClaiming { get; private set; }

    public static long HeldInCompany { get; private set; }

    public static long HeldNoRoom { get; private set; }

    /// <summary>When each unwanted guild was first seen unwanted. Kept across restarts as the time elapsed.</summary>
    private static readonly Dictionary<string, long> _low = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How many members each guild had at the last look, so a guild that took somebody in starts its clock again.</summary>
    private static readonly Dictionary<string, int> _seen = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>When each closed name was closed, in the world's time. See <see cref="NameRestHours"/>.</summary>
    private static readonly Dictionary<string, DateTime> _closed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The names founded and closed since the shard came up, for the summary.</summary>
    private static readonly List<string> _foundedNames = [];

    private static readonly List<string> _closedNames = [];

    /// <summary>Minutes this guild has been unwanted, or nought.</summary>
    public static int LowMinutes(Guild guild) =>
        guild?.Name != null && _low.TryGetValue(guild.Name, out var since) ? (int)Math.Max(0, (Core.TickCount - since) / 60000) : 0;

    /// <summary>Whether a closed guild's name is still resting. See <see cref="NameRestHours"/>.</summary>
    private static bool Resting(string name) =>
        name != null && _closed.TryGetValue(name, out var when) && Core.Now - when < TimeSpan.FromHours(Math.Max(0, NameRestHours));

    /// <summary>Closed names still resting.</summary>
    private static int RestingNames()
    {
        var n = 0;

        foreach (var name in _closed.Keys)
        {
            if (Resting(name))
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Guilds standing that the engine has not disbanded.</summary>
    private static int Living()
    {
        var n = 0;

        foreach (var guild in _guilds.Values)
        {
            if (!guild.Disbanded)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Free places in every standing guild but this one: where its members would go if it closed.</summary>
    private static int Room(Guild but)
    {
        var room = 0;

        foreach (var guild in _guilds.Values)
        {
            if (ReferenceEquals(guild, but) || guild.Disbanded)
            {
                continue;
            }

            room += Math.Max(0, Ceiling(guild) - guild.Members.Count);
        }

        return room;
    }

    /// <summary>
    /// Keeps every guild's unwanted clock and closes at most one guild whose clock has run out.
    ///
    /// <para>
    /// <b>Unwanted is three facts at once:</b> fewer than <see cref="Least"/> members, attractiveness under
    /// <see cref="LowAppeal"/>, and nobody joined since the last look — a guild that takes anybody in starts its clock
    /// again. After <see cref="CloseAfterMs"/> of that the guild closes, the head's guild as much as any. On the summaries
    /// of 04.10.2026 the five guilds of thirty-five never read under 8.0, the lowest guild of five or more was The Hammer
    /// at 1.4 with seven members (outside the rule by its count), and the rule would have closed The Needle (0.2 all day)
    /// at 06:25 and The Ash (1 member, 0.2) at 07:15; on the evening's log The Crown (3 members, 0.6) at 21:25.
    /// </para>
    ///
    /// <para>
    /// <b>Held open, each counted:</b> while it is at war (the war ends by its own rule first; a guild that is gone
    /// cannot be scored), while it is laying claim to a square, while any member stands in a company, and while the
    /// other guilds have fewer free places than it has members — closing must not leave a bot outside every guild, which
    /// is the one thing <see cref="Gather"/> cannot mend.
    /// </para>
    /// </summary>
    private static void Wane(long now)
    {
        List<Guild> due = null;
        List<string> stale = null;

        // A clock read back off the save for a guild that did not come back with it.
        foreach (var name in _low.Keys)
        {
            if (!_guilds.ContainsKey(name))
            {
                (stale ??= []).Add(name);
            }
        }

        for (var i = 0; stale != null && i < stale.Count; i++)
        {
            _low.Remove(stale[i]);
        }

        foreach (var (name, guild) in _guilds)
        {
            if (guild.Disbanded)
            {
                continue;
            }

            var count = guild.Members.Count;
            var gained = _seen.TryGetValue(name, out var was) && count > was;

            _seen[name] = count;

            if (count >= Least || gained || Appeal(guild) >= LowAppeal)
            {
                _low.Remove(name);

                continue;
            }

            // A guild founded by a split has its own time before its clock may start (06.10.2026): five who left a full guild
            // are not judged by the rule for a guild nobody wants on the day they leave. See YoungMs.
            if (Young(name, now))
            {
                _low.Remove(name);
                HeldYoung++;

                continue;
            }

            if (!_low.TryGetValue(name, out var since))
            {
                _low[name] = now;

                continue;
            }

            if (now - (since + CloseAfterMs) >= 0)
            {
                (due ??= []).Add(guild);
            }
        }

        if (!Closing || due == null)
        {
            return;
        }

        for (var i = 0; i < due.Count; i++)
        {
            var guild = due[i];

            if (BotWar.Fighting(guild.Name) > 0)
            {
                HeldAtWar++;

                continue;
            }

            if (BotClaim.Making(guild) != null)
            {
                HeldClaiming++;

                continue;
            }

            if (InCompany(guild))
            {
                HeldInCompany++;

                continue;
            }

            if (Room(guild) < guild.Members.Count)
            {
                HeldNoRoom++;

                continue;
            }

            if (Close(guild, now))
            {
                return;
            }
        }
    }

    private static bool InCompany(Guild guild)
    {
        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false, Squad: not null })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Closes one guild: its chest shared among its last members, the members let go for <see cref="Gather"/> to place,
    /// the engine's disbanding (which takes its guildstone, its enemies and its allies with it), its seat, its widenings
    /// and its name taken off the books.
    ///
    /// <para>
    /// <b>What the rest of the shard already does for a guild that is gone, and is left to do it.</b> Its squares belong
    /// to nobody at the claim's next look (<c>BotClaim.Sweep</c>), its town house is vacated at the houses' next looks
    /// (<c>BotGuildHouses.Look</c>), and wars are not an issue because a guild at war is not closed. It holds no hall by
    /// construction: a hall alone is two points of attractiveness. The coin is moved, never made or lost: the chest's
    /// gold goes to the members' banks in equal shares, the odd coins to the head, and the log line says how much.
    /// </para>
    /// </summary>
    private static bool Close(Guild guild, long now)
    {
        var name = guild.Name;
        var abbrev = guild.Abbreviation;
        var appeal = Appeal(guild);
        var lowMinutes = LowMinutes(guild);
        var head = Head(guild);
        List<BotMobile> members = [];

        if (head != null)
        {
            members.Add(head);
        }

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } member && !ReferenceEquals(member, head))
            {
                members.Add(member);
            }
        }

        // Somebody has to receive the chest; a guild with no member left is the engine's to disband, not this.
        if (members.Count == 0)
        {
            return false;
        }

        var gold = BotChest.Empty(name);
        var each = gold / members.Count;
        var odd = gold - each * members.Count;

        for (var i = 0; i < members.Count; i++)
        {
            var share = each + (i == 0 ? odd : 0);

            if (share > 0 && !Banker.Deposit(members[i], share))
            {
                members[i].AddToBackpack(new Gold(share));
            }
        }

        guild.Disband();

        var released = new string[members.Count];

        for (var i = 0; i < members.Count; i++)
        {
            var bot = members[i];

            bot.DisplayGuildTitle = false;
            _joined.Remove(bot.Serial);

            // Not a walk-out: the guild closed under it. Whatever bar it earned by crossing into this guild is lifted, or it
            // would stand outside every guild for two days. See Cools.
            _quit.Remove(bot.Serial);
            released[i] = $"{bot.Name} the {bot.Class?.Name}";
        }

        _guilds.Remove(name);
        _low.Remove(name);
        _seen.Remove(name);
        _widened.Remove(name);
        _young.Remove(name);
        _splitAt.Remove(name);
        _left.Remove(name);
        _closed[name] = Core.Now;
        _closedNames.Add(name);
        BotSeat.Drop(name);

        Closed++;
        ClosedGold += gold;
        Released += members.Count;

        logger.Warning(
            "{Guild} [{Abbrev}] has closed: attractiveness {Appeal:F1} under {Line:F1} with {Members} members for {Minutes} minutes and nobody joining; its chest of {Gold}gp was shared among {Released}; {Left} guilds stand, and the name rests {Hours} hours",
            name,
            abbrev,
            appeal,
            LowAppeal,
            members.Count,
            lowMinutes,
            gold,
            string.Join(", ", released),
            _guilds.Count,
            NameRestHours
        );

        BotAlarm.Note("guild", $"{name} closed: attractiveness {appeal:F1} with {members.Count} members for {lowMinutes} minutes, nobody joining; {gold}gp shared", members.Count, _guilds.Count, $"{lowMinutes}m");

        return true;
    }

    /// <summary>The unwanted clocks and the resting names, for the store. See <see cref="BotGuildWidenStore"/>.</summary>
    internal static void SaveClosings(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(_low.Count);

        foreach (var (name, since) in _low)
        {
            writer.Write(name);
            writer.Write(Math.Max(0L, now - since));
        }

        writer.WriteEncodedInt(_closed.Count);

        foreach (var (name, when) in _closed)
        {
            writer.Write(name);
            writer.Write(when);
        }
    }

    internal static int LoadClosings(IGenericReader reader)
    {
        _low.Clear();
        _closed.Clear();

        var now = Core.TickCount;
        var low = reader.ReadEncodedInt();

        for (var i = 0; i < low; i++)
        {
            var name = reader.ReadString();
            var elapsed = reader.ReadLong();

            if (!string.IsNullOrEmpty(name))
            {
                _low[name] = now - Math.Max(0L, elapsed);
            }
        }

        var closed = reader.ReadEncodedInt();

        for (var i = 0; i < closed; i++)
        {
            var name = reader.ReadString();
            var when = reader.ReadDateTime();

            if (!string.IsNullOrEmpty(name))
            {
                _closed[name] = when;
            }
        }

        return _low.Count;
    }

    /// <summary>Whether five members of a full guild may leave it to found a new one. See <see cref="Hive"/>.</summary>
    public static bool Splitting { get; set; } = true;

    /// <summary>
    /// The attractiveness at or above which a guild with room counts as somewhere to go, so that no split is made while it
    /// stands: five. A hall (2) and fifteen members (3), or anything that shows as much.
    ///
    /// <para>
    /// <b>Measured on the night of 06.10.2026, and why it is not <see cref="LowAppeal"/>.</b> The one guild with room all night
    /// was The Ash: 1 to 5 members, a hall and a square, appeal 3.2 to 4.0 for all but forty minutes (5.0–6.0 from 06:32 to
    /// 07:17), against 8.0 to 42.0 for the five guilds of thirty-five. At the closing bar of 2.0 it would have stopped every
    /// split of the night, and no rule moves a member of a guild with something to show into its places: a bar that blocks
    /// and a door that does not exist add up to nothing ever happening. Five says a guild with room is an answer only when it
    /// is a guild a member of a guild of thirty-five would recognise as one.
    /// </para>
    /// </summary>
    public static double SplitBar { get; set; } = 5.0;

    /// <summary>
    /// How long a member must stand under the keep (<see cref="BotEstate.Keep"/>) while its guild's chest holds at least
    /// <see cref="RichChest"/>, unbroken, before it counts as getting no share: an hour.
    /// </summary>
    public static int UnsharedMs { get; set; } = 3600000;

    /// <summary>A chest this full is rich: half a hall's price, the same line at which a purse counts in <see cref="Worth(Guild)"/>.</summary>
    public static int RichChest => BotEstate.Price / 2;

    /// <summary>The least running time between two splits anywhere on the island: three hours. Patrick: "a new guild every few hours, not every ten minutes".</summary>
    public static int SplitGapMs { get; set; } = 10800000;

    /// <summary>The least running time before a guild that lost five to a split may lose five again: twelve hours.</summary>
    public static int SplitParentMs { get; set; } = 43200000;

    /// <summary>
    /// How long a guild founded by a split is spared the closing clock (<see cref="Wane"/>): six hours of running time. Five
    /// members are already over the closing rule's line; this covers the guild that loses one to an outlawing or a closing's
    /// placement in its first hours, which would otherwise close two hours later with nothing yet built.
    /// </summary>
    public static int YoungMs { get; set; } = 21600000;

    /// <summary>Guilds founded by a split.</summary>
    public static long Splits { get; private set; }

    /// <summary>Founders, by the reason each had (one founder may count under more than one).</summary>
    public static long FoundersUnshared { get; private set; }

    public static long FoundersFar { get; private set; }

    public static long FoundersQuarrel { get; private set; }

    /// <summary>
    /// Looks that made no split, by the first thing that stopped them. Looks, not guilds, one a minute: a full guild held
    /// back by the gap is counted again every minute it is held.
    /// </summary>
    public static long SplitFew { get; private set; }

    public static long SplitAtWar { get; private set; }

    public static long SplitParentCool { get; private set; }

    public static long SplitAtMost { get; private set; }

    public static long SplitRoom { get; private set; }

    public static long SplitGap { get; private set; }

    public static long SplitNameless { get; private set; }

    /// <summary>Closing looks that found a guild founded by a split unwanted and held it inside <see cref="YoungMs"/>.</summary>
    public static long HeldYoung { get; private set; }

    /// <summary>When each member first stood under the keep while its guild's chest was rich, unbroken since. In memory: an hour.</summary>
    private static readonly Dictionary<Serial, long> _unshared = [];

    private static readonly HashSet<Serial> _unsharedNow = [];

    private static readonly List<Serial> _unsharedGone = [];

    /// <summary>When each guild last lost five to a split. Kept across restarts as running time elapsed.</summary>
    private static readonly Dictionary<string, long> _splitAt = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Places each guild's splits left, not yet filled. They are the founders' old places, barred to them (<see cref="Spurned"/>),
    /// and no rule moves anybody else into a guild with something to show — so they are not room another five could take,
    /// and counted as room they would stop every split after the first. Shrinks as the guild fills.
    /// </summary>
    private static readonly Dictionary<string, int> _left = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Guilds founded by a split: when, and out of which guild.</summary>
    private static readonly Dictionary<string, (long Tick, string From)> _young = new(StringComparer.OrdinalIgnoreCase);

    private static long _lastSplit;

    private static bool _hasSplit;

    /// <summary>"The Raven ← The Lantern", for the summary.</summary>
    private static readonly List<string> _splitNames = [];

    private const int WhyUnshared = 1;

    private const int WhyFar = 2;

    private const int WhyQuarrel = 4;

    /// <summary>Whether this guild was founded by a split and is still inside <see cref="YoungMs"/>.</summary>
    private static bool Young(string name, long now) =>
        name != null && _young.TryGetValue(name, out var young) && now - (young.Tick + YoungMs) < 0;

    /// <summary>
    /// Five members of a full guild leave it and found a new one under one of the fifty names. At most one split a look.
    ///
    /// <para>
    /// <b>Patrick, 06.10.2026, ~10:00: "Five leaving a full guild to found a new one under one of the fifty names: we do it."</b>
    /// Until this, no member of a guild with something to show ever left it (<see cref="Review"/> looks only at a guild with
    /// <see cref="Worth(Guild)"/> 0), so on a world where every bot is in a guild the fifty names were used only after a wipe.
    /// </para>
    ///
    /// <para>
    /// <b>The rule.</b> A guild at its <see cref="Ceiling"/> (crowding: it can take nobody) loses five when at least five of its
    /// members have a reason of their own, by rules the shard already keeps:
    /// </para>
    /// <list type="bullet">
    /// <item><b>no share</b> — the member has stood under the keep (<see cref="BotEstate.Keep"/>, 800gp) for
    /// <see cref="UnsharedMs"/> unbroken while the guild's chest held <see cref="RichChest"/> (2500gp) or more;</item>
    /// <item><b>far</b> — it lives (<see cref="BotResidence"/>) in another town than its guild's house or hall, or neither the
    /// hall nor the house stands within <see cref="BotSeat.Settled"/> tiles of its town;</item>
    /// <item><b>a quarrel</b> — half or more of the other guilds hold its guild at or under <see cref="BotRegard.Enmity"/>, or the
    /// town it lives in has put its guild out (<see cref="BotBurgh.Bars"/>).</item>
    /// </list>
    /// <para>
    /// The head never leaves, nor a member in a company, nor one inside the bar for having left a guild lately. The five are
    /// those with the most reasons, living where most of them live, the farthest from the guild's seat first; the most
    /// aggrieved leads. Refused, each counted: the guild at war, inside its own <see cref="SplitParentMs"/>,
    /// <see cref="MostGuilds"/> standing, a guild with room at or above <see cref="SplitBar"/>, inside <see cref="SplitGapMs"/>
    /// of the last split, no bare name.
    /// </para>
    ///
    /// <para>
    /// <b>What keeps it from ping-ponging.</b> The founders are written into the walk-out bar against the guild they left,
    /// saved across restarts (<see cref="BotGuildSplitStore"/>): no guild takes them for <see cref="RejoinMs"/> and they never
    /// cross out of the new guild meanwhile (<see cref="Cools"/>), so a new guild with nothing to show does not bleed back into
    /// the places they left. The guild they left may not split again for twelve hours; no split anywhere for three; and the new
    /// guild is spared the closing clock for six (<see cref="YoungMs"/>).
    /// </para>
    ///
    /// <para>
    /// <b>What they take.</b> Their own packs and banks, nothing else: the chest stays with the guild they left, they give back
    /// their keys to its hall and outpost, and the new guild starts with no hall, no ground and no house, seated by the town
    /// most of them live in (a bare seat, which the guild-house settler reads), free to claim free squares by the rules every
    /// guild claims by.
    /// </para>
    /// </summary>
    private static void Hive(long now)
    {
        if (!Running || !_dealt)
        {
            return;
        }

        Unshared(now);
        ShrinkLeft();

        if (!Splitting)
        {
            return;
        }

        Guild parent = null;
        List<(BotMobile Bot, int Why, int Far)> leavers = null;
        var war = false;
        var cool = false;

        foreach (var guild in _guilds.Values)
        {
            if (guild.Disbanded || guild.Members.Count < Ceiling(guild))
            {
                continue;
            }

            var found = Leavers(guild, now);

            if (found.Count < Least)
            {
                continue;
            }

            if (_splitAt.TryGetValue(guild.Name, out var at) && now - (at + SplitParentMs) < 0)
            {
                cool = true;

                continue;
            }

            if (BotWar.Fighting(guild.Name) > 0)
            {
                war = true;

                continue;
            }

            if (leavers == null || found.Count > leavers.Count)
            {
                parent = guild;
                leavers = found;
            }
        }

        if (parent == null)
        {
            if (war)
            {
                SplitAtWar++;
            }
            else if (cool)
            {
                SplitParentCool++;
            }
            else
            {
                SplitFew++;
            }

            return;
        }

        if (Living() >= MostGuilds)
        {
            SplitAtMost++;

            return;
        }

        if (WantedRoom(parent) != null)
        {
            SplitRoom++;

            return;
        }

        if (_hasSplit && now - (_lastSplit + SplitGapMs) < 0)
        {
            SplitGap++;

            return;
        }

        if (!FreeName(out var named, true))
        {
            SplitNameless++;

            return;
        }

        Found(parent, leavers, named, now);
    }

    /// <summary>Keeps every member's no-share clock: under the keep while its guild's chest is rich, unbroken.</summary>
    private static void Unshared(long now)
    {
        _unsharedNow.Clear();

        foreach (var guild in _guilds.Values)
        {
            if (guild.Disbanded || BotChest.Holds(guild.Name) < RichChest)
            {
                continue;
            }

            for (var i = 0; i < guild.Members.Count; i++)
            {
                if (guild.Members[i] is BotMobile { Deleted: false } member && BotYield.Spendable(member) < BotEstate.Keep)
                {
                    _unsharedNow.Add(member.Serial);
                    _unshared.TryAdd(member.Serial, now);
                }
            }
        }

        _unsharedGone.Clear();

        foreach (var serial in _unshared.Keys)
        {
            if (!_unsharedNow.Contains(serial))
            {
                _unsharedGone.Add(serial);
            }
        }

        for (var i = 0; i < _unsharedGone.Count; i++)
        {
            _unshared.Remove(_unsharedGone[i]);
        }
    }

    /// <summary>The places a split left shrink to what the guild still has free: a guild that filled them owes nothing.</summary>
    private static void ShrinkLeft()
    {
        if (_left.Count == 0)
        {
            return;
        }

        List<(string Name, int Left)> change = null;

        foreach (var (name, left) in _left)
        {
            var free = _guilds.TryGetValue(name, out var guild) && !guild.Disbanded ? Math.Max(0, Ceiling(guild) - guild.Members.Count) : 0;

            if (free < left)
            {
                (change ??= []).Add((name, free));
            }
        }

        for (var i = 0; change != null && i < change.Count; i++)
        {
            if (change[i].Left <= 0)
            {
                _left.Remove(change[i].Name);
            }
            else
            {
                _left[change[i].Name] = change[i].Left;
            }
        }
    }

    /// <summary>
    /// The members of a full guild free to leave it and with a reason of their own, each with its reasons and its distance
    /// from the guild's seat. See <see cref="Hive"/>.
    /// </summary>
    private static List<(BotMobile Bot, int Why, int Far)> Leavers(Guild guild, long now)
    {
        List<(BotMobile Bot, int Why, int Far)> found = [];
        var despised = Despised(guild);
        var hall = BotEstate.Hall(guild);
        var seat = BotSeat.Of(guild.Name);

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false, Alive: true, Squad: null } bot
                || bot.Map == null || bot.Map == Map.Internal || IsHead(bot) || Outside(bot) || Cools(bot)
                || hall is { Deleted: false } && hall.Owner == bot)
            {
                continue;
            }

            var why = 0;

            if (_unshared.TryGetValue(bot.Serial, out var since) && now - (since + UnsharedMs) >= 0)
            {
                why |= WhyUnshared;
            }

            var lives = BotResidence.Of(bot);

            if (Far(lives, guild, hall))
            {
                why |= WhyFar;
            }

            if (despised || lives != null && BotBurgh.Bars(guild, lives))
            {
                why |= WhyQuarrel;
            }

            if (why != 0)
            {
                found.Add((bot, why, seat == Point3D.Zero ? 0 : BotSeat.Gap(bot.Location, seat)));
            }
        }

        return found;
    }

    /// <summary>
    /// Whether a member living in <paramref name="lives"/> is far from its guild: another town than the guild's house (or its
    /// hall's town, without a house), or neither hall nor house within <see cref="BotSeat.Settled"/> of its town's square.
    /// </summary>
    private static bool Far(BotTowns.Town lives, Guild guild, Server.Multis.BaseHouse hall)
    {
        if (lives == null)
        {
            return false;
        }

        var standing = hall is { Deleted: false };
        var home = BotGuildHouses.TownOf(guild) ?? (standing ? BotTowns.Nearest(hall.Location) : null);

        if (home != null && !string.Equals(home.Name, lives.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var house = BotGuildHouses.Seat(guild.Name);
        var nearHall = standing && BotSeat.Gap(hall.Location, lives.Square) <= BotSeat.Settled;
        var nearHouse = house != Point3D.Zero && BotSeat.Gap(house, lives.Square) <= BotSeat.Settled;

        return !nearHall && !nearHouse;
    }

    /// <summary>Whether half or more of the other guilds hold this one at or under <see cref="BotRegard.Enmity"/>.</summary>
    private static bool Despised(Guild guild)
    {
        var others = 0;
        var enemies = 0;

        foreach (var other in _guilds.Values)
        {
            if (ReferenceEquals(other, guild) || other.Disbanded)
            {
                continue;
            }

            others++;

            if (BotRegard.Of(other.Name, guild.Name) <= BotRegard.Enmity)
            {
                enemies++;
            }
        }

        return enemies > 0 && enemies * 2 >= others;
    }

    /// <summary>
    /// A guild other than <paramref name="but"/> with room — places beyond those its own splits left — and attractiveness at or
    /// above <see cref="SplitBar"/>: somewhere to go, so no split. Or nothing.
    /// </summary>
    private static Guild WantedRoom(Guild but)
    {
        foreach (var guild in _guilds.Values)
        {
            if (ReferenceEquals(guild, but) || guild.Disbanded)
            {
                continue;
            }

            var room = Ceiling(guild) - guild.Members.Count - _left.GetValueOrDefault(guild.Name);

            if (room > 0 && Appeal(guild) >= SplitBar)
            {
                return guild;
            }
        }

        return null;
    }

    /// <summary>The split itself. See <see cref="Hive"/>.</summary>
    private static void Found(Guild parent, List<(BotMobile Bot, int Why, int Far)> leavers, (string Name, string Abbrev) named, long now)
    {
        // Where most of them live, and those living there first.
        Dictionary<string, int> towns = [];
        BotTowns.Town town = null;
        var most = 0;

        for (var i = 0; i < leavers.Count; i++)
        {
            if (BotResidence.Of(leavers[i].Bot) is not { } lives)
            {
                continue;
            }

            var n = towns.GetValueOrDefault(lives.Name) + 1;
            towns[lives.Name] = n;

            if (n > most)
            {
                most = n;
                town = lives;
            }
        }

        leavers.Sort(
            (a, b) =>
            {
                var aHere = town != null && ReferenceEquals(BotResidence.Of(a.Bot), town);
                var bHere = town != null && ReferenceEquals(BotResidence.Of(b.Bot), town);

                if (aHere != bHere)
                {
                    return aHere ? -1 : 1;
                }

                var byWhy = Reasons(b.Why).CompareTo(Reasons(a.Why));

                return byWhy != 0 ? byWhy : b.Far.CompareTo(a.Far);
            }
        );

        List<BotMobile> band = [];
        var said = new string[Least];

        for (var i = 0; i < leavers.Count && band.Count < Least; i++)
        {
            var (bot, why, _) = leavers[i];

            band.Add(bot);
            said[band.Count - 1] = $"{bot.Name} the {bot.Class?.Name} ({Say(why)})";

            if ((why & WhyUnshared) != 0)
            {
                FoundersUnshared++;
            }

            if ((why & WhyFar) != 0)
            {
                FoundersFar++;
            }

            if ((why & WhyQuarrel) != 0)
            {
                FoundersQuarrel++;
            }
        }

        town ??= BotTowns.Nearest(band[0].Location);

        // They give back their keys to the hall and the outpost of the guild they leave; the chest is the guild's and stays.
        var keys = Unkey(BotEstate.Hall(parent), band) + Unkey(BotOutpost.Of(parent.Name), band);

        Form(named, band);

        // Seated where they live: a bare seat by the town's bank, which the guild-house settler reads as the town to settle in
        // (Britain: a house once it has a hall and the price; elsewhere a free building in that town).
        if (town != null && BotSettle.Hearth(town) is var hearth && hearth != Point3D.Zero)
        {
            BotSeat.Restore(named.Name, hearth);
        }

        for (var i = 0; i < band.Count; i++)
        {
            _quit[band[i].Serial] = (now, parent.Name);
        }

        _splitAt[parent.Name] = now;
        _left[parent.Name] = _left.GetValueOrDefault(parent.Name) + band.Count;
        _young[named.Name] = (now, parent.Name);
        _lastSplit = now;
        _hasSplit = true;
        Splits++;
        _splitNames.Add($"{named.Name} ← {parent.Name}");

        logger.Warning(
            "{Guild} [{Abbrev}] has been founded by a split: {Count} left {Parent} ({Left} of {Ceiling} now, appeal {Appeal:F1}) — {Founders}, led by {Leader}; seated by {Town}, with no hall, no ground and no house, the chest left behind, {Keys} keys to the old hall and outpost given back; barred from {Parent} and any other guild for {Bar} hours, {Parent} splits again no sooner than {Cool} hours, nobody splits for {Gap} hours, and the closing clock spares {Guild} for {Young} hours; {Standing} guilds stand",
            named.Name,
            named.Abbrev,
            band.Count,
            parent.Name,
            parent.Members.Count,
            Ceiling(parent),
            Appeal(parent),
            string.Join(", ", said),
            band[0].Name,
            town?.Name ?? "nowhere known",
            keys,
            parent.Name,
            RejoinMs / 3600000,
            parent.Name,
            SplitParentMs / 3600000,
            SplitGapMs / 3600000,
            named.Name,
            YoungMs / 3600000,
            _guilds.Count
        );

        BotAlarm.Note("guild", $"{named.Name} [{named.Abbrev}] was founded by {band.Count} who left {parent.Name}, led by {band[0].Name}", band.Count, _guilds.Count, "-");
    }

    private static int Reasons(int why) => (why & WhyUnshared) + ((why & WhyFar) >> 1) + ((why & WhyQuarrel) >> 2);

    private static string Say(int why)
    {
        var say = (why & WhyUnshared) != 0 ? "no share" : null;

        if ((why & WhyFar) != 0)
        {
            say = say == null ? "far" : say + ", far";
        }

        if ((why & WhyQuarrel) != 0)
        {
            say = say == null ? "a quarrel" : say + ", a quarrel";
        }

        return say ?? "none";
    }

    /// <summary>Takes these bots off a house's co-owners and friends. Returns how many entries went.</summary>
    private static int Unkey(Server.Multis.BaseHouse house, List<BotMobile> band)
    {
        if (house is not { Deleted: false })
        {
            return 0;
        }

        var n = 0;

        for (var i = 0; i < band.Count; i++)
        {
            if (house.CoOwners?.Remove(band[i]) == true)
            {
                n++;
            }

            if (house.Friends?.Remove(band[i]) == true)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Bots inside the walk-out bar now.</summary>
    private static int InsideBar()
    {
        var now = Core.TickCount;
        var n = 0;

        foreach (var (_, quit) in _quit)
        {
            if (now - (quit.Tick + RejoinMs) < 0)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>The walk-out bars and the splits' clocks, for <see cref="BotGuildSplitStore"/>, as running time elapsed.</summary>
    internal static void SaveSplits(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(InsideBar());

        foreach (var (serial, quit) in _quit)
        {
            if (now - (quit.Tick + RejoinMs) < 0)
            {
                writer.Write(serial);
                writer.Write(Math.Max(0L, now - quit.Tick));
                writer.Write(quit.Guild ?? "");
            }
        }

        writer.WriteEncodedInt(_splitAt.Count);

        foreach (var (name, at) in _splitAt)
        {
            writer.Write(name);
            writer.Write(Math.Max(0L, now - at));
        }

        writer.WriteEncodedInt(_left.Count);

        foreach (var (name, left) in _left)
        {
            writer.Write(name);
            writer.WriteEncodedInt(left);
        }

        writer.WriteEncodedInt(_young.Count);

        foreach (var (name, young) in _young)
        {
            writer.Write(name);
            writer.Write(Math.Max(0L, now - young.Tick));
            writer.Write(young.From ?? "");
        }

        writer.Write(_hasSplit);
        writer.Write(_hasSplit ? Math.Max(0L, now - _lastSplit) : 0L);
    }

    internal static int LoadSplits(IGenericReader reader, out int young)
    {
        _quit.Clear();
        _splitAt.Clear();
        _left.Clear();
        _young.Clear();

        var now = Core.TickCount;
        var quits = reader.ReadEncodedInt();

        for (var i = 0; i < quits; i++)
        {
            var serial = reader.ReadSerial();
            var elapsed = reader.ReadLong();
            var guild = reader.ReadString();

            _quit[serial] = (now - Math.Max(0L, elapsed), guild);
        }

        var splits = reader.ReadEncodedInt();

        for (var i = 0; i < splits; i++)
        {
            var name = reader.ReadString();
            var elapsed = reader.ReadLong();

            if (!string.IsNullOrEmpty(name))
            {
                _splitAt[name] = now - Math.Max(0L, elapsed);
            }
        }

        var lefts = reader.ReadEncodedInt();

        for (var i = 0; i < lefts; i++)
        {
            var name = reader.ReadString();
            var left = reader.ReadEncodedInt();

            if (!string.IsNullOrEmpty(name) && left > 0)
            {
                _left[name] = left;
            }
        }

        var youngs = reader.ReadEncodedInt();

        for (var i = 0; i < youngs; i++)
        {
            var name = reader.ReadString();
            var elapsed = reader.ReadLong();
            var from = reader.ReadString();

            if (!string.IsNullOrEmpty(name))
            {
                _young[name] = (now - Math.Max(0L, elapsed), from);
            }
        }

        _hasSplit = reader.ReadBool();

        var since = reader.ReadLong();

        _lastSplit = _hasSplit ? now - Math.Max(0L, since) : 0;

        young = _young.Count;

        return _quit.Count;
    }

    /// <summary>
    /// What this guild is working towards, in one phrase.
    ///
    /// <para>
    /// <b>Read off the estate ladder rather than stored anywhere.</b> A guild has no plan and needs none: the
    /// order it does things in is decided by what it has — no hall, then a hall with nothing in it, then a
    /// counter, then ground — and each step is a question somebody already answers. A stored goal would be a
    /// second copy of that going stale, which is this project's most familiar defect.
    /// </para>
    /// </summary>
    public static string Aim(Guild guild)
    {
        if (guild == null)
        {
            return "nothing";
        }

        var hall = BotEstate.Hall(guild);
        var fund = BotEstate.Fund(guild);

        if (hall is not { Deleted: false })
        {
            return $"a hall — {fund} of {BotEstate.Price}gp";
        }

        if (BotEstate.Merchants(hall) < BotEstate.MostMerchants)
        {
            return "a merchant for its counter";
        }

        var held = BotClaim.Holds(guild.Name);

        if (held < BotClaim.Free)
        {
            return $"ground — {held} of {BotClaim.Free} free squares taken";
        }

        return $"more ground — {held} squares held, {BotClaim.Price}gp the next";
    }

    /// <summary>
    /// What the whole guild is engaged in at this moment, or nothing in particular.
    ///
    /// The order is by how much of the guild it takes: a war, then a claim being held, then a hall being
    /// carried across the island, then the one-member offices. Anything below that is ordinary work and is
    /// nobody's business but the bot's.
    /// </summary>
    public static string Task(Guild guild)
    {
        if (guild == null)
        {
            return "nothing";
        }

        var bid = BotClaim.Making(guild);

        if (bid != null)
        {
            return $"holding {bid.Middle.X},{bid.Middle.Y} — {BotClaim.Left(bid) / 1000}s left, {bid.Peak} gathered";
        }

        if (BotExile.Owed(guild) is { Deleted: false } winner)
        {
            return $"moving its hall out of the yard at {winner.X},{winner.Y}";
        }

        if (BotOffice.Busy(BotSupplier.Office, guild))
        {
            return "stocking its counter";
        }

        if (BotOffice.Busy(BotSteward.Office, guild))
        {
            return "raising its hall";
        }

        return guild.Enemies is { Count: > 0 } ? "at war" : "its own work";
    }

    /// <summary>Members a leader has taken in. See <see cref="BotRoster"/>.</summary>
    public static long Hired { get; private set; }

    /// <summary>Members a leader has put out.</summary>
    public static long Expelled { get; private set; }

    /// <summary>
    /// A leader takes somebody in.
    ///
    /// <para>
    /// <b>Patrick's order of 11.09.2026, and it is the door the shard was missing rather than a new
    /// power.</b> <see cref="Enrol"/> is the only way into a guild and it is called at birth and nowhere
    /// else, so a guild that lost members had no way to replace them: on the evening this was written all
    /// five guilds on the shard stood at one member — the maker, alone, because a maker never leaves — with
    /// seventy bots outside them and nothing in the code able to put the two facts together.
    /// </para>
    ///
    /// <para>
    /// <b>Every refusal is its own answer, and none of them is a bucket called "other".</b> Whether a place
    /// went unfilled because the guild was full, because the bot's class stands outside the bands, or
    /// because it walked out of this very guild an hour ago are three different facts about an evening.
    /// </para>
    /// </summary>
    public static bool Recruit(Guild guild, BotMobile bot, string why)
    {
        if (!Running || guild == null || guild.Disbanded || bot is not { Deleted: false, Alive: true })
        {
            return false;
        }

        if (bot.Guild != null)
        {
            HeldElsewhere++;

            return false;
        }

        if (Outside(bot))
        {
            Excused++;

            return false;
        }

        if (guild.Members.Count >= Ceiling(guild) && !Widen(guild, $"to take {bot.Name} the {bot.Class?.Name} on: {why ?? "no reason given"}"))
        {
            Crowded++;

            return false;
        }

        // The narrow bar, not the wide one: see Spurned. A leader may take in somebody another guild lost;
        // it may not take back the member it lost itself.
        if (Spurned(bot, guild))
        {
            Cooling++;

            return false;
        }

        guild.AddMember(bot);
        Show(bot);
        Joined(bot);
        Hired++;

        logger.Information(
            "{Guild} has taken {Bot} the {Class} on, {Count} of {Most} now — {Why}",
            guild.Name,
            bot.Name,
            bot.Class?.Name,
            guild.Members.Count,
            Ceiling(guild),
            why ?? "no reason given"
        );

        return true;
    }

    /// <summary>
    /// A leader puts somebody out.
    ///
    /// <para>
    /// <b>It does not earn the bot the two-day bar, and that is the whole difference between being expelled
    /// and leaving.</b> <see cref="RejoinMs"/> is the price of a decision a bot made for itself; a bot
    /// shown the door made no decision and is free to be taken in by anybody who wants it on the next beat.
    /// Reading the two as the same event would make one leader's judgement a sentence passed on the rest of
    /// the island.
    /// </para>
    ///
    /// <para>
    /// <b>The head cannot be put out either</b>, for the reason <see cref="Review"/> gives: expelling
    /// whoever leads the guild would be the guild dissolving by a side door rather than a member leaving.
    /// </para>
    /// </summary>
    public static bool Expel(Guild guild, BotMobile bot, string why)
    {
        if (!Running || guild == null || bot is not { Deleted: false })
        {
            return false;
        }

        if (!ReferenceEquals(bot.Guild, guild))
        {
            return false;
        }

        if (IsHead(bot))
        {
            Spared++;

            return false;
        }

        guild.RemoveMember(bot);
        bot.DisplayGuildTitle = false;
        _joined.Remove(bot.Serial);
        Expelled++;

        logger.Information(
            "{Guild} has put {Bot} the {Class} out, {Count} left — {Why}",
            guild.Name,
            bot.Name,
            bot.Class?.Name,
            guild.Members.Count,
            why ?? "no reason given"
        );

        return true;
    }

    /// <summary>Members a herald talked out of another guild into its own. A subset of <see cref="Crossed"/>. See <see cref="Poach"/>.</summary>
    public static long Poached { get; private set; }

    /// <summary>
    /// A member discontented in its guild crosses to another at that guild's herald's invitation (07.10.2026, officers20; see
    /// <c>BotHeraldStand</c>). The same crossing <see cref="Review"/> makes, by the same rules: never the head, never across a
    /// war, never inside the walk-out bar or back into a guild it walked out of, only into room; the bar is earned exactly as by
    /// walking out, so the crossing is a decision made once.
    /// </summary>
    public static bool Poach(Guild from, Guild to, BotMobile bot, string why)
    {
        if (!Running || !Leaving || !Defecting || from == null || to == null || to.Disbanded || ReferenceEquals(from, to)
            || bot is not { Deleted: false, Alive: true } || !ReferenceEquals(bot.Guild, from) || IsHead(bot))
        {
            return false;
        }

        if (Cools(bot) || Spurned(bot, to))
        {
            Cooling++;

            return false;
        }

        if (from.Enemies != null && from.Enemies.Contains(to))
        {
            return false;
        }

        if (to.Members.Count >= Ceiling(to))
        {
            Crowded++;

            return false;
        }

        var now = Core.TickCount;
        var since = _joined.TryGetValue(bot.Serial, out var joined) ? joined : now;

        // AddMember takes the bot out of the guild it is in, so there is no moment outside both.
        to.AddMember(bot);
        Show(bot);

        _quit[bot.Serial] = (now, from.Name);
        Joined(bot);
        Walked++;
        Crossed++;
        Poached++;

        logger.Information(
            "{Bot} the {Class} has left {From} for {To} after {Minutes} minutes in it — {Why}; {To} is {Count} of {Most} now",
            bot.Name,
            bot.Class?.Name,
            from.Name,
            to.Name,
            (now - since) / 60000,
            why ?? "no reason given",
            to.Name,
            to.Members.Count,
            Ceiling(to)
        );

        return true;
    }

    /// <summary>Offers of a place made to a bot that already had one.</summary>
    public static long HeldElsewhere { get; private set; }

    /// <summary>Offers of a place made by a guild that was already at <see cref="Most"/>.</summary>
    public static long Crowded { get; private set; }

    /// <summary>Times a leader tried to put its own maker out.</summary>
    public static long Spared { get; private set; }

    /// <summary>Notes the moment a bot joined, which is what <see cref="Patience"/> is measured from.</summary>
    private static void Joined(Mobile bot)
    {
        if (bot is { Deleted: false })
        {
            _joined[bot.Serial] = Core.TickCount;
        }
    }

    /// <summary>
    /// Whether these two are of the same guild.
    ///
    /// Asked of the engine's own record rather than of a table here: a bot's guild can be changed by
    /// anything, and two answers to one question is how this shard's oldest defects were built.
    /// </summary>
    public static bool Same(Mobile a, Mobile b) =>
        Running && a?.Guild != null && ReferenceEquals(a.Guild, b?.Guild);

    /// <summary>
    /// What a piece of work about <paramref name="other"/> is worth to <paramref name="bot"/>, given whose
    /// company they keep. One for a stranger, <see cref="Kinship"/> for one of your own.
    /// </summary>
    public static double Worth(Mobile bot, Mobile other) => Same(bot, other) ? Kinship : 1.0;

    /// <summary>
    /// The best maker in this guild — the one whose trade is to keep the rest of it equipped.
    ///
    /// <para>
    /// Patrick's order of 09.09.2026: <i>the crafter in a guild lives to equip its guildmates as well as it
    /// can</i>. This is who that is. Chosen by skill rather than by seniority, so a guild with two smiths
    /// sends orders to the better one.
    /// </para>
    /// </summary>
    public static BotMobile Maker(Guild guild)
    {
        if (guild?.Members == null)
        {
            return null;
        }

        BotMobile best = null;
        var bestSkill = -1.0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } member || !IsMaker(member))
            {
                continue;
            }

            var skill = member.Class?.MainSkill == null
                ? 0.0
                : member.Skills[member.Class.MainSkill.Value]?.Base ?? 0.0;

            if (skill > bestSkill)
            {
                best = member;
                bestSkill = skill;
            }
        }

        return best;
    }

    /// <summary>
    /// What a member keeps back when its guild stands a cost for somebody else. See <see cref="Stand"/>.
    ///
    /// <para>
    /// <b>The estate's number, not a second one.</b> This was its own 300 while the hall levy's was 300,
    /// and the two agreed by coincidence. On the night of 13.09.2026 the estate's keep-back went to 100
    /// and the hirer, which asks <c>BotEstate.Fund</c> whether the guild can pay, said yes 4,369 times in
    /// forty-three minutes while this levy, keeping 300 back, said no to every one of them — "could not
    /// raise 1252gp for a contract", a hundred times a minute, at no cost to anybody but the auction. Two
    /// thresholds on one shelf; now there is one.
    /// </para>
    /// </summary>
    public static int Keep => BotEstate.Keep;

    /// <summary>Coin the guilds have put into their own members' hands, and how often.</summary>
    public static long Stood { get; private set; }

    public static long Standings { get; private set; }

    /// <summary>Times a guild was asked to stand a cost and could not.</summary>
    public static long Cannot { get; private set; }

    /// <summary>
    /// The share of what a mate holds above the keep that one call on it may take. A quarter (02.10.2026, build 389; Patrick's
    /// option 1): richest first down to the keep made the keep a ceiling, see <c>BotEstate.Keep</c>.
    /// </summary>
    public static double StandShare { get; set; } = 0.25;

    /// <summary>Gold the cap left with its holders when a call asked for more. For the Guilds line.</summary>
    public static long CappedGold { get; private set; }

    /// <summary>
    /// The guild pays what one of its members cannot, so the member can buy what it needs.
    ///
    /// <para>
    /// <b>Patrick's order of 09.09.2026 — "the crafter in a guild lives to equip its guildmates" — carried
    /// out through the market rather than by hand.</b> The machinery for equipping a bot already exists and
    /// works end to end: <c>BotArmourer</c> asks what piece a bot most needs, raises an order for it, a
    /// crafter fills it, and <c>Rearm</c> puts it on. The only thing that ever stopped it was money — half
    /// this population has never held more than the four hundred it was born with, and armour is the first
    /// thing a bot buys that it does not need <em>today</em>.
    /// </para>
    ///
    /// <para>
    /// So a guild stands the difference. The coin goes into the member's account and the member buys its own
    /// armour, which means the order goes on the same board every crafter reads — and the crafter most
    /// likely to fill it is the guild's own, because a guildmate's order is worth <see cref="Kinship"/> more
    /// to it. The guild's money ends in the guild's maker's purse, having become a breastplate on the way.
    /// That is the whole of it, and not a line of new economy was needed for it.
    /// </para>
    ///
    /// <para>
    /// Taken richest first, each keeping <see cref="Keep"/> back, exactly as the hall levy does — and it
    /// never takes from the bot it is paying for, which would be a bot lending itself money.
    /// </para>
    /// </summary>
    public static bool Stand(BotMobile member, int need)
    {
        if (!Running || member is not { Deleted: false } || need <= 0 || member.Guild is not Guild guild)
        {
            return false;
        }

        var mates = new List<BotMobile>();

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } mate && mate != member)
            {
                mates.Add(mate);
            }
        }

        mates.Sort((a, b) => BotYield.Wealth(b).CompareTo(BotYield.Wealth(a)));

        var got = 0;

        for (var i = 0; i < mates.Count && got < need; i++)
        {
            var mate = mates[i];
            var pack = mate.Backpack;

            if (pack == null)
            {
                continue;
            }

            // <b>A share of what the mate holds above the keep, a call, never the whole of it (02.10.2026, build 389; Patrick's
            // option 1).</b> Richest first down to the keep made the keep a ceiling: see BotEstate.Keep. What the cap withholds
            // is counted, so a guild that cannot stand for a member is a number and not a silence.
            var above = BotYield.Spendable(mate) - Keep;

            if (above <= 0)
            {
                continue;
            }

            var allowed = Math.Max(0, (int)(above * Math.Clamp(StandShare, 0.0, 1.0)));
            var asked = Math.Min(above, need - got);

            if (allowed < asked)
            {
                CappedGold += asked - allowed;
            }

            var spare = Math.Min(allowed, need - got);

            if (spare <= 0)
            {
                continue;
            }

            var carried = pack.GetAmount(typeof(Gold));
            var fromPack = Math.Min(carried, spare);
            var fromBank = spare - fromPack;

            if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
            {
                continue;
            }

            if (fromBank > 0 && !Banker.Withdraw(mate, fromBank))
            {
                if (fromPack > 0)
                {
                    pack.DropItem(new Gold(fromPack));
                }

                continue;
            }

            got += spare;

            // The guild's decision, out of whatever this mate was doing. The member's side is booked by whoever
            // asked, because only the asker knows whether the money is spent on its own errand. See BotYield.Aside.
            BotYield.Aside(mate, spare);
        }

        if (got < need)
        {
            // Hand back whatever was taken. A guild that half-pays for a hauberk has bought nothing and is
            // poorer, which is the one outcome worth more than the armour.
            if (got > 0)
            {
                Banker.Deposit(member, got);

                // Handed over for nothing bought, so no errand of the member's earned it.
                BotYield.Aside(member, -got);
            }

            Cannot++;

            return false;
        }

        Banker.Deposit(member, got);
        Stood += got;
        Standings++;

        return true;
    }

    /// <summary>One line for the shard's own summary.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "the population keeps no guilds";
        }

        if (_guilds.Count == 0)
        {
            return "no guilds have been raised yet";
        }

        var say = Server.Text.ValueStringBuilder.Create(320);

        try
        {
            var makerless = 0;
            var held = 0;

            foreach (var (name, guild) in _guilds)
            {
                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                say.Append(name);
                say.Append(" [");
                say.Append(guild.Abbreviation);
                say.Append("] ");
                say.Append((guild.Members?.Count ?? 0).ToString());

                held += guild.Members?.Count ?? 0;

                // Asked of the field behind Disbanded rather than of Leader, whose getter holds an election.
                if (guild.Disbanded)
                {
                    say.Append(" DISBANDED");

                    continue;
                }

                var head = Head(guild);

                if (head != null)
                {
                    say.Append(" under ");
                    say.Append(head.Name);
                    say.Append(" the ");
                    say.Append(head.Class?.Name ?? "?");
                }

                // Its attractiveness, and how long it has been unwanted if it is (06.10.2026). See Appeal and Wane.
                say.Append(" appeal ");
                say.Append(Appeal(guild).ToString("F1"));

                if (_low.ContainsKey(name))
                {
                    say.Append(" unwanted ");
                    say.Append(LowMinutes(guild).ToString());
                    say.Append("m");
                }

                if (Maker(guild) == null)
                {
                    makerless++;
                }
            }

            var loose = 0;
            var roster = BotPopulation.Bots;

            for (var i = 0; i < roster.Count; i++)
            {
                if (roster[i] is { Deleted: false, Guild: null } bot && !Outside(bot))
                {
                    loose++;
                }
            }

            // <b>Said whether or not anything has been stood yet.</b> The walk-outs and the roster used to sit
            // inside the branch that prints only once a guild has paid for somebody, so on a poor shard the
            // number of members lost was a number nobody printed.
            var stood = (Standings == 0
                    ? "; nothing has been stood for anybody yet"
                    : $"; {Stood}gp stood for members {Standings} times, {Cannot} times a guild could not, {CappedGold}gp left with their holders by the {StandShare:P0} cap on a call above the {Keep}gp keep")
                + $"; {Walked} members left a guild that had nothing to show ({Crossed} of them for a guild that had, {Nowhere} looks found no guild worth leaving for), {Cooling} refused a guild for having lately left one"
                + $"; {BotRoster.Describe()}"
                + $"; {BotCharter.Describe()}";

            var founding = !Founding
                ? "founding is OFF"
                : $"{Founded} founded and {Gathered} joined since the shard came up, {Stones} given the stone the engine wants; "
                  + $"looks that placed nobody: {TooFew} found fewer than {Least - 1} others without a guild to found with, "
                  + $"{Nameless} found every name taken, {AtMost} found {MostGuilds} guilds standing, {Barred48} were inside the bar for walking out, {Unplaced} found every guild full"
                  + (_foundedNames.Count == 0 ? "" : $" (founded: {string.Join(", ", _foundedNames)})");

            // Patrick's order of 06.10.2026: guilds nobody wants may close. See Wane.
            var closing = !Closing
                ? "; closing is OFF"
                : $"; closing: a guild of fewer than {Least} with attractiveness (worth + members/{Least}) under {LowAppeal:F1} and nobody joining for {CloseAfterMs / 60000} minutes closes — "
                  + $"{Closed} closed{(_closedNames.Count == 0 ? "" : $" ({string.Join(", ", _closedNames)})")}, {ClosedGold}gp of their chests shared among {Released} members let go, "
                  + $"looks that held a due guild open: {HeldAtWar} at war, {HeldClaiming} laying claim, {HeldInCompany} with a member in a company, {HeldNoRoom} with too few free places elsewhere; "
                  + $"{RestingNames()} names resting";

            // Patrick's word of 06.10.2026, ~10:00: five leaving a full guild found a new one. See Hive.
            var splits = !Splitting
                ? "; splits are OFF"
                : $"; splits: five of a full guild with a reason found under one of the fifty while no guild with room reads appeal {SplitBar:F1} or more — "
                  + $"{Splits} founded by a split{(_splitNames.Count == 0 ? "" : $" ({string.Join(", ", _splitNames)})")}, "
                  + $"founders' reasons: {FoundersUnshared} no share (under the {BotEstate.Keep}gp keep {UnsharedMs / 60000}m while the chest held {RichChest}gp+), {FoundersFar} far from the guild's house or hall, {FoundersQuarrel} a quarrel; "
                  + $"looks that split nobody: {SplitFew} no full guild had {Least} free to go with a reason, {SplitAtWar} at war, {SplitParentCool} inside a guild's {SplitParentMs / 3600000}h after its split, "
                  + $"{SplitAtMost} at {MostGuilds} guilds, {SplitRoom} a guild with room at the bar, {SplitGap} inside {SplitGapMs / 3600000}h of the last split, {SplitNameless} no bare name; "
                  + $"{_unshared.Count} members under the keep in a rich guild now; {HeldYoung} closing looks spared a split guild under {YoungMs / 3600000}h; {InsideBar()} bots inside the walk-out bar (kept across restarts)";

            var rule = $"{Least} to found and {Most} at most, anybody may found, a full guild widening by {WidenBy} for {WidenPrice}gp up to {MostWidenings} times ({Widenings} bought, {Unwidened} wanted and not raised)";
            var short_ = makerless == 0 ? "" : $", {makerless} of them with nobody to make anything";

            // Who elected whom, and the plots against the heads (07.10.2026). See BotElection.
            return $"{_guilds.Count} guilds holding {held} bots with {loose} outside them ({rule}{short_}), a guildmate worth ×{Kinship:F2}: {say.ToString()}; {founding}{closing}{splits}{stood}; {BotElection.Describe()}";
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>
    /// A world reload is a different population.
    ///
    /// The guilds themselves are left alone: they are world objects, they will be found by name and revived
    /// when the new population is dealt out, and deleting them here would throw away the one thing about
    /// them worth keeping — that a guild on this island is older than any bot in it.
    /// </summary>
    public static void Forget()
    {
        _guilds.Clear();
        Enrolled = 0;
        Excused = 0;
        Walked = 0;
        Cooling = 0;
        _quit.Clear();
        _joined.Clear();
        _reviewed = 0;
        _turn = 0;
        Unplaced = 0;
        Stood = 0;
        Standings = 0;
        Cannot = 0;
        CappedGold = 0;
        Hired = 0;
        Expelled = 0;
        HeldElsewhere = 0;
        Crowded = 0;
        Spared = 0;
        Crossed = 0;
        Poached = 0;
        Nowhere = 0;
        Founded = 0;
        Gathered = 0;
        TooFew = 0;
        Nameless = 0;
        Barred48 = 0;
        Stones = 0;
        _cursor = 0;
        _dealt = false;
        _widened.Clear();
        Widenings = 0;
        Unwidened = 0;

        // A new world: no guild is unwanted yet and every name is free again (06.10.2026).
        _low.Clear();
        _seen.Clear();
        _closed.Clear();
        _foundedNames.Clear();
        _closedNames.Clear();
        AtMost = 0;
        Closed = 0;
        ClosedGold = 0;
        Released = 0;
        HeldAtWar = 0;
        HeldClaiming = 0;
        HeldInCompany = 0;
        HeldNoRoom = 0;

        // A new world: nobody has left anybody, and no guild came out of another (06.10.2026).
        _unshared.Clear();
        _splitAt.Clear();
        _left.Clear();
        _young.Clear();
        _splitNames.Clear();
        _hasSplit = false;
        _lastSplit = 0;
        Splits = 0;
        FoundersUnshared = 0;
        FoundersFar = 0;
        FoundersQuarrel = 0;
        SplitFew = 0;
        SplitAtWar = 0;
        SplitParentCool = 0;
        SplitAtMost = 0;
        SplitRoom = 0;
        SplitGap = 0;
        SplitNameless = 0;
        HeldYoung = 0;

        BotRoster.Forget();
    }
}
