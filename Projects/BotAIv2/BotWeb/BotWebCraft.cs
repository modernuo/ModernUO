using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The crafting chain as the engine defines it and the population fills it: for each material the craft
/// systems consume, where it comes from, how much is on the market, what can be made from it, who can make
/// each thing, and what is wanted.
///
/// <para>
/// <b>Read out of the craft systems, not written down here.</b> Every recipe, its skill and its material
/// come from the same <c>CraftSystem</c> tables the bots use, so the page cannot describe a chain the bots
/// cannot walk. What this file adds is the accounting: the sources of the raw materials (which the tables do
/// not know), the market's stock, the population's skills and the day's output.
/// </para>
/// </summary>
public static class BotWebCraft
{
    /// <summary>Recipes whose material is one of these are shown; the rest of a system is folded into "other".</summary>
    private static readonly (string Name, Func<CraftSystem> System, SkillName Skill)[] Systems =
    [
        ("Blacksmithy", () => DefBlacksmithy.CraftSystem, SkillName.Blacksmith),
        ("Tailoring", () => DefTailoring.CraftSystem, SkillName.Tailoring),
        ("Alchemy", () => DefAlchemy.CraftSystem, SkillName.Alchemy),
        ("Fletching", () => DefBowFletching.CraftSystem, SkillName.Fletching),
        ("Cooking", () => DefCooking.CraftSystem, SkillName.Cooking),
        ("Inscription", () => DefInscription.CraftSystem, SkillName.Inscribe),
        ("Tinkering", () => DefTinkering.CraftSystem, SkillName.Tinkering),
        ("Carpentry", () => DefCarpentry.CraftSystem, SkillName.Carpentry)
    ];

    /// <summary>Who gathers a raw material: nobody (it is bought), whoever has a skill, one class, or every fighter.</summary>
    private enum Gatherers
    {
        Nobody,
        BySkill,
        Gatherer,
        Fighters
    }

    /// <summary>Where a raw material comes from, for the ones the craft tables cannot say.</summary>
    private static (string From, string How, Gatherers Who, SkillName Skill) Source(Type material)
    {
        if (material == null)
        {
            return ("?", "?", Gatherers.Nobody, SkillName.Alchemy);
        }

        if (typeof(BaseIngot).IsAssignableFrom(material))
        {
            return (material.Name.Replace("Ingot", "Ore"), "mined and smelted at a forge", Gatherers.BySkill, SkillName.Mining);
        }

        if (material == typeof(Board) || material == typeof(Log) || material.Name.EndsWith("Board", StringComparison.Ordinal) || material.Name.EndsWith("Log", StringComparison.Ordinal))
        {
            return ("Log", "felled", Gatherers.BySkill, SkillName.Lumberjacking);
        }

        if (material == typeof(Leather) || typeof(BaseLeather).IsAssignableFrom(material) || material == typeof(Hides) || typeof(BaseHides).IsAssignableFrom(material))
        {
            return ("Hides", "carved off a hunted creature and cut", Gatherers.Fighters, SkillName.Anatomy);
        }

        if (material == typeof(Cloth) || material == typeof(BoltOfCloth) || material == typeof(UncutCloth))
        {
            return ("Cotton", "bought from a tailor", Gatherers.Nobody, SkillName.Tailoring);
        }

        if (material == typeof(Shaft))
        {
            return ("Log", "cut from a log", Gatherers.BySkill, SkillName.Fletching);
        }

        if (material == typeof(Bottle) || material == typeof(BlankScroll) || material == typeof(Feather))
        {
            return (material.Name, "bought from a shop", Gatherers.Nobody, SkillName.Alchemy);
        }

        if (typeof(BaseReagent).IsAssignableFrom(material))
        {
            return (material.Name, "picked by a gatherer, or bought from a mage", Gatherers.Gatherer, SkillName.Alchemy);
        }

        if (material == typeof(RawRibs) || material == typeof(RawBird) || material == typeof(RawLambLeg) || material == typeof(RawChickenLeg) || material == typeof(RawFishSteak))
        {
            return (material.Name, "carved off a hunted creature", Gatherers.Fighters, SkillName.Anatomy);
        }

        return (material.Name, "bought or found", Gatherers.Nobody, SkillName.Alchemy);
    }

    /// <summary>How many alive bots gather that way.</summary>
    private static int Gathering(Gatherers who, SkillName skill, Dictionary<SkillName, List<double>> able, int gatherers, int fighters) =>
        who switch
        {
            Gatherers.BySkill => Count(able, skill, 20.0),
            Gatherers.Gatherer => gatherers,
            Gatherers.Fighters => fighters,
            _ => 0
        };

    private sealed class Chain
    {
        public string System;
        public Type Material;
        public List<CraftItem> Products = [];
    }

    public static string Build()
    {
        var bots = BotPopulation.Bots;

        // Every alive bot's skills, by skill, sorted, so "how many can make this" is a binary search.
        Dictionary<SkillName, List<double>> able = [];
        var gatherers = 0;
        var fighters = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Alive: true } || bot.Skills == null)
            {
                continue;
            }

            if (bot.Class?.Name == "Gatherer")
            {
                gatherers++;
            }

            if (bot.Class?.Role is BotRole.Melee or BotRole.Ranged or BotRole.Caster)
            {
                fighters++;
            }

            foreach (var skill in Skills)
            {
                var value = bot.Skills[skill]?.Value ?? 0.0;

                if (value <= 0.0)
                {
                    continue;
                }

                if (!able.TryGetValue(skill, out var list))
                {
                    able[skill] = list = [];
                }

                list.Add(value);
            }
        }

        foreach (var list in able.Values)
        {
            list.Sort();
        }

        // The market, by kind: what the bots' shops by the bank show and their cheapest ask (market change, 01.10.2026: the
        // auction's stalls are gone; the JSON's "onMarket" and "cheapest" keep their names and read the shops now).
        Dictionary<Type, (int Units, int Cheapest)> stock = [];

        foreach (var front in BotShopkeep.Open)
        {
            if (front == null || front.Shut)
            {
                continue;
            }

            foreach (var (kind, units, ask) in front.Ranked())
            {
                if (kind == null || units <= 0)
                {
                    continue;
                }

                stock[kind] = stock.TryGetValue(kind, out var s) ? (s.Units + units, Math.Min(s.Cheapest, ask)) : (units, ask);
            }
        }

        Dictionary<Type, int> wanted = [];

        foreach (var want in BotMarket.Wants)
        {
            if (want?.Kind == null)
            {
                continue;
            }

            var left = Math.Max(0, want.Amount);

            wanted[want.Kind] = wanted.TryGetValue(want.Kind, out var n) ? n + left : left;
        }

        // The recipes, grouped by system and first material.
        Dictionary<(string, Type), Chain> chains = [];

        foreach (var (name, get, _) in Systems)
        {
            CraftSystem system;

            try
            {
                system = get();
            }
            catch
            {
                continue;
            }

            if (system?.CraftItems == null)
            {
                continue;
            }

            var recipes = system.CraftItems;

            for (var i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];

                if (recipe?.ItemType == null || recipe.Resources == null || recipe.Resources.Count == 0)
                {
                    continue;
                }

                var material = recipe.Resources[0].ItemType;

                if (material == null)
                {
                    continue;
                }

                if (!chains.TryGetValue((name, material), out var chain))
                {
                    chains[(name, material)] = chain = new Chain { System = name, Material = material };
                }

                chain.Products.Add(recipe);
            }
        }

        List<Chain> ordered = [.. chains.Values];

        ordered.Sort(
            (a, b) =>
            {
                var s = string.CompareOrdinal(a.System, b.System);

                return s != 0 ? s : b.Products.Count.CompareTo(a.Products.Count);
            }
        );

        return BotJson.Object(
            w =>
            {
                w.WriteString("at", DateTime.Now.ToString("HH:mm:ss"));
                w.WritePropertyName("chains");
                w.WriteStartArray();

                foreach (var chain in ordered)
                {
                    var (from, how, who, bySkill) = Source(chain.Material);
                    var has = stock.TryGetValue(chain.Material, out var s);

                    w.WriteStartObject();
                    w.WriteString("system", chain.System);
                    w.WriteString("material", chain.Material.Name);
                    w.WriteString("from", from);
                    w.WriteString("how", how);
                    w.WriteNumber("gatherers", Gathering(who, bySkill, able, gatherers, fighters));
                    w.WriteNumber("onMarket", has ? s.Units : 0);
                    w.WriteNumber("cheapest", has ? s.Cheapest : 0);
                    w.WriteNumber("wanted", wanted.TryGetValue(chain.Material, out var wm) ? wm : 0);
                    w.WriteNumber("madeToday", BotCraftwork.MadeOf(chain.Material));
                    w.WriteString("into", chain.System);

                    var skillOf = MainSkill(chain.Products);
                    var least = double.MaxValue;

                    foreach (var recipe in chain.Products)
                    {
                        least = Math.Min(least, BotCraftwork.Requirement(recipe, skillOf));
                    }

                    w.WriteNumber("makers", skillOf == SkillName.Alchemy && least == double.MaxValue ? 0 : Count(able, skillOf, least == double.MaxValue ? 0.0 : least));
                    w.WriteString("skill", skillOf.ToString());

                    w.WritePropertyName("products");
                    w.WriteStartArray();

                    foreach (var recipe in chain.Products)
                    {
                        var kind = recipe.ItemType;
                        var need = BotCraftwork.Requirement(recipe, skillOf);
                        var onMarket = stock.TryGetValue(kind, out var ps) ? ps.Units : 0;

                        w.WriteStartObject();
                        w.WriteString("item", kind.Name);
                        w.WriteString("skill", skillOf.ToString());
                        w.WriteNumber("minSkill", Math.Round(need, 1));
                        w.WriteNumber("needs", recipe.Resources[0].Amount);
                        w.WriteNumber("materials", recipe.Resources.Count);
                        w.WriteBoolean("needsHeat", recipe.NeedHeat);
                        w.WriteBoolean("needsOven", recipe.NeedOven);
                        w.WriteNumber("makers", Count(able, skillOf, need));
                        w.WriteNumber("onMarket", onMarket);
                        w.WriteNumber("wanted", wanted.TryGetValue(kind, out var wk) ? wk : 0);
                        w.WriteNumber("madeToday", BotCraftwork.MadeOf(kind));
                        w.WriteEndObject();
                    }

                    w.WriteEndArray();
                    w.WriteEndObject();
                }

                w.WriteEndArray();

                w.WritePropertyName("refusals");
                w.WriteStartArray();

                foreach (var (number, text, count) in BotCraftEar.Tallies())
                {
                    w.WriteStartObject();
                    w.WriteString("what", text);
                    w.WriteNumber("cliloc", number);
                    w.WriteNumber("count", count);
                    w.WriteEndObject();
                }

                w.WriteEndArray();

                w.WritePropertyName("wants");
                w.WriteStartArray();

                foreach (var want in BotMarket.Wants)
                {
                    if (want?.Kind == null)
                    {
                        continue;
                    }

                    w.WriteStartObject();
                    w.WriteString("item", want.Kind.Name);
                    // "lodged" was the escrow; a want holds no gold since 01.10.2026, so it is nought, and "coming" is what carriers
                    // are bringing to it now. "amount" is what is still wanted (Amount comes down as goods are handed over).
                    w.WriteNumber("amount", Math.Max(0, want.Amount));
                    w.WriteNumber("price", want.Offer);
                    w.WriteNumber("lodged", 0);
                    w.WriteNumber("coming", want.Coming);
                    w.WriteNumber("raised", want.Raises);
                    w.WriteNumber("filled", want.Filled);
                    BotJson.StringOrNull(w, "by", want.Buyer?.Self?.Name);
                    w.WriteBoolean("makeable", BotShopper.Makeable(want.Kind));
                    w.WriteEndObject();
                }

                w.WriteEndArray();

                w.WritePropertyName("produced");
                w.WriteStartArray();

                foreach (var (kind, made) in BotCraftwork.Produced())
                {
                    w.WriteStartObject();
                    w.WriteString("item", kind.Name);
                    w.WriteNumber("made", made);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }
        );
    }

    private static readonly SkillName[] Skills =
    [
        SkillName.Blacksmith, SkillName.Tailoring, SkillName.Alchemy, SkillName.Fletching, SkillName.Cooking, SkillName.Inscribe,
        SkillName.Tinkering, SkillName.Carpentry, SkillName.Mining, SkillName.Lumberjacking, SkillName.Anatomy, SkillName.Herding
    ];

    private static SkillName MainSkill(List<CraftItem> products)
    {
        Dictionary<SkillName, int> votes = [];

        foreach (var recipe in products)
        {
            var skills = recipe.Skills;

            if (skills == null || skills.Count == 0)
            {
                continue;
            }

            var skill = skills[0].SkillToMake;

            votes[skill] = votes.TryGetValue(skill, out var n) ? n + 1 : 1;
        }

        var best = SkillName.Blacksmith;
        var most = -1;

        foreach (var (skill, n) in votes)
        {
            if (n > most)
            {
                most = n;
                best = skill;
            }
        }

        return best;
    }

    /// <summary>How many alive bots have at least that much of the skill.</summary>
    private static int Count(Dictionary<SkillName, List<double>> able, SkillName skill, double least)
    {
        if (!able.TryGetValue(skill, out var list) || list.Count == 0)
        {
            return 0;
        }

        var lo = 0;
        var hi = list.Count;

        while (lo < hi)
        {
            var mid = (lo + hi) / 2;

            if (list[mid] < least)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return list.Count - lo;
    }
}
