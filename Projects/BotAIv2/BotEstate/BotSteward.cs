using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Who decides the guild can afford a hall, and offers to go and raise one.
///
/// <para>
/// <b>Any member may go and raise it, and the guild's own claim is what stops two of them doing it.</b>
/// This was written leader-only at first, on the reasoning that one bot cannot race itself — and it was a
/// veto wearing a preference's clothes. The Crown could pay for a hall from its first minute and never
/// bought one: its leader is the Captain, the Captain joins a company, and a bot on the <c>Bound</c> rung
/// has its auction switched off entirely. Six minutes of a running shard with 32 bots busy, 17 bound and
/// none free went by with the money in hand and nobody able to be offered the work. A guild of two whose
/// leader is in a company is a guild that can never build anything.
/// </para>
///
/// <para>
/// So the offer goes to whoever of the guild is choosing work, and the claim below keeps it to one at a
/// time: a claim expires by the clock rather than by an event, because the bot holding it may die, drop
/// the errand or be taken into a company, and none of those three sends anybody a message.
/// </para>
///
/// <para>
/// <b>What it can afford is asked here rather than in the work.</b> An undertaking that says it needs five
/// thousand gold makes its bot feel destitute — <c>BotDeed.Outlay</c> is what poverty is measured against —
/// and the money is not the bot's anyway. So the affordability test lives on this side of the offer, where
/// it costs the bot nothing to fail.
/// </para>
///
/// <para>
/// The plot search is throttled because it is the expensive half: every candidate is a full
/// <c>HousePlacement.Check</c>, and this is asked on the beat of every free leader on the shard.
/// </para>
/// </summary>
public sealed class BotSteward : IBotProposer
{
    /// <summary>How often anybody may go looking for ground.</summary>
    public static int LookMs { get; set; } = 2000;

    /// <summary>
    /// The share of the price at which a guild starts looking for somewhere to build.
    ///
    /// <para>
    /// Below the price, on purpose. Ground and money accumulate on two different clocks, and gating the
    /// search on the money made them wait for each other: one guild of two members could pay, so two bots
    /// out of forty-nine were doing all the looking, and the island was being examined at a fiftieth of the
    /// rate it could have been. Looking costs the engine a few tile queries and commits nobody to anything.
    /// </para>
    /// </summary>
    public static double LookAt { get; set; } = 0.5;

    /// <summary>
    /// How long a claim lasts after the last time the bot holding it was heard from.
    ///
    /// <para>
    /// <b>Renewed by the errand itself, so this is a timeout and not a deadline.</b> Written as a deadline
    /// it expired mid-walk: Fenna took the Blade's hall at 22:16 and was still eighty tiles short of the
    /// plot when the three minutes ran out, so Ilsa was handed the same errand and walked two minutes to be
    /// told the hall was already up. The bot that is actually working keeps its claim; a bot that has died,
    /// dropped it or been taken into a company stops renewing and the guild is free again.
    /// </para>
    /// </summary>
    public static int ClaimMs { get; set; } = 180000;

    /// <summary>Halls offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Times a guild could pay and no ground could be found.</summary>
    public static long Groundless { get; private set; }

    /// <summary>Times ground was in hand and the guild could not yet pay for what would stand on it.</summary>
    public static long Waiting { get; private set; }

    private static long _next;

    private static bool _looked;

    /// <summary>Which guilds have somebody on it, and until when.</summary>

    public string Name => "steward";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || bot?.Self is not BotMobile { Deleted: false } body)
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

        if (BotEstate.Hall(guild) != null)
        {
            return null;
        }

        if (BotEstate.Standing >= BotEstate.MaxHalls)
        {
            return null;
        }

        var fund = BotEstate.Fund(guild);

        if (fund < BotEstate.Price * LookAt)
        {
            return null;
        }

        var now = Core.TickCount;

        if (Claimed(guild, now))
        {
            return null;
        }

        // Subtraction, and a flag rather than a zero: a tick count on some hosts starts enormous and wraps
        // negative, so "now < _next" is wrong twice a day and "_next == 0 means never looked" is wrong once.
        if (_looked && now - _next < 0)
        {
            return null;
        }

        _looked = true;
        _next = now + LookMs;

        // From the guild's own seat, which is where it lives. See BotSeat: nought means the population's home.
        if (!BotPlot.Find(body, BotSeat.Of(guild), out var plot))
        {
            Groundless++;

            return null;
        }

        // The ground is known and remembered. Whether this guild can pay for it is a separate question, and
        // it is asked here rather than above so that a guild halfway to the price still does the looking.
        if (fund < BotEstate.Price)
        {
            Waiting++;

            return null;
        }

        Offered++;
        BotOffice.Offering(Office, guild);

        return new BotHall(guild, body.Map, plot);
    }

    /// <summary>This officer's name in the shared register of who is on what. See <see cref="BotOffice"/>.</summary>
    public const string Office = "steward";

    /// <summary>Whether somebody of this guild is already away raising its hall.</summary>
    private static bool Claimed(Guild guild, long now) => BotOffice.Busy(Office, guild);

    /// <summary>
    /// The errand is alive and still on it. Called every beat by <c>BotHall.Advance</c>, which is what turns
    /// the claim from a deadline into a timeout.
    /// </summary>
    public static void Hold(Guild guild) => BotOffice.Hold(Office, guild, ClaimMs);

    /// <summary>
    /// The errand is over, however it went, so the guild is free to try again.
    ///
    /// Called from <c>BotHall.Drop</c>, which the decision layer calls for every ending there is — done,
    /// failed, dropped, dead. A claim released only on success is a claim that becomes permanent the first
    /// time a bot is killed walking to a plot.
    /// </summary>
    public static void Release(Guild guild) => BotOffice.Release(Office, guild);

    /// <summary>Part of the estate's line: what the offer side of this has been doing.</summary>
    public static string Describe() =>
        $"the steward offered a hall {Offered} times, held off {Waiting} times with the ground in hand and the money not, and found nothing to build on {Groundless} times";

    public static void Forget()
    {
        Offered = 0;
        Groundless = 0;
        Waiting = 0;
        _next = 0;
        _looked = false;
    }
}
