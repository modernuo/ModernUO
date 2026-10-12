using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Whether the guild wants a merchant in its hall, can pay for one, and who is going to fetch it.
///
/// <para>
/// The third of the estate's proposers and the same shape as the other two — any member may go, one at a
/// time per guild, and what the guild can afford is asked here rather than inside the work. A hall must
/// exist first, which makes this the second piece of work on the shard that exists because the population
/// built something.
/// </para>
///
/// <para>
/// It is asked for last of the three: a hall, then the tools its trade needs, then a shopkeeper to sell what
/// those tools make. That order is not enforced anywhere — it falls out of what each costs.
/// </para>
/// </summary>
public sealed class BotHirer : IBotProposer
{
    /// <summary>How long a claim lasts after the bot holding it was last heard from.</summary>
    public static int ClaimMs { get; set; } = 240000;

    /// <summary>Merchants offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Times a guild wanted one and could not pay.</summary>
    public static long Wanting { get; private set; }

    /// <summary>
    /// And how short the last such guild was.
    ///
    /// <para>
    /// <b>A refusal for want of money that does not say how much money is half an instrument.</b> The
    /// steward's line has always said "The Hammer has 443 of 5000 and is 4557 short", and reading it takes a
    /// second; this one said "held off 349 times for want of the money" and left the reader to guess whether
    /// the guild was fifty gold short or a thousand — the difference between a dial to move and an hour to
    /// wait. That is this project's most-repeated defect in its mildest form, and it is one line.
    /// </para>
    /// </summary>
    public static int Short { get; private set; }

    /// <summary>What the guild nearest to affording one actually had, so the pair can be read together.</summary>
    public static int Nearest { get; private set; }

    /// <summary>Times nobody within reach sells a contract of employment.</summary>
    public static long Unsold { get; private set; }

    /// <summary>Times the hall already had all the merchants it is allowed. The good answer.</summary>
    public static long Staffed { get; private set; }

    /// <summary>Times the guild was still fitting out its workshop, which comes first.</summary>
    public static long Fitting { get; private set; }

    /// <summary>Times another member was already away fetching one.</summary>
    public static long Claimed { get; private set; }

    /// <summary>This officer's name in the shared register of who is on what. See <see cref="BotOffice"/>.</summary>
    public const string Office = "hirer";

    public string Name => "hirer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || !BotEstate.Merchanting || bot?.Self is not BotMobile { Deleted: false } body)
        {
            return null;
        }

        if (body.Map == null || body.Map == Map.Internal || body.Guild is not Guild guild)
        {
            return null;
        }

        // The Shadow keeps no house, no counter and no hired hands: its home is a fire and a chest far outside every
        // town, and a band with a signboard has stopped being secret. See BotUnderworld.Band.
        if (BotUnderworld.Band(guild))
        {
            return null;
        }

        var hall = BotEstate.Hall(guild);

        if (hall is not { Deleted: false } || hall.Map != body.Map)
        {
            return null;
        }

        if (BotEstate.Merchants(hall) >= BotEstate.MostMerchants)
        {
            Staffed++;

            return null;
        }

        // The tools first. A guild that has not finished fitting out its workshop has better uses for the
        // money than a shopkeeper to sell what it cannot yet make.
        if (BotFittings.Wanting(hall, guild, out _))
        {
            Fitting++;

            return null;
        }

        var now = Core.TickCount;

        if (BotOffice.Busy(Office, guild))
        {
            Claimed++;

            return null;
        }

        var shop = BotShops.Nearest(bot, typeof(ContractOfEmployment));

        if (shop == null)
        {
            Unsold++;

            return null;
        }

        var price = BotShops.Price(shop, typeof(ContractOfEmployment));

        if (price <= 0)
        {
            Unsold++;

            return null;
        }

        // A guild whose levy fell short at the counter is not offered again until its rest is over. See Rest.
        if (_rested.TryGetValue(guild.Name, out var until))
        {
            if (now - until < 0)
            {
                Resting++;

                return null;
            }

            _rested.Remove(guild.Name);
        }

        var fund = BotEstate.Fund(guild);

        if (fund < price)
        {
            Wanting++;

            if (fund > Nearest)
            {
                Nearest = fund;
                Short = price - fund;
            }

            return null;
        }

        Offered++;
        BotOffice.Offering(Office, guild);

        return new BotHire(guild, hall, shop, price);
    }

    /// <summary>The errand is alive and still on it. Called every beat by <c>BotHire.Advance</c>.</summary>
    public static void Hold(Guild guild) => BotOffice.Hold(Office, guild, ClaimMs);

    /// <summary>The errand is over, however it went.</summary>
    public static void Release(Guild guild) => BotOffice.Release(Office, guild);

    /// <summary>
    /// How long a guild whose levy fell short at the counter is left alone before a merchant is offered again.
    ///
    /// <para>
    /// <b>A refusal that is not remembered is asked again on the next beat (06.10.2026, work15).</b> The office claim is
    /// released when the errand fails, so the next free member of the same guild was offered the same contract at once:
    /// Aerendyl failed five times in one second at 01:08:40. Ten minutes is about what dues and work add to a chest.
    /// </para>
    /// </summary>
    public static int RestMs { get; set; } = 600000;

    private static readonly Dictionary<string, long> _rested = new();

    /// <summary>Guilds rested after their levy fell short at the counter, and offers held off while one rested.</summary>
    public static long Rests { get; private set; }

    public static long Resting { get; private set; }

    /// <summary>The guild could not pay at the counter: no offer to it until <see cref="RestMs"/> has passed.</summary>
    public static void Rest(Guild guild)
    {
        if (guild?.Name == null)
        {
            return;
        }

        _rested[guild.Name] = Core.TickCount + RestMs;
        Rests++;
    }

    /// <summary>Part of the estate's line.</summary>
    public static string Describe() =>
        $"the hirer offered a merchant {Offered} times, held off {Wanting} times for want of the money"
        + (Wanting > 0 ? $" (the best-off guild that wanted one had {Nearest}gp free and was {Short} short)" : "")
        + $" and {Unsold} times for want of anybody selling a contract; "
        + $"{Staffed} halls already had their merchant, {Fitting} guilds were still fitting out a workshop first, {Claimed} already had somebody fetching one; "
        + $"{Rests} guilds rested after a levy fell short at the counter, {Resting} offers held off while one rested";

    public static void Forget()
    {
        Rests = 0;
        Resting = 0;
        _rested.Clear();
        Offered = 0;
        Wanting = 0;
        Unsold = 0;
        Short = 0;
        Nearest = 0;
        Staffed = 0;
        Fitting = 0;
        Claimed = 0;
    }
}
