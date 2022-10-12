namespace Server.Mobiles;

public class SummonLesserUndead : SummonUndead
{
    private int _group;

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        _group = Utility.Random(2);
        base.Trigger(trigger, source, target);
    }

    public override BaseCreature CreateSummon(BaseCreature source)
    {
        return _group switch
        {
            0 => Utility.Random(4) switch
            {
                0 => new Skeleton(),
                1 => new SkeletalMage(),
                2 => new Mummy(),
                3 => new Zombie(),
            },
            _ => Utility.Random(4) switch
            {
                0 => new Ghoul(),
                1 => new Spectre(),
                2 => new Shade(),
                3 => new Bogle()
            }
        };
    }
}
