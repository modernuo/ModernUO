using System;

namespace Server.Mobiles;

[Flags]
public enum MonsterAbilityTrigger
{
    None = 0x00000000,   // Passive, or some custom event.
    Action = 0x00000001, // On Think
    TakeMeleeDamage = 0x00000002,
    GiveMeleeDamage = 0x00000004, // Includes firing a bow
    TakeSpellDamage = 0x00000008,
    GiveSpellDamage = 0x00000010,
    OnCombatAction = 0x000000020,

    TakeDamage = TakeMeleeDamage | TakeSpellDamage
}
