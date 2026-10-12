using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Turns a class's kit into things a bot is actually holding.
///
/// <para>
/// One step, run once at birth, and it is the only place in v2 that creates a bot's belongings. The
/// first version had this as a switch statement inside the bot itself, which meant that "what does a
/// smith own" was a question you answered by reading control flow, and that the ordering rules below —
/// every one of them a bug that was paid for — were scattered through it as comments.
/// </para>
///
/// <para>
/// <b>Order is not cosmetic here.</b> A two-handed weapon is refused by the engine while anything at
/// all is in the other hand, so whatever needs both hands goes on first. Handing out the dagger before
/// the bow cost the first version ten archers who spent their entire lives stabbing skeletons with
/// knives while carrying the bows they had trained for — and it was invisible, because the bow was in
/// the pack, exactly where a spare weapon belongs.
/// </para>
/// </summary>
public static class BotOutfit
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOutfit));

    /// <summary>
    /// Bottles for whoever brews. Not bound: they are consumed, and a brewer that runs out of glass has
    /// a reason to go to town, which is a reason worth having.
    /// </summary>
    private const int StartingBottles = 8;

    /// <summary>
    /// Blank scrolls for whoever writes. Enough to find out whether the trade pays before walking to a
    /// shop for more, and not enough to fill a book with.
    /// </summary>
    private const int StartingScrolls = 20;

    /// <summary>
    /// Coin every bot is born holding, set from <c>Configuration/bot-population.json</c>.
    ///
    /// <para>
    /// <b>Seed capital, and the shard does not work without some.</b> Gold enters this world in exactly one
    /// place — a monster's purse — so a population born with nothing has nothing with which to start any work
    /// that costs something: sewing buys cloth, writing buys paper, restocking buys bandages, and every one of
    /// them fails on its first beat against an empty purse. The only thing that can happen then is digging,
    /// because digging is free. This is the float that lets the other trades open at all, and it is spent
    /// rather than replenished: once it has gone round, what keeps the economy running is what the hunters
    /// bring in.
    /// </para>
    ///
    /// <para>
    /// It is not bound. Bound is for the tools of a trade; coin is meant to move, and a purse that death could
    /// not touch would make dying free in the one currency that matters.
    /// </para>
    /// </summary>
    public static int Purse { get; set; } = 400;

    /// <summary>Bots outfitted since the shard came up. For the summary.</summary>
    public static int Outfitted { get; private set; }

    /// <summary>Things bound to their owners, all told.</summary>
    public static int Bound { get; private set; }

    /// <summary>
    /// Dresses a bot in its class and returns the record of what may not be taken from it.
    ///
    /// The bond comes back rather than being filed in a table here: the bot owns it, and when the bot
    /// is deleted it goes with it. See <see cref="BotBond"/> for why that matters.
    /// </summary>
    public static BotBond Give(Mobile bot, BotClass klass)
    {
        var bond = new BotBond();

        if (bot?.Backpack == null || klass == null)
        {
            logger.Warning("Asked to outfit a bot with no pack or no class; nothing handed over");
            return bond;
        }

        var kit = klass.Kit;

        // Both hands first, whatever wants them: the staff, or the bow. No class has both.
        GiveStaff(bot, klass, bond);
        GiveWeapon(bot, kit.Ranged, bond);

        // Then the one-handed blade, for classes that roll one.
        GiveWeapon(bot, kit.Melee, bond);

        // Then the archer's knife, which goes in the pack on purpose — it is what the bot falls back to,
        // not what it opens with, and putting it in a hand would take the bow out of two.
        if (kit.Sidearm.HasValue)
        {
            Hand(bot, BotBinding.Make(kit.Sidearm.Value.Weapon, 1), bond, pack: true);
        }

        GiveTools(bot, klass);
        GiveArmour(bot, kit, bond);
        GiveBook(bot, kit, bond);
        GiveSupplies(bot, klass);

        Outfitted++;

        return bond;
    }

    /// <summary>
    /// Rolls one of the offered weapons and gives it. Nothing happens for a class that is offered none —
    /// the brawler's whole point is empty hands.
    /// </summary>
    private static void GiveWeapon(Mobile bot, IReadOnlyList<BotWeaponOption> options, BotBond bond)
    {
        if (options == null || options.Count == 0 || bond.Weapon.HasValue)
        {
            return;
        }

        // One roll, and it settles three things at once: what the bot holds, which skill it will train,
        // and what it consumes. They are one decision because they are one fact — see BotWeaponOption.
        // <b>Born to a weapon the body cannot lift.</b> The roll took any option of the kit, and the options ask
        // different strength: 769 things were "passed over as beyond this body" in one day of 16.09.2026, and Lysa
        // fought bare-handed with a war mace in her pack that wanted 66 of strength she did not have. The options are
        // tried from a random start and the first the body can wield is taken; when none can be, the lightest is, and
        // the body is given the strength for it — a birth being the one moment a stat is free. Patrick's decision,
        // that evening.
        var start = Utility.Random(options.Count);
        var chosen = options[start];
        Item weapon = null;
        Item lightest = null;
        var lightestOption = chosen;
        var least = int.MaxValue;

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[(start + i) % options.Count];
            var made = BotBinding.Make(option.Weapon, 1);

            if (made == null)
            {
                continue;
            }

            var need = made is BaseWeapon arm ? arm.StrRequirement : 0;

            if (bot.Str >= need)
            {
                weapon = made;
                chosen = option;

                if (i > 0)
                {
                    Refitted++;
                }

                break;
            }

            if (need < least)
            {
                lightest?.Delete();
                lightest = made;
                lightestOption = option;
                least = need;
            }
            else
            {
                made.Delete();
            }
        }

        if (weapon == null)
        {
            if (lightest == null)
            {
                return;
            }

            weapon = lightest;
            chosen = lightestOption;
            lightest = null;
            Overborn++;

            if (bot.RawStr < least)
            {
                bot.RawStr = least;
            }
        }

        lightest?.Delete();

        bond.Weapon = chosen;

        Hand(bot, weapon, bond, pack: false);

        if (chosen.Ammunition == null || chosen.AmmunitionCount <= 0)
        {
            return;
        }

        var ammunition = BotBinding.Make(chosen.Ammunition, chosen.AmmunitionCount);

        if (ammunition == null)
        {
            return;
        }

        bot.Backpack.DropItem(ammunition);

        // Bound by count rather than by flag, which is the only way the rule about a spent quiver can be
        // stated at all. See BotBinding.TrimAmmunition.
        BotBinding.BindStack(ammunition, chosen.AmmunitionCount, bond);
        Bound++;
    }

    private static void GiveStaff(Mobile bot, BotClass klass, BotBond bond)
    {
        if (!klass.Kit.Staff)
        {
            return;
        }

        var staff = new BotCasterStaff { Hue = klass.StaffHue };

        Hand(bot, staff, bond, pack: false);
    }

    /// <summary>
    /// Every tool this class should be holding: what its kit names, plus the ones its skills imply.
    ///
    /// <para>
    /// <b>One list, asked by two callers, and that is the whole point of it existing.</b> Birth hands these
    /// out; <see cref="BotShopper"/> reads the same list to notice one has gone and buy another. A second
    /// list of "which tools does a mage need" kept next to the shopping code is a list that will disagree
    /// with this one the first time a class changes.
    /// </para>
    ///
    /// <para>
    /// The mortar and the pen are derived from the build rather than named in the kit, on purpose: brewing
    /// and writing are open to anybody with the skill and the materials, so what a class has is the
    /// disposition — and a skill target is exactly what a disposition is.
    /// </para>
    /// </summary>
    public static List<Type> ToolsFor(BotClass klass)
    {
        List<Type> tools = [];

        if (klass == null)
        {
            return tools;
        }

        var kit = klass.Kit;

        for (var i = 0; i < kit.Tools.Count; i++)
        {
            tools.Add(kit.Tools[i]);
        }

        // Universal, and not a class's business: cloth off a corpse becomes bandages with these, so a bot
        // that loots is a bot that goes on healing.
        tools.Add(typeof(Scissors));

        // <b>The same argument, and it was the whole reason leather did not exist on this shard.</b> Hide
        // enters the world through exactly one door — a corpse somebody carves — and nobody was carrying
        // anything to carve with but the swordsmen, who happen to be holding a blade for other reasons. So a
        // tailor's every order for anything cut from hide was unfillable at any price, with the material
        // lying in the grass all over Felucca.
        //
        // Named here rather than in a class kit because skinning is a disposition and not a trade: it costs
        // no skill, it is one call to the engine, and a bot standing over something it just killed is already
        // in the only place it can be done. A mage with a staff has as much business taking a hide as a
        // swordsman. See BotSlay's Skin, and note that being on this list is also what makes BotShopper buy
        // another when one goes and what stops BotUnload selling it.
        tools.Add(typeof(SkinningKnife));

        if (Wants(klass, SkillName.Alchemy))
        {
            tools.Add(typeof(MortarPestle));
        }

        // <b>Universal, like the scissors and the skinning knife, and for the same argument.</b> Patrick's
        // order of 05.09.2026 opened cooking to the population, and cooking is a disposition rather than a
        // trade: the ingredient is a carcass, everybody hunts, DefCooking asks for no fire and no place, and
        // the first recipes want no skill at all. A skillet in every pack is what makes the meat that was
        // being walked across the island for two gold into a supper somebody wants. See BotOven.
        tools.Add(typeof(Skillet));

        if (Wants(klass, SkillName.Inscribe))
        {
            tools.Add(typeof(ScribesPen));
        }

        // The tinker's tools, for the class that targets Tinkering: the one trade that makes every other
        // trade's tool. See BotTinkering.
        if (Wants(klass, SkillName.Tinkering))
        {
            tools.Add(typeof(TinkerTools));
        }

        return tools;
    }

    /// <summary>
    /// How few uses a tool may have left before the next one is bought, while this one still works.
    ///
    /// <para>
    /// <b>The engine destroys a tool on its last use, and the job it happens in fails for it.</b> On 24.09.2026 that
    /// was "nothing to dig with" 52 times, "no hammer" 27, "nothing to sew with" 6 and "nothing to fletch with" 3 —
    /// five to ten in every hundred failures of every session — because the shopper wanted a tool only when there
    /// was none, so every tool's life ended inside a job. Bryn's pickaxe, bought at 12:31:31, went through fifty
    /// swings over eight trips and gave out on the first swing of the ninth. Five is half a dig and most of a forge
    /// shift: a trip begun below it is the trip the tool will not finish.
    /// </para>
    /// </summary>
    public static int SpareAt { get; set; } = 5;

    /// <summary>Whether this tool is near enough its end that the next one should be bought. See <see cref="SpareAt"/>.</summary>
    public static bool Spent(Item tool) => tool is IUsesRemaining { UsesRemaining: var left } && left <= SpareAt;

    /// <summary>
    /// The most worn of these in the pack — fewest uses left — or null.
    ///
    /// <para>
    /// <b>The old one first, so that buying the next one before the last gives out does not become carrying two.</b>
    /// The engine's own search answers with whichever it meets first, and a fresh tool met first would leave the
    /// spent one in the pack for ever — and with it the want for yet another, since the shopper asks whether every
    /// tool of the kind is spent.
    /// </para>
    /// </summary>
    public static T Oldest<T>(Container pack, Predicate<T> can = null) where T : Item
    {
        if (pack == null)
        {
            return null;
        }

        T oldest = null;
        var least = int.MaxValue;

        foreach (var item in pack.FindItemsByType<T>(true, can))
        {
            var left = item is IUsesRemaining worn ? worn.UsesRemaining : int.MaxValue;

            if (oldest == null || left < least)
            {
                oldest = item;
                least = left;
            }
        }

        return oldest;
    }

    /// <summary>
    /// Hands out the tools, <b>unbound</b>.
    ///
    /// <para>
    /// <b>Only the weapon is the bot's for ever.</b> A tool is a thing that wears through: the engine gives a
    /// fresh one 25 to 75 uses, spends one an attempt and destroys it at zero. That is the mechanic, not a
    /// defect in it — and the honest answer to a smith with no hammer is not an unbreakable hammer, it is a
    /// smith walking to a shop with its own money. It is also the only reason a shard needs toolmakers at
    /// all, and it puts a floor under what a trade costs to keep running.
    /// </para>
    ///
    /// <para>
    /// So a tool weighs, drops on death and has to be bought again — and none of that is a problem the way it
    /// was for the weapon, because <see cref="BotShopper"/> answers all three with one errand. What must
    /// never happen is the silent version: a bot whose tool is gone and who does not know to replace it looks
    /// exactly like a bot that never had one.
    /// </para>
    /// </summary>
    private static void GiveTools(Mobile bot, BotClass klass)
    {
        var pack = bot.Backpack;
        var tools = ToolsFor(klass);

        for (var i = 0; i < tools.Count; i++)
        {
            var tool = tools[i].CreateInstance<Item>();

            if (tool == null)
            {
                continue;
            }

            // <b>Into the pack, never into a hand.</b> This used to try a hand first, and the comment that
            // did it admitted in its own last sentence that there was nothing to gain: the engine's CheckTool
            // does not ask to be held, and a pickaxe, a hammer and a pen all work from the pack. What it cost
            // was the sage, born with no weapon in its kit, standing in a fight holding a skinning knife —
            // because a knife is a BaseWeapon as far as the engine is concerned, the hand was free, and the
            // one thing that would have taken it back out only ever adds.
            //
            // The rule is the population's, not this method's: a tool comes out when there is something to
            // use it on, and between times a bot holds what it fights with. See BotMobile.Rank, which says
            // the same thing at the other end.
            pack.DropItem(tool);
        }
    }

    private static void GiveBook(Mobile bot, BotKit kit, BotBond bond)
    {
        var spells = kit.Spells;

        if (spells.Count == 0)
        {
            return;
        }

        var book = new Spellbook();

        for (var i = 0; i < spells.Count; i++)
        {
            book.Content |= 1ul << spells[i];
        }

        // In the pack, never in a hand — that is the whole reason the staff exists. Bound, because a
        // caster that loses its book is not a caster and cannot earn the price of another.
        Hand(bot, book, bond, pack: true);
    }

    /// <summary>
    /// Bandages, reagents, glass and paper. None of it bound: it is meant to run out.
    ///
    /// <para>
    /// <b>All eight reagents, not the six the starting spells happen to need.</b> Six was right while a
    /// book held three spells and could hold no more. It stops being right the moment a book grows: half
    /// of the first three circles want bloodmoss or mandrake root — clumsy, agility, cunning, strength,
    /// bless, teleport, unlock, wall of stone — and so does almost every scroll worth writing. A caster
    /// that has collected a spell it can never cast has been given a defect, not a spell.
    /// </para>
    ///
    /// <para>
    /// Running out is the point. A shortage that becomes another bot's wage is an economy; a shortage that
    /// is quietly refilled is scenery. Today the refill is a shopkeeper — herbs are shop goods in this era
    /// and no skill in the engine picks them — so what the market can actually be short of is the thing
    /// only a bot can make: see <c>BotSpells/</c>.
    /// </para>
    /// </summary>
    private static void GiveSupplies(Mobile bot, BotClass klass)
    {
        var pack = bot.Backpack;
        var kit = klass.Kit;

        if (Purse > 0)
        {
            pack.DropItem(new Gold(Purse));
        }

        if (kit.Bandages > 0)
        {
            pack.DropItem(new Bandage(kit.Bandages));
        }

        if (kit.Reagents > 0)
        {
            var each = kit.Reagents;

            pack.DropItem(new SulfurousAsh(each));
            pack.DropItem(new BlackPearl(each));
            pack.DropItem(new Garlic(each));
            pack.DropItem(new Ginseng(each));
            pack.DropItem(new SpidersSilk(each));
            pack.DropItem(new Nightshade(each));
            pack.DropItem(new Bloodmoss(each));
            pack.DropItem(new MandrakeRoot(each));
        }

        // The glass and the paper only. The mortar and the pen themselves are tools and go out with the rest
        // of them — see ToolsFor, which is also what buys another when one wears through.
        if (Wants(klass, SkillName.Alchemy))
        {
            pack.DropItem(new Bottle(StartingBottles));
        }

        if (Wants(klass, SkillName.Inscribe))
        {
            pack.DropItem(new BlankScroll(StartingScrolls));
        }

        // The bottles. Not bound and not meant to last: a potion is the only mending in the game that works
        // while something is hitting a bot, which is exactly why it is the thing a bot runs out of.
        var bottles = PotionsFor(klass);

        for (var i = 0; i < bottles.Count; i++)
        {
            var (kind, count) = bottles[i];

            for (var made = 0; made < count; made++)
            {
                var bottle = kind.CreateInstance<Item>();

                if (bottle != null)
                {
                    pack.DropItem(bottle);
                }
            }
        }
    }

    /// <summary>
    /// The bottles this class carries, family by family, with how many of each.
    ///
    /// <para>
    /// <b>One list asked by two callers</b>, for the same reason the tools are: birth hands these out and
    /// <see cref="BotShopper"/> reads the same list to notice one has been drunk and buy another. The limits
    /// are the class's own (<c>PotionLimit</c>) and default to one, so a class says nothing unless it means to —
    /// the brawler asks for two heals because empty hands are all it has.
    /// </para>
    ///
    /// <para>
    /// Only the families that mend. See <see cref="BotArsenal.Draughts"/>: the other six are buffs and weapons
    /// declared as limits with nothing to use them, and shopping for a bottle nobody drinks is an errand that
    /// produces nothing.
    /// </para>
    /// </summary>
    public static List<(Type Kind, int Count)> PotionsFor(BotClass klass)
    {
        List<(Type, int)> bottles = [];

        if (klass == null)
        {
            return bottles;
        }

        var families = BotArsenal.Draughts;

        for (var i = 0; i < families.Count; i++)
        {
            var type = BotArsenal.Potion(families[i]);
            var count = klass.PotionLimit(families[i]);

            if (type != null && count > 0)
            {
                bottles.Add((type, count));
            }
        }

        return bottles;
    }

    /// <summary>Whether this class works towards a skill at all.</summary>
    /// <summary>
    /// Whether this class brews, and therefore whether an empty bottle is stock or rubbish to it.
    ///
    /// Asked of the same skill list the mortar is handed out from, so a class that takes up alchemy later
    /// starts keeping its glass without anybody remembering to come back here.
    /// </summary>
    public static bool Brews(BotClass klass) => klass != null && Wants(klass, SkillName.Alchemy);

    private static bool Wants(BotClass klass, SkillName skill)
    {
        var skills = klass.Skills;

        for (var i = 0; i < skills.Count; i++)
        {
            if (skills[i].Skill == skill)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gives one thing and binds it. Worn if it can be and the caller wants it worn, in the pack
    /// otherwise — and deleted rather than dropped on the floor if the bot has nowhere to put it, since
    /// a bot with no pack is a bot that is about to be thrown away anyway.
    /// </summary>
    /// <summary>
    /// Armour a class is born in. Worn rather than packed, and bound like everything else issued at birth.
    ///
    /// Bound matters here more than usual: unbound it would read as merchandise the first time the bot went
    /// to a counter with a full pack, and a brawler that sold its own gloves would be selling its trade.
    /// </summary>
    private static void GiveArmour(Mobile bot, BotKit kit, BotBond bond)
    {
        var armour = kit.Armour;

        for (var i = 0; i < armour.Count; i++)
        {
            Hand(bot, BotBinding.Make(armour[i], 1), bond, pack: false);
        }
    }

    private static void Hand(Mobile bot, Item item, BotBond bond, bool pack)
    {
        if (item == null)
        {
            return;
        }

        // Bound before it is placed. Weight matters to whether the engine will let a bot carry it, and a
        // thing that is weighed on the way in can tip a pack over the line before it is ever weightless.
        BotBinding.Bind(item, bond);
        Bound++;

        if (!pack && bot.EquipItem(item))
        {
            return;
        }

        var backpack = bot.Backpack;

        if (backpack != null)
        {
            backpack.DropItem(item);
            return;
        }

        item.Delete();
    }

    /// <summary>
    /// Back to zero. Called when the population is rebuilt, which happens on every world load.
    ///
    /// Without it these are lifetime-of-process totals wearing the name of a population count, and a
    /// reload would report twice as many bots outfitted as exist. The first version kept its coin
    /// counters honest the same way; the ones it forgot were the ones that lied.
    /// </summary>
    public static void Reset()
    {
        Outfitted = 0;
        Bound = 0;
        Refitted = 0;
        Overborn = 0;
    }

    /// <summary>What the population was handed, for the minute's summary.</summary>
    public static string Describe() =>
        $"{Outfitted} bots outfitted, {Bound} things bound to their owners, {Refitted} born to a lighter weapon than the roll for want of strength, {Overborn} given the strength for the lightest";

    /// <summary>Bots whose birth roll asked more strength than they had, given the first option of the kit they could wield.</summary>
    public static long Refitted { get; private set; }

    /// <summary>Bots no option of the kit fitted, given the lightest and the strength for it.</summary>
    public static long Overborn { get; private set; }
}
