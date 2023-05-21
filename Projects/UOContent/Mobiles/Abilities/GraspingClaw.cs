using System;

namespace Server.Mobiles;

public class GraspingClaw : MonsterAbilitySingleTargetDoT
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.GraspingClaw;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.GiveMeleeDamage;
    public override double ChanceToTrigger => 0.10;

    private const string Name = "GraspingClaw";

    protected override void OnBeforeTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        if (RemoveEffect(source, defender))
        {
            defender.SendLocalizedMessage(1070837); // The creature lands another blow in your weakened state.
        }
        else
        {
            // The blow from the creature's claws has made you more susceptible to physical attacks.
            defender.SendLocalizedMessage(1070836);
        }
    }

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        base.OnTarget(trigger, source, defender);

        /**
         * Grasping Claw
         * Start cliloc: 1070836
         * Effect: Physical resistance -15% for 5 seconds
         * Refresh Cliloc: 1070837
         * End cliloc: 1070838
         * Graphic:
         * - Type: "3"
         * - From: Player
         * - To: 0x0
         * - ItemId: "0x37B9"
         * - ItemIdName: "glow"
         * - FromLocation: "(1149 808, 32)"
         * - ToLocation: "(1149 808, 32)"
         * - Speed: "10" - Duration: "5"
         * - FixedDirection: "True"
         * - Explode: "False"
         */

        source.DoHarmful(defender);
        var mod = new ResistanceMod(ResistanceType.Physical, Name, -(defender.PhysicalResistance * 15 / 100));
        defender.AddResistanceMod(mod);

        defender.FixedEffect(0x37B9, 10, 5);
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
        defender.SendLocalizedMessage(1070838); // Your resistance to physical attacks has returned.
    }
}
