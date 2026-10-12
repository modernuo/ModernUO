using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The guild's store at its hall: the members' consumables held in common, a kit issued from it as a debt, and the debts
/// repaid from the member's next spoils.
///
/// <para>
/// <b>Patrick's order (07.10.2026): "the guild store holds the members' goods" and "a kit issued is a debt".</b> Until this the
/// only goods a guild had were on its merchant's shelf (pack14, box16). The store is small on purpose: one ledger per guild of
/// the consumables a fight takes — bandages, draughts, reagents, arrows and bolts (<see cref="BotProvision.Consumable"/>) — kept
/// at the guild's seat (its town house's heart, else its hall's door), at most <see cref="MostUnits"/> of a kind and
/// <see cref="MostBottles"/> of a draught. Goods come in three ways and only three: the porter puts a member's capped surplus in
/// when it stands within <see cref="TakeReach"/> of the store (<see cref="Take"/>, from <c>BotUnload.Store</c>), the quartermaster
/// takes surplus from members standing at the store during its shift, and the guild's wants are bought by the quartermaster and
/// paid back by the chest (<see cref="BotQuartermaster"/>). Nothing is made: a unit in is a lot taken out of a pack and deleted, a
/// unit out is an item made of the same kind in a pack, and the counts on both sides are the same counts.
/// </para>
///
/// <para>
/// <b>A kit is a debt, never a gift.</b> A member short of its kit and either under <see cref="PoorBelow"/> or a newcomer draws
/// what it lacks from the store (<see cref="Issue"/>); what it drew is worth the market's price for each unit, and that is owed to
/// the guild's chest. The debt is collected like the moneylender's lien (<c>BotLoans</c>): every minute <see cref="LienShare"/> of
/// every rise in the debtor's wealth goes to the chest until it is paid. A debt not paid in <see cref="DefaultHours"/> of the
/// debtor's play is written off and the debtor draws no kit again; a debtor gone from the island or a guild closed writes it off
/// too. All of it counted.
/// </para>
///
/// <para>
/// <b>The members' goods, so a gift counts as one.</b> What a member puts in is a gift to the guild at the market's price, and is
/// written to its record as dues given (<see cref="BotElection.Gave"/>), which is what an election weighs gifts by.
/// </para>
/// </summary>
public static class BotGuildStore
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuildStore));

    /// <summary>Whether the guilds keep stores at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The most a store holds of one stacking kind.</summary>
    public static int MostUnits { get; set; } = 120;

    /// <summary>The most a store holds of one draught (they do not stack, and a store of seventy is the courier's hoard again).</summary>
    public static int MostBottles { get; set; } = 30;

    /// <summary>How near the store a porter must stand for its capped surplus to go in instead of the bank box.</summary>
    public static int TakeReach { get; set; } = 40;

    /// <summary>Whether the porter puts surplus into the store at all. See <see cref="Take"/>.</summary>
    public static bool PorterGives { get; set; } = true;

    /// <summary>Whether a gift counts as dues given in the guild's elections.</summary>
    public static bool GiftsCount { get; set; } = true;

    /// <summary>What a member's spendable must be under to be issued a kit as poor. The moneylender's bar and a half.</summary>
    public static int PoorBelow { get; set; } = 150;

    /// <summary>The most a member may owe the store and still be issued another kit.</summary>
    public static int MostOwed { get; set; } = 0;

    /// <summary>The share of every rise in a debtor's wealth that goes to the chest. The moneylender's half.</summary>
    public static double LienShare { get; set; } = 0.5;

    /// <summary>Hours of the debtor's own play before an unpaid kit is written off.</summary>
    public static double DefaultHours { get; set; } = 6.0;

    /// <summary>The guess at a unit's price when the market has learned none.</summary>
    public static int PriceGuess { get; set; } = 5;

    /// <summary>How often the lien is taken.</summary>
    public static int CollectMs { get; set; } = 60000;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long GiftsIn { get; private set; }

    public static long GiftUnits { get; private set; }

    public static long GiftGold { get; private set; }

    public static long PorterUnits { get; private set; }

    public static long HandedUnits { get; private set; }

    public static long BoughtUnits { get; private set; }

    public static long FullRefused { get; private set; }

    public static long Kits { get; private set; }

    public static long KitUnits { get; private set; }

    public static long KitGold { get; private set; }

    public static long KitPackRefused { get; private set; }

    public static long KitEmpty { get; private set; }

    public static long LienTakes { get; private set; }

    public static long LienGold { get; private set; }

    public static long LienHeld { get; private set; }

    public static long PaidOff { get; private set; }

    public static long WrittenOff { get; private set; }

    public static long WrittenGold { get; private set; }

    public static long Shelved { get; private set; }

    public static long ShelvedGold { get; private set; }

    // ---- state -------------------------------------------------------------------------------------------------------------

    private static readonly Dictionary<string, Dictionary<Type, int>> _stores = new(StringComparer.Ordinal);

    /// <summary>When each guild last issued each kind, for the surplus rule.</summary>
    private static readonly Dictionary<(string, Type), long> _lastIssued = [];

    public sealed class Debt
    {
        public string Debtor;

        public string Guild;

        public int Owed;

        public int Repaid;

        public int Mark;

        public double Played;

        public int Left => Math.Max(0, Owed - Repaid);
    }

    private static readonly Dictionary<string, Debt> _debts = new(StringComparer.Ordinal);

    private static readonly HashSet<string> _marked = new(StringComparer.Ordinal);

    private static long _lastCollect;

    private static readonly List<string> _ended = [];

    // ---- where -------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Where the store stands: the heart of the guild's town house first, the door of its hall second; nothing without either.
    /// The same order a member is born and walks home by (<see cref="BotSeat.Home"/>).
    /// </summary>
    public static bool At(Guild guild, out Map map, out Point3D at)
    {
        map = BotPopulation.Home;
        at = Point3D.Zero;

        if (!Running || guild?.Name == null || map == null)
        {
            return false;
        }

        var housed = BotGuildHouses.Seat(guild.Name);

        if (housed != Point3D.Zero)
        {
            at = housed;

            return true;
        }

        if (BotEstate.Hall(guild) is { Deleted: false } hall && hall.Map == map)
        {
            at = hall.BanLocation;

            return at != Point3D.Zero;
        }

        return false;
    }

    /// <summary>Whether this kind may be kept in a store.</summary>
    public static bool Storable(Type kind) => kind != null && BotProvision.Consumable(kind);

    private static bool Bottle(Type kind) => kind != null && typeof(BasePotion).IsAssignableFrom(kind);

    /// <summary>The most a store holds of this kind.</summary>
    public static int Most(Type kind) => Bottle(kind) ? MostBottles : MostUnits;

    /// <summary>What this guild's store holds of this kind.</summary>
    public static int Held(string guild, Type kind) =>
        guild != null && kind != null && _stores.TryGetValue(guild, out var store) && store.TryGetValue(kind, out var n) ? n : 0;

    /// <summary>What this guild's store holds, by kind. Never null; read only.</summary>
    public static IReadOnlyDictionary<Type, int> Of(string guild) =>
        guild != null && _stores.TryGetValue(guild, out var store) ? store : _empty;

    private static readonly Dictionary<Type, int> _empty = [];

    private static Dictionary<Type, int> Store(string guild)
    {
        if (!_stores.TryGetValue(guild, out var store))
        {
            _stores[guild] = store = [];
        }

        return store;
    }

    /// <summary>The store's goods at the market's price.</summary>
    public static int Worth(string guild)
    {
        var worth = 0L;

        foreach (var (kind, n) in Of(guild))
        {
            worth += (long)n * Price(kind);
        }

        return (int)Math.Min(int.MaxValue, worth);
    }

    public static int Price(Type kind) => Math.Max(1, BotMarket.Worth(kind, PriceGuess));

    /// <summary>Room left for this kind in this guild's store.</summary>
    public static int Room(string guild, Type kind) => Math.Max(0, Most(kind) - Held(guild, kind));

    /// <summary>The stock in a few words, for the speech: "80 bandages, 12 lesser heal potions".</summary>
    public static string Stock(string guild, int most = 4)
    {
        var store = Of(guild);

        if (store.Count == 0)
        {
            return "nothing yet";
        }

        var say = new StringBuilder();
        var shown = 0;

        foreach (var (kind, n) in store)
        {
            if (n <= 0)
            {
                continue;
            }

            if (shown++ >= most)
            {
                say.Append(", and more");

                break;
            }

            say.Append(shown > 1 ? ", " : "").Append(n).Append(' ').Append(BotVoice.Words(kind.Name));
        }

        return shown == 0 ? "nothing yet" : say.ToString();
    }

    // ---- in ----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The porter's door into the store (from <c>BotUnload.Store</c>, before the bank box): a whole lot of a member's capped
    /// surplus goes into its guild's store when the member stands within <see cref="TakeReach"/> of it and the store has room
    /// for all of it. True when the lot went in and is gone; false leaves it untouched for the box.
    /// </summary>
    public static bool Take(Mobile body, Item lot)
    {
        if (!Running || !PorterGives || body is not BotMobile bot || bot.Guild is not Guild guild || lot is not { Deleted: false })
        {
            return false;
        }

        if (!BotOfficers.Keeps(guild) || !At(guild, out var map, out var at) || bot.Map != map || !bot.InRange(at, TakeReach))
        {
            return false;
        }

        var units = Math.Max(1, lot.Amount);

        if (!In(bot, guild, lot, units, "the porter"))
        {
            return false;
        }

        PorterUnits += units;

        return true;
    }

    /// <summary>A member's surplus handed to the quartermaster at the store. Whole lots only; true when it went in.</summary>
    internal static bool Hand(BotMobile member, Guild guild, Item lot)
    {
        var units = Math.Max(1, lot?.Amount ?? 0);

        if (!In(member, guild, lot, units, "the quartermaster"))
        {
            return false;
        }

        HandedUnits += units;

        return true;
    }

    private static bool In(BotMobile member, Guild guild, Item lot, int units, string via)
    {
        var kind = lot?.GetType();

        if (!Storable(kind) || lot.Deleted || BotBinding.IsBound(lot, member.Bond))
        {
            return false;
        }

        if (Room(guild.Name, kind) < units)
        {
            FullRefused++;

            return false;
        }

        var store = Store(guild.Name);

        store[kind] = store.GetValueOrDefault(kind) + units;
        lot.Delete();

        var gold = units * Price(kind);

        GiftsIn++;
        GiftUnits += units;
        GiftGold += gold;

        if (GiftsCount)
        {
            BotElection.Gave(member, gold);
        }

        logger.Information(
            "{Bot} put {Units} {Kind} into {Guild}'s store through {Via}: a gift worth {Gold}gp; the store holds {Held} of them",
            member.Name,
            units,
            kind.Name,
            guild.Name,
            via,
            gold,
            store[kind]
        );

        return true;
    }

    /// <summary>
    /// Goods the guild bought: the quartermaster's want delivered, put into the store from its pack. Reimbursed by the caller
    /// at the want's price. Returns the units that went in.
    /// </summary>
    internal static int Bought(BotMobile qm, Guild guild, Type kind, int most)
    {
        var pack = qm?.Backpack;

        if (pack == null || most <= 0 || !Storable(kind))
        {
            return 0;
        }

        var room = Math.Min(most, Room(guild.Name, kind));
        var have = pack.GetAmount(kind);
        var units = Math.Min(room, have);

        if (units <= 0)
        {
            if (room <= 0)
            {
                FullRefused++;
            }

            return 0;
        }

        if (!pack.ConsumeTotal(kind, units))
        {
            return 0;
        }

        var store = Store(guild.Name);

        store[kind] = store.GetValueOrDefault(kind) + units;
        BoughtUnits += units;

        return units;
    }

    // ---- out ---------------------------------------------------------------------------------------------------------------

    /// <summary>Whether this member may be issued a kit at all: short of it, poor or new, owing nothing, never a defaulter.</summary>
    public static string Unfit(BotMobile member)
    {
        if (member?.Name == null)
        {
            return "nobody";
        }

        if (_marked.Contains(member.Name))
        {
            return "a defaulter";
        }

        if (_debts.TryGetValue(member.Name, out var debt) && debt.Left > MostOwed)
        {
            return "still owing the store";
        }

        if (BotYield.Spendable(member) >= PoorBelow && !BotOfficers.Newcomer(member))
        {
            return "able to buy its own";
        }

        if (!BotProvision.Short(member))
        {
            return "not short of its kit";
        }

        return null;
    }

    /// <summary>Whether the store holds anything of what this member lacks.</summary>
    public static bool Lacks(BotMobile member, string guild)
    {
        var pack = member?.Backpack;

        if (pack == null)
        {
            return false;
        }

        foreach (var (kind, n) in Of(guild))
        {
            // The doors' floor, not the shopper's: the store never issues a healer's attack herbs as a debt (08.10.2026, cast30
            // review A2/B F3; see BotProvision.Armed).
            if (n > 0 && BotProvision.DoorFloor(member, kind) > pack.GetAmount(kind))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The quartermaster hands a member what it lacks of its kit from the store, and the member owes the guild's chest what it
    /// was worth. Returns the units handed over; nought when the store had nothing it lacked or the pack took nothing.
    /// </summary>
    public static int Issue(BotMobile qm, BotMobile member, out string what, out int owed)
    {
        what = null;
        owed = 0;

        var pack = member?.Backpack;

        if (!Running || qm?.Guild is not Guild guild || pack == null || !_stores.TryGetValue(guild.Name, out var store))
        {
            return 0;
        }

        var given = 0;
        var say = new StringBuilder();
        List<(Type Kind, int Units)> plan = [];

        foreach (var (kind, n) in store)
        {
            // The doors' floor: see Lacks (08.10.2026, cast30).
            var lack = BotProvision.DoorFloor(member, kind) - pack.GetAmount(kind);

            if (n > 0 && lack > 0)
            {
                plan.Add((kind, Math.Min(n, lack)));
            }
        }

        for (var i = 0; i < plan.Count; i++)
        {
            var (kind, units) = plan[i];
            var put = Give(member, pack, kind, units);

            if (put <= 0)
            {
                KitPackRefused++;

                continue;
            }

            store[kind] -= put;
            given += put;
            owed += put * Price(kind);
            _lastIssued[(guild.Name, kind)] = Core.TickCount;
            say.Append(say.Length == 0 ? "" : ", ").Append(put).Append(' ').Append(BotVoice.Words(kind.Name));
        }

        if (given <= 0)
        {
            KitEmpty++;

            return 0;
        }

        what = say.ToString();
        Kits++;
        KitUnits += given;
        KitGold += owed;

        if (!_debts.TryGetValue(member.Name, out var debt) || debt.Guild != guild.Name)
        {
            debt = _debts[member.Name] = new Debt { Debtor = member.Name, Guild = guild.Name };
        }

        debt.Owed += owed;
        debt.Mark = BotYield.Wealth(member);

        logger.Information(
            "{Qm} the quartermaster issued {Bot} the {Class} a kit from {Guild}'s store: {What}, worth {Owed}gp, owed to the chest out of {Share:P0} of its next takings ({Why}; {Spendable}gp spendable)",
            qm.Name,
            member.Name,
            member.Class?.Name,
            guild.Name,
            what,
            owed,
            LienShare,
            BotOfficers.Newcomer(member) ? "a newcomer" : "under the poverty line",
            BotYield.Spendable(member)
        );

        BotEvents.Post("officers", member, $"drew a kit from {guild.Name}'s store: {what}, {owed}gp owed");

        return given;
    }

    /// <summary>Makes <paramref name="units"/> of a kind in a pack, one item at a time for a kind that does not stack. Returns what went in.</summary>
    private static int Give(Mobile member, Container pack, Type kind, int units)
    {
        var put = 0;

        while (put < units)
        {
            var item = BotBinding.Make(kind, 1);

            if (item == null)
            {
                break;
            }

            var lot = item.Stackable ? units - put : 1;

            if (lot > 1)
            {
                item.Amount = lot;
            }

            if (!pack.TryDropItem(member, item, false))
            {
                item.Delete();

                break;
            }

            put += lot;
        }

        return put;
    }

    // ---- debts -------------------------------------------------------------------------------------------------------------

    /// <summary>What this bot owes its guild's store, nought for nothing.</summary>
    public static int Owes(string name) => name != null && _debts.TryGetValue(name, out var debt) ? debt.Left : 0;

    /// <summary>The lien's look at every debtor, on its clock. From the module's timer.</summary>
    internal static void Collect(long now)
    {
        if (_debts.Count == 0 || now - _lastCollect < CollectMs)
        {
            if (_debts.Count == 0)
            {
                _lastCollect = now;
            }

            return;
        }

        var minutes = _lastCollect == 0 ? 0.0 : (now - _lastCollect) / 60000.0;

        _lastCollect = now;
        _ended.Clear();

        foreach (var (name, debt) in _debts)
        {
            var guild = BotGuilds.Named(debt.Guild);

            if (guild == null || guild.Disbanded)
            {
                Write(debt, $"{debt.Guild} is closed");
                _ended.Add(name);

                continue;
            }

            var debtor = BotOfficers.Named(name);

            if (debtor == null)
            {
                if (!BotRest.Records.ContainsKey(name))
                {
                    Write(debt, "the debtor is gone from the island");
                    _ended.Add(name);
                }

                continue;
            }

            if (debtor.Map == null || debtor.Map == Map.Internal)
            {
                continue;
            }

            debt.Played += minutes;

            if (Lien(debt, debtor))
            {
                _ended.Add(name);

                continue;
            }

            if (debt.Played >= DefaultHours * 60.0 && debt.Left > 0)
            {
                _marked.Add(name);
                Write(debt, $"{DefaultHours:0.#}h of its play passed with {debt.Left}gp unpaid; it draws no kit again");
                _ended.Add(name);
            }
        }

        for (var i = 0; i < _ended.Count; i++)
        {
            _debts.Remove(_ended[i]);
        }
    }

    private static bool Lien(Debt debt, BotMobile debtor)
    {
        var wealth = BotYield.Wealth(debtor);

        if (wealth <= debt.Mark)
        {
            debt.Mark = wealth;

            return false;
        }

        var take = Math.Min(debt.Left, (int)Math.Ceiling((wealth - debt.Mark) * Math.Clamp(LienShare, 0.0, 1.0)));

        if (take <= 0)
        {
            return false;
        }

        if (!BotMarket.Charge(debtor, take))
        {
            LienHeld++;

            return false;
        }

        // Out of the debtor's work by a decision outside it, into the chest: booked aside, as the moneylender's lien is.
        BotYield.Aside(debtor, take);
        BotChest.Put(debt.Guild, take);

        debt.Repaid += take;
        debt.Mark = wealth - take;
        LienTakes++;
        LienGold += take;

        if (debt.Left > 0)
        {
            return false;
        }

        PaidOff++;

        logger.Information(
            "{Bot} has paid {Guild}'s store back in full: {Owed}gp for its kits, out of its takings over {Hours:F1}h of play; the chest holds {Chest}gp",
            debt.Debtor,
            debt.Guild,
            debt.Owed,
            debt.Played / 60.0,
            BotChest.Holds(debt.Guild)
        );

        BotVoice.Say(debtor, "guild", BotVoice.Phrase("officers:repaid", null, debtor, null) ?? "My debt to the guild's store is paid.");

        return true;
    }

    private static void Write(Debt debt, string why)
    {
        WrittenOff++;
        WrittenGold += debt.Left;

        logger.Information(
            "{Guild}'s store wrote off {Bot}'s debt — {Why}: {Repaid} of {Owed}gp repaid, {Left}gp lost to the chest",
            debt.Guild,
            debt.Debtor,
            why,
            debt.Repaid,
            debt.Owed,
            debt.Left
        );
    }

    // ---- surplus -------------------------------------------------------------------------------------------------------------

    /// <summary>How long a kind must go unissued before what the store holds over <see cref="KeepShare"/> of its most is surplus.</summary>
    public static int SurplusAfterMs { get; set; } = 10800000;

    /// <summary>The share of a kind's most the store keeps whatever happens.</summary>
    public static double KeepShare { get; set; } = 0.6;

    /// <summary>The most units of one kind put on the shelf at one shift.</summary>
    public static int ShelfLot { get; set; } = 40;

    /// <summary>
    /// One lot of the store's surplus onto the guild's merchant at the hall, at the market's price: a stacking kind no member has
    /// drawn for <see cref="SurplusAfterMs"/>, above <see cref="KeepShare"/> of its most. The merchant's takings come back to the
    /// chest by the quartermaster's hand (<see cref="BotQuartermaster"/>). Returns the units shelved.
    /// </summary>
    internal static int Shelve(BotMobile qm, Guild guild)
    {
        if (!_stores.TryGetValue(guild.Name, out var store) || BotShelf.Of(guild) is not { Deleted: false } merchant)
        {
            return 0;
        }

        var now = Core.TickCount;

        foreach (var (kind, n) in store)
        {
            if (Bottle(kind) || n <= (int)(Most(kind) * KeepShare))
            {
                continue;
            }

            if (_lastIssued.TryGetValue((guild.Name, kind), out var last) && now - last < SurplusAfterMs)
            {
                continue;
            }

            if (!BotShelf.Room(merchant, kind))
            {
                continue;
            }

            var units = Math.Min(ShelfLot, n - (int)(Most(kind) * KeepShare));
            var item = BotBinding.Make(kind, 1);

            if (item == null || !item.Stackable)
            {
                item?.Delete();

                continue;
            }

            item.Amount = units;

            var price = units * Price(kind);

            if (!BotShelf.Put(merchant, item, price))
            {
                if (!item.Deleted)
                {
                    item.Delete();
                }

                continue;
            }

            store[kind] = n - units;
            Shelved += units;
            ShelvedGold += price;

            logger.Information(
                "{Qm} the quartermaster put {Units} {Kind} of {Guild}'s store's surplus on the hall's merchant at {Price}gp: nobody drew one for {Hours}h and the store keeps {Kept}",
                qm.Name,
                units,
                kind.Name,
                guild.Name,
                price,
                SurplusAfterMs / 3600000,
                store[kind]
            );

            return units;
        }

        return 0;
    }

    // ---- telling -----------------------------------------------------------------------------------------------------------

    public static string Tell(string guild)
    {
        var owed = 0;
        var debtors = 0;

        foreach (var debt in _debts.Values)
        {
            if (debt.Guild == guild)
            {
                debtors++;
                owed += debt.Left;
            }
        }

        return $"store {Stock(guild, 8)} (worth {Worth(guild)}gp), {debtors} members owing {owed}gp";
    }

    public static string Describe()
    {
        var stores = 0;
        var units = 0L;
        var worth = 0L;

        foreach (var (guild, store) in _stores)
        {
            stores++;

            foreach (var n in store.Values)
            {
                units += n;
            }

            worth += Worth(guild);
        }

        var owed = 0L;

        foreach (var debt in _debts.Values)
        {
            owed += debt.Left;
        }

        return !Running
            ? "OFF"
            : $"{stores} stores holding {units} units worth {worth}gp; in: {GiftsIn} gifts of {GiftUnits} units worth {GiftGold}gp ({PorterUnits} by the porter, {HandedUnits} handed at the store), {BoughtUnits} bought for the guild, {FullRefused} refused for a full kind; "
              + $"out: {Kits} kits of {KitUnits} units worth {KitGold}gp issued as debts ({KitPackRefused} lots a full pack refused, {KitEmpty} draws that found nothing it lacked); "
              + $"debts: {_debts.Count} standing owing {owed}gp, {LienTakes} lien takes of {LienGold}gp into the chests ({LienHeld} held for a purse that could not pay), {PaidOff} paid off, {WrittenOff} written off ({WrittenGold}gp lost), {_marked.Count} defaulters; "
              + $"{Shelved} units of surplus shelved for {ShelvedGold}gp";
    }

    public static void Write(Utf8JsonWriter w, string guild)
    {
        w.WritePropertyName("store");
        w.WriteStartObject();

        foreach (var (kind, n) in Of(guild))
        {
            if (n > 0)
            {
                w.WriteNumber(kind.Name, n);
            }
        }

        w.WriteEndObject();
        w.WriteNumber("storeWorth", Worth(guild));

        var owed = 0;
        var debtors = 0;

        foreach (var debt in _debts.Values)
        {
            if (debt.Guild == guild)
            {
                debtors++;
                owed += debt.Left;
            }
        }

        w.WriteNumber("debtors", debtors);
        w.WriteNumber("owed", owed);
    }

    // ---- persistence -----------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_stores.Count);

        foreach (var (guild, store) in _stores)
        {
            writer.Write(guild);
            writer.WriteEncodedInt(store.Count);

            foreach (var (kind, n) in store)
            {
                writer.Write(kind.Name);
                writer.WriteEncodedInt(Math.Max(0, n));
            }
        }

        writer.WriteEncodedInt(_debts.Count);

        foreach (var debt in _debts.Values)
        {
            writer.Write(debt.Debtor);
            writer.Write(debt.Guild);
            writer.WriteEncodedInt(debt.Owed);
            writer.WriteEncodedInt(debt.Repaid);
            writer.WriteEncodedInt(debt.Mark);
            writer.Write(debt.Played);
        }

        writer.WriteEncodedInt(_marked.Count);

        foreach (var name in _marked)
        {
            writer.Write(name);
        }
    }

    internal static (int Stores, int Debts) Load(IGenericReader reader)
    {
        _stores.Clear();
        _debts.Clear();
        _marked.Clear();

        var stores = reader.ReadEncodedInt();
        var lost = 0;

        for (var i = 0; i < stores; i++)
        {
            var guild = reader.ReadString();
            var kinds = reader.ReadEncodedInt();
            Dictionary<Type, int> store = [];

            for (var k = 0; k < kinds; k++)
            {
                var name = reader.ReadString();
                var n = reader.ReadEncodedInt();
                var kind = string.IsNullOrEmpty(name) ? null : AssemblyHandler.FindTypeByName(name);

                if (kind == null || !Storable(kind))
                {
                    lost += n;

                    continue;
                }

                store[kind] = n;
            }

            if (!string.IsNullOrEmpty(guild))
            {
                _stores[guild] = store;
            }
        }

        if (lost > 0)
        {
            logger.Warning("{Lost} units in the guilds' stores were of kinds this build cannot keep, and are not read back", lost);
        }

        var debts = reader.ReadEncodedInt();

        for (var i = 0; i < debts; i++)
        {
            var debt = new Debt
            {
                Debtor = reader.ReadString(),
                Guild = reader.ReadString(),
                Owed = reader.ReadEncodedInt(),
                Repaid = reader.ReadEncodedInt(),
                Mark = reader.ReadEncodedInt(),
                Played = reader.ReadDouble()
            };

            if (!string.IsNullOrEmpty(debt.Debtor) && debt.Left > 0)
            {
                _debts[debt.Debtor] = debt;
            }
        }

        var marked = reader.ReadEncodedInt();

        for (var i = 0; i < marked; i++)
        {
            var name = reader.ReadString();

            if (!string.IsNullOrEmpty(name))
            {
                _marked.Add(name);
            }
        }

        return (_stores.Count, _debts.Count);
    }

    /// <summary>A reload: the counters only. The goods and the debts are the store's to keep.</summary>
    public static void Forget()
    {
        GiftsIn = 0;
        GiftUnits = 0;
        GiftGold = 0;
        PorterUnits = 0;
        HandedUnits = 0;
        BoughtUnits = 0;
        FullRefused = 0;
        Kits = 0;
        KitUnits = 0;
        KitGold = 0;
        KitPackRefused = 0;
        KitEmpty = 0;
        LienTakes = 0;
        LienGold = 0;
        LienHeld = 0;
        PaidOff = 0;
        WrittenOff = 0;
        WrittenGold = 0;
        Shelved = 0;
        ShelvedGold = 0;
        _lastCollect = 0;
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): every guild's store emptied and every kit debt struck off, so the save writes them
    /// empty.</b> <see cref="Forget"/> keeps the goods and the debts for a reload; a new world founds its guilds again, and an old
    /// name's store would have handed its old stock to a guild that never gathered it, and a newcomer under an old name would have
    /// been born owing for a kit it never drew. This empties exactly what <see cref="Save"/> writes — the stores, the debts, the
    /// defaulters — and when each kind was last issued.
    ///
    /// <para>
    /// <b>No coin is held here.</b> A store's stock is goods counted by kind, given or carried in or bought from a counter with the
    /// buyer's coin at the time; a kit debt is a line in the book, repaid out of the debtor's takings into the guild's chest as it
    /// was earned. Emptying them moves no gold. Returns the stores emptied.
    /// </para>
    /// </summary>
    public static int Wipe()
    {
        var stores = _stores.Count;

        _stores.Clear();
        _lastIssued.Clear();
        _debts.Clear();
        _marked.Clear();
        _ended.Clear();
        Forget();

        return stores;
    }
}
