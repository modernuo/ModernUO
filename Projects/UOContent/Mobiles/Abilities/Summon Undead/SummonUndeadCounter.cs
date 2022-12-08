using System;

namespace Server.Mobiles;

public abstract class SummonUndeadCounter : MonsterAbility
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.SummonCounter;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage;

    public override double ChanceToTrigger => 0.05;
    public override TimeSpan MinTriggerCooldown => TimeSpan.FromMinutes(5);
    public override TimeSpan MaxTriggerCooldown => TimeSpan.FromMinutes(5);

    public virtual double ChanceToPolymorph => 0.25;
    public virtual TimeSpan PolymorphDuration => TimeSpan.FromMinutes(5);
    public virtual int GetTransformBodyValue() => Utility.RandomBool() ? 50 : 56;
    public virtual int AmountToSummon => 4;
    public virtual int SummonRange => 4;

    public abstract BaseCreature CreateSummon(BaseCreature source);

    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger) =>
        source.Followers < AmountToSummon && base.CanTrigger(source, trigger);

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        var amount = AmountToSummon - source.Followers;
        var distance = SummonRange;

        int willTransform = !source.Paralyzed && ChanceToPolymorph > 0 && ChanceToPolymorph > Utility.RandomDouble()
            ? Utility.Random(amount)
            : -1;

        for (var i = 0; i < amount; i++)
        {
            var loc = Utility.GetValidLocationInLOS(source.Map, source, distance);
            BaseCreature summon;
            if (i == willTransform)
            {
                summon = source;
                source.BodyMod = GetTransformBodyValue();
                source.HueMod = 0;
                // Name is not changed
                Timer.DelayCall(PolymorphDuration, EndPolymorph, source);
            }
            else
            {
                summon = CreateSummon(source);
                summon.Team = source.Team;
                summon.Summoned = true;
                summon.SummonMaster = source;
                summon.FightMode = FightMode.Closest;
                summon.Combatant = target;
            }

            summon.MoveToWorld(loc, source.Map);
            Effects.SendLocationEffect(summon.Location, summon.Map, 0x3728, 10);
            summon.PlaySound(0x48F);
            summon.PlaySound(summon.GetAttackSound());
        }

        base.Trigger(trigger, source, target);
    }

    private static void EndPolymorph(BaseCreature source)
    {
        source.BodyMod = 0;
        source.HueMod = -1;
    }
}
