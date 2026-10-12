using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Brewing: what a potion is made of, and the swing that makes it.
///
/// <para>
/// <b>The whole trade was scaffolded and never built.</b> Three classes on this shard — Healer, Mage and
/// Sage — ask for Alchemy at a hundred; <see cref="BotOutfit"/> hands every one of them a mortar and pestle
/// on the strength of that skill; <c>BotUnload</c> keeps their empty glass out of the market because "glass
/// is stock to an alchemist"; and the word <c>DefAlchemy</c> appeared nowhere in the assembly. The tool was
/// issued, the glass was protected and the skill was trained for a trade that did not exist. Meanwhile the
/// only potion on the island came off an alchemist's shelf at fifteen gold, and on the evening of 27.08.2026
/// two hundred and twenty-one of two hundred and fifty-two moments at death's door were a bot reaching for
/// a bottle it did not have.
/// </para>
///
/// <para>
/// <b>Two materials, which is what makes this its own file rather than another caller of
/// <see cref="BotCraftwork"/>.</b> Every recipe in <c>DefAlchemy</c> declares a reagent and a
/// <see cref="Bottle"/> — <c>AddRes(index, typeof(Bottle), …)</c> on every single one — and
/// <c>BotCraftwork.Simple</c> refuses any recipe with more than one resource, by design and for good
/// reasons on the trades it serves. So the arithmetic of "can I make one" is done here, over both halves.
/// </para>
///
/// <para>
/// <b>The glass is the half that comes back.</b> A drunk potion leaves its bottle in the pack — the engine
/// does it, <c>BasePotion.Drink</c> — so a population that drinks is a population that produces the second
/// material of its own supply. The alchemist's counter sells the rest at five gold a hundred, which makes
/// glass the cheap half and the reagent the dear one, the opposite way round from every other trade here.
/// </para>
/// </summary>
public static class BotFlask
{
    /// <summary>The alchemy system, or null before content initialisation has built it.</summary>
    public static CraftSystem System => DefAlchemy.CraftSystem;

    /// <summary>The tool. Handed out with the skill; see <see cref="BotOutfit.ToolsFor"/>.</summary>
    public static BaseTool Kit(Mobile bot) => BotOutfit.Oldest<MortarPestle>(bot?.Backpack);

    /// <summary>
    /// How many of each herb a brewer keeps on hand.
    ///
    /// <para>
    /// <b>A brewer's reagents were nobody's errand.</b> <c>BotShopper</c> buys them only for a build whose kit
    /// declares reagents, which is every caster and no crafter — so the bot carrying the mortar was never sent
    /// for the one half of its trade it cannot gather. It read "had the glass but no herbs" 57 times in a
    /// five-minute window with 1936 herbs on the population's own stalls.
    /// </para>
    ///
    /// <para>
    /// Five, which is one draught of each of the eight and no hoard. Bought rather than ordered by the armful:
    /// the shopper takes a stall before a counter and only asks the board when neither has any, so this cannot
    /// repeat what ordering glass did to this shard's trade. See <c>BotStores</c> for that.
    /// </para>
    /// </summary>
    public static int Herbs { get; set; } = 5;

    /// <summary>How far below its own skill a bot keeps its work. The needle's figure, for the needle's reason.</summary>
    public static double Margin { get; set; } = 5.0;

    /// <summary>
    /// How many bottles are worth setting up for. Below this the walk to a counter costs more than the batch.
    /// </summary>
    public static int LeastBottles { get; set; } = 5;

    /// <summary>How many empties to buy at a time when the pack has run out of glass.</summary>
    public static int Batch { get; set; } = 20;

    /// <summary>
    /// The most of one draught a bot may hold and have out for sale at once. Five.
    ///
    /// <para>
    /// <b>Patrick's order of 05.09.2026, against a trade that was drowning the market in its own output.</b>
    /// A brewer works until its reagents or its glass run out, so one round could put twenty of a kind on a
    /// stall — and the shard's markdown then spent hours undoing it: "Wulfric cut Lesser Heal Potion to 3gp
    /// after 23 sat unsold", from an opening ask of sixteen. Five of a kind is a stock a buyer can clear, and
    /// it is well over what any class carries for itself (<c>BotClass.PotionLimit</c> is two or three), so
    /// the cap never fights the kit.
    /// </para>
    ///
    /// <para>
    /// <b>Counted across the pack and the bot's own stall together, which is what makes it a limit on selling
    /// as well as on making.</b> Counting the pack alone would be a cap a brewer walks out of by listing what
    /// it has and starting again — the same hole the smith's ordering had, where a bot refused to buy iron
    /// because it was selling iron and nothing joined the two facts up.
    /// </para>
    /// </summary>
    public static int Cap { get; set; } = 5;

    /// <summary>
    /// How long a bot stands off a draught after reaching the cap on it. Ten minutes.
    ///
    /// <para>
    /// Per bot and per draught, so a brewer at five heal potions still brews cure. The cap alone would be
    /// enough to stop the making; the rest is what stops the <em>asking</em> — without it the proposer offers
    /// the same round every beat, is refused by the cap, and the population's whole decision budget goes on a
    /// question already answered. It is the cooldown's job in <c>BotSchool.RestMs</c>, for the same reason.
    /// </para>
    /// </summary>
    public static int RestMs { get; set; } = 600000;

    /// <summary>Times a draught was passed over because the bot was already at the cap. For the summary.</summary>
    public static long Capped { get; private set; }

    /// <summary>Times a bot was stood off a draught for reaching the cap. For the summary.</summary>
    public static long Rests { get; private set; }

    /// <summary>Who is standing off what, and until when. Keyed by the bot and the draught.</summary>
    private static readonly Dictionary<(Serial Who, Type What), long> _resting = new();

    /// <summary>
    /// How many of this draught the bot has between its pack and its own bank box.
    ///
    /// <para>
    /// Both, always. See <see cref="Cap"/>: a count that looked only at the pack would be a limit on carrying rather than on
    /// producing, and the porter puts a brewer's unsold draughts in its bank box (its own stall, until the auction went on
    /// 01.10.2026).
    /// </para>
    /// </summary>
    public static int Held(IBotWilful bot, Mobile body, Type kind)
    {
        if (kind == null)
        {
            return 0;
        }

        return Amount(body, kind) + (body?.FindBankNoCreate()?.GetAmount(kind) ?? 0);
    }

    /// <summary>
    /// Whether every draught this bot could make stands at its cap or is resting.
    ///
    /// <para>
    /// <b>Asked so that the cap does not get reported as an empty pack.</b> A bot at five of everything
    /// answers null from <see cref="Likeliest"/>, and the alchemist reads a null there as "no glass and no
    /// herbs either" — which put 377 well-stocked brewers into the <c>had neither</c> bucket within a quarter
    /// of an hour of the cap going in. A new gate needs a new bucket; this is the question that fills it.
    /// </para>
    /// </summary>
    public static bool AtCap(IBotWilful will, Mobile bot)
    {
        if (bot == null || System == null)
        {
            return false;
        }

        var families = BotArsenal.Draughts;
        var makeable = 0;

        for (var i = 0; i < families.Count; i++)
        {
            var kind = BotArsenal.Potion(families[i]);

            if (kind == null || Recipe(bot, kind) == null)
            {
                continue;
            }

            makeable++;

            if (!Resting(bot, kind) && Held(will, bot, kind) < Cap)
            {
                return false;
            }
        }

        // A bot that can make nothing at all is not a bot that has made too much.
        return makeable > 0;
    }

    /// <summary>Whether this bot is standing off this draught for having reached the cap on it.</summary>
    public static bool Resting(Mobile body, Type kind)
    {
        if (body == null || kind == null || !_resting.TryGetValue((body.Serial, kind), out var until))
        {
            return false;
        }

        // Compared by subtraction against a stamp that was itself a real tick. See BotStipend for the host
        // whose counter starts enormous and wraps negative.
        if (Core.TickCount - until < 0)
        {
            return true;
        }

        _resting.Remove((body.Serial, kind));

        return false;
    }

    /// <summary>Stands this bot off this draught for <see cref="RestMs"/>. Says nothing twice.</summary>
    public static void Rest(Mobile body, Type kind)
    {
        if (body == null || kind == null || Resting(body, kind))
        {
            return;
        }

        // Swept here rather than on a timer: the store is only ever read and written on this one path, and a
        // population that stops brewing stops paying for the sweep at the same moment it stops filling it.
        if (_resting.Count > 512)
        {
            Sweep();
        }

        _resting[(body.Serial, kind)] = Core.TickCount + RestMs;
        Rests++;
    }

    private static void Sweep()
    {
        List<(Serial, Type)> lapsed = [];

        foreach (var (key, until) in _resting)
        {
            if (Core.TickCount - until >= 0)
            {
                lapsed.Add(key);
            }
        }

        for (var i = 0; i < lapsed.Count; i++)
        {
            _resting.Remove(lapsed[i]);
        }
    }

    /// <summary>Forgotten with the world, like every other store in this assembly that is keyed by serial.</summary>
    public static void Forget()
    {
        _resting.Clear();
        Capped = 0;
        Rests = 0;
    }

    /// <summary>
    /// What a brewed potion opens at on the market.
    ///
    /// <para>
    /// Fourteen, against the alchemist's fifteen — <c>SBAlchemist</c>, <c>GenericBuyInfo(typeof(
    /// LesserHealPotion), 15, 10, …)</c>. A shopkeeper is the ceiling and never the preference, so an opening
    /// ask has to be under it to be a trade at all; and it is comfortably over what the two halves cost,
    /// which is a reagent at three or four and glass at five. See <c>BotFletching.Worth</c>, where the same
    /// number was one gold over the shelf and the arrows therefore could not be sold.
    /// </para>
    /// </summary>
    public static int Worth { get; set; } = 14;

    public static int Amount(Mobile bot, Type stuff) =>
        stuff == null ? 0 : bot?.Backpack?.GetAmount(stuff) ?? 0;

    /// <summary>Empty glass in the pack. Half of every recipe in the trade.</summary>
    public static int Bottles(Mobile bot) => Amount(bot, typeof(Bottle));

    /// <summary>
    /// Whether this is a potion recipe, and what reagent it eats.
    ///
    /// <para>
    /// Asked of the recipe's own resource list rather than of a table of potions kept here to drift: exactly
    /// two resources, one of them glass, and the other is the answer.
    /// </para>
    /// </summary>
    public static bool Twofold(CraftItem recipe, out Type reagent, out int units, out int glass)
    {
        reagent = null;
        units = 0;
        glass = 0;

        var resources = recipe?.Resources;

        if (resources == null || resources.Count != 2 || recipe.ItemType == null)
        {
            return false;
        }

        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];

            if (res.ItemType == typeof(Bottle))
            {
                glass = Math.Max(1, res.Amount);

                continue;
            }

            reagent = res.ItemType;
            units = Math.Max(1, res.Amount);
        }

        return reagent != null && glass > 0;
    }

    /// <summary>What one attempt at this recipe needs, in reagent and in glass.</summary>
    public static (Type Reagent, int Units, int Glass) Costs(CraftItem recipe) =>
        Twofold(recipe, out var reagent, out var units, out var glass) ? (reagent, units, glass) : (null, 0, 0);

    /// <summary>
    /// The reagents this trade actually consumes, asked of the recipes rather than listed here.
    ///
    /// <para>
    /// <b>A brewer wants two of the eight, and telling it to want all eight is telling it to shop for forty
    /// minutes before it reaches either.</b> There are two draught families on this shard — heal and cure —
    /// and between them they burn ginseng and garlic. The shopper's reagent list is in casting order, ash and
    /// pearl first, and it buys one kind per errand at roughly two errands a bot in twenty minutes. So a
    /// brewer sent after "reagents" spent its first four trips on ash and black pearl it can never use, and
    /// the summary read "had the glass but no herbs" on a rising share the whole time — 35% of new asks in
    /// one window, 58% in the next.
    /// </para>
    ///
    /// <para>
    /// Read from <c>Costs</c>, so adding a third draught family adds its reagent here and nowhere else. Built
    /// once: the recipe table does not change while the shard is up.
    /// </para>
    /// </summary>
    public static IReadOnlyList<Type> Needs
    {
        get
        {
            if (_needs is { Count: > 0 })
            {
                return _needs;
            }

            return _needs = Wanted();
        }
    }

    private static IReadOnlyList<Type> _needs;

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFlask));

    private static bool _saidNeeds;

    /// <summary>
    /// Why the last look at the mortar found nothing, split apart because one bucket was holding three
    /// answers.
    ///
    /// <para>
    /// <b>"Had the glass but no herbs" was everything Choose refused for.</b> It refuses on three quite
    /// different grounds — the skill will not carry any draught, the reagent is not in the pack, or the
    /// recipe table has nothing that fits — and the alchemist wrote all three into one number. Measured on
    /// 05.09.2026 the share of new asks landing there rose from 14% to 37% inside one run *after* the
    /// reagents were fixed, which is a time pattern rather than a supply one and could not be read at all
    /// while the three were together.
    /// </para>
    /// </summary>
    public enum Refusal
    {
        /// <summary>A draught can be made. Not a refusal.</summary>
        None,

        /// <summary>Every recipe wants more Alchemy than this bot has, less the margin.</summary>
        Unskilled,

        /// <summary>Skill enough, and not one of the reagents in the pack.</summary>
        Reagentless,

        /// <summary>Every draught it could make is at its cap or resting.</summary>
        Full
    }

    /// <summary>Why the last <see cref="Choose"/> came back empty. For the summary.</summary>
    public static Refusal Why { get; private set; }

    private static IReadOnlyList<Type> Wanted()
    {
        List<Type> want = [];
        var families = BotArsenal.Draughts;

        var system = System;
        var recipes = system?.CraftItems;

        if (recipes == null)
        {
            // Before content initialisation there is no table to read. Not cached in that case — see Needs,
            // which asks again next time rather than remembering an empty answer for the life of the shard.
            return want;
        }

        for (var i = 0; i < families.Count; i++)
        {
            var kind = BotArsenal.Potion(families[i]);

            for (var j = 0; j < recipes.Count; j++)
            {
                // Asked of the table and not of a bot: this is what the trade consumes, not what one bot's
                // skill will carry today.
                if (recipes[j].ItemType != kind || !Twofold(recipes[j], out var reagent, out _, out _))
                {
                    continue;
                }

                if (!want.Contains(reagent))
                {
                    want.Add(reagent);
                }

                break;
            }
        }

        // <b>Said once, and here rather than at boot, because at boot it is a lie.</b> The first version of
        // this line logged from BotCraftModule.Initialize and reported "0 reagents: nothing" every start —
        // DefAlchemy.CraftSystem is an ordinary property filled during content initialisation, not a lazy
        // one, so there is no table to read at that moment and the line said nothing about what the shard
        // does. Said on the first real resolution instead, which is the only moment the answer is true.
        //
        // A derived list that comes back empty is a feature that silently does nothing, and this trade has
        // already been exactly that once today.
        if (!_saidNeeds && want.Count > 0)
        {
            _saidNeeds = true;

            logger.Information(
                "A brewer is sent after {Count} reagents, {Herbs} of each, read off the recipes: {Names}",
                want.Count,
                Herbs,
                string.Join(", ", want.ConvertAll(t => t.Name))
            );
        }


        return want;
    }

    /// <summary>How much skill a recipe asks for. Its minimum, which is what the engine rolls against.</summary>
    public static double Requirement(CraftItem recipe)
    {
        var skills = recipe?.Skills;

        return skills == null || skills.Count == 0 ? 0.0 : skills[0].MinSkill;
    }

    /// <summary>The recipe that makes exactly this potion, or null when this bot cannot brew one.</summary>
    public static CraftItem Recipe(Mobile bot, Type wanted)
    {
        var system = System;

        if (bot == null || system == null || wanted == null)
        {
            return null;
        }

        var able = bot.Skills[SkillName.Alchemy].Value;
        var recipes = system.CraftItems;

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];

            if (recipe.ItemType != wanted || !Twofold(recipe, out var reagent, out var units, out var glass))
            {
                continue;
            }

            if (Requirement(recipe) > able - Margin)
            {
                continue;
            }

            if (Amount(bot, reagent) < units || Bottles(bot) < glass)
            {
                continue;
            }

            return recipe;
        }

        return null;
    }

    /// <summary>
    /// The best thing this bot could brew right now, or null.
    ///
    /// <para>
    /// <b>Only what the population actually drinks.</b> The system carries three tiers of heal and a rack of
    /// buffs and weapons; a bot that brewed the hardest thing it could manage would fill the market with
    /// explosion potions nobody on this island has ever reached for. <see cref="BotArsenal.Draughts"/> is the
    /// shard's own answer to "what is a supply" — the same list birth issues from and the same list
    /// <c>BotShopper</c> restocks against — so brewing follows it and cannot drift from it.
    /// </para>
    ///
    /// <para>
    /// Within that, what somebody has put money down for wins outright; failing that, whichever the bot has
    /// the most material for, so a brewer works through its own glut rather than its own bottleneck.
    /// </para>
    /// </summary>
    public static CraftItem Choose(IBotWilful will, Mobile bot, out Type made)
    {
        made = null;
        Why = Refusal.None;

        if (bot == null || System == null)
        {
            return null;
        }

        // Which of the three grounds the refusal rests on, if it comes to one. See Refusal.
        var anySkilled = false;
        var anyFull = false;

        CraftItem best = null;
        var bestStock = 0;

        CraftItem asked = null;
        var bestBid = 0;
        Type askedKind = null;

        var families = BotArsenal.Draughts;

        for (var i = 0; i < families.Count; i++)
        {
            var kind = BotArsenal.Potion(families[i]);
            var recipe = Recipe(bot, kind);

            if (recipe == null)
            {
                continue;
            }

            anySkilled = true;

            // <b>Five of a kind is the whole of what one bot may have going, pack and stall together.</b> The
            // draught is passed over rather than the round refused, so a brewer at its cap on heal potions
            // goes on to cure — which is the point of counting each kind apart. See Cap and RestMs.
            if (Resting(bot, kind))
            {
                Capped++;
                anyFull = true;

                continue;
            }

            if (Held(will, bot, kind) >= Cap)
            {
                Capped++;
                anyFull = true;
                Rest(bot, kind);

                continue;
            }

            var bid = BotMarket.Best(kind);

            if (bid > bestBid)
            {
                bestBid = bid;
                asked = recipe;
                askedKind = kind;
            }

            var (reagent, units, _) = Costs(recipe);
            var stock = units <= 0 ? 0 : Amount(bot, reagent) / units;

            if (stock <= bestStock)
            {
                continue;
            }

            bestStock = stock;
            best = recipe;
            made = kind;
        }

        // <b>Why, when the answer is nothing.</b> Three quite different grounds arrive at the alchemist as
        // one number — "had the glass but no herbs" — and it was 211 of a window's asks with a share that
        // rose from 14% to 37% inside one run *after* the reagents were put right, which is a pattern in
        // time rather than in supply and could not be read while the three were together. Named in the order
        // that decides what to do: a bot that cannot make anything wants a lesson, one that is full wants
        // nothing, and one merely out of reagents wants an errand.
        if (best == null && asked == null)
        {
            Why = anyFull ? Refusal.Full : anySkilled ? Refusal.Reagentless : Refusal.Unskilled;
        }

        if (asked == null)
        {
            return best;
        }

        made = askedKind;

        return asked;
    }

    /// <summary>
    /// How many more of this recipe the pack could still pay for, counting both halves.
    ///
    /// The binding one of the two, which on this trade swaps about: glass is cheap and comes back off every
    /// potion anybody drinks, and the reagent is the half a gatherer has to walk into a wood for.
    /// </summary>
    public static int Possible(Mobile bot, CraftItem recipe)
    {
        var (reagent, units, glass) = Costs(recipe);

        if (reagent == null || units <= 0 || glass <= 0)
        {
            return 0;
        }

        return Math.Min(Amount(bot, reagent) / units, Bottles(bot) / glass);
    }

    /// <summary>
    /// Which draught this bot is likeliest to be able to brew once it has glass, ignoring the glass.
    ///
    /// <para>
    /// Asked by the errand that goes to a counter for bottles, and it has to be asked without them: at that
    /// moment <see cref="Choose"/> answers null for every recipe, because every recipe wants a bottle. A
    /// brewer that walked to the alchemist for glass and then discovered it had garlic and not ginseng would
    /// have bought its way into the same refusal it set out to cure.
    /// </para>
    /// </summary>
    public static Type Likeliest(IBotWilful will, Mobile bot)
    {
        var system = System;

        if (bot == null || system == null)
        {
            return null;
        }

        var able = bot.Skills[SkillName.Alchemy].Value;
        var recipes = system.CraftItems;
        var families = BotArsenal.Draughts;

        Type best = null;
        var bestStock = -1;

        for (var f = 0; f < families.Count; f++)
        {
            var kind = BotArsenal.Potion(families[f]);

            if (kind == null)
            {
                continue;
            }

            // The same cap the recipe choice keeps, on the errand that walks to a counter for glass: buying
            // bottles to make a sixth heal potion is exactly the trip Cap exists to stop, and leaving it out
            // here would have been a limit on brewing paid for by a walk.
            //
            // <b>Counted, and the counting is the point of this second look.</b> A bot with every draught at
            // its cap answers null here, and the alchemist above reads a null from Likeliest as "no glass and
            // no herbs either" — so within a quarter of an hour of the cap going in, the brew line read "377
            // had neither" about brewers whose packs were full. That is the same bucket-swallowing-a-second-
            // cause this file was corrected for earlier the same night, put back by a new gate with no name.
            if (Resting(bot, kind) || Held(will, bot, kind) >= Cap)
            {
                continue;
            }

            for (var i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];

                if (recipe.ItemType != kind || !Twofold(recipe, out var reagent, out var units, out _))
                {
                    continue;
                }

                if (Requirement(recipe) > able - Margin)
                {
                    continue;
                }

                var stock = units <= 0 ? 0 : Amount(bot, reagent) / units;

                if (stock > bestStock)
                {
                    bestStock = stock;
                    best = kind;
                }

                break;
            }
        }

        return bestStock > 0 ? best : null;
    }

    /// <summary>
    /// How many bottles the herbs in the pack could fill with this draught, up to what the cap still allows of it, or nought.
    ///
    /// <para>
    /// The glass trip's size (<c>BotAlchemist.Glassware</c>). Asked of the same recipe, skill and cap as <see cref="Likeliest"/>,
    /// which chose the draught, and like it without the glass — <see cref="Recipe"/> refuses a recipe the pack has no bottle
    /// for, which is every recipe at the moment this is asked.
    /// </para>
    /// </summary>
    public static int GlassFor(IBotWilful will, Mobile bot, Type potion)
    {
        var system = System;

        if (bot == null || system == null || potion == null)
        {
            return 0;
        }

        var able = bot.Skills[SkillName.Alchemy].Value;
        var recipes = system.CraftItems;

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];

            if (recipe.ItemType != potion || !Twofold(recipe, out var reagent, out var units, out var glass) || units <= 0 || glass <= 0)
            {
                continue;
            }

            if (Requirement(recipe) > able - Margin)
            {
                return 0;
            }

            var room = Math.Max(0, Cap - Held(will, bot, potion));

            return Math.Min(Amount(bot, reagent) / units, room) * glass;
        }

        return 0;
    }

    /// <summary>One attempt. The engine rolls, and the pack is counted afterwards — see BotCraftwork.Swing.</summary>
    public static bool Swing(Mobile bot, CraftItem recipe, Type reagent, BaseTool tool) =>
        BotCraftwork.Swing(bot, System, recipe, reagent, tool);

    /// <summary>How many of that thing are in the pack. The only honest way to count what a swing produced.</summary>
    public static int Made(Mobile bot, Type kind) => BotCraftwork.Made(bot, kind);
}
