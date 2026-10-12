using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Who decides the guild can afford its next workbench, and sends somebody to fetch it.
///
/// <para>
/// The same shape as <see cref="BotSteward"/>, one storey down: any member may go, one at a time per guild,
/// and the affordability is asked here rather than inside the work — because the money is the guild's and a
/// bot made to feel eight hundred gold short would refuse everything else on the shard until it stopped.
/// </para>
///
/// <para>
/// It is offered only where there is a hall to put the thing in, which makes it the first piece of work on
/// this shard that exists <em>because</em> the population built something.
/// </para>
/// </summary>
public sealed class BotFitter : IBotProposer
{
    /// <summary>How long a claim lasts after the bot holding it was last heard from.</summary>
    public static int ClaimMs { get; set; } = 120000;

    /// <summary>Benches offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Times a guild wanted one and could not pay for it.</summary>
    public static long Wanting { get; private set; }

    /// <summary>
    /// Times the hall already had everything its guild wants.
    ///
    /// <para>
    /// <b>The good answer, and it had no bucket, so it read as the bad one.</b> The line said "the fitter
    /// offered a bench 0 times and held off 0 times for want of the money; no benches have been bought" —
    /// two noughts and a nought, which is exactly what a proposer that is never asked would print. It took
    /// reading the wishlists to find that all four standing halls are in fact finished. A gate without a
    /// bucket makes the denominator a lie; this project has paid for that one twice in a day.
    /// </para>
    /// </summary>
    public static long Furnished { get; private set; }

    /// <summary>Times the bot had no hall to fit out, which is most of them.</summary>
    public static long Homeless { get; private set; }

    /// <summary>This officer's name in the shared register of who is on what. See <see cref="BotOffice"/>.</summary>
    public const string Office = "fitter";

    public string Name => "fitter";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || !BotFittings.Running || bot?.Self is not BotMobile { Deleted: false } body)
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
            Homeless++;

            return null;
        }

        if (!BotFittings.Wanting(hall, guild, out var bench))
        {
            Furnished++;

            return null;
        }

        var now = Core.TickCount;

        if (BotOffice.Busy(Office, guild))
        {
            return null;
        }

        // What the guild can raise without beggaring anybody. Asked after the wishlist, so a guild whose hall
        // is already fitted out is never measured at all.
        if (BotEstate.Fund(guild) < bench.Price)
        {
            Wanting++;

            return null;
        }

        Offered++;
        BotOffice.Offering(Office, guild);

        return new BotBench(guild, hall, bench);
    }

    /// <summary>The errand is alive and still on it. Called every beat by <c>BotBench.Advance</c>.</summary>
    public static void Hold(Guild guild) => BotOffice.Hold(Office, guild, ClaimMs);

    /// <summary>The errand is over, however it went.</summary>
    public static void Release(Guild guild) => BotOffice.Release(Office, guild);

    /// <summary>Part of the estate's line.</summary>
    public static string Describe() =>
        $"the fitter offered a bench {Offered} times, held off {Wanting} times for want of the money, "
        + $"found {Furnished} halls already holding everything their guild wants and {Homeless} bots with no hall at all; {BotFittings.Fittings()}";

    public static void Forget()
    {
        Offered = 0;
        Wanting = 0;
        Furnished = 0;
        Homeless = 0;
    }
}
