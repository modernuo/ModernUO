using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// A guild's dues: the share of what its members' work brings in that goes into the guild's chest, and what the chest keeps
/// back from everyday spending while the guild saves for its hall or its house in Britain.
///
/// <para>
/// <b>Why no hall had been raised since the wipe of 29.09.2026, measured before anything was changed.</b> A guild's money was
/// never a store. <c>BotEstate.Fund</c> was the sum, at the moment of asking, of what each member held above
/// <c>BotEstate.Keep</c> (300gp), and it did not count the chest at all. On the day of the wipe the middling purse read
/// 19 to 242gp in all 207 money lines, 68gp at the median (19:53: "poorest 0gp, middling 27gp"), so hardly a member ever
/// stood above the keep — and the
/// few who did were emptied again within minutes by <c>BotGuilds.Stand</c>, which draws on exactly the same spare, richest
/// first, down to the same 300, to pay for other members' armour and, since build 259, their compulsory supplies. Fifty-three
/// of the day's 207 money lines name a fattest purse of exactly 300gp; one session stood 29,374gp for members (16:04:31). Two
/// claimants on one pool, and the one that spends every minute always wins against the one that has to wait for five
/// thousand. The chest, the one thing that does keep, is fed only by tithes on held ground, ground is a hall's yard or a
/// claim, and a claim is offered only to a guild with a hall — 1,366 of 1,366 looks at 19:53 "had no hall to want ground
/// near". A ring of three gates, and the Estate line printed "0 of 5000" for every guild all day.
/// </para>
///
/// <para>
/// <b>The repair is a store, not a bigger number.</b> <see cref="Share"/> of the coin a member's settled work brought in (the
/// takings' own <c>Coin</c>, pack, bank and escrow, the same figure the ledger learns from) is paid into the guild's chest
/// the moment the work is settled — before the next purchase or the next stand can reach it — less whatever that work
/// already tithed to the same chest on the guild's own ground. Nothing is minted: the coin moves from a member to its guild,
/// and a hall's price leaves the world when the hall is bought, as it always has. It is booked aside for the member
/// (<c>BotYield.Aside</c>), so no piece of work is taught that dues are its cost. The chest now counts in <c>Fund</c>, which
/// every purchase's gate asks; and while the guild has no hall — or is saving for a house in Britain — the chest keeps that
/// price back from the everyday draws (<c>BotProvision.Fund</c>, a ship's price), which still go to the members as before.
/// </para>
///
/// <para>
/// <b>What it costs the members, in the day's numbers.</b> Settled work brought in some 14,000 to 17,500gp an hour across
/// the population on 29.09.2026 (the positive coin of every finished line: 14,176/h 17:38–18:38, 17,569/h 19:12–19:36), about
/// nine tenths of it to guild members (86 of 96 bots at 19:53). A fifth is then some 2,500 to 3,100gp an hour into seven
/// chests — a guild of fifteen reaches the hall's 5,000 in nine to eleven hours, a guild of six in about a day. That is a
/// judgement, not an accident: the dial is <see cref="Share"/> and the price is <c>BotEstate.Price</c>, and the
/// "Treasuries:" line prints what each chest gained since the start, so the rate can be read before either is moved.
/// </para>
/// </summary>
public static class BotDues
{
    /// <summary>Whether members pay dues into their guild's chest. Off, and the chest is fed by tithes alone, as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The share of the coin a member's work brings in that goes into its guild's chest. A fifth.</summary>
    public static double Share { get; set; } = 0.2;

    /// <summary>
    /// A chest at or above this takes no dues. Twenty thousand: every purchase the estate knows of — a hall, a house in Britain,
    /// a paid claim, a widening — is under it, and coin sitting in a chest beyond every price is coin no member can spend.
    /// Counted when it stops a payment, so a full chest is never silent.
    /// </summary>
    public static int Ceiling { get; set; } = 20000;

    /// <summary>Whether the chest keeps back what its guild is saving for from the everyday draws. See <see cref="Goal"/>.</summary>
    public static bool Saving { get; set; } = true;

    /// <summary>Dues paid, and how often.</summary>
    public static long Paid { get; private set; }

    public static long Payments { get; private set; }

    /// <summary>Dues not paid because the chest was at <see cref="Ceiling"/>.</summary>
    public static long Full { get; private set; }

    /// <summary>Dues owed that neither the pack nor the account could pay: the coin was spent inside the work that earned it.</summary>
    public static long Unpaid { get; private set; }

    /// <summary>Gold an everyday draw was refused because the chest was keeping it for the guild's next purchase.</summary>
    public static long Kept { get; private set; }

    /// <summary>Dues held back, and the gold of them, because they would have taken a member under the keep. See <see cref="Pay"/>.</summary>
    public static long UnderKeep { get; private set; }

    public static long UnderKeepGold { get; private set; }

    private static readonly Dictionary<string, long> _paidBy = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, long> _keptBy = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A member's settled work brought in <paramref name="coin"/>; its guild takes its share, less <paramref name="tithed"/>,
    /// what the same work already paid into the same chest as a tithe on the guild's own ground. From the pack first and the
    /// account after, as every payment on this shard is taken (<c>BotStable.Buy</c>). Returns what was paid.
    /// </summary>
    public static int Pay(Mobile body, int coin, int tithed)
    {
        if (!Running || Share <= 0.0 || coin <= 0 || body is not BotMobile { Deleted: false } bot || bot.Guild is not Guild guild)
        {
            return 0;
        }

        // The Shadow keeps no hall and buys no ground; its chest is its hideout's. See BotUnderworld.Band.
        if (BotUnderworld.Band(guild))
        {
            return 0;
        }

        var held = BotChest.Holds(guild.Name);

        if (held >= Ceiling)
        {
            Full++;

            return 0;
        }

        var want = Math.Min((int)(coin * Share) - Math.Max(0, tithed), Ceiling - held);

        // <b>Only above the keep (02.10.2026, build 389; Patrick's option 1).</b> A fifth of a 30gp bot's hunt kept it at 30gp:
        // the dues took 22,170gp in the nine hours of build 388 while the median purse read 95. The chest is fed by members who
        // hold more than the keep, and what it is not fed is counted.
        var room = BotYield.Spendable(bot) - BotEstate.Keep;

        if (want > 0 && room < want)
        {
            UnderKeep++;
            UnderKeepGold += want - Math.Max(0, room);
            want = Math.Max(0, room);
        }

        if (want <= 0)
        {
            return 0;
        }

        var pack = bot.Backpack;
        var fromPack = pack == null ? 0 : Math.Min(pack.GetAmount(typeof(Gold)), want);

        if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
        {
            fromPack = 0;
        }

        var fromBank = Math.Min(want - fromPack, Banker.GetBalance(bot));

        if (fromBank > 0 && !Banker.Withdraw(bot, fromBank))
        {
            fromBank = 0;
        }

        var paid = fromPack + fromBank;

        if (paid <= 0)
        {
            Unpaid++;

            return 0;
        }

        BotChest.Put(guild.Name, paid);

        // Coin given to the chest is one of the things members elect their head by (07.10.2026). See BotElection.Gave.
        BotElection.Gave(bot, paid);

        // A decision outside the work: the work that just settled has been measured already, and a piece of work put down
        // round this one must not read the dues as its own loss when it settles. See BotYield.Aside.
        BotYield.Aside(bot, paid);

        Paid += paid;
        Payments++;
        _paidBy[guild.Name] = _paidBy.GetValueOrDefault(guild.Name) + paid;

        return paid;
    }

    /// <summary>
    /// What the guild's chest is keeping back now, and for what: the hall's price while it has none, else the price of a house
    /// in Britain while it saves for one (<c>BotGuildHouses.Saving</c>), else nothing.
    /// </summary>
    public static int Goal(Guild guild, out string what)
    {
        what = null;

        if (!Saving || guild == null)
        {
            return 0;
        }

        // What the members voted to save for, once they have voted (07.10.2026). See BotPurpose: "nothing" keeps nothing back.
        if (BotPurpose.Goal(guild, out var voted, out var price))
        {
            what = voted;

            return price;
        }

        if (BotEstate.Running && BotEstate.Hall(guild) == null && !BotUnderworld.Band(guild))
        {
            what = "the hall";

            return BotEstate.Price;
        }

        var britain = BotGuildHouses.Saving(guild);

        if (britain > 0)
        {
            what = "a house in Britain";

            return britain;
        }

        return 0;
    }

    /// <summary>
    /// What the chest may pay for everyday things: what it holds above what it keeps back for its goal. <b>A share of what it
    /// holds, not the whole goal (29.09.2026, build 335).</b> Kept back to the full 5000 of a hall, the chests paid for no
    /// compulsory kit at all — 64 % of 2343 asks for a delve on 334 were refused for want of supplies, bots holding 65gp on
    /// average and no member above the 300gp keep to stand for another (`night-r7-delves.md`). The chest keeps
    /// <see cref="SaveShare"/> of what it holds toward the goal and spares the rest.
    /// </summary>
    public static int Spare(Guild guild)
    {
        if (guild == null)
        {
            return 0;
        }

        var holds = BotChest.Holds(guild.Name);
        var keep = Math.Min(Goal(guild, out _), (int)(holds * Math.Clamp(SaveShare, 0.0, 1.0)));

        return Math.Max(0, holds - keep);
    }

    /// <summary>The share of what a saving chest holds that it keeps back toward its goal. See <see cref="Spare"/>.</summary>
    public static double SaveShare { get; set; } = 0.75;

    /// <summary>An everyday draw of <paramref name="asked"/> was cut to what the chest could spare. Counted, by guild.</summary>
    public static void Keep(Guild guild, int asked, int spare)
    {
        var kept = Math.Min(asked, BotChest.Holds(guild?.Name)) - spare;

        if (guild == null || kept <= 0)
        {
            return;
        }

        Kept += kept;
        _keptBy[guild.Name] = _keptBy.GetValueOrDefault(guild.Name) + kept;
    }

    /// <summary>
    /// The "Treasuries:" line: for every guild, what its chest holds and what its members could spare above the keep — the
    /// two halves of <c>BotEstate.Fund</c> — how many members stand above the keep at all and how rich the richest is, what
    /// dues brought in and what the chest kept back, and where the guild stands on the ladder: its seat, its hall, its
    /// ground. Written so that the day's question — "why is nothing built?" — is answered by one line, dues on or off.
    /// </summary>
    public static string Describe()
    {
        using var say = ValueStringBuilder.Create(1024);

        say.Append(
            Running
                ? $"dues of {Share:P0} on the coin members' work brings in, only above the {BotEstate.Keep}gp keep: {Paid}gp paid in {Payments} times, {UnderKeep} held back ({UnderKeepGold}gp) for a member at or under the keep, {Unpaid} owed and already spent, {Full} not taken for a chest at {Ceiling}gp; "
                : "no dues are taken; "
        );

        say.Append($"chests kept {Kept}gp back from everyday draws for what their guilds save for; members stood {BotGuilds.Stood}gp for each other out of the same spare the hall asks for ({BotGuilds.Cannot} times they could not)");

        // What the members voted to save for, and the chests' politics: tribute between chests and the town's commission (07.10.2026).
        say.Append("; ");
        say.Append(BotPurpose.Describe());

        // Where claim money goes, held and handed back (06.10.2026, build 408). See BotClaim.Close and BotCity.Tax.
        say.Append("; ");
        say.Append(BotClaim.DescribeMoney());

        foreach (var guild in BotGuilds.Standing)
        {
            if (guild is not { Disbanded: false } || BotUnderworld.Band(guild))
            {
                continue;
            }

            var above = 0;
            var members = 0;
            var richest = 0;

            for (var i = 0; i < guild.Members.Count; i++)
            {
                if (guild.Members[i] is not BotMobile { Deleted: false } member)
                {
                    continue;
                }

                members++;

                var wealth = BotYield.Spendable(member);

                if (wealth > BotEstate.Keep)
                {
                    above++;
                }

                richest = Math.Max(richest, wealth);
            }

            var chest = BotChest.Holds(guild.Name);
            var spare = BotEstate.Spare(guild);
            var goal = Goal(guild, out var what);
            var hall = BotEstate.Hall(guild);
            var house = BotGuildHouses.Of(guild.Name);
            var seat = BotSeat.Of(guild);

            say.Append("; ");
            say.Append(guild.Name);
            say.Append($": chest {chest}gp + members' spare {spare}gp ({above} of {members} above the {BotEstate.Keep}gp keep, the richest {richest}gp)");

            if (goal > 0)
            {
                say.Append($" = {chest + spare} of {what}'s {goal}gp");
            }

            say.Append($", dues {_paidBy.GetValueOrDefault(guild.Name)}gp in, {_keptBy.GetValueOrDefault(guild.Name)}gp kept back; ");
            say.Append(
                house != null
                    ? $"house in {house.Town}"
                    : seat != Point3D.Zero
                        ? $"seat at {seat.X},{seat.Y}"
                        : "no seat"
            );
            say.Append(hall is { Deleted: false } ? $", hall at {hall.X},{hall.Y}" : ", no hall");
            say.Append($", {BotClaim.Holds(guild.Name)} squares");
        }

        return say.ToString();
    }

    public static void Forget()
    {
        Paid = 0;
        Payments = 0;
        Full = 0;
        Unpaid = 0;
        Kept = 0;
        UnderKeep = 0;
        UnderKeepGold = 0;
        _paidBy.Clear();
        _keptBy.Clear();
    }
}
