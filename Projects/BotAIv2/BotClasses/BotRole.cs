namespace Server.BotAI.V2;

/// <summary>
/// What a class contributes to a group, as opposed to what it is called.
///
/// Exists because the interesting questions about a population are never about class names. The city
/// hunt in the first version wanted "two medics, three casters and five bows" and had no way to ask
/// for them: it had to name every archetype that might satisfy each slot, so adding a class silently
/// excluded it from every muster in the shard. A tag answers the question once.
///
/// One tag per class, chosen by what the class fills rather than by everything it can do — a
/// warrior-mage holds the melee line and happens to cast, so it is <see cref="Melee"/> and reports
/// its casting through <see cref="BotClass.Casts"/> instead.
/// </summary>
public enum BotRole
{
    /// <summary>Closes and holds. Warrior, warrior-mage, brawler.</summary>
    Melee,

    /// <summary>Fights at distance and needs ammunition. Archer, warrior-archer.</summary>
    Ranged,

    /// <summary>Damage and utility out of a spellbook. Mage.</summary>
    Caster,

    /// <summary>Keeps everybody else standing. Healer.</summary>
    Medic,

    /// <summary>Makes and gathers rather than fights. Crafter, gatherer.</summary>
    Producer
}
