using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What "bound" means, in one place.
///
/// Bound is two promises and a ceiling: the thing weighs nothing, death does not take it, and nobody
/// may sell it. Each of the three answers a failure the first version measured, and none of them is
/// decoration.
///
/// <para>
/// <b>Weightless.</b> Past <c>40 + 3.5 × Str</c> stones the engine charges five stamina and more for
/// every single step, and refuses the step outright once stamina reaches zero — so an overloaded bot
/// drains itself flat in a dozen paces and then stands there for the rest of the shard's life. Three
/// bots spent an entire session exactly that way, and the log insisted six hundred times over that the
/// ground was clear and the engine approved of the step. It did. Stamina was not in the message. For a
/// gatherer, whose whole job is to fill a pack, tools that weigh are ore that cannot be carried.
/// </para>
///
/// <para>
/// <b>Kept through death.</b> A bot's working tools are what let it start again after being killed. The
/// first version's smith who lost its hammer was not a smith any more — it could not forge, could not
/// take commissions, and quietly spent the rest of its life hitting skeletons like everybody else. An
/// entire mechanism existed there to soften this: spare tools kept in a bank box, and a trip across
/// Britain to fetch one. Binding retires that mechanism rather than improving it.
/// </para>
///
/// <para>
/// <b>Not merchandise.</b> A bound item is never sold, auctioned, scrapped or posted. Without this the
/// weightlessness becomes an exploit — a bot would carry a free anvil to market — and worse, the
/// scrapper would eat the hammer, since "destroy it" was the first version's final answer for anything
/// nobody would buy.
/// </para>
///
/// <para>
/// <b>The engine does two thirds of this and the ledger does the rest.</b> <c>LootType.Newbied</c> is
/// how this era keeps a thing with its owner through death, and it works on whole objects. It cannot
/// express a partial stack, which is why ammunition is counted instead — see
/// <see cref="TrimAmmunition"/>.
/// </para>
/// </summary>
public static class BotBinding
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBinding));

    private static bool _warnedAboutEra;

    /// <summary>
    /// Binds one indivisible thing: weightless, kept through death, recorded so nothing sells it.
    /// </summary>
    public static void Bind(Item item, BotBond bond)
    {
        if (item == null || bond == null)
        {
            return;
        }

        // Newbied is how the pre-AOS engine keeps a thing with its owner. On an AOS-era shard the flag
        // is inert — insurance replaced it — and the ledger below would then be the only thing standing
        // between a bot and losing its trade to one bad fight. Said once, loudly, rather than
        // discovered from a population of smiths with no hammers.
        if (Core.AOS)
        {
            if (!_warnedAboutEra)
            {
                _warnedAboutEra = true;

                logger.Warning(
                    "This shard runs AOS rules, where LootType.Newbied does nothing; bound gear survives death only by being handed back on resurrection"
                );
            }
        }
        else if (item.LootType == LootType.Regular)
        {
            item.LootType = LootType.Newbied;
        }

        Weightless(item);
        Dye(item);

        bond.Items.Add(item.Serial);

        var type = item.GetType();

        if (!bond.Issued.Contains(type))
        {
            bond.Issued.Add(type);
        }
    }

    /// <summary>
    /// Binds a stack by <em>count</em>: this much of it, and no more, is the bot's own.
    ///
    /// The flag goes on as well, so the ordinary case costs the ledger nothing at death — but the count
    /// is what is authoritative, because the flag on a merged stack is a coin toss.
    /// </summary>
    public static void BindStack(Item stack, int granted, BotBond bond)
    {
        if (stack == null || bond == null || granted <= 0)
        {
            return;
        }

        Mark(stack);

        bond.Ammunition[stack.GetType()] = granted;
    }

    /// <summary>
    /// The flag and the weight, without touching the ledger.
    ///
    /// Split out for one narrow reason: <see cref="TrimAmmunition"/> re-marks a stack while walking the
    /// ledger, and a helper that also wrote to it would be mutating a dictionary mid-enumeration.
    /// </summary>
    private static void Mark(Item stack)
    {
        if (!Core.AOS && stack.LootType == LootType.Regular)
        {
            stack.LootType = LootType.Newbied;
        }

        Weightless(stack);
        Dye(stack);
    }

    /// <summary>
    /// The hue of a weapon bound to a bot for life. Patrick's order of 13.09.2026: blue, so that the blade
    /// a bot will never lose can be told from one it picked up, at a glance, in the client. Weapons only -
    /// the first cut dyed the whole bound kit and put the population in blue shirts, which was not the order.
    /// </summary>
    public static int BoundHue { get; set; } = 1152;

    private static void Dye(Item item)
    {
        if (item is BaseWeapon && BoundHue > 0)
        {
            item.Hue = BoundHue;
        }
    }

    /// <summary>
    /// Whether this exact item is one the bot was given. Asked by anything that would part it from it.
    /// </summary>
    public static bool IsBound(Item item, BotBond bond) =>
        item != null && bond != null && bond.Items.Contains(item.Serial);

    /// <summary>
    /// Bound things a door turned away: a stall or a want. It should read nought, because every chooser that
    /// hands goods to one passes over what is bound; a number above it names a chooser that does not.
    /// </summary>
    public static long Refused { get; private set; }

    /// <summary>Bound things taken off bots, and entitlements struck off with them. See <see cref="Forfeit"/>.</summary>
    public static long Forfeited { get; private set; }

    /// <summary>
    /// Whether this is bound and so may not go through the door asking — counted when it is. For the doors
    /// themselves (the auction's List and Fill, until 01.10.2026; a shop's till and a carrier's meeting read
    /// <c>BotShopkeep.Merchandise</c> now); a chooser filters with <see cref="IsBound"/> and has nothing to report.
    ///
    /// <para>
    /// <b>Added with <c>BotMobile.Rewield</c> on 15.09.2026.</b> Until then a bot's bound weapon was in its hand
    /// and only a sidearm or a tool rode in a pack, so a scan of the pack missed the weapon by construction. Once
    /// a better weapon is wielded the bound one rides in the pack — on purpose, so that a bot stripped in a war
    /// has it — and a peddle a shopkeeper turned down listed everything of that kind it was carrying.
    /// </para>
    /// </summary>
    public static bool Refuses(Item item, BotBond bond)
    {
        if (!IsBound(item, bond))
        {
            return false;
        }

        Refused++;

        return true;
    }

    /// <summary>
    /// Whether this type of ammunition is bound to the bot at all, and up to what count.
    /// </summary>
    public static int BoundCount(Type type, BotBond bond) =>
        type != null && bond != null && bond.Ammunition.TryGetValue(type, out var granted) ? granted : 0;

    /// <summary>
    /// The death rule for ammunition: the bot keeps <c>min(carried, granted)</c> and the corpse gets
    /// the rest.
    ///
    /// <para>
    /// Called from the bot's own death hook, where the corpse already exists and already holds whatever
    /// the engine decided to take — so both halves of "how much did it have" are readable, and the
    /// arithmetic does not depend on which way a stack merge happened to set the loot flag. That
    /// independence is the point: it is the one thing about stacks that cannot be relied upon.
    /// </para>
    ///
    /// <para>
    /// An archer born with a hundred and fifty arrows who has spent all but one rises with one, because
    /// bound is a ceiling and not a refill. One who bought two hundred more rises with a hundred and
    /// fifty and leaves the rest on the ground for whoever walks past, because a quiver is something an
    /// archer is supposed to have to think about.
    /// </para>
    /// </summary>
    public static void TrimAmmunition(Mobile bot, BotBond bond, Container corpse)
    {
        var pack = bot?.Backpack;

        if (pack == null || bond == null || bond.Ammunition.Count == 0)
        {
            return;
        }

        foreach (var (type, granted) in bond.Ammunition)
        {
            var inPack = TakeAll(pack, type);
            var inCorpse = corpse is { Deleted: false } ? TakeAll(corpse, type) : 0;
            var carried = inPack + inCorpse;

            if (carried <= 0)
            {
                continue;
            }

            var keep = Math.Min(carried, granted);
            var lost = carried - keep;

            if (keep > 0)
            {
                var kept = Make(type, keep);

                if (kept != null)
                {
                    pack.DropItem(kept);

                    // Marked, not re-registered: the granted count is already in the ledger and this
                    // loop is reading it.
                    Mark(kept);
                }
            }

            if (lost > 0 && corpse is { Deleted: false })
            {
                var dropped = Make(type, lost);

                if (dropped != null)
                {
                    corpse.DropItem(dropped);
                }
            }

            if (lost > 0)
            {
                logger.Information(
                    "{Name} died holding {Carried} {Ammo} and keeps {Kept}; {Lost} were not its own",
                    bot.Name,
                    carried,
                    type.Name,
                    keep,
                    lost
                );
            }
        }
    }

    /// <summary>
    /// Hands back anything bound that no longer exists on the bot. Run on resurrection.
    ///
    /// The check is "does the bot hold one of these", not "is that serial still alive", and the
    /// simplification is safe because of what bound means: a bound thing weighs nothing and cannot be
    /// sold, so there is no reason for a bot ever to put one down. If it is not on the bot, it is gone.
    ///
    /// Mostly a safety net — the engine keeps these through death by itself in this era — and the net
    /// is worth having because the alternative failure is invisible: a bot that quietly stopped being
    /// able to do its trade goes on hitting skeletons and looks like every other bot doing that.
    /// </summary>
    public static int Restore(Mobile bot, BotBond bond)
    {
        var pack = bot?.Backpack;

        if (pack == null || bond == null)
        {
            return 0;
        }

        var handed = 0;

        for (var i = 0; i < bond.Issued.Count; i++)
        {
            var type = bond.Issued[i];

            if (Holds(bot, type))
            {
                continue;
            }

            var replacement = Make(type, 1);

            if (replacement == null)
            {
                continue;
            }

            pack.DropItem(replacement);
            Bind(replacement, bond);
            handed++;

            logger.Information("{Name} rose without its {Thing} and was handed another", bot.Name, type.Name);
        }

        return handed;
    }

    /// <summary>Weapons handed back to bots a catch had left bare. See <see cref="Reissue(BotMobile)"/>.</summary>
    public static long Reissued { get; private set; }

    /// <summary>
    /// Hands a bot back the weapon it was born to when a catch took the blade together with the entitlement.
    ///
    /// <para>
    /// <b>Patrick, 12.10.2026: the caught keep their weapons, and the bandits caught before that get theirs back.</b>
    /// Until then <see cref="Forfeit"/> struck every bound weapon off. The roll survived it — <c>bond.Weapon</c> is the
    /// build, not property — so what is handed back is the very kind the bot was born to, the class's sidearm and the
    /// staff if its class carries them. A kind still owed is left alone, so asking twice hands nothing twice; a kind the
    /// bot holds anyway, bought back, is owed again without a second one handed. It goes into the pack bound;
    /// <c>BotMobile.Rearm</c> puts it in the hand. Bound tools the engine calls weapons cannot be handed back: nothing
    /// recorded which of them a bot had.
    /// </para>
    /// </summary>
    public static int Reissue(BotMobile bot)
    {
        if (bot is not { Deleted: false, Bond: { } bond, Backpack: { } pack })
        {
            return 0;
        }

        var handed = 0;

        if (bond.Weapon is { Weapon: { } kind })
        {
            handed += Reissue(bot, bond, pack, kind);
        }

        if (bot.Class?.Kit?.Sidearm is { Weapon: { } side })
        {
            handed += Reissue(bot, bond, pack, side);
        }

        if (bot.Class?.Kit is { Staff: true })
        {
            handed += Reissue(bot, bond, pack, typeof(BotCasterStaff));
        }

        return handed;
    }

    private static int Reissue(Mobile bot, BotBond bond, Container pack, Type kind)
    {
        if (bond.Issued.Contains(kind))
        {
            return 0;
        }

        // Bought back unbound: the one it holds is its own to lose, and the entitlement it was born with comes back.
        if (Holds(bot, kind))
        {
            bond.Issued.Add(kind);

            return 0;
        }

        var weapon = Make(kind, 1);

        if (weapon == null)
        {
            return 0;
        }

        // Bound before it is placed, as at birth: weighed on the way in it could tip a full pack over the line.
        Bind(weapon, bond);
        pack.DropItem(weapon);
        Reissued++;

        logger.Information("{Name} is handed back the {Thing} a catch took off it", bot.Name, kind.Name);

        return 1;
    }

    /// <summary>
    /// Takes the bond away: everything bound still on the bot, and the entitlement behind it.
    ///
    /// <para>
    /// <b>The entitlement is the part that matters.</b> Bound gear is not kept through death by the engine on this
    /// shard, it is kept by <see cref="Restore"/> handing back anything on <c>bond.Issued</c> that the bot no longer
    /// holds. A punishment that takes a thief's sword without striking the sword off that list hands it a new sword
    /// the moment it rises. See <c>BotOutlaw.Seize</c>, which is the only caller: Patrick's rule of 18.09.2026 that a
    /// catch leaves a bandit with nothing.
    /// </para>
    ///
    /// <para>
    /// <b>Except its weapons, with <paramref name="spared"/>.</b> Patrick, 12.10.2026: "everything is taken, but the
    /// weapon should be left", and then: "the bound weapon must not be taken." What the catch spared stays on the bot and
    /// stays bound, and every bound weapon kind stays owed, so a blade that is gone anyway is handed back on rising like
    /// before. A bound knife or pickaxe is a weapon to the engine and stays too.
    /// </para>
    /// </summary>
    public static int Forfeit(Mobile bot, BotBond bond, HashSet<Item> spared = null)
    {
        if (bond == null)
        {
            return 0;
        }

        var taken = 0;

        if (bot is { Deleted: false })
        {
            List<Item> gone = [];
            var worn = bot.Items;

            for (var i = 0; i < worn.Count; i++)
            {
                if (IsBound(worn[i], bond) && !(spared != null && worn[i] is BaseWeapon))
                {
                    gone.Add(worn[i]);
                }
            }

            if (bot.Backpack is { } pack)
            {
                for (var i = 0; i < pack.Items.Count; i++)
                {
                    if (IsBound(pack.Items[i], bond) && !(spared != null && pack.Items[i] is BaseWeapon))
                    {
                        gone.Add(pack.Items[i]);
                    }
                }
            }

            for (var i = 0; i < gone.Count; i++)
            {
                gone[i].Delete();
                taken++;
            }
        }

        if (spared != null)
        {
            var owed = bond.Issued.Count;
            HashSet<Serial> kept = [];

            foreach (var item in spared)
            {
                kept.Add(item.Serial);
            }

            bond.Items.RemoveWhere(serial => !kept.Contains(serial) && World.FindItem(serial) is not BaseWeapon { Deleted: false });
            bond.Issued.RemoveAll(type => !typeof(BaseWeapon).IsAssignableFrom(type));

            var struck = owed - bond.Issued.Count;

            Forfeited += taken + struck;

            return taken + struck;
        }

        var entitled = bond.Issued.Count;

        // <b>The entitlements go; the build stays.</b> Issued and Items are property — what this bot is owed for free
        // when it rises — and striking them off is the whole point. Weapon and Ammunition are not property: they are
        // the roll this body was made with, and they are what BotShopper reads to know that this bot fights with a
        // katana and carries fifty arrows. The first cut of this cleared all four, which would have left a bandit
        // stripped at the cell door unable to buy a weapon for the rest of the session, because the roll is made once,
        // at birth, in BotOutfit.Give. Buying one back binds it again and the entitlement comes with it, by Bind.
        bond.Items.Clear();
        bond.Issued.Clear();

        Forfeited += taken + entitled;

        return taken + entitled;
    }

    /// <summary>
    /// Zero weight, stated in one place so the single uncertain engine call in this folder is also in
    /// one place. If <c>Item.Weight</c> turns out not to be settable in this fork, this is the only
    /// line that has to change.
    /// </summary>
    private static void Weightless(Item item) => item.Weight = 0.0;

    /// <summary>Whether the bot has one of these on it — worn, wielded or in the pack.</summary>
    private static bool Holds(Mobile bot, Type type)
    {
        var worn = bot.Items;

        for (var i = 0; i < worn.Count; i++)
        {
            if (worn[i].GetType() == type)
            {
                return true;
            }
        }

        var pack = bot.Backpack;

        if (pack == null)
        {
            return false;
        }

        var carried = pack.Items;

        for (var i = 0; i < carried.Count; i++)
        {
            if (carried[i].GetType() == type)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes every item of this type from the container and returns how many there were in total.
    ///
    /// Counting and taking in one pass because the caller always wants both, and because a stack that
    /// is counted and then looked up again is a stack that can change between the two.
    /// </summary>
    private static int TakeAll(Container container, Type type)
    {
        var items = container.Items;
        var total = 0;

        // Backwards: removing from a live list.
        for (var i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];

            if (item.GetType() != type)
            {
                continue;
            }

            total += item.Amount;
            item.Delete();
        }

        return total;
    }

    /// <summary>
    /// One item of this type, of this amount, or null if the type cannot be built.
    ///
    /// Null rather than throwing: a kit naming a type the shard does not have is a configuration
    /// mistake, and it should cost that one item rather than the bot.
    /// </summary>
    internal static Item Make(Type type, int amount)
    {
        if (type == null)
        {
            return null;
        }

        // <b>The engine's activator, not the framework's, and this line was the whole of an archer's
        // problem.</b> <c>Activator.CreateInstance</c> looks for a genuinely parameterless constructor, and
        // ammunition has none: <c>Arrow(int amount = 1)</c> and <c>Bolt(int amount = 1)</c> declare an
        // optional parameter instead. So every archer ever born threw the same warning and went out with an
        // empty quiver — twice a boot, in every log, for a whole evening. The market's own splitter was fixed
        // for exactly this and the kit was not. <c>Type.CreateInstance&lt;T&gt;()</c> fills optional
        // parameters with <c>Type.Missing</c> and returns the object.
        Item item;

        try
        {
            item = type.CreateInstance<Item>();
        }
        catch (Exception e)
        {
            logger.Warning(e, "Could not make a {Thing} for a bot's kit", type.Name);
            return null;
        }

        if (item == null)
        {
            logger.Warning("A bot's kit names {Thing}, which is not an item", type.Name);
            return null;
        }

        if (amount > 1)
        {
            item.Amount = amount;
        }

        return item;
    }
}
