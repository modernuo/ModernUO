namespace Server.Mobiles;

public class SummonPixiesCounter : MonsterAbility
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.SummonCounter;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage;
    public override double ChanceToTrigger => 0.1;

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        var amount = Utility.RandomMinMax(3, 6);
        for (var i = 0; i < amount; i++)
        {
            var pixie = new Pixie { Team = source.Team, FightMode = FightMode.Closest };
            pixie.MoveToWorld(source.Map.GetRandomNearbyLocation(source.Location), source.Map);
            pixie.Combatant = target;
        }

        base.Trigger(trigger, source, target);
    }
}
