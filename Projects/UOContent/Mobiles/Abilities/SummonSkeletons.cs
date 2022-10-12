namespace Server.Mobiles;

public class SummonSkeletons : SummonUndead
{
    public override BaseCreature CreateSummon(BaseCreature source) => new Skeleton();
}
