namespace Server.Mobiles;

public abstract partial class MonsterAbility
{
    // Fire Breath
    public static FireBreath FireBreath => new();
    public static ChaosBreath ChaosBreath => new();
    public static ColdBreath ColdBreath => new();

    public static GraspingClaw GraspingClaw => new();

    // Summon Undead
    public static SummonSkeletons SummonSkeletons => new();
    public static SummonLesserUndead SummonLesserUndead => new();

    // Stun
    public static ColossalBlow ColossalBlow => new();

    public static PoisonGasAreaAttack PoisonGasAreaAttack => new();

    public static ThrowHatchetAttack ThrowHatchetAttack => new();
}
