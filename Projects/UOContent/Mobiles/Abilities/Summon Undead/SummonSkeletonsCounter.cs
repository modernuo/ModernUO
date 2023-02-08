namespace Server.Mobiles;

public class SummonSkeletonsCounter : SummonUndeadCounter
{
    public override BaseCreature CreateSummon(BaseCreature source) => new Skeleton();
}
