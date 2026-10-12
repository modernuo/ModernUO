using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The guild's chest: what the guild's ground earns, and what it pays for before any member's pack is asked.
///
/// <para>
/// <b>A claim was a cost and never an income.</b> A guild levied its members for a square, held it, and got
/// nothing back but a pin on the map and a war when somebody else wanted it; the ground was hunted by everybody
/// and paid the holder nothing. Patrick's fourth Quad idea of 16.09.2026, taken that evening: hunting on ground a
/// guild holds pays the holder a tithe, and the chest pays the guild's next claim or hall before any member's pack
/// is asked (<see cref="BotEstate.Levy"/>). The reeve (<see cref="BotReeve"/>) is the other half of the same
/// idea — the guild's own company for the guild's own ground.
/// </para>
///
/// <para>
/// Only coin that actually arrived in a pack is tithed (<see cref="BotTakings.Coin"/>), only from work that braves
/// the ground (<see cref="BotDeed.Braves"/>: hunts, prowls, bands, harrows, plunder), and only when the pack still
/// has it; otherwise nothing is taken and nothing is owed. The holder's own members pay the same tithe — into their
/// own chest. Kept across restarts by <see cref="BotChestStore"/>; wiped with the claims on a reset.
/// </para>
/// </summary>
public static class BotChest
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotChest));

    /// <summary>Whether tithes are taken and chests pay at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The share of coin taken on held ground that goes to the guild holding it. A tenth.</summary>
    public static double Rate { get; set; } = 0.1;

    private static readonly Dictionary<string, int> _chests = new(StringComparer.Ordinal);

    /// <summary>Tithes paid, and what they came to. For the summary.</summary>
    public static long Tithes { get; private set; }

    public static long TitheGold { get; private set; }

    /// <summary>Times a chest paid towards a levy, and what it paid. For the summary.</summary>
    public static long Draws { get; private set; }

    public static long DrawnGold { get; private set; }

    /// <summary>What this guild's chest holds.</summary>
    public static int Holds(string guild) => guild != null && _chests.TryGetValue(guild, out var gold) ? gold : 0;

    /// <summary>
    /// A tenth of the coin this work brought in, if the square it was on is held by a guild and the pack still has
    /// it. Called from the will's settling, for work that braves the ground. Returns what went into the bot's own guild's
    /// chest, which its dues are then reduced by (<see cref="BotDues.Pay"/>): a tithe on the guild's own ground is the
    /// same payment to the same chest, and is not asked twice.
    /// </summary>
    public static int Tithe(Mobile body, Map map, Point3D where, int coin)
    {
        if (!Running || coin <= 0 || map == null || map == Map.Internal || body?.Backpack is not { } pack)
        {
            return 0;
        }

        // <b>The holder of the ground, yards included, from 26.09.2026.</b> Until then only a claimed square was asked
        // (BotClaim.Owner), so a hall's own yard — the ground a guild holds most surely — paid it nothing.
        var owner = BotLand.Holder(map, where);

        if (owner == null)
        {
            return 0;
        }

        // The guild's own pay their dues at the old tithe; a stranger pays the toll the holder charges, which may be
        // nothing. See BotToll: Patrick's order of 26.09.2026 on hunting a guild's land.
        var own = body.Guild?.Name == owner;
        var rate = own ? Rate : BotToll.Rate(owner, body);

        if (rate <= 0.0)
        {
            if (!own)
            {
                BotToll.Owed(owner, body);
            }

            return 0;
        }

        var share = (int)(coin * rate);

        if (share <= 0 || !pack.ConsumeTotal(typeof(Gold), share))
        {
            return 0;
        }

        _chests[owner] = Holds(owner) + share;

        if (own)
        {
            Tithes++;
            TitheGold += share;

            return share;
        }

        BotToll.Paid(owner, body, share);

        return 0;
    }

    /// <summary>Coin paid into a guild's chest by one of its members as dues. Counted by <see cref="BotDues"/>, not here.</summary>
    public static void Put(string guild, int gold)
    {
        if (string.IsNullOrEmpty(guild) || gold <= 0)
        {
            return;
        }

        _chests[guild] = Holds(guild) + gold;
    }

    /// <summary>
    /// A draw handed back: the levy it was drawn for fell short, and a short levy buys nothing. Undoes the draw's count as
    /// well as its coin, so "draws of Ngp on the chests" still means coin that bought something. See <c>BotEstate.Take</c>.
    /// </summary>
    public static void Return(string guild, int gold)
    {
        if (string.IsNullOrEmpty(guild) || gold <= 0)
        {
            return;
        }

        _chests[guild] = Holds(guild) + gold;
        Draws--;
        DrawnGold -= gold;
        Returned += gold;

        logger.Information("{Guild}'s chest had its {Gold}gp back: the levy it paid into fell short; {Held}gp in it", guild, gold, Holds(guild));
    }

    /// <summary>Coin handed back to chests from levies that fell short. See <see cref="Return"/>.</summary>
    public static long Returned { get; private set; }

    /// <summary>What the chest pays towards a levy on the guild: as much of the want as it holds.</summary>
    public static int Draw(string guild, int want)
    {
        if (!Running || want <= 0)
        {
            return 0;
        }

        var held = Holds(guild);

        if (held <= 0)
        {
            return 0;
        }

        var drawn = Math.Min(held, want);

        _chests[guild] = held - drawn;
        Draws++;
        DrawnGold += drawn;

        // When the chest last paid for something, for the conspiracy's "the chest paid nothing for us while rich" (07.10.2026).
        _lastDraw[guild] = Core.TickCount;

        logger.Information("{Guild}'s chest paid {Gold}gp of the {Want}gp asked of the guild; {Left}gp left in it", guild, drawn, want, held - drawn);

        return drawn;
    }

    /// <summary>When each chest last paid towards something (<see cref="Draw"/>), as a tick. Session only.</summary>
    private static readonly Dictionary<string, long> _lastDraw = new(StringComparer.Ordinal);

    private static long _openedTick = Core.TickCount;

    /// <summary>
    /// How long since this guild's chest last paid towards anything, in milliseconds of running time; since the module opened
    /// when it has not paid this session. Asked by <c>BotElection</c>'s conspiracy (07.10.2026, politics17).
    /// </summary>
    public static long SinceDraw(string guild) =>
        Core.TickCount - (guild != null && _lastDraw.TryGetValue(guild, out var at) ? at : _openedTick);

    /// <summary>Coin moved from one guild's chest to another's — tribute (<c>BotTribute</c>) — and how often. Nothing is made or lost.</summary>
    public static long Transfers { get; private set; }

    public static long TransferGold { get; private set; }

    /// <summary>
    /// Moves up to <paramref name="gold"/> from <paramref name="from"/>'s chest to <paramref name="to"/>'s: as much as the payer
    /// holds. Not a draw — it buys nothing — so it is counted apart (<see cref="Transfers"/>). Returns what moved.
    /// </summary>
    public static int Pay(string from, string to, int gold)
    {
        if (!Running || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || from == to || gold <= 0)
        {
            return 0;
        }

        var moved = Math.Min(Holds(from), gold);

        if (moved <= 0)
        {
            return 0;
        }

        _chests[from] = Holds(from) - moved;
        _chests[to] = Holds(to) + moved;
        Transfers++;
        TransferGold += moved;

        return moved;
    }

    /// <summary>Break-ins that took something out of a chest, and what they took. See <c>BotBurgle</c>.</summary>
    public static long Thefts { get; private set; }

    public static long StolenGold { get; private set; }

    /// <summary>What a burglar carries out of this guild's chest: as much of the want as it holds.</summary>
    public static int Steal(string guild, int want)
    {
        var held = Holds(guild);

        if (!Running || want <= 0 || held <= 0)
        {
            return 0;
        }

        var taken = Math.Min(held, want);

        _chests[guild] = held - taken;
        Thefts++;
        StolenGold += taken;

        return taken;
    }

    /// <summary>
    /// Takes a closed guild's chest off the books and hands back what it held, for the closing to share among the guild's
    /// last members (06.10.2026, <c>BotGuilds.Close</c>). Not a draw: nothing was bought with it, and the closing's own
    /// line says where the coin went.
    /// </summary>
    public static int Empty(string guild)
    {
        if (string.IsNullOrEmpty(guild) || !_chests.Remove(guild, out var gold))
        {
            return 0;
        }

        return Math.Max(0, gold);
    }

    /// <summary>The chests, for the store.</summary>
    internal static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_chests.Count);

        foreach (var (guild, gold) in _chests)
        {
            writer.Write(guild);
            writer.WriteEncodedInt(gold);
        }
    }

    /// <summary>The chests read back. Returns how many.</summary>
    internal static int Load(IGenericReader reader)
    {
        _chests.Clear();

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var guild = reader.ReadString();
            var gold = reader.ReadEncodedInt();

            if (!string.IsNullOrEmpty(guild) && gold > 0)
            {
                _chests[guild] = gold;
            }
        }

        return _chests.Count;
    }

    /// <summary>Part of the estate's line.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "no tithes are taken";
        }

        using var say = ValueStringBuilder.Create(256);

        say.Append($"{Tithes} tithes of {TitheGold}gp paid to the guilds holding the ground hunted ({Rate:P0} of the coin), {Draws} draws of {DrawnGold}gp on the chests ({Returned}gp handed back from levies that fell short); chests: ");

        if (_chests.Count == 0)
        {
            say.Append("none hold anything");

            return say.ToString();
        }

        var first = true;

        foreach (var (guild, gold) in _chests)
        {
            if (!first)
            {
                say.Append(", ");
            }

            first = false;
            say.Append(guild);
            say.Append(' ');
            say.Append(gold);
            say.Append("gp");
        }

        return say.ToString();
    }

    /// <summary>The chests emptied: a reset takes the claims with it, and the chests were the claims' income.</summary>
    public static void Wipe() => _chests.Clear();

    public static void Forget()
    {
        Tithes = 0;
        TitheGold = 0;
        Draws = 0;
        DrawnGold = 0;
        Returned = 0;
        Transfers = 0;
        TransferGold = 0;
        _lastDraw.Clear();
        _openedTick = Core.TickCount;
    }
}
