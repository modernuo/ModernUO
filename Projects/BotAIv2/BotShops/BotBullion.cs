using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A crafter with money buys its metal instead of going and digging it.
///
/// <para>
/// <b>Digging is the cheapest way to get metal and very nearly the worst.</b> A mining trip is eight minutes
/// of walking to a seam, swinging at it, carrying rock to a fire and the metal to a counter — and at the end
/// of it a crafter has twenty ingots and has spent the eight minutes not crafting. Its skill is in the forge,
/// not in the rock. So once there is coin in the purse the sensible thing is to let somebody whose trade
/// <em>is</em> the rock do the walking, and pay them for it.
/// </para>
///
/// <para>
/// <b>It buys by asking rather than by shopping, and that is the whole elegance of it.</b> An order on the
/// board is read by every miner on the shard before it banks a single ingot — the carrier (<see cref="BotCourier"/>) weighs
/// every want against what a miner holds, and walks the ingots over. So a crafter putting
/// up money for metal does not merely acquire metal: it redirects the population's miners onto the thing the
/// population is short of, without anybody being told anything. That is the same trick as everything else
/// here — a fact left where others will pass it, and arithmetic on the far side.
/// </para>
///
/// <para>
/// Ordered on 24.08.2026: <em>if a crafter has a decent amount of money, say more than eight hundred to a
/// thousand, let it simply buy the ore rather than walk after it — buying and smelting is faster, and for a
/// crafter that is exactly right.</em>
/// </para>
/// </summary>
public sealed class BotBullion : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBullion));

    /// <summary>
    /// What a bot must still have left <em>after</em> paying for the metal: the blade it is wearing out, the
    /// bandages it heals with, the scrolls it fights with.
    ///
    /// <para>
    /// <b>This used to be an absolute bar of nine hundred, and it shut the forge for an entire night.</b> The
    /// reasoning was sound and the arithmetic was never done: a batch is twenty ingots offered at eight, so
    /// the order costs a hundred and sixty — and the bar stood at five and a half times the bill. Over eight
    /// hours the population banked a hundred and twenty-four times, averaging a hundred and thirty-nine gold,
    /// and broke a thousand exactly once. The summary read "0 ordered metal, 3412 cannot afford to buy it"
    /// window after window while the smith sat at the same thirty-three pieces it had forged before dawn.
    /// </para>
    ///
    /// <para>
    /// A reserve rather than a bar, so the two numbers on this shelf cannot drift apart again: whatever a
    /// batch happens to cost, what is asked is that cost plus enough to go on living.
    /// </para>
    ///
    /// <para>
    /// <b>A hundred and fifty, and the first attempt at this put it at two hundred and fifty by eye and
    /// landed ten gold above where crafters plateau.</b> A bot banks everything over its working float
    /// whenever the pack reaches BotUnload.Purse, so it cycles between a hundred and about four hundred all
    /// told — and a batch at a hundred and sixty plus a reserve of two hundred and fifty comes to four
    /// hundred and ten. Sixteen minutes of "0 ordered metal, 248 cannot afford to buy it" for want of ten
    /// coins. Set instead to what a bot's living actually costs: a bolt of cloth is forty, a pack of bandages
    /// a hundred, and what is left over buys the scrolls and reagents in between.
    /// </para>
    ///
    /// <para>
    /// <see cref="BotPurse.KeepBack"/> since 15.09.2026 unless dialled apart: one number for one fact. It was 150.
    /// </para>
    /// </summary>
    public static int Reserve
    {
        get => _reserve ?? BotPurse.KeepBack;
        set => _reserve = value;
    }

    private static int? _reserve;

    /// <summary>How many ingots are enough to be worth working with. Below this it is worth ordering more.</summary>
    public static int Enough { get; set; } = 20;

    /// <summary>How many are ordered at a time when the purse allows it.</summary>
    public static int Batch { get; set; } = 20;

    /// <summary>The fewest worth putting on the board at all. See the note where it is used.</summary>
    public static int Least { get; set; } = 6;

    /// <summary>What an ingot is offered at when nothing on the shard has priced one.</summary>
    public static int Guess { get; set; } = 8;

    /// <summary>What this is reckoned at per minute. Low: it is one beat's work and then waiting.</summary>
    public static double Prior { get; set; } = 20.0;

    private static bool _said;

    /// <summary>Every gate apart, and the denominator with them.</summary>
    public static long Asked { get; private set; }

    public static long Poor { get; private set; }

    /// <summary>Asks turned away for coming again inside the minute. See BotNeeds.</summary>
    public static long Soon { get; private set; }

    public static long Stocked { get; private set; }

    public static long Standing { get; private set; }

    /// <summary>
    /// Smiths short of iron in the pack that took their own out of their bank box at a bank instead of ordering it.
    ///
    /// <para>
    /// <b>A bot cannot buy from itself.</b> Thirty-one failed orders for iron in one hour on 26.08.2026 came from three smiths
    /// every one of them selling its own ingots on a stall. The stall is gone (01.10.2026); the bank box is where a smith's own
    /// metal lies now, and it is reached at a bank (<see cref="BotMarket.FromBank"/>).
    /// </para>
    /// </summary>
    public static long Shelved { get; private set; }

    public static long Ordered { get; private set; }

    /// <summary>Metal orders bid at a glut's surplus price rather than the island's. See <c>Propose</c> (build 388).</summary>
    public static long GlutBids { get; private set; }

    /// <summary>Metal orders put in a coloured metal the island's bank boxes hold in bulk (06.10.2026, box16). See <see cref="BotAnvil.Order"/>.</summary>
    public static long FromBoxes { get; private set; }

    /// <summary>Metal orders sized past <see cref="Batch"/> to the order a smith passed over (build next). See <see cref="BotSmith.Short"/>.</summary>
    public static long ForOrders { get; private set; }

    public string Name => "Bullion";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Only for bots whose trade eats metal. A warrior with a thousand gold has better uses for it, and a
        // gatherer buying ore would be buying its own job. Asked of the kit rather than of the class name, so
        // any class that picks up a hammer later is included without this file being edited.
        if (!Smiths(bot.Class))
        {
            return null;
        }

        Asked++;

        // Once a minute per bot, on the same clock the rest of the needs keep. See BotNeeds. Counted, or
        // the gates below stop adding up to Asked.
        if (!BotNeeds.Due(body, "metal"))
        {
            Soon++;

            return null;
        }

        // <b>What it is short of, not what the file says.</b> A smith with eighty Blacksmithy works bronze,
        // and ordering iron while forty bronze ingots sat in the pack was a bot buying what it did not need
        // with money it wanted for what it did. Best answers with the dearest metal the bot can both use and
        // fill a batch from; when it cannot fill any, that answer is iron and iron is what gets ordered.
        // <b>And when the pack fills no batch, a coloured metal the island's bank boxes hold in bulk (06.10.2026, box16).</b> See
        // BotAnvil.Order: ten thousand coloured bars lay in boxes and an empty smith ordered iron.
        var kind = BotAnvil.Order(body, Batch, out var boxed);

        var carried = body.Backpack?.GetAmount(kind) ?? 0;

        // <b>Enough for the order it passed over, when that is more (03.10.2026, build next).</b> An order is taken with two pieces'
        // worth in the pack (36 ingots for chain legs) and this stocked a smith to twenty. See BotSmith.Short.
        var forOrder = BotSmith.Short(body);
        var enough = Math.Max(Enough, forOrder);

        if (carried >= enough)
        {
            Stocked++;

            return null;
        }

        // Its own iron is iron. Asked with the market's own question rather than a second one of this file's
        // own, taken out of its own bank box when it stands at a bank.
        if (BotMarket.FromBank(bot, kind, enough - carried) > 0)
        {
            Shelved++;

            return null;
        }

        // Its own metal of any kind it works before an order for the boxes' metal: what Best's fallback took out of the box before.
        if (boxed && BotAnvil.Fetch(bot, body, enough - carried) > 0)
        {
            Shelved++;

            return null;
        }

        if (BotMarket.Wanted(bot, kind) != null)
        {
            Standing++;

            return null;
        }

        // A coloured bar at the price the shops open it at (01.10.2026): no shopkeeper prices one, so the same reckoning stands on
        // both sides of the meeting and a smith's offer is never under the bar it can work. Iron as before. See BotAnvil.Reckon.
        var offer = BotMarket.Worth(kind, Math.Max(Guess, BotAnvil.Reckon(kind)));

        // <b>A glutted metal is bid for at its surplus price (01.10.2026, build 388; the Opus dossier of 386, row 18).</b> Iron
        // ingots stood at 154–303 piled against 0 asked, at the balance's floor of ×0.10, through the whole of 386 and 387 —
        // while the smiths read "887 cannot afford to buy it" and "1878 short of metal", every one of the seven crafters under
        // a hundred gold. The pile is worth a tenth to the miners holding it (BotGlut.Yield counts it so), and a smith bidding
        // the island's full price for it was the one party on the shard not reading the balance: a batch of twenty at eight
        // asked a hundred and sixty of a purse that had sixty. Bid at the surplus price, the same batch asks sixteen, the
        // carriers walk the miners' pile to the forge (BotCourier.LeastGold is eight a delivery), and the glut drains into
        // armour. Not a handout: the smith pays what the metal is worth today.
        var factor = BotGlut.Enabled ? BotGlut.Factor(kind) : 1.0;

        if (factor < 1.0)
        {
            offer = Math.Max(1, (int)Math.Round(offer * factor));
            GlutBids++;
        }

        // <b>Measured against everything the bot owns, not against what happens to be in the pocket.</b> The
        // price this order is paid at the meeting by BotMarket.Charge, which spends the pack and then reaches
        // into the account — so asking the pack alone asks a question nobody is answering. It mattered from
        // the moment the purse stopped being emptied into the bank on every trip to a counter: a bot keeps a
        // hundred on it by design now, so a pack-only test could never clear any bar worth having.
        // <b>As many as the purse will carry, down to a floor, rather than a batch of twenty or nothing.</b>
        // A fixed batch against a fixed reserve is a multiplier with no floor, and a multiplier with no floor
        // is a veto: at 12:01 on 04.09.2026 this read "157 cannot afford to buy it" out of 288, while the
        // same smiths could have paid for six ingots and gone to work. The twin of this correction is in
        // BotStores, on the trades that eat feather, wood and hide.
        var wealth = BotYield.Spendable(body);
        var afford = offer <= 0 ? 0 : (wealth - Reserve) / offer;
        var units = Math.Min(Math.Max(Batch, enough - carried), afford);

        if (units < Least)
        {
            Poor++;

            return null;
        }

        if (enough > Enough && carried + Batch < enough)
        {
            ForOrders++;
        }

        Ordered++;

        if (boxed)
        {
            FromBoxes++;
        }

        Once(body);

        return BotOrder.For(map, body.Location, bot, kind, offer, units);
    }

    /// <summary>Whether this class carries a hammer, which is the only definition of a smith worth having.</summary>
    private static bool Smiths(BotClass klass)
    {
        var tools = klass?.Kit?.Tools;

        for (var i = 0; tools != null && i < tools.Count; i++)
        {
            if (tools[i] == typeof(SmithHammer))
            {
                return true;
            }
        }

        return false;
    }

    private static void Once(Mobile body)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} has money enough to buy its metal rather than dig it, and has put the order to the miners",
            body.Name
        );
    }

    public static string Describe() =>
        Asked == 0
            ? "no crafter has been asked about metal"
            : $"{Asked} asked: {Ordered} ordered metal ({FromBoxes} of them in a coloured metal the bank boxes hold {BotAnvil.BoxedLeast} bars or more of, {GlutBids} bid at a glut's surplus price, {ForOrders} sized to an armour order the smith had passed over), {Standing} already have an order out, {Stocked} have {Enough} ingots already (or what the order they passed over takes), {Shelved} took their own out of their bank box at a bank, {Poor} cannot afford to buy it, {Soon} asked again inside the minute";

    public static void Forget()
    {
        _said = false;
        GlutBids = 0;
        FromBoxes = 0;
        ForOrders = 0;
        Asked = 0;
        Poor = 0;
        Soon = 0;
        Stocked = 0;
        Shelved = 0;
        Standing = 0;
        Ordered = 0;
    }
}
