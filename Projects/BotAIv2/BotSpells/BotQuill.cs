using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Writing scrolls: what can be written, what it takes, and the attempt itself.
///
/// <para>
/// <b>Everything goes through the shard's own <see cref="CraftSystem"/></b>, exactly as sewing does — the same
/// call a player's craft window makes. The skill check, the failure, the blank and the herbs burnt on a bad
/// attempt and the scroll that appears are all the engine's, so the Inscribe a scribe gains here is gained by
/// inscribing rather than by being credited.
/// </para>
///
/// <para>
/// <b>It is the same shape as the needle and it needs no place either.</b> A pen works where the bot is
/// standing, so this is the second trade rather than the third: a smith still needs a forge. What it needs
/// instead is mana — four for the first circle and fifty for the eighth, which at fifty Intelligence is a
/// mage's entire pool — and that is what makes Meditation on a mage's own vector something other than
/// decoration.
/// </para>
///
/// <para>
/// <b>And unlike cloth, what comes off it has no shopkeeper above the third circle.</b> That is the whole
/// reason this trade is worth adding: it is the first work in the project whose output only another bot can
/// buy.
/// </para>
/// </summary>
public static class BotQuill
{
    /// <summary>
    /// How far below its own skill a scribe keeps its work. The same five points the needle uses, and the same
    /// argument: at the very edge most attempts burn the material, and well below it nothing is learned.
    /// </summary>
    public static double Margin { get; set; } = 5.0;

    /// <summary>
    /// What a scribe adds to what its materials cost, as a share of them, when it prices its own work.
    ///
    /// <para>
    /// <b>Nothing here had ever compared the two sides of the trade.</b> A scroll was listed at whatever the
    /// market last paid for one, or failing that at what a shopkeeper charges for that circle, and the herbs
    /// and paper it was made of were never in the arithmetic at all. So a scribe could — and on 02.09.2026 a
    /// population of them did — buy a hundred gold of reagents, write, and list the result for ninety. The
    /// mages of this shard were the poorest bots on it, and this is the half of the reason that is about
    /// price rather than about how much they bought.
    /// </para>
    ///
    /// <para>
    /// A fifth. Patrick's own figure, "fifteen or twenty percent, so the reagents pay for themselves and they
    /// earn as well" — and it is a floor under the asking price, not a tax on it: where the market already
    /// pays more than cost and a fifth, the market's number stands and the scribe is simply doing well.
    /// </para>
    /// </summary>
    public static double Markup { get; set; } = 0.20;

    /// <summary>
    /// How many of each herb a caster keeps back from the pen, whatever it is writing.
    ///
    /// <para>
    /// <b>A mage that writes with its last reagent has disarmed itself.</b> Casting from the book consumes
    /// herbs — see <c>BotStrike.Ready</c>, which refuses a spell on an empty pack — so a scribe that writes
    /// until the pack is bare cannot then fight, and fighting is the living it is meant to fall back on. The
    /// old rule asked only whether one attempt's worth was present, so a mage born with thirty of each herb
    /// wrote thirty times and walked into the field with nothing to throw.
    /// </para>
    /// </summary>
    public static int Reserve { get; set; } = 10;

    /// <summary>What a herb is reckoned to cost where no shopkeeper within reach sells it.</summary>
    public static int HerbGuess { get; set; } = 3;

    /// <summary>The inscription system, or null before content initialisation has built it.</summary>
    public static CraftSystem System => DefInscription.CraftSystem;

    /// <summary>
    /// What a counter charges for one of a herb. Remembered, because a shelf price in this era does not move
    /// and this is asked once per recipe per decision.
    /// </summary>
    private static readonly Dictionary<Type, int> _herbage = [];

    /// <summary>What one of this herb costs, from the nearest shelf that has it, or the guess.</summary>
    public static int Herb(Mobile bot, Type kind)
    {
        if (kind == null)
        {
            return 0;
        }

        if (_herbage.TryGetValue(kind, out var known))
        {
            return known;
        }

        var shop = BotShops.Nearest(bot, kind);
        var price = shop == null ? 0 : BotShops.Price(shop, kind);

        if (price <= 0)
        {
            return HerbGuess;
        }

        _herbage[kind] = price;

        return price;
    }

    /// <summary>
    /// What one attempt at this recipe costs in materials: the paper, and every herb it burns.
    ///
    /// <para>
    /// The blank is passed in rather than looked up because the proposer has just priced it at the counter it
    /// is about to walk to, and that is the price this bot will actually pay.
    /// </para>
    /// </summary>
    public static int Cost(Mobile bot, CraftItem recipe, int paper)
    {
        var resources = recipe?.Resources;

        if (resources == null)
        {
            return 0;
        }

        var bill = Math.Max(0, paper);

        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];

            if (res.ItemType == null || res.ItemType == typeof(BlankScroll))
            {
                continue;
            }

            bill += Math.Max(1, res.Amount) * Herb(bot, res.ItemType);
        }

        return bill;
    }

    /// <summary>The least this may be listed for: what it cost to make, and the markup on top.</summary>
    public static int Asking(int cost) => cost <= 0 ? 0 : (int)Math.Ceiling(cost * (1.0 + Markup));

    /// <summary>The pen this bot writes with, if it is carrying one.</summary>
    public static ScribesPen Pen(Mobile bot) => BotOutfit.Oldest<ScribesPen>(bot?.Backpack);

    /// <summary>How many blank scrolls are in the pack.</summary>
    public static int Blanks(Mobile bot) => bot?.Backpack?.GetAmount(typeof(BlankScroll)) ?? 0;

    /// <summary>The Inscribe this recipe asks for, or zero when it asks for none.</summary>
    public static double Requirement(CraftItem recipe)
    {
        var skills = recipe?.Skills;

        if (skills == null)
        {
            return 0.0;
        }

        for (var i = 0; i < skills.Count; i++)
        {
            if (skills[i].SkillToMake == SkillName.Inscribe)
            {
                return skills[i].MinSkill;
            }
        }

        return 0.0;
    }

    /// <summary>
    /// Whether the pack holds the herbs this recipe consumes.
    ///
    /// <para>
    /// <b>The blank scroll is deliberately not counted here</b>, and it is not an oversight. This question is
    /// asked by a proposer that is about to walk to a shop for paper, so counting paper would have it answer
    /// "nothing can be written" and stay home. Mana is left out for the same kind of reason: it comes back on
    /// its own, and a missing herb does not.
    /// </para>
    /// </summary>
    public static bool Stocked(Mobile bot, CraftItem recipe) => Stocked(bot, recipe, Reserve);

    /// <summary>
    /// The same, keeping back <paramref name="reserve"/> of each herb. A session written to order keeps none back (03.10.2026): it
    /// is a few attempts for a scroll somebody needs, not a pen running until the pack is bare, and the shopper puts the herbs back.
    /// </summary>
    public static bool Stocked(Mobile bot, CraftItem recipe, int reserve)
    {
        var pack = bot?.Backpack;
        var resources = recipe?.Resources;

        if (pack == null || resources == null || resources.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];

            if (res.ItemType == null || res.ItemType == typeof(BlankScroll))
            {
                continue;
            }

            // The attempt's own herbs, and the handful kept back to fight with. See Reserve: a scribe that
            // spends its last ginseng on a scroll it means to sell cannot cast the spell it just wrote.
            if (pack.GetAmount(res.ItemType) < Math.Max(1, res.Amount) + Math.Max(0, reserve))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This scribe's chance of writing one of these at its present Inscribe: the engine's own sum (nil at the bottom of the
    /// circle's band, certain at the top), clamped to a share.
    /// </summary>
    public static double Chance(Mobile bot, CraftItem recipe)
    {
        if (bot == null || recipe == null || System == null)
        {
            return 0.0;
        }

        var chance = recipe.GetSuccessChance(bot, null, System, false, out var able);

        return able ? Math.Clamp(chance, 0.0, 1.0) : 0.0;
    }

    /// <summary>
    /// What one session written to order has to buy at the counter for paper, and whether it can: blank scrolls for the attempts
    /// the units take at this scribe's chance, less those it carries, and the herbs those attempts burn that the pack lacks — at
    /// the same counter, whose shelf has to hold them. False when it does not sell a herb the pack is short of.
    ///
    /// <para>
    /// <b>The session's outlay was twenty blanks whatever it was for, and that refused the scribes the work.</b>
    /// <c>BotInscribe.Outlay</c> was <c>Batch × price</c>: 100gp at the mage's 5gp, 240gp at a 12gp counter. Of 21 bots that
    /// write on 03.10.2026 at 03:25, seven had 100gp in pack and bank together, before their own wants held any of it; the log
    /// named the refusal 68 times in four sessions as the first veto of a take ("inscribe costs 100gp and it has 11gp free"),
    /// once from Kaelthas holding ten blanks left from the twenty it had just bought. A Greater Heal for a healer is one scroll:
    /// at an even chance it is two blanks and two of each of four herbs, 34gp at most.
    /// </para>
    /// </summary>
    private static bool Supply(
        Mobile bot, BaseVendor shop, CraftItem recipe, int units, double chance, int paper, out int attempts, out int blanks,
        out List<(Type Herb, int Amount)> herbs, out int outlay
    )
    {
        attempts = Math.Clamp((int)Math.Ceiling(Math.Max(1, units) / Math.Max(0.05, chance)), 1, Math.Max(1, BotInscribe.Batch));
        blanks = Math.Max(0, attempts - Blanks(bot));
        outlay = blanks * Math.Max(0, paper);
        herbs = null;

        var pack = bot?.Backpack;
        var resources = recipe?.Resources;

        if (pack == null || resources == null)
        {
            return false;
        }

        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];

            if (res.ItemType == null || res.ItemType == typeof(BlankScroll))
            {
                continue;
            }

            var lacking = attempts * Math.Max(1, res.Amount) - pack.GetAmount(res.ItemType);

            if (lacking <= 0)
            {
                continue;
            }

            var price = shop == null ? 0 : BotShops.Price(shop, res.ItemType);

            if (price <= 0)
            {
                return false;
            }

            herbs ??= [];
            herbs.Add((res.ItemType, lacking));
            outlay += lacking * price;
        }

        return true;
    }

    /// <summary>
    /// Whether this scribe should write this scroll itself rather than ask the island for it: it has the pen, the book, the
    /// Inscribe and the pool for it, a counter for paper that also sells whatever herb it lacks, and what the scroll costs at its
    /// own chance is no more than the want it would say would pay. Asked by <see cref="BotSeeker"/> and <see cref="BotArmoury"/>
    /// before they say a want; <see cref="Plan"/> then offers the session as its own order.
    ///
    /// <para>
    /// <b>Scribes asked the island for scrolls they could write, and the asking locked the pen.</b> Mirthal (Inscribe 42.9) and
    /// Lorandil (23.9) held wants for Lightning at 124gp on 03.10.2026, Caelith (79.6) for Mark and Energy Bolt at 81gp each; five
    /// of the 64 scroll wants said in the four sessions of 02.10–03.10 were asked by a bot that could write them. The money a want
    /// holds is not free, so the same bots were refused the paper — "inscribe costs 100gp and it has 82gp free of 383gp"
    /// (Caelith, 03:24:46) — and a line nobody else would answer stood until it failed. A weak scribe whose own attempts would cost
    /// more than a better one asks still asks: that is what the market is for.
    /// </para>
    /// </summary>
    public static bool OwnPen(IBotWilful wilful, Type kind)
    {
        var bot = wilful?.Self;

        if (bot == null || System == null || Pen(bot) == null || BotGrimoire.Book(bot) == null)
        {
            return false;
        }

        var recipe = BotScribes.Recipe(kind);
        var spell = BotGrimoire.SpellOf(kind);

        if (recipe == null || spell < 0 || Requirement(recipe) > bot.Skills[SkillName.Inscribe].Value - Margin || recipe.Mana > bot.ManaMax)
        {
            return false;
        }

        var chance = Chance(bot, recipe);

        if (chance <= 0.0)
        {
            return false;
        }

        var shop = BotShops.Nearest(wilful, typeof(BlankScroll));
        var paper = shop == null ? 0 : BotShops.Price(shop, typeof(BlankScroll));

        // And it can pay for that session now: a scribe with too little free to buy the paper asks, as it always did.
        if (paper <= 0 || !Supply(bot, shop, recipe, 1, chance, paper, out _, out _, out _, out var outlay) || BotYield.Spendable(bot) < outlay)
        {
            return false;
        }

        var offer = BotMarket.Worth(kind, BotGrimoire.ShopPrice(BotGrimoire.Circle(spell)));

        return Cost(bot, recipe, paper) / chance <= offer;
    }

    /// <summary>
    /// What this scribe should write now, and what it has to buy first; null when nothing. See <see cref="BotQuillPlan"/>.
    ///
    /// <para>
    /// <b>Only what the Inscribe allows, and that is the engine's rule.</b> <c>DefInscription.CanCraft</c> asks the scribe's own
    /// book whether it knows the spell, and since Patrick's order of 25.09.2026 a bot is let write by its Inscribe alone
    /// (<c>DefInscription.Knows</c>, set in <see cref="BotSpellsModule"/>). The circle's band stays the engine's: five points
    /// under its own skill, <see cref="Margin"/>, and a pool of mana for it.
    /// </para>
    ///
    /// <para>
    /// <b>Paid work first, practice afterwards</b>, and the ordering is the cure rather than a weight. "Hardest first, then
    /// price" dropped anything easier than the best so far before the market was asked, so a funded, standing, twice-raised order
    /// sat on the board all evening while the scribe wrote something harder that nobody had asked for (Mind Blast, 26.08.2026).
    /// So three tallies, preferred in order: somebody else's want this scribe can carry to — a line on the Needs board, the
    /// dearest first — then its own book's next gap or its own scrolls to throw, when it writes them for less than asking would
    /// cost (<see cref="OwnPen"/>), then practice: the hardest thing it reliably makes, and at equal difficulty the better price.
    /// </para>
    ///
    /// <para>
    /// <b>A session to order is sized to the order (03.10.2026).</b> It buys the paper and the herbs the units take at this
    /// scribe's chance and no more (<see cref="Supply"/>), so a herb the pack is short of no longer drops a paid scroll for a
    /// practice piece: Kaelthas wrote Poison and Magic Arrow at 03:25 with three Lightning wants of 124gp standing — Magic Arrow
    /// is ash alone, so the herb it lacked was mandrake root, last of the eight on the shopper's list and lost with every death.
    /// And a want that pays under what the scroll costs at this scribe's chance is passed over (counted), so a scribe with one
    /// chance in eight does not buy eight blanks and thirty herbs to be paid for one; the want stays for a better hand.
    /// </para>
    /// </summary>
    public static BotQuillPlan Plan(IBotWilful wilful, BaseVendor shop, int paper)
    {
        var bot = wilful?.Self;
        var system = System;
        var pack = bot?.Backpack;

        if (system == null || pack == null)
        {
            return null;
        }

        var skill = bot.Skills[SkillName.Inscribe].Value;
        var pool = bot.ManaMax;
        var recipes = system.CraftItems;

        // The book is fetched once. Asking whether a spell is already known used to walk the pack looking for the book on every
        // candidate recipe, which is thirty-odd pack scans for one decision — and this runs on the bot's own beat.
        var book = BotGrimoire.Book(bot);

        // Its own: the next gap in its book, unless the scroll for it is already in the pack (the seeker writes that one in), and
        // the attack scroll it keeps a few of. Asked once, before the loop.
        var gap = OwnGap(bot);
        var kept = BotArmoury.Kept(bot, out _);
        var keptShort = kept == null ? 0 : Math.Max(0, BotArmoury.Stock - pack.GetAmount(kept));

        // What the porter keeps of each kind for the bot itself (BotUnload.Keeps): the carrier walks only what is over it, so a
        // scroll written for a want has to be written past it. Read once.
        var keep = BotUnload.Keeps(wilful);

        BotQuillPlan line = null;
        BotQuillPlan own = null;

        // What it has free, its wants' reservation taken off: a session to order dearer than this is passed over (counted) for
        // practice the auction would not refuse, rather than offered and refused for want of the money (BotAppraisal.Weigh).
        var free = BotYield.Spendable(bot);

        CraftItem best = null;
        var bestNeeds = -1.0;
        var bestWorth = 0;
        Type bestKind = null;

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            var kind = recipe.ItemType;
            var spell = BotGrimoire.SpellOf(kind);

            // Scrolls only. The same system makes runebooks, spellbooks and bulk order books, and none of those is what this
            // trade is for.
            if (spell < 0)
            {
                continue;
            }

            var needs = Requirement(recipe);

            if (needs > skill - Margin || recipe.Mana > pool)
            {
                continue;
            }

            // Has anybody put money down for one of these. A scan of the open wants — cheap enough to ask of every recipe, and
            // asked before the practice drop below, which would otherwise throw the answer away before anybody looked at it.
            var funded = BotMarket.Best(kind) > 0;
            var mine = kind == gap || kind == kept && keptShort > 0;

            // Difficulty decides among practice pieces, and an order is not practice. For everything else the drop stands: it
            // is what keeps the pack from being counted for all forty writable scrolls every time a mage wonders what to do.
            if (!funded && !mine && best != null && needs < bestNeeds)
            {
                continue;
            }

            // What may be attempted at all: the Inscribe above decides for a bot (BotSpellsModule.Start and DefInscription.Knows,
            // Patrick's order of 25.09.2026); without the seam — the engine as shipped — the book does, as it did until then.
            if (DefInscription.Knows?.Invoke(bot, spell) != true && book?.HasSpell(spell) != true)
            {
                continue;
            }

            var chance = Chance(bot, recipe);

            if (chance <= 0.0)
            {
                continue;
            }

            var cost = Cost(bot, recipe, paper);
            var asking = Math.Max(BotMarket.Worth(kind, BotGrimoire.ShopPrice(BotGrimoire.Circle(spell))), Asking(cost));

            // Somebody else's want: a line. The dearest one this scribe can carry to, that no other scribe is writing — and that
            // its own pack does not already answer over what it keeps, which is a carrier's walk rather than more paper: the
            // makers' own question (BotCourier.Holding).
            if (funded && BotScribes.Order(wilful, kind) is { } want && want.Offer > (line?.For?.Offer ?? 0)
                && !BotCourier.Holding(wilful, want))
            {
                // <b>Written past what it keeps (03.10.2026, the review of this build).</b> A sage keeps three Energy Bolts to throw
                // and a reader its Resurrection scroll (BotUnload.Keeps); a scroll written for a want into a keep that was short is
                // absorbed by it and never carried. So the session writes the want's units and what the keep lacks, and a kind
                // kept whole is no line for this scribe at all.
                keep.TryGetValue(kind, out var allowed);
                var units = allowed >= int.MaxValue
                    ? 0
                    : want.Payable + Math.Max(0, allowed - BotHandover.Carried(wilful, kind));

                if (units <= 0)
                {
                    BotScribes.Keeping();
                }
                else if (cost > 0 && want.Offer < cost / chance)
                {
                    BotScribes.Under();
                }
                else if (!Supply(bot, shop, recipe, units, chance, paper, out var attempts, out var blanks, out var herbs, out var outlay))
                {
                    BotScribes.Herbless();
                }
                else if (outlay > free)
                {
                    BotScribes.Poor();
                }
                else
                {
                    line = new BotQuillPlan
                    {
                        Recipe = recipe, Kind = kind, Worth = Math.Max(asking, want.Offer), For = want, Units = units,
                        Chance = chance, Attempts = attempts, Paper = blanks, Herbs = herbs, Outlay = outlay
                    };
                }
            }

            // Its own book's gap, or its own scrolls to throw, when writing them costs it no more than asking would. One scroll a
            // session, the size OwnPen judged it by: the next session writes the next.
            if (mine && own == null && (cost <= 0 || cost / chance <= BotMarket.Worth(kind, BotGrimoire.ShopPrice(BotGrimoire.Circle(spell))))
                && Supply(bot, shop, recipe, 1, chance, paper, out var tries, out var sheets, out var stock, out var bill) && bill <= free)
            {
                own = new BotQuillPlan
                {
                    Recipe = recipe, Kind = kind, Worth = asking, Own = true, Units = 1, Chance = chance, Attempts = tries,
                    Paper = sheets, Herbs = stock, Outlay = bill
                };
            }

            // Practice: the hardest thing it reliably makes with the herbs it has and the handful it keeps to fight with.
            if (best != null && needs < bestNeeds || !Stocked(bot, recipe))
            {
                continue;
            }

            // <b>What it is made of, against what it will fetch.</b> Writing at a loss is refused outright, and what is written
            // is listed at no less than cost and the markup; where the market already pays more than that, the market's
            // number stands.
            if (cost > 0 && BotMarket.Worth(kind, BotGrimoire.ShopPrice(BotGrimoire.Circle(spell))) < cost)
            {
                continue;
            }

            if (best != null && !Better(needs, asking, bestNeeds, bestWorth))
            {
                continue;
            }

            best = recipe;
            bestNeeds = needs;
            bestWorth = asking;
            bestKind = kind;
        }

        if (line != null || own != null)
        {
            return line ?? own;
        }

        if (best == null)
        {
            return null;
        }

        // Practice writes until the paper or the herbs run out, as it always has: a batch when the pack holds no paper, none when
        // it holds some.
        var practicePaper = Blanks(bot) > 0 ? 0 : Math.Max(1, BotInscribe.Batch);

        return new BotQuillPlan
        {
            Recipe = best, Kind = bestKind, Worth = bestWorth, Chance = Chance(bot, best), Attempts = Math.Max(1, BotInscribe.Batch),
            Paper = practicePaper, Outlay = practicePaper * Math.Max(0, paper)
        };
    }

    /// <summary>The scroll for the next gap in this bot's book, when the pack does not already hold one; else null. See BotSeeker.</summary>
    private static Type OwnGap(Mobile bot)
    {
        var spell = BotGrimoire.Missing(bot);

        if (spell < 0 || spell == BotRaise.Spell && BotRaise.KeepsScroll(bot))
        {
            return null;
        }

        var kind = BotGrimoire.ScrollFor(spell);

        return kind == null || Held(bot, kind) > 0 ? null : kind;
    }

    /// <summary>Harder wins, because that is where the skill is; at equal difficulty, the better price.</summary>
    private static bool Better(double needs, int worth, double bestNeeds, int bestWorth)
    {
        if (needs > bestNeeds)
        {
            return true;
        }

        if (needs < bestNeeds)
        {
            return false;
        }

        return worth > bestWorth;
    }

    /// <summary>
    /// One attempt. The engine takes it from here: it rolls, spends the mana, and either a scroll appears in
    /// the pack a moment later or the blank and the herbs are gone.
    ///
    /// Nothing is returned about the outcome, because the outcome is not known yet — the craft runs on its own
    /// timer. Whoever swings counts what is in the pack afterwards. The first version counted attempts and
    /// reported forty-four things made about a smith that had produced nothing.
    /// </summary>
    public static bool Swing(Mobile bot, CraftItem recipe, BaseTool pen)
    {
        if (bot == null || recipe == null || pen == null || System == null)
        {
            return false;
        }

        // No resource type to choose: a scroll is made of the herbs the recipe names and nothing else, so
        // there is no equivalent of "which cloth" for the engine to ask about.
        recipe.Craft(bot, System, null, pen);

        return true;
    }

    /// <summary>How many of that kind of scroll are in the pack.</summary>
    public static int Held(Mobile bot, Type kind) =>
        kind == null ? 0 : bot?.Backpack?.GetAmount(kind, true) ?? 0;

    /// <summary>Every scroll of that kind in the pack, so it can be written into a book or sold.</summary>
    public static List<SpellScroll> Gather(Mobile bot, Type kind)
    {
        List<SpellScroll> made = [];

        var pack = bot?.Backpack;

        if (pack == null || kind == null)
        {
            return made;
        }

        // A snapshot: writing into a book and listing on the market both move things out of the pack, which
        // mutates the list being read.
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            if (carried[i] is SpellScroll scroll && !scroll.Deleted && scroll.Movable && kind.IsInstanceOfType(scroll))
            {
                made.Add(scroll);
            }
        }

        return made;
    }
}

/// <summary>
/// What one session at the pen is for, and what it has to buy before it starts. Made by <see cref="BotQuill.Plan"/>, carried by
/// <see cref="BotInscribe"/> from the proposal to the last scroll, so the work writes what it was chosen for.
/// </summary>
public sealed class BotQuillPlan
{
    public CraftItem Recipe { get; init; }

    public Type Kind { get; init; }

    /// <summary>What one is counted at: the island's price, or cost and the markup, or the want's offer.</summary>
    public int Worth { get; init; }

    /// <summary>Somebody else's want the session is written for: a line on the Needs board. Null otherwise.</summary>
    public BotWant For { get; init; }

    /// <summary>Written for its own book's next gap or its own scrolls to throw.</summary>
    public bool Own { get; init; }

    /// <summary>
    /// How many it is written for — for a want, its units and what the bot's own keep of the kind lacks, so that the want's are
    /// carriable; practice writes until the paper or the herbs run out.
    /// </summary>
    public int Units { get; init; }

    /// <summary>The scribe's chance at the time of choosing.</summary>
    public double Chance { get; init; }

    /// <summary>The attempts the units are expected to take at that chance; for practice, a batch.</summary>
    public int Attempts { get; init; }

    /// <summary>Blank scrolls to buy at the counter.</summary>
    public int Paper { get; init; }

    /// <summary>Herbs to buy at the same counter, and how many of each; null when the pack has what the attempts burn.</summary>
    public List<(Type Herb, int Amount)> Herbs { get; init; }

    /// <summary>What the paper and the herbs come to at that counter: the session's outlay.</summary>
    public int Outlay { get; init; }

    /// <summary>Written to order — a want, its own book, its own stock — rather than for practice.</summary>
    public bool Order => For != null || Own;

    /// <summary>Whether anything at all has to be bought first.</summary>
    public bool Buys => Paper > 0 || Herbs is { Count: > 0 };
}
