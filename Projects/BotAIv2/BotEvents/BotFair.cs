using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The fair (07.10.2026, Patrick's second idea: "a fair in a town once a day: lower prices, traders and crafters come, a tournament
/// before Argus, a lottery"). Once every <see cref="EveryMs"/>, in the next town of the turn that has <see cref="LeastShops"/>
/// shopkeepers, announced <see cref="AheadMs"/> before it opens and open for <see cref="LastsMs"/>.
///
/// <list type="bullet">
/// <item><b>Prices.</b> A bot buying at an NPC's counter in the fair's town pays <see cref="Discount"/> of the shelf: the town's
/// standing factor (<c>BotBurgh.Factor</c>) is multiplied by it, and the rebate is settled the way a town's kindness always is
/// (<c>BotBurgh.Settle</c>): less coin destroyed at an NPC's counter, none made.</item>
/// <item><b>A reason to come.</b> Traders and crafters within <see cref="Reach"/> are offered the walk to the fair
/// (<see cref="BotFairgoer"/>), priced at what fairs have paid — the ledger keeps <c>fair</c> as a trade of its own, and
/// <see cref="Prior"/> is only the guess before the first one.</item>
/// <item><b>The championship.</b> Called at the fair's opening if its hour allows (<see cref="BotSeasons.CallTourney"/>), and any
/// championship called while the fair is open stands in the fair's town (<c>BotTourney</c> reads <see cref="TownName"/>).</item>
/// <item><b>The lottery.</b> A ticket is <see cref="Ticket"/>gp, one to a bot: bought by every fairgoer on arrival and by any bot
/// standing in the town with <see cref="LotteryLeast"/> to spare. The whole pot goes to one ticket drawn at the close. Zero-sum:
/// tickets are charged (<c>BotMarket.Charge</c>, which destroys the coin) and the pot is paid into the winner's account; a restart
/// before the draw hands every ticket back (<see cref="BotEventsStore"/>).</item>
/// </list>
/// </summary>
public static class BotFair
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFair));

    public static bool Running { get; set; } = true;

    /// <summary>How often a fair is held: once a day.</summary>
    public static int EveryMs { get; set; } = 86400000;

    /// <summary>How long after a boot the first fair opens, if none was held within <see cref="EveryMs"/> before it.</summary>
    public static int FirstMs { get; set; } = 5400000;

    /// <summary>How long before it opens a fair is announced, and how long it is open.</summary>
    public static int AheadMs { get; set; } = 3600000;

    public static int LastsMs { get; set; } = 7200000;

    /// <summary>What a bot pays at an NPC's counter in the fair's town, as a share of the shelf.</summary>
    public static double Discount { get; set; } = 0.9;

    /// <summary>The fewest shopkeepers a town needs to hold a fair.</summary>
    public static int LeastShops { get; set; } = 3;

    /// <summary>A lottery ticket, and the least a bot must have to spare to buy one.</summary>
    public static int Ticket { get; set; } = 10;

    public static int LotteryLeast { get; set; } = 200;

    /// <summary>How often the bots in the fair's town are offered a ticket.</summary>
    public static int TicketEveryMs { get; set; } = 60000;

    /// <summary>How far from the fair traders and crafters are offered the walk to it; how long a fairgoer stays.</summary>
    public static int Reach { get; set; } = 300;

    public static int StayMs { get; set; } = 180000;

    /// <summary>What going to the fair is reckoned at per minute before the ledger has seen it pay.</summary>
    public static double Prior { get; set; } = 15.0;

    // ---- state -------------------------------------------------------------------------------------------------------------

    public enum Phase
    {
        None,
        Announced,
        Open
    }

    public static Phase State { get; private set; }

    /// <summary>The town of the fair announced or open, or null.</summary>
    public static BotTowns.Town Town { get; private set; }

    /// <summary>The fair's town while it is open, for the championship's ring; null otherwise.</summary>
    public static string TownName => State == Phase.Open ? Town?.Name : null;

    public static bool Open => State == Phase.Open && Town != null;

    /// <summary>
    /// The fair standing is the Great Fair, called by a town relic's holder (07.10.2026, relics20): the same fair with twice the
    /// rebate at its counters, outside the turn of the towns and the day's calendar. See <see cref="CallGreat"/>.
    /// </summary>
    public static bool Great { get; private set; }

    /// <summary>The guild that called the Great Fair standing, and with which relic.</summary>
    public static string Patron { get; private set; }

    public static string PatronRelic { get; private set; }

    /// <summary>Great Fairs held.</summary>
    public static long GreatHeld { get; private set; }

    /// <summary>The ordinary fair's clock while a Great Fair stands: put back at its close, so the day's fair keeps its day.</summary>
    private static long _resumeTick;

    /// <summary>What a bot pays at the fair's counters as a share of the shelf: <see cref="Discount"/>, its rebate twice at a Great Fair.</summary>
    public static double Rate => Math.Clamp(Great ? 1.0 - 2.0 * (1.0 - Discount) : Discount, 0.5, 1.0);

    private static int Off => 100 - (int)Math.Round(Rate * 100);

    private static long _opensTick;

    private static long _closesTick;

    private static long _ticketTick;

    private static bool _seeded;

    private static long _nextTick;

    /// <summary>When the last fair opened, in UTC; kept by the store so a restart does not hold a second fair the same day.</summary>
    public static DateTime LastUtc { get; internal set; } = DateTime.MinValue;

    /// <summary>The turn of the towns, kept by the store.</summary>
    public static int Turn { get; internal set; }

    /// <summary>
    /// A town whose steward declared a holiday (07.10.2026, steward20): the next fair announced goes there if it has the shops,
    /// and the turn's town otherwise. Set with <see cref="CallNow"/> by <c>BotStewardship</c>; cleared when read.
    /// </summary>
    public static string Asked { get; set; }

    private static readonly Dictionary<Serial, int> _tickets = [];

    private static readonly HashSet<Serial> _came = [];

    public static int Pot { get; private set; }

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Held { get; private set; }

    public static long NoTown { get; private set; }

    public static long Offered { get; private set; }

    public static long Goers { get; private set; }

    public static long Arrived { get; private set; }

    public static long Missed { get; private set; }

    public static long Tickets { get; private set; }

    public static long TicketGold { get; private set; }

    public static long Draws { get; private set; }

    public static long PotsPaid { get; private set; }

    public static long PotGold { get; private set; }

    public static long Refunded { get; private set; }

    public static long PotLost { get; private set; }

    public static long Priced { get; private set; }

    public static long Rebates { get; private set; }

    public static long RebateGold { get; private set; }

    public static long Tourneys { get; private set; }

    public static long TourneysRefused { get; private set; }

    public static void Beat(long now)
    {
        if (!_seeded)
        {
            _seeded = true;

            // The next opening: a day after the last one, never sooner than FirstMs after this boot.
            var sinceLast = LastUtc == DateTime.MinValue ? long.MaxValue : (long)(DateTime.UtcNow - LastUtc).TotalMilliseconds;
            var wait = sinceLast >= EveryMs ? FirstMs : Math.Max(FirstMs, EveryMs - sinceLast);

            _nextTick = now + wait;
        }

        switch (State)
        {
            case Phase.None:
                {
                    if (Running && now - (_nextTick - AheadMs) >= 0)
                    {
                        Announce(now);
                    }

                    break;
                }
            case Phase.Announced:
                {
                    if (now - _opensTick >= 0)
                    {
                        Begin(now);
                    }

                    break;
                }
            case Phase.Open:
                {
                    if (now - _closesTick >= 0)
                    {
                        Close(now, "its hours are over");

                        break;
                    }

                    if (now - _ticketTick >= TicketEveryMs)
                    {
                        _ticketTick = now;
                        Sell();
                    }

                    break;
                }
        }
    }

    /// <summary>Set by hand to announce a fair at the next beat, opening <see cref="AheadMs"/> later.</summary>
    public static bool CallNow
    {
        get => false;
        set
        {
            if (value && State == Phase.None)
            {
                _seeded = true;
                _nextTick = Core.TickCount + AheadMs;
            }
        }
    }

    /// <summary>
    /// The Great Fair, called by the guild holding a town's relic (07.10.2026, relics20; Patrick: "the right to call the Great
    /// Fair"): announced now in that town and opened <see cref="AheadMs"/> later, its counters at twice the rebate. Refused while
    /// another fair is announced or open, or when the town has fewer than <see cref="LeastShops"/> shopkeepers. Outside the turn and
    /// the day's calendar: the ordinary fair's clock is put back when it closes. See <c>BotRelics</c>.
    /// </summary>
    public static bool CallGreat(BotTowns.Town town, string guild, string relic)
    {
        if (!Running || State != Phase.None || town == null || ShopsIn(town) < LeastShops)
        {
            return false;
        }

        var now = Core.TickCount;

        if (!_seeded)
        {
            _seeded = true;
            _nextTick = now + Math.Max(0, FirstMs);
        }

        _resumeTick = _nextTick;
        Great = true;
        Patron = guild;
        PatronRelic = relic;
        Town = town;
        State = Phase.Announced;
        _opensTick = now + Math.Max(0, AheadMs);

        logger.Information(
            "{Guild}, holding {Relic}, calls the Great Fair in {Town}: it opens in {Minutes} minutes for {Hours:F1} hours — counters at {Rate:P0} of the shelf for bots",
            guild,
            relic,
            town.Name,
            AheadMs / 60000,
            LastsMs / 3600000.0,
            Rate
        );

        BotSeasons.Announce(
            "fair",
            BotPopulation.Home,
            town.Square,
            $"Hear ye! By {relic}, {guild} calls the Great Fair in {town.Name} in {AheadMs / 60000} minutes: every counter {Off}% off for {LastsMs / 3600000.0:0.#} hours, and a lottery at {Ticket}gp a ticket."
        );

        return true;
    }

    private static void Announce(long now)
    {
        // A steward's holiday first (07.10.2026, steward20). See Asked.
        var town = Asked != null && BotTowns.Find(Asked) is { FromHome: true } asked && Shops(asked) >= LeastShops ? asked : Next();

        Asked = null;

        if (town == null)
        {
            NoTown++;
            _nextTick = now + Math.Max(3600000, EveryMs / 4);

            logger.Information("No fair this time: no town reached from home has {Least} shopkeepers", LeastShops);

            return;
        }

        Town = town;
        State = Phase.Announced;
        _opensTick = now + Math.Max(0, AheadMs);

        logger.Information(
            "A fair is announced in {Town}: it opens in {Minutes} minutes for {Hours:F1} hours — counters at {Discount:P0} of the shelf for bots, a lottery at {Ticket}gp a ticket, the championship at the fair if its hour allows",
            town.Name,
            AheadMs / 60000,
            LastsMs / 3600000.0,
            Discount,
            Ticket
        );

        BotSeasons.Announce(
            "fair",
            BotPopulation.Home,
            town.Square,
            $"Hear ye! A fair in {town.Name} in {AheadMs / 60000} minutes: every counter {100 - (int)Math.Round(Discount * 100)}% off for {LastsMs / 3600000.0:0.#} hours, a championship, and a lottery at {Ticket}gp a ticket."
        );
    }

    private static void Begin(long now)
    {
        State = Phase.Open;
        _closesTick = now + Math.Max(60000, LastsMs);
        _ticketTick = now;
        _tickets.Clear();
        _came.Clear();
        Pot = 0;
        Held++;

        // A Great Fair is outside the calendar and the turn: the day's fair is still owed (relics20).
        if (Great)
        {
            GreatHeld++;
        }
        else
        {
            LastUtc = DateTime.UtcNow;
            Turn++;
        }

        logger.Information("The {Great}fair in {Town} opens: for {Hours:F1} hours bots pay {Discount:P0} at its counters", Great ? "Great " : "", Town.Name, LastsMs / 3600000.0, Rate);

        BotSeasons.Announce("fair", BotPopulation.Home, Town.Square, $"The {(Great ? "Great Fair" : "fair")} in {Town.Name} is open! Counters {Off}% off, lottery tickets {Ticket}gp at the bank.");

        if (BotSeasons.CallTourney is { } call)
        {
            string said;

            try
            {
                said = call(Town.Name);
            }
            catch (Exception e)
            {
                said = e.Message;
            }

            if (said != null && said.StartsWith("the championship is on", StringComparison.OrdinalIgnoreCase))
            {
                Tourneys++;
            }
            else
            {
                TourneysRefused++;
            }

            logger.Information("The fair in {Town} asked for the championship: {Said}", Town.Name, said);
        }

        Sell();
    }

    private static void Close(long now, string why)
    {
        var town = Town;

        Draw(town);

        logger.Information(
            "The fair in {Town} closes ({Why}): {Came} came for it, {Tickets} tickets sold this fair; {Rebates} rebates at its counters for {Gold}gp so far in all",
            town?.Name,
            why,
            _came.Count,
            Tickets,
            Rebates,
            RebateGold
        );

        State = Phase.None;
        Town = null;
        _tickets.Clear();
        _came.Clear();

        // The day's fair keeps its day after a Great Fair: its clock is put back as it stood when the Great Fair was called (relics20).
        _nextTick = Great ? _resumeTick : now + Math.Max(3600000, EveryMs);
        Great = false;
        Patron = null;
        PatronRelic = null;
    }

    /// <summary>Shopkeepers in a town, up to <see cref="LeastShops"/>. For a steward's holiday (steward20).</summary>
    private static int Shops(BotTowns.Town town)
    {
        var shops = BotShops.Shops;
        var count = 0;

        for (var i = 0; i < shops.Count && count < LeastShops; i++)
        {
            if (shops[i] is { Deleted: false } v && town.Bounds.Contains(v.Location))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The next town of the turn that is reached from home and has <see cref="LeastShops"/> shopkeepers.</summary>
    private static BotTowns.Town Next()
    {
        var towns = BotTowns.All;
        var shops = BotShops.Shops;

        for (var step = 0; step < towns.Count; step++)
        {
            var town = towns[(Turn + step) % towns.Count];

            if (!town.FromHome)
            {
                continue;
            }

            if (ShopsIn(town, shops) >= LeastShops)
            {
                Turn += step;

                return town;
            }
        }

        return null;
    }

    /// <summary>The shopkeepers standing in a town, counted up to <see cref="LeastShops"/>.</summary>
    internal static int ShopsIn(BotTowns.Town town) => ShopsIn(town, BotShops.Shops);

    private static int ShopsIn(BotTowns.Town town, IReadOnlyList<BaseVendor> shops)
    {
        var count = 0;

        for (var i = 0; i < shops.Count && count < LeastShops; i++)
        {
            if (shops[i] is { Deleted: false } v && town.Bounds.Contains(v.Location))
            {
                count++;
            }
        }

        return count;
    }

    // ---- the counter -----------------------------------------------------------------------------------------------------------

    /// <summary>What a bot pays at this counter as a share of the shelf: <see cref="Discount"/> at the fair's town while it is open.</summary>
    public static double Counter(Mobile buyer, BaseVendor vendor)
    {
        if (!Open || buyer is not BotMobile || vendor == null || !Town.Bounds.Contains(vendor.Location))
        {
            return 1.0;
        }

        Priced++;

        return Rate;
    }

    /// <summary>A rebate settled at a counter: counted as the fair's when it is open and the buyer stands in its town.</summary>
    public static void Rebated(Mobile buyer, int back)
    {
        if (Open && back > 0 && buyer != null && Town.Bounds.Contains(buyer.Location))
        {
            Rebates++;
            RebateGold += back;
        }
    }

    // ---- the lottery --------------------------------------------------------------------------------------------------------

    /// <summary>A ticket for one bot, if it has none and can spare the price. True when bought.</summary>
    public static bool Buy(Mobile bot)
    {
        if (!Open || bot is not BotMobile { Deleted: false, Alive: true } || Ticket <= 0 || _tickets.ContainsKey(bot.Serial))
        {
            return false;
        }

        if (BotYield.Spendable(bot) < Math.Max(Ticket, LotteryLeast) || !BotMarket.Charge(bot, Ticket))
        {
            return false;
        }

        _tickets[bot.Serial] = 1;
        Pot += Ticket;
        Tickets++;
        TicketGold += Ticket;

        return true;
    }

    private static void Sell()
    {
        if (!Open)
        {
            return;
        }

        var bots = BotPopulation.Bots;
        var sold = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is { Deleted: false, Alive: true } && bot.Map == BotPopulation.Home && Town.Bounds.Contains(bot.Location) && Buy(bot))
            {
                sold++;

                if (sold <= 2)
                {
                    BotVoice.Say(bot, "local", $"A ticket for the {Town.Name} lottery, please. The pot stands at {Pot}gp!");
                }
            }
        }

        if (sold > 0)
        {
            logger.Information("{Sold} bots in {Town} bought a lottery ticket at {Ticket}gp; the pot stands at {Pot}gp on {Tickets} tickets", sold, Town.Name, Ticket, Pot, _tickets.Count);
        }
    }

    private static void Draw(BotTowns.Town town)
    {
        if (Pot <= 0)
        {
            return;
        }

        Draws++;

        List<Serial> drawn = [];

        foreach (var (serial, _) in _tickets)
        {
            drawn.Add(serial);
        }

        while (drawn.Count > 0)
        {
            var pick = Utility.Random(drawn.Count);
            var who = World.FindMobile(drawn[pick]);

            drawn.RemoveAt(pick);

            if (who is not { Deleted: false } || !BotSeasons.Pay(who, Pot))
            {
                continue;
            }

            PotsPaid++;
            PotGold += Pot;

            logger.Information("{Name} won the lottery of the fair in {Town}: {Pot}gp on {Tickets} tickets", who.Name, town?.Name, Pot, _tickets.Count);

            BotSeasons.Announce("fair", BotPopulation.Home, town?.Square ?? Point3D.Zero, $"{who.Name} wins the {town?.Name} lottery: {Pot}gp on {_tickets.Count} tickets!");
            BotSeasons.Say(who, "world", $"I won the lottery at the {town?.Name} fair — {Pot}gp!");

            Pot = 0;

            return;
        }

        // Nobody holding a ticket can be found: the coin was taken and has nowhere to go. Said and counted, never quietly.
        PotLost += Pot;

        logger.Warning("The lottery of the fair in {Town} found no ticket holder to pay: {Pot}gp on {Tickets} tickets is lost", town?.Name, Pot, _tickets.Count);

        Pot = 0;
    }

    /// <summary>Every ticket of a fair cut short by a restart, back to its holder. Called by the store after the world is read.</summary>
    internal static void Refund(List<(Serial Who, int Paid)> tickets)
    {
        for (var i = 0; i < tickets.Count; i++)
        {
            if (World.FindMobile(tickets[i].Who) is { Deleted: false } who && BotSeasons.Pay(who, tickets[i].Paid))
            {
                Refunded += tickets[i].Paid;
            }
            else
            {
                PotLost += tickets[i].Paid;
            }
        }
    }

    /// <summary>The tickets of an open fair, for the store.</summary>
    internal static IEnumerable<(Serial Who, int Paid)> Holders()
    {
        foreach (var (serial, _) in _tickets)
        {
            yield return (serial, Ticket);
        }
    }

    // ---- the fairgoers ----------------------------------------------------------------------------------------------------------

    /// <summary>Whether this bot has come to this fair already, or is on its way.</summary>
    internal static bool Came(Mobile bot) => bot != null && _came.Contains(bot.Serial);

    internal static void Going(Mobile bot)
    {
        if (bot != null && _came.Add(bot.Serial))
        {
            Goers++;
        }
    }

    internal static void Offer() => Offered++;

    internal static void Reached(Mobile bot)
    {
        Arrived++;

        if (Buy(bot))
        {
            BotSeasons.Say(bot, "local", $"At the {Town?.Name} fair at last — and a lottery ticket for luck.");
        }
    }

    internal static void Late() => Missed++;

    /// <summary>When the open fair closes, for a fairgoer judging whether the walk is still worth it.</summary>
    internal static long ClosesTick => _closesTick;

    public static string Describe() =>
        !Running
            ? "off"
            : $"{Held} held ({NoTown} times no town had {LeastShops} shopkeepers); "
              + (State switch
              {
                  Phase.Open => $"{(Great ? $"the Great Fair of {Patron} ({PatronRelic}) " : "")}open now in {Town?.Name} for {Math.Max(0, _closesTick - Core.TickCount) / 60000} minutes more, {_came.Count} came, the pot {Pot}gp on {_tickets.Count} tickets; ",
                  Phase.Announced => $"{(Great ? $"the Great Fair of {Patron} " : "")}announced in {Town?.Name}, opening in {Math.Max(0, _opensTick - Core.TickCount) / 60000} minutes; ",
                  _ => _seeded ? $"the next announced in {Math.Max(0, _nextTick - AheadMs - Core.TickCount) / 60000} minutes; " : ""
              })
              + $"{GreatHeld} Great Fairs held at {Math.Clamp(1.0 - 2.0 * (1.0 - Discount), 0.5, 1.0):P0}; {Priced} counter prices at {Discount:P0} (or the Great Fair's), {Rebates} rebates for {RebateGold}gp; {Offered} walks offered to traders and crafters, {Goers} taken, {Arrived} arrived, {Missed} too late; "
              + $"lottery: {Tickets} tickets for {TicketGold}gp, {Draws} draws, {PotsPaid} pots paid for {PotGold}gp, {Refunded}gp refunded after a restart, {PotLost}gp lost for want of a holder; championships at the fair: {Tourneys} called, {TourneysRefused} refused by its hour";

    /// <summary>A fair cut short by a reload: every ticket handed back, the fair closed.</summary>
    public static void Stand()
    {
        if (_tickets.Count > 0)
        {
            List<(Serial Who, int Paid)> back = [..Holders()];

            Refund(back);
            Pot = 0;
            _tickets.Clear();
        }

        State = Phase.None;
        Town = null;
        Great = false;
        Patron = null;
        PatronRelic = null;
    }

    public static void Forget()
    {
        State = Phase.None;
        Town = null;
        Great = false;
        Patron = null;
        PatronRelic = null;
        GreatHeld = 0;
        _tickets.Clear();
        _came.Clear();
        Pot = 0;
        _seeded = false;
        Held = NoTown = Offered = Goers = Arrived = Missed = Tickets = TicketGold = Draws = PotsPaid = PotGold = Refunded = PotLost = 0;
        Priced = Rebates = RebateGold = Tourneys = TourneysRefused = 0;
    }
}
