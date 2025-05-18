using System;

namespace Server.Mobiles;

public class FanningFire : MonsterAbilitySingleTargetDoT
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.FanningFire;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.CombatAction;

    public FanningFire(double chanceToTrigger, int fireResistMod, int minDamage, int maxDamage)
    {
        ChanceToTrigger = chanceToTrigger;
        FireResistMod = fireResistMod;
        MinDamage = minDamage;
        MaxDamage = maxDamage;
    }

    public sealed override double ChanceToTrigger { get; }

    public int FireResistMod { get; }

    public int MinDamage { get; }

    public int MaxDamage { get; }

    public const string Name = "FanningFire";

    protected override void OnBeforeTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        // Renew the effects
        RemoveEffect(source, defender);

        // The creature fans you with fire, reducing your resistance to fire attacks.
        defender.SendLocalizedMessage(1070833);
    }

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        base.OnTarget(trigger, source, defender);

        /**
         * Fanning Fire
         * Start cliloc: 1070833
         * Effect: Fire res -10% for 10 seconds. Does not stack.
         * Damage: 35-45, 100% fire
         * End cliloc: 1070834
         * Sound: 0x208
         * Graphic:
         * - Type: "3"
         * - From: "0x57D4F5B"
         * - To: "0x0"
         * - ItemId: "0x3709"
         * - ItemIdName: "fire column"
         * - FromLocation: "(994 325, 16)"
         * - ToLocation: "(994 325, 16)"
         * - Speed: "10"
         * - Duration: "30"
         * - FixedDirection: "True"
         * - Explode: "False"
         * - Hue: "0x0"
         * - RenderMode: "0x0"
         * - Effect: "0x34"
         * - ExplodeEffect: "0x1"
         * - ExplodeSound: "0x0"
         * - Layer: "5"
         * - Unknown: "0x0"
         */

        source.DoHarmful(defender);
        defender.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, Name, FireResistMod));

        defender.FixedParticles(0x3709, 10, 30, 0x34, EffectLayer.RightFoot);
        defender.PlaySound(0x208);

        AOS.Damage(defender, source, Utility.RandomMinMax(MinDamage, MaxDamage), 0, 100, 0, 0, 0);
    }

    protected override void EffectTick(BaseCreature source, Mobile defender, ref TimeSpan nextDelay)
    {
    }

    protected override void EndEffect(BaseCreature source, Mobile defender)
    {
        defender.RemoveResistanceMod(Name);
    }

    protected override void OnEffectExpired(BaseCreature source, Mobile defender)
    {
        // Your resistance to fire attacks has returned.
        defender.SendLocalizedMessage(1070834);
    }
}
