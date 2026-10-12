using System;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot with a spoken want a walk to a bot's shop that sells the thing for no more than it says it will pay.
///
/// <para>
/// <b>The buyer's half of hand to hand.</b> A want is a buyer that has said aloud what it needs and what it will pay — a
/// tailor's leather, a cook's ribs, a warrior's replacement cap — and it is answered one of two ways: a carrier walks the thing
/// to it (<see cref="BotCourier"/>), or it walks to where the thing is on show. When an open shop within the bot's reach holds
/// the kind and its price, with the walk counted (<see cref="BotShopkeep.Best"/>), is at or under the want's own offer, the bot
/// is offered the walk; at the till it pays the keeper's price, never more than its offer, and the want comes down by what it
/// bought (<see cref="BotShopkeep.Visit"/>). What the shop could not supply is still wanted.
/// </para>
///
/// <para>
/// <b>A purchase, not work</b> (see <c>BotRestock</c>): the errand is the shopper's own, unpaid and claimed at what a shopping
/// trip is claimed at, so it is taken when nothing that produces anything is on offer and never over work in hand.
/// </para>
/// </summary>
public sealed class BotPatron : IBotProposer
{
    public string Name => "Patron";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>Bots looked at while a shop was open.</summary>
    public static long Looks { get; private set; }

    /// <summary>Of those, bots with an open want of their own.</summary>
    public static long Wanting { get; private set; }

    /// <summary>A shop held the thing, and its price with the walk came to more than the want offers.</summary>
    public static long Dearer { get; private set; }

    /// <summary>Walks to a shop offered.</summary>
    public static long Offered { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        // Nothing to walk to: the ordinary answer, and it costs a count.
        if (!BotShopkeep.Running || BotShopkeep.OpenCount == 0)
        {
            return null;
        }

        var body = bot?.Self;

        if (body is not { Alive: true } || body.Map == null || body.Map == Map.Internal)
        {
            return null;
        }

        Looks++;

        var wants = BotMarket.Wants;
        var mine = false;

        BotStorefront best = null;
        Type kind = null;
        var unit = 0;
        var units = 0;
        var saving = double.MinValue;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want == null || !ReferenceEquals(want.Buyer, bot) || !want.IsOpen)
            {
                continue;
            }

            mine = true;

            var front = BotShopkeep.Best(bot, want.Kind, want.Payable, out var ask, out var n, out var landed);

            if (front == null)
            {
                continue;
            }

            if (landed > want.Offer)
            {
                Dearer++;

                continue;
            }

            var save = (want.Offer - landed) * n;

            if (best != null && save <= saving)
            {
                continue;
            }

            best = front;
            kind = want.Kind;
            unit = ask;
            units = n;
            saving = save;
        }

        if (mine)
        {
            Wanting++;
        }

        if (best == null)
        {
            return null;
        }

        Offered++;

        return new BotRestock(best, kind, units, unit, true);
    }

    public static string Describe() =>
        Looks == 0
            ? "nobody has looked at a shop for a want on the board"
            : $"{Looks} looks at the shops for wants on the board: {Wanting} bots had a want open, {Offered} walks to a shop offered, {Dearer} times a shop held the thing dearer than the want offers";

    public static void Forget()
    {
        Looks = 0;
        Wanting = 0;
        Dearer = 0;
        Offered = 0;
    }
}
