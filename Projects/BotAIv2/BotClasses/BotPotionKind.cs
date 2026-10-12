namespace Server.BotAI.V2;

/// <summary>
/// Potions grouped by what they do, which is the granularity a carrying limit needs.
///
/// The engine's own <c>PotionEffect</c> distinguishes tiers — lesser, regular and greater heal are
/// three values — and a limit expressed in those terms would let a bot carry three heal potions and
/// call it one of each. A bot that is out of bottles is out of bottles regardless of which grade it
/// drank, so the limit counts families.
///
/// <see cref="Mana"/> has no engine effect behind it: mana potions do not exist in this era and are
/// this project's own item, riding on <c>PotionEffect.Refresh</c>. It is listed here because a bot
/// still has to be told how many of them it may hold, and because the warrior-mage's allowance of two
/// is the only exception to the flat limit of one that is not about healing.
/// </summary>
public enum BotPotionKind
{
    /// <summary>Closes wounds. The only healing available to a bot in contact with something.</summary>
    Heal,

    /// <summary>Ends a poison, which otherwise outlasts several rounds of bandaging.</summary>
    Cure,

    /// <summary>Stamina back, which is what a bot that cannot take another step is short of.</summary>
    Refresh,

    /// <summary>Mana back. This project's own item — see the remarks on the type.</summary>
    Mana,

    /// <summary>Strength for a while.</summary>
    Strength,

    /// <summary>Dexterity for a while.</summary>
    Agility,

    /// <summary>A weapon, not a supply: poison goes on a blade or down somebody else's throat.</summary>
    Poison,

    /// <summary>The other weapon. Thrown, and the only ranged option a non-caster can brew.</summary>
    Explosion
}
