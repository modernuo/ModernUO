using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A trader's order book: a blue book in its pack, written by the trader and read by anybody who opens it.
///
/// <para>
/// <b>Plan v2 of 01.10.2026, point 3: "give them a fun feature — write the orders into a book, and strike them out when
/// delivered" (build 385).</b> The cover counts the orders written, delivered and struck; one page each for the orders open
/// now — for whom, what, how many, at what, when and where it was taken, where it is being bought; and below them the closed
/// orders, newest first, each marked X with how it ended: delivered and for how much, or struck and why.
/// </para>
///
/// <para>
/// <b>An ordinary engine book</b>, a <see cref="BlueBook"/> that the trader writes and nobody else can: not writable, not
/// movable, blessed so it stays with its trader through a death. A player or a GM who opens it reads it like any book; the
/// pages are the book's own and are saved with it, so a restart keeps the history and strikes whatever was open ("the island
/// slept"). Era-appropriate: the blue book is the classic one.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotOrderBook : BlueBook
{
    [Constructible]
    public BotOrderBook() : base("Order Book", "a trader", 1, false)
    {
        Movable = false;
        LootType = LootType.Blessed;
    }

    /// <summary>The pages, written whole. A book's page count is what its pages array holds.</summary>
    internal void Rewrite(string title, string author, BookPageInfo[] pages)
    {
        Title = title;
        Author = author;
        Pages = pages;
    }
}

/// <summary>One order in a trader's book: a want it took, the price agreed, and how it ended.</summary>
public sealed class BotTradeOrder
{
    public int Number { get; init; }

    public BotWant Want { get; init; }

    public Mobile Buyer { get; init; }

    public string BuyerName { get; init; }

    public Type Kind { get; init; }

    public string Item { get; init; }

    /// <summary>Units promised; brought down if the purchase came up short.</summary>
    public int Units { get; set; }

    /// <summary>The price agreed for one: the buyer's own offer, or the carriage it agreed to.</summary>
    public int Price { get; init; }

    /// <summary>The buyer's own offer when the order was taken.</summary>
    public int Offered { get; init; }

    public DateTime Taken { get; init; }

    public string Town { get; init; }

    public string From { get; set; }

    public int Handed { get; set; }

    public int Paid { get; set; }

    public bool Closed { get; set; }

    /// <summary>Whether the buyer has been seen lying dead, and since when: the tick means nothing until the flag is set.</summary>
    public bool DeadSeen { get; set; }

    public long DeadSince { get; set; }
}

/// <summary>
/// The book's contents for one trader, held in memory and written into its <see cref="BotOrderBook"/> at every change. Read
/// back from the book's own pages the first time a trader writes after a boot, so the counts and the history survive.
/// </summary>
public sealed class BotOrders
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOrders));

    /// <summary>The widest line a book page shows comfortably.</summary>
    public const int Width = 20;

    private const int LinesPerPage = 8;

    private static readonly Dictionary<Serial, BotOrders> _books = [];

    private static readonly Dictionary<string, long> _struckWhy = [];

    // ---- Island-wide, this session ----------------------------------------------------------------------------------------

    public static long LinesWritten { get; private set; }

    public static long LinesDelivered { get; private set; }

    public static long LinesStruck { get; private set; }

    public static long Restarted { get; private set; }

    public static long BooksMade { get; private set; }

    // ---- This trader's --------------------------------------------------------------------------------------------------------

    private readonly List<BotTradeOrder> _open = [];

    private readonly List<string[]> _history = [];

    private bool _read;

    public int Written { get; private set; }

    public int Delivered { get; private set; }

    public int Struck { get; private set; }

    public long Margin { get; private set; }

    public int Next { get; private set; } = 1;

    public IReadOnlyList<BotTradeOrder> Open => _open;

    public static BotOrders Of(Mobile trader)
    {
        if (trader == null)
        {
            return null;
        }

        if (!_books.TryGetValue(trader.Serial, out var orders))
        {
            orders = new BotOrders();
            _books[trader.Serial] = orders;
        }

        return orders;
    }

    /// <summary>A line written: the trader has taken this want at this price.</summary>
    public BotTradeOrder Write(Mobile trader, BotWant want, int units, int price, string from)
    {
        ReadBack(trader);

        var buyer = want?.Buyer?.Self;
        var order = new BotTradeOrder
        {
            Number = Next++,
            Want = want,
            Buyer = buyer,
            BuyerName = buyer?.Name ?? "somebody",
            Kind = want?.Kind,
            Item = want?.Kind == null ? "goods" : BotShopkeep.Words(want.Kind),
            Units = units,
            Price = price,
            Offered = want?.Offer ?? price,
            Taken = DateTime.Now,
            Town = buyer == null ? "the road" : BotTowns.Of(buyer.Location)?.Name ?? "the road",
            From = from
        };

        _open.Add(order);
        Written++;
        LinesWritten++;

        Render(trader);

        logger.Information(
            "{Trader} wrote order #{Number} into its book: {Units} {Item} for {Buyer} in {Town} at {Price}gp each (offered {Offered}), to buy {From}",
            trader?.Name,
            order.Number,
            units,
            order.Item,
            order.BuyerName,
            order.Town,
            price,
            order.Offered,
            from
        );

        return order;
    }

    /// <summary>The line struck as delivered: handed over, paid for.</summary>
    public void Deliver(Mobile trader, BotTradeOrder order, int units, int paid, int cost)
    {
        if (order == null || order.Closed)
        {
            return;
        }

        order.Handed += units;
        order.Paid += paid;
        order.Closed = true;
        _open.Remove(order);

        var margin = paid - cost;

        Delivered++;
        Margin += margin;
        LinesDelivered++;

        Close(
            $"X #{order.Number} {order.BuyerName}",
            $"{units} {order.Item}",
            $"done {paid}gp ({(margin >= 0 ? "+" : "")}{margin})"
        );

        Render(trader);
    }

    /// <summary>The line struck unfulfilled, with the reason.</summary>
    public void Strike(Mobile trader, BotTradeOrder order, string why)
    {
        if (order == null || order.Closed)
        {
            return;
        }

        order.Closed = true;
        _open.Remove(order);

        Struck++;
        LinesStruck++;

        var reason = why ?? "given up";

        // Counted by reason for the summary; a reason with a name or a number in it would grow the table without end, so past
        // a few dozen kinds of reason the rest are counted together.
        var bucket = _struckWhy.ContainsKey(reason) || _struckWhy.Count < 48 ? reason : "other reasons";

        _struckWhy[bucket] = _struckWhy.GetValueOrDefault(bucket) + 1;

        Close($"X #{order.Number} {order.BuyerName}", $"{order.Units} {order.Item}", $"struck: {reason}");

        Render(trader);

        logger.Information(
            "{Trader} struck order #{Number} out of its book: {Units} {Item} for {Buyer} — {Why}",
            trader?.Name,
            order.Number,
            order.Units,
            order.Item,
            order.BuyerName,
            reason
        );
    }

    /// <summary>The open order's "where it is bought" line, changed when the trader goes to another counter.</summary>
    public void Rebought(Mobile trader, BotTradeOrder order, string from)
    {
        if (order is { Closed: false })
        {
            order.From = from;
            Render(trader);
        }
    }

    private void Close(params string[] lines)
    {
        List<string> wrapped = [];

        for (var i = 0; i < lines.Length; i++)
        {
            Wrap(lines[i], i == 0 ? "" : " ", wrapped);
        }

        _history.Insert(0, [.. wrapped]);

        var most = Math.Max(1, BotTrading.BookHistory);

        while (_history.Count > most)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    /// <summary>
    /// The book's own pages read back, once per boot: its counts from the cover, its closed orders, and whatever it showed as
    /// open — the orders of a world that has since restarted, which nobody is bringing any more, struck as such.
    /// </summary>
    private void ReadBack(Mobile trader)
    {
        if (_read)
        {
            return;
        }

        _read = true;

        var book = trader?.Backpack?.FindItemByType<BotOrderBook>();

        if (book?.Pages == null || book.Pages.Length == 0)
        {
            return;
        }

        var pages = book.Pages;
        List<string> entry = null;

        for (var p = 0; p < pages.Length; p++)
        {
            var lines = pages[p]?.Lines;

            if (lines == null || lines.Length == 0)
            {
                continue;
            }

            if (p == 0)
            {
                Written = Count(lines, "written ");
                Delivered = Count(lines, "delivered ");
                Struck = Count(lines, "struck ");
                Next = Math.Max(Next, Count(lines, "next #"));
                Margin = Count(lines, "margin ");

                continue;
            }

            if (lines[0].StartsWith('#') && lines[0].EndsWith(" OPEN", StringComparison.Ordinal))
            {
                // An order open when the world stopped: struck, since the want it answered went with the world.
                var number = lines[0][1..^5].Trim();
                var buyer = lines.Length > 1 ? lines[1].Replace("for ", "") : "somebody";
                var what = lines.Length > 2 ? lines[2] : "";

                List<string> lost = [];
                Wrap($"X #{number} {buyer}", "", lost);
                Wrap(what, " ", lost);
                Wrap("struck: the island slept", " ", lost);
                _history.Add([.. lost]);

                Struck++;
                Restarted++;

                continue;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith("X #", StringComparison.Ordinal))
                {
                    if (entry != null)
                    {
                        _history.Add([.. entry]);
                    }

                    entry = [line];
                }
                else if (line.StartsWith(' ') && entry != null)
                {
                    entry.Add(line);
                }
            }
        }

        if (entry != null)
        {
            _history.Add([.. entry]);
        }

        while (_history.Count > Math.Max(1, BotTrading.BookHistory))
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private static int Count(string[] lines, string label)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line == null || !line.StartsWith(label, StringComparison.Ordinal))
            {
                continue;
            }

            var number = line.AsSpan(label.Length).Trim();

            if (number.EndsWith("gp", StringComparison.Ordinal))
            {
                number = number[..^2];
            }

            if (int.TryParse(number, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    /// <summary>The whole book written again from what is held here. Cheap: it happens when an order is taken or ends.</summary>
    private void Render(Mobile trader)
    {
        var pack = trader?.Backpack;

        if (pack == null)
        {
            return;
        }

        var book = pack.FindItemByType<BotOrderBook>();

        if (book == null)
        {
            book = new BotOrderBook();

            if (!pack.TryDropItem(trader, book, false))
            {
                pack.DropItem(book);
            }

            BooksMade++;
        }

        List<BookPageInfo> pages =
        [
            new(
                "ORDER BOOK",
                Cut(trader.Name),
                $"written {Written}",
                $"delivered {Delivered}",
                $"struck {Struck}",
                $"open {_open.Count}",
                Cut($"margin {Margin}gp"),
                $"next #{Next}"
            )
        ];

        for (var i = 0; i < _open.Count; i++)
        {
            var o = _open[i];

            pages.Add(
                new BookPageInfo(
                    $"#{o.Number} OPEN",
                    Cut($"for {o.BuyerName}"),
                    Cut($"{o.Units} {o.Item}"),
                    Cut(o.Price > o.Offered ? $"at {o.Price}gp ({o.Offered})" : $"at {o.Price}gp each"),
                    $"taken {o.Taken:dd.MM HH:mm}",
                    Cut($"in {o.Town}"),
                    Cut($"buy {o.From}"),
                    ""
                )
            );
        }

        List<string> lines = [];

        for (var i = 0; i < _history.Count; i++)
        {
            var entry = _history[i];

            // An entry is never split across two pages: it starts a new page when it would not fit.
            var used = lines.Count % LinesPerPage;

            if (used > 0 && used + entry.Length > LinesPerPage)
            {
                while (lines.Count % LinesPerPage != 0)
                {
                    lines.Add("");
                }
            }

            lines.AddRange(entry);
        }

        for (var i = 0; i < lines.Count; i += LinesPerPage)
        {
            var count = Math.Min(LinesPerPage, lines.Count - i);

            pages.Add(new BookPageInfo(lines.GetRange(i, count).ToArray()));
        }

        book.Rewrite("Order Book", trader.Name, [.. pages]);
    }

    private static string Cut(string line) => line == null ? "" : line.Length <= Width ? line : line[..Width];

    /// <summary>A line folded at <see cref="Width"/>, the folds and every line after the first indented by <paramref name="indent"/>.</summary>
    private static void Wrap(string text, string indent, List<string> into)
    {
        var rest = (indent + (text ?? "")).TrimEnd();

        while (rest.Length > Width)
        {
            var cut = rest.LastIndexOf(' ', Width);

            if (cut <= indent.Length)
            {
                cut = Width;
            }

            into.Add(rest[..cut]);
            rest = " " + rest[cut..].TrimStart();
        }

        if (rest.Trim().Length > 0)
        {
            into.Add(rest);
        }
    }

    public static string Describe()
    {
        var open = 0;

        foreach (var (_, orders) in _books)
        {
            open += orders._open.Count;
        }

        List<(string Why, long Times)> why = [];

        foreach (var (reason, times) in _struckWhy)
        {
            why.Add((reason, times));
        }

        why.Sort(static (a, b) => b.Times.CompareTo(a.Times));

        var said = new List<string>();

        for (var i = 0; i < why.Count && i < 5; i++)
        {
            said.Add($"{why[i].Why} {why[i].Times}");
        }

        return $"{LinesWritten} lines written, {LinesDelivered} struck as delivered, {LinesStruck} struck unfulfilled"
               + (said.Count > 0 ? $" ({string.Join(", ", said)})" : "")
               + $", {open} open now, {Restarted} open at the last stop struck when read back, {BooksMade} books opened";
    }

    public static void Forget()
    {
        _books.Clear();
        _struckWhy.Clear();
        LinesWritten = 0;
        LinesDelivered = 0;
        LinesStruck = 0;
        Restarted = 0;
        BooksMade = 0;
    }
}
