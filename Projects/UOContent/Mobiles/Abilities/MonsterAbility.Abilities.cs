namespace Server.Mobiles;

public abstract partial class MonsterAbility
{
    public static MonsterAbility FireBreath { get; } = new FireBreath();
    public static MonsterAbility ChaosBreath { get; } = new ChaosBreath();
    public static MonsterAbility ColdBreath { get; } = new ColdBreath();
}
