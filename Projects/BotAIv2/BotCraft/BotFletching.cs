using System;
using Server.Engines.Craft;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// What a fletcher needs to know: the craft system, the tool, and the two-step chain that turns a log and a
/// feather into an arrow.
///
/// <para>
/// <b>Why this trade exists at all.</b> Arrows were the only consumable on this shard with no source. The
/// population is born with a hundred and fifty apiece and thirteen bots shoot them; the provisioner keeps
/// twenty at a time and the variety dealer thirty to sixty; gleaning brings back one or two off the ground
/// and the rest break. So the arrow supply could only ever fall, and on the morning of 04.09.2026 it hit
/// bottom: five archers failed sixty-seven fights in ten minutes, every one of them reading "100% of it left
/// and not a scratch in 45s" — a bow being swung with nothing in it. The blade in the pack (see
/// <c>BotArms.Quiver</c>) is what stops that being a dead bot; this is what stops it happening.
/// </para>
///
/// <para>
/// <b>Two steps and no station, which is what makes this the shortest craft on the shard.</b> A smith needs
/// a forge with an anvil beside it and a miner to bring it metal. Fletching needs a tool in the pack and
/// nothing else: <c>Log → Shaft</c>, then <c>Shaft + Feather → Arrow</c>, both at Fletching 0–40, which is
/// to say anybody who owns the tool can do it. And every recipe in the chain is <c>SetUseAllRes</c>, so one
/// action spends everything the pack holds — a hundred logs become a hundred shafts in a single swing.
/// </para>
///
/// <para>
/// <b>Where the two materials come from is the whole point of the trade.</b> Logs are sold by carpenters, so
/// they are money. Feathers are sold by <em>nobody</em> — there is not one <c>typeof(Feather)</c> in any
/// vendor's stock on this shard — and come off birds, twenty-five to a chicken and thirty-six to an eagle,
/// which the hunters kill all day and list on the population's own market. So the arrow is the first thing
/// on this island that cannot be made without something another bot went out and killed.
/// </para>
/// </summary>
public static class BotFletching
{
    /// <summary>The engine's fletching system, or null before content initialisation has built it.</summary>
    public static CraftSystem System => DefBowFletching.CraftSystem;

    /// <summary>The tool. Without one a bot with the skill is a bot with an opinion about arrows.</summary>
    public static BaseTool Kit(Mobile bot) => BotOutfit.Oldest<FletcherTools>(bot?.Backpack);

    /// <summary>What one arrow eats: one shaft and one feather.</summary>
    public const int PerArrow = 1;

    /// <summary>What one shaft eats: one log.</summary>
    public const int PerShaft = 1;

    /// <summary>How many arrows are worth making a trip for. Below this the chain costs more than it returns.</summary>
    public static int LeastArrows { get; set; } = 20;

    /// <summary>
    /// How much a made arrow opens at on the market.
    ///
    /// <para>
    /// <b>Two, which is what the sentence here has always claimed and what the number never was.</b> It read
    /// three and called itself "the provisioner's own price"; the provisioner asks two —
    /// <c>SBProvisioner</c>, <c>GenericBuyInfo(typeof(Arrow), 2, 20, …)</c> — and one gold is the whole
    /// difference between a trade and a stall that stands until the peddler carries it to a counter. A
    /// shopkeeper is the ceiling everywhere else in this market; an opening ask above it is a thing no bot on
    /// this island can ever rationally buy.
    /// </para>
    /// </summary>
    public static int Worth { get; set; } = 2;

    public static int Amount(Mobile bot, Type stuff) =>
        stuff == null ? 0 : bot?.Backpack?.GetAmount(stuff) ?? 0;

    public static int Logs(Mobile bot) => Amount(bot, typeof(Log));

    public static int Shafts(Mobile bot) => Amount(bot, typeof(Shaft));

    public static int Feathers(Mobile bot) => Amount(bot, typeof(Feather));

    /// <summary>
    /// How many feathers a fletcher keeps off a corpse rather than listing them.
    ///
    /// <para>
    /// <b>The half nobody sells was being sold.</b> No shopkeeper on this shard stocks a feather — the fletcher
    /// says so in as many words — so the only feathers on the island are the ones that come off birds. And
    /// <c>BotSlay.Rifle</c> lists everything lifted off a corpse, so a fletcher standing over a bird put its
    /// only source of feathers straight onto a stall and then read "no feathers and nobody sells one" on the
    /// next beat: 65 of 262 asks on 05.09.2026, with <c>BotAuction.Reclaim</c> already in the proposer as a
    /// patch on the same wound.
    /// </para>
    ///
    /// <para>
    /// Twenty, which is <see cref="LeastArrows"/> — a round of arrows and no more, so a fletcher stocks itself
    /// and everything past that still reaches the market for whoever else wants it.
    /// </para>
    /// </summary>
    public static int Keeps { get; set; } = 20;

    /// <summary>Stacks of feathers kept back for a fletcher's own use. For the summary.</summary>
    public static long Spared { get; private set; }

    /// <summary>Stacks sold on instead, because nobody there could fletch or they were already stocked.</summary>
    public static long Sold { get; private set; }

    /// <summary>Feathers this bot should keep for its own bench instead of listing. See <see cref="Keeps"/>.</summary>
    public static bool Spares(Mobile bot, Item item)
    {
        if (item == null || bot == null || item.GetType() != typeof(Feather) || Kit(bot) == null)
        {
            return false;
        }

        // Counted after it is in the pack, which is where the caller lifts it to before asking.
        if (Feathers(bot) <= Keeps)
        {
            Spared++;

            return true;
        }

        Sold++;

        return false;
    }

    /// <summary>Forgotten with the world.</summary>
    public static void ForgetTrade()
    {
        Spared = 0;
        Sold = 0;
    }

    /// <summary>
    /// How many arrows this bot could make right now without buying anything.
    ///
    /// <para>
    /// Shafts it already holds plus shafts its logs would become, capped by feathers — because a feather is
    /// the half nobody sells and is therefore always the binding constraint. Stated as one number so that
    /// every gate in the trade asks the same question and no two of them can drift apart.
    /// </para>
    /// </summary>
    public static int Possible(Mobile bot)
    {
        var shafts = Shafts(bot) + Logs(bot) / PerShaft;

        return Math.Min(shafts, Feathers(bot)) / PerArrow;
    }

    /// <summary>The recipe for one named thing out of one named material, or null if this bot cannot work it.</summary>
    public static CraftItem Recipe(Mobile bot, Type material, Type wanted) =>
        BotCraftwork.Recipe(bot, System, SkillName.Fletching, material, wanted);

    /// <summary>
    /// The recipe that feathers a shaft into an arrow, or null when this bot cannot work it.
    ///
    /// <para>
    /// <b>This needs its own lookup, and its absence is why no bot on this shard has ever made an arrow.</b>
    /// <c>BotCraftwork.Simple</c> — which every other recipe lookup here goes through — refuses any recipe
    /// with more than one resource, by design and for good reasons on the trades it serves. The arrow has
    /// two: <c>DefBowFletching</c> declares <c>AddCraft(typeof(Arrow), …, typeof(Shaft), …)</c> and then
    /// <c>AddRes(index, typeof(Feather), …)</c> on the next line. So the general lookup answered null for
    /// the arrow every time it was ever asked, and the trade reported "it does not know how to feather a
    /// shaft" — twelve times in half an hour on 04.09.2026, the first half hour in which any fletcher had
    /// ever held a feather to try it with.
    /// </para>
    ///
    /// <para>
    /// Shaft into arrow is the only two-material step in this trade; log into shaft is a single material and
    /// still goes through the general lookup. See <c>BotFlask</c>, where every recipe in the trade is a pair
    /// and the whole file exists for that reason.
    /// </para>
    /// </summary>
    public static CraftItem Feathering(Mobile bot)
    {
        var system = System;

        if (bot == null || system == null)
        {
            return null;
        }

        var able = bot.Skills[SkillName.Fletching].Value;
        var recipes = system.CraftItems;

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            var resources = recipe.Resources;

            if (recipe.ItemType != typeof(Arrow) || resources == null || resources.Count != 2)
            {
                continue;
            }

            var shafts = 0;
            var feathers = 0;

            for (var r = 0; r < resources.Count; r++)
            {
                var res = resources[r];

                if (res.ItemType == typeof(Shaft))
                {
                    shafts = Math.Max(1, res.Amount);
                }
                else if (res.ItemType == typeof(Feather))
                {
                    feathers = Math.Max(1, res.Amount);
                }
            }

            if (shafts <= 0 || feathers <= 0)
            {
                continue;
            }

            if (BotCraftwork.Requirement(recipe, SkillName.Fletching) > able - BotCraftwork.Margin)
            {
                continue;
            }

            // Both halves counted, because a swing spends both and the engine simply declines when either is
            // short — silently, which is what makes an unasked question here cost a whole afternoon.
            if (Shafts(bot) < shafts || Feathers(bot) < feathers)
            {
                continue;
            }

            return recipe;
        }

        return null;
    }

    /// <summary>One turn of the handle. True when the engine accepted the attempt.</summary>
    public static bool Swing(Mobile bot, CraftItem recipe, Type material, BaseTool tool) =>
        BotCraftwork.Swing(bot, System, recipe, material, tool);

    /// <summary>How many of that thing are in the pack. The only honest way to count what a swing produced.</summary>
    public static int Made(Mobile bot, Type kind) =>
        kind == null ? 0 : bot?.Backpack?.GetAmount(kind, true) ?? 0;
}
