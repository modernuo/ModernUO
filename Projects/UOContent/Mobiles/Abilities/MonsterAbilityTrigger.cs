using System;

namespace Server.Mobiles;

[Flags]
public enum MonsterAbilityTrigger : ulong
{
    None                = 0x0000000000000000, // Passive, or some custom event.
    Think               = 0x0000000000000001,
    TakeMeleeDamage     = 0x0000000000000002,
    GiveMeleeDamage     = 0x0000000000000004, // Includes firing a bow
    TakeSpellDamage     = 0x0000000000000008,
    GiveSpellDamage     = 0x0000000000000010,
    CombatAction        = 0x0000000000000020,
    Death               = 0x0000000000000040,
    Movement            = 0x0000000000000080,

    GiveDamage = GiveMeleeDamage | GiveSpellDamage,
    TakeDamage = TakeMeleeDamage | TakeSpellDamage
}
