using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.BotAI.V2;

/// <summary>
/// One window onto the whole population, onto the market it trades in, and onto what it cannot get hold of.
///
/// <para>
/// <b>This exists because the first version could not answer "why is that bot doing that".</b> Its decisions
/// were unobservable, so every question about behaviour was answered by watching a shard for an evening — and
/// the answers, when they finally came, were things like "it is trading, one tick at a time, and walking a
/// graveyard while it does". Everything on the first tab is a number the decision layer already keeps; the
/// only new thing here is that they are in one place, side by side, per bot.
/// </para>
///
/// <para>
/// The column that matters most is the last kind: <b>the vector</b>. A bot's class declares what it is
/// working towards, and that share — how far along its own trade it is — is the only measure of whether this
/// population is going anywhere at all. Money says what a bot has; the vector says whether it is becoming
/// something.
/// </para>
///
/// <para>
/// The third tab is the newest and the one an admin will reach for when the population looks idle: it is the
/// demand side of the market — who is short of what, at what price, and with how much of their own money
/// already down. A shard where nothing is being asked for and a shard where everything is being asked for at
/// four times its opening offer look identical on the other two tabs.
/// </para>
///
/// <para>
/// Built as a <see cref="DynamicGump"/> rather than a cached one because every row is different, and behind
/// a static <see cref="DisplayTo"/> because that is this shard's rule: prerequisites are checked before the
/// gump exists, so it can never be sent empty.
/// </para>
/// </summary>
public sealed class BotDashboardGump : DynamicGump
{
    private const int Width = 1180;

    private const int Height = 560;

    /// <summary>How many rows a page holds. Twelve fits the height without a scrollbar.</summary>
    private const int Rows = 12;

    private const int RowHeight = 26;

    private const int Ink = 0x480;

    private const int Head = 0x481;

    private const int Good = 0x3F;

    private const int Bad = 0x21;

    /// <summary>The champion's row: gold. Patrick's order of 16.09.2026.</summary>
    private const int Gold = 0x35;

    /// <summary>A murderer's row: red, as the engine names it. A bot in a cell keeps the red — it is still a murderer.</summary>
    private const int Red = 0x26;

    /// <summary>A bot out of the world, resting: the whole row in grey. Patrick's order of 24.09.2026. See BotRest.</summary>
    private const int Offline = 0x3B2;

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDashboardGump));

    private const int BotsTab = 0;

    private const int MarketTab = 1;

    private const int NeedsTab = 2;

    /// <summary>
    /// The city: the one tab that does something to the world rather than reporting on it.
    ///
    /// <para>
    /// Everything else here is a window. This is a lever, and it is on a dashboard whose command is
    /// registered at <c>AccessLevel.Administrator</c> and whose responses check the same again — because a
    /// gump reply is a packet and a packet can be sent by anybody who has ever seen the window.
    /// </para>
    /// </summary>
    private const int CrownTab = 3;

    /// <summary>
    /// What the population knows, as opposed to what any one bot knows.
    ///
    /// <para>
    /// <b>The one tab that is about the shard's memory rather than its state.</b> Everything on the other
    /// four is a photograph — who is alive, what is on show at the shops, what somebody is short of, what the city
    /// wants — and all of it is true only for the second it was drawn. This is the opposite: what the island
    /// has taught thirty-three bots between them, decaying on its own clock, and it is the page to read when
    /// the question is "why is everybody suddenly doing that".
    /// </para>
    /// </summary>
    private const int KnownTab = 4;

    /// <summary>
    /// The island itself: every quadrant the population has an opinion about, worst ground first.
    ///
    /// <para>
    /// <b>The one tab that is about the world rather than about the bots.</b> Everything else here reports on
    /// what the population is doing; this reports on what it has found out about where it lives, which is the
    /// page to read when deciding where anybody should be sent. Sorted worst first because a list of safe
    /// ground is a list nobody acts on, and beside each dire square is whether the Baron has actually gone
    /// there — the one column that says whether the map is being used or merely kept.
    /// </para>
    /// </summary>
    private const int QuadTab = 5;

    /// <summary>
    /// What the watcher has declared, and where it is.
    ///
    /// <para>
    /// <b>The one tab about something that was nobody's plan.</b> Argus reads the shard, forms an opinion
    /// and turns it into a revel — a trade made worth three times as much for twelve minutes, sometimes with
    /// an orc camp raised to go with it. None of that is visible from the other five tabs: the population
    /// simply starts mining, and the reason is a sentence in a log file. This is that sentence, with the
    /// clock beside it and a button that puts whoever is reading it on the spot.
    /// </para>
    ///
    /// <para>
    /// Read through <see cref="BotCrier"/> rather than from the watcher itself, because the watcher lives in
    /// an assembly that references this one and must never be referenced back.
    /// </para>
    /// </summary>
    private const int RevelTab = 6;

    /// <summary>
    /// What the population owns: the guild halls, where they stand and what is in them.
    ///
    /// <para>
    /// <b>The only tab about something permanent.</b> Everything else here is gone by the next restart — the
    /// bots, their purses, their errands, the market. A hall is a world object: it outlives the population
    /// that levied for it, is found again at the next start by the name on its sign, and stays on the island
    /// until somebody takes it down. That is worth a page which says where each one is and puts whoever is
    /// reading it on the doorstep.
    /// </para>
    ///
    /// <para>
    /// The half of the page that is easy to miss is the lower half: the guilds that have <em>no</em> hall,
    /// with what they have raised against what one costs. A shard where nothing is ever built looks the same
    /// from every other tab, and the answer is always one of two numbers — the money, or the ground.
    /// </para>
    /// </summary>
    private const int HallTab = 7;

    /// <summary>
    /// The squares of the island guilds have taken, and the claims being fought over now.
    ///
    /// <b>Patrick's order of 10.09.2026.</b> Ownership of ground was written into the map pins and into one
    /// clause of a five-hundred-character summary line, which is to say it was legible to nobody. This is
    /// the register itself: who holds what, how they got it, and — the half that only matters while it is
    /// happening — who is standing in a field right now waiting to find out.
    /// </summary>
    private const int ClaimTab = 8;

    /// <summary>
    /// The guilds themselves: how many, what they are working towards, what they are all doing, and who
    /// they are at war with.
    ///
    /// <b>The one view of this population that is about the bands rather than the bots.</b> Everything else
    /// here counts individuals or things; five rows of this say what kind of evening the island is having.
    /// </summary>
    private const int GuildTab = 9;

    /// <summary>
    /// The bandits at large and what is proved against each of them.
    ///
    /// <b>Patrick's order of 17.09.2026, night:</b> "a tab with the bandits and how many murders are on each of them;
    /// only the uncaught and the current ones — the caught and those who have served their time come off the list."
    /// So this is not a rap sheet but a wanted board: what the city would pay today, for whom, and where it was last
    /// standing.
    /// </summary>
    private const int BandTab = 10;

    private readonly int _tab;

    private readonly int _page;

    private readonly int _pages;

    /// <summary>
    /// The exact rows this window is showing.
    ///
    /// Snapshotted in the constructor rather than read again while drawing or answering, and that is not
    /// tidiness: bots take turns and the market moves between the moment a window is sent and the moment a
    /// button on it comes back. A row that means one thing on screen and another in the response handler is
    /// how an admin tool teleports somebody to the wrong bot.
    /// </summary>
    private readonly List<BotMobile> _bots = [];

    /// <summary>The market's lots on show, one kind at one open shop by the bank. See <c>BotShopkeep.Lots</c>.</summary>
    private readonly List<(BotStorefront Front, Type Kind, int Units, int Ask)> _stalls = [];

    private readonly List<BotWant> _wants = [];

    /// <summary>The halls this window is showing, snapshotted like every other row on it.</summary>
    private readonly List<BaseHouse> _estate = [];

    /// <summary>
    /// Where the Claims tab's rows are, in the order the page draws them: the claims running, then the
    /// squares held.
    ///
    /// <para>
    /// <b>Only the destinations are frozen, and that is the whole of what the button needs.</b> A claim
    /// lasts five minutes and can settle between the moment this window is drawn and the moment somebody
    /// clicks a row on it — so the row's numbers are read live while drawing, and the place is taken now.
    /// A button that read the live list again would walk to whatever had shuffled into that position, which
    /// is the fault this file's own note about snapshots was written for.
    /// </para>
    /// </summary>
    private readonly List<(Map Map, Point3D Where, string What)> _spots = [];

    public override bool Singleton => true;

    private BotDashboardGump(int tab, int page) : base(30, 30)
    {
        _tab = tab is MarketTab or NeedsTab or CrownTab or KnownTab or QuadTab or RevelTab or HallTab
            or ClaimTab or GuildTab or BandTab
            ? tab
            : BotsTab;

        if (_tab == MarketTab)
        {
            var market = Market();

            _pages = Math.Max(1, (market.Count + Rows - 1) / Rows);
            _page = Math.Clamp(page, 0, _pages - 1);

            Fill(market, _stalls);

            return;
        }

        if (_tab == NeedsTab)
        {
            var needs = Needs();

            _pages = Math.Max(1, (needs.Count + Rows - 1) / Rows);
            _page = Math.Clamp(page, 0, _pages - 1);

            Fill(needs, _wants);

            return;
        }

        // <b>The quadrant tab's visit list is the rangers, not the population, and that is not a shortcut.</b>
        // Visit reads _bots by row, and Fill only ever puts the *current page* into it — so a button numbered
        // from the whole population would point at whoever happens to be twelfth on this page, or at nothing
        // at all. One list, filled with exactly the bots this page offers to walk to.
        if (_tab is QuadTab or RevelTab or ClaimTab or GuildTab)
        {
            _pages = 1;
            _page = 0;

            if (_tab == ClaimTab)
            {
                // The same order the page draws in, and taken here so the buttons cannot drift off their
                // rows. See _spots.
                foreach (var bid in BotClaim.Bids)
                {
                    if (_spots.Count >= 5)
                    {
                        break;
                    }

                    _spots.Add((bid.Map, bid.Middle, $"{BotClaim.Short(bid.Guild)} is claiming this square"));
                }

                // The held rows are numbered from five whatever the claims did, so the gap is filled rather
                // than closed: a button's number is its row on the screen, and screens with two lists on
                // them are where off-by-one teleports come from.
                while (_spots.Count < 5)
                {
                    _spots.Add((null, Point3D.Zero, null));
                }

                foreach (var (key, guild, _) in BotClaim.Owned())
                {
                    if (_spots.Count >= 5 + Rows - 2)
                    {
                        break;
                    }

                    var middle = BotClaim.Middle(
                        BotPopulation.Home,
                        new Point3D(
                            key.X * BotQuad.Side + BotQuad.Side / 2,
                            key.Y * BotQuad.Side + BotQuad.Side / 2,
                            0
                        )
                    );

                    _spots.Add((BotPopulation.Home, middle, $"{BotClaim.Short(guild)} holds this square"));
                }
            }

            return;
        }

        if (_tab == HallTab)
        {
            _pages = 1;
            _page = 0;

            foreach (var hall in BotEstate.Halls)
            {
                if (hall is { Deleted: false })
                {
                    _estate.Add(hall);
                }
            }

            return;
        }

        var people = Population();

        _pages = Math.Max(1, (people.Count + Rows - 1) / Rows);
        _page = Math.Clamp(page, 0, _pages - 1);

        Fill(people, _bots);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, Width, Height, 5054);
        builder.AddAlphaRegion(8, 8, Width - 16, Height - 16);

        builder.AddLabelCropped(14, 12, 1070, 20, Head, "BotAI v2 — dashboard");

        // Tabs. The one being shown is drawn as a label rather than a button, so the window always says which
        // of the two it is.
        Tab(ref builder, 250, "Bots", BotsTab);
        Tab(ref builder, 335, "Market", MarketTab);
        Tab(ref builder, 430, "Needs", NeedsTab);
        Tab(ref builder, 515, "City", CrownTab);
        Tab(ref builder, 590, "Known", KnownTab);
        Tab(ref builder, 675, "Quad", QuadTab);
        Tab(ref builder, 750, "Revel", RevelTab);
        Tab(ref builder, 830, "Halls", HallTab);
        Tab(ref builder, 905, "Claims", ClaimTab);
        Tab(ref builder, 995, "Guilds", GuildTab);
        Tab(ref builder, 1075, "Band", BandTab);

        builder.AddButton(Width - 90, 12, 4014, 4016, 5);
        builder.AddLabelCropped(Width - 60, 12, 46, 20, Ink, "refresh");

        builder.AddImageTiled(14, 36, Width - 28, 1, 9274);

        if (_tab == MarketTab)
        {
            MarketPage(ref builder);
        }
        else if (_tab == CrownTab)
        {
            CrownPage(ref builder);
        }
        else if (_tab == NeedsTab)
        {
            NeedsPage(ref builder);
        }
        else if (_tab == KnownTab)
        {
            KnownPage(ref builder);
        }
        else if (_tab == QuadTab)
        {
            QuadPage(ref builder);
        }
        else if (_tab == RevelTab)
        {
            RevelPage(ref builder);
        }
        else if (_tab == HallTab)
        {
            HallsPage(ref builder);
        }
        else if (_tab == ClaimTab)
        {
            ClaimsPage(ref builder);
        }
        else if (_tab == GuildTab)
        {
            GuildsPage(ref builder);
        }
        else if (_tab == BandTab)
        {
            BandPage(ref builder);
        }
        else
        {
            BotsPage(ref builder);
        }

        Footer(ref builder);
    }

    /// <summary>
    /// The bot's name with its rank behind a comma — "Perri, Apprentice Swordsman" — spelled the way the
    /// engine spells a player's.
    ///
    /// <para>
    /// <b>Here rather than in <c>Mobile.Title</c>, which is where it used to be and where it did harm.</b>
    /// Title means <em>custom</em> title to this engine: <c>AddNameProperties</c> joins it to the name with a
    /// bare space, so a bot in the world read "Perri Apprentice Swordsman" while a player reads "Arold,
    /// Grandmaster Alchemist" — and a non-empty Title makes <c>Titles.ComputeTitle</c> skip the skill-title
    /// branch, which is the only branch that writes the comma. Storing it there both mangled the punctuation
    /// and disabled the code that would have got it right.
    /// </para>
    ///
    /// <para>
    /// So the rank lives on the bot and is spelled out here, in the one frame that wants it, and the world
    /// label is left to the engine.
    /// </para>
    /// </summary>
    private static string Named(BotMobile bot)
    {
        var name = bot.Name ?? "?";
        var rank = bot.BotRank;
        var named = string.IsNullOrEmpty(rank) ? name : $"{name}, {rank}";

        // The two standings the colour carries, spelled out as well, for whoever reads the frame in one hue.
        if (bot.NameHue == BotMobile.ChampionHue)
        {
            named += " — champion";
        }

        if (BotOutlaw.Jailed(bot))
        {
            named += $" — in a cell, {BotOutlaw.LeftInCell(bot)} min";
        }
        else if (BotOutlaw.IsRed(bot))
        {
            named += " — RED";
        }

        // Back from rest on a guild's call to fight. See BotRest.Calls.
        if (!Away(bot) && BotRest.CalledBy(bot) is { } calledBy)
        {
            named += $" — called by {calledBy}";
        }

        return named;
    }

    /// <summary>Out of the world, resting: parked on the internal map. See BotRest and BotPopulation.Park.</summary>
    private static bool Away(BotMobile bot) => bot.Map == null || bot.Map == Map.Internal;

    /// <summary>The hue of a bot's name in the roll: dead, red, champion, or plain.</summary>
    private static int NameHue(BotMobile bot) =>
        !bot.Alive ? Bad
        : BotOutlaw.IsRed(bot) || BotOutlaw.Jailed(bot) ? Red
        : bot.NameHue == BotMobile.ChampionHue ? Gold
        : Ink;

    private void BotsPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 44, 250, 20, Head, "name");
        builder.AddLabelCropped(270, 44, 34, 20, Head, "ai");
        builder.AddLabelCropped(310, 44, 70, 20, Head, "class");
        builder.AddLabelCropped(386, 44, 60, 20, Head, "rung");
        builder.AddLabelCropped(452, 44, 214, 20, Head, "doing");
        // What the bot has proved on the proving ground rather than what the formula says (26.09.2026): the deepest dungeon
        // whose worst creature it has beaten alone. See BotProving.Beats.
        builder.AddLabelCropped(672, 44, 56, 20, Head, "beats");
        builder.AddLabelCropped(734, 44, 46, 20, Head, "mood");
        builder.AddLabelCropped(786, 44, 56, 20, Head, "vector");
        builder.AddLabelCropped(848, 44, 60, 20, Head, "purse");
        builder.AddLabelCropped(914, 44, 68, 20, Head, "bank");
        builder.AddLabelCropped(988, 44, 40, 20, Head, "box");
        builder.AddLabelCropped(1034, 44, 50, 20, Head, "goods");
        builder.AddLabelCropped(1090, 44, 76, 20, Head, "worth");

        for (var i = 0; i < _bots.Count; i++)
        {
            var bot = _bots[i];
            var y = 68 + i * RowHeight;
            var resolve = bot.Resolve;

            // Out of the world, resting: the same columns, all of them grey, and what it is doing is when it comes back.
            if (Away(bot))
            {
                builder.AddLabelCropped(14, y, 250, 20, Offline, $"{Named(bot)} — offline");
                builder.AddLabelCropped(270, y, 34, 20, Offline, bot.Minded ? "(AI)" : "");
                builder.AddLabelCropped(310, y, 70, 20, Offline, bot.Class?.Name ?? "?");
                builder.AddLabelCropped(386, y, 60, 20, Offline, "offline");
                builder.AddLabelCropped(452, y, 214, 20, Offline, BotRest.Offline(bot) ?? "resting");
                builder.AddLabelCropped(672, y, 56, 20, Offline, BotProving.Beats(bot));
                builder.AddLabelCropped(734, y, 46, 20, Offline, $"{bot.Mood:P0}");
                builder.AddLabelCropped(786, y, 56, 20, Offline, $"{bot.Progress:P0}");
                builder.AddLabelCropped(848, y, 60, 20, Offline, $"{Purse(bot)}");
                builder.AddLabelCropped(914, y, 68, 20, Offline, $"{Banker.GetBalance(bot)}");
                builder.AddLabelCropped(988, y, 40, 20, Offline, $"{bot.BankBox?.TotalItems ?? 0}");
                builder.AddLabelCropped(1034, y, 50, 20, Offline, $"{BotShopkeep.Look(bot).Units}");
                builder.AddLabelCropped(1090, y, 50, 20, Offline, $"{BotShopkeep.Look(bot).Worth}");

                continue;
            }

            builder.AddLabelCropped(14, y, 250, 20, NameHue(bot), Named(bot));
            builder.AddLabelCropped(270, y, 34, 20, Ink, bot.Minded ? "(AI)" : "");
            builder.AddLabelCropped(310, y, 70, 20, Ink, bot.Class?.Name ?? "?");
            builder.AddLabelCropped(386, y, 60, 20, Rung(resolve), $"{resolve.Standing}");
            builder.AddLabelCropped(452, y, 214, 20, Ink, Doing(bot));
            builder.AddLabelCropped(672, y, 56, 20, Ink, BotProving.Beats(bot));
            builder.AddLabelCropped(734, y, 46, 20, Shade(bot.Mood, 0.5), $"{bot.Mood:P0}");
            builder.AddLabelCropped(786, y, 56, 20, Shade(bot.Progress, 0.35), $"{bot.Progress:P0}");
            builder.AddLabelCropped(848, y, 60, 20, Ink, $"{Purse(bot)}");
            builder.AddLabelCropped(914, y, 68, 20, Ink, $"{Banker.GetBalance(bot)}");
            builder.AddLabelCropped(988, y, 40, 20, Ink, $"{bot.BankBox?.TotalItems ?? 0}");
            // What it holds to sell, pack surplus and bank box, at its own prices (the auction's stalls until 01.10.2026).
            builder.AddLabelCropped(1034, y, 50, 20, Ink, $"{BotShopkeep.Look(bot).Units}");
            builder.AddLabelCropped(1090, y, 50, 20, Ink, $"{BotShopkeep.Look(bot).Worth}");

            // Straight to whoever is on this row. The one thing an admin always wants next after reading a
            // line like this is to look at the bot it describes.
            builder.AddButton(1146, y, 4005, 4007, 100 + i);
        }

        if (_bots.Count == 0)
        {
            builder.AddLabelCropped(
                14,
                68,
                1152,
                20,
                Bad,
                "No bots. Check bots.population.enabled and bot-population.json"
            );
        }

        var (units, worth) = Shown();

        // Two labels rather than one interpolated line: a string-returning call inside a hole is the one
        // shape that defeats the zero-allocation handler, and the census line is long.
        builder.AddLabelCropped(14, Height - 52, 130, 20, Ink, $"{BotPopulation.Count} bots, {BotPopulation.Living} alive");
        builder.AddLabelCropped(150, Height - 52, 150, 20, Offline, $"{BotPopulation.Away.Count} offline, resting");
        builder.AddLabelCropped(306, Height - 52, 860, 20, Ink, BotWill.Describe());

        builder.AddLabelCropped(
            14,
            Height - 34,
            1010,
            20,
            Ink,
            $"market: {BotShopkeep.OpenCount} shops open by the bank showing {units} things worth {worth}gp, {BotMarket.Asks} wants said"
        );
    }

    /// <summary>
    /// The market's lots: every kind on show at every bot's shop open by the bank, with its keeper's ask. Since the auction went
    /// (01.10.2026) nothing can be bought from here — a sale is hand to hand, at the till — so the row's button takes the admin to
    /// the keeper, where a buyer would have to stand.
    /// </summary>
    private void MarketPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 44, 230, 20, Head, "item");
        builder.AddLabelCropped(250, 44, 64, 20, Head, "amount");
        builder.AddLabelCropped(320, 44, 64, 20, Head, "ask");
        builder.AddLabelCropped(390, 44, 64, 20, Head, "worth");
        builder.AddLabelCropped(460, 44, 200, 20, Head, "keeper");
        builder.AddLabelCropped(670, 44, 160, 20, Head, "town");

        for (var i = 0; i < _stalls.Count; i++)
        {
            var (front, kind, units, ask) = _stalls[i];
            var y = 66 + i * (RowHeight + 8);
            var look = BotWant.Look(kind);

            // The thing itself, not its name. A market you can only read is a spreadsheet.
            builder.AddItem(20, y - 4, look.ItemId, look.Hue);

            builder.AddLabelCropped(64, y, 180, 20, Ink, look.Label);
            builder.AddLabelCropped(250, y, 64, 20, Ink, $"{units}");
            builder.AddLabelCropped(320, y, 64, 20, Ink, $"{ask}");
            builder.AddLabelCropped(390, y, 64, 20, Ink, $"{units * ask}");
            builder.AddLabelCropped(460, y, 200, 20, Ink, front.Keeper?.Name ?? "gone");
            builder.AddLabelCropped(670, y, 160, 20, Ink, front.Town);

            builder.AddButton(838, y, 4005, 4007, 200 + i);
            builder.AddLabelCropped(858, y, 308, 20, Ink, "go to the keeper");
        }

        if (_stalls.Count == 0)
        {
            builder.AddLabelCropped(
                14,
                68,
                1152,
                20,
                Bad,
                "No shop is open by the bank. Makers and gatherers keep shop when they hold what the island asks for"
            );
        }

        builder.AddLabelCropped(14, Height - 34, 1010, 20, Ink, BotMarket.Describe());
    }

    /// <summary>
    /// What the population is short of, at what price, and what carriers are bringing to it.
    ///
    /// <para>
    /// <b>This is the tab that answers "why is nobody mining".</b> The other two say what bots are doing and
    /// what they have made; neither can say what the shard has been unable to get hold of. A want with its
    /// offer four times what it opened at and nothing filled is the clearest sentence this population can
    /// speak: somebody has been trying to buy that for half an hour and nobody here can make it.
    /// </para>
    /// </summary>
    private void NeedsPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 44, 230, 20, Head, "wanted");
        builder.AddLabelCropped(250, 44, 54, 20, Head, "count");
        builder.AddLabelCropped(310, 44, 54, 20, Head, "offer");
        builder.AddLabelCropped(370, 44, 60, 20, Head, "coming");
        builder.AddLabelCropped(436, 44, 54, 20, Head, "filled");
        builder.AddLabelCropped(496, 44, 58, 20, Head, "paid");
        builder.AddLabelCropped(560, 44, 66, 20, Head, "moves");
        builder.AddLabelCropped(686, 44, 480, 20, Head, "buyer");

        for (var i = 0; i < _wants.Count; i++)
        {
            var want = _wants[i];
            var y = 66 + i * (RowHeight + 8);

            builder.AddItem(20, y - 4, want.ItemId, want.Hue);

            builder.AddLabelCropped(64, y, 180, 20, Ink, want.Label);
            builder.AddLabelCropped(250, y, 54, 20, want.IsOpen ? Ink : Bad, $"{want.Amount}");
            builder.AddLabelCropped(310, y, 54, 20, Ink, $"{want.Offer}");
            builder.AddLabelCropped(370, y, 60, 20, want.Coming > 0 ? Good : Ink, $"{want.Coming}");
            builder.AddLabelCropped(436, y, 54, 20, Ink, $"{want.Filled}");
            builder.AddLabelCropped(496, y, 58, 20, Ink, $"{want.Paid}");

            // Raises mean the opposite of what they mean on a stall: a want that keeps going up is one the
            // shard cannot supply, so the colours are the other way round on purpose.
            builder.AddLabelCropped(560, y, 66, 20, want.Raises > want.Cuts ? Bad : Good, $"+{want.Raises}/-{want.Cuts}");
            builder.AddLabelCropped(686, y, 150, 20, Ink, want.Buyer?.Self?.Name ?? "gone");
        }

        if (_wants.Count == 0)
        {
            builder.AddLabelCropped(14, 68, 1152, 20, Bad, "Nobody is short of anything they cannot buy off a shelf");
        }

        builder.AddLabelCropped(14, Height - 34, 1010, 20, Ink, BotMarket.Describe());
    }

    /// <summary>
    /// What the population has found out: where work pays, where blood is spilt, and how much of it was
    /// learned by the three bots that think.
    ///
    /// <para>
    /// <b>Both maps on one page, because they are read together or not at all.</b> "This patch pays forty a
    /// minute" and "this square has killed two people" are the two halves of every decision a bot makes about
    /// where to go, and they were previously visible only as one sentence each at the bottom of two other
    /// tabs. Side by side they answer the question an admin actually has, which is whether the population is
    /// avoiding somewhere for a good reason.
    /// </para>
    ///
    /// <para>
    /// The <c>mind</c> column is the point of the whole page. Three bots on this shard think with a model and
    /// thirty do not, and the design has always been that the three <em>supplement</em> the rest rather than
    /// replace them — so the useful question is how much of what everybody now knows came from them. A column
    /// of noughts would mean three expensive bots are learning only for themselves.
    /// </para>
    /// </summary>
    /// <summary>
    /// The island, worst ground first, with what the Baron is doing about it.
    ///
    /// <para>
    /// Sorted by safety ascending and never by anything else: a list of safe ground is a list nobody acts on.
    /// The rightmost column is the one that matters — whether a square bad enough to want a great hunt has
    /// actually had one sent to it. "Dire and nobody going" is the single reading on this page that means
    /// something is wrong, and it is the reason the column exists rather than being inferred from two others.
    /// </para>
    /// </summary>
    private void QuadPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 100, 140, 20, Head, "quadrant");
        builder.AddLabelCropped(160, 100, 74, 20, Head, "safety");
        builder.AddLabelCropped(240, 100, 144, 20, Head, "standing");
        builder.AddLabelCropped(390, 100, 84, 20, Head, "crossings");
        builder.AddLabelCropped(480, 100, 64, 20, Head, "blows");
        builder.AddLabelCropped(550, 100, 64, 20, Head, "dead");
        builder.AddLabelCropped(620, 100, 546, 20, Head, "the Baron");

        var quads = BotQuad.Worst(Rows - 2);

        for (var i = 0; i < quads.Count; i++)
        {
            var quad = quads[i];
            var y = 122 + i * (RowHeight + 8);
            var middle = quad.Middle;

            builder.AddLabelCropped(14, y, 140, 20, quad.Trodden ? Ink : Bad, $"({middle.X}, {middle.Y})");

            // Red once it is worth going to, green once it is too quiet to bother hunting in, plain between.
            var tint = quad.Safety <= BotQuad.Wanted ? Bad : quad.Safety > BotQuad.TooQuiet ? Good : Ink;

            builder.AddLabelCropped(160, y, 74, 20, tint, $"{quad.Safety:F2}");
            builder.AddLabelCropped(240, y, 140, 20, tint, Standing(quad));
            builder.AddLabelCropped(390, y, 84, 20, Ink, $"{quad.Passes}");
            builder.AddLabelCropped(480, y, 64, 20, quad.Blows > 0 ? Bad : Ink, $"{quad.Blows}");
            builder.AddLabelCropped(550, y, 64, 20, quad.Deaths > 0 ? Bad : Ink, $"{quad.Deaths}");

            var (word, colour) = Baron(quad);

            builder.AddLabelCropped(620, y, 200, 20, colour, word);
        }

        if (quads.Count == 0)
        {
            builder.AddLabelCropped(14, 124, 1152, 20, Bad, "The population has not walked anywhere yet");
        }

        builder.AddLabelCropped(14, Height - 34, 1010, 20, Ink, BotQuad.Describe());
    }

    /// <summary>What sort of ground this is, in the words the rules are written in.</summary>
    private static string Standing(BotQuad.Quad quad)
    {
        if (!quad.Trodden)
        {
            return "never stood in";
        }

        if (quad.Safety <= BotQuad.Dire)
        {
            return "dire";
        }

        if (quad.Safety <= BotQuad.Wanted)
        {
            return "worth hunting";
        }

        if (quad.Safety > BotQuad.TooQuiet)
        {
            return "too quiet to hunt";
        }

        return quad.Swept ? "swept by rangers" : "ordinary";
    }

    /// <summary>
    /// Whether the Baron has gone to this square, is on his way, or has not been sent.
    ///
    /// The last of those is only worth saying about ground bad enough to deserve him: "nobody is going to
    /// that meadow" is true of almost every square on the island and tells nobody anything.
    /// </summary>
    private static (string Word, int Colour) Baron(BotQuad.Quad quad)
    {
        if (BotHarrow.Square != Point3D.Zero && BotQuad.Key(quad.Map, BotHarrow.Square) == (quad.Map?.MapID ?? -1, quad.X, quad.Y))
        {
            return ("marching on it now", Good);
        }

        if (quad.HarrowedTick != 0)
        {
            return ("harrowed already", Good);
        }

        return quad.Safety <= BotQuad.Dire ? ("dire, and nobody going", Bad) : ("—", Ink);
    }

    private void KnownPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 44, 260, 20, Head, "trade");
        builder.AddLabelCropped(280, 44, 84, 20, Head, "pays/min");
        builder.AddLabelCropped(370, 44, 54, 20, Head, "seen");
        builder.AddLabelCropped(430, 44, 84, 20, Head, "mind");

        builder.AddLabelCropped(520, 44, 94, 20, Head, "trade");
        builder.AddLabelCropped(620, 44, 64, 20, Head, "claims");
        builder.AddLabelCropped(690, 44, 64, 20, Head, "pays");
        builder.AddLabelCropped(760, 44, 406, 20, Head, "seen");

        var known = BotCommons.Best(Rows);

        for (var i = 0; i < known.Count; i++)
        {
            var (kind, _, _, perMinute, settled, minded) = known[i];
            var y = 66 + i * (RowHeight + 8);

            builder.AddLabelCropped(14, y, 120, 20, Ink, kind);
            builder.AddLabelCropped(280, y, 84, 20, perMinute > 0 ? Good : Bad, $"{perMinute:F0}");

            // Pale until the board has enough behind it to be believed — see BotCommons.Confidence, which is
            // the same number the arithmetic uses to decide how much of the answer this patch is.
            builder.AddLabelCropped(370, y, 54, 20, settled >= 4 ? Ink : Bad, $"{settled}");
            builder.AddLabelCropped(430, y, 736, 20, minded > 0 ? Good : Ink, $"{minded}");
        }

        if (known.Count == 0)
        {
            builder.AddLabelCropped(14, 68, 1152, 20, Bad, "The population has not found out anything about anywhere yet");
        }

        // <b>What every trade says it is worth against what it turned out to be worth.</b> This is the column
        // that changes what somebody does about the shard rather than what they think of it: a row where the
        // claim is far above the payment is a constant in the source that stopped being true, and the whole
        // population has been chasing it. Sorted by that gap, worst overstatement first, because an
        // overstatement sends everybody at work that pays nothing and an understatement only surprises them.
        var gaps = BotCommons.Gaps(Rows);

        for (var i = 0; i < gaps.Count; i++)
        {
            var (kind, claimed, measured, settled, minded) = gaps[i];
            var y = 66 + i * (RowHeight + 8);

            builder.AddLabelCropped(520, y, 90, 20, minded > 0 ? Good : Ink, kind);
            builder.AddLabelCropped(620, y, 64, 20, Ink, $"{claimed:F0}");

            // Red when the claim is more than half again what the work pays: that is a number worth going and
            // looking at, and anything less is the ordinary noise of a shard that changes.
            builder.AddLabelCropped(690, y, 64, 20, measured * 1.5 < claimed ? Bad : Good, $"{measured:F0}");
            builder.AddLabelCropped(760, y, 406, 20, settled >= 25 ? Ink : Bad, $"{settled}");
        }

        if (gaps.Count == 0)
        {
            builder.AddLabelCropped(520, 68, 646, 20, Bad, "No trade has been measured against its own claim yet");
        }

        builder.AddLabelCropped(14, Height - 34, 1010, 20, Ink, BotCommons.Describe());
    }

    private void Footer(ref DynamicGumpBuilder builder)
    {
        builder.AddImageTiled(14, Height - 62, Width - 28, 1, 9274);

        if (_page > 0)
        {
            builder.AddButton(Width - 150, Height - 34, 4014, 4016, 3);
        }

        builder.AddLabelCropped(Width - 120, Height - 34, 80, 20, Ink, $"page {_page + 1} of {_pages}");

        if (_page + 1 < _pages)
        {
            builder.AddButton(Width - 34, Height - 34, 4005, 4007, 4);
        }
    }

    /// <summary>How many lots one press of the city's button buys at the bots' shops by the bank.</summary>
    private const int CrownLots = 10;

    /// <summary>
    /// The city, which is to say the only demand on this shard that does not come out of a monster's purse.
    ///
    /// <para>
    /// One button, and it does exactly what the label says: buys ten lots at the bots' shops open by the bank at whatever the
    /// keepers are asking (the stalls, until the auction went on 01.10.2026), and pays out of the treasury's minted coin. That is the point of it. Every
    /// coin here otherwise enters through a corpse, so the population's market is sixteen bots passing the
    /// same purse round while their stalls fill with goods none of them wants; an outside buyer is what turns
    /// production into income. It also teaches prices, because the purchase is booked exactly as a bot's is.
    /// </para>
    ///
    /// <para>
    /// It stays a button rather than becoming a timer on purpose: printed money is a decision somebody should
    /// have to make, and be able to stop making, one press at a time.
    /// </para>
    /// </summary>
    private void CrownPage(ref DynamicGumpBuilder builder)
    {
        var (units, worth) = Shown();

        builder.AddLabelCropped(14, 44, 1152, 20, Head, "the city buys at the market");

        builder.AddLabelCropped(
            14,
            76,
            1152,
            20,
            Ink,
            $"The market has {BotShopkeep.OpenCount} shops open by the bank: {units} things their keepers are asking {worth}gp for."
        );

        builder.AddLabelCropped(
            14,
            100,
            1152,
            20,
            Ink,
            $"Buying takes {CrownLots} lots at the open shops and pays the keepers what they ask, in coin into their packs."
        );

        builder.AddLabelCropped(
            14,
            124,
            1152,
            20,
            Bad,
            $"Paid from the treasury: {BotCity.Purse}gp of {BotCity.Cap}, minted at {BotCity.MintPerHour}gp an hour. Argus has the same lever."
        );

        builder.AddButton(14, 160, 4005, 4007, 8);
        builder.AddLabelCropped(52, 160, 1114, 20, Head, $"buy {CrownLots} lots at the shops by the bank");

        builder.AddLabelCropped(14, 200, Width - 28, 20, Ink, $"standing orders: {BotCity.Wants()}");
        builder.AddLabelCropped(
            14,
            224,
            1152,
            20,
            Ink,
            $"so far: {BotShopkeep.Sales} sales at the shops for {BotShopkeep.Takings}gp and {BotHandover.Exchanges} carriers' meetings for {BotHandover.Gold}gp; the city spent {BotCity.Spent}gp"
        );
    }

    /// <summary>The button's work, said out loud to whoever pressed it and written down for everyone else.</summary>
    private static void Sent(Mobile from)
    {
        var (lots, units, paid) = BotCity.Buy(CrownLots, from.Name);

        if (lots <= 0)
        {
            from.SendMessage(BotCity.Purse <= 0 ? "The treasury is empty." : "The city sent for goods and found nothing on offer it could pay for.");

            return;
        }

        from.SendMessage($"The city bought {units} things in {lots} lots at the shops for {paid}gp.");

        logger.Information(
            "{Who} had the city buy {Units} things in {Lots} lots at the bots' shops for {Paid}gp",
            from.Name,
            units,
            lots,
            paid
        );
    }

    /// <summary>
    /// The wanted board: every bandit at large, dearest head first, with what is proved against it. A bot in a cell is
    /// not here — it is caught, and the list is of the ones that are not.
    /// </summary>
    private void BandPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 100, 140, 20, Head, "bandit");
        builder.AddLabelCropped(160, 100, 94, 20, Head, "trade");
        builder.AddLabelCropped(260, 100, 154, 20, Head, "standing");
        builder.AddLabelCropped(420, 100, 84, 20, Head, "murders");
        builder.AddLabelCropped(510, 100, 74, 20, Head, "unseen");
        builder.AddLabelCropped(590, 100, 94, 20, Head, "robberies");
        builder.AddLabelCropped(690, 100, 94, 20, Head, "demands");
        builder.AddLabelCropped(790, 100, 94, 20, Head, "price");
        builder.AddLabelCropped(890, 100, 139, 20, Head, "where");
        builder.AddLabelCropped(1035, 100, 131, 20, Head, "go");

        var roll = BotUnderworld.AtLarge();

        for (var i = 0; i < roll.Count && i < Rows - 2; i++)
        {
            var one = roll[i];
            var y = 122 + i * (RowHeight + 8);

            builder.AddLabelCropped(14, y, 140, 20, one.Red ? Red : one.Fence ? Gold : Ink, one.Name);
            builder.AddLabelCropped(160, y, 95, 20, Ink, one.Trade);
            builder.AddLabelCropped(260, y, 155, 20, one.Known ? Bad : Ink, Standing(one));
            builder.AddLabelCropped(420, y, 84, 20, one.Murders > 0 ? Bad : Ink, $"{one.Murders}");
            builder.AddLabelCropped(510, y, 74, 20, Ink, $"{one.Unseen}");
            builder.AddLabelCropped(590, y, 94, 20, Ink, $"{one.Robberies}");
            builder.AddLabelCropped(690, y, 94, 20, Ink, $"{one.Extortions}");
            builder.AddLabelCropped(790, y, 94, 20, one.Price > 0 ? Gold : Ink, $"{one.Price}gp");
            builder.AddLabelCropped(890, y, 139, 20, Ink, $"({one.Where.X}, {one.Where.Y})");
            builder.AddButton(1035, y, 4005, 4007, 500 + i);
        }

        if (roll.Count == 0)
        {
            builder.AddLabelCropped(14, 124, 1152, 20, Good, "No bandit is at large on the island");
        }

        builder.AddButton(14, Height - 66, 4005, 4007, 18);
        builder.AddLabelCropped(
            46,
            Height - 66,
            1120,
            20,
            BotUnderworld.Exists && BotUnderworld.Hideout != Point3D.Zero ? Gold : Ink,
            "stand in the band's hideout"
        );

        builder.AddLabelCropped(14, Height - 34, 1010, 20, Ink, BotUnderworld.Describe());
    }

    /// <summary>What this one is, in the fewest words that tell a hunter what it is walking into.</summary>
    private static string Standing(BotUnderworld.Wanted one)
    {
        if (one.Fence)
        {
            return one.Hunted ? "the band's fence, wanted" : "the band's fence";
        }

        if (one.Member)
        {
            return one.Red ? "of The Shadow, red" : one.Hunted ? "of The Shadow, wanted" : "of The Shadow";
        }

        return one.Red ? "red" : one.Hunted ? "wanted" : "at large";
    }

    private void Tab(ref DynamicGumpBuilder builder, int x, string name, int tab)
    {
        if (_tab == tab)
        {
            builder.AddLabel(x + 20, 12, Head, name);

            return;
        }

        // <b>Every tab needs a number here and a case below, and the fallback hides a missing one.</b> The
        // arm reads "anything else is the Bots tab", so a tab added without its number does not fail — it
        // quietly becomes a second button for Bots, which is exactly what happened to Known and is
        // indistinguishable from a tab that will not open.
        builder.AddButton(
            x,
            12,
            4005,
            4007,
            tab switch
            {
                MarketTab => 2,
                NeedsTab  => 6,
                CrownTab  => 7,
                KnownTab  => 9,
                QuadTab   => 10,
                RevelTab  => 11,
                HallTab   => 14,
                ClaimTab  => 15,
                GuildTab  => 16,
                BandTab   => 17,
                _         => 1
            }
        );
        builder.AddLabel(x + 20, 12, Ink, name);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender?.Mobile;

        // Checked again here, and not only in the command: a gump response is a packet, and a packet can be
        // sent by anybody who has ever seen this window.
        if (from == null || from.AccessLevel < AccessLevel.Administrator)
        {
            return;
        }

        var button = info.ButtonID;

        switch (button)
        {
            case 0:
                return;

            case 1:
                DisplayTo(from, BotsTab);

                return;

            case 2:
                DisplayTo(from, MarketTab);

                return;

            case 3:
                DisplayTo(from, _tab, _page - 1);

                return;

            case 4:
                DisplayTo(from, _tab, _page + 1);

                return;

            case 5:
                DisplayTo(from, _tab, _page);

                return;

            case 6:
                DisplayTo(from, NeedsTab);

                return;

            case 7:
                DisplayTo(from, CrownTab);

                return;

            case 8:
                Sent(from);

                DisplayTo(from, CrownTab);

                return;

            case 9:
                DisplayTo(from, KnownTab);

                return;

            case 10:
                DisplayTo(from, QuadTab);

                return;

            case 11:
                DisplayTo(from, RevelTab);

                return;

            case 12:
                Stand(from, false);

                return;

            case 13:
                Stand(from, true);

                return;

            case >= 60 and < 66:
                Beside(from, button - 60);

                return;

            case 14:
                DisplayTo(from, HallTab);

                return;

            case 15:
                DisplayTo(from, ClaimTab);

                return;

            case 16:
                DisplayTo(from, GuildTab);

                return;

            case 17:
                DisplayTo(from, BandTab);

                return;

            case 18:
                Lair(from);

                return;
        }

        // Before the two below it, and the order is the whole of it being right: every one of these ranges
        // is open-ended, so the highest has to be asked first or it is answered by the one underneath.
        if (button >= 500)
        {
            Tail(from, button - 500);

            return;
        }

        if (button >= 400)
        {
            Ground(from, button - 400);

            return;
        }

        if (button >= 300)
        {
            Doorstep(from, button - 300);

            return;
        }

        if (button >= 200)
        {
            Bought(from, button - 200);

            return;
        }

        if (button >= 100)
        {
            Visit(from, button - 100);
        }
    }

    /// <summary>
    /// The halls: one row each, and under them the guilds that have not built yet and why.
    /// </summary>
    /// <summary>
    /// The squares guilds have taken, and the claims running now.
    ///
    /// <para>
    /// Two lists rather than one, because they answer different questions and one of them is urgent. What is
    /// held is the state of the island; what is being claimed is something a person may want to walk over and
    /// watch, and it disappears within five minutes of appearing.
    /// </para>
    /// </summary>
    private void ClaimsPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 44, 1152, 20, Head, "being claimed now");
        builder.AddLabelCropped(14, 68, 140, 20, Head, "square");
        builder.AddLabelCropped(160, 68, 134, 20, Head, "by");
        builder.AddLabelCropped(300, 68, 124, 20, Head, "for");
        builder.AddLabelCropped(430, 68, 104, 20, Head, "gathered");
        builder.AddLabelCropped(540, 68, 104, 20, Head, "blood");
        builder.AddLabelCropped(650, 68, 104, 20, Head, "left");
        builder.AddLabelCropped(760, 68, 406, 20, Head, "taken from");

        var running = 0;

        foreach (var bid in BotClaim.Bids)
        {
            if (running >= 5)
            {
                break;
            }

            var y = 92 + running++ * RowHeight;
            var left = BotClaim.Left(bid) / 1000;

            builder.AddLabelCropped(14, y, 140, 20, Ink, $"{bid.Middle.X}, {bid.Middle.Y}");
            builder.AddLabelCropped(160, y, 130, 20, Head, BotClaim.Short(bid.Guild) ?? "nobody");
            builder.AddLabelCropped(
                300,
                y,
                124,
                20,
                Ink,
                bid.Want switch
                {
                    BotClaim.Kind.Oust  => "to take it",
                    BotClaim.Kind.Strip => "to strike a name off",
                    _                   => "to settle it"
                }
            );

            // Green once the gathering is enough, because that is the one number the claimant can still do
            // anything about while the clock runs.
            builder.AddLabelCropped(
                430,
                y,
                104,
                20,
                bid.Peak >= BotClaim.Gather ? Good : Bad,
                $"{bid.Peak} of {BotClaim.Gather}"
            );

            builder.AddLabelCropped(
                540,
                y,
                104,
                20,
                bid.Fallen > bid.Felled ? Bad : Ink,
                bid.Felled + bid.Fallen == 0 ? "none" : $"{bid.Felled} for, {bid.Fallen} against"
            );

            builder.AddLabelCropped(650, y, 104, 20, Ink, $"{left}s");
            builder.AddLabelCropped(760, y, 140, 20, Ink, BotClaim.Short(bid.From) ?? "nobody");

            if (running - 1 < _spots.Count)
            {
                builder.AddButton(940, y, 4005, 4007, 400 + running - 1);
                builder.AddLabelCropped(970, y, 196, 20, Ink, "go");
            }
        }

        if (running == 0)
        {
            builder.AddLabelCropped(14, 92, 1152, 20, Ink, "Nobody is claiming anything at the moment.");
        }

        var top = 92 + Math.Max(1, running) * RowHeight + 18;

        builder.AddImageTiled(14, top - 10, Width - 28, 1, 9274);
        builder.AddLabelCropped(14, top, 140, 20, Head, "held");
        builder.AddLabelCropped(160, top, 134, 20, Head, "square");
        builder.AddLabelCropped(300, top, 214, 20, Head, "how it was taken");
        builder.AddLabelCropped(520, top, 646, 20, Head, "safety");

        var held = 0;

        foreach (var (key, guild, bought) in BotClaim.Owned())
        {
            if (held >= Rows - 2)
            {
                break;
            }

            var y = top + 26 + held++ * RowHeight;
            var middle = new Point3D(
                key.X * BotQuad.Side + BotQuad.Side / 2,
                key.Y * BotQuad.Side + BotQuad.Side / 2,
                0
            );

            builder.AddLabelCropped(14, y, 130, 20, Head, BotClaim.Short(guild) ?? "nobody");
            builder.AddLabelCropped(160, y, 134, 20, Ink, $"{middle.X}, {middle.Y}");
            builder.AddLabelCropped(
                300,
                y,
                214,
                20,
                Ink,
                bought ? $"bought for {BotClaim.Price}gp or taken" : "a free claim"
            );

            var quad = BotQuad.Known(BotPopulation.Home, middle);

            builder.AddLabelCropped(520, y, 114, 20, Ink, quad == null ? "not walked" : $"{BotQuad.Reading(quad):F1}");

            // Numbered on from the claims above, because both lists share one snapshot: the row a person
            // clicks is the place that row named when the window was drawn.
            var spot = 5 + held - 1;

            if (spot < _spots.Count)
            {
                builder.AddButton(640, y, 4005, 4007, 400 + spot);
                builder.AddLabelCropped(670, y, 496, 20, Ink, "go");
            }
        }

        if (held == 0)
        {
            builder.AddLabelCropped(14, top + 26, 1152, 20, Ink, "No square of the island belongs to anybody yet.");
        }
    }

    /// <summary>
    /// The guilds: their size, what they are working towards, what they are all doing, and their quarrels.
    /// </summary>
    private void GuildsPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 60, 130, 20, Head, "guild");
        builder.AddLabelCropped(150, 60, 74, 20, Head, "of them");
        builder.AddLabelCropped(230, 60, 84, 20, Head, "purse");
        builder.AddLabelCropped(320, 60, 74, 20, Head, "ground");
        builder.AddLabelCropped(400, 60, 234, 20, Head, "working towards");
        builder.AddLabelCropped(640, 60, 254, 20, Head, "all doing now");
        builder.AddLabelCropped(900, 60, 134, 20, Head, "at war with");
        builder.AddLabelCropped(1040, 60, 126, 20, Head, "allied to");

        var row = 0;

        foreach (var guild in BotGuilds.Standing)
        {
            if (guild == null || row >= Rows)
            {
                continue;
            }

            var y = 86 + row++ * (RowHeight + 4);
            var wars = Names(guild.Enemies);
            var allies = Names(guild.Allies);

            builder.AddLabelCropped(14, y, 130, 20, Head, BotClaim.Short(guild.Name) ?? guild.Name);
            builder.AddLabelCropped(150, y, 74, 20, Ink, $"{guild.Members?.Count ?? 0}");
            builder.AddLabelCropped(230, y, 84, 20, Ink, $"{BotEstate.Fund(guild)}gp");
            builder.AddLabelCropped(320, y, 74, 20, Ink, $"{BotClaim.Holds(guild.Name)}");
            builder.AddLabelCropped(400, y, 230, 20, Ink, BotGuilds.Aim(guild));
            builder.AddLabelCropped(640, y, 250, 20, Ink, BotGuilds.Task(guild));
            builder.AddLabelCropped(900, y, 130, 20, wars == null ? Ink : Bad, wars ?? "nobody");
            builder.AddLabelCropped(1040, y, 126, 20, allies == null ? Ink : Good, allies ?? "nobody");
        }

        if (row == 0)
        {
            builder.AddLabelCropped(14, 86, 1152, 20, Bad, "No guild has been mustered.");
        }

        var foot = 86 + Math.Max(1, row) * (RowHeight + 4) + 18;

        builder.AddImageTiled(14, foot - 10, Width - 28, 1, 9274);

        // <b>Said out loud because an empty column is not an answer.</b> Nothing on this shard forms an
        // alliance yet: the engine keeps the list and no bot ever adds to it, so "allied to nobody" is a
        // fact about the code rather than about the guilds, and a reader deserves to know which.
        builder.AddLabelCropped(
            14,
            foot,
            1152,
            20,
            Ink,
            "Alliances are the engine's own list; nothing on this shard yet asks a guild to make one."
        );
    }

    /// <summary>Guild names as one short phrase, or nothing at all when the list is empty.</summary>
    private static string Names(List<Guild> guilds)
    {
        if (guilds == null || guilds.Count == 0)
        {
            return null;
        }

        var say = "";

        for (var i = 0; i < guilds.Count; i++)
        {
            if (guilds[i] == null)
            {
                continue;
            }

            say += say.Length == 0 ? BotClaim.Short(guilds[i].Name) : ", " + BotClaim.Short(guilds[i].Name);
        }

        return say.Length == 0 ? null : say;
    }

    private void HallsPage(ref DynamicGumpBuilder builder)
    {
        builder.AddLabelCropped(14, 60, 140, 20, Head, "guild");
        builder.AddLabelCropped(160, 60, 114, 20, Head, "where");
        builder.AddLabelCropped(280, 60, 154, 20, Head, "owner");
        builder.AddLabelCropped(440, 60, 114, 20, Head, "of the guild");
        builder.AddLabelCropped(560, 60, 134, 20, Head, "inside");
        builder.AddLabelCropped(700, 60, 154, 20, Head, "condition");
        builder.AddLabelCropped(860, 60, 94, 20, Head, "paid");
        builder.AddLabelCropped(960, 60, 206, 20, Head, "go");

        for (var i = 0; i < _estate.Count && i < Rows; i++)
        {
            var hall = _estate[i];
            var y = 86 + i * (RowHeight + 4);

            builder.AddLabelCropped(14, y, 140, 20, Head, hall.Sign?.Name ?? "unnamed");
            builder.AddLabelCropped(160, y, 114, 20, Ink, $"{hall.X}, {hall.Y}");
            builder.AddLabelCropped(280, y, 150, 20, Ink, hall.Owner?.Name ?? "nobody");
            builder.AddLabelCropped(440, y, 114, 20, Ink, $"{(hall.CoOwners?.Count ?? 0) + 1}");
            builder.AddLabelCropped(560, y, 134, 20, Ink, $"{Furnishings(hall)} things");
            builder.AddLabelCropped(700, y, 150, 20, Ink, Condition(hall));
            builder.AddLabelCropped(860, y, 94, 20, Ink, $"{hall.Price}gp");

            builder.AddButton(960, y, 4005, 4007, 300 + i);
        }

        if (_estate.Count == 0)
        {
            builder.AddLabelCropped(14, 86, 1152, 20, Bad, "Nothing has been built on this island yet.");
        }

        // The other half of the answer, and the half worth reading when the first is empty: what each guild
        // without a hall has raised, against what one costs.
        var y2 = 86 + Math.Max(1, _estate.Count) * (RowHeight + 4) + 20;

        builder.AddImageTiled(14, y2 - 12, Width - 28, 1, 9274);
        builder.AddLabelCropped(14, y2, 140, 20, Head, "still saving");

        var waiting = 0;

        foreach (var guild in BotGuilds.Standing)
        {
            if (BotEstate.Hall(guild) != null)
            {
                continue;
            }

            var fund = BotEstate.Fund(guild);
            var row = y2 + 26 + waiting++ * 24;

            builder.AddLabelCropped(14, row, 140, 20, Ink, guild.Name);

            if (fund >= BotEstate.Price)
            {
                builder.AddLabelCropped(
                    160,
                    row,
                    1006,
                    20,
                    Good,
                    $"has {fund}gp of {BotEstate.Price} — it is the ground it is waiting for"
                );
            }
            else
            {
                builder.AddLabelCropped(
                    160,
                    row,
                    1006,
                    20,
                    Ink,
                    $"has {fund}gp of {BotEstate.Price}, {BotEstate.Price - fund}gp short"
                );
            }
        }

        if (waiting == 0)
        {
            builder.AddLabelCropped(160, y2, 1006, 20, Good, "every guild has one");
        }

        builder.AddLabelCropped(14, Height - 34, 1010, 20, Ink, BotPlot.Describe());
    }

    /// <summary>How many things stand in a hall: its fittings and whatever is locked down in it.</summary>
    private static int Furnishings(BaseHouse hall) => (hall.Addons?.Count ?? 0) + (hall.LockDowns?.Count ?? 0);

    /// <summary>
    /// The engine's own word for how a house is wearing, which is the one number about a hall that can go
    /// wrong quietly: an unowned house in this era decays, and nothing else on this shard would say so.
    /// </summary>
    private static string Condition(BaseHouse hall) => hall.DecayLevel.ToString();

    /// <summary>Puts whoever pressed the button outside a hall's door.</summary>
    /// <summary>
    /// Stands the watcher in the middle of a claimed or contested square.
    ///
    /// The middle of the square rather than anywhere near a bot: the gathering is what is worth looking at,
    /// and a claim that nobody turned up for is exactly the thing a person opens this tab to see.
    /// </summary>
    private void Ground(Mobile from, int row)
    {
        if (row < 0 || row >= _spots.Count)
        {
            return;
        }

        var (map, where, what) = _spots[row];

        if (map == null || map == Map.Internal || where == Point3D.Zero)
        {
            from.SendMessage("There is nothing on that row any more.");

            DisplayTo(from, ClaimTab);

            return;
        }

        from.MoveToWorld(where, map);
        from.SendMessage(what ?? "The square.");

        DisplayTo(from, ClaimTab);
    }

    private void Doorstep(Mobile from, int row)
    {
        if (row < 0 || row >= _estate.Count)
        {
            return;
        }

        var hall = _estate[row];

        if (hall.Deleted || hall.Map == null || hall.Map == Map.Internal)
        {
            from.SendMessage("That hall is not standing any more.");

            DisplayTo(from, HallTab);

            return;
        }

        // The ban location rather than the middle of the house: it is the engine's own "outside the front
        // door" for every multi it knows, and arriving inside somebody's wall is not an entrance.
        from.MoveToWorld(hall.BanLocation, hall.Map);
        from.SendMessage($"The hall of {hall.Sign?.Name ?? "somebody"}, {Furnishings(hall)} things inside it.");

        DisplayTo(from, HallTab);
    }

    /// <summary>
    /// The revel: what is on, what it is for, and where to stand to watch it.
    /// </summary>
    private void RevelPage(ref DynamicGumpBuilder builder)
    {
        var notice = BotCrier.Read();

        if (notice == null)
        {
            // Nothing is watching, which is a different thing from nothing being declared, and the tab says
            // which of the two it is rather than showing an empty table that could mean either.
            builder.AddLabelCropped(
                14,
                60,
                1152,
                20,
                Bad,
                "No watcher is running on this shard, so nothing is being declared."
            );
            builder.AddLabelCropped(
                14,
                84,
                1152,
                20,
                Ink,
                "Revels come from the minds assembly. Without it this tab has nothing to show."
            );

            return;
        }

        builder.AddLabelCropped(14, 60, 1152, 20, Head, "what is on");

        if (notice.Running)
        {
            builder.AddLabelCropped(
                14,
                86,
                1152,
                20,
                Good,
                $"{notice.Kind} is worth x{notice.Bonus:F1} for another {Clock(notice.EndsIn)}, prize {notice.Prize}gp"
            );

            builder.AddLabelCropped(14, 112, 100, 20, Head, "it said");
            builder.AddHtml(120, 112, Width - 150, 40, notice.Said ?? "nothing at all");

            builder.AddLabelCropped(14, 158, 100, 20, Head, "because");
            builder.AddHtml(120, 158, Width - 150, 60, notice.Why ?? "no reason was given");

            if (notice.Entered == 0)
            {
                builder.AddLabelCropped(14, 226, 1152, 20, Bad, "nobody has taken it up yet");
            }
            else
            {
                builder.AddLabelCropped(
                    14,
                    226,
                    1152,
                    20,
                    Ink,
                    $"{notice.Entered} have taken it up; {notice.Leading} leads with {notice.LeadingDid}"
                );
            }

            if (notice.Wave > 0)
            {
                builder.AddLabelCropped(
                    14,
                    250,
                    1152,
                    20,
                    Good,
                    $"wave {notice.Wave} is standing, {notice.Standing} of it alive"
                );
            }
        }
        else
        {
            builder.AddLabelCropped(
                14,
                86,
                1152,
                20,
                Ink,
                $"Nothing is on. The watcher may declare again in {Clock(notice.NextIn)}."
            );

            if (notice.Past is { Length: > 0 })
            {
                builder.AddLabelCropped(14, 112, 1152, 20, Ink, $"the last one — {notice.Past[0]}");
            }
            else
            {
                builder.AddLabelCropped(14, 112, 1152, 20, Ink, "nothing has been declared yet");
            }
        }

        // Where to stand. Two buttons rather than one: the thing itself may be an hour's walk from the
        // watcher who declared it, and on a quiet shard the more useful of the two is whichever is nearer
        // to something happening.
        var where = notice.Map != null && notice.Where != Point3D.Zero;

        builder.AddImageTiled(14, 262, Width - 28, 1, 9274);

        if (where)
        {
            builder.AddButton(14, 276, 4005, 4007, 12);

            if (notice.Camp)
            {
                builder.AddLabelCropped(52, 276, 1114, 20, Head, $"go to the camp at ({notice.Where.X}, {notice.Where.Y})");
            }
            else
            {
                builder.AddLabelCropped(
                    52,
                    276,
                    1114,
                    20,
                    Head,
                    $"go to where it was called, ({notice.Where.X}, {notice.Where.Y})"
                );
            }
        }
        else
        {
            builder.AddLabelCropped(14, 276, 1152, 20, Ink, "nowhere in particular: this one is a price, not a place");
        }

        if (notice.WatcherMap != null && notice.Watcher != Point3D.Zero)
        {
            builder.AddButton(14, 306, 4005, 4007, 13);
            builder.AddLabelCropped(
                52,
                306,
                1114,
                20,
                Head,
                $"go to {notice.WatcherName ?? "the watcher"}, standing at ({notice.Watcher.X}, {notice.Watcher.Y})"
            );
        }

        builder.AddImageTiled(14, 340, Width - 28, 1, 9274);

        builder.AddLabelCropped(14, 354, 100, 20, Head, "the crown");
        builder.AddLabelCropped(
            120,
            354,
            1046,
            20,
            Ink,
            $"{notice.Declared} declared, {notice.Won} won, {notice.Ignored} ignored, {notice.Bands} taken by a whole guild; {notice.Paid}gp paid out, {notice.Collected}gp taken in tax, {notice.Purse}gp in the purse"
        );

        builder.AddLabelCropped(14, 384, Width - 28, 20, Ink, notice.Waves ?? "no waves have been called");

        // What the watcher's own events have actually come to, trade by trade. A kind held three times that
        // drew nobody is the row worth seeing, and it is the row the watcher is now shown before it chooses.
        builder.AddLabelCropped(14, 404, Width - 28, 20, Ink, notice.Ledger ?? "none have been held yet");

        builder.AddLabelCropped(14, 434, 100, 20, Head, "before this");

        var past = notice.Past ?? [];

        for (var i = 0; i < past.Length && i < 5; i++)
        {
            builder.AddLabelCropped(120, 434 + i * 24, 540, 20, Ink, past[i]);
        }

        // The squad of watchers, one button each. Patrick's order of 13.09.2026: the helpers reachable the
        // way the watcher is. Read live rather than from the notice, because a watcher moves every twenty
        // seconds and the notice is written once a revel.
        var squad = BotCrier.Squad();

        builder.AddLabelCropped(680, 434, 486, 20, Head, "the watchers");

        if (squad.Count == 0)
        {
            builder.AddLabelCropped(680, 458, 486, 20, Ink, "nobody is watching");
        }

        for (var i = 0; i < squad.Count && i < 4; i++)
        {
            var (name, _, at) = squad[i];

            builder.AddButton(680, 458 + i * 24, 4005, 4007, 60 + i);
            builder.AddLabelCropped(718, 458 + i * 24, Width - 740, 20, Ink, $"go to {name}, standing at ({at.X}, {at.Y})");
        }

        if (past.Length == 0)
        {
            builder.AddLabelCropped(120, 434, 554, 20, Ink, "nothing has ended yet");
        }
    }

    /// <summary>Milliseconds as a clock a person reads, because 431000 is not a length of time to anybody.</summary>
    private static string Clock(long ms)
    {
        var seconds = Math.Max(0, ms / 1000);

        return $"{seconds / 60}m {seconds % 60:D2}s";
    }

    /// <summary>
    /// Puts whoever pressed the button where the revel is, or beside the watcher who declared it.
    ///
    /// The notice is read again rather than remembered from when the window was drawn: a revel ends on its
    /// own clock, and a button that teleports an administrator to where a camp used to be is worse than a
    /// button that says the camp is gone.
    /// </summary>
    /// <summary>Puts the administrator beside the nth watcher of the squad. See BotCrier.Squad.</summary>
    private void Beside(Mobile from, int which)
    {
        var squad = BotCrier.Squad();

        if (which < 0 || which >= squad.Count)
        {
            from.SendMessage("That watcher is nowhere to be found.");

            DisplayTo(from, RevelTab);

            return;
        }

        var (name, map, at) = squad[which];

        if (map == null || map == Map.Internal || at == Point3D.Zero)
        {
            from.SendMessage($"{name} has no body at the moment.");

            DisplayTo(from, RevelTab);

            return;
        }

        from.MoveToWorld(at, map);
        from.SendMessage($"{name} is standing here.");

        DisplayTo(from, RevelTab);
    }

    private void Stand(Mobile from, bool watcher)
    {
        var notice = BotCrier.Read();

        var map = watcher ? notice?.WatcherMap : notice?.Map;
        var at = watcher ? notice?.Watcher ?? Point3D.Zero : notice?.Where ?? Point3D.Zero;

        if (map == null || map == Map.Internal || at == Point3D.Zero)
        {
            from.SendMessage(watcher ? "The watcher is nowhere to be found." : "That revel has no place any more.");

            DisplayTo(from, RevelTab);

            return;
        }

        from.MoveToWorld(at, map);

        if (watcher)
        {
            from.SendMessage($"{notice.WatcherName ?? "The watcher"} is standing here.");
        }
        else
        {
            from.SendMessage($"This is where {notice.Kind ?? "the last revel"} was called.");
        }

        DisplayTo(from, RevelTab);
    }

    /// <summary>Stands the caller where The Shadow hides. Patrick's ask of 18.09.2026 for this tab.</summary>
    private void Lair(Mobile from)
    {
        var at = BotUnderworld.Hideout;
        var map = BotLair.Fire?.Map ?? BotLair.Chest?.Map ?? from.Map;

        if (!BotUnderworld.Exists || at == Point3D.Zero || map == null || map == Map.Internal)
        {
            from.SendMessage("The Shadow has no hideout at the moment.");

            DisplayTo(from, BandTab);

            return;
        }

        from.MoveToWorld(at, map);
        from.SendMessage($"The Shadow's hideout: {BotLair.Describe()}");

        DisplayTo(from, BandTab);
    }

    /// <summary>Stands the caller beside one bandit on the roll.</summary>
    private void Tail(Mobile from, int row)
    {
        var roll = BotUnderworld.AtLarge();

        if (row < 0 || row >= roll.Count)
        {
            DisplayTo(from, BandTab);

            return;
        }

        var one = roll[row];

        if (one.Map == null || one.Map == Map.Internal || one.Where == Point3D.Zero)
        {
            from.SendMessage($"{one.Name} is nowhere to be found.");

            DisplayTo(from, BandTab);

            return;
        }

        from.MoveToWorld(one.Where, one.Map);
        from.SendMessage($"{one.Name}: {Standing(one)}, {one.Murders} murders, {one.Price}gp on its head.");

        DisplayTo(from, BandTab);
    }

    private void Visit(Mobile from, int row)
    {
        if (row < 0 || row >= _bots.Count)
        {
            return;
        }

        var bot = _bots[row];

        if (bot.Deleted)
        {
            from.SendMessage("That bot is not in the world any more.");

            return;
        }

        if (Away(bot))
        {
            from.SendMessage($"{bot.Name} the {bot.Class?.Name} is offline, {BotRest.Offline(bot) ?? "resting"}.");

            DisplayTo(from, _tab, _page);

            return;
        }

        from.MoveToWorld(bot.Location, bot.Map);
        from.SendMessage($"{bot.Name} the {bot.Class?.Name}: {Doing(bot)}");

        DisplayTo(from, _tab, _page);
    }

    /// <summary>
    /// To the keeper of a lot on the Market tab. The admin's "buy" button went with the auction (01.10.2026): a purchase is hand to
    /// hand at the till, and this goes to where the till is.
    /// </summary>
    private void Bought(Mobile from, int row)
    {
        if (row < 0 || row >= _stalls.Count)
        {
            return;
        }

        var (front, kind, units, ask) = _stalls[row];
        var keeper = front.Keeper;

        if (keeper is not { Deleted: false } || !BotShopkeep.Live(front))
        {
            from.SendMessage("That shop has shut.");
        }
        else
        {
            from.MoveToWorld(keeper.Location, keeper.Map);
            from.SendMessage($"{keeper.Name}'s shop in {front.Town}: {units} {kind.Name} at {ask}gp.");
        }

        DisplayTo(from, _tab, _page);
    }

    /// <summary>What the open shops show between them, in things and at their asks.</summary>
    private static (int Units, int Worth) Shown()
    {
        var units = 0;
        var worth = 0;
        var open = BotShopkeep.Open;

        for (var i = 0; i < open.Count; i++)
        {
            if (!open[i].Shut)
            {
                units += open[i].Units;
                worth += open[i].Worth;
            }
        }

        return (units, worth);
    }

    /// <summary>
    /// The only way in. Everything it checks is checked before the window exists, so the window is never
    /// sent empty and never sent to somebody who cannot use it.
    /// </summary>
    public static void DisplayTo(Mobile from, int tab = BotsTab, int page = 0)
    {
        if (from?.NetState == null || from.AccessLevel < AccessLevel.Administrator)
        {
            return;
        }

        if (!BotCore.Enabled)
        {
            from.SendMessage("The bot assembly is switched off — bots.enabled in modernuo.json.");

            return;
        }

        from.CloseGump<BotDashboardGump>();
        from.SendGump(new BotDashboardGump(tab, page));
    }

    private void Fill<T>(List<T> from, List<T> into)
    {
        var start = _page * Rows;

        for (var i = start; i < from.Count && into.Count < Rows; i++)
        {
            into.Add(from[i]);
        }
    }

    /// <summary>
    /// The bots that exist, holes left by deleted ones removed: the ones in the world first, then the ones resting out of
    /// it, which the roll shows in grey (Patrick's order of 24.09.2026) rather than leaving off.
    /// </summary>
    private static List<BotMobile> Population()
    {
        var all = BotPopulation.Bots;
        List<BotMobile> alive = [];

        for (var i = 0; i < all.Count; i++)
        {
            if (all[i] is { Deleted: false })
            {
                alive.Add(all[i]);
            }
        }

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is { Deleted: false })
            {
                alive.Add(away[i]);
            }
        }

        return alive;
    }

    private static List<BotWant> Needs()
    {
        var all = BotMarket.Wants;
        List<BotWant> wants = [];

        for (var i = 0; i < all.Count; i++)
        {
            wants.Add(all[i]);
        }

        return wants;
    }

    /// <summary>The market's lots on show at the shops by the bank. See <c>BotShopkeep.Lots</c>.</summary>
    private static List<(BotStorefront Front, Type Kind, int Units, int Ask)> Market() => BotShopkeep.Lots();

    private static int Purse(Mobile bot) => bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;

    private static string Doing(BotMobile bot)
    {
        var deed = bot.Resolve.Deed;

        if (deed != null)
        {
            return deed.ToString();
        }

        return bot.Alive ? "nothing" : "dead";
    }

    private static int Rung(BotResolve resolve) =>
        resolve.Standing switch
        {
            BotStanding.Free => Ink,
            BotStanding.Busy => Good,
            _ => Bad
        };

    /// <summary>Red below the mark, plain above it. Colour is the only thing a table of numbers cannot say.</summary>
    private static int Shade(double value, double mark) => value < mark ? Bad : Ink;
}
