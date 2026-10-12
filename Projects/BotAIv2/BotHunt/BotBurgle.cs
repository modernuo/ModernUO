using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// A bandit breaks into a guild's hall or a bot's own house: creeps up hidden, picks the lock of the chest with a
/// lockpick, and carries off a share of what was put by — and is wanted if anybody sees it.
///
/// <para>
/// <b>Patrick, 26.09.2026, beside the order that made The Shadow every guild's enemy:</b> "since The Shadow is the enemy of
/// every guild, bandits may rob guild houses — by stealth and lockpicking." <b>And on the night of 29.09.2026, his second
/// addition:</b> "houses, like guild halls, can be robbed by bandits, but that needs the lockpicking skill, lockpicks
/// themselves and stealth. If a bandit is seen in a house or a hall, it is declared wanted." So it is the band's trade,
/// and a bandit out of every guild for its crimes (<c>BotUnderworld.Outlawed</c>): chosen instead of a person when the
/// thought of robbery comes (<see cref="Share"/> of the times), against the hall whose guild has the most put by, or the
/// house whose chest holds the most, and has not been broken into within <see cref="AgainMs"/>.
/// </para>
///
/// <para>
/// <b>The chest is the treasury, and the lock is a skill roll with a pick in hand.</b> A guild's money is a number kept by
/// guild name (<see cref="BotChest"/>), and the wooden chest in its hall is its furniture; a bot's house keeps real things
/// in a real chest (<see cref="BotAbode.Chest"/>). The engine's own lockpick refuses a locked-down container to anybody
/// not a co-owner, so the pick is the engine's skill check (<c>Mobile.CheckSkill</c>, Lockpicking, which also trains it)
/// against a lock that is harder in a bigger house (<see cref="Lock"/>), one try every <see cref="PickMs"/>,
/// <see cref="MostPicks"/> tries in all, and a <see cref="Lockpick"/> in the pack that a failed try snaps now and then
/// (<see cref="BreakChance"/>). A pick that opens a hall's chest takes <see cref="TakeShare"/> of the treasury, at most
/// <see cref="MostTake"/>; one that opens a house's chest takes up to <see cref="MostThings"/> things out of it.
/// </para>
///
/// <para>
/// <b>Seen is wanted.</b> Hidden on the way in when it can hide at all (<c>BotShadow.Hide</c>, within <see cref="HideAt"/>),
/// moving quietly when its Stealth lets it (<c>BotShadow.Quiet</c>); a bot with neither walks in openly. A burglar that is
/// not hidden at a try, or is revealed by a noisy one (<see cref="NoiseChance"/>), with a bot outside the band in sight
/// within <see cref="Sight"/>, is posted wanted on that witness's word (<c>BotOutlaw.Want</c>) — the law's own machinery
/// then hunts it — and a guild fighter that can see one of the band sets on it as before (<c>BotFeuder</c>).
/// </para>
/// </summary>
public sealed class BotBurgle : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBurgle));

    public const string Trade = "burgle";

    /// <summary>Whether bandits break into halls and houses at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The share of a bandit's thoughts of robbery that are of a hall or a house, when there is one worth it.</summary>
    public static double Share { get; set; } = 0.3;

    /// <summary>The least a guild must have put by for its hall to be worth breaking into.</summary>
    public static int Least { get; set; } = 200;

    /// <summary>The fewest things a house's chest must hold to be worth it.</summary>
    public static int LeastThings { get; set; } = 3;

    /// <summary>What a pick that opens a hall's chest takes of the treasury, and the most it takes.</summary>
    public static double TakeShare { get; set; } = 0.25;

    public static int MostTake { get; set; } = 2000;

    /// <summary>The most things a pick that opens a house's chest carries off.</summary>
    public static int MostThings { get; set; } = 3;

    /// <summary>How long the same hall or house is left alone after a break-in, or a try at one. An hour.</summary>
    public static int AgainMs { get; set; } = 3600000;

    /// <summary>How far a bandit goes for a hall or a house.</summary>
    public static int Reach { get; set; } = 800;

    public static int PickMs { get; set; } = 3000;

    public static int MostPicks { get; set; } = 6;

    /// <summary>The chance a failed try is heard.</summary>
    public static double NoiseChance { get; set; } = 0.25;

    /// <summary>The chance a failed try snaps the pick.</summary>
    public static double BreakChance { get; set; } = 0.35;

    /// <summary>How near the hall a burglar hides.</summary>
    public static int HideAt { get; set; } = 14;

    /// <summary>How far a witness may be to see a burglar at the chest.</summary>
    public static int Sight { get; set; } = 12;

    /// <summary>Lockpicks a bot is given when it joins the band. See <c>BotUnderworld</c>.</summary>
    public static int PicksIssued { get; set; } = 5;

    public static int CapMs { get; set; } = 600000;

    public static double Prior { get; set; } = 300.0;

    public static long Taken { get; private set; }

    public static long Opened { get; private set; }

    public static long Stuck { get; private set; }

    public static long Heard { get; private set; }

    public static long Gold { get; private set; }

    /// <summary>Things carried out of houses, picks snapped, burglars posted wanted, and thoughts of a break-in with no pick in the pack.</summary>
    public static long Things { get; private set; }

    public static long Snapped { get; private set; }

    public static long Wanted { get; private set; }

    public static long NoPick { get; private set; }

    private static readonly Dictionary<BaseHouse, long> _tried = [];

    private readonly BaseHouse _house;

    /// <summary>The guild whose hall this is, or null for a bot's own house.</summary>
    private readonly string _guild;

    /// <summary>Whose house, when it is a house.</summary>
    private readonly string _owner;

    private readonly long _began;

    private long _pickTick;

    private int _picks;

    private bool _counted;

    private bool _posted;

    public BotBurgle(BaseHouse house, string guild) : this(house, guild, null)
    {
    }

    public BotBurgle(BaseHouse house, string guild, string owner)
    {
        _house = house;
        _guild = guild;
        _owner = owner;
        _began = Core.TickCount;
        _pickTick = _began - PickMs;
    }

    /// <summary>Whether this bot has a pick to work with.</summary>
    public static bool HasPick(Mobile body) => (body?.Backpack?.GetAmount(typeof(Lockpick)) ?? 0) > 0;

    /// <summary>
    /// The hall with the most put by, or the house whose chest holds the most, within reach and not tried lately; the
    /// hall's guild or the house's owner in <paramref name="whose"/>. Null with no pick in the pack: a burglar without one
    /// has nothing to try the lock with.
    /// </summary>
    public static BaseHouse Target(Mobile body, Map map, out string whose)
    {
        whose = null;

        if (!Running || body == null || map == null)
        {
            return null;
        }

        if (!HasPick(body))
        {
            NoPick++;

            return null;
        }

        BaseHouse best = null;
        var most = Least - 1;
        var now = Core.TickCount;

        foreach (var (name, hall) in BotEstate.Held)
        {
            if (hall is not { Deleted: false } || hall.Map != map || !body.InRange(hall.BanLocation, Reach))
            {
                continue;
            }

            if (_tried.TryGetValue(hall, out var tried) && now - tried < AgainMs)
            {
                continue;
            }

            var gold = BotChest.Holds(name);

            if (gold > most)
            {
                most = gold;
                best = hall;
                whose = name;
            }
        }

        // A house's chest is reckoned by the things in it, a thing worth a hundred against the hall's gold, so a full
        // chest beats a thin treasury and a thin chest never beats a fat one.
        foreach (var house in BotAbode.Houses)
        {
            if (house is not { Deleted: false } || house.Map != map || !body.InRange(house.BanLocation, Reach))
            {
                continue;
            }

            if (_tried.TryGetValue(house, out var tried) && now - tried < AgainMs)
            {
                continue;
            }

            var owner = OwnerOf(house);

            if (owner == null || string.Equals(owner, body.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var things = BotAbode.Chest(house)?.Items.Count ?? 0;

            if (things < LeastThings)
            {
                continue;
            }

            var worth = things * 100;

            if (worth > most)
            {
                most = worth;
                best = house;
                whose = owner;
            }
        }

        return best;
    }

    /// <summary>Whose house this is, from the sign; null for a hall or an unsigned house.</summary>
    public static string OwnerOf(BaseHouse house)
    {
        var sign = house?.Sign?.Name;

        return !string.IsNullOrEmpty(sign) && sign.EndsWith(BotAbode.Suffix, StringComparison.Ordinal)
            ? sign[..^BotAbode.Suffix.Length]
            : null;
    }

    /// <summary>Whether this house is one of the population's own rather than a guild's hall.</summary>
    public static bool IsAbode(BaseHouse house) => OwnerOf(house) != null;

    private static (double Low, double High) Lock(BaseHouse hall) =>
        BotHallKind.Of(hall).Size switch
        {
            1 => (20.0, 70.0),
            2 => (30.0, 80.0),
            3 => (40.0, 90.0),
            _ => (50.0, 100.0)
        };

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Braves => true;

    public override bool Unpaid => true;

    public override Map Map => _house?.Map;

    public override Point3D Where => _house?.BanLocation ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => CapMs / 60000.0;

    public override SkillName? Trains => SkillName.Lockpicking;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    private string Place => _owner != null ? $"{_owner}'s house" : $"the hall of {_guild}";

    public override string Stage => _picks == 0 ? $"creeping up on {Place}" : $"picking the lock of the chest in {Place} ({_picks} tries)";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _house is not { Deleted: false })
        {
            return BotDoing.Failed("the house is gone");
        }

        if (!_counted)
        {
            _counted = true;
            Taken++;
            _tried[_house] = Core.TickCount;
        }

        var now = Core.TickCount;

        if (now - _began >= CapMs)
        {
            Stuck++;

            return BotDoing.Failed("the break-in took too long");
        }

        var chest = BotAbode.Chest(_house);

        if (chest == null)
        {
            return BotDoing.Failed($"there is no chest in {Place}");
        }

        var at = chest.GetWorldLocation();

        if (!body.InRange(at, 1))
        {
            if (!body.Hidden && body.InRange(_house.BanLocation, HideAt) && BotShadow.Running)
            {
                BotShadow.Hide(body);
            }

            if (body.Hidden && BotShadow.MayStealth(body))
            {
                BotShadow.Quiet(body);
            }

            return BotDoing.Walk(_house.Map, at, BotArrival.Within(1), $"into {Place}");
        }

        if (now - _pickTick < PickMs)
        {
            return BotDoing.Work($"picking the lock of the chest in {Place}");
        }

        if (!HasPick(body))
        {
            Stuck++;

            return BotDoing.Failed($"no lockpick left for the chest in {Place}");
        }

        _pickTick = now;
        _picks++;

        // In plain sight at the chest: whoever sees it posts it wanted. See Seen.
        if (!body.Hidden)
        {
            Seen(body);
        }

        var (low, high) = Lock(_house);

        if (body.CheckSkill(SkillName.Lockpicking, low, high))
        {
            Opened++;

            return _owner != null ? Rob(body, chest) : Plunder(body);
        }

        if (Utility.RandomDouble() < BreakChance && body.Backpack?.ConsumeTotal(typeof(Lockpick), 1) == true)
        {
            Snapped++;
        }

        if (Utility.RandomDouble() < NoiseChance && body.Hidden)
        {
            Heard++;
            body.RevealingAction();
            Seen(body);
        }

        if (_picks >= MostPicks)
        {
            Stuck++;

            return BotDoing.Failed($"the lock of the chest in {Place} would not give (Lockpicking {body.Skills.Lockpicking.Base:F1})");
        }

        return BotDoing.Work($"picking the lock of the chest in {Place}");
    }

    /// <summary>A hall's chest opened: a share of the guild's treasury.</summary>
    private BotDoing Plunder(Mobile body)
    {
        var take = (int)Math.Min(MostTake, BotChest.Holds(_guild) * TakeShare);
        var got = take <= 0 ? 0 : BotChest.Steal(_guild, take);

        if (got <= 0)
        {
            return BotDoing.Done($"picked the lock of {_guild}'s chest and found it empty");
        }

        body.AddToBackpack(new Gold(got));
        Gold += got;
        BotUnderworld.Burgled(body);

        logger.Information(
            "{Name} broke into the hall of {Guild} and carried {Gold}gp out of its chest (Lockpicking {Skill:F1}, {Tries} tries{Hidden})",
            body.Name,
            _guild,
            got,
            body.Skills.Lockpicking.Base,
            _picks,
            body.Hidden ? ", unseen" : ", in plain sight"
        );

        return BotDoing.Done($"carried {got}gp out of the hall of {_guild}");
    }

    /// <summary>A house's chest opened: up to <see cref="MostThings"/> things out of it, the dearest first.</summary>
    private BotDoing Rob(Mobile body, Container chest)
    {
        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("nowhere to put anything");
        }

        List<Item> loot = [];

        for (var i = 0; i < chest.Items.Count; i++)
        {
            if (chest.Items[i] is { Deleted: false } item)
            {
                loot.Add(item);
            }
        }

        loot.Sort(static (a, b) => Worth(b).CompareTo(Worth(a)));

        var carried = 0;

        for (var i = 0; i < loot.Count && carried < MostThings; i++)
        {
            var item = loot[i];

            if (item is Gold coins)
            {
                Gold += coins.Amount;
            }

            if (pack.TryDropItem(body, item, false))
            {
                carried++;
                Things++;
            }
        }

        if (carried == 0)
        {
            return BotDoing.Done($"picked the lock of the chest in {_owner}'s house and could carry nothing out of it");
        }

        BotUnderworld.Burgled(body);

        logger.Information(
            "{Name} broke into {Owner}'s house and carried {Things} things out of its chest (Lockpicking {Skill:F1}, {Tries} tries{Hidden})",
            body.Name,
            _owner,
            carried,
            body.Skills.Lockpicking.Base,
            _picks,
            body.Hidden ? ", unseen" : ", in plain sight"
        );

        return BotDoing.Done($"carried {carried} things out of {_owner}'s house");
    }

    private static int Worth(Item item) => item is Gold g ? g.Amount : item is BaseWeapon or BaseArmor ? 150 : 40;

    /// <summary>A burglar in plain sight at the chest, with a bot outside the band within <see cref="Sight"/> that can see it, is posted wanted on that bot's word. Once a break-in.</summary>
    private void Seen(Mobile body)
    {
        if (_posted || body is not BotMobile burglar || body.Map == null)
        {
            return;
        }

        foreach (var other in body.Map.GetMobilesInRange<BotMobile>(body.Location, Sight))
        {
            if (other == body || other is not { Deleted: false, Alive: true } || BotUnderworld.Member(other) || !other.InLOS(body))
            {
                continue;
            }

            _posted = true;
            Wanted++;
            BotOutlaw.Want(burglar, other.Name, $"seen breaking into {Place}");

            logger.Information("{Name} was seen by {Witness} breaking into {Place} and is wanted", body.Name, other.Name, Place);

            return;
        }
    }

    public static string Describe() =>
        Taken == 0
            ? "no hall or house has been broken into"
            : $"{Taken} break-ins tried: {Opened} chests opened for {Gold}gp and {Things} things, {Stuck} locks that would not give or break-ins that ran out of time, {Heard} burglars heard and revealed, {Wanted} posted wanted by a witness, {Snapped} picks snapped, {NoPick} thoughts of a break-in with no pick in the pack";

    public static void Forget()
    {
        _tried.Clear();
        Taken = 0;
        Opened = 0;
        Stuck = 0;
        Heard = 0;
        Gold = 0;
        Things = 0;
        Snapped = 0;
        Wanted = 0;
        NoPick = 0;
    }
}
