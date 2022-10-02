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

    // Poison
    public static PoisonGasCounter PoisonGasCounter => new();
    public static PoisonGasAreaAttack PoisonGasAreaAttack => new();

    // Explosions
    public static DeathExplosion DeathExplosion => new();

    public static ThrowHatchetCounter ThrowHatchetCounter => new();

    public static DestroyEquipment DestroyEquipment => new();

    public static DrainLifeAreaAttack DrainLifeAreaAttack => new();
}
