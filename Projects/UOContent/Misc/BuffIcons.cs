using System;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;

namespace Server;

public class BuffInfo
{
    private TimerExecutionToken _timerToken;

    public BuffInfo(
        BuffIcon iconID, int titleCliloc, TimeSpan duration = default, TextDefinition args = null,
        bool retainThroughDeath = false
    ) : this(iconID, titleCliloc, titleCliloc + 1, duration, args, retainThroughDeath)
    {
    }

    public BuffInfo(
        BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan duration = default, TextDefinition args = null,
        bool retainThroughDeath = false
    )
    {
        ID = iconID;
        TitleCliloc = titleCliloc;
        SecondaryCliloc = secondaryCliloc;
        Duration = duration;
        Args = args;
        RetainThroughDeath = retainThroughDeath;
    }

    public static bool Enabled { get; private set; }

    public BuffIcon ID { get; }

    public int TitleCliloc { get; }

    public int SecondaryCliloc { get; }

    public DateTime StartTime { get; private set; }

    public TimeSpan Duration { get; }

    public bool RetainThroughDeath { get; }

    public TextDefinition Args { get; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("buffIcons.enable", Core.ML);
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (!Enabled)
        {
            return;
        }

        pm.ResendBuffs();
    }

    public void StartTimer(Mobile m)
    {
        if (Duration != TimeSpan.Zero)
        {
            StartTime = Core.Now;
            Timer.StartTimer(Duration, () => RemoveBuff(m, this), out _timerToken);
        }
    }

    public static void AddBuff(Mobile m, BuffInfo b)
    {
        (m as PlayerMobile)?.AddBuff(b);
    }

    public static void RemoveBuff(Mobile m, BuffInfo b)
    {
        if (b == null)
        {
            return;
        }

        b._timerToken.Cancel();
        (m as PlayerMobile)?.RemoveBuff(b.ID);
    }

    public static void RemoveBuff(Mobile m, BuffIcon b)
    {
        (m as PlayerMobile)?.RemoveBuff(b);
    }
}

public enum BuffIcon : short
{
    DismountPrevention = 0x3E9,
    NoRearm = 0x3EA,
    //Currently, no 0x3EB or 0x3EC
    NightSight = 0x3ED,	//*
    DeathStrike,
    EvilOmen,
    HonoredDebuff,
    AchievePerfection,
    DivineFury,         //*
    EnemyOfOne,         //*
    HidingAndOrStealth, //*
    ActiveMeditation,   //*
    BloodOathCaster,    //*
    BloodOathCurse,     //*
    CorpseSkin,         //*
    Mindrot,            //*
    PainSpike,          //*
    Strangle,
    GiftOfRenewal,     //*
    AttuneWeapon,      //*
    Thunderstorm,      //*
    EssenceOfWind,     //*
    EtherealVoyage,    //*
    GiftOfLife,        //*
    ArcaneEmpowerment, //*
    MortalStrike,
    ReactiveArmor, //*
    Protection,    //*
    ArchProtection,
    MagicReflection, //*
    Incognito,       //*
    Disguised,
    AnimalForm,
    Polymorph,
    Invisibility, //*
    Paralyze,     //*
    Poison,
    Bleed,
    Clumsy,     //*
    FeebleMind, //*
    Weaken,     //*
    Curse,      //*
    MassCurse,
    Agility,  //*
    Cunning,  //*
    Strength, //*
    Bless,    //*
    Sleep,
    StoneForm,
    SpellPlague,
    Berserk,
    MassSleep,
    Fly,
    Inspire,
    Invigorate,
    Resilience,
    Perseverance,
    TribulationTarget,
    DespairTarget,
    FishPie = 0x426,
    HitLowerAttack,
    HitLowerDefense,
    DualWield,
    Block,
    DefenseMastery,
    DespairCaster,
    Healing,
    SpellFocusingBuff,
    SpellFocusingDebuff,
    RageFocusingDebuff,
    RageFocusingBuff,
    Warding,
    TribulationCaster,
    ForceArrow,
    Disarm,
    Surge,
    Feint,
    TalonStrike,
    PsychicAttack,
    ConsecrateWeapon,
    GrapesOfWrath,
    EnemyOfOneDebuff,
    HorrificBeast,
    LichForm,
    VampiricEmbrace,
    CurseWeapon,
    ReaperForm,
    ImmolatingWeapon,
    Enchant,
    HonorableExecution,
    Confidence,
    Evasion,
    CounterAttack,
    LightningStrike,
    MomentumStrike,
    OrangePetals,
    RoseOfTrinsic,
    PoisonImmunity,
    Veterinary,
    Perfection,
    Honored,
    ManaPhase,
    FanDancerFanFire,
    Rage,
    Webbing,
    MedusaStone,
    TrueFear,
    AuraOfNausea,
    HowlOfCacophony,
    GazeDespair,
    HiryuPhysicalResistance,
    RuneBeetleCorruption,
    BloodwormAnemia,
    RotwormBloodDisease,
    SkillUseDelay,
    FactionStatLoss,
    HeatOfBattleStatus,
    CriminalStatus,
    ArmorPierce,
    SplinteringEffect,
    SwingSpeedDebuff,
    WraithForm,
    CityTradeDeal = 0x466,
    HumilityDebuff = 0x467,
    Spirituality,
    Humility,
    // Skill Masteries
    Rampage,
    Stagger, // Debuff
    Toughness,
    Thrust,
    Pierce,   // Debuff
    PlayingTheOdds,
    FocusedEye,
    Onslaught, // Debuff
    ElementalFury,
    ElementalFuryDebuff, // Debuff
    CalledShot,
    Knockout,
    SavingThrow,
    Conduit,
    EtherealBurst,
    MysticWeapon,
    ManaShield,
    AnticipateHit,
    Warcry,
    Shadow,
    WhiteTigerForm,
    Bodyguard,
    HeightenedSenses,
    Tolerance,
    DeathRay,
    DeathRayDebuff,
    Intuition,
    EnchantedSummoning,
    ShieldBash,
    Whispering,
    CombatTraining,
    InjectedStrikeDebuff,
    InjectedStrike,
    UnknownTomato,
    PlayingTheOddsDebuff,
    DragonTurtleDebuff,
    Boarding,
    Potency,
    ThrustDebuff,
    FistsOfFury, // 1169
    BarrabHemolymphConcentrate,
    JukariBurnPoiltice,
    KurakAmbushersEssence,
    BarakoDraftOfMight,
    UraliTranceTonic,
    SakkhraProphylaxis, // 1175
    Sparks,
    Swarm,
    BoneBreaker,
    Unknown2,
    SwarmImmune,
    BoneBreakerImmune,
    UnknownGoblin,
    UnknownRedDrop,
    UnknownStar,
    FeintDebuff,
    CaddelliteInfused,
    PotionGloriousFortune,
    MysticalPolymorphTotem,
    UnknownDebuff,
}
