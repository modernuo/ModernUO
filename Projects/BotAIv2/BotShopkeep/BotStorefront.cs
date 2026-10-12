using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// One bot's shop while it is open: who keeps it, where it stands, what it holds and at what price, and what it has taken.
///
/// <para>
/// <b>A snapshot, not the goods.</b> Unlike a stall on the auction that was, nothing here is lifted out of the world: the
/// goods stay in the keeper's own pack and bank box, where the engine keeps them and a save keeps them, and they move to a
/// customer only at the till (<see cref="BotShopkeep.Serve"/>). What this holds is the keeper's last count of them, taken
/// every <see cref="BotShopkeep.StockMs"/> and cut down at every sale, so that a customer deciding whether the walk is
/// worth it reads a dictionary rather than two containers.
/// </para>
/// </summary>
public sealed class BotStorefront
{
    private readonly Dictionary<Type, int> _units = [];

    private readonly Dictionary<Type, int> _asks = [];

    private readonly Dictionary<Type, int> _sold = [];

    private readonly HashSet<Serial> _customers = [];

    public BotStorefront(BotMobile keeper, BotDeed deed, Map map, Point3D stand, Point3D bank, string town)
    {
        Keeper = keeper;
        Deed = deed;
        Map = map;
        Location = stand;
        Bank = bank;
        Town = town ?? "the country";
        OpenedTick = Core.TickCount;
    }

    /// <summary>The bot behind the counter.</summary>
    public BotMobile Keeper { get; }

    /// <summary>The shift this shop belongs to. A shop whose keeper is holding anything else is shut. See <see cref="BotShopkeep.Live"/>.</summary>
    public BotDeed Deed { get; }

    public Map Map { get; }

    /// <summary>Where the keeper stands: a tile a few steps from the banker. Fixed for the shift, so a customer's walk is stable.</summary>
    public Point3D Location { get; }

    /// <summary>The banker the pitch was chosen beside. Two shops beside one banker keep <see cref="BotShopkeep.Apart"/> tiles between them.</summary>
    public Point3D Bank { get; }

    /// <summary>The town the bank stands in, by the region's name; "the country" outside every town.</summary>
    public string Town { get; }

    public long OpenedTick { get; }

    /// <summary>Set when the shift ends or is put down. A shut shop is taken off the list on the same beat.</summary>
    public bool Shut { get; internal set; }

    /// <summary>When the stock was last counted.</summary>
    public long StockedTick { get; internal set; }

    /// <summary>Things on show at the last count, and what they come to at the keeper's asks.</summary>
    public int Units { get; private set; }

    public int Worth { get; private set; }

    /// <summary>Of <see cref="Worth"/>, what the population is short of or has money down for (<see cref="BotShopkeep.Demanded"/>).</summary>
    public int Demanded { get; private set; }

    /// <summary>What was demanded when the shop opened: the denominator of the measured sell-through. See <see cref="BotShopkeep.Share"/>.</summary>
    public int Shown { get; internal set; }

    public int Kinds => _units.Count;

    /// <summary>Things sold this shift, sales made, and the coin taken for them.</summary>
    public int Sold { get; private set; }

    public int Sales { get; private set; }

    public int Takings { get; private set; }

    /// <summary>Different bots served this shift.</summary>
    public int Customers => _customers.Count;

    /// <summary>How many of this kind were on show at the last count, less what has sold since.</summary>
    public int Held(Type kind) => kind != null && _units.TryGetValue(kind, out var units) ? units : 0;

    /// <summary>What the keeper asks for one of this kind, or nought when it is not on show.</summary>
    public int Ask(Type kind) => kind != null && _asks.TryGetValue(kind, out var price) ? price : 0;

    /// <summary>Whether any of this kind sold this shift. Read at the close, where a kind nobody bought is marked down.</summary>
    public bool SoldAny(Type kind) => kind != null && _sold.ContainsKey(kind);

    /// <summary>The kinds on show, for the close and for the page.</summary>
    public IEnumerable<Type> Goods => _units.Keys;

    /// <summary>A fresh count. The dictionaries are copied, never kept, so the caller may reuse its own.</summary>
    internal void Restock(Dictionary<Type, int> units, Dictionary<Type, int> asks, int demanded)
    {
        _units.Clear();
        _asks.Clear();

        var total = 0;
        var worth = 0;

        foreach (var (kind, count) in units)
        {
            if (count <= 0 || !asks.TryGetValue(kind, out var price) || price <= 0)
            {
                continue;
            }

            _units[kind] = count;
            _asks[kind] = price;
            total += count;
            worth += count * price;
        }

        Units = total;
        Worth = worth;
        Demanded = demanded;
        StockedTick = Core.TickCount;
    }

    /// <summary>A sale, written into the snapshot at once, so the next customer's arithmetic does not count goods already gone.</summary>
    internal void Took(Mobile buyer, Type kind, int units, int gold)
    {
        if (units <= 0 || kind == null)
        {
            return;
        }

        if (_units.TryGetValue(kind, out var held))
        {
            var left = held - units;

            if (left > 0)
            {
                _units[kind] = left;
            }
            else
            {
                _units.Remove(kind);
            }

            var price = _asks.TryGetValue(kind, out var ask) ? ask : 0;

            Units = Math.Max(0, Units - units);
            Worth = Math.Max(0, Worth - units * price);
        }

        _sold[kind] = (_sold.TryGetValue(kind, out var before) ? before : 0) + units;

        Sold += units;
        Sales++;
        Takings += gold;

        if (buyer != null)
        {
            _customers.Add(buyer.Serial);
        }
    }

    /// <summary>A price moved during the shift. The snapshot follows at once rather than at the next count.</summary>
    internal void Priced(Type kind, int price)
    {
        if (kind != null && price > 0 && _asks.ContainsKey(kind))
        {
            _asks[kind] = price;
        }
    }

    /// <summary>The kinds on show, dearest stock first, with their counts and asks: for the page and the cry.</summary>
    public List<(Type Kind, int Units, int Ask)> Ranked()
    {
        List<(Type Kind, int Units, int Ask)> ranked = [];

        foreach (var (kind, units) in _units)
        {
            ranked.Add((kind, units, _asks.TryGetValue(kind, out var ask) ? ask : 0));
        }

        ranked.Sort(static (a, b) => (b.Units * b.Ask).CompareTo(a.Units * a.Ask));

        return ranked;
    }

    public override string ToString() => $"{Keeper?.Name}'s shop in {Town} at ({Location.X}, {Location.Y}): {Units} things worth {Worth}gp";
}
