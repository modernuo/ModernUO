namespace Server.Mobiles;

public abstract partial class MonsterAbility
{
    public static FireBreath FireBreath { get; } = new();
    public static ChaosBreath ChaosBreath { get; } = new();
    public static ColdBreath ColdBreath { get; } = new();

    public static GraspingClaw GraspingClaw { get; } = new();
}
