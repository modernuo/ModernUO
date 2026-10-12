// <b>All that is left of the King's Rangers, and it is left on purpose.</b>
//
// The company was cut from the shard on 02.09.2026 by instruction: they were five bots outside the
// population's own economy, raised apart, never revived, walking their own ground on their own clock, and
// they turned up in every measurement as five bots doing something nobody else did.
//
// Their gear could not go with them. A type whose name is in the world save cannot simply stop existing:
// GenericEntityPersistence reads the save's type table before it reads a single object, and a name it
// cannot resolve makes it ask the console "Delete all of those types? (y/n)" — a question a headless shard
// cannot answer, so it hangs at boot with no error. Measured that evening: nineteen ranger item types in
// Items.idx and ZERO objects of any of them. Nothing was being kept alive but the names.
//
// So the names stay and the company does not. Nothing constructs any of these any more; they exist so that
// the world still loads, and they cost the shard one line in a type table.

using ModernUO.Serialization;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The King's Rangers' kit: the Baron's livery on five more bodies.
///
/// <para>
/// <b>The same gold and the same red, by order, and it is the point rather than a saving.</b> These five are
/// the crown's, as the Baron is, and a person watching from a client should be able to tell that at a glance
/// from further away than a name will carry. Gold plate under a red cloak is what the crown looks like on
/// this shard; anything else would have made them read as another warband.
/// </para>
///
/// <para>
/// <b>Restored rather than indestructible, exactly as the Baron's is.</b> A piece with no durability at all
/// is a piece the engine deletes, so the blow lands, the engine takes its point, and the point is put back.
/// It also means <c>BotUpkeep</c> reads every piece at full life and never asks the population for a
/// replacement nobody could make.
/// </para>
///
/// <para>
/// <b>And the archer wears studded, which is not a downgrade.</b> A full plate suit on a shooting build is a
/// build that cannot draw: this era wants a free arm for the bow. Studded in the same livery is the armour a
/// bow can actually be used in, and the choice is the engine's rule rather than a preference.
/// </para>
/// </summary>
public static class BotRangerKit
{
    /// <summary>Everything the rangers wear takes the Baron's own red. One livery, one constant.</summary>
    public const int Livery = BotRegalia.RoyalRed;
}

/// <summary>The ranger's blade. Swordsmanship, and it never blunts.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerBlade : Broadsword
{
    [Constructible]
    public BotRangerBlade()
    {
        Name = "a King's Ranger blade";
        Hue = BotRangerKit.Livery;
    }

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1.0)
    {
        var hits = HitPoints;
        var max = MaxHitPoints;

        base.OnHit(attacker, defender, damageBonus);

        if (Deleted)
        {
            return;
        }

        // Maximum first: HitPoints refuses to move while the maximum is nought, so the other order would
        // silently do nothing. The Baron's halberd carries the same note and the same ordering.
        MaxHitPoints = max;
        HitPoints = hits;
    }
}

/// <summary>The ranger's bow. Archery, and it never blunts.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerBow : Bow
{
    [Constructible]
    public BotRangerBow()
    {
        Name = "a King's Ranger bow";
        Hue = BotRangerKit.Livery;
    }

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1.0)
    {
        var hits = HitPoints;
        var max = MaxHitPoints;

        base.OnHit(attacker, defender, damageBonus);

        if (Deleted)
        {
            return;
        }

        MaxHitPoints = max;
        HitPoints = hits;
    }
}

/// <summary>The ranger's shield. Parried with, and it never splits.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerShield : HeaterShield
{
    [Constructible]
    public BotRangerShield()
    {
        Name = "a King's Ranger shield";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The ranger's cuirass.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerChest : PlateChest
{
    [Constructible]
    public BotRangerChest()
    {
        Name = "a King's Ranger cuirass";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The ranger's greaves.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerLegs : PlateLegs
{
    [Constructible]
    public BotRangerLegs()
    {
        Name = "King's Ranger greaves";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The ranger's vambraces.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerArms : PlateArms
{
    [Constructible]
    public BotRangerArms()
    {
        Name = "King's Ranger vambraces";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The ranger's gauntlets.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerGloves : PlateGloves
{
    [Constructible]
    public BotRangerGloves()
    {
        Name = "King's Ranger gauntlets";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The ranger's gorget.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerGorget : PlateGorget
{
    [Constructible]
    public BotRangerGorget()
    {
        Name = "a King's Ranger gorget";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The ranger's helm.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerHelm : PlateHelm
{
    [Constructible]
    public BotRangerHelm()
    {
        Name = "a King's Ranger helm";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The archer's tunic. Studded, so the bow can be drawn: see the file note.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerArcherChest : StuddedChest
{
    [Constructible]
    public BotRangerArcherChest()
    {
        Name = "a King's Ranger tunic";
        Hue = BotRangerKit.Livery;
        MaxHitPoints = 0;
    }
}

/// <summary>The archer's leggings.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerArcherLegs : StuddedLegs
{
    [Constructible]
    public BotRangerArcherLegs()
    {
        Name = "King's Ranger leggings";
        Hue = BotRangerKit.Livery;
        MaxHitPoints = 0;
    }
}

/// <summary>The archer's sleeves.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerArcherArms : StuddedArms
{
    [Constructible]
    public BotRangerArcherArms()
    {
        Name = "King's Ranger sleeves";
        Hue = BotRangerKit.Livery;
        MaxHitPoints = 0;
    }
}

/// <summary>The archer's bracers.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerArcherGloves : StuddedGloves
{
    [Constructible]
    public BotRangerArcherGloves()
    {
        Name = "King's Ranger bracers";
        Hue = BotRangerKit.Livery;
        MaxHitPoints = 0;
    }
}

/// <summary>The archer's collar.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerArcherGorget : StuddedGorget
{
    [Constructible]
    public BotRangerArcherGorget()
    {
        Name = "a King's Ranger collar";
        Hue = BotRangerKit.Livery;
        MaxHitPoints = 0;
    }
}

/// <summary>The archer's cap. Leather, because a plate helm and a bowstring do not agree.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerArcherCap : LeatherCap
{
    [Constructible]
    public BotRangerArcherCap()
    {
        Name = "a King's Ranger cap";
        Hue = BotRangerKit.Livery;
        MaxHitPoints = 0;
    }
}

/// <summary>
/// The red royal cloak of the rangers. Clothing rather than armour: it stops nothing, and is worn for the
/// one reason the Baron's is — so that five bots in a field read as the crown's from across it.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotRangerCloak : Cloak
{
    [Constructible]
    public BotRangerCloak()
    {
        Name = "a red royal cloak";
        Hue = BotRangerKit.Livery;
        MaxHitPoints = 0;
    }
}

/// <summary>
/// The King's Rangers' own spellbook: the first five circles, whole, and bound to the bot.
///
/// <para>
/// <b>A book of its own rather than one assembled from a kit list.</b> The ordinary route builds a book by
/// setting one bit per spell named on the class, which is right for a bot that learns its trade over an
/// evening and wrong for a company issued its equipment by the crown. This one is complete the moment it is
/// made: every spell of circles one through five, so <c>BotStrike</c> can always walk its ladder down from
/// the strongest the mana will pay for, and nothing anywhere has to remember to add the next one.
/// </para>
///
/// <para>
/// The sixth circle and above are deliberately absent, and the class note says why: that is where one caster
/// starts making the other four rangers scenery.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotRangerBook : Spellbook
{
    /// <summary>How many circles the crown issues. Five, by order.</summary>
    public const int Circles = 5;

    [Constructible]
    public BotRangerBook() : base(Full())
    {
        Name = "a King's Ranger spellbook";
        Hue = BotRangerKit.Livery;
        LootType = LootType.Blessed;
    }

    /// <summary>
    /// Every spell in the first five circles, as the engine's own bitmask.
    ///
    /// Eight spells to a circle and the ids are laid out in circle order, so the first forty bits are exactly
    /// circles one through five. Built rather than written out: a literal would be a second place to be wrong
    /// the first time anybody changes what a circle holds.
    /// </summary>
    private static ulong Full()
    {
        var content = 0ul;

        for (var i = 0; i < Circles * 8; i++)
        {
            content |= 1ul << i;
        }

        return content;
    }
}

/// <summary>The ranger mage's robe. Dress rather than armour — see BotHarness on why it cannot be ordered.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerRobe : Robe
{
    [Constructible]
    public BotRangerRobe()
    {
        Name = "a King's Ranger robe";
        Hue = BotRangerKit.Livery;
    }
}

/// <summary>The ranger healer's robe. The same livery, named for the office so the two read apart up close.</summary>
[SerializationGenerator(0, false)]
public partial class BotRangerHealerRobe : Robe
{
    [Constructible]
    public BotRangerHealerRobe()
    {
        Name = "a King's Ranger surgeon's robe";
        Hue = BotRangerKit.Livery;
    }
}
