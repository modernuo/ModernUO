using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a thief a turn in the crowd by a bank: the engine's own Stealing on a bot's pack.
///
/// <para>
/// <b>Patrick, 07.10.2026: "thieves stealing from packs in the crowd by the bank (Stealing is in the engine)".</b> Nothing about
/// the theft is the bots': the thief joins the engine's thieves' guild, puts its weapon away (both hands must be free), uses the
/// skill and targets the mark, and the engine picks a random thing off the top of the mark's pack, weighs it against the skill,
/// takes as much of a stack as the skill allows, and decides whether anybody noticed — a thief at nought is noticed almost
/// always, at a hundred one time in three. Noticed is a criminal act: grey, and in a guarded town with a shopkeeper or a banker
/// within eight tiles the engine sends its guards at once. That is the risk Patrick named, and it is the engine's.
/// </para>
///
/// <para>
/// <b>Bounded twice.</b> One thief per <see cref="BotCrime.ThiefPerBots"/> bots in the world, and <see cref="PerHour"/> tries
/// across the whole island: a thief caught by a bank usually dies to a guard, and deaths per bot-hour are one of the night's
/// key numbers. Each thief waits <see cref="EveryMs"/> between tries of its own.
/// </para>
/// </summary>
public sealed class BotCutpurse : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCutpurse));

    /// <summary>Whether thieves work the crowds at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Tries at a pack across the island in an hour of uptime.</summary>
    public static int PerHour { get; set; } = 6;

    /// <summary>How long one thief waits between its turns in a crowd.</summary>
    public static int EveryMs { get; set; } = 900000;

    /// <summary>How far round the bank a thief looks for a mark.</summary>
    public static int CrowdTiles { get; set; } = 8;

    /// <summary>
    /// How long a thief waits by the bank for somebody worth robbing before it gives up. A minute (three until 07.10.2026, loops18):
    /// the turn is offered only at a bank where a crowd stood when it was offered (<see cref="LeastCrowd"/>), so a long wait is a
    /// crowd that has gone, not one still coming.
    /// </summary>
    public static int CrowdWaitMs { get; set; } = 60000;

    /// <summary>
    /// <b>The marks standing by a bank for a turn there to be offered (07.10.2026, loops18).</b> On build 418's first 35 minutes 681
    /// turns were offered and one was tried: every thief went to the nearest town's bank, which for Kirsten and Orlando was Skara
    /// Brae's, where nobody came ("nobody worth robbing came by the bank in Skara Brae"), and the other turns dropped at the end of
    /// their dwell, outbid by a peddle, while they waited. The banks are counted once every <see cref="CrowdLookMs"/> for the whole
    /// island, and a thief is sent to the nearest within <see cref="CrowdReach"/> that has this many in its crowd. Nought: any bank.
    /// </summary>
    public static int LeastCrowd { get; set; } = 2;

    /// <summary>How often the crowds by the banks are counted, for the whole island. Thirty seconds, the market's own cadence.</summary>
    public static int CrowdLookMs { get; set; } = 30000;

    /// <summary>How far a thief goes for a crowd, in tiles from where it stands. Its own leash still holds (<c>BotPopulation.Within</c>).</summary>
    public static int CrowdReach { get; set; } = 400;

    /// <summary>
    /// Whether a turn in a crowd is held against ordinary offers once taken (<see cref="BotDeed.Steadfast"/>, 07.10.2026, loops18): the
    /// turn is a few minutes, and on build 418 eleven of twelve drops were "outbid by peddle/unload/deliver" or "jumped" at the end
    /// of the dwell, with the thief still waiting for its mark. What will not wait still gets through, and a company's call is not
    /// made to a thief at its turn (<c>BotSquads.Thieving</c>). Off: as before.
    /// </summary>
    public static bool Holds { get; set; } = true;

    /// <summary>Turns not offered because no bank within reach had a crowd (<see cref="LeastCrowd"/>). Looks, not thieves.</summary>
    public static long Crowdless { get; private set; }

    /// <summary>Times the crowds by the banks were counted.</summary>
    public static long Counted { get; private set; }

    private static readonly List<(Point3D Bank, string Town, int Marks)> _crowds = [];

    private static Map _crowdMap;

    private static long _crowdTick;

    /// <summary>Whether a bot by a bank is somebody a thief could take from: in the world, seen, something in its pack, not one of the band.</summary>
    internal static bool Mark(BotMobile other) =>
        other is { Deleted: false, Alive: true, Hidden: false } && other.Backpack is { } pack && pack.Items.Count > 0
        && !BotUnderworld.Member(other) && !BotCrime.IsThief(other) && !BotOutlaw.Jailed(other);

    /// <summary>Counts the marks by every town's bank, at most once every <see cref="CrowdLookMs"/>: one query a town, island-wide.</summary>
    private static void CountCrowds(Map map)
    {
        var now = Core.TickCount;

        if (_crowdMap == map && now - _crowdTick < CrowdLookMs)
        {
            return;
        }

        _crowdMap = map;
        _crowdTick = now;
        _crowds.Clear();
        Counted++;

        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            var bank = BotShopkeep.BankNear(map, towns[i].Square);

            if (bank == Point3D.Zero)
            {
                continue;
            }

            var marks = 0;

            foreach (var other in map.GetMobilesInRange<BotMobile>(bank, CrowdTiles))
            {
                if (Mark(other))
                {
                    marks++;
                }
            }

            _crowds.Add((bank, towns[i].Name, marks));
        }
    }

    /// <summary>The bank nearest the thief, within reach and its leash, with a crowd of at least <see cref="LeastCrowd"/>; Zero if none.</summary>
    private static Point3D Crowd(BotMobile thief, Map map, out string town)
    {
        town = null;

        CountCrowds(map);

        var best = Point3D.Zero;
        var nearest = int.MaxValue;

        for (var i = 0; i < _crowds.Count; i++)
        {
            var (bank, name, marks) = _crowds[i];

            if (marks < LeastCrowd)
            {
                continue;
            }

            var away = Math.Max(Math.Abs(bank.X - thief.X), Math.Abs(bank.Y - thief.Y));

            if (away > CrowdReach || away >= nearest || !BotPopulation.Within(map, bank, thief))
            {
                continue;
            }

            nearest = away;
            best = bank;
            town = name;
        }

        return best;
    }

    /// <summary>How long a thief may close on one mark before it picks another.</summary>
    public static int CloseMs { get; set; } = 45000;

    /// <summary>Tries at the engine in one turn: a stack too heavy or a bag is refused before any roll, and the thief tries again.</summary>
    public static int Tries { get; set; } = 3;

    /// <summary>The chance a mark notices a theft nobody saw, after the thief has gone: it knows it was robbed, not by whom.</summary>
    public static double NoticeChance { get; set; } = 0.5;

    /// <summary>How near a bystander must stand to the mark to see a thief caught and add its word to the mark's.</summary>
    public static int WitnessTiles { get; set; } = 8;

    /// <summary>What a turn in the crowd is reckoned at before the ledger has measured one.</summary>
    public static double Prior { get; set; } = 40.0;

    /// <summary>How far a thief walks away from the mark afterwards.</summary>
    public static int SlipTiles { get; set; } = 14;

    public static long Offered { get; private set; }

    public static long Attempts { get; private set; }

    public static long Lifted { get; private set; }

    public static long LiftedGold { get; private set; }

    public static long LiftedThings { get; private set; }

    public static long LiftedWorth { get; private set; }

    public static long Fumbled { get; private set; }

    public static long Refused { get; private set; }

    public static long Noticed { get; private set; }

    public static long Witnessed { get; private set; }

    public static long Missed { get; private set; }

    public static long NoCrowd { get; private set; }

    public static long Quota { get; private set; }

    private static readonly Queue<long> _tries = new();

    private static readonly Dictionary<string, long> _last = new(StringComparer.Ordinal);

    public string Name => "Cutpurse";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>Whether the island's hourly tries have room for one more.</summary>
    public static bool Room()
    {
        var now = Core.TickCount;

        while (_tries.Count > 0 && now - _tries.Peek() >= 3600000)
        {
            _tries.Dequeue();
        }

        return _tries.Count < Math.Max(0, PerHour);
    }

    /// <summary>One try spent, of the island's and of the thief's.</summary>
    public static void Spent(Mobile thief)
    {
        var now = Core.TickCount;

        _tries.Enqueue(now);

        if (thief?.Name != null)
        {
            _last[thief.Name] = now;
        }

        Attempts++;
    }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!Running || !BotCrime.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body || !BotCrime.IsThief(body))
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Tired || body.Squad != null || body.Criminal || BotOutlaw.Outlaw(body)
            || BotOutlaw.Jailed(body) || BotCrime.Posted(body) || BotDungeon.Under(body.Location))
        {
            return null;
        }

        var now = Core.TickCount;

        if (_last.TryGetValue(body.Name, out var last) && now - last < EveryMs)
        {
            return null;
        }

        if (!Room())
        {
            Quota++;

            return null;
        }

        // To a bank with a crowd, not the nearest town's (07.10.2026, loops18). See LeastCrowd.
        var bank = Crowd(body, map, out var town);

        if (bank == Point3D.Zero)
        {
            Crowdless++;

            return null;
        }

        Offered++;

        return new BotPickpocket(map, bank, town ?? BotCrime.TownAt(bank));
    }

    public static void Lift(int gold, int things, int worth)
    {
        Lifted++;
        LiftedGold += gold;
        LiftedThings += things;
        LiftedWorth += worth;
    }

    public static void Fumble() => Fumbled++;

    public static void Refuse() => Refused++;

    public static void Notice() => Noticed++;

    public static void Witness() => Witnessed++;

    public static void Miss() => Missed++;

    public static void Empty() => NoCrowd++;

    public static string Describe() =>
        !Running
            ? "the cutpurses: off"
            : $"the cutpurses: {Offered} turns in a crowd offered, {Attempts} tries at a pack ({PerHour} an hour at most, {Quota} offers held for it), {Lifted} lifted {LiftedGold}gp and {LiftedThings} things worth {LiftedWorth}gp, {Fumbled} came away with nothing, {Refused} the engine refused before any roll, {Noticed} noticed by the mark and {Witnessed} by a bystander, {Missed} thefts the mark found later, {NoCrowd} found nobody worth robbing; {Crowdless} looks found no bank within {CrowdReach} tiles with {LeastCrowd} in its crowd ({Counted} counts of the banks), {BotSquads.ThievesSpared} thieves at their turn left to it by a company's call or a levy";

    public static void Forget()
    {
        _tries.Clear();
        _last.Clear();
        Offered = 0;
        Attempts = 0;
        Lifted = 0;
        LiftedGold = 0;
        LiftedThings = 0;
        LiftedWorth = 0;
        Fumbled = 0;
        Refused = 0;
        Noticed = 0;
        Witnessed = 0;
        Missed = 0;
        NoCrowd = 0;
        Quota = 0;
        Crowdless = 0;
        Counted = 0;
        _crowds.Clear();
        _crowdMap = null;
    }
}

/// <summary>A thief's turn in the crowd by a bank: find a mark, close on it, try its pack, and slip away. See <see cref="BotCutpurse"/>.</summary>
public sealed class BotPickpocket : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPickpocket));

    public const string Trade = "steal";

    private readonly Map _map;

    private readonly Point3D _bank;

    private readonly string _town;

    private BotMobile _mark;

    private long _arrived;

    private long _closing;

    private long _looked;

    private int _tries;

    private bool _done;

    private bool _caught;

    private Point3D _away;

    private long _leaving;

    private string _outcome;

    private bool _lifted;

    private int _made;

    private readonly List<(Type Kind, int Amount)> _goods = [];

    public BotPickpocket(Map map, Point3D bank, string town)
    {
        _map = map;
        _bank = bank;
        _town = town;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _bank;

    public override double Expects => BotCutpurse.Prior;

    public override double Minutes => 4.0;

    public override SkillName? Trains => SkillName.Stealing;

    /// <summary>What the stolen goods will fetch at the fence: takings in their own right, as an ingot is to a smith.</summary>
    public override int Made => _made;

    /// <summary>Once a mark is chosen the thief is about it: no ordinary offer takes it off the mark or the getaway.</summary>
    public override bool Committed => _mark != null || _done;

    /// <summary>Held against ordinary offers for its own few minutes, the walk to the crowd and the wait included. See <see cref="BotCutpurse.Holds"/>.</summary>
    public override bool Steadfast => BotCutpurse.Holds;

    /// <summary>Taken up again after something that would not wait: the wait for a mark starts again, and so do the clocks of a close or a getaway.</summary>
    public override void Resumed(IBotWilful bot)
    {
        var now = Core.TickCount;

        _arrived = 0;
        _looked = 0;

        if (_mark != null)
        {
            _closing = now;
        }

        if (_done)
        {
            _leaving = now;
        }
    }

    public override string Stage =>
        _done ? $"slipping away from the bank in {_town}"
        : _mark != null ? $"closing on {_mark.Name} by the bank in {_town}"
        : $"watching the crowd by the bank in {_town}";

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        var now = Core.TickCount;

        if (_done)
        {
            // The getaway: a few steps away from the mark, then over.
            if (body.InRange(_away, 2) || now - _leaving >= 20000)
            {
                return _lifted ? BotDoing.Done(_outcome) : BotDoing.Failed(_outcome);
            }

            return BotDoing.Walk(_map, _away, BotArrival.Within(2), _caught ? "running from the guards" : "slipping into the crowd");
        }

        if (body.Criminal || BotOutlaw.Outlaw(body))
        {
            return BotDoing.Failed("it is grey already and the guards would know it");
        }

        if (_mark == null)
        {
            if (!body.InRange(_bank, 6))
            {
                return BotDoing.Walk(_map, _bank, BotArrival.Within(4), $"to the crowd by the bank in {_town}");
            }

            if (_arrived == 0)
            {
                _arrived = now;
            }

            if (now - _looked >= 5000)
            {
                _looked = now;
                _mark = Choose(body);

                if (_mark != null)
                {
                    _closing = now;
                }
            }

            if (_mark == null)
            {
                if (now - _arrived >= BotCutpurse.CrowdWaitMs)
                {
                    BotCutpurse.Empty();

                    return BotDoing.Failed($"nobody worth robbing came by the bank in {_town}");
                }

                return BotDoing.Work($"watching the crowd by the bank in {_town}");
            }
        }

        if (_mark is not { Deleted: false, Alive: true } || _mark.Map != _map || !_mark.InRange(_bank, BotCutpurse.CrowdTiles * 2))
        {
            _mark = null;

            return BotDoing.Work("lost its mark in the crowd");
        }

        if (!body.InRange(_mark.Location, 1))
        {
            if (now - _closing >= BotCutpurse.CloseMs)
            {
                _mark = null;

                return BotDoing.Work("its mark kept moving");
            }

            return BotDoing.Walk(_map, _mark, BotArrival.Beside, $"sidling up to {_mark.Name}");
        }

        if (!BotShadow.Ready(body))
        {
            return BotDoing.Work($"waiting beside {_mark.Name} for its hands to be free");
        }

        if (!BotCutpurse.Room())
        {
            return BotDoing.Failed("the town is watchful tonight");
        }

        return Try(body, _mark, now);
    }

    /// <summary>The mark: a bot of nobody's band near the bank with something in its pack, the fattest purse first.</summary>
    private BotMobile Choose(BotMobile thief)
    {
        BotMobile best = null;
        var most = -1;

        foreach (var other in _map.GetMobilesInRange<BotMobile>(_bank, BotCutpurse.CrowdTiles))
        {
            if (other == thief || !BotCutpurse.Mark(other) || BotGuilds.Same(thief, other) || !thief.CanBeHarmful(other, false))
            {
                continue;
            }

            var purse = other.Backpack.GetAmount(typeof(Gold));

            if (purse > most)
            {
                most = purse;
                best = other;
            }
        }

        return best;
    }

    /// <summary>One try at the mark's pack through the engine's own skill, and what came of it.</summary>
    private BotDoing Try(BotMobile thief, BotMobile mark, long now)
    {
        var before = BotCrime.Census(thief);
        var grey = thief.Criminal;
        var pack = thief.Backpack;

        // Both hands must be free: the engine's rule. What is put away is put back the moment the try is over.
        List<Item> stowed = [];

        if (pack != null)
        {
            Stow(thief, pack, Layer.OneHanded, stowed);
            Stow(thief, pack, Layer.TwoHanded, stowed);
        }

        thief.NpcGuild = NpcGuild.ThievesGuild;
        thief.Combatant = null;
        thief.Warmode = false;

        BotCutpurse.Spent(thief);
        _tries++;

        if (global::Server.Skills.UseSkill(thief, SkillName.Stealing) && thief.Target != null)
        {
            thief.Target.Invoke(thief, mark);
        }

        for (var i = 0; i < stowed.Count; i++)
        {
            if (stowed[i] is { Deleted: false } held && held.RootParent == thief && held.Parent != thief)
            {
                thief.EquipItem(held);
            }
        }

        if (stowed.Count > 0)
        {
            thief.Rearm();
        }

        var after = BotCrime.Census(thief);
        var gold = BotCrime.Gained(before, after, _goods);
        var caught = !grey && thief.Criminal;
        var worth = gold;
        var things = 0;

        for (var i = 0; i < _goods.Count; i++)
        {
            worth += _goods[i].Amount * BotCrime.Worth(_goods[i].Kind);
            things += _goods[i].Amount;
            BotCrime.Heat(thief, _goods[i].Kind, _goods[i].Amount, _town, mark.Name);
        }

        if (gold <= 0 && things <= 0 && !caught)
        {
            // Refused before any roll (a bag, a stack too heavy, something bound) or fumbled unseen: try again while there are tries.
            BotCutpurse.Refuse();

            if (_tries < Math.Max(1, BotCutpurse.Tries))
            {
                return BotDoing.Work($"its hand came out of {mark.Name}'s pack empty");
            }

            BotCutpurse.Fumble();

            return Leave(thief, mark, now, false, $"came away from {mark.Name}'s pack with nothing");
        }

        var sheet = BotCrime.Of(thief.Name, true);

        if (gold > 0 || things > 0)
        {
            BotCutpurse.Lift(gold, things, worth);
            sheet.Thefts++;
            _lifted = true;
            _made += (int)((worth - gold) * BotFencer.FenceShare);
        }
        else
        {
            BotCutpurse.Fumble();
        }

        BotCrime.Crime(thief);

        var what = things > 0 ? $"{(gold > 0 ? $"{gold}gp and " : "")}{things} {Things(_goods)}" : $"{gold}gp";

        logger.Information(
            "{Thief} the {Class} {Did} {What} from {Mark}'s pack by the bank in {Town} at Stealing {Skill:F1}{Seen}",
            thief.Name,
            thief.Class?.Name,
            gold > 0 || things > 0 ? "lifted" : "tried for",
            gold > 0 || things > 0 ? what : "anything",
            mark.Name,
            _town,
            thief.Skills.Stealing.Value,
            caught ? ", and was seen" : ", unseen"
        );

        if (caught)
        {
            BotCutpurse.Notice();
            BotVoice.Say(mark, "cry", $"Thief! {thief.Name} has a hand in my pack!", true);
            BotCrime.Report(thief, mark, _town, "picking pockets by the bank", worth);

            // A bystander who saw it adds its word: two reports and the town posts the thief.
            foreach (var other in _map.GetMobilesInRange<BotMobile>(mark.Location, BotCutpurse.WitnessTiles))
            {
                if (other == thief || other == mark || other is not { Deleted: false, Alive: true } || BotUnderworld.Member(other)
                    || BotCrime.IsThief(other) || !other.CanSee(thief))
                {
                    continue;
                }

                BotCutpurse.Witness();
                BotVoice.Say(other, "local", $"I saw it too! {thief.Name}, the thief!", true);
                BotCrime.Report(thief, other, _town, "picking pockets by the bank", 0);

                break;
            }

            BotEvents.Post("crime", thief, $"caught with a hand in {mark.Name}'s pack by the bank in {_town}");
        }
        else if (gold > 0 || things > 0)
        {
            BotEvents.Post("crime", thief, $"lifted {what} from {mark.Name} by the bank in {_town}, unseen");

            if (Utility.RandomDouble() < BotCutpurse.NoticeChance)
            {
                BotCutpurse.Miss();
                BotVoice.Say(mark, "local", "My purse! Somebody has been in my pack!", true);
            }
        }

        return Leave(thief, mark, now, caught, caught ? $"caught with a hand in {mark.Name}'s pack" : $"lifted {what} from {mark.Name}");
    }

    private BotDoing Leave(BotMobile thief, BotMobile mark, long now, bool caught, string outcome)
    {
        _done = true;
        _caught = caught;
        _outcome = outcome;
        _leaving = now;

        var dx = thief.X - mark.X;
        var dy = thief.Y - mark.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomBool() ? 1 : -1;
            dy = Utility.RandomBool() ? 1 : -1;
        }

        var len = Math.Max(1.0, Math.Sqrt(dx * dx + dy * dy));
        var x = thief.X + (int)Math.Round(dx / len * BotCutpurse.SlipTiles);
        var y = thief.Y + (int)Math.Round(dy / len * BotCutpurse.SlipTiles);

        _away = new Point3D(x, y, _map.GetAverageZ(x, y));

        return BotDoing.Walk(_map, _away, BotArrival.Within(2), caught ? "running from the guards" : "slipping into the crowd");
    }

    private static void Stow(Mobile thief, Container pack, Layer layer, List<Item> stowed)
    {
        if (thief.FindItemOnLayer(layer) is { } held && pack.TryDropItem(thief, held, false))
        {
            stowed.Add(held);
        }
    }

    private static string Things(List<(Type Kind, int Amount)> goods) =>
        goods.Count == 0 ? "things" : goods.Count == 1 ? goods[0].Kind.Name : $"{goods[0].Kind.Name} and more";
}
