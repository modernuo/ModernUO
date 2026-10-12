using System;

namespace Server.BotAI.V2;

/// <summary>
/// One weapon a class may be born holding, together with the skill that makes it land, how far the
/// bot will train that skill, and whatever the weapon needs in order to work at all.
///
/// The weapon and its skill are one fact and are therefore one type. The first version learned this
/// the expensive way twice over: a profile trained Swordsmanship and then handed out a mace or a war
/// fork at random, so a third of every melee build spent its life swinging a weapon it had no skill
/// in — and the equipment scorer, which compared damage numbers, went on telling it that the weapon
/// was good. Damage says what a weapon does when it connects; the skill decides whether it connects.
///
/// So the roll happens once, at birth, and picks a <em>pairing</em>. A bot that rolled the war mace
/// trains Macing to the same target the swordsman trains Swordsmanship, and the two are equally
/// competent — different to watch, identical on paper.
/// </summary>
/// <param name="Weapon">The item type handed out. Bound to the bot: weightless and kept through death.</param>
/// <param name="Skill">The skill that governs hitting with it.</param>
/// <param name="Target">
/// How high the bot wants that skill. Deliberately part of the option rather than read from a shared
/// table: it is the number that has to move when a weapon family turns out to be weaker, and having it
/// here means that edit is one line in one place.
/// </param>
/// <param name="Ammunition">
/// What the weapon consumes, or null for anything that consumes nothing. Belongs to the weapon rather
/// than to the kit because it is decided by the same roll: a bot that drew the crossbow needs bolts,
/// and a kit that named arrows in advance would arm it with the wrong thing.
/// </param>
/// <param name="AmmunitionCount">
/// How much ammunition is granted — and, exactly, how much of it is bound.
///
/// The rule as specified: an archer born with a hundred arrows who has spent ninety-nine of them
/// rises with one. Bound is a ceiling on what death gives back, not a refill, so what returns is
/// <c>min(carried, granted)</c>. Arrows bought or picked up beyond the grant are ordinary property and
/// stay in the corpse like anything else — which is what keeps a quiver something an archer has to
/// think about.
///
/// It cannot be done with the engine's own flag, and that is why the count is here. Stacks merge: a
/// hundred bound arrows and fifty bought ones become one stack of a hundred and fifty carrying a
/// single loot flag, and whichever flag wins is wrong for half the stack. A remembered count has no
/// such problem.
/// </param>
public readonly record struct BotWeaponOption(
    Type Weapon,
    SkillName Skill,
    double Target,
    Type Ammunition = null,
    int AmmunitionCount = 0
);
