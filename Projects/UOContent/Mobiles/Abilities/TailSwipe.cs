using System;

namespace Server.Mobiles;

public class TailSwipe : MonsterAbilitySingleTarget
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.TailSwipe;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.GiveMeleeDamage;
    public override double ChanceToTrigger => 0.3;

    // ServUO's SpecialAbility base cooldown; TailSwipe does not override it.
    public override TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(30);
    public override TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(30);

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        source.DoHarmful(defender);

        defender.PlaySound(0x204);
        defender.FixedEffect(0x376A, 6, 1);

        // OSI/Stratics reports roughly a 90% stun / 10% confuse split (ServUO's even 50/50 is incorrect).
        if (Utility.RandomDouble() < 0.10)
        {
            defender.SendLocalizedMessage(1112555); // You're left confused as the creature's tail catches you right in the face!
            defender.AddStatMod(new StatMod(StatType.Dex, "[TailSwipe] Dex", -20, TimeSpan.FromSeconds(5.0)));
            defender.AddStatMod(new StatMod(StatType.Int, "[TailSwipe] Int", -20, TimeSpan.FromSeconds(5.0)));
        }
        else
        {
            defender.SendLocalizedMessage(1112554); // You're stunned as the creature's tail knocks the wind out of you.
            defender.Paralyze(TimeSpan.FromSeconds(3.0));
        }
    }
}
