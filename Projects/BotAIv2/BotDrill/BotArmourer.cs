using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Puts an order on the board for the best piece of armour this bot is not wearing.
///
/// <para>
/// <b>Nobody on this shard wore armour, and nothing had ever noticed.</b> <c>BotOutfit</c> hands out a
/// shirt, a pair of trousers, boots and a weapon; there is no armour anywhere in the kit, so every bot on the
/// island fought skeletons in its shirtsleeves from the day the population was first raised. That was never
/// a fault in the outfitter — armour is meant to be made and bought, which is the whole point of having
/// smiths and tailors — but the demand side of it did not exist, so the smiths had nothing anybody wanted and
/// the bots had nothing on. Half the machinery was already written and idle: <c>Rearm</c> puts on any
/// <c>BaseArmor</c> it finds in a pack, <c>BotUpkeep</c> reads armour durability and reorders a worn piece,
/// and <c>BotClass.NeedsMeditation</c> was documented as "refuses armour that would stop it meditating" with
/// nothing in the world for it to refuse.
/// </para>
///
/// <para>
/// <b>What to want is asked rather than listed.</b> The first version of this file carried five hard-coded
/// ringmail types and gave a mage the same answer as a brawler. <see cref="BotHarness"/> reads the shard's
/// own craft systems instead, ranks by <em>harm stopped over a piece's life, per gold</em>, and lets each
/// bot spend in proportion to how often something has actually been hitting it. Those two together are what
/// send plate to the warrior in the graveyard and nothing at all to the miner.
/// </para>
///
/// <para>
/// <b>And it only ever wants what somebody can actually make.</b> A want nobody can fill is worse than an
/// empty board: it reads as demand, it holds escrow, and it teaches every seeker that orders are not worth
/// walking to. The best crafter alive is asked before an order is raised, so "nobody is good enough yet" is a
/// refusal with a name on it rather than a mystery on the market.
/// </para>
/// </summary>
public sealed class BotArmourer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotArmourer));

    /// <summary>
    /// The most a bot must still have left after paying for a piece.
    ///
    /// <para>
    /// Armour is the first thing on this shard a bot buys that it does not need <em>today</em>, so it is the
    /// first thing that can starve one. A bot with no arrows, no bandages and a fine hauberk has made itself
    /// worse, and this is the whole of what stops it.
    /// </para>
    ///
    /// <para>
    /// <b>A ceiling now rather than a flat rule, because as a flat rule it collided with a number in another
    /// file and the two of them together were a ban.</b> This stood at four hundred; a bot is born holding
    /// four hundred (<c>bot-population.json</c>, <c>Purse</c>); and the median purse on this shard is four
    /// hundred, which is to say half the population has never earned a coin beyond what it was given. So the
    /// rule read "keep back exactly what you were born with" and a leather sleeve costing ninety-six was out
    /// of reach of half the shard — 541 refusals of 1032 in one window, with the fattest purse among them at
    /// 603gp. Neither number was wrong. They had simply never been compared, which is the commonest defect
    /// on this project and the hardest to see, because both files look perfectly reasonable on their own.
    /// </para>
    ///
    /// <para>
    /// See <see cref="Keeps"/>: what is actually kept back is the smaller of this and the price of the thing
    /// being bought. A ninety-six gold sleeve asks a bot to have a hundred and ninety-two, which a working
    /// bot has; a three hundred gold breastplate still asks for six hundred, so the dear pieces go on
    /// meaning what they meant. The rule this expresses — never spend past your own footing — is kept, and
    /// it stops being a rule that only the rich can obey.
    /// </para>
    /// </summary>
    public static int Reserve { get; set; } = 400;

    /// <summary>
    /// What this bot must have left after paying <paramref name="price"/>. Never more than the price itself.
    /// </summary>
    public static int Keeps(int price) => Math.Min(Reserve, Math.Max(1, price));

    /// <summary>
    /// Most pieces one bot may have outstanding on the board at once.
    ///
    /// About the board rather than about the bot. Twenty bots each wanting six pieces is a hundred and twenty
    /// standing wants against four crafters, every one of them holding escrow — a market that looks busy and
    /// settles nothing.
    /// </summary>
    public static int MostOrders { get; set; } = 2;

    /// <summary>What ordering is reckoned at per minute. The same as any other trip to the board.</summary>
    public static double Prior { get; set; } = 30.0;

    private static bool _said;

    public string Name => "Armourer";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    public static long Covered { get; private set; }

    public static long Standing { get; private set; }

    public static long Unmakeable { get; private set; }

    /// <summary>Bots nothing has laid a finger on lately, for whom armour is a hypothetical.</summary>
    public static long Unbloodied { get; private set; }

    public static long Broke { get; private set; }

    /// <summary>Pieces of armour a guild paid for because the wearer could not. See BotGuilds.Stand.</summary>
    public static long Guilded { get; private set; }

    /// <summary>
    /// The fattest purse among those turned away for want of money. See <see cref="BotStable.Richest"/>.
    /// </summary>
    public static long Richest { get; private set; }

    /// <summary>
    /// Pieces <em>offered for</em>, which is not orders placed on the board.
    ///
    /// <para>
    /// <b>Named for the outcome while counting the attempt, and that is the third time in one day.</b> This
    /// read "22 orders raised" against exactly two orders that ever reached the board: a proposer hands the
    /// auction a deed and the auction takes what it likes, and an errand to post a want is reckoned at
    /// thirty a minute against a hunt's ninety. Most of these are simply outbid, which is correct behaviour
    /// and a terrible thing to call an order. A counter is a sentence, and this one was a false one.
    /// </para>
    /// </summary>
    public static long Offers { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || body is not BotMobile wearer)
        {
            return null;
        }

        Asked++;

        if (Outstanding(bot) >= MostOrders)
        {
            Standing++;

            return null;
        }

        // What a quiet life is worth. A bot nothing has touched in an hour is not offered armour at all —
        // see BotHarness.Purse.
        var purse = BotHarness.Purse(wearer);

        if (purse <= 0)
        {
            Unbloodied++;

            return null;
        }

        var (piece, bare, unmakeable) = Wanted(bot, wearer, purse);

        if (piece == null)
        {
            if (unmakeable)
            {
                Unmakeable++;
            }
            else if (bare)
            {
                Standing++;
            }
            else
            {
                Covered++;
            }

            return null;
        }

        // Twice the material, which is the opening ask and not the price: the market moves it from there, the
        // same way an ingot's does. A crafter has to be paid for the skill as well as for the iron, or nobody
        // takes the order. The material at what it goes for now (03.10.2026, build next): the market's reckoning
        // no longer lifts this to another buyer's raised offer, so the stand-in price of a hide would be the
        // opening itself. See BotHarness.Piece.Making.
        var offer = BotMarket.Worth(piece.Kind, piece.Making());

        // <b>Pack and bank together, and reading only the pack made this dead on arrival.</b> Twenty-one
        // looks at the captain's armour in five minutes and twenty-one refusals for want of money — while it
        // had thirteen hundred gold of lesson fees sitting in the bank, because every seller on this shard is
        // paid by deposit. BotYield.Wealth is the shard's own answer to "what can this bot actually spend".
        var wealth = BotYield.Spendable(body);

        if (wealth - offer <= Keeps(offer))
        {
            // <b>Before giving up: ask the guild.</b> Patrick's order of 09.09.2026 is that a guild keeps its
            // own equipped, and this is the seam it works through — the member is short, the guild stands the
            // difference, the member orders its own armour off the same board as everybody else, and the
            // crafter most likely to fill that order is the guild's own. See BotGuilds.Stand.
            var owing = offer + Keeps(offer) - wealth;

            if (BotGuilds.Stand(wearer, owing))
            {
                Guilded++;

                // Handed over by the guild while the wearer is in the middle of something else, and not that
                // errand's takings. See BotYield.Aside.
                BotYield.Aside(wearer, -owing);
            }
            else
            {
                Broke++;

                if (wealth > Richest)
                {
                    Richest = wealth;
                }

                return null;
            }
        }

        Offers++;

        Once(body, piece, offer);

        return BotOrder.For(map, body.Location, bot, piece.Kind, offer);
    }

    /// <summary>
    /// The best piece this bot is neither wearing nor already waiting for, and why not when there is none.
    ///
    /// <para>
    /// <b>The standing-order check belongs inside this walk, and putting it outside blocked the queue.</b>
    /// Written as "find the first bare slot; if it is already on the board, do nothing", a bot stops dead at
    /// the first piece nobody can make. A close helm wants 37.9 blacksmithy against ringmail's 12 to 22, so a
    /// smith halfway up the ladder can forge four of the five — and the bot would have ordered the helm,
    /// waited for it for ever, and never once asked for the arms and gloves it could have had that afternoon.
    /// One unfillable want at the head of a list must not cancel the rest of the list.
    /// </para>
    /// </summary>
    private static (BotHarness.Piece Piece, bool Bare, bool Unmakeable) Wanted(IBotWilful bot, BotMobile wearer, int purse)
    {
        var layers = BotHarness.Layers;
        var bare = false;
        var unmakeable = false;

        for (var i = 0; i < layers.Count; i++)
        {
            var where = layers[i];

            // Armour on the layer, not merely something on the layer: a bot wearing a shirt is not wearing a
            // hauberk, and the outfitter dresses every one of them in a shirt.
            if (wearer.FindItemOnLayer(where) is BaseArmor)
            {
                continue;
            }

            bare = true;

            var best = BotHarness.Best(wearer, where, BotHarness.Ablest, purse);

            if (best == null)
            {
                // Something belongs here and nothing on this island is good enough to make it yet. A
                // different fact from "already ordered", and the two were one number until a summary was
                // read and could not say which had happened.
                unmakeable |= BotHarness.Best(wearer, where, null, purse) != null;

                continue;
            }

            if (Carrying(wearer, best.Kind) || BotMarket.Wanted(bot, best.Kind) != null)
            {
                continue;
            }

            return (best, true, false);
        }

        return (null, bare, unmakeable);
    }

    /// <summary>How many wants this bot already has standing on the board, of any kind.</summary>
    private static int Outstanding(IBotWilful bot)
    {
        var wants = BotMarket.Wants;
        var mine = 0;

        for (var i = 0; i < wants.Count; i++)
        {
            if (ReferenceEquals(wants[i].Buyer, bot))
            {
                mine++;
            }
        }

        return mine;
    }

    /// <summary>Whether the piece is already in the pack, waiting to be put on.</summary>
    private static bool Carrying(Mobile body, Type kind)
    {
        var pack = body.Backpack;

        if (pack == null)
        {
            return false;
        }

        var items = pack.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (kind.IsInstanceOfType(items[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static void Once(Mobile body, BotHarness.Piece piece, int offer)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first bot on this shard ever to want armour: a {Item} — {Rating:F0} of protection for {Cost}gp of material — offered at {Offer}gp",
            body.Name,
            piece.Kind.Name,
            piece.Rating,
            piece.Cost,
            offer
        );
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has ever looked at what it is wearing"
            : $"{Asked} looks at what a bot is wearing: {Offers} offered for (the auction takes what it takes), {Covered} were covered, {Standing} were already waiting on one, {Unmakeable} wanted something no crafter here is good enough to make, {Unbloodied} have not been hit lately enough to want any, {Broke} could not afford a piece and keep the price of it back, up to {Reserve}gp (the fattest purse among them held {Richest}gp); {BotHarness.Describe()}";

    public static void Forget()
    {
        _said = false;
        Asked = 0;
        Covered = 0;
        Standing = 0;
        Unmakeable = 0;
        Unbloodied = 0;
        Broke = 0;
        Richest = 0;
        Offers = 0;
    }
}
