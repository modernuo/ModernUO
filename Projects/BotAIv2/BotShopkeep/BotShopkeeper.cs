using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot with goods the island is asking for a shift keeping shop by Britain's bank — the market — or, for a guild's
/// member, by its guild's own town's bank.
///
/// <para>
/// <b>Asked of anybody, decided by what is in the pack and the bank box.</b> No class is a shopkeeper by birth: a hunter with
/// forty reagents off corpses in the bank, a weaver with bandages cut from its own cloth, a smith with the pieces nobody ordered
/// — each of them is a shop for as long as it holds what somebody is short of. The goods are counted the way the
/// porter counts them (<see cref="BotShopkeep.Look"/>), priced the way a shop will price them, and the shift is offered only
/// when what the island is actually asking for comes to <see cref="BotShopkeep.OpenWorth"/> or more: a shop of bones is not
/// offered at all, and a shop that nobody wants anything from would be a bot standing at a bank for eight minutes.
/// </para>
///
/// <para>
/// <b>Priced at what such a shift sells, and corrected by what it sold.</b> The claim is the demanded stock times the share of
/// it a shift is measured to sell (<see cref="BotShopkeep.Share"/>), over the shift's minutes; the ledger then corrects it by
/// what this bot's shifts at this bank really took, so a keeper whose customers never come stops being offered the shift, and
/// one whose bank is busy is offered it over the dig.
/// </para>
///
/// <para>
/// <b>An offer reserves nothing.</b> The pitch is chosen here so the walk to it is stable, but it is taken only when the keeper
/// is standing on it and opens (<see cref="BotShopkeep.OpenShop"/>): an offer that loses the auction has spoken for nothing.
/// </para>
/// </summary>
public sealed class BotShopkeeper : IBotProposer
{
    public string Name => "Shopkeeper";

    public BotStanding Rung => BotStanding.Free;

    // ---- Every gate, counted apart. --------------------------------------------------------------------------

    /// <summary>Bots looked at.</summary>
    public static long Looks { get; private set; }

    /// <summary>Shifts offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Shut a shop less than <see cref="BotShopkeep.RestMs"/> ago.</summary>
    public static long Rested { get; private set; }

    /// <summary>The island already had <see cref="BotShopkeep.MostOpen"/> shops open.</summary>
    public static long Full { get; private set; }

    /// <summary>Held less than <see cref="BotShopkeep.OpenWorth"/> of anything at all. Most answers, and not a refusal.</summary>
    public static long Thin { get; private set; }

    /// <summary>Held enough goods, but not enough of what anybody is short of or has money down for.</summary>
    public static long Unwanted { get; private set; }

    /// <summary>A red bot, a criminal or one of The Shadow: a town's guards are no customers.</summary>
    public static long Outlawed { get; private set; }

    /// <summary>No banker within <see cref="BotShopkeep.BankTiles"/> of home.</summary>
    public static long NoBank { get; private set; }

    /// <summary>The bank already had <see cref="BotShopkeep.PerBank"/> shops beside it.</summary>
    public static long Crowded { get; private set; }

    /// <summary>Every pitch at the bank taken, lately unreachable for this bot, or proved to have no way to it.</summary>
    public static long NoPitch { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotShopkeep.Running || bot?.Self is not BotMobile body || !body.Alive)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        Looks++;

        // <b>A shop is kept by those who make or gather goods, not by fighters (01.10.2026).</b> Measured over 1,076 online bot-hours
        // on 30.09.2026 (`night-r29-stalling.md`): 103 bot-hours a day went to shifts, 88 of them by Warriors, Archers, the two kinds
        // of mage, WarriorArchers and Tamers standing at a bank with what came off corpses, and 53 % of all shifts ended "nobody
        // came to buy". A fighter's surplus goes to the bank box, to a carrier's walk when somebody wants it, and to a shopkeeper
        // when nobody does; the shop by the bank is the makers' and gatherers' (and the coming traders').
        if (!FightersKeepShop && !Merchant(body))
        {
            Fighters++;

            return null;
        }

        if (BotShopkeep.Resting(body))
        {
            Rested++;

            return null;
        }

        if (BotShopkeep.Spoken >= BotShopkeep.MostOpen)
        {
            Full++;

            return null;
        }

        var stock = BotShopkeep.Look(body);

        if (stock.Worth < BotShopkeep.OpenWorth)
        {
            Thin++;

            return null;
        }

        if (stock.Demanded < BotShopkeep.OpenWorth)
        {
            Unwanted++;

            return null;
        }

        // A red name at a bank is a fight with the guards, not a shop. The fence keeps the band's goods at the hideout.
        if (body.Kills >= 5 || body.Criminal || BotUnderworld.Member(body))
        {
            Outlawed++;

            return null;
        }

        // <b>A guild's trader at home, everybody else in Britain (01.10.2026; Patrick, 30.09: "all trade and all the workshops
        // live in Britain, and a guild's traders prefer to trade in their home town").</b> Britain first was already the rule
        // (build 355: on 338–350 the shops by other banks were "too far off" for 98–99 % of the buyers' looks); what changes is the
        // fallback. The bank by the bot's own home is gone — a shop at Skara Brae's bank is not the market — and in its place a
        // guild member keeps shop at its guild's own town (its house, else its seat), where the guild lives and trades, when it is
        // standing near that bank; failing that, Britain's. A bot with neither near is not offered a shift: its goods go by
        // carrier to a want, to a shopkeeper, or to Britain with a traveller's journey (BotCapital.Trader).
        var bank = Home(map, body);

        if (bank != Point3D.Zero)
        {
            ToHome++;
        }
        else
        {
            bank = Britain(map, body);

            if (bank != Point3D.Zero)
            {
                ToBritain++;
            }
        }

        if (bank == Point3D.Zero)
        {
            NoBank++;

            return null;
        }

        // <b>A shift is for a bot already near its bank (30.09.2026, build 358).</b> Offered wherever the keeper happened to be,
        // a shift began with a walk the auction did not wait out: of the shifts taken on 353–356, 134 were dropped "going to
        // the bank … outbid by restock at 178/min after 8.2 of 10.0 minutes reckoned" against 38 finished — the keeper never
        // stood at its pitch, and buyers who had set out found no shop. Only within ShiftReach of the bank it would keep shop at.
        if (!Utility.InRange(body.Location, bank, ShiftReach))
        {
            Afar++;

            return null;
        }

        if (BotShopkeep.Crowded(map, bank, body))
        {
            Crowded++;

            return null;
        }

        var stand = BotShopkeep.Pitch(map, bank, bot);

        if (stand == Point3D.Zero)
        {
            NoPitch++;

            return null;
        }

        Offered++;

        return new BotKeepShop(map, stand, bank, BotShopkeep.Claim(stock.Demanded), stock.Demanded);
    }

    /// <summary>Whether fighters are offered shifts too. Off since 01.10.2026: see <see cref="Merchant"/>.</summary>
    public static bool FightersKeepShop { get; set; } = false;

    /// <summary>Looks at a fighter, which keeps no shop. See <see cref="Merchant"/>.</summary>
    public static long Fighters { get; private set; }

    /// <summary>
    /// Whether this bot keeps shop: a maker or a gatherer (<see cref="BotRole.Producer"/> — the crafter, the gatherer, the
    /// architect). The seam the traders' faction joins at when it comes.
    /// </summary>
    public static bool Merchant(Mobile body) => body is BotMobile { Class: { } klass } && klass.Role == BotRole.Producer;

    /// <summary>How far from Britain's bank a keeper is still sent there rather than to the bank by its home.</summary>
    public static int BritainReach { get; set; } = 200;

    /// <summary>How near its bank a bot must stand to be offered a shift there. See <see cref="Propose"/>.</summary>
    public static int ShiftReach { get; set; } = 200;

    /// <summary>Looks at which the bot stood further than ShiftReach from the bank it would keep shop at.</summary>
    public static long Afar { get; private set; }

    /// <summary>Shifts offered at Britain's bank. See <see cref="Britain"/>.</summary>
    public static long ToBritain { get; private set; }

    /// <summary>Shifts offered at a guild's own town's bank to one of its members. See <see cref="Home"/>.</summary>
    public static long ToHome { get; private set; }

    /// <summary>How long a guild's town is remembered by its name. A minute: guilds move house rarely, and this is asked every review.</summary>
    public static int HomeMs { get; set; } = 60000;

    private static readonly Dictionary<string, (Point3D Bank, long Tick)> _homes = [];

    /// <summary>
    /// The bank of this bot's guild's own town, when the bot is a guild member standing within <see cref="ShiftReach"/> of it and
    /// the town is not Britain itself (Britain's own members trade at the market as everybody does); zero otherwise. The town is
    /// the guild's house's (<c>BotGuildHouses.TownOf</c>), else its seat's (<c>BotParley.Seat</c>). The Shadow keeps no shop.
    /// </summary>
    private static Point3D Home(Map map, Mobile body)
    {
        if (body?.Guild is not Guilds.Guild guild || BotUnderworld.Member(body))
        {
            return Point3D.Zero;
        }

        var now = Core.TickCount;
        var name = guild.Name ?? "";

        if (!_homes.TryGetValue(name, out var known) || now - known.Tick >= HomeMs)
        {
            var town = BotGuildHouses.TownOf(guild);

            if (town == null)
            {
                BotParley.Seat(guild, out town, out _);
            }

            var bank = Point3D.Zero;

            if (town != null && !string.Equals(town.Name, BotCapital.Name, StringComparison.OrdinalIgnoreCase))
            {
                var hearth = BotSettle.Of(town)?.Hearth ?? town.Square;

                bank = hearth == Point3D.Zero ? Point3D.Zero : BotShopkeep.BankNear(map, hearth);
            }

            if (_homes.Count > 256)
            {
                _homes.Clear();
            }

            _homes[name] = known = (bank, now);
        }

        var home = known.Bank;

        if (home == Point3D.Zero || !Utility.InRange(body.Location, home, ShiftReach) || !BotGates.Joined(map, body.Location, home)
            || BotShopkeep.Crowded(map, home, body))
        {
            return Point3D.Zero;
        }

        return home;
    }

    /// <summary>
    /// Britain's bank for this keeper, or zero when it is out of reach, not joined, or full. The town is the one the trade's centre
    /// is named for (<see cref="BotCapital.Name"/>, 30.09.2026), so a dial that moves the capital moves its shops with it.
    /// </summary>
    private static Point3D Britain(Map map, Mobile body)
    {
        var town = BotTowns.Find(BotCapital.Name);

        if (town == null)
        {
            return Point3D.Zero;
        }

        var hearth = BotSettle.Of(town)?.Hearth ?? town.Square;
        var bank = hearth == Point3D.Zero ? Point3D.Zero : BotShopkeep.BankNear(map, hearth);

        if (bank == Point3D.Zero || !Utility.InRange(body.Location, bank, BritainReach) || !BotGates.Joined(map, body.Location, bank)
            || BotShopkeep.Crowded(map, bank, body))
        {
            return Point3D.Zero;
        }

        return bank;
    }

    public static string Describe() =>
        Looks == 0
            ? "nobody has been looked at for a shop"
            : $"{Looks} looks for a shop: {Offered} shifts offered, {Fighters} were fighters, who keep no shop, {Thin} held under {BotShopkeep.OpenWorth}gp of goods, "
              + $"{Unwanted} held nothing the island is short of or has money down for, {Rested} were resting from a shift, "
              + $"{Outlawed} were outlaws, {ToBritain} sent to Britain's bank (within {BritainReach} tiles), {ToHome} guild members sent to their guild's own town's bank, {Afar} further than {ShiftReach} tiles from the bank, {NoBank} were near neither Britain nor their guild's town, {Crowded} found the bank already had {BotShopkeep.PerBank} shops, "
              + $"{NoPitch} found no pitch free there, {Full} found the island had {BotShopkeep.MostOpen} open";

    public static void Forget()
    {
        Looks = 0;
        Fighters = 0;
        Offered = 0;
        Rested = 0;
        Full = 0;
        Thin = 0;
        Unwanted = 0;
        Outlawed = 0;
        NoBank = 0;
        ToBritain = 0;
        ToHome = 0;
        _homes.Clear();
        Afar = 0;
        Crowded = 0;
        NoPitch = 0;
    }
}
