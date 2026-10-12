using System;
using System.Collections.Generic;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The shops' book: what each town's bots bought from bots' shops and from shopkeepers, by kind, at what price, and what customers
/// came for and found gone.
///
/// <para>
/// <b>Kept for the merchant who does not exist yet.</b> Patrick's order of 29.09.2026 names the next step — a caste, and a guild of
/// merchants, that buy what moves, store it, and carry it between towns. Such a bot needs to know three things per town and kind:
/// what bots there pay a shopkeeper for it (the demand a bot could have met and did not — the <c>Counter</c> columns), what bots'
/// shops there sold it for (the supply that exists and its price — the <c>Shop</c> columns), and where customers walked to a shop
/// and found it gone (<c>Missed</c>). This writes all three from the moment the first shop opens, so the merchant, when it comes,
/// starts from a measured book rather than a guess. Nothing reads it yet but the summary and the page.
/// </para>
///
/// <para>
/// In memory only, like every other tally of trade on this shard; bounded at <see cref="MostRows"/>, past which the rows touched
/// longest ago are dropped.
/// </para>
/// </summary>
public static class BotShopBook
{
    /// <summary>One town and one kind.</summary>
    public sealed class Row
    {
        public string Town;

        public Type Kind;

        /// <summary>Bought at bots' shops in this town: units, gold, and the last price paid for one.</summary>
        public long ShopUnits;

        public long ShopGold;

        public int ShopLast;

        /// <summary>Bought by bots from shopkeepers in this town: units, gold, and the last price paid for one.</summary>
        public long CounterUnits;

        public long CounterGold;

        public int CounterLast;

        /// <summary>Customers who reached a shop here for this kind and found it gone.</summary>
        public long Missed;

        public long TouchedTick;
    }

    /// <summary>Rows kept at most.</summary>
    public static int MostRows { get; set; } = 512;

    private static readonly Dictionary<(string Town, Type Kind), Row> _rows = [];

    public static IEnumerable<Row> Rows => _rows.Values;

    private static Row RowOf(string town, Type kind)
    {
        town ??= "the country";

        if (_rows.TryGetValue((town, kind), out var row))
        {
            row.TouchedTick = Core.TickCount;

            return row;
        }

        if (_rows.Count >= MostRows)
        {
            Trim();
        }

        row = new Row { Town = town, Kind = kind, TouchedTick = Core.TickCount };
        _rows[(town, kind)] = row;

        return row;
    }

    /// <summary>A sale at a bot's shop.</summary>
    public static void Sold(string town, Type kind, int units, int gold)
    {
        if (kind == null || units <= 0)
        {
            return;
        }

        var row = RowOf(town, kind);

        row.ShopUnits += units;
        row.ShopGold += gold;
        row.ShopLast = gold / Math.Max(1, units);
    }

    /// <summary>A bot's purchase from a shopkeeper, booked under the town the bot was standing in. Called from <c>BotRestock</c>.</summary>
    public static void Counter(Mobile buyer, Type kind, int units, int gold)
    {
        if (buyer == null || kind == null || units <= 0)
        {
            return;
        }

        var row = RowOf(BotTowns.Of(buyer.Location)?.Name, kind);

        row.CounterUnits += units;
        row.CounterGold += gold;
        row.CounterLast = gold / Math.Max(1, units);
    }

    /// <summary>A customer reached a shop for this kind and found none left.</summary>
    public static void Missed(string town, Type kind)
    {
        if (kind != null)
        {
            RowOf(town, kind).Missed++;
        }
    }

    /// <summary>Drops the quarter of the rows touched longest ago.</summary>
    private static void Trim()
    {
        List<(long Tick, (string, Type) Key)> ordered = [];

        foreach (var (key, row) in _rows)
        {
            ordered.Add((row.TouchedTick, key));
        }

        ordered.Sort(static (a, b) => a.Tick.CompareTo(b.Tick));

        for (var i = 0; i < ordered.Count / 4 + 1 && i < ordered.Count; i++)
        {
            _rows.Remove(ordered[i].Key);
        }
    }

    /// <summary>
    /// The heaviest few rows by what bots paid shopkeepers — the trade a bot's shop, or a merchant, could have had — each with
    /// what bots' shops sold beside it.
    /// </summary>
    public static string Describe(int most)
    {
        if (_rows.Count == 0)
        {
            return "the shops' book is empty";
        }

        List<Row> ordered = [.. _rows.Values];

        ordered.Sort(static (a, b) => b.CounterGold.CompareTo(a.CounterGold));

        using var say = ValueStringBuilder.Create(256);

        say.Append("the shops' book, by what bots paid shopkeepers: ");

        for (var i = 0; i < ordered.Count && i < most; i++)
        {
            var row = ordered[i];

            if (i > 0)
            {
                say.Append("; ");
            }

            say.Append($"{row.Town} {row.Kind.Name} {row.CounterUnits} from shopkeepers for {row.CounterGold}gp against {row.ShopUnits} from bots' shops for {row.ShopGold}gp");

            if (row.Missed > 0)
            {
                say.Append($", {row.Missed} came to a shop and found none");
            }
        }

        return say.ToString();
    }

    public static void Forget() => _rows.Clear();
}
