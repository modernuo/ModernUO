namespace Server.Mobiles;

public class PoisonGasAreaAttack : AreaEffectMonsterAbility
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.MassPoison;

    public override void Trigger(BaseCreature source, Mobile target)
    {
        source.FixedParticles(0x376A, 9, 32, 0x2539, EffectLayer.LeftHand);
        source.PlaySound(0x1DE);

        base.Trigger(source, target);
    }

    protected override void DoEffectTarget(BaseCreature source, Mobile defender)
    {
        defender.ApplyPoison(source, Poison.Deadly);
    }
}
