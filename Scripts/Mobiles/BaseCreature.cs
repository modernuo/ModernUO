using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server.ContextMenus;
using Server.Engines.ConPVP;
using Server.Engines.MLQuests;
using Server.Engines.PartySystem;
using Server.Engines.Quests;
using Server.Engines.Quests.Doom;
using Server.Engines.Quests.Haven;
using Server.Ethics;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Necromancy;
using Server.Spells.Sixth;
using Server.Spells.Spellweaving;
using Server.Targeting;

namespace Server.Mobiles
{
  #region Enums

  /// <summary>
  ///   Summary description for MobileAI.
  /// </summary>
  public enum FightMode
  {
    None, // Never focus on others
    Aggressor, // Only attack aggressors
    Strongest, // Attack the strongest
    Weakest, // Attack the weakest
    Closest, // Attack the closest
    Evil // Only attack aggressor -or- negative karma
  }

  public enum OrderType
  {
    None, //When no order, let's roam
    Come, //"(All/Name) come"  Summons all or one pet to your location.
    Drop, //"(Name) drop"  Drops its loot to the ground (if it carries any).
    Follow, //"(Name) follow"  Follows targeted being.

    //"(All/Name) follow me"  Makes all or one pet follow you.
    Friend, //"(Name) friend"  Allows targeted player to confirm resurrection.
    Unfriend, // Remove a friend
    Guard, //"(Name) guard"  Makes the specified pet guard you. Pets can only guard their owner.

    //"(All/Name) guard me"  Makes all or one pet guard you.
    Attack, //"(All/Name) kill",

    //"(All/Name) attack"  All or the specified pet(s) currently under your control attack the target.
    Patrol, //"(Name) patrol"  Roves between two or more guarded targets.
    Release, //"(Name) release"  Releases pet back into the wild (removes "tame" status).
    Stay, //"(All/Name) stay" All or the specified pet(s) will stop and stay in current spot.
    Stop, //"(All/Name) stop Cancels any current orders to attack, guard or follow.
    Transfer //"(Name) transfer" Transfers complete ownership to targeted player.
  }

  [Flags]
  public enum FoodType
  {
    None = 0x0000,
    Meat = 0x0001,
    FruitsAndVegies = 0x0002,
    GrainsAndHay = 0x0004,
    Fish = 0x0008,
    Eggs = 0x0010,
    Gold = 0x0020
  }

  [Flags]
  public enum PackInstinct
  {
    None = 0x0000,
    Canine = 0x0001,
    Ostard = 0x0002,
    Feline = 0x0004,
    Arachnid = 0x0008,
    Daemon = 0x0010,
    Bear = 0x0020,
    Equine = 0x0040,
    Bull = 0x0080
  }

  public enum ScaleType
  {
    Red,
    Yellow,
    Black,
    Green,
    White,
    Blue,
    All
  }

  public enum MeatType
  {
    Ribs,
    Bird,
    LambLeg
  }

  public enum HideType
  {
    Regular,
    Spined,
    Horned,
    Barbed
  }

  #endregion

  public class DamageStore : IComparable<DamageStore>
  {
    public int m_Damage;
    public bool m_HasRight;
    public Mobile m_Mobile;

    public DamageStore(Mobile m, int damage)
    {
      m_Mobile = m;
      m_Damage = damage;
    }

    public int CompareTo(DamageStore ds)
    {
      return ds?.m_Damage ?? 0 - m_Damage;
    }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class FriendlyNameAttribute : Attribute
  {
    public FriendlyNameAttribute(TextDefinition friendlyName)
    {
      FriendlyName = friendlyName;
    }
    //future use: Talisman 'Protection/Bonus vs. Specific Creature

    public TextDefinition FriendlyName{ get; }

    public static TextDefinition GetFriendlyNameFor(Type t)
    {
      if (t.IsDefined(typeof(FriendlyNameAttribute), false))
      {
        object[] objs = t.GetCustomAttributes(typeof(FriendlyNameAttribute), false);

        if (objs.Length > 0)
          return (objs[0] as FriendlyNameAttribute)?.FriendlyName ?? "";
      }

      return t.Name;
    }
  }

  public class BaseCreature : Mobile, IHonorTarget, IQuestGiver
  {
    public const int MaxLoyalty = 100;

    public const int MaxOwners = 5;

    public const int DefaultRangePerception = 16;
    public const int OldRangePerception = 10;

    private const double ChanceToRummage = 0.5; // 50%

    private const double MinutesToNextRummageMin = 1.0;
    private const double MinutesToNextRummageMax = 4.0;

    private const double MinutesToNextChanceMin = 0.25;
    private const double MinutesToNextChanceMax = 0.75;

    private static Type[] m_AnimateDeadTypes =
    {
      typeof(MoundOfMaggots), typeof(HellSteed), typeof(SkeletalMount),
      typeof(WailingBanshee), typeof(Wraith), typeof(SkeletalDragon),
      typeof(LichLord), typeof(FleshGolem), typeof(Lich),
      typeof(SkeletalKnight), typeof(BoneKnight), typeof(Mummy),
      typeof(SkeletalMage), typeof(BoneMagi), typeof(PatchworkSkeleton)
    };

    private static double[] m_StandardActiveSpeeds =
    {
      0.175, 0.1, 0.15, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.8
    };

    private static double[] m_StandardPassiveSpeeds =
    {
      0.350, 0.2, 0.4, 0.5, 0.6, 0.8, 1.0, 1.2, 1.6, 2.0
    };

    private static Mobile m_NoDupeGuards;

    private static bool EnableRummaging = true;

    private DateTime m_IdleReleaseTime;

    private long m_NextRummageTime;

    /* until we are sure about who should be getting deleted, move them instead */
    /* On OSI, they despawn */

    private bool m_ReturnQueued;

    public BaseCreature(AIType ai,
      FightMode mode,
      int iRangePerception,
      int iRangeFight,
      double dActiveSpeed,
      double dPassiveSpeed)
    {
      if (iRangePerception == OldRangePerception)
        iRangePerception = DefaultRangePerception;

      m_Loyalty = MaxLoyalty; // Wonderfully Happy

      m_CurrentAI = ai;
      m_DefaultAI = ai;

      RangePerception = iRangePerception;
      RangeFight = iRangeFight;

      FightMode = mode;

      m_Team = 0;

      SpeedInfo.GetSpeeds(this, ref dActiveSpeed, ref dPassiveSpeed);

      ActiveSpeed = dActiveSpeed;
      PassiveSpeed = dPassiveSpeed;
      m_CurrentSpeed = dPassiveSpeed;

      Debug = false;

      m_SpellAttack = new List<Type>();
      m_SpellDefense = new List<Type>();

      m_Controlled = false;
      m_ControlMaster = null;
      ControlTarget = null;
      m_ControlOrder = OrderType.None;

      m_bTamable = false;

      Owners = new List<Mobile>();

      NextReacquireTime = Core.TickCount + (int)ReacquireDelay.TotalMilliseconds;

      ChangeAIType(AI);

      InhumanSpeech speechType = SpeechType;

      speechType?.OnConstruct(this);

      if (IsInvulnerable && !Core.AOS)
        NameHue = 0x35;

      GenerateLoot(true);
    }

    public BaseCreature(Serial serial) : base(serial)
    {
      m_SpellAttack = new List<Type>();
      m_SpellDefense = new List<Type>();

      Debug = false;
    }

    public virtual string DefaultName => null;
    public virtual string CorpseName => null;

    [CommandProperty(AccessLevel.GameMaster)]
    public override string Name
    {
      get
      {
        if (NameMod == null && base.Name == null)
          return DefaultName;

        return base.Name;
      }
      set => base.Name = value == DefaultName ? null : value;
    }

    public virtual InhumanSpeech SpeechType => null;

    /* Do not serialize this till the code is finalized */

    [CommandProperty(AccessLevel.GameMaster)]
    public bool SeeksHome{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string CorpseNameOverride{ get; set; }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool IsStabled
    {
      get => m_IsStabled;
      set
      {
        m_IsStabled = value;
        if (m_IsStabled)
          StopDeleteTimer();
      }
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public Mobile StabledBy{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsPrisoner{ get; set; }

    protected DateTime SummonEnd{ get; set; }

    public virtual Faction FactionAllegiance => null;
    public virtual int FactionSilverWorth => 30;

    public virtual double WeaponAbilityChance => 0.4;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsParagon
    {
      get => m_Paragon;
      set
      {
        if (m_Paragon == value)
          return;
        if (value)
          Paragon.Convert(this);
        else
          Paragon.UnConvert(this);

        m_Paragon = value;

        InvalidateProperties();
      }
    }

    public virtual bool HasManaOveride => false;

    public virtual FoodType FavoriteFood => FoodType.Meat;
    public virtual PackInstinct PackInstinct => PackInstinct.None;

    public List<Mobile> Owners{ get; private set; }

    public virtual bool AllowMaleTamer => true;
    public virtual bool AllowFemaleTamer => true;
    public virtual bool SubdueBeforeTame => false;
    public virtual bool StatLossAfterTame => SubdueBeforeTame;
    public virtual bool ReduceSpeedWithDamage => true;
    public virtual bool IsSubdued => SubdueBeforeTame && Hits < HitsMax / 10;

    public virtual bool Commandable => true;

    public virtual Poison HitPoison => null;
    public virtual double HitPoisonChance => 0.5;
    public virtual Poison PoisonImmune => null;

    public virtual bool BardImmune => false;
    public virtual bool Unprovokable => BardImmune || IsDeadPet;
    public virtual bool Uncalmable => BardImmune || IsDeadPet;
    public virtual bool AreaPeaceImmune => BardImmune || IsDeadPet;

    public virtual bool BleedImmune => false;
    public virtual double BonusPetDamageScalar => 1.0;

    public virtual bool DeathAdderCharmable => false;

    //TODO: Find the pub 31 tweaks to the DispelDifficulty and apply them of course.
    public virtual double DispelDifficulty // at this skill level we dispel 50% chance
      => 0.0;

    public virtual double DispelFocus // at difficulty - focus we have 0%, at difficulty + focus we have 100%
      => 20.0;

    public virtual bool DisplayWeight => Backpack is StrongBackpack;

    public virtual bool CanFly => false;

    public virtual bool IsInvulnerable => false;

    public BaseAI AIObject{ get; private set; }

    public virtual OppositionGroup OppositionGroup => null;

    public virtual bool IsAnimatedDead
    {
      get
      {
        if (!Summoned)
          return false;

        Type type = GetType();

        bool contains = false;

        for (int i = 0; !contains && i < m_AnimateDeadTypes.Length; ++i)
          contains = type == m_AnimateDeadTypes[i];

        return contains;
      }
    }

    public virtual bool IsNecroFamiliar => Summoned && m_ControlMaster != null &&
        SummonFamiliarSpell.Table.TryGetValue(m_ControlMaster, out BaseCreature bc) &&
        bc == this;

    public virtual bool DeleteCorpseOnDeath => !Core.AOS && m_bSummoned;

    [CommandProperty(AccessLevel.GameMaster)]
    public int Loyalty
    {
      get => m_Loyalty;
      set => m_Loyalty = Math.Min(Math.Max(value, 0), MaxLoyalty);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public WayPoint CurrentWayPoint{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public IPoint2D TargetLocation{ get; set; }

    public virtual Mobile ConstantFocus => null;

    public virtual bool DisallowAllMoves => false;

    public virtual bool InitialInnocent => false;

    public virtual bool AlwaysMurderer => false;

    public virtual bool AlwaysAttackable => false;

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int DamageMin
    {
      get => m_DamageMin;
      set => m_DamageMin = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int DamageMax
    {
      get => m_DamageMax;
      set => m_DamageMax = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int HitsMax
    {
      get
      {
        if (HitsMaxSeed <= 0)
          return Str;

        int value = HitsMaxSeed + GetStatOffset(StatType.Str);

        if (value < 1)
          value = 1;
        else if (value > 65000)
          value = 65000;

        return value;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int HitsMaxSeed{ get; set; } = -1;

    [CommandProperty(AccessLevel.GameMaster)]
    public override int StamMax
    {
      get
      {
        if (StamMaxSeed <= 0)
          return Dex;

        int value = StamMaxSeed + GetStatOffset(StatType.Dex);

        if (value < 1)
          value = 1;
        else if (value > 65000)
          value = 65000;

        return value;

      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int StamMaxSeed{ get; set; } = -1;

    [CommandProperty(AccessLevel.GameMaster)]
    public override int ManaMax
    {
      get
      {
        if (ManaMaxSeed <= 0)
          return Int;

        int value = ManaMaxSeed + GetStatOffset(StatType.Int);

        if (value < 1)
          value = 1;
        else if (value > 65000)
          value = 65000;

        return value;

      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ManaMaxSeed{ get; set; } = -1;

    public virtual bool CanOpenDoors => !Body.IsAnimal && !Body.IsSea;

    public virtual bool CanMoveOverObstacles => Core.AOS || Body.IsMonster;

    public virtual bool CanDestroyObstacles => false;

    /*
    Seems this actually was removed on OSI somewhere between the original bug report and now.
    We will call it ML, until we can get better information. I suspect it was on the OSI TC when
    originally it taken out of RunUO, and not implemented on OSIs production shards until more
    recently.  Either way, this is, or was, accurate OSI behavior, and just entirely
    removing it was incorrect.  OSI followers were distracted by being attacked well into
    AoS, at very least.

    */

    public virtual bool CanBeDistracted => !Core.ML;

    public override bool ShouldCheckStatTimers => false;

    public virtual bool CanAngerOnTame => false;

    protected virtual BaseAI ForcedAI => null;

    [CommandProperty(AccessLevel.GameMaster)]
    public AIType AI
    {
      get => m_CurrentAI;
      set
      {
        m_CurrentAI = value;

        if (m_CurrentAI == AIType.AI_Use_Default)
          m_CurrentAI = m_DefaultAI;

        ChangeAIType(m_CurrentAI);
      }
    }

    [CommandProperty(AccessLevel.Administrator)]
    public bool Debug{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Team
    {
      get => m_Team;
      set
      {
        m_Team = value;
        OnTeamChange();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile FocusMob{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public FightMode FightMode{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int RangePerception{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int RangeFight{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int RangeHome{ get; set; } = 10;

    [CommandProperty(AccessLevel.GameMaster)]
    public double ActiveSpeed{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public double PassiveSpeed{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public double CurrentSpeed
    {
      get => TargetLocation != null ? 0.3 : m_CurrentSpeed;
      set
      {
        if (m_CurrentSpeed != value)
        {
          m_CurrentSpeed = value;
          AIObject?.OnCurrentSpeedChanged();
        }
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D Home
    {
      get => m_Home;
      set => m_Home = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Map HomeMap{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Controlled
    {
      get => m_Controlled;
      set
      {
        if (m_Controlled == value)
          return;

        m_Controlled = value;
        Delta(MobileDelta.Noto);

        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile ControlMaster
    {
      get => m_ControlMaster;
      set
      {
        if (m_ControlMaster == value || this == value)
          return;

        RemoveFollowers();
        m_ControlMaster = value;
        AddFollowers();
        if (m_ControlMaster != null)
          StopDeleteTimer();

        Delta(MobileDelta.Noto);
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile SummonMaster
    {
      get => m_SummonMaster;
      set
      {
        if (m_SummonMaster == value || this == value)
          return;

        RemoveFollowers();
        m_SummonMaster = value;
        AddFollowers();

        Delta(MobileDelta.Noto);
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile ControlTarget{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D ControlDest{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public OrderType ControlOrder
    {
      get => m_ControlOrder;
      set
      {
        m_ControlOrder = value;

        AIObject?.OnCurrentOrderChanged();

        InvalidateProperties();

        m_ControlMaster?.InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool BardProvoked{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool BardPacified{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile BardMaster{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile BardTarget{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime BardEndTime{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MinTameSkill{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Tamable
    {
      get => m_bTamable && !m_Paragon;
      set => m_bTamable = value;
    }

    [CommandProperty(AccessLevel.Administrator)]
    public bool Summoned
    {
      get => m_bSummoned;
      set
      {
        if (m_bSummoned == value)
          return;

        NextReacquireTime = Core.TickCount;

        m_bSummoned = value;
        Delta(MobileDelta.Noto);

        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.Administrator)]
    public int ControlSlots{ get; set; } = 1;

    public virtual bool NoHouseRestrictions => false;
    public virtual bool IsHouseSummonable => false;

    public virtual bool AutoDispel => false;
    public virtual double AutoDispelChance => Core.SE ? .10 : 1.0;

    public virtual bool IsScaryToPets => false;
    public virtual bool IsScaredOfScaryThings => true;

    public virtual bool CanRummageCorpses => false;

    public virtual bool DeleteOnRelease => m_bSummoned;

    public virtual bool CanDrop => IsBonded;

    public virtual int TreasureMapLevel => -1;

    public virtual bool IgnoreYoungProtection => false;

    public bool NoKillAwards{ get; set; }

    #region Mondain's Legacy

    public virtual bool GivesMLMinorArtifact => false;

    #endregion

    /* To save on cpu usage, RunUO creatures only reacquire creatures under the following circumstances:
     *  - 10 seconds have elapsed since the last time it tried
     *  - The creature was attacked
     *  - Some creatures, like dragons, will reacquire when they see someone move
     *
     * This functionality appears to be implemented on OSI as well
     */

    public long NextReacquireTime{ get; set; }

    public virtual TimeSpan ReacquireDelay => TimeSpan.FromSeconds(10.0);
    public virtual bool ReacquireOnMovement => false;
    public virtual bool AcquireOnApproach => m_Paragon;
    public virtual int AcquireOnApproachRange => 10;

    public static bool Summoning{ get; set; }

    public virtual bool CanBreath => HasBreath && !Summoned;
    public virtual bool IsDispellable => Summoned && !IsAnimatedDead;

    public virtual bool
      PlayerRangeSensitive //If they are following a waypoint, they'll continue to follow it even if players aren't around
      => CurrentWayPoint == null;

    public virtual bool ReturnsToHome =>
      SeeksHome && Home != Point3D.Zero && !m_ReturnQueued && !Controlled && !Summoned;

    // used for deleting untamed creatures [in houses]

    [CommandProperty(AccessLevel.GameMaster)]
    public bool RemoveIfUntamed{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int RemoveStep{ get; set; }

    public HonorContext ReceivedHonorContext{ get; set; }

    public virtual WeaponAbility GetWeaponAbility()
    {
      return null;
    }

    public virtual bool IsEnemy(Mobile m)
    {
      if (OppositionGroup?.IsEnemy(this, m) == true)
        return true;

      if (m is BaseGuard)
        return false;

      if (GetFactionAllegiance(m) == Allegiance.Ally)
        return false;

      Ethic ourEthic = EthicAllegiance;
      Player pl = Ethics.Player.Find(m, true);

      if (pl?.IsShielded == true && (ourEthic == null || ourEthic == pl.Ethic))
        return false;

      if (m is PlayerMobile mobile && mobile.HonorActive)
        return false;

      if (!(m is BaseCreature c) || m is MilitiaFighter)
        return true;

      if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)))
        return false;

      if (FightMode == FightMode.Evil && m.Karma < 0 || c.FightMode == FightMode.Evil && Karma < 0)
        return true;

      return m_Team != c.m_Team || (m_bSummoned || m_Controlled) != (c.m_bSummoned || c.m_Controlled);
    }

    public override string ApplyNameSuffix(string suffix)
    {
      if (IsParagon && !GivesMLMinorArtifact)
      {
        suffix = suffix.Length == 0 ? "(Paragon)" : $"{suffix} (Paragon)";
      }

      return base.ApplyNameSuffix(suffix);
    }

    public virtual bool CheckControlChance(Mobile m)
    {
      if (GetControlChance(m) > Utility.RandomDouble())
      {
        Loyalty += 1;
        return true;
      }

      PlaySound(GetAngerSound());

      if (Body.IsAnimal)
        Animate(10, 5, 1, true, false, 0);
      else if (Body.IsMonster)
        Animate(18, 5, 1, true, false, 0);

      Loyalty -= 3;
      return false;
    }

    public virtual bool CanBeControlledBy(Mobile m)
    {
      return GetControlChance(m) > 0.0;
    }

    public virtual double GetControlChance(Mobile m, bool useBaseSkill = false)
    {
      if (MinTameSkill <= 291 || m_bSummoned || m.AccessLevel >= AccessLevel.GameMaster)
        return 1.0;

      int dMinTameSkill = MinTameSkill;

      if (dMinTameSkill > -249 && AnimalTaming.CheckMastery(m, this))
        dMinTameSkill = -249;

      int taming =
        (int)((useBaseSkill ? m.Skills.AnimalTaming.Base : m.Skills.AnimalTaming.Value) * 10);
      int lore =
        (int)((useBaseSkill ? m.Skills.AnimalLore.Base : m.Skills.AnimalLore.Value) * 10);
      int bonus, chance = 700;

      if (Core.ML)
      {
        int SkillBonus = taming - dMinTameSkill;
        int LoreBonus = lore - dMinTameSkill;

        int SkillMod = 6, LoreMod = 6;

        if (SkillBonus < 0)
          SkillMod = 28;
        if (LoreBonus < 0)
          LoreMod = 14;

        SkillBonus *= SkillMod;
        LoreBonus *= LoreMod;

        bonus = (SkillBonus + LoreBonus) / 2;
      }
      else
      {
        int difficulty = dMinTameSkill;
        int weighted = (taming * 4 + lore) / 5;
        bonus = weighted - difficulty;

        if (bonus <= 0)
          bonus *= 14;
        else
          bonus *= 6;
      }

      chance += bonus;

      if (chance >= 0 && chance < 200)
        chance = 200;
      else if (chance > 990)
        chance = 990;

      chance -= (MaxLoyalty - m_Loyalty) * 10;

      return (double)chance / 1000;
    }

    public override void Damage(int amount, Mobile from)
    {
      int oldHits = Hits;

      if (Core.AOS && !Summoned && Controlled && 0.2 > Utility.RandomDouble())
        amount = (int)(amount * BonusPetDamageScalar);

      if (EvilOmenSpell.TryEndEffect(this))
        amount = (int)(amount * 1.25);

      Mobile oath = BloodOathSpell.GetBloodOath(from);

      if (oath == this)
      {
        amount = (int)(amount * 1.1);
        from.Damage(amount, from);
      }

      base.Damage(amount, from);

      if (SubdueBeforeTame && !Controlled && oldHits > HitsMax / 10 && Hits <= HitsMax / 10)
        PublicOverheadMessage(MessageType.Regular, 0x3B2, false,
          "* The creature has been beaten into subjugation! *");
    }

    public override void SetLocation(Point3D newLocation, bool isTeleport)
    {
      base.SetLocation(newLocation, isTeleport);

      if (isTeleport)
        AIObject?.OnTeleported();
    }

    public override void OnBeforeSpawn(Point3D location, Map m)
    {
      if (Paragon.CheckConvert(this, location, m))
        IsParagon = true;

      base.OnBeforeSpawn(location, m);
    }

    public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
    {
      if (!Alive || IsDeadPet)
        return ApplyPoisonResult.Immune;

      if (EvilOmenSpell.TryEndEffect(this))
        poison = PoisonImpl.IncreaseLevel(poison);

      ApplyPoisonResult result = base.ApplyPoison(from, poison);

      if (from != null && result == ApplyPoisonResult.Poisoned && PoisonTimer is PoisonImpl.PoisonTimer timer)
        timer.From = from;

      return result;
    }

    public override bool CheckPoisonImmunity(Mobile from, Poison poison)
    {
      return base.CheckPoisonImmunity(from, poison) ||
             (m_Paragon ? PoisonImpl.IncreaseLevel(PoisonImmune) : PoisonImmune)?.Level >= poison.Level;
    }

    public void Unpacify()
    {
      BardEndTime = DateTime.UtcNow;
      BardPacified = false;
    }

    public virtual void CheckDistracted(Mobile from)
    {
      if (Utility.RandomDouble() < .10)
      {
        ControlTarget = from;
        ControlOrder = OrderType.Attack;
        Combatant = from;
        Warmode = true;
      }
    }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
      if (BardPacified && (HitsMax - Hits) * 0.001 > Utility.RandomDouble())
        Unpacify();

      int disruptThreshold;
      //NPCs can use bandages too!
      if (!Core.AOS)
        disruptThreshold = 0;
      else if (from?.Player == true)
        disruptThreshold = 18;
      else
        disruptThreshold = 25;

      if (amount > disruptThreshold)
      {
        BandageContext c = BandageContext.GetContext(this);

        c?.Slip();
      }

      if (Confidence.IsRegenerating(this))
        Confidence.StopRegenerating(this);

      WeightOverloading.FatigueOnDamage(this, amount);

      InhumanSpeech speechType = SpeechType;

      if (speechType != null && !willKill)
        speechType.OnDamage(this, amount);

      ReceivedHonorContext?.OnTargetDamaged(from, amount);

      if (!willKill)
      {
        if (CanBeDistracted && ControlOrder == OrderType.Follow) CheckDistracted(from);
      }
      else if (from is PlayerMobile mobile)
      {
        Timer.DelayCall(TimeSpan.FromSeconds(10), mobile.RecoverAmmo);
      }

      base.OnDamage(amount, from, willKill);
    }

    public virtual void OnDamagedBySpell(Mobile from)
    {
      if (CanBeDistracted && ControlOrder == OrderType.Follow) CheckDistracted(from);
    }

    public virtual void OnHarmfulSpell(Mobile from)
    {
    }

    public virtual void CheckReflect(Mobile caster, ref bool reflect)
    {
    }

    public virtual void OnCarve(Mobile from, Corpse corpse, Item with)
    {
      int feathers = Feathers;
      int wool = Wool;
      int meat = Meat;
      int hides = Hides;
      int scales = Scales;

      if (feathers == 0 && wool == 0 && meat == 0 && hides == 0 && scales == 0 || Summoned || IsBonded ||
          corpse.Animated)
      {
        if (corpse.Animated)
          corpse.SendLocalizedMessageTo(from, 500464); // Use this on corpses to carve away meat and hide
        else
          from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
      }
      else
      {
        if (Core.ML && from.Race == Race.Human)
          hides = (int)Math.Ceiling(hides * 1.1); // 10% bonus only applies to hides, ore & logs

        if (corpse.Map == Map.Felucca)
        {
          feathers *= 2;
          wool *= 2;
          hides *= 2;

          if (Core.ML)
          {
            meat *= 2;
            scales *= 2;
          }
        }

        new Blood(0x122D).MoveToWorld(corpse.Location, corpse.Map);

        if (feathers != 0)
        {
          corpse.AddCarvedItem(new Feather(feathers), from);
          from.SendLocalizedMessage(500479); // You pluck the bird. The feathers are now on the corpse.
        }

        if (wool != 0)
        {
          corpse.AddCarvedItem(new TaintedWool(wool), from);
          from.SendLocalizedMessage(500483); // You shear it, and the wool is now on the corpse.
        }

        if (meat != 0)
        {
          if (MeatType == MeatType.Ribs)
            corpse.AddCarvedItem(new RawRibs(meat), from);
          else if (MeatType == MeatType.Bird)
            corpse.AddCarvedItem(new RawBird(meat), from);
          else if (MeatType == MeatType.LambLeg)
            corpse.AddCarvedItem(new RawLambLeg(meat), from);

          from.SendLocalizedMessage(500467); // You carve some meat, which remains on the corpse.
        }

        if (hides != 0)
        {
          Item holding = from.Weapon as Item;

          if (Core.AOS && holding is SkinningKnife)
          {
            Item leather = null;

            switch (HideType)
            {
              case HideType.Regular:
                leather = new Leather(hides);
                break;
              case HideType.Spined:
                leather = new SpinedLeather(hides);
                break;
              case HideType.Horned:
                leather = new HornedLeather(hides);
                break;
              case HideType.Barbed:
                leather = new BarbedLeather(hides);
                break;
            }

            if (leather != null)
            {
              if (!from.PlaceInBackpack(leather))
              {
                corpse.DropItem(leather);
                from.SendLocalizedMessage(500471); // You skin it, and the hides are now in the corpse.
              }
              else
              {
                from.SendLocalizedMessage(
                  1073555); // You skin it and place the cut-up hides in your backpack.
              }
            }
          }
          else
          {
            if (HideType == HideType.Regular)
              corpse.DropItem(new Hides(hides));
            else if (HideType == HideType.Spined)
              corpse.DropItem(new SpinedHides(hides));
            else if (HideType == HideType.Horned)
              corpse.DropItem(new HornedHides(hides));
            else if (HideType == HideType.Barbed)
              corpse.DropItem(new BarbedHides(hides));

            from.SendLocalizedMessage(500471); // You skin it, and the hides are now in the corpse.
          }
        }

        if (scales != 0)
        {
          ScaleType sc = ScaleType;

          switch (sc)
          {
            case ScaleType.Red:
              corpse.AddCarvedItem(new RedScales(scales), from);
              break;
            case ScaleType.Yellow:
              corpse.AddCarvedItem(new YellowScales(scales), from);
              break;
            case ScaleType.Black:
              corpse.AddCarvedItem(new BlackScales(scales), from);
              break;
            case ScaleType.Green:
              corpse.AddCarvedItem(new GreenScales(scales), from);
              break;
            case ScaleType.White:
              corpse.AddCarvedItem(new WhiteScales(scales), from);
              break;
            case ScaleType.Blue:
              corpse.AddCarvedItem(new BlueScales(scales), from);
              break;
            case ScaleType.All:
            {
              corpse.AddCarvedItem(new RedScales(scales), from);
              corpse.AddCarvedItem(new YellowScales(scales), from);
              corpse.AddCarvedItem(new BlackScales(scales), from);
              corpse.AddCarvedItem(new GreenScales(scales), from);
              corpse.AddCarvedItem(new WhiteScales(scales), from);
              corpse.AddCarvedItem(new BlueScales(scales), from);
              break;
            }
          }

          from.SendMessage("You cut away some scales, but they remain on the corpse.");
        }

        corpse.Carved = true;

        if (corpse.IsCriminalAction(from))
          from.CriminalAction(true);
      }
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(19); // version

      writer.Write((int)m_CurrentAI);
      writer.Write((int)m_DefaultAI);

      writer.Write(RangePerception);
      writer.Write(RangeFight);

      writer.Write(m_Team);

      writer.Write(ActiveSpeed);
      writer.Write(PassiveSpeed);
      writer.Write(m_CurrentSpeed);

      writer.Write(m_Home.X);
      writer.Write(m_Home.Y);
      writer.Write(m_Home.Z);

      // Version 1
      writer.Write(RangeHome);

      int i = 0;

      writer.Write(m_SpellAttack.Count);
      for (i = 0; i < m_SpellAttack.Count; i++) writer.Write(m_SpellAttack[i].ToString());

      writer.Write(m_SpellDefense.Count);
      for (i = 0; i < m_SpellDefense.Count; i++) writer.Write(m_SpellDefense[i].ToString());

      // Version 2
      writer.Write((int)FightMode);

      writer.Write(m_Controlled);
      writer.Write(m_ControlMaster);
      writer.Write(ControlTarget);
      writer.Write(ControlDest);
      writer.Write((int)m_ControlOrder);
      writer.WriteEncodedInt(MinTameSkill);
      // Removed in version 9
      //writer.Write( (double) m_dMaxTameSkill );
      writer.Write(m_bTamable);
      writer.Write(m_bSummoned);

      if (m_bSummoned)
        writer.WriteDeltaTime(SummonEnd);

      writer.Write(ControlSlots);

      // Version 3
      writer.Write(m_Loyalty);

      // Version 4
      writer.Write(CurrentWayPoint);

      // Verison 5
      writer.Write(m_SummonMaster);

      // Version 6
      writer.Write(HitsMaxSeed);
      writer.Write(StamMaxSeed);
      writer.Write(ManaMaxSeed);
      writer.Write(m_DamageMin);
      writer.Write(m_DamageMax);

      // Version 7
      writer.Write(m_PhysicalResistance);
      writer.Write(PhysicalDamage);

      writer.Write(m_FireResistance);
      writer.Write(FireDamage);

      writer.Write(m_ColdResistance);
      writer.Write(ColdDamage);

      writer.Write(m_PoisonResistance);
      writer.Write(PoisonDamage);

      writer.Write(m_EnergyResistance);
      writer.Write(EnergyDamage);

      // Version 8
      writer.Write(Owners, true);

      // Version 10
      writer.Write(IsDeadPet);
      writer.Write(m_IsBonded);
      writer.Write(BondingBegin);
      writer.Write(OwnerAbandonTime);

      // Version 11
      writer.Write(m_HasGeneratedLoot);

      // Version 12
      writer.Write(m_Paragon);

      // Version 13
      writer.Write(Friends?.Count > 0);

      if (Friends?.Count > 0)
        writer.Write(Friends, true);

      // Version 14
      writer.Write(RemoveIfUntamed);
      writer.Write(RemoveStep);

      // Version 17
      if (IsStabled || Controlled && ControlMaster != null)
        writer.Write(TimeSpan.Zero);
      else
        writer.Write(DeleteTimeLeft);

      // Version 18
      writer.Write(CorpseNameOverride);

      // Version 19
      writer.Write(HomeMap);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      m_CurrentAI = (AIType)reader.ReadInt();
      m_DefaultAI = (AIType)reader.ReadInt();

      RangePerception = reader.ReadInt();
      RangeFight = reader.ReadInt();

      m_Team = reader.ReadInt();

      ActiveSpeed = reader.ReadDouble();
      PassiveSpeed = reader.ReadDouble();
      m_CurrentSpeed = reader.ReadDouble();

      if (RangePerception == OldRangePerception)
        RangePerception = DefaultRangePerception;

      m_Home.X = reader.ReadInt();
      m_Home.Y = reader.ReadInt();
      m_Home.Z = reader.ReadInt();

      if (version >= 1)
      {
        RangeHome = reader.ReadInt();

        int iCount = reader.ReadInt();
        for (int i = 0; i < iCount; i++)
        {
          string str = reader.ReadString();
          Type type = Type.GetType(str);

          if (type != null) m_SpellAttack.Add(type);
        }

        iCount = reader.ReadInt();
        for (int i = 0; i < iCount; i++)
        {
          string str = reader.ReadString();
          Type type = Type.GetType(str);

          if (type != null) m_SpellDefense.Add(type);
        }
      }
      else
      {
        RangeHome = 0;
      }

      if (version >= 2)
      {
        FightMode = (FightMode)reader.ReadInt();

        m_Controlled = reader.ReadBool();
        m_ControlMaster = reader.ReadMobile();
        ControlTarget = reader.ReadMobile();
        ControlDest = reader.ReadPoint3D();
        m_ControlOrder = (OrderType)reader.ReadInt();

        MinTameSkill = reader.ReadEncodedInt();

        if (version < 9)
          reader.ReadDouble();

        m_bTamable = reader.ReadBool();
        m_bSummoned = reader.ReadBool();

        if (m_bSummoned)
        {
          SummonEnd = reader.ReadDeltaTime();
          new UnsummonTimer(m_ControlMaster, this, SummonEnd - DateTime.UtcNow).Start();
        }

        ControlSlots = reader.ReadInt();
      }
      else
      {
        FightMode = FightMode.Closest;

        m_Controlled = false;
        m_ControlMaster = null;
        ControlTarget = null;
        m_ControlOrder = OrderType.None;
      }

      if (version >= 3)
        m_Loyalty = reader.ReadInt();
      else
        m_Loyalty = MaxLoyalty; // Wonderfully Happy

      if (version >= 4)
        CurrentWayPoint = reader.ReadItem() as WayPoint;

      if (version >= 5)
        m_SummonMaster = reader.ReadMobile();

      if (version >= 6)
      {
        HitsMaxSeed = reader.ReadInt();
        StamMaxSeed = reader.ReadInt();
        ManaMaxSeed = reader.ReadInt();
        m_DamageMin = reader.ReadInt();
        m_DamageMax = reader.ReadInt();
      }

      if (version >= 7)
      {
        m_PhysicalResistance = reader.ReadInt();
        PhysicalDamage = reader.ReadInt();

        m_FireResistance = reader.ReadInt();
        FireDamage = reader.ReadInt();

        m_ColdResistance = reader.ReadInt();
        ColdDamage = reader.ReadInt();

        m_PoisonResistance = reader.ReadInt();
        PoisonDamage = reader.ReadInt();

        m_EnergyResistance = reader.ReadInt();
        EnergyDamage = reader.ReadInt();
      }

      if (version >= 8)
        Owners = reader.ReadStrongMobileList();
      else
        Owners = new List<Mobile>();

      if (version >= 10)
      {
        IsDeadPet = reader.ReadBool();
        m_IsBonded = reader.ReadBool();
        BondingBegin = reader.ReadDateTime();
        OwnerAbandonTime = reader.ReadDateTime();
      }

      if (version >= 11)
        m_HasGeneratedLoot = reader.ReadBool();
      else
        m_HasGeneratedLoot = true;

      if (version >= 12)
        m_Paragon = reader.ReadBool();
      else
        m_Paragon = false;

      if (version >= 13 && reader.ReadBool())
        Friends = reader.ReadStrongMobileList();
      else if (version < 13 && m_ControlOrder >= OrderType.Unfriend)
        ++m_ControlOrder;

      if (version < 16 && Loyalty != MaxLoyalty)
        Loyalty *= 10;

      double activeSpeed = ActiveSpeed;
      double passiveSpeed = PassiveSpeed;

      SpeedInfo.GetSpeeds(this, ref activeSpeed, ref passiveSpeed);

      bool isStandardActive = false;
      for (int i = 0; !isStandardActive && i < m_StandardActiveSpeeds.Length; ++i)
        isStandardActive = ActiveSpeed == m_StandardActiveSpeeds[i];

      bool isStandardPassive = false;
      for (int i = 0; !isStandardPassive && i < m_StandardPassiveSpeeds.Length; ++i)
        isStandardPassive = PassiveSpeed == m_StandardPassiveSpeeds[i];

      if (isStandardActive && m_CurrentSpeed == ActiveSpeed)
        m_CurrentSpeed = activeSpeed;
      else if (isStandardPassive && m_CurrentSpeed == PassiveSpeed)
        m_CurrentSpeed = passiveSpeed;

      if (isStandardActive && !m_Paragon)
        ActiveSpeed = activeSpeed;

      if (isStandardPassive && !m_Paragon)
        PassiveSpeed = passiveSpeed;

      if (version >= 14)
      {
        RemoveIfUntamed = reader.ReadBool();
        RemoveStep = reader.ReadInt();
      }

      TimeSpan deleteTime = TimeSpan.Zero;

      if (version >= 17)
        deleteTime = reader.ReadTimeSpan();

      if (deleteTime > TimeSpan.Zero || LastOwner != null && !Controlled && !IsStabled)
      {
        if (deleteTime == TimeSpan.Zero)
          deleteTime = TimeSpan.FromDays(3.0);

        m_DeleteTimer = new DeleteTimer(this, deleteTime);
        m_DeleteTimer.Start();
      }

      if (version >= 18)
        CorpseNameOverride = reader.ReadString();

      if (version >= 19)
        HomeMap = reader.ReadMap();

      if (version <= 14 && m_Paragon && Hue == 0x31) Hue = Paragon.Hue; //Paragon hue fixed, should now be 0x501.

      if (Core.AOS && NameHue == 0x35)
        NameHue = -1;

      CheckStatTimers();

      ChangeAIType(m_CurrentAI);

      AddFollowers();

      if (IsAnimatedDead)
        AnimateDeadSpell.Register(m_SummonMaster, this);
    }

    public virtual bool IsHumanInTown()
    {
      return Body.IsHuman && Region.IsPartOf<GuardedRegion>();
    }

    public virtual bool CheckGold(Mobile from, Item dropped)
    {
      return dropped is Gold gold && OnGoldGiven(from, gold);
    }

    public virtual bool OnGoldGiven(Mobile from, Gold dropped)
    {
      if (CheckTeachingMatch(from))
      {
        if (Teach(m_Teaching, from, dropped.Amount, true))
        {
          dropped.Delete();
          return true;
        }
      }
      else if (IsHumanInTown())
      {
        Direction = GetDirectionTo(from);

        int oldSpeechHue = SpeechHue;

        SpeechHue = 0x23F;
        SayTo(from, "Thou art giving me gold?");

        SayTo(from, dropped.Amount >= 400 ? "'Tis a noble gift." : "Money is always welcome.");

        SpeechHue = 0x3B2;
        SayTo(from, 501548); // I thank thee.

        SpeechHue = oldSpeechHue;

        dropped.Delete();
        return true;
      }

      return false;
    }

    public virtual bool OverrideBondingReqs()
    {
      return false;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
      if (CheckFeed(from, dropped))
        return true;
      if (CheckGold(from, dropped))
        return true;

      // Note: Yes, this happens for all questers (regardless of type, e.g. escorts),
      // even if they can't offer you anything at the moment
      if (MLQuestSystem.Enabled && CanGiveMLQuest && from is PlayerMobile mobile)
      {
        MLQuestSystem.Tell(this, mobile,
          1074893); // You need to mark your quest items so I don't take the wrong object.  Then speak to me.
        return false;
      }

      return base.OnDragDrop(from, dropped);
    }

    public void ChangeAIType(AIType NewAI)
    {
      AIObject?.m_Timer.Stop();

      if (ForcedAI != null)
      {
        AIObject = ForcedAI;
        return;
      }

      AIObject = null;

      switch (NewAI)
      {
        case AIType.AI_Melee:
          AIObject = new MeleeAI(this);
          break;
        case AIType.AI_Animal:
          AIObject = new AnimalAI(this);
          break;
        case AIType.AI_Berserk:
          AIObject = new BerserkAI(this);
          break;
        case AIType.AI_Archer:
          AIObject = new ArcherAI(this);
          break;
        case AIType.AI_Healer:
          AIObject = new HealerAI(this);
          break;
        case AIType.AI_Vendor:
          AIObject = new VendorAI(this);
          break;
        case AIType.AI_Mage:
          AIObject = new MageAI(this);
          break;
        case AIType.AI_Predator:
          //m_AI = new PredatorAI(this);
          AIObject = new MeleeAI(this);
          break;
        case AIType.AI_Thief:
          AIObject = new ThiefAI(this);
          break;
      }
    }

    public virtual void OnTeamChange()
    {
    }

    public override void RevealingAction()
    {
      InvisibilitySpell.RemoveTimer(this);

      base.RevealingAction();
    }

    public void RemoveFollowers()
    {
      if (m_ControlMaster != null)
      {
        m_ControlMaster.Followers -= ControlSlots;
        if (m_ControlMaster is PlayerMobile mobile)
        {
          mobile.AllFollowers.Remove(this);
          if (mobile.AutoStabled.Contains(this))
            mobile.AutoStabled.Remove(this);
        }
      }
      else if (m_SummonMaster != null)
      {
        m_SummonMaster.Followers -= ControlSlots;
        (m_SummonMaster as PlayerMobile)?.AllFollowers.Remove(this);
      }

      if (m_ControlMaster?.Followers < 0)
        m_ControlMaster.Followers = 0;

      if (m_SummonMaster?.Followers < 0)
        m_SummonMaster.Followers = 0;
    }

    public void AddFollowers()
    {
      if (m_ControlMaster != null)
      {
        m_ControlMaster.Followers += ControlSlots;
        if (m_ControlMaster is PlayerMobile mobile)
          mobile.AllFollowers.Add(this);
      }
      else if (m_SummonMaster != null)
      {
        m_SummonMaster.Followers += ControlSlots;
        if (m_SummonMaster is PlayerMobile mobile)
          mobile.AllFollowers.Add(this);
      }
    }

    public virtual void OnGotMeleeAttack(Mobile attacker)
    {
      if (AutoDispel && attacker is BaseCreature creature && creature.IsDispellable &&
          AutoDispelChance > Utility.RandomDouble())
        Dispel(creature);
    }

    public virtual void Dispel(Mobile m)
    {
      Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 8, 20,
        5042);
      Effects.PlaySound(m, m.Map, 0x201);

      m.Delete();
    }

    public virtual void OnGaveMeleeAttack(Mobile defender)
    {
      Poison p = m_Paragon ? PoisonImpl.IncreaseLevel(HitPoison) : HitPoison;

      if (p != null && HitPoisonChance >= Utility.RandomDouble())
      {
        defender.ApplyPoison(this, p);

        if (Controlled)
          CheckSkill(SkillName.Poisoning, 0, Skills.Poisoning.CapFixedPoint);
      }

      if (AutoDispel && defender is BaseCreature creature && creature.IsDispellable &&
          AutoDispelChance > Utility.RandomDouble())
        Dispel(creature);
    }

    public override void OnAfterDelete()
    {
      if (AIObject != null)
      {
        AIObject.m_Timer?.Stop();
        AIObject = null;
      }

      if (m_DeleteTimer != null)
      {
        m_DeleteTimer.Stop();
        m_DeleteTimer = null;
      }

      FocusMob = null;

      if (IsAnimatedDead)
        AnimateDeadSpell.Unregister(m_SummonMaster, this);

      if (MLQuestSystem.Enabled)
        MLQuestSystem.HandleDeletion(this);

      base.OnAfterDelete();
    }

    public void DebugSay(string text)
    {
      if (Debug)
        PublicOverheadMessage(MessageType.Regular, 41, false, text);
    }

    public void DebugSay(string format, params object[] args)
    {
      if (Debug)
        PublicOverheadMessage(MessageType.Regular, 41, false, string.Format(format, args));
    }

    /*
     * This function can be overridden.. so a "Strongest" mobile, can have a different definition depending
     * on who check for value
     * -Could add a FightMode.Preferred
     *
     */

    public virtual double GetFightModeRanking(Mobile m, FightMode acqType, bool bPlayerOnly)
    {
      if (bPlayerOnly && !m.Player)
        return double.MinValue;

      switch (acqType)
      {
        case FightMode.Strongest:
          return m.Skills.Tactics.Value + m.Str; //returns strongest mobile

        case FightMode.Weakest:
          return -m.Hits; // returns weakest mobile

        default:
          return -GetDistanceToSqrt(m); // returns closest mobile
      }
    }

    // Turn, - for left, + for right
    // Basic for now, needs work
    public virtual void Turn(int iTurnSteps)
    {
      int v = (int)Direction;

      Direction = (Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80));
    }

    public virtual void TurnInternal(int iTurnSteps)
    {
      int v = (int)Direction;

      SetDirection((Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80)));
    }

    public bool IsHurt()
    {
      return Hits != HitsMax;
    }

    public double GetHomeDistance()
    {
      return GetDistanceToSqrt(m_Home);
    }

    public virtual int GetTeamSize(int iRange)
    {
      int iCount = 0;

      foreach (Mobile m in GetMobilesInRange(iRange))
        if (m != this && m is BaseCreature creature && !creature.Deleted && creature.Team == Team &&
            CanSee(creature))
          iCount++;

      return iCount;
    }

    public override void AggressiveAction(Mobile aggressor, bool criminal)
    {
      base.AggressiveAction(aggressor, criminal);

      if (ControlMaster != null && NotorietyHandlers.CheckAggressor(ControlMaster.Aggressors, aggressor))
        aggressor.Aggressors.Add(AggressorInfo.Create(this, aggressor, true));

      OrderType ct = m_ControlOrder;

      if (AIObject != null)
      {
        if (!Core.ML || ct != OrderType.Follow && ct != OrderType.Stop && ct != OrderType.Stay)
        {
          AIObject.OnAggressiveAction(aggressor);
        }
        else
        {
          DebugSay("I'm being attacked but my master told me not to fight.");
          Warmode = false;
          return;
        }
      }

      StopFlee();

      ForceReacquire();

      if (!IsEnemy(aggressor))
      {
        Player pl = Ethics.Player.Find(aggressor, true);

        if (pl?.IsShielded == true)
          pl.FinishShield();
      }

      if (aggressor.ChangingCombatant && (m_Controlled || m_bSummoned) &&
          (ct == OrderType.Come || !Core.ML && ct == OrderType.Stay || ct == OrderType.Stop || ct == OrderType.None ||
           ct == OrderType.Follow))
      {
        ControlTarget = aggressor;
        ControlOrder = OrderType.Attack;
      }
      else if (Combatant == null && !BardPacified)
      {
        Warmode = true;
        Combatant = aggressor;
      }
    }

    public override bool OnMoveOver(Mobile m)
    {
      if (m is BaseCreature creature && !creature.Controlled)
        return !Alive || !creature.Alive || IsDeadBondedPet || creature.IsDeadBondedPet ||
               Hidden && AccessLevel > AccessLevel.Player;

      #region Dueling

      if (Region.IsPartOf<SafeZone>() && m is PlayerMobile pm &&
          (pm.DuelContext?.Started != true || pm.DuelContext.Finished ||
          pm.DuelPlayer?.Eliminated != false))
        return true;

      #endregion

      return base.OnMoveOver(m);
    }

    public virtual void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
    {
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
      base.GetContextMenuEntries(from, list);

      if (Commandable)
        AIObject?.GetContextMenuEntries(from, list);

      if (m_bTamable && !m_Controlled && from.Alive)
        list.Add(new TameEntry(from, this));

      AddCustomContextEntries(from, list);

      if (CanTeach && from.Alive)
      {
        Skills ourSkills = Skills;
        Skills theirSkills = from.Skills;

        for (int i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
        {
          Skill skill = ourSkills[i];
          Skill theirSkill = theirSkills[i];

          if (skill?.Base >= 60.0 && CheckTeach(skill.SkillName, from))
          {
            int toTeach = skill.BaseFixedPoint / 3;

            if (toTeach > 420)
              toTeach = 420;

            list.Add(new TeachEntry((SkillName)i, this, from, toTeach > theirSkill.BaseFixedPoint));
          }
        }
      }
    }

    public override bool HandlesOnSpeech(Mobile from)
    {
      return (SpeechType?.Flags & IHSFlags.OnSpeech) != 0 && from.InRange(this, 3) ||
             AIObject?.HandlesOnSpeech(from) == true && from.InRange(this, RangePerception);
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
      InhumanSpeech speechType = SpeechType;

      if (speechType?.OnSpeech(this, e.Mobile, e.Speech) == true)
        e.Handled = true;
      else if (!e.Handled && AIObject != null && e.Mobile.InRange(this, RangePerception))
        AIObject.OnSpeech(e);
    }

    public override bool IsHarmfulCriminal(Mobile target)
    {
      return (!Controlled || target != m_ControlMaster) && (!Summoned || target != m_SummonMaster) &&
             (!(target is BaseCreature creature) || !creature.InitialInnocent || creature.Controlled) &&
             (!(target is PlayerMobile mobile) || mobile.PermaFlags.Count <= 0) && base.IsHarmfulCriminal(target);
    }

    public override void CriminalAction(bool message)
    {
      base.CriminalAction(message);

      if (Controlled || Summoned)
      {
        if (m_ControlMaster != null && m_ControlMaster.Player)
          m_ControlMaster.CriminalAction(false);
        else if (m_SummonMaster != null && m_SummonMaster.Player)
          m_SummonMaster.CriminalAction(false);
      }
    }

    public override void DoHarmful(Mobile target, bool indirect)
    {
      base.DoHarmful(target, indirect);

      if (target == this || target == m_ControlMaster || target == m_SummonMaster || !Controlled && !Summoned)
        return;

      List<AggressorInfo> list = Aggressors;

      for (int i = 0; i < list.Count; ++i)
      {
        AggressorInfo ai = list[i];

        if (ai.Attacker == target)
          return;
      }

      list = Aggressed;

      for (int i = 0; i < list.Count; ++i)
      {
        AggressorInfo ai = list[i];

        if (ai.Defender == target)
        {
          if (m_ControlMaster != null && m_ControlMaster.Player && m_ControlMaster.CanBeHarmful(target, false))
            m_ControlMaster.DoHarmful(target, true);
          else if (m_SummonMaster != null && m_SummonMaster.Player && m_SummonMaster.CanBeHarmful(target, false))
            m_SummonMaster.DoHarmful(target, true);

          return;
        }
      }
    }

    public void ReleaseGuardDupeLock()
    {
      m_NoDupeGuards = null;
    }

    public void ReleaseGuardLock()
    {
      EndAction<GuardedRegion>();
    }

    public virtual bool CheckIdle()
    {
      if (Combatant != null)
        return false; // in combat.. not idling

      if (m_IdleReleaseTime > DateTime.MinValue)
      {
        // idling...

        if (DateTime.UtcNow >= m_IdleReleaseTime)
        {
          m_IdleReleaseTime = DateTime.MinValue;
          return false; // idle is over
        }

        return true; // still idling
      }

      if (95 > Utility.Random(100))
        return false; // not idling, but don't want to enter idle state

      m_IdleReleaseTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(15, 25));

      if (Body.IsHuman)
        switch (Utility.Random(2))
        {
          case 0:
            CheckedAnimate(5, 5, 1, true, true, 1);
            break;
          case 1:
            CheckedAnimate(6, 5, 1, true, false, 1);
            break;
        }
      else if (Body.IsAnimal)
        switch (Utility.Random(3))
        {
          case 0:
            CheckedAnimate(3, 3, 1, true, false, 1);
            break;
          case 1:
            CheckedAnimate(9, 5, 1, true, false, 1);
            break;
          case 2:
            CheckedAnimate(10, 5, 1, true, false, 1);
            break;
        }
      else if (Body.IsMonster)
        switch (Utility.Random(2))
        {
          case 0:
            CheckedAnimate(17, 5, 1, true, false, 1);
            break;
          case 1:
            CheckedAnimate(18, 5, 1, true, false, 1);
            break;
        }

      PlaySound(GetIdleSound());
      return true; // entered idle state
    }

    /*
      this way, due to the huge number of locations this will have to be changed
      Perhaps we can change this in the future when fixing game play is not the
      major issue.
    */

    public virtual void CheckedAnimate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
    {
      if (!Mounted)
        base.Animate(action, frameCount, repeatCount, forward, repeat, delay);
    }

    private void CheckAIActive()
    {
      Map map = Map;

      if (PlayerRangeSensitive && AIObject != null && map?.GetSector(Location).Active == true)
        AIObject.Activate();
    }

    public override void OnCombatantChange()
    {
      base.OnCombatantChange();

      Warmode = Combatant?.Deleted == false && Combatant.Alive;

      if (CanFly && Warmode)
        Flying = false;
    }

    protected override void OnMapChange(Map oldMap)
    {
      CheckAIActive();

      base.OnMapChange(oldMap);
    }

    protected override void OnLocationChange(Point3D oldLocation)
    {
      CheckAIActive();

      base.OnLocationChange(oldLocation);
    }

    public virtual void ForceReacquire()
    {
      NextReacquireTime = Core.TickCount;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
      if (AcquireOnApproach && !Controlled && !Summoned && FightMode != FightMode.Aggressor)
      {
        if (InRange(m.Location, AcquireOnApproachRange) && !InRange(oldLocation, AcquireOnApproachRange) &&
            CanBeHarmful(m) && IsEnemy(m))
        {
          Combatant = FocusMob = m;
          AIObject?.MoveTo(m, true, 1);
          DoHarmful(m);
        }
      }
      else if (ReacquireOnMovement)
      {
        ForceReacquire();
      }

      InhumanSpeech speechType = SpeechType;

      speechType?.OnMovement(this, m, oldLocation);

      /* Begin notice sound */
      if ((!m.Hidden || m.AccessLevel == AccessLevel.Player) && m.Player && FightMode != FightMode.Aggressor &&
          FightMode != FightMode.None && Combatant == null && !Controlled && !Summoned &&
          InRange(m.Location, 18) && !InRange(oldLocation, 18))
      {
        if (Body.IsMonster)
          Animate(11, 5, 1, true, false, 1);

        PlaySound(GetAngerSound());
      }
      /* End notice sound */

      if (MLQuestSystem.Enabled && CanShout && m is PlayerMobile mobile)
        CheckShout(mobile, oldLocation);

      if (m_NoDupeGuards == m)
        return;

      if (!Body.IsHuman || Kills >= 5 || AlwaysMurderer || AlwaysAttackable || m.Kills < 5 ||
          !m.InRange(Location, 12) || !m.Alive)
        return;

      GuardedRegion guardedRegion = Region.GetRegion<GuardedRegion>();

      if (guardedRegion?.IsDisabled() == false && guardedRegion.IsGuardCandidate(m) && BeginAction<GuardedRegion>())
      {
        Say(1013037 + Utility.Random(16));
        guardedRegion.CallGuards(Location);

        Timer.DelayCall(TimeSpan.FromSeconds(5.0), ReleaseGuardLock);

        m_NoDupeGuards = m;
        Timer.DelayCall(TimeSpan.Zero, ReleaseGuardDupeLock);
      }
    }

    public void AddSpellAttack(Type type)
    {
      m_SpellAttack.Add(type);
    }

    public void AddSpellDefense(Type type)
    {
      m_SpellDefense.Add(type);
    }

    public Spell GetAttackSpellRandom()
    {
      if (m_SpellAttack.Count == 0)
        return null;

      Type type = m_SpellAttack[Utility.Random(m_SpellAttack.Count)];
      return Activator.CreateInstance(type, this, null) as Spell;
    }

    public Spell GetDefenseSpellRandom()
    {
      if (m_SpellDefense.Count == 0)
        return null;

      Type type = m_SpellDefense[Utility.Random(m_SpellDefense.Count)];
      return Activator.CreateInstance(type, this, null) as Spell;
    }

    public Spell GetSpellSpecific(Type type)
    {
      int i;

      for (i = 0; i < m_SpellAttack.Count; i++)
        if (m_SpellAttack[i] == type)
          return Activator.CreateInstance(type, this, null) as Spell;

      for (i = 0; i < m_SpellDefense.Count; i++)
        if (m_SpellDefense[i] == type)
          return Activator.CreateInstance(type, this, null) as Spell;

      return null;
    }

    public static void Cap(ref int val, int min, int max)
    {
      if (val < min)
        val = min;
      else if (val > max)
        val = max;
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (from.AccessLevel >= AccessLevel.GameMaster && !Body.IsHuman)
      {
        Container pack = Backpack;

        pack?.DisplayTo(from);
      }

      if (DeathAdderCharmable && from.CanBeHarmful(this, false))
        if (SummonFamiliarSpell.Table.TryGetValue(from, out BaseCreature bc) && (bc as DeathAdder)?.Deleted == false)
        {
          from.SendAsciiMessage("You charm the snake.  Select a target to attack.");
          from.Target = new DeathAdderCharmTarget(this);
        }

      if (MLQuestSystem.Enabled && CanGiveMLQuest && from is PlayerMobile mobile)
        MLQuestSystem.OnDoubleClick(this, mobile);

      base.OnDoubleClick(from);
    }

    public override void AddNameProperties(ObjectPropertyList list)
    {
      base.AddNameProperties(list);

      if (MLQuestSystem.Enabled && CanGiveMLQuest)
        list.Add(1072269); // Quest Giver

      if (Core.ML)
      {
        if (DisplayWeight)
          list.Add(TotalWeight == 1 ? 1072788 : 1072789, TotalWeight.ToString()); // Weight: ~1_WEIGHT~ stones

        if (m_ControlOrder == OrderType.Guard)
          list.Add(1080078); // guarding
      }

      if (Summoned && !(IsAnimatedDead || IsNecroFamiliar || this is Clone))
      {
        list.Add(1049646); // (summoned)
      }
      else if (Controlled && Commandable)
      {
        if (IsBonded) //Intentional difference (showing ONLY bonded when bonded instead of bonded & tame)
          list.Add(1049608); // (bonded)
        else
          list.Add(502006); // (tame)
      }
    }

    public override void OnSingleClick(Mobile from)
    {
      if (Controlled && Commandable)
      {
        int number;

        if (Summoned)
          number = 1049646; // (summoned)
        else if (IsBonded)
          number = 1049608; // (bonded)
        else
          number = 502006; // (tame)

        PrivateOverheadMessage(MessageType.Regular, 0x3B2, number, from.NetState);
      }

      base.OnSingleClick(from);
    }

    public override bool OnBeforeDeath()
    {
      int treasureLevel = TreasureMapLevel;

      if (treasureLevel == 1 && Map == Map.Trammel && TreasureMap.IsInHavenIsland(this))
      {
        Mobile killer = LastKiller;

        if (killer is BaseCreature bc)
          killer = bc.GetMaster();

        if (killer is PlayerMobile mobile && mobile.Young)
          treasureLevel = 0;
      }

      if (!Summoned && !NoKillAwards && !IsBonded)
      {
        if (treasureLevel >= 0)
        {
          if (m_Paragon && Paragon.ChestChance > Utility.RandomDouble())
            PackItem(new ParagonChest(Name, treasureLevel));
          else if ((Map == Map.Felucca || Map == Map.Trammel) && TreasureMap.LootChance >= Utility.RandomDouble())
            PackItem(new TreasureMap(treasureLevel, Map));
        }

        if (m_Paragon && Paragon.ChocolateIngredientChance > Utility.RandomDouble())
          switch (Utility.Random(4))
          {
            case 0:
              PackItem(new CocoaButter());
              break;
            case 1:
              PackItem(new CocoaLiquor());
              break;
            case 2:
              PackItem(new SackOfSugar());
              break;
            case 3:
              PackItem(new Vanilla());
              break;
          }
      }

      if (!Summoned && !NoKillAwards && !m_HasGeneratedLoot)
      {
        m_HasGeneratedLoot = true;
        GenerateLoot(false);
      }

      if (!NoKillAwards && Region.IsPartOf("Doom"))
      {
        int bones = TheSummoningQuest.GetDaemonBonesFor(this);

        if (bones > 0)
          PackItem(new DaemonBone(bones));
      }

      if (IsAnimatedDead)
        Effects.SendLocationEffect(Location, Map, 0x3728, 13, 1, 0x461, 4);

      InhumanSpeech speechType = SpeechType;
      speechType?.OnDeath(this);
      ReceivedHonorContext?.OnTargetKilled();

      return base.OnBeforeDeath();
    }

    public int ComputeBonusDamage(List<DamageEntry> list, Mobile m)
    {
      int bonus = 0;

      for (int i = list.Count - 1; i >= 0; --i)
      {
        DamageEntry de = list[i];

        if (de.Damager == m || !(de.Damager is BaseCreature bc))
          continue;

        if (bc.GetMaster() == m)
          bonus += de.DamageGiven;
      }

      return bonus;
    }

    public Mobile GetMaster()
    {
      if (Controlled && ControlMaster != null)
        return ControlMaster;
      if (Summoned && SummonMaster != null)
        return SummonMaster;

      return null;
    }

    public static List<DamageStore> GetLootingRights(List<DamageEntry> damageEntries, int hitsMax)
    {
      List<DamageStore> rights = new List<DamageStore>();

      for (int i = damageEntries.Count - 1; i >= 0; --i)
      {
        if (i >= damageEntries.Count)
          continue;

        DamageEntry de = damageEntries[i];

        if (de.HasExpired)
        {
          damageEntries.RemoveAt(i);
          continue;
        }

        int damage = de.DamageGiven;

        List<DamageEntry> respList = de.Responsible;

        for (int j = 0; j < respList?.Count; ++j)
        {
          DamageEntry subEntry = respList[j];
          Mobile master = subEntry.Damager;

          if (master?.Deleted != false || !master.Player)
            continue;

          bool needNewSubEntry = true;

          for (int k = 0; needNewSubEntry && k < rights.Count; ++k)
          {
            DamageStore ds = rights[k];

            if (ds.m_Mobile == master)
            {
              ds.m_Damage += subEntry.DamageGiven;
              needNewSubEntry = false;
            }
          }

          if (needNewSubEntry)
            rights.Add(new DamageStore(master, subEntry.DamageGiven));

          damage -= subEntry.DamageGiven;
        }

        Mobile m = de.Damager;

        if (m?.Deleted != false || !m.Player)
          continue;

        if (damage <= 0)
          continue;

        bool needNewEntry = true;

        for (int j = 0; needNewEntry && j < rights.Count; ++j)
        {
          DamageStore ds = rights[j];

          if (ds.m_Mobile == m)
          {
            ds.m_Damage += damage;
            needNewEntry = false;
          }
        }

        if (needNewEntry)
          rights.Add(new DamageStore(m, damage));
      }

      if (rights.Count > 0)
      {
        rights[0].m_Damage =
          (int)(rights[0].m_Damage *
                1.25
          ); //This would be the first valid person attacking it.  Gets a 25% bonus.  Per 1/19/07 Five on Friday

        if (rights.Count > 1)
          rights.Sort(); //Sort by damage

        int topDamage = rights[0].m_Damage;
        int minDamage;

        if (hitsMax >= 3000)
          minDamage = topDamage / 16;
        else if (hitsMax >= 1000)
          minDamage = topDamage / 8;
        else if (hitsMax >= 200)
          minDamage = topDamage / 4;
        else
          minDamage = topDamage / 2;

        for (int i = 0; i < rights.Count; ++i)
        {
          DamageStore ds = rights[i];

          ds.m_HasRight = ds.m_Damage >= minDamage;
        }
      }

      return rights;
    }

    public virtual void OnKilledBy(Mobile mob)
    {
      #region Mondain's Legacy

      if (GivesMLMinorArtifact)
      {
        if (MondainsLegacy.CheckArtifactChance(mob, this))
          MondainsLegacy.GiveArtifactTo(mob);
      }

      #endregion

      else if (m_Paragon)
      {
        if (Paragon.CheckArtifactChance(mob, this))
          Paragon.GiveArtifactTo(mob);
      }
    }

    public override void OnDeath(Container c)
    {
      MeerMage.StopEffect(this, false);

      if (IsBonded)
      {
        int sound = GetDeathSound();

        if (sound >= 0)
          Effects.PlaySound(this, Map, sound);

        Warmode = false;

        Poison = null;
        Combatant = null;

        Hits = 0;
        Stam = 0;
        Mana = 0;

        IsDeadPet = true;
        ControlTarget = ControlMaster;
        ControlOrder = OrderType.Follow;

        ProcessDeltaQueue();
        SendIncomingPacket();
        SendIncomingPacket();

        // TODO: This can be done in Parallel if there are lots of them.
        List<AggressorInfo> aggressors = Aggressors;

        for (int i = 0; i < aggressors.Count; ++i)
        {
          AggressorInfo info = aggressors[i];

          if (info.Attacker.Combatant == this)
            info.Attacker.Combatant = null;
        }

        List<AggressorInfo> aggressed = Aggressed;

        for (int i = 0; i < aggressed.Count; ++i)
        {
          AggressorInfo info = aggressed[i];

          if (info.Defender.Combatant == this)
            info.Defender.Combatant = null;
        }

        Mobile owner = ControlMaster;

        if (owner?.Deleted != false || owner.Map != Map || !owner.InRange(this, 12) || !CanSee(owner) ||
            !InLOS(owner))
        {
          if (OwnerAbandonTime == DateTime.MinValue)
            OwnerAbandonTime = DateTime.UtcNow;
        }
        else
        {
          OwnerAbandonTime = DateTime.MinValue;
        }

        GiftOfLifeSpell.HandleDeath(this);

        CheckStatTimers();
      }
      else
      {
        if (!Summoned && !NoKillAwards)
        {
          int totalFame = Fame / 100;
          int totalKarma = -Karma / 100;

          if (Map == Map.Felucca)
          {
            totalFame += totalFame / 10 * 3;
            totalKarma += totalKarma / 10 * 3;
          }

          List<DamageStore> list = GetLootingRights(DamageEntries, HitsMax);
          List<Mobile> titles = new List<Mobile>();
          List<int> fame = new List<int>();
          List<int> karma = new List<int>();

          bool givenQuestKill = false;
          bool givenFactionKill = false;
          bool givenToTKill = false;

          for (int i = 0; i < list.Count; ++i)
          {
            DamageStore ds = list[i];

            if (!ds.m_HasRight)
              continue;

            Party party = Engines.PartySystem.Party.Get(ds.m_Mobile);

            if (party != null)
            {
              int divedFame = totalFame / party.Members.Count;
              int divedKarma = totalKarma / party.Members.Count;

              for (int j = 0; j < party.Members.Count; ++j)
              {
                PartyMemberInfo info = party.Members[j];

                if (info?.Mobile != null)
                {
                  int index = titles.IndexOf(info.Mobile);

                  if (index == -1)
                  {
                    titles.Add(info.Mobile);
                    fame.Add(divedFame);
                    karma.Add(divedKarma);
                  }
                  else
                  {
                    fame[index] += divedFame;
                    karma[index] += divedKarma;
                  }
                }
              }
            }
            else
            {
              titles.Add(ds.m_Mobile);
              fame.Add(totalFame);
              karma.Add(totalKarma);
            }

            OnKilledBy(ds.m_Mobile);

            if (!givenFactionKill)
            {
              givenFactionKill = true;
              Faction.HandleDeath(this, ds.m_Mobile);
            }

            Region region = ds.m_Mobile.Region;

            if (!givenToTKill && (Map == Map.Tokuno || region.IsPartOf("Yomotsu Mines") ||
                                  region.IsPartOf("Fan Dancer's Dojo")))
            {
              givenToTKill = true;
              TreasuresOfTokuno.HandleKill(this, ds.m_Mobile);
            }

            if (ds.m_Mobile is PlayerMobile pm)
            {
              if (MLQuestSystem.Enabled) MLQuestSystem.HandleKill(pm, this);

              if (givenQuestKill)
                continue;

              QuestSystem qs = pm.Quest;

              if (qs != null)
              {
                qs.OnKill(this, c);
                givenQuestKill = true;
              }
            }
          }

          for (int i = 0; i < titles.Count; ++i)
          {
            Titles.AwardFame(titles[i], fame[i], true);
            Titles.AwardKarma(titles[i], karma[i], true);
          }
        }

        base.OnDeath(c);

        if (DeleteCorpseOnDeath)
          c.Delete();
      }
    }

    public override void OnDelete()
    {
      Mobile m = m_ControlMaster;
      SetControlMaster(null);

      SummonMaster = null;
      ReceivedHonorContext?.Cancel();
      base.OnDelete();
      m?.InvalidateProperties();
    }

    public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
    {
      if (target is BaseFactionGuard)
        return false;

      if (target is BaseCreature creature && creature.IsInvulnerable || target is PlayerVendor || target is TownCrier)
      {
        if (message)
        {
          if (target.Title == null)
            SendMessage("{0} cannot be harmed.", target.Name);
          else
            SendMessage("{0} {1} cannot be harmed.", target.Name, target.Title);
        }

        return false;
      }

      return base.CanBeHarmful(target, message, ignoreOurBlessedness);
    }

    public override bool CanBeRenamedBy(Mobile from)
    {
      return Controlled && from == ControlMaster && !from.Region.IsPartOf<Jail>() ||
             base.CanBeRenamedBy(from);
    }

    public bool SetControlMaster(Mobile m)
    {
      if (m == null)
      {
        ControlMaster = null;
        Controlled = false;
        ControlTarget = null;
        ControlOrder = OrderType.None;
        Guild = null;

        Delta(MobileDelta.Noto);
      }
      else
      {
        ISpawner se = Spawner;
        if (se?.UnlinkOnTaming == true)
        {
          Spawner.Remove(this);
          Spawner = null;
        }

        if (m.Followers + ControlSlots > m.FollowersMax)
        {
          m.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
          return false;
        }

        CurrentWayPoint = null; //so tamed animals don't try to go back

        Home = Point3D.Zero;

        ControlMaster = m;
        Controlled = true;
        ControlTarget = null;
        ControlOrder = OrderType.Come;
        Guild = null;

        if (m_DeleteTimer != null)
        {
          m_DeleteTimer.Stop();
          m_DeleteTimer = null;
        }

        Delta(MobileDelta.Noto);
      }

      InvalidateProperties();

      return true;
    }

    public override void OnRegionChange(Region Old, Region New)
    {
      base.OnRegionChange(Old, New);

      if (Controlled && Spawner is SpawnEntry se && !se.UnlinkOnTaming && (New == null || !New.AcceptsSpawnsFrom(se.Region)))
      {
        Spawner.Remove(this);
        Spawner = null;
      }
    }

    public static bool Summon(BaseCreature creature, Mobile caster, Point3D p, int sound, TimeSpan duration)
    {
      return Summon(creature, true, caster, p, sound, duration);
    }

    public static bool Summon(BaseCreature creature, bool controlled, Mobile caster, Point3D p, int sound,
      TimeSpan duration)
    {
      if (caster.Followers + creature.ControlSlots > caster.FollowersMax)
      {
        caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
        creature.Delete();
        return false;
      }

      Summoning = true;

      if (controlled)
        creature.SetControlMaster(caster);

      creature.RangeHome = 10;
      creature.Summoned = true;

      creature.SummonMaster = caster;

      Container pack = creature.Backpack;

      if (pack != null)
        for (int i = pack.Items.Count - 1; i >= 0; --i)
        {
          if (i >= pack.Items.Count)
            continue;

          pack.Items[i].Delete();
        }

      new UnsummonTimer(caster, creature, duration).Start();
      creature.SummonEnd = DateTime.UtcNow + duration;

      creature.MoveToWorld(p, caster.Map);

      Effects.PlaySound(p, creature.Map, sound);

      Summoning = false;

      return true;
    }

    public virtual void OnThink()
    {
      long tc = Core.TickCount;

      if (EnableRummaging && CanRummageCorpses && !Summoned && !Controlled && tc - m_NextRummageTime >= 0)
      {
        double min, max;

        if (ChanceToRummage > Utility.RandomDouble() && Rummage())
        {
          min = MinutesToNextRummageMin;
          max = MinutesToNextRummageMax;
        }
        else
        {
          min = MinutesToNextChanceMin;
          max = MinutesToNextChanceMax;
        }

        double delay = min + Utility.RandomDouble() * (max - min);
        m_NextRummageTime = tc + (int)TimeSpan.FromMinutes(delay).TotalMilliseconds;
      }

      if (CanBreath && tc - m_NextBreathTime >= 0
      ) // tested: controlled dragons do breath fire, what about summoned skeletal dragons?
      {
        Mobile target = Combatant;

        if (target?.Alive == true && !target.IsDeadBondedPet && CanBeHarmful(target) && target.Map == Map &&
            !IsDeadBondedPet && target.InRange(this, BreathRange) && InLOS(target) && !BardPacified)
        {
          if (Core.TickCount - m_NextBreathTime < 30000 && Utility.RandomBool()) BreathStart(target);

          m_NextBreathTime = tc + (int)TimeSpan.FromSeconds(BreathMinDelay + Utility.RandomDouble() * (BreathMaxDelay - BreathMinDelay)).TotalMilliseconds;
        }
      }

      if ((CanHeal || CanHealOwner) && Alive && !IsHealing && !BardPacified)
      {
        Mobile owner = ControlMaster;

        if (owner != null && CanHealOwner && tc - m_NextHealOwnerTime >= 0 && CanBeBeneficial(owner, true, true) &&
            owner.Map == Map && InRange(owner, HealStartRange) && InLOS(owner) &&
            owner.Hits < HealOwnerTrigger * owner.HitsMax)
        {
          HealStart(owner);

          m_NextHealOwnerTime = tc + (int)TimeSpan.FromSeconds(HealOwnerInterval).TotalMilliseconds;
        }
        else if (CanHeal && tc - m_NextHealTime >= 0 && CanBeBeneficial(this) &&
                 (Hits < HealTrigger * HitsMax || Poisoned))
        {
          HealStart(this);

          m_NextHealTime = tc + (int)TimeSpan.FromSeconds(HealInterval).TotalMilliseconds;
        }
      }

      if (ReturnsToHome && IsSpawnerBound() && !InRange(Home, RangeHome))
      {
        if (Combatant == null && Warmode == false && Utility.RandomDouble() < .10) /* some throttling */
        {
          m_FailedReturnHome = !Move(GetDirectionTo(Home.X, Home.Y)) ? m_FailedReturnHome + 1 : 0;

          if (m_FailedReturnHome > 5)
          {
            SetLocation(Home, true);

            m_FailedReturnHome = 0;
          }
        }
      }
      else
      {
        m_FailedReturnHome = 0;
      }

      if (HasAura && tc - m_NextAura >= 0)
      {
        AuraDamage();
        m_NextAura = tc + (int)AuraInterval.TotalMilliseconds;
      }
    }

    public virtual bool Rummage()
    {
      IPooledEnumerable<Corpse> eable = GetItemsInRange<Corpse>(2);
      Corpse toRummage = eable.FirstOrDefault(item => item.Items.Count > 0);

      eable.Free();

      if (toRummage == null)
        return false;

      if (Backpack == null)
        return false;

      List<Item> items = toRummage.Items;

      for (int i = 0; i < items.Count; ++i)
      {
        Item item = items[Utility.Random(items.Count)];

        Lift(item, item.Amount, out bool rejected, out LRReason _);

        if (!rejected && Drop(this, new Point3D(-1, -1, 0)))
        {
          // *rummages through a corpse and takes an item*
          PublicOverheadMessage(MessageType.Emote, 0x3B2, 1008086);
          //TODO: Instancing of Rummaged stuff.
          return true;
        }
      }

      return false;
    }

    public void Pacify(Mobile master, DateTime endtime)
    {
      BardPacified = true;
      BardEndTime = endtime;
    }

    public override Mobile GetDamageMaster(Mobile damagee)
    {
      if (BardProvoked && damagee == BardTarget)
        return BardMaster;
      if (m_Controlled && m_ControlMaster != null)
        return m_ControlMaster;
      if (m_bSummoned && m_SummonMaster != null)
        return m_SummonMaster;

      return base.GetDamageMaster(damagee);
    }

    public void Provoke(Mobile master, Mobile target, bool bSuccess)
    {
      BardProvoked = true;

      if (!Core.ML) PublicOverheadMessage(MessageType.Emote, EmoteHue, false, "*looks furious*");

      if (bSuccess)
      {
        PlaySound(GetIdleSound());

        BardMaster = master;
        BardTarget = target;
        Combatant = target;
        BardEndTime = DateTime.UtcNow + TimeSpan.FromSeconds(30.0);

        if (target is BaseCreature t)
        {
          if (t.Unprovokable || t.IsParagon && BaseInstrument.GetBaseDifficulty(t) >= 160.0)
            return;

          t.BardProvoked = true;

          t.BardMaster = master;
          t.BardTarget = this;
          t.Combatant = this;
          t.BardEndTime = DateTime.UtcNow + TimeSpan.FromSeconds(30.0);
        }
      }
      else
      {
        PlaySound(GetAngerSound());

        BardMaster = master;
        BardTarget = target;
      }
    }

    public bool FindMyName(string str, bool bWithAll)
    {
      string name = Name;

      if (name == null || str.Length < name.Length)
        return false;

      string[] wordsString = str.Split(' ');
      string[] wordsName = name.Split(' ');

      for (int j = 0; j < wordsName.Length; j++)
      {
        string wordName = wordsName[j];

        bool bFound = false;
        for (int i = 0; i < wordsString.Length; i++)
        {
          string word = wordsString[i];

          if (Insensitive.Equals(word, wordName))
            bFound = true;

          if (bWithAll && Insensitive.Equals(word, "all"))
            return true;
        }

        if (!bFound)
          return false;
      }

      return true;
    }

    public static void TeleportPets(Mobile master, Point3D loc, Map map, bool onlyBonded = false)
    {
      List<Mobile> move = new List<Mobile>();

      foreach (Mobile m in master.GetMobilesInRange(3))
        if (m is BaseCreature pet)
          if (pet.Controlled && pet.ControlMaster == master && !onlyBonded || pet.IsBonded)
            if (pet.ControlOrder == OrderType.Guard || pet.ControlOrder == OrderType.Follow ||
                pet.ControlOrder == OrderType.Come)
              move.Add(pet);

      foreach (Mobile m in move)
        m.MoveToWorld(loc, map);
    }

    public virtual void ResurrectPet()
    {
      if (!IsDeadPet)
        return;

      OnBeforeResurrect();

      Poison = null;

      Warmode = false;

      Hits = 10;
      Stam = StamMax;
      Mana = 0;

      ProcessDeltaQueue();

      IsDeadPet = false;

      Effects.SendPacket(Location, Map, new BondedStatus(0, Serial, 0));

      SendIncomingPacket();
      SendIncomingPacket();

      OnAfterResurrect();

      Mobile owner = ControlMaster;

      if (owner?.Deleted != false || owner.Map != Map || !owner.InRange(this, 12) || !CanSee(owner) ||
          !InLOS(owner))
      {
        if (OwnerAbandonTime == DateTime.MinValue)
          OwnerAbandonTime = DateTime.UtcNow;
      }
      else
      {
        OwnerAbandonTime = DateTime.MinValue;
      }

      CheckStatTimers();
    }

    public override bool CanBeDamaged()
    {
      if (IsDeadPet || IsInvulnerable)
        return false;

      return base.CanBeDamaged();
    }

    private bool IsSpawnerBound()
    {
      return Map != null && Map != Map.Internal &&
             FightMode != FightMode.None && RangeHome >= 0 &&
             !Controlled && !Summoned && Spawner is Spawner spawner && spawner.Map == Map;
    }

    public override void OnSectorDeactivate()
    {
      if (!Deleted && ReturnsToHome && IsSpawnerBound() && !InRange(Home, RangeHome + 5))
      {
        Timer.DelayCall(TimeSpan.FromSeconds(Utility.Random(45) + 15), GoHome_Callback);

        m_ReturnQueued = true;
      }
      else if (PlayerRangeSensitive)
      {
        AIObject?.Deactivate();
      }

      base.OnSectorDeactivate();
    }

    public void GoHome_Callback()
    {
      if (m_ReturnQueued && IsSpawnerBound())
        if (!Map.GetSector(X, Y).Active)
        {
          SetLocation(Home, true);

          if (!Map.GetSector(X, Y).Active) AIObject?.Deactivate();
        }

      m_ReturnQueued = false;
    }

    public override void OnSectorActivate()
    {
      if (PlayerRangeSensitive) AIObject?.Activate();

      base.OnSectorActivate();
    }

    private class TameEntry : ContextMenuEntry
    {
      private BaseCreature m_Mobile;

      public TameEntry(Mobile from, BaseCreature creature) : base(6130, 6)
      {
        m_Mobile = creature;

        Enabled = Enabled && (from.Female ? creature.AllowFemaleTamer : creature.AllowMaleTamer);
      }

      public override void OnClick()
      {
        if (!Owner.From.CheckAlive())
          return;

        Owner.From.TargetLocked = true;
        AnimalTaming.DisableMessage = true;

        if (Owner.From.UseSkill(SkillName.AnimalTaming))
          Owner.From.Target.Invoke(Owner.From, m_Mobile);

        AnimalTaming.DisableMessage = false;
        Owner.From.TargetLocked = false;
      }
    }

    private class DeathAdderCharmTarget : Target
    {
      private BaseCreature m_Charmed;

      public DeathAdderCharmTarget(BaseCreature charmed) : base(-1, false, TargetFlags.Harmful)
      {
        m_Charmed = charmed;
      }

      protected override void OnTarget(Mobile from, object targeted)
      {
        if (!m_Charmed.DeathAdderCharmable || m_Charmed.Combatant != null || !from.CanBeHarmful(m_Charmed, false))
          return;

        if (!(SummonFamiliarSpell.Table.TryGetValue(from, out BaseCreature bc) && (bc as DeathAdder)?.Deleted == false))
          return;

        if (!(targeted is Mobile targ && from.CanBeHarmful(targ, false)))
          return;

        from.RevealingAction();
        from.DoHarmful(targ, true);

        m_Charmed.Combatant = targ;

        if (m_Charmed.AIObject != null)
          m_Charmed.AIObject.Action = ActionType.Combat;
      }
    }

    #region Var declarations

    private AIType m_CurrentAI; // The current AI
    private AIType m_DefaultAI; // The default AI

    private int m_Team; // Monster Team

    private double m_CurrentSpeed; // The current speed, lets say it could be changed by something;

    private Point3D m_Home; // The home position of the creature, used by some AI

    private List<Type> m_SpellAttack; // List of attack spell/power
    private List<Type> m_SpellDefense; // List of defensive spell/power

    private bool m_Controlled; // Is controlled
    private Mobile m_ControlMaster; // My master
    private OrderType m_ControlOrder; // My order

    private int m_Loyalty;

    private bool m_bTamable;

    private bool m_bSummoned;

    private Mobile m_SummonMaster;

    private int m_DamageMin = -1;
    private int m_DamageMax = -1;

    private int m_PhysicalResistance;
    private int m_FireResistance;
    private int m_ColdResistance;
    private int m_PoisonResistance;
    private int m_EnergyResistance;

    private bool m_IsStabled;

    private bool m_HasGeneratedLoot; // have we generated our loot yet?

    private bool m_Paragon;

    private int m_FailedReturnHome; /* return to home failure counter */

    #endregion

    #region ML Quest System

    private List<MLQuest> m_MLQuests;

    public List<MLQuest> MLQuests
    {
      get
      {
        if (m_MLQuests == null)
        {
          if (StaticMLQuester)
            m_MLQuests = MLQuestSystem.FindQuestList(GetType());
          else
            m_MLQuests = ConstructQuestList();

          if (m_MLQuests == null)
            return
              MLQuestSystem
                .EmptyList; // return EmptyList, but don't cache it (run construction again next time)
        }

        return m_MLQuests;
      }
    }

    public virtual bool CanGiveMLQuest => MLQuests.Count != 0;
    public virtual bool StaticMLQuester => true;

    protected virtual List<MLQuest> ConstructQuestList()
    {
      return null;
    }

    public virtual bool CanShout => false;

    public const int ShoutRange = 8;
    public static readonly TimeSpan ShoutDelay = TimeSpan.FromMinutes(1);

    private DateTime m_MLNextShout;

    private void CheckShout(PlayerMobile pm, Point3D oldLocation)
    {
      if (m_MLNextShout > DateTime.UtcNow || pm.Hidden || !pm.Alive)
        return;

      int shoutRange = ShoutRange;

      if (!InRange(pm.Location, shoutRange) || InRange(oldLocation, shoutRange) || !CanSee(pm) || !InLOS(pm))
        return;

      MLQuestContext context = MLQuestSystem.GetContext(pm);

      if (context?.IsFull == true)
        return;

      MLQuest quest = MLQuestSystem.RandomStarterQuest(this, pm, context);

      if (quest?.Activated != true || context?.IsDoingQuest(quest) == true)
        return;

      Shout(pm);
      m_MLNextShout = DateTime.UtcNow + ShoutDelay;
    }

    public virtual void Shout(PlayerMobile pm)
    {
    }

    #endregion

    #region Bonding

    public const bool BondingEnabled = true;

    public virtual bool IsBondable => BondingEnabled && !Summoned;
    public virtual TimeSpan BondingDelay => TimeSpan.FromDays(7.0);
    public virtual TimeSpan BondingAbandonDelay => TimeSpan.FromDays(1.0);

    public override bool CanRegenHits => !IsDeadPet && base.CanRegenHits;
    public override bool CanRegenStam => !IsParagon && !IsDeadPet && base.CanRegenStam;
    public override bool CanRegenMana => !IsDeadPet && base.CanRegenMana;

    public override bool IsDeadBondedPet => IsDeadPet;

    private bool m_IsBonded;

    [CommandProperty(AccessLevel.GameMaster)]
    public Spawner MySpawner => Spawner as Spawner;

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile LastOwner
    {
      get
      {
        if (Owners == null || Owners.Count == 0)
          return null;

        return Owners[Owners.Count - 1];
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsBonded
    {
      get => m_IsBonded;
      set
      {
        m_IsBonded = value;
        InvalidateProperties();
      }
    }

    public bool IsDeadPet{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime BondingBegin{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime OwnerAbandonTime{ get; set; }

    #endregion

    #region Delete Previously Tamed Timer

    private DeleteTimer m_DeleteTimer;

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan DeleteTimeLeft
    {
      get
      {
        if (m_DeleteTimer != null && m_DeleteTimer.Running)
          return m_DeleteTimer.Next - DateTime.UtcNow;

        return TimeSpan.Zero;
      }
    }

    private class DeleteTimer : Timer
    {
      private Mobile m;

      public DeleteTimer(Mobile creature, TimeSpan delay) : base(delay)
      {
        m = creature;
        Priority = TimerPriority.OneMinute;
      }

      protected override void OnTick()
      {
        m.Delete();
      }
    }

    public void BeginDeleteTimer()
    {
      if (!(this is BaseEscortable) && !Summoned && !Deleted && !IsStabled)
      {
        StopDeleteTimer();
        m_DeleteTimer = new DeleteTimer(this, TimeSpan.FromDays(3.0));
        m_DeleteTimer.Start();
      }
    }

    public void StopDeleteTimer()
    {
      if (m_DeleteTimer != null)
      {
        m_DeleteTimer.Stop();
        m_DeleteTimer = null;
      }
    }

    #endregion

    #region Elemental Resistance/Damage

    public override int BasePhysicalResistance => m_PhysicalResistance;
    public override int BaseFireResistance => m_FireResistance;
    public override int BaseColdResistance => m_ColdResistance;
    public override int BasePoisonResistance => m_PoisonResistance;
    public override int BaseEnergyResistance => m_EnergyResistance;

    [CommandProperty(AccessLevel.GameMaster)]
    public int PhysicalResistanceSeed
    {
      get => m_PhysicalResistance;
      set
      {
        m_PhysicalResistance = value;
        UpdateResistances();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int FireResistSeed
    {
      get => m_FireResistance;
      set
      {
        m_FireResistance = value;
        UpdateResistances();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ColdResistSeed
    {
      get => m_ColdResistance;
      set
      {
        m_ColdResistance = value;
        UpdateResistances();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int PoisonResistSeed
    {
      get => m_PoisonResistance;
      set
      {
        m_PoisonResistance = value;
        UpdateResistances();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int EnergyResistSeed
    {
      get => m_EnergyResistance;
      set
      {
        m_EnergyResistance = value;
        UpdateResistances();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int PhysicalDamage{ get; set; } = 100;

    [CommandProperty(AccessLevel.GameMaster)]
    public int FireDamage{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ColdDamage{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int PoisonDamage{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int EnergyDamage{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ChaosDamage{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DirectDamage{ get; set; }

    #endregion

    #region Breath ability, like dragon fire breath

    private long m_NextBreathTime;

    // Must be overridden in subclass to enable
    public virtual bool HasBreath => false;

    // Base damage given is: CurrentHitPoints * BreathDamageScalar
    public virtual double BreathDamageScalar => Core.AOS ? 0.16 : 0.05;

    // Min/max seconds until next breath
    public virtual double BreathMinDelay => 30.0;
    public virtual double BreathMaxDelay => 45.0;

    // Creature stops moving for 1.0 seconds while breathing
    public virtual double BreathStallTime => 1.0;

    // Effect is sent 1.3 seconds after BreathAngerSound and BreathAngerAnimation is played
    public virtual double BreathEffectDelay => 1.3;

    // Damage is given 1.0 seconds after effect is sent
    public virtual double BreathDamageDelay => 1.0;

    public virtual int BreathRange => RangePerception;

    // Damage types
    public virtual int BreathChaosDamage => 0;
    public virtual int BreathPhysicalDamage => 0;
    public virtual int BreathFireDamage => 100;
    public virtual int BreathColdDamage => 0;
    public virtual int BreathPoisonDamage => 0;
    public virtual int BreathEnergyDamage => 0;

    // Is immune to breath damages
    public virtual bool BreathImmune => false;

    // Effect details and sound
    public virtual int BreathEffectItemID => 0x36D4;
    public virtual int BreathEffectSpeed => 5;
    public virtual int BreathEffectDuration => 0;
    public virtual bool BreathEffectExplodes => false;
    public virtual bool BreathEffectFixedDir => false;
    public virtual int BreathEffectHue => 0;
    public virtual int BreathEffectRenderMode => 0;

    public virtual int BreathEffectSound => 0x227;

    // Anger sound/animations
    public virtual int BreathAngerSound => GetAngerSound();
    public virtual int BreathAngerAnimation => 12;

    public virtual void BreathStart(Mobile target)
    {
      BreathStallMovement();
      BreathPlayAngerSound();
      BreathPlayAngerAnimation();

      Direction = GetDirectionTo(target);

      Timer.DelayCall(TimeSpan.FromSeconds(BreathEffectDelay), BreathEffect_Callback, target);
    }

    public virtual void BreathStallMovement()
    {
      if (AIObject != null)
        AIObject.NextMove = Core.TickCount + (int)(BreathStallTime * 1000);
    }

    public virtual void BreathPlayAngerSound()
    {
      PlaySound(BreathAngerSound);
    }

    public virtual void BreathPlayAngerAnimation()
    {
      Animate(BreathAngerAnimation, 5, 1, true, false, 0);
    }

    public virtual void BreathEffect_Callback(Mobile target)
    {
      if (!target.Alive || !CanBeHarmful(target))
        return;

      BreathPlayEffectSound();
      BreathPlayEffect(target);

      Timer.DelayCall(TimeSpan.FromSeconds(BreathDamageDelay), BreathDamage_Callback, target);
    }

    public virtual void BreathPlayEffectSound()
    {
      PlaySound(BreathEffectSound);
    }

    public virtual void BreathPlayEffect(Mobile target)
    {
      Effects.SendMovingEffect(this, target, BreathEffectItemID,
        BreathEffectSpeed, BreathEffectDuration, BreathEffectFixedDir,
        BreathEffectExplodes, BreathEffectHue, BreathEffectRenderMode);
    }

    public virtual void BreathDamage_Callback(Mobile target)
    {
      if (target is BaseCreature creature && creature.BreathImmune)
        return;

      if (CanBeHarmful(target))
      {
        DoHarmful(target);
        BreathDealDamage(target);
      }
    }

    public virtual void BreathDealDamage(Mobile target)
    {
      if (!Evasion.CheckSpellEvasion(target))
      {
        int physDamage = BreathPhysicalDamage;
        int fireDamage = BreathFireDamage;
        int coldDamage = BreathColdDamage;
        int poisDamage = BreathPoisonDamage;
        int nrgyDamage = BreathEnergyDamage;

        if (BreathChaosDamage > 0)
          switch (Utility.Random(5))
          {
            case 0:
              physDamage += BreathChaosDamage;
              break;
            case 1:
              fireDamage += BreathChaosDamage;
              break;
            case 2:
              coldDamage += BreathChaosDamage;
              break;
            case 3:
              poisDamage += BreathChaosDamage;
              break;
            case 4:
              nrgyDamage += BreathChaosDamage;
              break;
          }

        if (physDamage == 0 && fireDamage == 0 && coldDamage == 0 && poisDamage == 0 && nrgyDamage == 0)
          target.Damage(BreathComputeDamage(), this); // Unresistable damage even in AOS
        else
          AOS.Damage(target, this, BreathComputeDamage(), physDamage, fireDamage, coldDamage, poisDamage,
            nrgyDamage);
      }
    }

    public virtual int BreathComputeDamage()
    {
      int damage = (int)(Hits * BreathDamageScalar);

      if (IsParagon)
        damage = (int)(damage / Paragon.HitsBuff);

      if (damage > 200)
        damage = 200;

      return damage;
    }

    #endregion

    #region Spill Acid

    public void SpillAcid(int amount)
    {
      SpillAcid(null, amount);
    }

    public void SpillAcid(Mobile target, int amount)
    {
      if (target != null && target.Map == null || Map == null)
        return;

      for (int i = 0; i < amount; ++i)
      {
        Point3D loc;
        Map map = Map;

        if (target != null && amount == 1)
        {
          loc = target.Location;
          map = target.Map;
        }
        else
          loc = map.GetRandomNearbyLocation(Location);

        Item acid = NewHarmfulItem();
        acid.MoveToWorld(loc, map);
      }
    }

    /*
      Solen Style, override me for other mobiles/items:
      kappa+acidslime, grizzles+whatever, etc.
    */

    public virtual Item NewHarmfulItem()
    {
      return new PoolOfAcid(TimeSpan.FromSeconds(10), 30, 30);
    }

    #endregion

    #region Flee!!!

    public virtual bool CanFlee => !m_Paragon;

    public DateTime EndFleeTime{ get; set; }

    public virtual void StopFlee()
    {
      EndFleeTime = DateTime.MinValue;
    }

    public virtual bool CheckFlee()
    {
      if (EndFleeTime == DateTime.MinValue)
        return false;

      if (DateTime.UtcNow >= EndFleeTime)
      {
        StopFlee();
        return false;
      }

      return true;
    }

    public virtual void BeginFlee(TimeSpan maxDuration)
    {
      EndFleeTime = DateTime.UtcNow + maxDuration;
    }

    #endregion

    #region Friends

    public List<Mobile> Friends{ get; private set; }

    public virtual bool AllowNewPetFriend => Friends == null || Friends.Count < 5;

    public virtual bool IsPetFriend(Mobile m)
    {
      return Friends.Contains(m);
    }

    public virtual void AddPetFriend(Mobile m)
    {
      if (Friends == null)
        Friends = new List<Mobile>();

      Friends.Add(m);
    }

    public virtual void RemovePetFriend(Mobile m)
    {
      Friends?.Remove(m);
    }

    public virtual bool IsFriend(Mobile m)
    {
      return OppositionGroup?.IsEnemy(this, m) != true && m is BaseCreature c && m_Team == c.m_Team
             && (m_bSummoned || m_Controlled) == (c.m_bSummoned || c.m_Controlled);
    }

    #endregion

    #region Allegiance

    public virtual Ethic EthicAllegiance => null;

    public enum Allegiance
    {
      None,
      Ally,
      Enemy
    }

    public virtual Allegiance GetFactionAllegiance(Mobile mob)
    {
      if (mob == null || mob.Map != Faction.Facet || FactionAllegiance == null)
        return Allegiance.None;

      Faction fac = Faction.Find(mob, true);

      if (fac == null)
        return Allegiance.None;

      return fac == FactionAllegiance ? Allegiance.Ally : Allegiance.Enemy;
    }

    public virtual Allegiance GetEthicAllegiance(Mobile mob)
    {
      if (mob == null || mob.Map != Faction.Facet || EthicAllegiance == null)
        return Allegiance.None;

      Ethic ethic = Ethic.Find(mob, true);

      if (ethic == null)
        return Allegiance.None;

      return ethic == EthicAllegiance ? Allegiance.Ally : Allegiance.Enemy;
    }

    #endregion

    #region Alter[...]Damage From/To

    public virtual void AlterDamageScalarFrom(Mobile caster, ref double scalar)
    {
    }

    public virtual void AlterDamageScalarTo(Mobile target, ref double scalar)
    {
    }

    public virtual void AlterSpellDamageFrom(Mobile from, ref int damage)
    {
    }

    public virtual void AlterSpellDamageTo(Mobile to, ref int damage)
    {
    }

    public virtual void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
    }

    public virtual void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
    }

    #endregion

    #region Food

    private static Type[] m_Eggs =
    {
      typeof(FriedEggs), typeof(Eggs)
    };

    private static Type[] m_Fish =
    {
      typeof(FishSteak), typeof(RawFishSteak)
    };

    private static Type[] m_GrainsAndHay =
    {
      typeof(BreadLoaf), typeof(FrenchBread), typeof(SheafOfHay)
    };

    private static Type[] m_Meat =
    {
      /* Cooked */
      typeof(Bacon), typeof(CookedBird), typeof(Sausage),
      typeof(Ham), typeof(Ribs), typeof(LambLeg),
      typeof(ChickenLeg),

      /* Uncooked */
      typeof(RawBird), typeof(RawRibs), typeof(RawLambLeg),
      typeof(RawChickenLeg),

      /* Body Parts */
      typeof(Head), typeof(LeftArm), typeof(LeftLeg),
      typeof(Torso), typeof(RightArm), typeof(RightLeg)
    };

    private static Type[] m_FruitsAndVegies =
    {
      typeof(HoneydewMelon), typeof(YellowGourd), typeof(GreenGourd),
      typeof(Banana), typeof(Bananas), typeof(Lemon), typeof(Lime),
      typeof(Dates), typeof(Grapes), typeof(Peach), typeof(Pear),
      typeof(Apple), typeof(Watermelon), typeof(Squash),
      typeof(Cantaloupe), typeof(Carrot), typeof(Cabbage),
      typeof(Onion), typeof(Lettuce), typeof(Pumpkin)
    };

    private static Type[] m_Gold =
    {
      // white wyrms eat gold..
      typeof(Gold)
    };

    public virtual bool CheckFoodPreference(Item f)
    {
      if (CheckFoodPreference(f, FoodType.Eggs, m_Eggs))
        return true;

      if (CheckFoodPreference(f, FoodType.Fish, m_Fish))
        return true;

      if (CheckFoodPreference(f, FoodType.GrainsAndHay, m_GrainsAndHay))
        return true;

      if (CheckFoodPreference(f, FoodType.Meat, m_Meat))
        return true;

      if (CheckFoodPreference(f, FoodType.FruitsAndVegies, m_FruitsAndVegies))
        return true;

      if (CheckFoodPreference(f, FoodType.Gold, m_Gold))
        return true;

      return false;
    }

    public virtual bool CheckFoodPreference(Item fed, FoodType type, Type[] types)
    {
      if ((FavoriteFood & type) == 0)
        return false;

      Type fedType = fed.GetType();
      bool contains = false;

      for (int i = 0; !contains && i < types.Length; ++i)
        contains = fedType == types[i];

      return contains;
    }

    public virtual bool CheckFeed(Mobile from, Item dropped)
    {
      if (!IsDeadPet && Controlled && (ControlMaster == from || IsPetFriend(from)))
      {
        Item f = dropped;

        if (CheckFoodPreference(f))
        {
          int amount = f.Amount;

          if (amount > 0)
          {
            int stamGain;

            if (f is Gold)
              stamGain = amount - 50;
            else
              stamGain = amount * 15 - 50;

            if (stamGain > 0)
              Stam += stamGain;

            if (Core.SE)
            {
              if (m_Loyalty < MaxLoyalty) m_Loyalty = MaxLoyalty;
            }
            else
            {
              for (int i = 0; i < amount; ++i)
                if (m_Loyalty < MaxLoyalty && 0.5 >= Utility.RandomDouble())
                  m_Loyalty += 10;
            }

            /* if ( happier )*/
            // looks like in OSI pets say they are happier even if they are at maximum loyalty
            SayTo(from, 502060); // Your pet looks happier.

            if (Body.IsAnimal)
              Animate(3, 5, 1, true, false, 0);
            else if (Body.IsMonster)
              Animate(17, 5, 1, true, false, 0);

            if (IsBondable && !IsBonded)
            {
              Mobile master = m_ControlMaster;

              if (master != null && master == from) //So friends can't start the bonding process
              {
                if (MinTameSkill <= 291 || master.Skills.AnimalTaming.BaseFixedPoint >= MinTameSkill ||
                    OverrideBondingReqs() ||
                    Core.ML && master.Skills.AnimalTaming.Fixed >= MinTameSkill)
                {
                  if (BondingBegin == DateTime.MinValue)
                  {
                    BondingBegin = DateTime.UtcNow;
                  }
                  else if (BondingBegin + BondingDelay <= DateTime.UtcNow)
                  {
                    IsBonded = true;
                    BondingBegin = DateTime.MinValue;
                    from.SendLocalizedMessage(1049666); // Your pet has bonded with you!
                  }
                }
                else if (Core.ML)
                {
                  from.SendLocalizedMessage(
                    1075268); // Your pet cannot form a bond with you until your animal taming ability has risen.
                }
              }
            }

            dropped.Delete();
            return true;
          }
        }
      }

      return false;
    }

    #endregion

    #region OnAction[...]

    public virtual void OnActionWander()
    {
    }

    public virtual void OnActionCombat()
    {
    }

    public virtual void OnActionGuard()
    {
    }

    public virtual void OnActionFlee()
    {
    }

    public virtual void OnActionInteract()
    {
    }

    public virtual void OnActionBackoff()
    {
    }

    #endregion

    #region Corpse Resources

    public virtual int Feathers => 0;
    public virtual int Wool => 0;

    public virtual MeatType MeatType => MeatType.Ribs;
    public virtual int Meat => 0;

    public virtual int Hides => 0;
    public virtual HideType HideType => HideType.Regular;

    public virtual int Scales => 0;
    public virtual ScaleType ScaleType => ScaleType.Red;

    #endregion

    #region Teaching

    public virtual bool CanTeach => false;

    public virtual bool CheckTeach(SkillName skill, Mobile from)
    {
      if (!CanTeach)
        return false;

      if (skill == SkillName.Stealth && from.Skills.Hiding.Base < Stealth.HidingRequirement)
        return false;

      if (skill == SkillName.RemoveTrap && (from.Skills.Lockpicking.Base < 50.0 ||
                                            from.Skills.DetectHidden.Base < 50.0))
        return false;

      if (!Core.AOS && (skill == SkillName.Focus || skill == SkillName.Chivalry || skill == SkillName.Necromancy))
        return false;

      return true;
    }

    public enum TeachResult
    {
      Success,
      Failure,
      KnowsMoreThanMe,
      KnowsWhatIKnow,
      SkillNotRaisable,
      NotEnoughFreePoints
    }

    public virtual TeachResult CheckTeachSkills(SkillName skill, Mobile m, int maxPointsToLearn, ref int pointsToLearn,
      bool doTeach)
    {
      if (!CheckTeach(skill, m) || !m.CheckAlive())
        return TeachResult.Failure;

      Skill ourSkill = Skills[skill];
      Skill theirSkill = m.Skills[skill];

      if (ourSkill == null || theirSkill == null)
        return TeachResult.Failure;

      int baseToSet = ourSkill.BaseFixedPoint / 3;

      if (baseToSet > 420)
        baseToSet = 420;
      else if (baseToSet < 200)
        return TeachResult.Failure;

      if (baseToSet > theirSkill.CapFixedPoint)
        baseToSet = theirSkill.CapFixedPoint;

      pointsToLearn = baseToSet - theirSkill.BaseFixedPoint;

      if (maxPointsToLearn > 0 && pointsToLearn > maxPointsToLearn)
      {
        pointsToLearn = maxPointsToLearn;
        baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
      }

      if (pointsToLearn < 0)
        return TeachResult.KnowsMoreThanMe;

      if (pointsToLearn == 0)
        return TeachResult.KnowsWhatIKnow;

      if (theirSkill.Lock != SkillLock.Up)
        return TeachResult.SkillNotRaisable;

      int freePoints = m.Skills.Cap - m.Skills.Total;
      int freeablePoints = 0;

      if (freePoints < 0)
        freePoints = 0;

      for (int i = 0; freePoints + freeablePoints < pointsToLearn && i < m.Skills.Length; ++i)
      {
        Skill sk = m.Skills[i];

        if (sk == theirSkill || sk.Lock != SkillLock.Down)
          continue;

        freeablePoints += sk.BaseFixedPoint;
      }

      if (freePoints + freeablePoints == 0)
        return TeachResult.NotEnoughFreePoints;

      if (freePoints + freeablePoints < pointsToLearn)
      {
        pointsToLearn = freePoints + freeablePoints;
        baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
      }

      if (doTeach)
      {
        int need = pointsToLearn - freePoints;

        for (int i = 0; need > 0 && i < m.Skills.Length; ++i)
        {
          Skill sk = m.Skills[i];

          if (sk == theirSkill || sk.Lock != SkillLock.Down)
            continue;

          if (sk.BaseFixedPoint < need)
          {
            need -= sk.BaseFixedPoint;
            sk.BaseFixedPoint = 0;
          }
          else
          {
            sk.BaseFixedPoint -= need;
            need = 0;
          }
        }

        /* Sanity check */
        if (baseToSet > theirSkill.CapFixedPoint ||
            m.Skills.Total - theirSkill.BaseFixedPoint + baseToSet > m.Skills.Cap)
          return TeachResult.NotEnoughFreePoints;

        theirSkill.BaseFixedPoint = baseToSet;
      }

      return TeachResult.Success;
    }

    public virtual bool CheckTeachingMatch(Mobile m)
    {
      if (m_Teaching == (SkillName)(-1))
        return false;

      if (m is PlayerMobile mobile)
        return mobile.Learning == m_Teaching;

      return true;
    }

    private SkillName m_Teaching = (SkillName)(-1);

    public virtual bool Teach(SkillName skill, Mobile m, int maxPointsToLearn, bool doTeach)
    {
      int pointsToLearn = 0;
      TeachResult res = CheckTeachSkills(skill, m, maxPointsToLearn, ref pointsToLearn, doTeach);

      switch (res)
      {
        case TeachResult.KnowsMoreThanMe:
        {
          Say(501508); // I cannot teach thee, for thou knowest more than I!
          break;
        }
        case TeachResult.KnowsWhatIKnow:
        {
          Say(501509); // I cannot teach thee, for thou knowest all I can teach!
          break;
        }
        case TeachResult.NotEnoughFreePoints:
        case TeachResult.SkillNotRaisable:
        {
          // Make sure this skill is marked to raise. If you are near the skill cap (700 points) you may need to lose some points in another skill first.
          m.SendLocalizedMessage(501510, "", 0x22);
          break;
        }
        case TeachResult.Success:
        {
          if (doTeach)
          {
            Say(501539); // Let me show thee something of how this is done.
            m.SendLocalizedMessage(501540); // Your skill level increases.

            m_Teaching = (SkillName)(-1);

            if (m is PlayerMobile mobile)
              mobile.Learning = (SkillName)(-1);
          }
          else
          {
            // I will teach thee all I know, if paid the amount in full.  The price is:
            Say(1019077, AffixType.Append, $" {pointsToLearn}", "");
            Say(1043108); // For less I shall teach thee less.

            m_Teaching = skill;

            if (m is PlayerMobile mobile)
              mobile.Learning = skill;
          }

          return true;
        }
      }

      return false;
    }

    #endregion

    #region Set[...]

    public void SetDamage(int val)
    {
      m_DamageMin = val;
      m_DamageMax = val;
    }

    public void SetDamage(int min, int max)
    {
      m_DamageMin = min;
      m_DamageMax = max;
    }

    public void SetHits(int val)
    {
      if (val < 1000 && !Core.AOS)
        val = val * 100 / 60;

      HitsMaxSeed = val;
      Hits = HitsMax;
    }

    public void SetHits(int min, int max)
    {
      if (min < 1000 && !Core.AOS)
      {
        min = min * 100 / 60;
        max = max * 100 / 60;
      }

      HitsMaxSeed = Utility.RandomMinMax(min, max);
      Hits = HitsMax;
    }

    public void SetStam(int val)
    {
      StamMaxSeed = val;
      Stam = StamMax;
    }

    public void SetStam(int min, int max)
    {
      StamMaxSeed = Utility.RandomMinMax(min, max);
      Stam = StamMax;
    }

    public void SetMana(int val)
    {
      ManaMaxSeed = val;
      Mana = ManaMax;
    }

    public void SetMana(int min, int max)
    {
      ManaMaxSeed = Utility.RandomMinMax(min, max);
      Mana = ManaMax;
    }

    public void SetStr(int val)
    {
      RawStr = val;
      Hits = HitsMax;
    }

    public void SetStr(int min, int max)
    {
      RawStr = Utility.RandomMinMax(min, max);
      Hits = HitsMax;
    }

    public void SetDex(int val)
    {
      RawDex = val;
      Stam = StamMax;
    }

    public void SetDex(int min, int max)
    {
      RawDex = Utility.RandomMinMax(min, max);
      Stam = StamMax;
    }

    public void SetInt(int val)
    {
      RawInt = val;
      Mana = ManaMax;
    }

    public void SetInt(int min, int max)
    {
      RawInt = Utility.RandomMinMax(min, max);
      Mana = ManaMax;
    }

    public void SetDamageType(ResistanceType type, int min, int max)
    {
      SetDamageType(type, Utility.RandomMinMax(min, max));
    }

    public void SetDamageType(ResistanceType type, int val)
    {
      switch (type)
      {
        case ResistanceType.Physical:
          PhysicalDamage = val;
          break;
        case ResistanceType.Fire:
          FireDamage = val;
          break;
        case ResistanceType.Cold:
          ColdDamage = val;
          break;
        case ResistanceType.Poison:
          PoisonDamage = val;
          break;
        case ResistanceType.Energy:
          EnergyDamage = val;
          break;
      }
    }

    public void SetResistance(ResistanceType type, int min, int max)
    {
      SetResistance(type, Utility.RandomMinMax(min, max));
    }

    public void SetResistance(ResistanceType type, int val)
    {
      switch (type)
      {
        case ResistanceType.Physical:
          m_PhysicalResistance = val;
          break;
        case ResistanceType.Fire:
          m_FireResistance = val;
          break;
        case ResistanceType.Cold:
          m_ColdResistance = val;
          break;
        case ResistanceType.Poison:
          m_PoisonResistance = val;
          break;
        case ResistanceType.Energy:
          m_EnergyResistance = val;
          break;
      }

      UpdateResistances();
    }

    public void SetSkill(SkillName name, double val)
    {
      Skills[name].BaseFixedPoint = (int)(val * 10);

      if (Skills[name].Base > Skills[name].Cap)
      {
        if (Core.SE)
          SkillsCap += Skills[name].BaseFixedPoint - Skills[name].CapFixedPoint;

        Skills[name].Cap = Skills[name].Base;
      }
    }

    public void SetSkill(SkillName name, double min, double max)
    {
      int minFixed = (int)(min * 10);
      int maxFixed = (int)(max * 10);

      Skills[name].BaseFixedPoint = Utility.RandomMinMax(minFixed, maxFixed);

      if (Skills[name].Base > Skills[name].Cap)
      {
        if (Core.SE)
          SkillsCap += Skills[name].BaseFixedPoint - Skills[name].CapFixedPoint;

        Skills[name].Cap = Skills[name].Base;
      }
    }

    public void SetFameLevel(int level)
    {
      switch (level)
      {
        case 1:
          Fame = Utility.RandomMinMax(0, 1249);
          break;
        case 2:
          Fame = Utility.RandomMinMax(1250, 2499);
          break;
        case 3:
          Fame = Utility.RandomMinMax(2500, 4999);
          break;
        case 4:
          Fame = Utility.RandomMinMax(5000, 9999);
          break;
        case 5:
          Fame = Utility.RandomMinMax(10000, 10000);
          break;
      }
    }

    public void SetKarmaLevel(int level)
    {
      switch (level)
      {
        case 0:
          Karma = -Utility.RandomMinMax(0, 624);
          break;
        case 1:
          Karma = -Utility.RandomMinMax(625, 1249);
          break;
        case 2:
          Karma = -Utility.RandomMinMax(1250, 2499);
          break;
        case 3:
          Karma = -Utility.RandomMinMax(2500, 4999);
          break;
        case 4:
          Karma = -Utility.RandomMinMax(5000, 9999);
          break;
        case 5:
          Karma = -Utility.RandomMinMax(10000, 10000);
          break;
      }
    }

    #endregion

    #region Pack & Loot

    #region Mondain's Legacy

    public void PackArcaneScroll(int min, int max)
    {
      PackArcaneScroll(Utility.RandomMinMax(min, max));
    }

    public void PackArcaneScroll(int amount)
    {
      for (int i = 0; i < amount; ++i)
        PackArcaneScroll();
    }

    public void PackArcaneScroll()
    {
      if (!Core.ML)
        return;

      PackItem(Loot.Construct(Loot.ArcanistScrollTypes));
    }

    #endregion

    public void PackPotion()
    {
      PackItem(Loot.RandomPotion());
    }

    public void PackArcanceScroll(double chance)
    {
      if (!Core.ML || chance <= Utility.RandomDouble())
        return;

      PackItem(Loot.Construct(Loot.ArcanistScrollTypes));
    }

    public void PackNecroScroll(int index)
    {
      if (!Core.AOS || 0.05 <= Utility.RandomDouble())
        return;

      PackItem(Loot.Construct(Loot.NecromancyScrollTypes, index));
    }

    public void PackScroll(int minCircle, int maxCircle)
    {
      PackScroll(Utility.RandomMinMax(minCircle, maxCircle));
    }

    public void PackScroll(int circle)
    {
      int min = (circle - 1) * 8;

      PackItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
    }

    public void PackMagicItems(int minLevel, int maxLevel, double armorChance = 0.30, double weaponChance = 0.15)
    {
      if (!PackArmor(minLevel, maxLevel, armorChance))
        PackWeapon(minLevel, maxLevel, weaponChance);
    }

    public virtual void DropBackpack()
    {
      if (Backpack?.Items.Count > 0)
      {
        Backpack b = new CreatureBackpack(Name);

        List<Item> list = new List<Item>(Backpack.Items);
        foreach (Item item in list) b.DropItem(item);

        BaseHouse house = BaseHouse.FindHouseAt(this);
        if (house != null)
          b.MoveToWorld(house.BanLocation, house.Map);
        else
          b.MoveToWorld(Location, Map);
      }
    }

    protected bool m_Spawning;
    protected int m_KillersLuck;

    public virtual void GenerateLoot(bool spawning)
    {
      m_Spawning = spawning;

      if (!spawning)
        m_KillersLuck = LootPack.GetLuckChanceForKiller(this);

      GenerateLoot();

      if (m_Paragon)
      {
        if (Fame < 1250)
          AddLoot(LootPack.Meager);
        else if (Fame < 2500)
          AddLoot(LootPack.Average);
        else if (Fame < 5000)
          AddLoot(LootPack.Rich);
        else if (Fame < 10000)
          AddLoot(LootPack.FilthyRich);
        else
          AddLoot(LootPack.UltraRich);
      }

      m_Spawning = false;
      m_KillersLuck = 0;
    }

    public virtual void GenerateLoot()
    {
    }

    public virtual void AddLoot(LootPack pack, int amount)
    {
      for (int i = 0; i < amount; ++i)
        AddLoot(pack);
    }

    public virtual void AddLoot(LootPack pack)
    {
      if (Summoned)
        return;

      Container backpack = Backpack ?? new Backpack { Movable = false };
      AddItem(backpack);

      pack.Generate(this, backpack, m_Spawning, m_KillersLuck);
    }

    public bool PackArmor(int minLevel, int maxLevel)
    {
      return PackArmor(minLevel, maxLevel, 1.0);
    }

    public bool PackArmor(int minLevel, int maxLevel, double chance)
    {
      if (chance <= Utility.RandomDouble())
        return false;

      Cap(ref minLevel, 0, 5);
      Cap(ref maxLevel, 0, 5);

      if (Core.AOS)
      {
        Item item = Loot.RandomArmorOrShieldOrJewelry();

        if (item == null)
          return false;

        GetRandomAOSStats(minLevel, maxLevel, out int attributeCount, out int min, out int max);

        if (item is BaseArmor armor)
          BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
        else if (item is BaseJewel jewel)
          BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);

        PackItem(item);
      }
      else
      {
        BaseArmor armor = Loot.RandomArmorOrShield();

        if (armor == null)
          return false;

        armor.ProtectionLevel = (ArmorProtectionLevel)RandomMinMaxScaled(minLevel, maxLevel);
        armor.Durability = (ArmorDurabilityLevel)RandomMinMaxScaled(minLevel, maxLevel);

        PackItem(armor);
      }

      return true;
    }

    public static void GetRandomAOSStats(int minLevel, int maxLevel, out int attributeCount, out int min, out int max)
    {
      int v = RandomMinMaxScaled(minLevel, maxLevel);

      if (v >= 5)
      {
        attributeCount = Utility.RandomMinMax(2, 6);
        min = 20;
        max = 70;
      }
      else if (v == 4)
      {
        attributeCount = Utility.RandomMinMax(2, 4);
        min = 20;
        max = 50;
      }
      else if (v == 3)
      {
        attributeCount = Utility.RandomMinMax(2, 3);
        min = 20;
        max = 40;
      }
      else if (v == 2)
      {
        attributeCount = Utility.RandomMinMax(1, 2);
        min = 10;
        max = 30;
      }
      else
      {
        attributeCount = 1;
        min = 10;
        max = 20;
      }
    }

    public static int RandomMinMaxScaled(int min, int max)
    {
      if (min == max)
        return min;

      if (min > max)
      {
        int hold = min;
        min = max;
        max = hold;
      }

      /* Example:
       *    min: 1
       *    max: 5
       *  count: 5
       *
       * total = (5*5) + (4*4) + (3*3) + (2*2) + (1*1) = 25 + 16 + 9 + 4 + 1 = 55
       *
       * chance for min+0 : 25/55 : 45.45%
       * chance for min+1 : 16/55 : 29.09%
       * chance for min+2 :  9/55 : 16.36%
       * chance for min+3 :  4/55 :  7.27%
       * chance for min+4 :  1/55 :  1.81%
       */

      int count = max - min + 1;
      int total = 0, toAdd = count;

      for (int i = 0; i < count; ++i, --toAdd)
        total += toAdd * toAdd;

      int rand = Utility.Random(total);
      toAdd = count;

      int val = min;

      for (int i = 0; i < count; ++i, --toAdd, ++val)
      {
        rand -= toAdd * toAdd;

        if (rand < 0)
          break;
      }

      return val;
    }

    public bool PackSlayer(double chance = 0.05)
    {
      if (chance <= Utility.RandomDouble())
        return false;

      if (Utility.RandomBool())
      {
        BaseInstrument instrument = Loot.RandomInstrument();

        if (instrument != null)
        {
          instrument.Slayer = SlayerGroup.GetLootSlayerType(GetType());
          PackItem(instrument);
        }
      }
      else if (!Core.AOS)
      {
        BaseWeapon weapon = Loot.RandomWeapon();

        if (weapon != null)
        {
          weapon.Slayer = SlayerGroup.GetLootSlayerType(GetType());
          PackItem(weapon);
        }
      }

      return true;
    }

    public bool PackWeapon(int minLevel, int maxLevel, double chance = 1.0)
    {
      if (chance <= Utility.RandomDouble())
        return false;

      Cap(ref minLevel, 0, 5);
      Cap(ref maxLevel, 0, 5);

      if (Core.AOS)
      {
        Item item = Loot.RandomWeaponOrJewelry();

        if (item == null)
          return false;

        GetRandomAOSStats(minLevel, maxLevel, out int attributeCount, out int min, out int max);

        if (item is BaseWeapon weapon)
          BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
        else if (item is BaseJewel jewel)
          BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);

        PackItem(item);
      }
      else
      {
        BaseWeapon weapon = Loot.RandomWeapon();

        if (weapon == null)
          return false;

        if (0.05 > Utility.RandomDouble())
          weapon.Slayer = SlayerName.Silver;

        weapon.DamageLevel = (WeaponDamageLevel)RandomMinMaxScaled(minLevel, maxLevel);
        weapon.AccuracyLevel = (WeaponAccuracyLevel)RandomMinMaxScaled(minLevel, maxLevel);
        weapon.DurabilityLevel = (WeaponDurabilityLevel)RandomMinMaxScaled(minLevel, maxLevel);

        PackItem(weapon);
      }

      return true;
    }

    public void PackGold(int amount)
    {
      if (amount > 0)
        PackItem(new Gold(amount));
    }

    public void PackGold(int min, int max)
    {
      PackGold(Utility.RandomMinMax(min, max));
    }

    public void PackStatue(int min, int max)
    {
      PackStatue(Utility.RandomMinMax(min, max));
    }

    public void PackStatue(int amount)
    {
      for (int i = 0; i < amount; ++i)
        PackStatue();
    }

    public void PackStatue()
    {
      PackItem(Loot.RandomStatue());
    }

    public void PackGem(int min, int max)
    {
      PackGem(Utility.RandomMinMax(min, max));
    }

    public void PackGem(int amount = 1)
    {
      if (amount <= 0)
        return;

      Item gem = Loot.RandomGem();

      gem.Amount = amount;

      PackItem(gem);
    }

    public void PackNecroReg(int min, int max)
    {
      PackNecroReg(Utility.RandomMinMax(min, max));
    }

    public void PackNecroReg(int amount)
    {
      for (int i = 0; i < amount; ++i)
        PackNecroReg();
    }

    public void PackNecroReg()
    {
      if (!Core.AOS)
        return;

      PackItem(Loot.RandomNecromancyReagent());
    }

    public void PackReg(int min, int max)
    {
      PackReg(Utility.RandomMinMax(min, max));
    }

    public void PackReg(int amount)
    {
      if (amount <= 0)
        return;

      Item reg = Loot.RandomReagent();

      reg.Amount = amount;

      PackItem(reg);
    }

    public void PackItem(Item item)
    {
      if (Summoned || item == null)
      {
        item?.Delete();
        return;
      }

      Container pack = Backpack ?? new Backpack { Movable = false };
      AddItem(pack);

      if (!item.Stackable || !pack.TryDropItem(this, item, false)) // try stack
        pack.DropItem(item); // failed, drop it anyway
    }

    #endregion

    #region Healing

    public virtual bool CanHeal => false;
    public virtual bool CanHealOwner => false;
    public virtual double HealScalar => 1.0;

    public virtual int HealSound => 0x57;
    public virtual int HealStartRange => 2;
    public virtual int HealEndRange => RangePerception;
    public virtual double HealTrigger => 0.78;
    public virtual double HealDelay => 6.5;
    public virtual double HealInterval => 0.0;
    public virtual bool HealFully => true;
    public virtual double HealOwnerTrigger => 0.78;
    public virtual double HealOwnerDelay => 6.5;
    public virtual double HealOwnerInterval => 30.0;
    public virtual bool HealOwnerFully => false;

    private long m_NextHealTime = Core.TickCount;
    private long m_NextHealOwnerTime = Core.TickCount;
    private Timer m_HealTimer;

    public bool IsHealing => m_HealTimer != null;

    public virtual void HealStart(Mobile patient)
    {
      bool onSelf = patient == this;

      //DoBeneficial( patient );

      RevealingAction();

      if (!onSelf)
      {
        patient.RevealingAction();
        patient.SendLocalizedMessage(1008078, false, Name); //  : Attempting to heal you.
      }

      double seconds = (onSelf ? HealDelay : HealOwnerDelay) + (patient.Alive ? 0.0 : 5.0);

      m_HealTimer = Timer.DelayCall(TimeSpan.FromSeconds(seconds), Heal, patient);
    }

    public virtual void Heal(Mobile patient)
    {
      if (!Alive || Map == Map.Internal || !CanBeBeneficial(patient, true, true) || patient.Map != Map ||
          !InRange(patient, HealEndRange))
      {
        StopHeal();
        return;
      }

      bool onSelf = patient == this;

      if (!patient.Alive)
      {
      }
      else if (patient.Poisoned)
      {
        int poisonLevel = patient.Poison.Level;

        int healing = Skills.Healing.Fixed;
        int anatomy = Skills.Anatomy.Fixed;
        double chance = (healing - 300) / 500 - poisonLevel;

        if (healing >= 600 && anatomy >= 600 && chance > Utility.RandomDouble())
          if (patient.CurePoison(this))
          {
            patient.SendLocalizedMessage(1010059); // You have been cured of all poisons.

            CheckSkill(SkillName.Healing, 0, 600 + poisonLevel * 100); // TODO: Verify formula
            CheckSkill(SkillName.Anatomy, 0, 1000);
          }
      }
      else if (BleedAttack.IsBleeding(patient))
      {
        patient.SendLocalizedMessage(1060167); // The bleeding wounds have healed, you are no longer bleeding!
        BleedAttack.EndBleed(patient, false);
      }
      else
      {
        int healing = Skills.Healing.Fixed;
        int anatomy = Skills.Anatomy.Fixed;
        double chance = (healing + 100) / 1000;

        if (chance > Utility.RandomDouble())
        {
          int min = anatomy / 100 + healing / 60 + 40;
          int max = anatomy / 80 + healing / 30 + 40;

          if (onSelf)
            max += 100;

          double toHeal = min + Utility.RandomDouble() * (max - min) / 10.0;

          toHeal *= HealScalar;

          patient.Heal((int)toHeal);

          CheckSkill(SkillName.Healing, 0, 900);
          CheckSkill(SkillName.Anatomy, 0, 1000);
        }
      }

      HealEffect(patient);

      StopHeal();

      if (onSelf && HealFully && Hits >= HealTrigger * HitsMax && Hits < HitsMax || !onSelf && HealOwnerFully &&
          patient.Hits >= HealOwnerTrigger * patient.HitsMax && patient.Hits < patient.HitsMax)
        HealStart(patient);
    }

    public virtual void StopHeal()
    {
      m_HealTimer?.Stop();

      m_HealTimer = null;
    }

    public virtual void HealEffect(Mobile patient)
    {
      patient.PlaySound(HealSound);
    }

    #endregion

    #region Damaging Aura

    private long m_NextAura;

    public virtual bool HasAura => false;
    public virtual TimeSpan AuraInterval => TimeSpan.FromSeconds(5);
    public virtual int AuraRange => 4;

    public virtual int AuraBaseDamage => 5;
    public virtual int AuraPhysicalDamage => 0;
    public virtual int AuraFireDamage => 100;
    public virtual int AuraColdDamage => 0;
    public virtual int AuraPoisonDamage => 0;
    public virtual int AuraEnergyDamage => 0;
    public virtual int AuraChaosDamage => 0;

    public virtual void AuraDamage()
    {
      if (!Alive || IsDeadBondedPet)
        return;

      IPooledEnumerable<Mobile> eable = GetMobilesInRange(AuraRange);

      IEnumerable<Mobile> list = eable.Where(m =>
        m != this && CanBeHarmful(m, false) && (Core.AOS || InLOS(m)) &&
        (m is BaseCreature bc && (bc.Controlled || bc.Summoned || bc.Team != Team) || m.Player));

      foreach (Mobile m in list)
      {
        AOS.Damage(m, this, AuraBaseDamage, AuraPhysicalDamage, AuraFireDamage, AuraColdDamage, AuraPoisonDamage,
          AuraEnergyDamage, AuraChaosDamage);
        AuraEffect(m);
      }

      eable.Free();
    }

    public virtual void AuraEffect(Mobile m)
    {
    }

    #endregion
  }

  public class LoyaltyTimer : Timer
  {
    private static TimeSpan InternalDelay = TimeSpan.FromMinutes(5.0);

    private DateTime m_NextHourlyCheck;

    public LoyaltyTimer() : base(InternalDelay, InternalDelay)
    {
      m_NextHourlyCheck = DateTime.UtcNow + TimeSpan.FromHours(1.0);
      Priority = TimerPriority.FiveSeconds;
    }

    public static void Initialize()
    {
      new LoyaltyTimer().Start();
    }

    protected override void OnTick()
    {
      if (DateTime.UtcNow >= m_NextHourlyCheck)
        m_NextHourlyCheck = DateTime.UtcNow + TimeSpan.FromHours(1.0);
      else
        return;

      List<BaseCreature> toRelease = new List<BaseCreature>();

      // added array for wild creatures in house regions to be removed
      List<BaseCreature> toRemove = new List<BaseCreature>();

      Parallel.ForEach(World.Mobiles.Values, m =>
      {
        if (!(m is BaseCreature c))
          return;

        if (c is BaseMount mount && mount.Rider != null)
        {
          mount.OwnerAbandonTime = DateTime.MinValue;
          return;
        }

        if (c.IsDeadPet)
        {
          Mobile owner = c.ControlMaster;

          if (!c.IsStabled && (owner?.Deleted != false || owner.Map != c.Map ||
                               !owner.InRange(c, 12) || !c.CanSee(owner) || !c.InLOS(owner)))
          {
            if (c.OwnerAbandonTime == DateTime.MinValue)
              c.OwnerAbandonTime = DateTime.UtcNow;
            else if (c.OwnerAbandonTime + c.BondingAbandonDelay <= DateTime.UtcNow)
              lock (toRemove)
              {
                toRemove.Add(c);
              }
          }
          else
          {
            c.OwnerAbandonTime = DateTime.MinValue;
          }
        }
        else if (c.Controlled && c.Commandable)
        {
          c.OwnerAbandonTime = DateTime.MinValue;

          if (c.Map != Map.Internal)
          {
            c.Loyalty -= BaseCreature.MaxLoyalty / 10;

            if (c.Loyalty < BaseCreature.MaxLoyalty / 10)
            {
              c.Say(1043270, c.Name); // * ~1_NAME~ looks around desperately *
              c.PlaySound(c.GetIdleSound());
            }

            if (c.Loyalty <= 0)
              lock (toRelease)
              {
                toRelease.Add(c);
              }
          }
        }

        // added lines to check if a wild creature in a house region has to be removed or not
        if (!c.Controlled && !c.IsStabled && (c.Region.IsPartOf<HouseRegion>() && c.CanBeDamaged() ||
                                              c.RemoveIfUntamed && c.Spawner == null))
        {
          c.RemoveStep++;

          if (c.RemoveStep >= 20)
            lock (toRemove)
            {
              toRemove.Add(c);
            }
        }
        else
        {
          c.RemoveStep = 0;
        }
      });

      // TODO: Parallelize this
      foreach (BaseCreature c in toRelease)
      {
        c.Say(1043255, c.Name); // ~1_NAME~ appears to have decided that is better off without a master!
        c.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully Happy
        c.IsBonded = false;
        c.BondingBegin = DateTime.MinValue;
        c.OwnerAbandonTime = DateTime.MinValue;
        c.ControlTarget = null;
        c.AIObject
          .DoOrderRelease(); // this will prevent no release of creatures left alone with AI disabled (and consequent bug of Followers)
        c.DropBackpack();
      }

      foreach (BaseCreature c in toRemove)
        c.Delete();
    }
  }
}
