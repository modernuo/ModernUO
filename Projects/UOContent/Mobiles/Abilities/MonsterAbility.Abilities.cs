﻿namespace Server.Mobiles;

public abstract partial class MonsterAbility
{
    // Fire Breath
    public static FireBreath FireBreath => new();
    public static ChaosBreath ChaosBreath => new();
    public static ColdBreath ColdBreath => new();

    // Resistance Debuffs
    public static GraspingClaw GraspingClaw => new();
    public static RuneCorruption RuneCorruption => new();
    public static FanningFire FanningFire => new();

    // Summon Undead
    public static SummonSkeletonsCounter SummonSkeletonsCounter => new();
    public static SummonLesserUndeadCounter SummonLesserUndeadCounter => new();

    // Stun
    public static ColossalBlow ColossalBlow => new();

    // Poison
    public static PoisonGasCounter PoisonGasCounter => new();
    public static PoisonGasAreaAttack PoisonGasAreaAttack => new();

    // Explosions
    public static DeathExplosion DeathExplosion => new();

    // Direct attack counters
    public static ThrowHatchetCounter ThrowHatchetCounter => new();
    public static EnergyBoltCounter EnergyBoltCounter => new();
    public static FanThrowCounter FanThrowCounter => new();

    public static DestroyEquipment DestroyEquipment => new();

    public static DrainLifeAreaAttack DrainLifeAreaAttack => new();

    public static MagicalBarrier MagicalBarrier => new();

    public static ReflectPhysicalDamage ReflectPhysicalDamage => new();
}
