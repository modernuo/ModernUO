using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The weapons of the era, named once, each beside the skill that swings it.
///
/// Exists so that "which blades may a bot be born with" has exactly one answer. The classes below
/// differ in how far they train a weapon, not in which weapons exist, and a pool repeated in nine
/// files is nine places to forget when a weapon family turns out to be weak.
///
/// Restricted to what the population has actually been seen carrying rather than to everything the
/// engine defines: a list of every weapon in the game would be a list mostly of items no bot has ever
/// held, and the differences between a katana and a broadsword are small mechanically and considerable
/// to watch, which is the entire reason for offering a choice.
/// </summary>
public static class BotArsenal
{
    /// <summary>Spellbook contents, by the engine's circle index. Named because 3 and 10 are not readable.</summary>
    public const int SpellHeal = 3;

    /// <summary>The first attack any caster has, and the only one a new one can afford to miss with.</summary>
    public const int SpellMagicArrow = 4;

    /// <summary>Weakens what it hits. A caster's contribution to a fight it cannot win alone.</summary>
    public const int SpellWeaken = 7;

    /// <summary>Ends a poison. The healer's, because bandages lose that race.</summary>
    public const int SpellCure = 10;

    /// <summary>
    /// The fourth circle's heal, and the one worth having.
    ///
    /// Nobody is born with it: no shopkeeper in the era sells the fourth circle, so a healer that wants it has
    /// to be written one by a scribe or buy it off another bot. That is deliberate — it is the first thing a
    /// caster's own book makes it want.
    /// </summary>
    public const int SpellGreaterHeal = 28;

    /// <summary>Harm: the second circle's direct damage.</summary>
    public const int SpellHarm = 11;

    /// <summary>Protection: a cast that a blow does not disturb, which is the medic's answer to mending under fire.</summary>
    public const int SpellProtection = 14;

    /// <summary>Fireball: the third circle's direct damage.</summary>
    public const int SpellFireball = 17;

    /// <summary>Arch cure: ends the poison of everybody around the caster.</summary>
    public const int SpellArchCure = 24;

    /// <summary>Arch protection: protection on everybody around the caster.</summary>
    public const int SpellArchProtection = 25;

    /// <summary>Lightning: the fourth circle's direct damage.</summary>
    public const int SpellLightning = 29;

    /// <summary>Energy bolt: the sixth circle's direct damage.</summary>
    public const int SpellEnergyBolt = 41;

    /// <summary>Resurrection: the eighth circle, and the last thing a medic's book is short of.</summary>
    public const int SpellResurrection = 58;

    /// <summary>A working quiver, not a lifetime's supply — and the count that death honours.</summary>
    public const int StartingAmmunition = 150;

    /// <summary>
    /// The bottle a family of potion actually is, named once here like everything else of the era.
    ///
    /// <para>
    /// <b>The lesser tier, and that is read off the shelves rather than chosen.</b> Every shopkeeper in this
    /// era stocks exactly <c>LesserHealPotion</c> and <c>LesserCurePotion</c> at fifteen gold — the regular and
    /// greater tiers are on no shelf anywhere, which makes them an alchemist's product and nobody else's. So a
    /// bot buys the weak one and will one day buy a better one from another bot, which is the same shape as
    /// scrolls above the third circle.
    /// </para>
    ///
    /// <para>
    /// Only the two that mend. The other six families in <see cref="BotPotionKind"/> are buffs and weapons: they
    /// are declared as carrying limits and nothing hands them out, because nothing yet has a use for them, and
    /// a bot shopping for six kinds of bottle nobody drinks is six errands that produce nothing.
    /// </para>
    /// </summary>
    public static Type Potion(BotPotionKind kind) =>
        kind switch
        {
            BotPotionKind.Heal => typeof(LesserHealPotion),
            BotPotionKind.Cure => typeof(LesserCurePotion),
            _ => null
        };

    /// <summary>The families a bot is actually given and actually drinks. See <see cref="Potion"/>.</summary>
    public static IReadOnlyList<BotPotionKind> Draughts { get; } =
    [
        BotPotionKind.Heal,
        BotPotionKind.Cure
    ];

    /// <summary>
    /// The melee families, all trained to the same target.
    ///
    /// Three swords, one mace and two fencing weapons: weighted towards swords because that is what
    /// the era's warrior is, and spread across three skills so that a population of warriors does not
    /// read as one man copied fifty times.
    /// </summary>
    public static IReadOnlyList<BotWeaponOption> Melee(double target) =>
    [
        new(typeof(Katana), SkillName.Swords, target),
        new(typeof(Broadsword), SkillName.Swords, target),
        new(typeof(VikingSword), SkillName.Swords, target),
        new(typeof(WarMace), SkillName.Macing, target),
        new(typeof(WarFork), SkillName.Fencing, target),
        new(typeof(Kryss), SkillName.Fencing, target)
    ];

    /// <summary>
    /// The same, plus the quarterstaff.
    ///
    /// For the warrior-mage alone, and it is the "or a staff" of its specification. A quarterstaff is
    /// a Macing weapon and an ordinary one — not the caster's staff, which pays mana back and would
    /// give this class a second helping of a talent it already has intrinsically.
    /// </summary>
    public static IReadOnlyList<BotWeaponOption> MeleeWithStaff(double target) =>
    [
        new(typeof(Katana), SkillName.Swords, target),
        new(typeof(Broadsword), SkillName.Swords, target),
        new(typeof(VikingSword), SkillName.Swords, target),
        new(typeof(WarMace), SkillName.Macing, target),
        new(typeof(WarFork), SkillName.Fencing, target),
        new(typeof(QuarterStaff), SkillName.Macing, target)
    ];

    /// <summary>A bow and its arrows. The archer's whole armoury.</summary>
    public static IReadOnlyList<BotWeaponOption> Bow(double target) =>
    [
        new(typeof(Bow), SkillName.Archery, target, typeof(Arrow), StartingAmmunition)
    ];

    /// <summary>
    /// Bow or crossbow, each with the right ammunition. Both are Archery, so the roll changes what the
    /// bot looks like and what it consumes, not what it trains.
    /// </summary>
    public static IReadOnlyList<BotWeaponOption> BowOrCrossbow(double target) =>
    [
        new(typeof(Bow), SkillName.Archery, target, typeof(Arrow), StartingAmmunition),
        new(typeof(Crossbow), SkillName.Archery, target, typeof(Bolt), StartingAmmunition)
    ];

    /// <summary>
    /// The knife an archer keeps for whatever gets close enough that shooting stops working. Trained
    /// far lower than the bow: it is an admission that the fight went wrong, not a second trade.
    /// </summary>
    public static BotWeaponOption Sidearm(double target) => new(typeof(Dagger), SkillName.Fencing, target);
}
