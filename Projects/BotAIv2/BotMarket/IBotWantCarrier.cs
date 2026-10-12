using System;

namespace Server.BotAI.V2;

/// <summary>
/// Work that has reserved units on a spoken want and is bringing them: the carrier's walk (<see cref="BotDeliver"/>) and the
/// trader's haul (<c>BotHaul</c>). The market's sweep (<see cref="BotWant.Sweep"/>) keeps a reservation only while the
/// courier's work in hand, or the work it has put down, is one of these and still carries that want.
///
/// <para>
/// <b>01.10.2026, build 385 (traders).</b> The sweep asked for a <see cref="BotDeliver"/> by type, so any other work that
/// reserved units had its reservation swept on the next beat and the want stood open to a second carrier for the same
/// goods. One interface instead of a second type test, so the market's core names no trade outside itself.
/// </para>
/// </summary>
public interface IBotWantCarrier
{
    /// <summary>Whether this work is bringing goods against this want.</summary>
    bool Carries(BotWant want);

    /// <summary>Put down for longer than its own give-up clock: not coming for now. See <see cref="BotDeliver.Stale"/>.</summary>
    bool Stale { get; }

    /// <summary>
    /// The kind this work is carrying for a buyer. Read by <see cref="BotHandover.Promised"/> so the carrier's own body does not
    /// put the goods on (01.10.2026, build 386): a trader wore the leather skirt it had bought for an order and struck the order
    /// "nothing left to hand over" — 22 of 41 failed hauls and 5 of 17 failed deliveries on build 386 ended that way.
    /// </summary>
    Type Goods { get; }
}
