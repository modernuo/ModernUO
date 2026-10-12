using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Asking the population, by name, for a replacement for something that is wearing out.
///
/// <para>
/// <b>The board of what the population needs was empty for days, and this is the half that was missing.</b>
/// Everything on this shard could already <em>supply</em> a want — a miner selling ingots, a tailor selling
/// cloth and a scribe selling scrolls all check the board before they check a shopkeeper — and exactly one
/// thing in the whole assembly ever <em>raised</em> one. So the board held at most a handful of scroll
/// requests from mages completing their books, and a smith could have read it every minute of every session
/// without ever finding work. A market needs somebody to want something.
/// </para>
///
/// <para>
/// <b>What a bot may ask for is settled by two refusals, and both are the order of 24.08.2026.</b> It may not
/// ask for something it could not use, and it may not ask for something it could not pay for. The first is
/// answered without a table of what each class may wear: a bot asks for <em>another one of what it is already
/// wearing</em>, so the question "could this bot use it" has already been answered by the fact that it is
/// holding one. The second is the market's own rule — <see cref="BotMarket.Ask"/> refuses a want its buyer could
/// not pay for — with a reserve on top, because a bot that spends its last coin ordering a sword has
/// no coin left to be resurrected with.
/// </para>
/// </summary>
public sealed class BotUpkeep : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotUpkeep));

    /// <summary>
    /// The share of an item's life left at which a replacement is worth ordering.
    ///
    /// <para>
    /// Ordered before it breaks, not after. A weapon that shatters mid-fight leaves a bot swinging its fists
    /// against something that is already hitting it, and the order it would then place takes minutes to be
    /// filled — so the whole point is to be holding the new one before the old one goes. A third of the life
    /// left is several fights' worth of warning.
    /// </para>
    /// </summary>
    public static double Worn { get; set; } = 0.34;

    /// <summary>Coin a bot keeps back rather than spending on gear: <see cref="BotPurse.KeepBack"/> unless dialled apart. It was 200.</summary>
    public static int Reserve
    {
        get => _reserve ?? BotPurse.KeepBack;
        set => _reserve = value;
    }

    private static int? _reserve;

    /// <summary>What ordering a replacement is reckoned at per minute before the ledger corrects it.</summary>
    public static double Prior { get; set; } = 30.0;

    /// <summary>What a piece of gear is guessed to be worth when nothing on the shard has priced one.</summary>
    public static int Guess { get; set; } = 60;

    private static bool _said;

    /// <summary>Every gate, counted apart. There is no bucket called "other".</summary>
    public static long Asked { get; private set; }

    public static long Sound { get; private set; }

    public static long Broke { get; private set; }

    public static long Standing { get; private set; }

    public static long Raised { get; private set; }

    /// <summary>Bots that already had something on the way. See the note in <see cref="Propose"/>.</summary>
    public static long Waiting { get; private set; }

    /// <summary>Asks turned away for coming again inside the minute. See BotNeeds.</summary>
    public static long Soon { get; private set; }

    /// <summary>
    /// The most worn thing anybody has been seen wearing, as a percentage of its life, and what it was.
    ///
    /// <para>
    /// <b>A nought beside an unset maximum is unreadable, and this counter has been printing one for
    /// weeks.</b> "60203 asked, 60203 are carrying nothing worn out" over four hours says the gate never
    /// opened; it does not say whether that is because nothing on this shard ever wears — a weapon that is
    /// re-issued, a mender that repairs, a layer nobody is reading — or because gear wears perfectly well
    /// and never reaches a third of its life inside one session. Those are different faults with different
    /// repairs, and the shard has spent a fortnight unable to tell them apart. The same correction
    /// BotShopper.Richest and BotStable.Richest were given.
    /// </para>
    /// </summary>
    public static double Worst { get; private set; } = 1.0;

    /// <summary>What that thing was, so the answer names something rather than a number.</summary>
    public static string WorstKind { get; private set; }

    /// <summary>Pieces of gear looked at. Zero here means nothing is being read at all, which is its own fault.</summary>
    public static long Looked { get; private set; }

    public string Name => "Upkeep";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        Asked++;

        // Once a minute per bot, on the same clock the rest of the needs keep. See BotNeeds. Counted, or
        // the gates below stop adding up to Asked.
        if (!BotNeeds.Due(body, "gear"))
        {
            Soon++;

            return null;
        }

        // <b>A bot with something on its way does not order a second one.</b> Under the auction that was goods paid for and waiting
        // on the board; now it is a carrier bringing what was asked for (01.10.2026): the want stands, units reserved on it.
        if (Coming(bot))
        {
            Waiting++;

            return null;
        }

        var tired = Failing(body);

        if (tired == null)
        {
            Sound++;

            return null;
        }

        var kind = tired.GetType();

        // Already on the board under this bot's name. The want raises its own offer on the market's beat and
        // gives up at the ceiling; asking again would only turn one order into nine.
        if (BotMarket.Wanted(bot, kind) != null)
        {
            Standing++;

            return null;
        }

        var offer = BotMarket.Worth(kind, Guess);

        if ((body.Backpack?.TotalGold ?? 0) - offer <= Reserve)
        {
            Broke++;

            return null;
        }

        Raised++;
        Once(body, tired);

        return BotOrder.For(map, body.Location, bot, kind, offer);
    }

    /// <summary>Whether a carrier is bringing this bot something it asked for.</summary>
    private static bool Coming(IBotWilful bot)
    {
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            if (ReferenceEquals(wants[i].Buyer, bot) && wants[i].Coming > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The worn-out thing this bot is wearing, or null.
    ///
    /// <para>
    /// Worn rather than owned: only what is on the body counts, and that is the wearability rule doing its
    /// work. A pickaxe in the pack is a tool and has its own shopping; the blade in a bot's hand and the mail
    /// on its back are the things whose failure is a fight lost.
    /// </para>
    ///
    /// <para>
    /// The worst first, so a bot with a nearly-dead sword and slightly scuffed boots orders the sword.
    /// </para>
    /// </summary>
    private static Item Failing(Mobile body)
    {
        Item worst = null;
        var least = 1.0;

        foreach (var item in body.Items)
        {
            var (now, max) = Life(item);

            if (max <= 0)
            {
                continue;
            }

            Looked++;

            var left = now / (double)max;

            // Recorded before the threshold is applied, which is the whole point of it: what the gate
            // refuses is exactly what nobody could see.
            if (left < Worst)
            {
                Worst = left;
                WorstKind = $"{item.GetType().Name} at {now} of {max}";
            }

            if (left > Worn || left >= least)
            {
                continue;
            }

            least = left;
            worst = item;
        }

        return worst;
    }

    /// <summary>How much life a thing has, and how much it had new. Zeroes for anything that does not wear out.</summary>
    private static (int Now, int Max) Life(Item item) =>
        item switch
        {
            BaseWeapon weapon => (weapon.HitPoints, weapon.MaxHitPoints),
            BaseArmor armor => (armor.HitPoints, armor.MaxHitPoints),
            _ => (0, 0)
        };

    private static void Once(Mobile body, Item tired)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        var (now, max) = Life(tired);

        logger.Information(
            "{Name} is the first to put an order on the board: its {Item} is down to {Now} of {Max} and it wants another",
            body.Name,
            tired.GetType().Name,
            now,
            max
        );
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been asked about their gear"
            : $"{Asked} asked: {Raised} ordered a replacement, {Waiting} already had one on the way, {Standing} already have one on order, "
              + $"{Broke} could not afford one, {Soon} asked again inside the minute, {Sound} are carrying nothing worn out (of {Looked} pieces read, "
              + $"the most worn was {WorstKind ?? "nothing at all"} — {(int)(Worst * 100)}% of its life left, against a bar of {(int)(Worn * 100)}%)";

    public static void Forget()
    {
        _said = false;
        Asked = 0;
        Sound = 0;
        Broke = 0;
        Standing = 0;
        Raised = 0;
        Waiting = 0;
        Soon = 0;
        Worst = 1.0;
        WorstKind = null;
        Looked = 0;
    }
}
