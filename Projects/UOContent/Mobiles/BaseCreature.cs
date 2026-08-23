using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.ConPVP;
using Server.Engines.MLQuests;
using Server.Engines.Quests.Doom;
using Server.Engines.Quests.Haven;
using Server.Engines.Spawners;
using Server.Engines.Virtues;
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
    /// <summary>
    ///     Summary description for MobileAI.
    /// </summary>
    public enum FightMode
    {
        None,      // Never focus on others
        Aggressor, // Only attack aggressors
        Strongest, // Attack the strongest
        Weakest,   // Attack the weakest
        Closest,   // Attack the closest
        Evil       // Only attack aggressor -or- negative karma
    }

    public enum OrderType
    {
        None,   // When no order, let's roam
        Come,   // "(All/Name) come"  Summons all or one pet to your location.
        Drop,   // "(Name) drop"  Drops its loot to the ground (if it carries any).
        Follow, // "(Name) follow"  Follows targeted being.

        // "(All/Name) follow me"  Makes all or one pet follow you.
        Friend,   // "(Name) friend"  Allows targeted player to confirm resurrection.
        Unfriend, // Remove a friend
        Guard,    // "(Name) guard"  Makes the specified pet guard you. Pets can only guard their owner.

        // "(All/Name) guard me"  Makes all or one pet guard you.
        Attack, // "(All/Name) kill",

        // "(All/Name) attack"  All or the specified pet(s) currently under your control attack the target.
        Patrol,   // "(Name) patrol"  Roves between two or more guarded targets.
        Release,  // "(Name) release"  Releases pet back into the wild (removes "tame" status).
        Stay,     // "(All/Name) stay" All or the specified pet(s) will stop and stay in current spot.
        Stop,     // "(All/Name) stop Cancels any current orders to attack, guard or follow.
        Transfer, // "(Name) transfer" Transfers complete ownership to targeted player.
        Rename    // "(Name) rename"  Changes the name of the pet.
    }

    [Flags]
    public enum FoodType
    {
        None = 0x0000,
        Meat = 0x0001,
        FruitsAndVeggies = 0x0002,
        GrainsAndHay = 0x0004,
        Fish = 0x0008,
        Eggs = 0x0010,
        Gold = 0x0020,
        Leather = 0x0040,
        Metal = 0x0080
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

        public int CompareTo(DamageStore ds) => (ds?.m_Damage ?? 0).CompareTo(m_Damage);
    }

    [SerializationGenerator(23, false)]
    public abstract partial class BaseCreature : Mobile, IHonorTarget, IQuestGiver
    {
        public enum Allegiance
        {
            None,
            Ally,
            Enemy
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

        public const int MaxLoyalty = 100;
        public const int LoyaltyIncreasePerFood = 10;
        public const int MaxLoyaltyIncrease = MaxLoyalty / LoyaltyIncreasePerFood;

        public const int MaxOwners = 5;

        public const int DefaultRangePerception = 16;

        private const double ChanceToRummage = 0.5;

        private const double MinutesToNextRummageMin = 1.0;
        private const double MinutesToNextRummageMax = 4.0;

        private const double MinutesToNextChanceMin = 0.25;
        private const double MinutesToNextChanceMax = 0.75;

        public const int ShoutRange = 8;

        private static readonly Type[] m_AnimateDeadTypes =
        {
            typeof(MoundOfMaggots), typeof(HellSteed), typeof(SkeletalMount),
            typeof(WailingBanshee), typeof(Wraith), typeof(SkeletalDragon),
            typeof(LichLord), typeof(FleshGolem), typeof(Lich),
            typeof(SkeletalKnight), typeof(BoneKnight), typeof(Mummy),
            typeof(SkeletalMage), typeof(BoneMagi), typeof(PatchworkSkeleton)
        };

        private static Mobile m_NoDupeGuards;

        private static readonly bool EnableRummaging = true;
        public static readonly TimeSpan ShoutDelay = TimeSpan.FromMinutes(1);

        private static readonly Type[] _eggs =
        {
            typeof(FriedEggs), typeof(Eggs)
        };

        private static readonly Type[] _fish =
        {
            typeof(FishSteak), typeof(RawFishSteak)
        };

        private static readonly Type[] _grainsAndHay =
        {
            typeof(BreadLoaf), typeof(FrenchBread), typeof(SheafOfHay)
        };

        private static readonly Type[] _meat =
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

        private static readonly Type[] _fruitsAndVeggies =
        {
            typeof(HoneydewMelon), typeof(YellowGourd), typeof(GreenGourd),
            typeof(Banana), typeof(Bananas), typeof(Lemon), typeof(Lime),
            typeof(Dates), typeof(Grapes), typeof(Peach), typeof(Pear),
            typeof(Apple), typeof(Watermelon), typeof(Squash),
            typeof(Cantaloupe), typeof(Carrot), typeof(Cabbage),
            typeof(Onion), typeof(Lettuce), typeof(Pumpkin)
        };

        private static readonly Type[] _gold =
        {
            // White wyrms eat gold.
            typeof(Gold)
        };

        private static readonly Type[] _metal =
        {
            // Materials
            typeof(BaseIngot), typeof(BaseOre),

            // Containers
            typeof(MetalChest), typeof(MetalGoldenChest), typeof(MetalBox),

            // Crafting
            typeof(Gears), typeof(Saw), typeof(Axle), typeof(AxleGears),
            typeof(ClockParts), typeof(Hinge), typeof(Springs), typeof(Spyglass),
            typeof(Fork), typeof(ForkLeft), typeof(ForkRight), typeof(Spoon),
            typeof(SpoonLeft), typeof(SpoonRight), typeof(Knife), typeof(KnifeLeft),
            typeof(KnifeRight), typeof(DrawKnife), typeof(Hammer), typeof(Froe), typeof(Inshave),
            typeof(Nails), typeof(RunicDovetailSaw), typeof(RunicHammer), typeof(Skillet),
            typeof(SledgeHammer), typeof(Tongs), typeof(TinkerTools), typeof(SmithHammer),
            typeof(AncientSmithyHammer), typeof(Scorp)
        };

        // --- Serialized state ---------------------------------------------------------
        // Nearly every field is behind a [SaveFlag] so a creature that matches its
        // defaults (including npc-speeds table values) writes only the version and flags.

        [SerializableField(0, setter: "private")]
        private AIType _defaultAI;

        [SerializableField(1, setter: "private")]
        [SaveFlag(nameof(ShouldSerializeCurrentAI), nameof(CurrentAIDefaultValue))]
        private AIType _currentAI;

        private bool ShouldSerializeCurrentAI() => _currentAI != _defaultAI;

        private AIType CurrentAIDefaultValue() => _defaultAI;

        [EncodedInt]
        [SerializableField(2)]
        [SaveFlag(nameof(ShouldSerializeRangePerception), nameof(RangePerceptionDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _rangePerception;

        private bool ShouldSerializeRangePerception() => _rangePerception != DefaultRangePerception;

        private int RangePerceptionDefaultValue() => DefaultRangePerception;

        [EncodedInt]
        [SerializableField(3)]
        [SaveFlag(nameof(ShouldSerializeRangeFight), nameof(RangeFightDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _rangeFight;

        private bool ShouldSerializeRangeFight() => _rangeFight != 1;

        private int RangeFightDefaultValue() => 1;

        [EncodedInt]
        [SerializableField(4)]
        [SaveFlag(nameof(ShouldSerializeRangeHome), nameof(RangeHomeDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _rangeHome = 10;

        private bool ShouldSerializeRangeHome() => _rangeHome != 10;

        private int RangeHomeDefaultValue() => 10;

        [EncodedInt]
        [SerializableField(5, fieldChanged: nameof(OnTeamChange))]
        [SaveFlag(nameof(ShouldSerializeTeam))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _team;

        private bool ShouldSerializeTeam() => _team != 0;

        private void OnTeamChange(int oldValue, int newValue) => OnTeamChange();

        [SerializableField(6)]
        [SaveFlag(nameof(ShouldSerializeFightMode), nameof(FightModeDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private FightMode _fightMode;

        private bool ShouldSerializeFightMode() => _fightMode != FightMode.Closest;

        private FightMode FightModeDefaultValue() => FightMode.Closest;

        /// <summary>
        /// The creature's npc-speeds bucket. Assigning applies the bucket's speeds; a
        /// type's constant bucket belongs in <see cref="DefaultSpeedClass"/>.
        /// </summary>
        [SerializableField(7, fieldChanged: nameof(OnSpeedClassChange))]
        [SaveFlag(nameof(ShouldSerializeSpeedClass), nameof(SpeedClassDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private SpeedLevel _speedClass;

        private bool ShouldSerializeSpeedClass() => _speedClass != DefaultSpeedClass;

        private SpeedLevel SpeedClassDefaultValue() => DefaultSpeedClass;

        private void OnSpeedClassChange(SpeedLevel oldValue, SpeedLevel newValue)
        {
            _speedEntry = null;
            ApplySpeedClass();
        }

        private bool _applyingSpeedClass;

        // Applies the current bucket's speeds, preserving the active/passive mode. The
        // guard keeps OnSpeedTuned from reading the half-assigned block as customization.
        private void ApplySpeedClass()
        {
            if (SpeedEntry == null)
            {
                return; // Custom (or an unloaded table) has no bucket to apply
            }

            _applyingSpeedClass = true;

            try
            {
                var wasActive = _currentSpeed == _activeSpeed && _currentSpeed != _passiveSpeed;

                GetSpeeds(out var activeSpeed, out var passiveSpeed);
                GetMoveSpeeds(out _activeMoveSpeed, out _passiveMoveSpeed);

                ActiveSpeed = activeSpeed;
                PassiveSpeed = passiveSpeed;
                CurrentSpeed = wasActive ? activeSpeed : passiveSpeed;
            }
            finally
            {
                _applyingSpeedClass = false;
            }
        }

        /// <summary>Seconds per AI decision while engaged; see <see cref="ActiveMoveSpeed"/> for movement pace.</summary>
        [SerializableField(8, fieldChanged: nameof(OnSpeedTuned))]
        [SaveFlag(nameof(ShouldSerializeSpeeds), nameof(ActiveSpeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private double _activeSpeed;

        // The four speeds are one block: either all conform to the bucket (elided as a
        // set), or the creature is custom and all four serialize. Partial conformance
        // cannot exist on the wire.
        private bool ShouldSerializeSpeeds()
        {
            if (_speedClass == SpeedLevel.Custom)
            {
                return true;
            }

            GetSpeeds(out var activeSpeed, out var passiveSpeed);
            GetMoveSpeeds(out var activeMoveSpeed, out var passiveMoveSpeed);

            return _activeSpeed != activeSpeed || _passiveSpeed != passiveSpeed ||
                   _activeMoveSpeed != activeMoveSpeed || _passiveMoveSpeed != passiveMoveSpeed;
        }

        // Tuning any speed away from the bucket makes the creature fully custom - the
        // bucket label must never lie. ApplySpeedClass assigns mid-transition and guards.
        private void OnSpeedTuned(double oldValue, double newValue)
        {
            if (!_applyingSpeedClass && _speedClass != SpeedLevel.Custom && ShouldSerializeSpeeds())
            {
                _speedClass = SpeedLevel.Custom;
                _speedEntry = null;
            }
        }

        private double ActiveSpeedDefaultValue()
        {
            GetSpeeds(out var activeSpeed, out _);
            return activeSpeed;
        }

        /// <summary>Seconds per AI decision while idle; see <see cref="PassiveMoveSpeed"/> for movement pace.</summary>
        [SerializableField(9, fieldChanged: nameof(OnSpeedTuned))]
        [SaveFlag(nameof(ShouldSerializeSpeeds), nameof(PassiveSpeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private double _passiveSpeed;

        private double PassiveSpeedDefaultValue()
        {
            GetSpeeds(out _, out var passiveSpeed);
            return passiveSpeed;
        }

        [SerializableField(10, fieldChanged: nameof(OnCurrentSpeedChange))]
        [SaveFlag(nameof(ShouldSerializeCurrentSpeed), nameof(CurrentSpeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private double _currentSpeed;

        private bool ShouldSerializeCurrentSpeed() => _currentSpeed != _passiveSpeed;

        private double CurrentSpeedDefaultValue() => _passiveSpeed;

        private void OnCurrentSpeedChange(double oldValue, double newValue) => AIObject?.OnCurrentSpeedChanged();

        /// <summary>
        /// Movement clock (seconds per step) while engaged; 0 = inherit
        /// <see cref="ActiveSpeed"/>. <see cref="CurrentMoveSpeed"/> resolves the pace.
        /// </summary>
        [SerializableField(11, allowFieldChange: nameof(CoerceMoveSpeed), fieldChanged: nameof(OnSpeedTuned))]
        [SaveFlag(nameof(ShouldSerializeSpeeds), nameof(ActiveMoveSpeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private double _activeMoveSpeed;

        /// <summary>
        /// Movement clock (seconds per step) while idle; 0 = inherit
        /// <see cref="PassiveSpeed"/>. <see cref="CurrentMoveSpeed"/> resolves the pace.
        /// </summary>
        [SerializableField(12, allowFieldChange: nameof(CoerceMoveSpeed), fieldChanged: nameof(OnSpeedTuned))]
        [SaveFlag(nameof(ShouldSerializeSpeeds), nameof(PassiveMoveSpeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private double _passiveMoveSpeed;

        private bool CoerceMoveSpeed(ref double value)
        {
            value = Math.Max(0, value); // anything non-positive means "inherit"
            return true;
        }

        private double ActiveMoveSpeedDefaultValue()
        {
            GetMoveSpeeds(out var activeMoveSpeed, out _);
            return activeMoveSpeed;
        }

        private double PassiveMoveSpeedDefaultValue()
        {
            GetMoveSpeeds(out _, out var passiveMoveSpeed);
            return passiveMoveSpeed;
        }

        [SerializableField(13)]
        [SaveFlag(nameof(ShouldSerializeHome))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private Point3D _home;

        private bool ShouldSerializeHome() => _home != Point3D.Zero;

        [SerializableField(14)]
        [SaveFlag(nameof(ShouldSerializeHomeMap))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private Map _homeMap;

        private bool ShouldSerializeHomeMap() => _homeMap != null;

        [SerializableField(15, fieldChanged: nameof(OnControlledChange))]
        [SaveFlag(nameof(ShouldSerializeControlled))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _controlled;

        private bool ShouldSerializeControlled() => _controlled;

        private void OnControlledChange(bool oldValue, bool newValue)
        {
            Delta(MobileDelta.Noto);
            InvalidateProperties();
        }

        // Field 16: ControlMaster (hand-written property; follower bookkeeping brackets the assignment)
        private Mobile _controlMaster;

        private bool ShouldSerializeControlMaster() => _controlMaster != null;

        [SerializableField(17)]
        [SaveFlag(nameof(ShouldSerializeControlTarget))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private Mobile _controlTarget;

        private bool ShouldSerializeControlTarget() => _controlTarget != null;

        [SerializableField(18)]
        [SaveFlag(nameof(ShouldSerializeControlDest))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private Point3D _controlDest;

        private bool ShouldSerializeControlDest() => _controlDest != Point3D.Zero;

        // Field 19: ControlOrder (hand-written property; order logic must run on equal re-assignment)
        private OrderType _controlOrder;

        private bool ShouldSerializeControlOrder() => _controlOrder != OrderType.None;

        [SerializableField(20)]
        [SaveFlag(nameof(ShouldSerializeMinTameSkill))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private double _minTameSkill;

        private bool ShouldSerializeMinTameSkill() => _minTameSkill != 0;

        // Field 21: Tamable (hand-written property; custom getter masks paragons)
        private bool _tamable;

        private bool ShouldSerializeTamable() => _tamable;

        [SerializableField(22, fieldChanged: nameof(OnSummonedChange))]
        [SaveFlag(nameof(ShouldSerializeSummoned))]
        [SerializedCommandProperty(AccessLevel.Administrator)]
        private bool _summoned;

        private bool ShouldSerializeSummoned() => _summoned;

        private void OnSummonedChange(bool oldValue, bool newValue)
        {
            NextReacquireTime = Core.TickCount;
            Delta(MobileDelta.Noto);
            InvalidateProperties();
        }

        [AnchoredDateTime]
        [SerializableField(23, getter: "protected", setter: "protected")]
        [SaveFlag(nameof(ShouldSerializeSummonEnd))]
        private DateTime _summonEnd;

        private bool ShouldSerializeSummonEnd() => _summoned;

        // Field 24: SummonMaster (hand-written property; follower bookkeeping brackets the assignment)
        private Mobile _summonMaster;

        private bool ShouldSerializeSummonMaster() => _summonMaster != null;

        [EncodedInt]
        [SerializableField(25)]
        [SaveFlag(nameof(ShouldSerializeControlSlots), nameof(ControlSlotsDefaultValue))]
        [SerializedCommandProperty(AccessLevel.Administrator)]
        private int _controlSlots = 1;

        private bool ShouldSerializeControlSlots() => _controlSlots != 1;

        private int ControlSlotsDefaultValue() => 1;

        [EncodedInt]
        [SerializableField(26, allowFieldChange: nameof(ClampLoyalty))]
        [SaveFlag(nameof(ShouldSerializeLoyalty), nameof(LoyaltyDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _loyalty;

        private bool ShouldSerializeLoyalty() => _loyalty != MaxLoyalty;

        private int LoyaltyDefaultValue() => MaxLoyalty;

        private bool ClampLoyalty(ref int value)
        {
            value = Math.Clamp(value, 0, MaxLoyalty);
            return true;
        }

        [SerializableField(27)]
        [SaveFlag(nameof(ShouldSerializeCurrentWayPoint))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private WayPoint _currentWayPoint;

        private bool ShouldSerializeCurrentWayPoint() => _currentWayPoint != null;

        [EncodedInt]
        [SerializableField(28)]
        [SaveFlag(nameof(ShouldSerializeHitsMaxSeed), nameof(HitsMaxSeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _hitsMaxSeed = -1;

        private bool ShouldSerializeHitsMaxSeed() => _hitsMaxSeed != -1;

        private int HitsMaxSeedDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(29)]
        [SaveFlag(nameof(ShouldSerializeStamMaxSeed), nameof(StamMaxSeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _stamMaxSeed = -1;

        private bool ShouldSerializeStamMaxSeed() => _stamMaxSeed != -1;

        private int StamMaxSeedDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(30)]
        [SaveFlag(nameof(ShouldSerializeManaMaxSeed), nameof(ManaMaxSeedDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _manaMaxSeed = -1;

        private bool ShouldSerializeManaMaxSeed() => _manaMaxSeed != -1;

        private int ManaMaxSeedDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(31)]
        [SaveFlag(nameof(ShouldSerializeDamageMin), nameof(DamageMinDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _damageMin = -1;

        private bool ShouldSerializeDamageMin() => _damageMin != -1;

        private int DamageMinDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(32)]
        [SaveFlag(nameof(ShouldSerializeDamageMax), nameof(DamageMaxDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _damageMax = -1;

        private bool ShouldSerializeDamageMax() => _damageMax != -1;

        private int DamageMaxDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(33, fieldChanged: nameof(OnResistanceSeedChange))]
        [SaveFlag(nameof(ShouldSerializePhysicalResistanceSeed))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _physicalResistanceSeed;

        private bool ShouldSerializePhysicalResistanceSeed() => _physicalResistanceSeed != 0;

        private void OnResistanceSeedChange(int oldValue, int newValue) => UpdateResistances();

        [EncodedInt]
        [SerializableField(34, fieldChanged: nameof(OnResistanceSeedChange))]
        [SaveFlag(nameof(ShouldSerializeFireResistSeed))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _fireResistSeed;

        private bool ShouldSerializeFireResistSeed() => _fireResistSeed != 0;

        [EncodedInt]
        [SerializableField(35, fieldChanged: nameof(OnResistanceSeedChange))]
        [SaveFlag(nameof(ShouldSerializeColdResistSeed))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _coldResistSeed;

        private bool ShouldSerializeColdResistSeed() => _coldResistSeed != 0;

        [EncodedInt]
        [SerializableField(36, fieldChanged: nameof(OnResistanceSeedChange))]
        [SaveFlag(nameof(ShouldSerializePoisonResistSeed))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _poisonResistSeed;

        private bool ShouldSerializePoisonResistSeed() => _poisonResistSeed != 0;

        [EncodedInt]
        [SerializableField(37, fieldChanged: nameof(OnResistanceSeedChange))]
        [SaveFlag(nameof(ShouldSerializeEnergyResistSeed))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _energyResistSeed;

        private bool ShouldSerializeEnergyResistSeed() => _energyResistSeed != 0;

        [EncodedInt]
        [SerializableField(38)]
        [SaveFlag(nameof(ShouldSerializePhysicalDamage), nameof(PhysicalDamageDefaultValue))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _physicalDamage = 100;

        private bool ShouldSerializePhysicalDamage() => _physicalDamage != 100;

        private int PhysicalDamageDefaultValue() => 100;

        [EncodedInt]
        [SerializableField(39)]
        [SaveFlag(nameof(ShouldSerializeFireDamage))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _fireDamage;

        private bool ShouldSerializeFireDamage() => _fireDamage != 0;

        [EncodedInt]
        [SerializableField(40)]
        [SaveFlag(nameof(ShouldSerializeColdDamage))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _coldDamage;

        private bool ShouldSerializeColdDamage() => _coldDamage != 0;

        [EncodedInt]
        [SerializableField(41)]
        [SaveFlag(nameof(ShouldSerializePoisonDamage))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _poisonDamage;

        private bool ShouldSerializePoisonDamage() => _poisonDamage != 0;

        [EncodedInt]
        [SerializableField(42)]
        [SaveFlag(nameof(ShouldSerializeEnergyDamage))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _energyDamage;

        private bool ShouldSerializeEnergyDamage() => _energyDamage != 0;

        [Tidy]
        [SerializableField(43, setter: "private")]
        [SaveFlag(nameof(ShouldSerializeOwners), nameof(OwnersDefaultValue))]
        private List<Mobile> _owners;

        private bool ShouldSerializeOwners()
        {
            _owners?.Tidy();
            return _owners?.Count > 0;
        }

        private List<Mobile> OwnersDefaultValue() => new();

        [SerializableField(44)]
        [SaveFlag(nameof(ShouldSerializeIsDeadPet))]
        private bool _isDeadPet;

        private bool ShouldSerializeIsDeadPet() => _isDeadPet;

        [SerializableField(45, fieldChanged: nameof(OnBondedChange))]
        [SaveFlag(nameof(ShouldSerializeIsBonded))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _isBonded;

        private bool ShouldSerializeIsBonded() => _isBonded;

        private void OnBondedChange(bool oldValue, bool newValue) => InvalidateProperties();

        [SerializableField(46)]
        [SaveFlag(nameof(ShouldSerializeBondingBegin))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private DateTime _bondingBegin;

        private bool ShouldSerializeBondingBegin() => _bondingBegin != DateTime.MinValue;

        [SerializableField(47)]
        [SaveFlag(nameof(ShouldSerializeOwnerAbandonTime))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private DateTime _ownerAbandonTime;

        private bool ShouldSerializeOwnerAbandonTime() => _ownerAbandonTime != DateTime.MinValue;

        [SerializableField(48)]
        [SaveFlag(nameof(ShouldSerializeHasGeneratedLoot))]
        private bool _hasGeneratedLoot;

        private bool ShouldSerializeHasGeneratedLoot() => _hasGeneratedLoot;

        // Field 49: IsParagon (hand-written property; the setter converts, which must not run at load)
        private bool _isParagon;

        private bool ShouldSerializeIsParagon() => _isParagon;

        [Tidy]
        [SerializableField(50, setter: "private")]
        [SaveFlag(nameof(ShouldSerializeFriends))]
        private List<Mobile> _friends;

        private bool ShouldSerializeFriends()
        {
            _friends?.Tidy();
            return _friends?.Count > 0;
        }

        [SerializableField(51)]
        [SaveFlag(nameof(ShouldSerializeRemoveIfUntamed))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _removeIfUntamed;

        private bool ShouldSerializeRemoveIfUntamed() => _removeIfUntamed;

        [EncodedInt]
        [SerializableField(52)]
        [SaveFlag(nameof(ShouldSerializeRemoveStep))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _removeStep;

        private bool ShouldSerializeRemoveStep() => _removeStep != 0;

        [SerializableField(53, setter: "private")]
        [SaveFlag(nameof(ShouldSerializePendingDeleteTimer))]
        [DeserializeTimer(nameof(DeserializePendingDeleteTimer))]
        private Timer _pendingDeleteTimer;

        // Stabled and controlled pets never resume a delete countdown (legacy parity).
        private bool ShouldSerializePendingDeleteTimer() =>
            _pendingDeleteTimer?.Running == true && !IsStabled && !(_controlled && _controlMaster != null);

        private void DeserializePendingDeleteTimer(TimeSpan delay)
        {
            _pendingDeleteTimer = new DeleteTimer(this, delay);
            _pendingDeleteTimer.Start();
        }

        [SerializableField(54)]
        [SaveFlag(nameof(ShouldSerializeCorpseNameOverride))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _corpseNameOverride;

        private bool ShouldSerializeCorpseNameOverride() => _corpseNameOverride != null;

        // --- Non-serialized state -------------------------------------------------------

        // Herding - forces the mob to walk to a specific location, paced by the movement
        // clock at HerdingMoveSpeed. Thinking is unaffected.
        private IPoint2D _targetLocation;

        private int m_FailedReturnHome; /* return to home failure counter */

        private TimerExecutionToken _healTimerToken;

        private DateTime m_IdleReleaseTime;

        private bool m_IsStabled;
        protected int m_KillersLuck;

        private DateTime m_MLNextShout;

        private List<MLQuest> m_MLQuests;

        private long m_NextAura;

        private long m_NextHealOwnerTime = Core.TickCount;

        private long m_NextHealTime = Core.TickCount;

        private long m_NextRummageTime;

        // On OSI these despawn; we queue a return home instead of deleting.
        private bool m_ReturnQueued;

        protected bool m_Spawning;

        private SkillName m_Teaching = (SkillName)(-1);

        public BaseCreature(
            AIType ai,
            FightMode mode = FightMode.Closest,
            int iRangePerception = DefaultRangePerception,
            int iRangeFight = 1
        )
        {
            _loyalty = MaxLoyalty;

            _currentAI = ai;
            _defaultAI = ai;

            _speedClass = DefaultSpeedClass;

            RangePerception = iRangePerception;
            RangeFight = iRangeFight;

            FightMode = mode;

            GetSpeeds(out _activeSpeed, out _passiveSpeed);
            GetMoveSpeeds(out _activeMoveSpeed, out _passiveMoveSpeed);
            _currentSpeed = _passiveSpeed;

            _team = 0;

            Debug = false;

            _controlled = false;
            _controlMaster = null;
            ControlTarget = null;
            _controlOrder = OrderType.None;

            _tamable = false;

            Owners = new List<Mobile>();

            NextReacquireTime = Core.TickCount + (int)ReacquireDelay.TotalMilliseconds;

            ChangeAIType(AI);

            var speechType = SpeechType;

            speechType?.OnConstruct(this);

            if (IsInvulnerable && !Core.AOS)
            {
                NameHue = 0x35;
            }

            GenerateLoot(true);
        }

        public BaseCreature(Serial serial) : base(serial)
        {
            _speedClass = DefaultSpeedClass;
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
                {
                    return DefaultName;
                }

                return base.Name;
            }
            set => base.Name = value == DefaultName ? null : value;
        }

        public virtual InhumanSpeech SpeechType => null;

        // Deliberately not serialized until the feature is finalized.
        [CommandProperty(AccessLevel.GameMaster)]
        public bool SeeksHome { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool IsStabled
        {
            get => m_IsStabled;
            set
            {
                m_IsStabled = value;
                if (m_IsStabled)
                {
                    StopDeleteTimer();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Mobile StabledBy { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPrisoner { get; set; }

        public virtual bool FollowsAcquireRules => true;

        public virtual Faction FactionAllegiance => null;
        public virtual int FactionSilverWorth => 30;

        public virtual double WeaponAbilityChance => 0.4;

        [SerializableProperty(49, useField: nameof(_isParagon))]
        [SaveFlag(nameof(ShouldSerializeIsParagon))]
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsParagon
        {
            get => _isParagon;
            set
            {
                if (_isParagon == value)
                {
                    return;
                }

                if (value)
                {
                    Paragon.Convert(this);
                }
                else
                {
                    Paragon.UnConvert(this);
                }

                _isParagon = value;

                InvalidateProperties();
                this.MarkDirty();
            }
        }

        public virtual bool HasManaOverride => false;

        public virtual FoodType FavoriteFood => FoodType.Meat;
        public virtual PackInstinct PackInstinct => PackInstinct.None;

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

        //TODO Apply the pub 31 DispelDifficulty tweaks
        // Skill level at which dispel succeeds 50% of the time.
        public virtual double DispelDifficulty => 0.0;

        // 0% at difficulty - focus, 100% at difficulty + focus.
        public virtual double DispelFocus => 20.0;

        public virtual bool DisplayWeight => Backpack is StrongBackpack;

        public virtual bool CanFly => false;

        public virtual bool IsInvulnerable => false;

        public BaseAI AIObject { get; private set; }

        public virtual OppositionGroup OppositionGroup => null;

        public virtual bool IsAnimatedDead
        {
            get
            {
                if (!Summoned)
                {
                    return false;
                }

                var type = GetType();

                var contains = false;

                for (var i = 0; !contains && i < m_AnimateDeadTypes.Length; ++i)
                {
                    contains = type == m_AnimateDeadTypes[i];
                }

                return contains;
            }
        }

        public virtual bool IsNecroFamiliar =>
            Summoned && _controlMaster != null &&
            SummonFamiliarSpell.Table.TryGetValue(_controlMaster, out var bc) && bc == this;

        public virtual bool DeleteCorpseOnDeath => !Core.AOS && _summoned;

        public virtual Mobile ConstantFocus => null;

        public virtual bool DisallowAllMoves => false;

        public virtual bool InitialInnocent => false;

        public virtual bool AlwaysMurderer => false;

        public override bool Murderer => AlwaysMurderer || base.Murderer;

        public virtual bool AlwaysAttackable => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax =>
            HitsMaxSeed <= 0 ? Str : Math.Clamp(HitsMaxSeed + GetStatOffset(StatType.Str), 1, 65000);

        [CommandProperty(AccessLevel.GameMaster)]
        public override int StamMax =>
            StamMaxSeed <= 0 ? Dex : Math.Clamp(StamMaxSeed + GetStatOffset(StatType.Dex), 1, 65000);

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ManaMax =>
            ManaMaxSeed <= 0 ? Int : Math.Clamp(ManaMaxSeed + GetStatOffset(StatType.Int), 1, 65000);

        public virtual bool CanOpenDoors => !Body.IsAnimal && !Body.IsSea;

        public virtual bool CanMoveOverObstacles => Core.AOS || Body.IsMonster;

        public virtual bool CanDestroyObstacles => false;

        // OSI followers were distracted by attacks well into AoS; removed around ML.
        public virtual bool CanBeDistracted => !Core.ML;

        public override bool ShouldCheckStatTimers => false;

        public virtual bool CanAngerOnTame => false;

        protected virtual BaseAI ForcedAI => null;

        [CommandProperty(AccessLevel.GameMaster)]
        public AIType AI
        {
            get => _currentAI;
            set
            {
                _currentAI = value;

                if (_currentAI == AIType.AI_Use_Default)
                {
                    _currentAI = _defaultAI;
                }

                ChangeAIType(_currentAI);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Debug { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile FocusMob { get; set; }

        /// <summary>
        /// How far a chase may stretch before the creature gives up its combatant. Between
        /// RangePerception and this leash it keeps chasing but may switch to closer targets.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int ChaseLeashRange => RangePerception * 2;

        // Herded creatures walk at a fixed standard pace regardless of their own speed
        // (RunUO's forced 0.3, without its TransformMoveDelay inflation to 0.6).
        private const double HerdingMoveSpeed = 0.3;

        [CommandProperty(AccessLevel.GameMaster)]
        public IPoint2D TargetLocation
        {
            get => _targetLocation;
            set => _targetLocation = value;
        }

        /// <summary>
        /// Resolved seconds per step: a verbatim active/passive <see cref="CurrentSpeed"/>
        /// maps to the matching movement value; a bespoke pace stays fused to both clocks.
        /// A herded creature is always driven at <see cref="HerdingMoveSpeed"/>.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public double CurrentMoveSpeed
        {
            get
            {
                if (_targetLocation != null)
                {
                    return HerdingMoveSpeed;
                }

                if (_currentSpeed == _activeSpeed)
                {
                    return _activeMoveSpeed > 0 ? _activeMoveSpeed : _activeSpeed;
                }

                if (_currentSpeed == _passiveSpeed)
                {
                    return _passiveMoveSpeed > 0 ? _passiveMoveSpeed : _passiveSpeed;
                }

                return _currentSpeed;
            }
        }

        [SerializableProperty(16, useField: nameof(_controlMaster))]
        [SaveFlag(nameof(ShouldSerializeControlMaster))]
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile ControlMaster
        {
            get => _controlMaster;
            set
            {
                if (_controlMaster == value || this == value)
                {
                    return;
                }

                RemoveFollowers();
                _controlMaster = value;
                AddFollowers();
                if (_controlMaster != null)
                {
                    StopDeleteTimer();
                }

                Delta(MobileDelta.Noto);
                this.MarkDirty();
            }
        }

        [SerializableProperty(24, useField: nameof(_summonMaster))]
        [SaveFlag(nameof(ShouldSerializeSummonMaster))]
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile SummonMaster
        {
            get => _summonMaster;
            set
            {
                if (_summonMaster == value || this == value)
                {
                    return;
                }

                RemoveFollowers();
                _summonMaster = value;
                AddFollowers();

                Delta(MobileDelta.Noto);
                this.MarkDirty();
            }
        }

        // Re-issuing the current order must still run the order logic (pet commands), so
        // this keeps a hand-written setter with no equality skip.
        [SerializableProperty(19, useField: nameof(_controlOrder))]
        [SaveFlag(nameof(ShouldSerializeControlOrder))]
        [CommandProperty(AccessLevel.GameMaster)]
        public OrderType ControlOrder
        {
            get => _controlOrder;
            set
            {
                var previous = _controlOrder;
                _controlOrder = value;

                AIObject?.OnCurrentOrderChanged(previous);

                InvalidateProperties();

                _controlMaster?.InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardProvoked { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardPacified { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardMaster { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardTarget { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BardEndTime { get; set; }

        [SerializableProperty(21, useField: nameof(_tamable))]
        [SaveFlag(nameof(ShouldSerializeTamable))]
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Tamable
        {
            get => _tamable && !_isParagon;
            set
            {
                _tamable = value;
                this.MarkDirty();
            }
        }

        public virtual bool NoHouseRestrictions => false;
        public virtual bool IsHouseSummonable => false;

        public virtual bool AutoDispel => false;
        public virtual double AutoDispelChance => Core.SE ? .10 : 1.0;

        public virtual bool IsScaryToPets => false;
        public virtual bool IsScaredOfScaryThings => true;

        public virtual bool CanRummageCorpses => false;

        public virtual bool DeleteOnRelease => _summoned;

        public virtual bool CanDrop => IsBonded;

        public virtual int TreasureMapLevel => -1;

        public virtual bool IgnoreYoungProtection => false;

        public bool NoKillAwards { get; set; }

        public virtual bool GivesMLMinorArtifact => false;

        // Reacquire only every ReacquireDelay, when attacked, or (for some creatures) on
        // seeing movement - OSI parity and a CPU saver.
        public long NextReacquireTime { get; set; }

        public virtual TimeSpan ReacquireDelay => TimeSpan.FromSeconds(10.0);
        public virtual bool ReacquireOnMovement => false;
        public virtual bool AcquireOnApproach => _isParagon;
        public virtual int AcquireOnApproachRange => 10;

        public static bool Summoning { get; set; }

        public virtual bool IsDispellable => Summoned && !IsAnimatedDead;

        // If they are following a waypoint, they'll continue to follow it even if players aren't around
        public virtual bool PlayerRangeSensitive => CurrentWayPoint == null;

        public virtual bool ReturnsToHome =>
            SeeksHome && Home != Point3D.Zero && !m_ReturnQueued && !Controlled && !Summoned;

        public virtual bool CanGiveMLQuest => MLQuests.Count != 0;
        public virtual bool StaticMLQuester => true;

        public virtual bool CanShout => false;

        public static bool BondingEnabled { get; private set; }

        public virtual bool IsBondable => BondingEnabled && !Summoned;
        public virtual TimeSpan BondingDelay => TimeSpan.FromDays(7.0);
        public virtual TimeSpan BondingAbandonDelay => TimeSpan.FromDays(1.0);

        public override bool CanRegenHits => !IsDeadPet && !Summoned && base.CanRegenHits;
        public override bool CanRegenStam => !IsParagon && !IsDeadPet && base.CanRegenStam;
        public override bool CanRegenMana => !IsDeadPet && base.CanRegenMana;

        public override bool IsDeadBondedPet => IsDeadPet;

        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner MySpawner => Spawner as Spawner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile LastOwner
        {
            get
            {
                if (Owners == null || Owners.Count == 0)
                {
                    return null;
                }

                return Owners[^1];
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan DeleteTimeLeft
        {
            get
            {
                if (_pendingDeleteTimer?.Running == true)
                {
                    return _pendingDeleteTimer.Next - Core.Now;
                }

                return TimeSpan.Zero;
            }
        }

        public override int BasePhysicalResistance => _physicalResistanceSeed;
        public override int BaseFireResistance => _fireResistSeed;
        public override int BaseColdResistance => _coldResistSeed;
        public override int BasePoisonResistance => _poisonResistSeed;
        public override int BaseEnergyResistance => _energyResistSeed;

        [CommandProperty(AccessLevel.GameMaster)]
        public int ChaosDamage { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DirectDamage { get; set; }

        public virtual bool BreathImmune => false;

        public virtual bool CanFlee => !_isParagon;

        public DateTime EndFleeTime { get; set; }

        public virtual bool AllowNewPetFriend => Friends == null || Friends.Count < 5;

        public virtual Ethic EthicAllegiance => null;

        public virtual int Feathers => 0;
        public virtual int Wool => 0;

        public virtual MeatType MeatType => MeatType.Ribs;
        public virtual int Meat => 0;

        public virtual int Hides => 0;
        public virtual HideType HideType => HideType.Regular;

        public virtual int Scales => 0;
        public virtual ScaleType ScaleType => ScaleType.Red;

        public virtual bool CanTeach => false;

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

        public bool IsHealing => _healTimerToken.Running;

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

        public HonorContext ReceivedHonorContext { get; set; }

        public List<MLQuest> MLQuests =>
            (m_MLQuests ??= StaticMLQuester ? MLQuestSystem.FindQuestList(GetType()) : ConstructQuestList()) ?? MLQuestSystem.EmptyList;

        public virtual MonsterAbility[] GetMonsterAbilities() => null;

        private MonsterAbilityTrigger _activeTriggers;

        public virtual MonsterAbility GetAbility(MonsterAbilityType type)
        {
            var abilities = GetMonsterAbilities();

            if (abilities == null)
            {
                return null;
            }

            for (var i = 0; i < abilities.Length; i++)
            {
                var ability = abilities[i];
                if (ability is MonsterAbilityGroup group)
                {
                    ability = group.GetAbilityWithType(type);
                    if (ability != null)
                    {
                        return ability;
                    }
                }
                else if (ability.AbilityType == type)
                {
                    return ability;
                }
            }

            return null;
        }

        public virtual bool HasAbility(MonsterAbility ability)
        {
            if (ability == null)
            {
                return false;
            }

            var abilities = GetMonsterAbilities();

            if (abilities == null)
            {
                return false;
            }

            for (var i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] == ability || (ability as MonsterAbilityGroup)?.HasAbility(ability) == true)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool TriggerAbility(MonsterAbilityTrigger trigger, Mobile defender)
        {
            if ((_activeTriggers & trigger) != 0)
            {
                return false;
            }

            var abilities = GetMonsterAbilities();

            if (abilities == null)
            {
                return false;
            }

            _activeTriggers |= trigger;
            try
            {
                var triggered = false;
                for (var i = 0; i < abilities.Length; i++)
                {
                    var ability = abilities[i];
                    if (ability.CanTrigger(this, trigger))
                    {
                        ability.Trigger(trigger, this, defender);
                        triggered = true;
                    }
                }

                return triggered;
            }
            finally
            {
                _activeTriggers &= ~trigger;
            }
        }

        public virtual void TriggerAbilityMove(MonsterAbilityTrigger trigger, Mobile defender, Direction d)
        {
            var abilities = GetMonsterAbilities();

            if (abilities == null)
            {
                return;
            }

            for (var i = 0; i < abilities.Length; i++)
            {
                var ability = abilities[i];
                if (ability.CanTrigger(this, trigger))
                {
                    ability.Move(this, d);
                }
            }
        }

        public virtual void TriggerAbilityAlterDamage(MonsterAbilityTrigger trigger, Mobile defender, ref int damage)
        {
            var abilities = GetMonsterAbilities();

            if (abilities == null)
            {
                return;
            }

            for (var i = 0; i < abilities.Length; i++)
            {
                var ability = abilities[i];
                if (ability.CanTrigger(this, trigger))
                {
                    if ((trigger & MonsterAbilityTrigger.GiveMeleeDamage) != 0)
                    {
                        ability.AlterMeleeDamageTo(this, defender, ref damage);
                    }

                    if ((trigger & MonsterAbilityTrigger.TakeMeleeDamage) != 0)
                    {
                        ability.AlterMeleeDamageFrom(this, defender, ref damage);
                    }

                    if ((trigger & MonsterAbilityTrigger.GiveSpellDamage) != 0)
                    {
                        ability.AlterSpellDamageTo(this, defender, ref damage);
                    }

                    if ((trigger & MonsterAbilityTrigger.TakeSpellDamage) != 0)
                    {
                        ability.AlterSpellDamageFrom(this, defender, ref damage);
                    }
                }
            }
        }

        public virtual void TriggerAbilityAlterDamageScalar(
            MonsterAbilityTrigger trigger,
            Mobile defender,
            ref double scalar
        )
        {
            var abilities = GetMonsterAbilities();

            if (abilities == null)
            {
                return;
            }

            for (var i = 0; i < abilities.Length; i++)
            {
                var ability = abilities[i];
                if (ability.CanTrigger(this, trigger))
                {
                    if ((trigger & MonsterAbilityTrigger.GiveSpellDamage) != 0)
                    {
                        ability.AlterSpellDamageScalarTo(this, defender, ref scalar);
                    }

                    if ((trigger & MonsterAbilityTrigger.TakeSpellDamage) != 0)
                    {
                        ability.AlterSpellDamageScalarFrom(this, defender, ref scalar);
                    }
                }
            }
        }

        public virtual WeaponAbility GetWeaponAbility() => null;

        public virtual bool IsEnemy(Mobile m)
        {
            if (OppositionGroup?.IsEnemy(this, m) == true)
            {
                return true;
            }

            if (m is BaseGuard)
            {
                return false;
            }

            if (GetFactionAllegiance(m) == Allegiance.Ally)
            {
                return false;
            }

            var ourEthic = EthicAllegiance;
            var pl = Ethics.Player.Find(m, true);

            if (pl?.IsShielded == true && (ourEthic == null || ourEthic == pl.Ethic))
            {
                return false;
            }

            if (VirtueSystem.GetVirtues(m as PlayerMobile)?.HonorActive == true)
            {
                return false;
            }

            if (m is not BaseCreature c || m is MilitiaFighter)
            {
                return true;
            }

            if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)))
            {
                return false;
            }

            if (_team != c.Team || FightMode == FightMode.Evil && m.Karma < 0 || c.FightMode == FightMode.Evil && Karma < 0)
            {
                return true;
            }

            var master = GetMaster();
            var cMaster = c.GetMaster();

            if (master == null)
            {
                // Non-summons will attack summons of non-NPCs
                return cMaster != null && cMaster is not BaseCreature;
            }

            // Summons will attack others summons, if they are enemies with their master
            // Pets will attack non-summons, but not other summons (legacy logic)
            return (master as BaseCreature)?.IsEnemy(cMaster ?? m) ?? cMaster == null;
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
            {
                Animate(10, 5, 1, true, false, 0);
            }
            else if (Body.IsMonster)
            {
                Animate(18, 5, 1, true, false, 0);
            }

            Loyalty -= 3;
            return false;
        }

        public virtual bool CanBeControlledBy(Mobile m) => GetControlChance(m) > 0.0;

        public virtual double GetControlChance(Mobile m, bool useBaseSkill = false)
        {
            if (MinTameSkill <= 29.1 || _summoned || m.AccessLevel >= AccessLevel.GameMaster)
            {
                return 1.0;
            }

            var minTameSkill = MinTameSkill;

            if (minTameSkill > -24.9 && AnimalTaming.CheckMastery(m, this))
            {
                minTameSkill = -24.9;
            }

            var taming = useBaseSkill
                ? m.Skills.AnimalTaming.BaseFixedPoint
                : m.Skills.AnimalTaming.Fixed;
            var lore = useBaseSkill
                ? m.Skills.AnimalLore.BaseFixedPoint
                : m.Skills.AnimalLore.Fixed;

            int bonus;

            if (Core.ML)
            {
                var skillBonus = taming - (int)(minTameSkill * 10);
                var loreBonus = lore - (int)(minTameSkill * 10);

                var skillMod = 6;
                var loreMod = 6;

                if (skillBonus < 0)
                {
                    skillMod = 28;
                }

                if (loreBonus < 0)
                {
                    loreMod = 14;
                }

                skillBonus *= skillMod;
                loreBonus *= loreMod;

                bonus = (skillBonus + loreBonus) / 2;
            }
            else
            {
                var difficulty = (int)(minTameSkill * 10);
                var weighted = (taming * 4 + lore) / 5;
                bonus = weighted - difficulty;

                if (bonus <= 0)
                {
                    bonus *= 14;
                }
                else
                {
                    bonus *= 6;
                }
            }

            var chance = Math.Clamp(700 + bonus, 220, 990);

            chance -= (MaxLoyalty - _loyalty) * 10;

            return chance / 1000.0;
        }

        public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
        {
            var oldHits = Hits;

            // Blood oath reflects the original damage the attacker dealt, before other modifiers.
            var hasBloodOath = from != null && BloodOathSpell.GetBloodOath(from) == this;
            var reflectedDamage = hasBloodOath ? amount : 0;

            if (Core.AOS && !Summoned && Controlled && Utility.RandomDouble() < 0.2)
            {
                amount = (int)(amount * BonusPetDamageScalar);
            }

            if (EvilOmenSpell.EndEffect(this))
            {
                amount = (int)(amount * 1.25);
            }

            if (hasBloodOath)
            {
                amount = (int)(amount * 1.2);
            }

            base.Damage(amount, from, informMount);

            // If the blood oath caster will die then damage is not reflected back to the attacker.
            if (hasBloodOath && Alive && !Deleted && !IsDeadBondedPet)
            {
                // Reflect the original damage back to the attacker, attributed to the caster.
                // The caster is a creature, so the Publish 48 (SA+) resist mitigation applies.
                from.Damage(
                    BloodOathSpell.ComputeReflectedDamage(reflectedDamage, from.Skills.MagicResist.Value, Core.SA),
                    this
                );
            }

            if (SubdueBeforeTame && !Controlled && oldHits > HitsMax / 10 && Hits <= HitsMax / 10)
            {
                // * The creature has been beaten into subjugation! *
                PublicOverheadMessage(MessageType.Regular, 0x3B2, 1080057);
            }
        }

        public override void SetLocation(Point3D newLocation, bool isTeleport)
        {
            base.SetLocation(newLocation, isTeleport);

            if (isTeleport)
            {
                AIObject?.OnTeleported();
            }
        }

        public override void OnBeforeSpawn(Point3D location, Map m)
        {
            if (Paragon.CheckConvert(this, location, m))
            {
                IsParagon = true;
            }

            base.OnBeforeSpawn(location, m);
        }

        public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (!Alive || IsDeadPet)
            {
                return ApplyPoisonResult.Immune;
            }

            if (EvilOmenSpell.EndEffect(this))
            {
                poison = PoisonImpl.IncreaseLevel(poison);
            }

            var result = base.ApplyPoison(from, poison);

            if (from != null && result == ApplyPoisonResult.Poisoned && PoisonTimer is PoisonImpl.PoisonTimer timer)
            {
                timer.From = from;
            }

            return result;
        }

        public override bool CheckPoisonImmunity(Mobile from, Poison poison) =>
            base.CheckPoisonImmunity(from, poison) ||
            (_isParagon ? PoisonImpl.IncreaseLevel(PoisonImmune) : PoisonImmune)?.Level >= poison.Level;

        public void Unpacify()
        {
            BardEndTime = Core.Now;
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
            {
                Unpacify();
            }

            int disruptThreshold;
            if (!Core.AOS)
            {
                disruptThreshold = 0;
            }
            else if (from?.Player == true)
            {
                disruptThreshold = 18;
            }
            else
            {
                disruptThreshold = 25;
            }

            if (amount > disruptThreshold)
            {
                var c = BandageContext.GetContext(this);

                c?.Slip();
            }

            Confidence.StopRegenerating(this);

            StaminaSystem.FatigueOnDamage(this, amount);

            var speechType = SpeechType;

            if (speechType != null && !willKill)
            {
                speechType.OnDamage(this, amount);
            }

            ReceivedHonorContext?.OnTargetDamaged(from, amount);

            if (!willKill && CanBeDistracted && ControlOrder == OrderType.Follow)
            {
                CheckDistracted(from);
            }

            base.OnDamage(amount, from, willKill);
        }

        public virtual void OnDamagedBySpell(Mobile from, int damage)
        {
            if (CanBeDistracted && ControlOrder == OrderType.Follow)
            {
                CheckDistracted(from);
            }

            TriggerAbility(MonsterAbilityTrigger.TakeSpellDamage, from);
        }

        public virtual void OnDamageSpell(Mobile defender, int damage)
        {
            TriggerAbility(MonsterAbilityTrigger.GiveSpellDamage, defender);
        }

        public virtual void OnHarmfulSpell(Mobile from)
        {
        }

        public virtual void CheckReflect(Mobile caster, ref bool reflect)
        {
        }

        public virtual void OnCarve(Mobile from, Corpse corpse, Item with)
        {
            var feathers = Feathers;
            var wool = Wool;
            var meat = Meat;
            var hides = Hides;
            var scales = Scales;

            if (feathers == 0 && wool == 0 && meat == 0 && hides == 0 && scales == 0 || Summoned || IsBonded ||
                corpse.Animated)
            {
                if (corpse.Animated)
                {
                    corpse.SendLocalizedMessageTo(from, 500464); // Use this on corpses to carve away meat and hide
                }
                else
                {
                    from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
                }
            }
            else
            {
                if (Core.ML && from.Race == Race.Human)
                {
                    hides = (int)Math.Ceiling(hides * 1.1); // 10% bonus only applies to hides, ore & logs
                }

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
                    {
                        corpse.AddCarvedItem(new RawRibs(meat), from);
                    }
                    else if (MeatType == MeatType.Bird)
                    {
                        corpse.AddCarvedItem(new RawBird(meat), from);
                    }
                    else if (MeatType == MeatType.LambLeg)
                    {
                        corpse.AddCarvedItem(new RawLambLeg(meat), from);
                    }

                    from.SendLocalizedMessage(500467); // You carve some meat, which remains on the corpse.
                }

                if (hides != 0)
                {
                    var holding = from.Weapon as Item;

                    if (Core.AOS && holding is SkinningKnife)
                    {
                        var leather = HideType switch
                        {
                            HideType.Regular => (Item)new Leather(hides),
                            HideType.Spined  => new SpinedLeather(hides),
                            HideType.Horned  => new HornedLeather(hides),
                            HideType.Barbed  => new BarbedLeather(hides),
                            _                => null
                        };

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
                                    1073555
                                ); // You skin it and place the cut-up hides in your backpack.
                            }
                        }
                    }
                    else
                    {
                        if (HideType == HideType.Regular)
                        {
                            corpse.DropItem(new Hides(hides));
                        }
                        else if (HideType == HideType.Spined)
                        {
                            corpse.DropItem(new SpinedHides(hides));
                        }
                        else if (HideType == HideType.Horned)
                        {
                            corpse.DropItem(new HornedHides(hides));
                        }
                        else if (HideType == HideType.Barbed)
                        {
                            corpse.DropItem(new BarbedHides(hides));
                        }

                        from.SendLocalizedMessage(500471); // You skin it, and the hides are now in the corpse.
                    }
                }

                if (scales != 0)
                {
                    var sc = ScaleType;

                    switch (sc)
                    {
                        case ScaleType.Red:
                            {
                                corpse.AddCarvedItem(new RedScales(scales), from);
                                break;
                            }
                        case ScaleType.Yellow:
                            {
                                corpse.AddCarvedItem(new YellowScales(scales), from);
                                break;
                            }
                        case ScaleType.Black:
                            {
                                corpse.AddCarvedItem(new BlackScales(scales), from);
                                break;
                            }
                        case ScaleType.Green:
                            {
                                corpse.AddCarvedItem(new GreenScales(scales), from);
                                break;
                            }
                        case ScaleType.White:
                            {
                                corpse.AddCarvedItem(new WhiteScales(scales), from);
                                break;
                            }
                        case ScaleType.Blue:
                            {
                                corpse.AddCarvedItem(new BlueScales(scales), from);
                                break;
                            }
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

                    from.SendLocalizedMessage(1079284); // You cut away some scales, but they remain on the corpse.
                }

                corpse.Carved = true;

                if (corpse.IsCriminalAction(from))
                {
                    from.CriminalAction(true);
                }
            }
        }

        // Pre-codegen loads only (versions 0-22); post-codegen bumps use MigrateFrom.
        private void Deserialize(IGenericReader reader, int version)
        {

            _currentAI = (AIType)reader.ReadInt();
            _defaultAI = (AIType)reader.ReadInt();

            _rangePerception = reader.ReadInt();
            _rangeFight = reader.ReadInt();

            _team = reader.ReadInt();

            _activeSpeed = reader.ReadDouble();
            _passiveSpeed = reader.ReadDouble();
            _currentSpeed = reader.ReadDouble();

            _home.X = reader.ReadInt();
            _home.Y = reader.ReadInt();
            _home.Z = reader.ReadInt();

            if (version >= 1)
            {
                _rangeHome = reader.ReadInt();

                if (version < 20)
                {
                    // Spell Attacks
                    var iCount = reader.ReadInt(); // Count
                    for (var i = 0; i < iCount; i++)
                    {
                        reader.ReadString(); // Spell Type
                    }

                    // Spell Defenses
                    iCount = reader.ReadInt(); // Count
                    for (var i = 0; i < iCount; i++)
                    {
                        reader.ReadString(); // Spell Type
                    }
                }
            }
            else
            {
                _rangeHome = 0;
            }

            if (version >= 2)
            {
                _fightMode = (FightMode)reader.ReadInt();

                _controlled = reader.ReadBool();
                _controlMaster = reader.ReadEntity<Mobile>();
                _controlTarget = reader.ReadEntity<Mobile>();
                _controlDest = reader.ReadPoint3D();
                _controlOrder = (OrderType)reader.ReadInt();

                _minTameSkill = reader.ReadDouble();

                if (version < 9)
                {
                    reader.ReadDouble();
                }

                _tamable = reader.ReadBool();
                _summoned = reader.ReadBool();

                if (_summoned)
                {
                    // The UnsummonTimer is restarted in AfterDeserialization.
                    _summonEnd = version >= 21 ? reader.ReadAnchoredTime() : reader.ReadDeltaTime();
                }

                _controlSlots = reader.ReadInt();
            }
            else
            {
                _fightMode = FightMode.Closest;

                _controlled = false;
                _controlMaster = null;
                _controlTarget = null;
                _controlOrder = OrderType.None;
            }

            if (version >= 3)
            {
                _loyalty = reader.ReadInt();
            }
            else
            {
                _loyalty = MaxLoyalty;
            }

            if (version >= 4)
            {
                _currentWayPoint = reader.ReadEntity<WayPoint>();
            }

            if (version >= 5)
            {
                _summonMaster = reader.ReadEntity<Mobile>();
            }

            if (version >= 6)
            {
                _hitsMaxSeed = reader.ReadInt();
                _stamMaxSeed = reader.ReadInt();
                _manaMaxSeed = reader.ReadInt();
                _damageMin = reader.ReadInt();
                _damageMax = reader.ReadInt();
            }

            if (version >= 7)
            {
                _physicalResistanceSeed = reader.ReadInt();
                _physicalDamage = reader.ReadInt();

                _fireResistSeed = reader.ReadInt();
                _fireDamage = reader.ReadInt();

                _coldResistSeed = reader.ReadInt();
                _coldDamage = reader.ReadInt();

                _poisonResistSeed = reader.ReadInt();
                _poisonDamage = reader.ReadInt();

                _energyResistSeed = reader.ReadInt();
                _energyDamage = reader.ReadInt();
            }

            if (version >= 8)
            {
                _owners = reader.ReadEntityList<Mobile>();
            }
            else
            {
                _owners = new List<Mobile>();
            }

            if (version >= 10)
            {
                _isDeadPet = reader.ReadBool();
                _isBonded = reader.ReadBool();
                _bondingBegin = reader.ReadDateTime();
                _ownerAbandonTime = reader.ReadDateTime();
            }

            _hasGeneratedLoot = version < 11 || reader.ReadBool();

            _isParagon = version >= 12 && reader.ReadBool();

            if (version >= 13 && reader.ReadBool())
            {
                _friends = reader.ReadEntityList<Mobile>();
            }
            else if (version < 13 && _controlOrder >= OrderType.Unfriend)
            {
                ++_controlOrder;
            }

            if (version < 16 && Loyalty != MaxLoyalty)
            {
                Loyalty *= 10;
            }

            if (version >= 14)
            {
                _removeIfUntamed = reader.ReadBool();
                _removeStep = reader.ReadInt();
            }

            var deleteTime = TimeSpan.Zero;

            if (version >= 17)
            {
                deleteTime = reader.ReadTimeSpan();
            }

            if (deleteTime > TimeSpan.Zero || LastOwner != null && !Controlled && !IsStabled)
            {
                if (deleteTime == TimeSpan.Zero)
                {
                    deleteTime = TimeSpan.FromDays(3.0);
                }

                _pendingDeleteTimer = new DeleteTimer(this, deleteTime);
                _pendingDeleteTimer.Start();
            }

            if (version >= 18)
            {
                _corpseNameOverride = reader.ReadString();
            }

            if (version >= 19)
            {
                _homeMap = reader.ReadMap();
            }

            if (version >= 22)
            {
                _activeMoveSpeed = reader.ReadDouble();
                _passiveMoveSpeed = reader.ReadDouble();
            }
            else
            {
                MigrateMoveSpeeds();
            }

            if (version <= 14 && _isParagon && Hue == 0x31)
            {
                Hue = Paragon.Hue; // Paragon hue fixed, should now be 0x501.
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Core.AOS && NameHue == 0x35)
            {
                NameHue = -1;
            }

            if (_summoned)
            {
                new UnsummonTimer(this, _summonEnd - Core.Now).Start();
            }

            // Abandoned-pet fallback: a pet with a former owner but no persisted delete
            // countdown still despawns (legacy loads restore their own timer above).
            if (_pendingDeleteTimer == null && LastOwner != null && !_controlled && !IsStabled)
            {
                _pendingDeleteTimer = new DeleteTimer(this, TimeSpan.FromDays(3.0));
                _pendingDeleteTimer.Start();
            }

            CheckStatTimers();

            ChangeAIType(_currentAI);

            AddFollowers();

            if (IsAnimatedDead)
            {
                AnimateDeadSpell.Register(_summonMaster, this);
            }
        }

        public virtual bool IsHumanInTown() => Body.IsHuman && Region.IsPartOf<GuardedRegion>();

        public virtual bool CheckGold(Mobile from, Item dropped) => dropped is Gold gold && OnGoldGiven(from, gold);

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

                var oldSpeechHue = SpeechHue;

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

        public virtual bool OverrideBondingReqs() => false;

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (CheckFeed(from, dropped))
            {
                return true;
            }

            if (CheckGold(from, dropped))
            {
                return true;
            }

            // Happens for all questers, even those with nothing to offer right now.
            if (MLQuestSystem.Enabled && CanGiveMLQuest && from is PlayerMobile mobile)
            {
                // You need to mark your quest items so I don't take the wrong object.  Then speak to me.
                MLQuestSystem.Tell(this, mobile, 1074893);
                return false;
            }

            return base.OnDragDrop(from, dropped);
        }

        public void ChangeAIType(AIType newAI)
        {
            AIObject?.AITimer.Stop();

            if (ForcedAI != null)
            {
                AIObject = ForcedAI;
                return;
            }

            AIObject = newAI switch
            {
                AIType.AI_Melee   => new MeleeAI(this),
                AIType.AI_Animal  => new AnimalAI(this),
                AIType.AI_Berserk => new BerserkAI(this),
                AIType.AI_Archer  => new ArcherAI(this),
                AIType.AI_Healer  => new HealerAI(this),
                AIType.AI_Vendor  => new VendorAI(this),
                AIType.AI_Mage    => new MageAI(this),
                AIType.AI_Predator =>
                    //TODO Implement PredatorAI
                    new MeleeAI(this),
                AIType.AI_Thief => new ThiefAI(this),
                _               => null
            };
        }

        public virtual void OnTeamChange()
        {
        }

        public override void RevealingAction()
        {
            InvisibilitySpell.StopTimer(this);

            base.RevealingAction();
        }

        public void RemoveFollowers()
        {
            var master = _controlMaster ?? _summonMaster;
            if (master != null)
            {
                master.Followers -= Math.Min(ControlSlots, master.Followers);
                if (master is PlayerMobile pm)
                {
                    pm.RemoveFollower(this);
                    pm.AutoStabled?.Remove(this);
                }
            }
        }

        public void AddFollowers()
        {
            var master = _controlMaster ?? _summonMaster;
            if (master != null)
            {
                master.Followers += ControlSlots;
                (master as PlayerMobile)?.AddFollower(this);
            }
        }

        public virtual void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            if (AutoDispel && attacker is BaseCreature creature && creature.IsDispellable &&
                AutoDispelChance > Utility.RandomDouble())
            {
                Dispel(creature);
            }

            TriggerAbility(MonsterAbilityTrigger.TakeMeleeDamage, attacker);
        }

        public override bool Move(Direction d)
        {
            if (!base.Move(d))
            {
                return false;
            }

            TriggerAbilityMove(MonsterAbilityTrigger.Movement, this, d);
            return true;
        }

        public virtual void Dispel(Mobile m)
        {
            Effects.SendLocationParticles(
                EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration),
                0x3728,
                8,
                20,
                5042
            );
            Effects.PlaySound(m, 0x201);

            m.Delete();
        }

        public virtual void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            var p = _isParagon ? PoisonImpl.IncreaseLevel(HitPoison) : HitPoison;

            if (p != null && HitPoisonChance >= Utility.RandomDouble())
            {
                defender.ApplyPoison(this, p);

                if (Controlled)
                {
                    CheckSkill(SkillName.Poisoning, 0, Skills.Poisoning.Cap);
                }
            }

            if (AutoDispel && defender is BaseCreature creature && creature.IsDispellable &&
                AutoDispelChance > Utility.RandomDouble())
            {
                Dispel(creature);
            }

            TriggerAbility(MonsterAbilityTrigger.GiveMeleeDamage, defender);
        }

        public override void OnAfterDelete()
        {
            if (AIObject != null)
            {
                AIObject.AITimer?.Stop();
                AIObject = null;
            }

            if (_pendingDeleteTimer != null)
            {
                _pendingDeleteTimer.Stop();
                _pendingDeleteTimer = null;
            }

            FocusMob = null;

            if (IsAnimatedDead)
            {
                AnimateDeadSpell.Unregister(_summonMaster, this);
            }

            if (Summoned && SummonMaster != null)
            {
                SummonFamiliarSpell.Unregister(SummonMaster, this);
            }

            if (MLQuestSystem.Enabled)
            {
                MLQuestSystem.HandleDeletion(this);
            }

            UnsummonTimer.StopTimer(this);
            StaminaSystem.RemoveEntry(this as IHasSteps);

            base.OnAfterDelete();
        }

        public virtual double GetFightModeRanking(Mobile m, FightMode acqType, bool bPlayerOnly)
        {
            if (bPlayerOnly && !m.Player)
            {
                return double.MinValue;
            }

            return acqType switch
            {
                FightMode.Strongest => m.Skills.Tactics.Value + m.Str, // returns strongest mobile
                FightMode.Weakest   => -m.Hits,                        // returns weakest mobile
                _                   => -this.GetDistanceToSqrt(m)
            };
        }

        // Turn: negative = left, positive = right.
        public virtual void Turn(int iTurnSteps)
        {
            var v = (int)Direction;

            Direction = (Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80));
        }

        public virtual void TurnInternal(int iTurnSteps)
        {
            var v = (int)Direction;

            SetDirection((Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80)));
        }

        public bool IsHurt() => Hits != HitsMax;

        public double GetHomeDistance() => this.GetDistanceToSqrt(_home);

        public virtual int GetTeamSize(int iRange)
        {
            var iCount = 0;

            foreach (var m in GetMobilesInRange(iRange))
            {
                if (m != this && m is BaseCreature creature && !creature.Deleted && creature.Team == Team &&
                    CanSee(creature))
                {
                    iCount++;
                }
            }

            return iCount;
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);

            if (ControlMaster != null && NotorietyHandlers.CheckAggressor(ControlMaster.Aggressors, aggressor))
            {
                aggressor.Aggressors.Add(AggressorInfo.Create(this, aggressor, true));
            }

            var ct = _controlOrder;

            if (AIObject != null)
            {
                if (!Core.ML || ct != OrderType.Follow && ct != OrderType.Stop && ct != OrderType.Stay)
                {
                    AIObject.OnAggressiveAction(aggressor);
                }
                else
                {
                    AIObject.DebugSay("I'm being attacked but my master told me not to fight.");
                    Warmode = false;
                    return;
                }
            }

            StopFlee();

            ForceReacquire();

            if (!IsEnemy(aggressor))
            {
                var pl = Ethics.Player.Find(aggressor, true);

                if (pl?.IsShielded == true)
                {
                    pl.FinishShield();
                }
            }

            if (aggressor.ChangingCombatant && (_controlled || _summoned) &&
                (ct == OrderType.Come || !Core.ML && ct == OrderType.Stay || ct is OrderType.Stop or OrderType.None or OrderType.Follow))
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
            {
                return !Alive || !creature.Alive || IsDeadBondedPet || creature.IsDeadBondedPet ||
                       Hidden && AccessLevel > AccessLevel.Player;
            }

            if (Region.IsPartOf<SafeZone>() && m is PlayerMobile pm &&
                (pm.DuelContext?.Started != true || pm.DuelContext.Finished ||
                 pm.DuelPlayer?.Eliminated != false))
            {
                return true;
            }

            return base.OnMoveOver(m);
        }

        public virtual void AddCustomContextEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);

            if (Commandable)
            {
                AIObject?.GetContextMenuEntries(from, ref list);
            }

            if (_tamable && !_controlled && from.Alive)
            {
                list.Add(new TameEntry(from.Female ? AllowFemaleTamer : AllowMaleTamer));
            }

            AddCustomContextEntries(from, ref list);

            if (CanTeach && from.Alive)
            {
                var ourSkills = Skills;
                var theirSkills = from.Skills;

                for (var i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                {
                    var skill = ourSkills[i];
                    var theirSkill = theirSkills[i];

                    if (skill?.Base >= 60.0 && CheckTeach(skill.SkillName, from))
                    {
                        var toTeach = skill.BaseFixedPoint / 3;

                        if (toTeach > 420)
                        {
                            toTeach = 420;
                        }

                        list.Add(new TeachEntry((SkillName)i, toTeach > theirSkill.BaseFixedPoint));
                    }
                }
            }
        }

        public override bool HandlesOnSpeech(Mobile from) =>
            (SpeechType?.Flags & IHSFlags.OnSpeech) != 0 && from.InRange(this, 3) ||
            AIObject?.HandlesOnSpeech(from) == true && from.InRange(this, RangePerception);

        public override void OnSpeech(SpeechEventArgs e)
        {
            var speechType = SpeechType;

            if (speechType?.OnSpeech(this, e.Mobile, e.Speech) == true)
            {
                e.Handled = true;
            }
            else if (!e.Handled && AIObject != null && e.Mobile.InRange(this, RangePerception))
            {
                AIObject.OnSpeech(e);
            }
        }

        public override bool IsHarmfulCriminal(Mobile target) =>
            (!Controlled || target != _controlMaster) && (!Summoned || target != _summonMaster) &&
            (target is not BaseCreature { InitialInnocent: true } creature || creature.Controlled) &&
            (target is not PlayerMobile mobile || mobile.PermaFlags.Count <= 0) && base.IsHarmfulCriminal(target);

        public override void CriminalAction(bool message)
        {
            base.CriminalAction(message);

            if (Controlled || Summoned)
            {
                if (_controlMaster?.Player == true)
                {
                    _controlMaster.CriminalAction(false);
                }
                else if (_summonMaster?.Player == true)
                {
                    _summonMaster.CriminalAction(false);
                }
            }
        }

        public override void DoHarmful(Mobile target, bool indirect = false)
        {
            base.DoHarmful(target, indirect);

            if (target == this || target == _controlMaster || target == _summonMaster || !Controlled && !Summoned)
            {
                return;
            }

            var list = Aggressors;

            for (var i = 0; i < list.Count; ++i)
            {
                var ai = list[i];

                if (ai.Attacker == target)
                {
                    return;
                }
            }

            list = Aggressed;

            for (var i = 0; i < list.Count; ++i)
            {
                var ai = list[i];

                if (ai.Defender == target)
                {
                    var master = GetMaster();
                    if (master?.Player == true && master.CanBeHarmful(target, false))
                    {
                        master.DoHarmful(target, true);
                    }

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
            {
                return false; // in combat, not idling
            }

            if (m_IdleReleaseTime > DateTime.MinValue)
            {
                if (Core.Now >= m_IdleReleaseTime)
                {
                    m_IdleReleaseTime = DateTime.MinValue;
                    return false; // idle is over
                }

                return true; // still idling
            }

            if (Utility.Random(100) < 95)
            {
                return false; // chose not to enter the idle state
            }

            var idleSeconds = Utility.RandomMinMax(NPCSpeeds.MinIdleSeconds, NPCSpeeds.MaxIdleSeconds);
            m_IdleReleaseTime = Core.Now + TimeSpan.FromSeconds(idleSeconds);

            if (Body.IsHuman)
            {
                CheckedAnimate(Utility.RandomBool() ? 5 : 6, 5, 1, true, false, 1);
            }
            else if (Body.IsAnimal)
            {
                switch (Utility.Random(3))
                {
                    case 0:
                        {
                            CheckedAnimate(3, 3, 1, true, false, 1);
                            break;
                        }
                    case 1:
                        {
                            CheckedAnimate(9, 5, 1, true, false, 1);
                            break;
                        }
                    case 2:
                        {
                            CheckedAnimate(10, 5, 1, true, false, 1);
                            break;
                        }
                }
            }
            else if (Body.IsMonster)
            {
                CheckedAnimate(Utility.RandomBool() ? 17 : 18, 5, 1, true, false, 1);
            }

            PlaySound(GetIdleSound());
            return true; // entered idle state
        }

        public virtual void CheckedAnimate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
        {
            if (!Mounted)
            {
                Animate(action, frameCount, repeatCount, forward, repeat, delay);
            }
        }

        private void CheckAIActive()
        {
            var map = Map;

            if (PlayerRangeSensitive && AIObject != null && map?.GetSector(Location).Active == true)
            {
                AIObject.Activate();
            }
        }

        public override void OnCombatantChange()
        {
            base.OnCombatantChange();

            Warmode = Combatant?.Deleted == false && Combatant.Alive;

            if (CanFly && Warmode)
            {
                Flying = false;
            }
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
            if (AcquireOnApproach && !Controlled && !Summoned && !BardPacified && FightMode != FightMode.Aggressor)
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

            SpeechType?.OnMovement(this, m, oldLocation);

            // Notice sound
            if ((!m.Hidden || m.AccessLevel == AccessLevel.Player) && m.Player && FightMode != FightMode.Aggressor &&
                FightMode != FightMode.None && Combatant == null && !Controlled && !Summoned && !BardPacified &&
                InRange(m.Location, 18) && !InRange(oldLocation, 18))
            {
                if (Body.IsMonster)
                {
                    Animate(11, 5, 1, true, false, 1);
                }

                PlaySound(GetAngerSound());
            }

            if (MLQuestSystem.Enabled && CanShout && m is PlayerMobile mobile)
            {
                CheckShout(mobile, oldLocation);
            }

            if (m_NoDupeGuards == m)
            {
                return;
            }

            if (!Body.IsHuman || Murderer || AlwaysAttackable || !m.Murderer ||
                !m.InRange(Location, 12) || !m.Alive)
            {
                return;
            }

            var guardedRegion = Region.GetRegion<GuardedRegion>();

            if (guardedRegion?.IsDisabled() == false && guardedRegion.IsGuardCandidate(m) && BeginAction<GuardedRegion>())
            {
                Say(1013037 + Utility.Random(16));
                guardedRegion.CallGuards(Location);

                Timer.StartTimer(TimeSpan.FromSeconds(5.0), ReleaseGuardLock);

                m_NoDupeGuards = m;
                Timer.StartTimer(ReleaseGuardDupeLock);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster && !Body.IsHuman)
            {
                var pack = Backpack;

                pack?.DisplayTo(from);
            }

            if (DeathAdderCharmable && from.CanBeHarmful(this, false))
            {
                if (SummonFamiliarSpell.Table.TryGetValue(from, out var bc) && (bc as DeathAdder)?.Deleted == false)
                {
                    from.SendAsciiMessage("You charm the snake.  Select a target to attack.");
                    from.Target = new DeathAdderCharmTarget(this);
                }
            }

            if (MLQuestSystem.Enabled && CanGiveMLQuest && from is PlayerMobile mobile)
            {
                MLQuestSystem.OnDoubleClick(this, mobile);
            }

            base.OnDoubleClick(from);
        }

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            if (MLQuestSystem.Enabled && CanGiveMLQuest)
            {
                list.Add(1072269); // Quest Giver
            }

            if (Core.ML)
            {
                if (DisplayWeight)
                {
                    list.Add(TotalWeight == 1 ? 1072788 : 1072789, TotalWeight); // Weight: ~1_WEIGHT~ stones
                }

                if (_controlOrder == OrderType.Guard)
                {
                    list.Add(1080078); // guarding
                }
            }

            if (Summoned && !(IsAnimatedDead || IsNecroFamiliar || this is Clone))
            {
                list.Add(1049646); // (summoned)
            }
            else if (Controlled && Commandable)
            {
                // Deliberate: show only (bonded), never (bonded) and (tame) together.
                if (IsBonded)
                {
                    list.Add(1049608); // (bonded)
                }
                else
                {
                    list.Add(502006); // (tame)
                }
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Controlled && Commandable)
            {
                int number;

                if (Summoned)
                {
                    number = 1049646; // (summoned)
                }
                else if (IsBonded)
                {
                    number = 1049608; // (bonded)
                }
                else
                {
                    number = 502006; // (tame)
                }

                PrivateOverheadMessage(MessageType.Regular, 0x3B2, number, from.NetState);
            }

            base.OnSingleClick(from);
        }

        public override bool OnBeforeDeath()
        {
            TriggerAbility(MonsterAbilityTrigger.Death, null);

            var treasureLevel = TreasureMapLevel;

            if (treasureLevel == 1 && Map == Map.Trammel && TreasureMap.IsInHavenIsland(this))
            {
                var killer = LastKiller;

                if (killer is BaseCreature bc)
                {
                    killer = bc.GetMaster();
                }

                if (killer is PlayerMobile mobile && mobile.Young)
                {
                    treasureLevel = 0;
                }
            }

            if (!Summoned && !NoKillAwards && !IsBonded)
            {
                if (treasureLevel >= 0)
                {
                    if (_isParagon && Paragon.ChestChance > Utility.RandomDouble())
                    {
                        PackItem(new ParagonChest(Name, treasureLevel));
                    }
                    else if ((Map == Map.Felucca || Map == Map.Trammel) && Utility.RandomDouble() < TreasureMap.LootChance)
                    {
                        PackItem(new TreasureMap(treasureLevel, Map));
                    }
                }

                if (_isParagon && Paragon.ChocolateIngredientChance > Utility.RandomDouble())
                {
                    switch (Utility.Random(4))
                    {
                        case 0:
                            {
                                PackItem(new CocoaButter());
                                break;
                            }
                        case 1:
                            {
                                PackItem(new CocoaLiquor());
                                break;
                            }
                        case 2:
                            {
                                PackItem(new SackOfSugar());
                                break;
                            }
                        case 3:
                            {
                                PackItem(new Vanilla());
                                break;
                            }
                    }
                }
            }

            if (!Summoned && !NoKillAwards && !_hasGeneratedLoot)
            {
                _hasGeneratedLoot = true;
                GenerateLoot(false);
            }

            if (!NoKillAwards && Region.IsPartOf("Doom"))
            {
                var bones = TheSummoningQuest.GetDaemonBonesFor(this);

                if (bones > 0)
                {
                    PackItem(new DaemonBone(bones));
                }
            }

            if (IsAnimatedDead)
            {
                Effects.SendLocationEffect(Location, Map, 0x3728, 13, 1, 0x461, 4);
            }

            var speechType = SpeechType;
            speechType?.OnDeath(this);
            ReceivedHonorContext?.OnTargetKilled();

            return base.OnBeforeDeath();
        }

        public int ComputeBonusDamage(List<DamageEntry> list, Mobile m)
        {
            var bonus = 0;

            for (var i = list.Count - 1; i >= 0; --i)
            {
                var de = list[i];

                if (de.Damager == m || de.Damager is not BaseCreature bc)
                {
                    continue;
                }

                if (bc.GetMaster() == m)
                {
                    bonus += de.DamageGiven;
                }
            }

            return bonus;
        }

        public Mobile GetMaster()
        {
            if (Controlled && ControlMaster != null)
            {
                return ControlMaster;
            }

            if (Summoned && SummonMaster != null)
            {
                return SummonMaster;
            }

            return null;
        }

        public virtual bool IsMonster => !Controlled || (GetMaster() as BaseCreature)?.IsMonster == true;

        public bool InActivePVPCombat() =>
            ControlOrder != OrderType.Follow &&
            Combatant is PlayerMobile ||
            Combatant is BaseCreature { Controlled: true } bc && bc.GetMaster() is PlayerMobile;

        public static List<DamageStore> GetLootingRights(List<DamageEntry> damageEntries, int hitsMax)
        {
            var rights = new List<DamageStore>();
            DamageStore firstDamager = null;

            for (var i = damageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= damageEntries.Count)
                {
                    continue;
                }

                var de = damageEntries[i];

                if (de.HasExpired)
                {
                    damageEntries.RemoveAt(i);
                    continue;
                }

                var damage = de.DamageGiven;

                var respList = de.Responsible;

                for (var j = 0; j < respList?.Count; ++j)
                {
                    var subEntry = respList[j];
                    var master = subEntry.Damager;

                    if (master?.Deleted != false || !master.Player)
                    {
                        continue;
                    }

                    var needNewSubEntry = true;

                    for (var k = 0; needNewSubEntry && k < rights.Count; ++k)
                    {
                        var ds = rights[k];

                        if (ds.m_Mobile == master)
                        {
                            ds.m_Damage += subEntry.DamageGiven;
                            needNewSubEntry = false;
                            firstDamager = ds;
                        }
                    }

                    if (needNewSubEntry)
                    {
                        var ds = new DamageStore(master, subEntry.DamageGiven);
                        rights.Add(ds);
                        firstDamager = ds;
                    }

                    damage -= subEntry.DamageGiven;
                }

                var m = de.Damager;

                if (m is not { Deleted: false, Player: true })
                {
                    continue;
                }

                if (damage <= 0)
                {
                    continue;
                }

                var needNewEntry = true;

                for (var j = 0; needNewEntry && j < rights.Count; ++j)
                {
                    var ds = rights[j];

                    if (ds.m_Mobile == m)
                    {
                        ds.m_Damage += damage;
                        needNewEntry = false;
                        firstDamager = ds;
                    }
                }

                if (needNewEntry)
                {
                    var ds = new DamageStore(m, damage);
                    rights.Add(ds);
                    firstDamager = ds;
                }
            }

            // Handle damage rights per Five on Friday: https://www.uoguide.com/Five_on_Friday_-_January_19,_2007
            if (rights.Count > 0)
            {
                if (firstDamager != null)
                {
                    firstDamager.m_Damage = (int)(firstDamager.m_Damage * 1.25);
                }

                if (rights.Count > 1)
                {
                    rights.Sort(); // Sort by damage
                }

                var topDamage = rights[0].m_Damage;

                var minDamage = hitsMax switch
                {
                    >= 3000 => topDamage / 16,
                    >= 1000 => topDamage / 8,
                    >= 200  => topDamage / 4,
                    _       => topDamage / 2
                };

                for (var i = 0; i < rights.Count; ++i)
                {
                    var ds = rights[i];

                    ds.m_HasRight = ds.m_Damage >= minDamage;
                }
            }

            return rights;
        }

        public virtual void OnKilledBy(Mobile mob)
        {
            if (GivesMLMinorArtifact)
            {
                if (MondainsLegacy.CheckArtifactChance(mob, this))
                {
                    MondainsLegacy.GiveArtifactTo(mob);
                }
            }
            else if (_isParagon)
            {
                if (Paragon.CheckArtifactChance(mob, this))
                {
                    Paragon.GiveArtifactTo(mob);
                }
            }
        }

        public override void OnDeath(Container c)
        {
            if (IsBonded)
            {
                Effects.PlaySound(this, GetDeathSound());

                Warmode = false;

                Poison = null;
                Combatant = null;

                Hits = 0;
                Stam = 0;
                Mana = 0;

                IsDeadPet = true;
                ControlTarget = ControlMaster;
                ControlOrder = OrderType.Follow;

                ProcessDelta();
                SendIncomingPacket();

                var aggressors = Aggressors;

                for (var i = 0; i < aggressors.Count; ++i)
                {
                    var info = aggressors[i];

                    if (info.Attacker.Combatant == this)
                    {
                        info.Attacker.Combatant = null;
                    }
                }

                var aggressed = Aggressed;

                for (var i = 0; i < aggressed.Count; ++i)
                {
                    var info = aggressed[i];

                    if (info.Defender.Combatant == this)
                    {
                        info.Defender.Combatant = null;
                    }
                }

                var owner = ControlMaster;

                if (owner?.Deleted != false || owner.Map != Map || !owner.InRange(this, 12) || !CanSee(owner) ||
                    !InLOS(owner))
                {
                    if (OwnerAbandonTime == DateTime.MinValue)
                    {
                        OwnerAbandonTime = Core.Now;
                    }
                }
                else
                {
                    OwnerAbandonTime = DateTime.MinValue;
                }

                CreatureEvents.CreatureDeathEvent(this);

                CheckStatTimers();
                return;
            }

            if (!Summoned && !NoKillAwards)
            {
                var (totalFame, totalKarma) = Titles.ComputeKillAwards(this, Map);

                var list = GetLootingRights(DamageEntries, HitsMax);
                using var titles = PooledRefList<Mobile>.Create();
                var fame = PooledRefList<int>.Create();
                var karma = PooledRefList<int>.Create();

                var givenQuestKill = false;
                var givenFactionKill = false;
                var givenToTKill = false;

                for (var i = 0; i < list.Count; ++i)
                {
                    var ds = list[i];

                    if (!ds.m_HasRight)
                    {
                        continue;
                    }

                    if (!Core.UOR)
                    {
                        var killer = LastKiller is BaseCreature bc ? bc.GetDamageMaster(this) : LastKiller;

                        if (ds.m_Mobile == killer)
                        {
                            titles.Add(ds.m_Mobile);
                            fame.Add(totalFame);
                            karma.Add(totalKarma);
                        }
                    }
                    else if (Engines.PartySystem.Party.Get(ds.m_Mobile) is { } party)
                    {
                        var divedFame = totalFame / party.Members.Count;
                        var divedKarma = totalKarma / party.Members.Count;

                        for (var j = 0; j < party.Members.Count; ++j)
                        {
                            var info = party.Members[j];

                            if (info?.Mobile != null)
                            {
                                var index = titles.IndexOf(info.Mobile);

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

                    var region = ds.m_Mobile.Region;

                    if (!givenToTKill && (Map == Map.Tokuno || region.IsPartOf("Yomotsu Mines") ||
                                          region.IsPartOf("Fan Dancer's Dojo")))
                    {
                        givenToTKill = true;
                        TreasuresOfTokuno.HandleKill(this, ds.m_Mobile);
                    }

                    if (ds.m_Mobile is PlayerMobile pm)
                    {
                        if (MLQuestSystem.Enabled)
                        {
                            MLQuestSystem.HandleKill(pm, this);
                        }

                        if (givenQuestKill)
                        {
                            continue;
                        }

                        var qs = pm.Quest;

                        if (qs != null)
                        {
                            qs.OnKill(this, c);
                            givenQuestKill = true;
                        }
                    }
                }

                for (var i = 0; i < titles.Count; ++i)
                {
                    Titles.AwardFame(titles[i], fame[i], true);
                    Titles.AwardKarma(titles[i], karma[i], true);
                }

                fame.Dispose();
                karma.Dispose();
            }

            base.OnDeath(c);

            if (DeleteCorpseOnDeath)
            {
                c.Delete();
            }

            CreatureEvents.CreatureDeathEvent(this);
        }

        public override void OnDelete()
        {
            CreatureEvents.CreatureDeletedEvent(this);

            var m = _controlMaster;
            SetControlMaster(null);

            SummonMaster = null;
            ReceivedHonorContext?.Cancel();

            base.OnDelete();
            m?.InvalidateProperties();
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            if (target is BaseFactionGuard)
            {
                return false;
            }

            if (target is BaseCreature creature && creature.IsInvulnerable || target is PlayerVendor or TownCrier)
            {
                if (message)
                {
                    if (target.Title == null)
                    {
                        SendMessage($"{target.Name} cannot be harmed.");
                    }
                    else
                    {
                        SendMessage($"{target.Name} {target.Title} cannot be harmed.");
                    }
                }

                return false;
            }

            return base.CanBeHarmful(target, message, ignoreOurBlessedness);
        }

        public override bool CanBeRenamedBy(Mobile from) =>
            Controlled && from == ControlMaster && !from.Region.IsPartOf<JailRegion>() ||
            base.CanBeRenamedBy(from);

        public bool SetControlMaster(Mobile m)
        {
            if (m == null)
            {
                ControlMaster = null;
                Controlled = false;
                ControlTarget = null;
                ControlOrder = OrderType.None;
            }
            else
            {
                if (Spawner?.UnlinkOnTaming == true)
                {
                    Spawner.Remove(this);
                    Spawner = null;
                }

                if (m.Followers + ControlSlots > m.FollowersMax)
                {
                    m.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                    return false;
                }

                CurrentWayPoint = null; // so tamed animals don't try to go back

                Home = Point3D.Zero;

                ControlMaster = m;
                Controlled = true;
                ControlTarget = null;
                ControlOrder = OrderType.Come;


                if (_pendingDeleteTimer != null)
                {
                    _pendingDeleteTimer.Stop();
                    _pendingDeleteTimer = null;
                }
            }

            Guild = null;

            Delta(MobileDelta.Noto);

            InvalidateProperties();

            return true;
        }

        public override void OnRegionChange(Region Old, Region New)
        {
            base.OnRegionChange(Old, New);

            if (Controlled && Spawner?.UnlinkOnTaming == false && New?.AcceptsSpawnsFrom(Spawner.Region) != true)
            {
                Spawner.Remove(this);
                Spawner = null;
            }
        }

        public static bool Summon(BaseCreature creature, Mobile caster, Point3D p, int sound, TimeSpan duration) =>
            Summon(creature, true, caster, p, sound, duration, null);

        public static bool Summon(
            BaseCreature creature, Mobile caster, Point3D p, int sound, TimeSpan duration,
            Action onUnsummon
        ) => Summon(creature, true, caster, p, sound, duration, onUnsummon);

        public static bool Summon(
            BaseCreature creature, bool controlled, Mobile caster, Point3D p, int sound,
            TimeSpan duration
        ) => Summon(creature, controlled, caster, p, sound, duration, null);

        public static bool Summon(
            BaseCreature creature, bool controlled, Mobile caster, Point3D p, int sound,
            TimeSpan duration, Action onUnsummon
        )
        {
            if (caster.Followers + creature.ControlSlots > caster.FollowersMax)
            {
                caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                creature.Delete();
                return false;
            }

            Summoning = true;

            if (controlled)
            {
                creature.SetControlMaster(caster);
            }

            creature.RangeHome = 10;
            creature.Summoned = true;
            creature.SummonMaster = caster;

            var pack = creature.Backpack;

            if (pack != null)
            {
                for (var i = pack.Items.Count - 1; i >= 0; --i)
                {
                    if (i >= pack.Items.Count)
                    {
                        continue;
                    }

                    pack.Items[i].Delete();
                }
            }

            new UnsummonTimer(creature, duration, onUnsummon).Start();
            creature.SummonEnd = Core.Now + duration;

            creature.MoveToWorld(p, caster.Map);

            Effects.PlaySound(p, creature.Map, sound);

            Summoning = false;

            return true;
        }

        public virtual void OnThink()
        {
            var tc = Core.TickCount;

            if (EnableRummaging && CanRummageCorpses && !Summoned && !Controlled && tc - m_NextRummageTime >= 0)
            {
                double min, max;

                if (Utility.RandomDouble() < ChanceToRummage && Rummage())
                {
                    min = MinutesToNextRummageMin;
                    max = MinutesToNextRummageMax;
                }
                else
                {
                    min = MinutesToNextChanceMin;
                    max = MinutesToNextChanceMax;
                }

                var delay = min + Utility.RandomDouble() * (max - min);
                m_NextRummageTime = tc + (int)TimeSpan.FromMinutes(delay).TotalMilliseconds;
            }

            // Fire breath, etc.
            TriggerAbility(MonsterAbilityTrigger.Think, Combatant);

            if ((CanHeal || CanHealOwner) && Alive && !IsHealing && !BardPacified)
            {
                var owner = ControlMaster;

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
                if (Combatant == null && !Warmode && Utility.RandomDouble() < .10) /* some throttling */
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
            if (Backpack == null)
            {
                return false;
            }

            Corpse toRummage = null;
            foreach (var c in GetItemsInRange<Corpse>(2))
            {
                if (c.Items.Count > 0)
                {
                    toRummage = c;
                    break;
                }
            }

            if (toRummage == null)
            {
                return false;
            }

            var items = toRummage.Items;

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items.RandomElement();

                Lift(item, item.Amount, out var rejected, out var _);

                if (!rejected && Drop(this, new Point3D(-1, -1, 0)))
                {
                    // *rummages through a corpse and takes an item*
                    PublicOverheadMessage(MessageType.Emote, 0x3B2, 1008086);
                    //TODO Instance rummaged loot
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
            {
                return BardMaster;
            }

            if (_controlled && _controlMaster != null)
            {
                return _controlMaster;
            }

            if (_summoned && _summonMaster != null)
            {
                return _summonMaster;
            }

            return base.GetDamageMaster(damagee);
        }

        public void Provoke(Mobile master, Mobile target, bool bSuccess)
        {
            BardProvoked = true;

            if (!Core.ML)
            {
                PublicOverheadMessage(MessageType.Emote, EmoteHue, false, "*looks furious*");
            }

            if (bSuccess)
            {
                PlaySound(GetIdleSound());

                BardMaster = master;
                BardTarget = target;
                Combatant = target;
                BardEndTime = Core.Now + TimeSpan.FromSeconds(30.0);

                if (target is BaseCreature t)
                {
                    if (t.Unprovokable || t.IsParagon && BaseInstrument.GetBaseDifficulty(t) >= 160.0)
                    {
                        return;
                    }

                    t.BardProvoked = true;

                    t.BardMaster = master;
                    t.BardTarget = this;
                    t.Combatant = this;
                    t.BardEndTime = Core.Now + TimeSpan.FromSeconds(30.0);
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
            var name = Name;

            if (name == null || str.Length < name.Length)
            {
                return false;
            }

            var wordsString = str.Split(' ');
            var wordsName = name.Split(' ');

            for (var j = 0; j < wordsName.Length; j++)
            {
                var wordName = wordsName[j];

                var bFound = false;
                for (var i = 0; i < wordsString.Length; i++)
                {
                    var word = wordsString[i];

                    if (word.InsensitiveEquals(wordName))
                    {
                        bFound = true;
                    }

                    if (bWithAll && word.InsensitiveEquals("all"))
                    {
                        return true;
                    }
                }

                if (!bFound)
                {
                    return false;
                }
            }

            return true;
        }

        public static void TeleportPets(Mobile master, Point3D loc, Map map, bool onlyBonded = false)
        {
            if (master is PlayerMobile { AllFollowers: not null } pm)
            {
                foreach (var m in pm.AllFollowers)
                {
                    if (m.Map == master.Map && master.InRange(m, 3) && m is BaseCreature
                            { Controlled: true, ControlOrder: OrderType.Guard or OrderType.Follow or OrderType.Come } pet &&
                        pet.ControlMaster == master && (!onlyBonded || pet.IsBonded))
                    {
                        m.MoveToWorld(loc, map);
                    }
                }

                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();
            foreach (var m in master.GetMobilesInRange(3))
            {
                if (m is BaseCreature
                        { Controlled: true, ControlOrder: OrderType.Guard or OrderType.Follow or OrderType.Come } pet &&
                    pet.ControlMaster == master && (!onlyBonded || pet.IsBonded))
                {
                    queue.Enqueue(pet);
                }
            }

            while (queue.Count > 0)
            {
                queue.Dequeue().MoveToWorld(loc, map);
            }
        }

        public virtual void ResurrectPet()
        {
            if (!IsDeadPet)
            {
                return;
            }

            OnBeforeResurrect();

            Poison = null;

            Warmode = false;

            Hits = 10;
            Stam = StamMax;
            Mana = 0;

            ProcessDelta();

            IsDeadPet = false;

            var buffer = stackalloc byte[OutgoingMobilePackets.BondedStatusPacketLength].InitializePacket();
            OutgoingMobilePackets.CreateBondedStatus(buffer, Serial, false);
            Effects.SendPacket(Location, Map, buffer);

            SendIncomingPacket();

            OnAfterResurrect();

            AIObject?.Activate();

            var owner = ControlMaster;

            if (owner?.Deleted == false && owner.Map == Map && owner.InRange(this, 12) && CanSee(owner) && InLOS(owner))
            {
                OwnerAbandonTime = DateTime.MinValue;
            }
            else if (OwnerAbandonTime == DateTime.MinValue)
            {
                OwnerAbandonTime = Core.Now;
            }

            CheckStatTimers();
        }

        public override bool CanBeDamaged()
        {
            if (IsDeadPet || IsInvulnerable)
            {
                return false;
            }

            return base.CanBeDamaged();
        }

        private bool IsSpawnerBound() =>
            Map != null && Map != Map.Internal &&
            FightMode != FightMode.None && RangeHome >= 0 &&
            !Controlled && !Summoned && (Spawner as Spawner)?.Map == Map;

        public override void OnSectorDeactivate()
        {
            if (!Deleted && ReturnsToHome && IsSpawnerBound() && !InRange(Home, RangeHome + 5))
            {
                Timer.StartTimer(TimeSpan.FromSeconds(Utility.Random(45) + 15), GoHome_Callback);

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
            if (m_ReturnQueued && IsSpawnerBound() && !Map.GetSector(X, Y).Active)
            {
                SetLocation(Home, true);

                if (PlayerRangeSensitive && !Map.GetSector(X, Y).Active)
                {
                    AIObject?.Deactivate();
                }
            }

            m_ReturnQueued = false;
        }

        public override void OnSectorActivate()
        {
            if (PlayerRangeSensitive)
            {
                AIObject?.Activate();
            }

            base.OnSectorActivate();
        }

        protected virtual List<MLQuest> ConstructQuestList() => null;

        private void CheckShout(PlayerMobile pm, Point3D oldLocation)
        {
            if (m_MLNextShout > Core.Now || pm.Hidden || !pm.Alive)
            {
                return;
            }

            var shoutRange = ShoutRange;

            if (!InRange(pm.Location, shoutRange) || InRange(oldLocation, shoutRange) || !CanSee(pm) || !InLOS(pm))
            {
                return;
            }

            var context = MLQuestSystem.GetContext(pm);

            if (context?.IsFull == true)
            {
                return;
            }

            var quest = MLQuestSystem.RandomStarterQuest(this, pm, context);

            if (quest?.Activated != true || context?.IsDoingQuest(quest) == true)
            {
                return;
            }

            Shout(pm);
            m_MLNextShout = Core.Now + ShoutDelay;
        }

        public virtual void Shout(PlayerMobile pm)
        {
        }

        public static void Configure()
        {
            BondingEnabled = ServerConfiguration.GetSetting("taming.enableBonding", Core.LBR);
        }

        public void BeginDeleteTimer()
        {
            if (this is not BaseEscortable && !Summoned && !Deleted && !IsStabled)
            {
                StopDeleteTimer();
                _pendingDeleteTimer = new DeleteTimer(this, TimeSpan.FromDays(3.0));
                _pendingDeleteTimer.Start();
            }
        }

        public void StopDeleteTimer()
        {
            if (_pendingDeleteTimer != null)
            {
                _pendingDeleteTimer.Stop();
                _pendingDeleteTimer = null;
            }
        }

        public void SpillAcid(int amount)
        {
            SpillAcid(null, amount);
        }

        public void SpillAcid(Mobile target, int amount)
        {
            if (target != null && target.Map == null || Map == null)
            {
                return;
            }

            for (var i = 0; i < amount; ++i)
            {
                Point3D loc;
                var map = Map;

                if (target != null && amount == 1)
                {
                    loc = target.Location;
                    map = target.Map;
                }
                else
                {
                    loc = map.GetRandomNearbyLocation(Location);
                }

                var acid = NewHarmfulItem();
                acid.MoveToWorld(loc, map);
            }
        }

        // Solen-style acid; override for other harmful drops (kappa slime, etc.).
        public virtual Item NewHarmfulItem() => new Acid(TimeSpan.FromSeconds(10), 30, 30);

        public virtual void StopFlee()
        {
            EndFleeTime = DateTime.MinValue;
        }

        public virtual bool CheckFlee()
        {
            if (EndFleeTime == DateTime.MinValue)
            {
                return false;
            }

            if (Core.Now >= EndFleeTime)
            {
                StopFlee();
                return false;
            }

            return true;
        }

        public virtual void BeginFlee(TimeSpan maxDuration)
        {
            EndFleeTime = Core.Now + maxDuration;
        }

        public virtual bool IsPetFriend(Mobile m) => Friends?.Contains(m) == true;

        public virtual void AddPetFriend(Mobile m)
        {
            Friends ??= new List<Mobile>();

            Friends.Add(m);
        }

        public virtual void RemovePetFriend(Mobile m) => Friends?.Remove(m);

        public virtual bool IsFriend(Mobile m) =>
            OppositionGroup?.IsEnemy(this, m) != true && m is BaseCreature c && _team == c._team
            && (_summoned || _controlled) == (c._summoned || c._controlled);

        public virtual Allegiance GetFactionAllegiance(Mobile mob)
        {
            if (mob == null || mob.Map != Faction.Facet || FactionAllegiance == null)
            {
                return Allegiance.None;
            }

            var fac = Faction.Find(mob, true);

            if (fac == null)
            {
                return Allegiance.None;
            }

            return fac == FactionAllegiance ? Allegiance.Ally : Allegiance.Enemy;
        }

        public virtual Allegiance GetEthicAllegiance(Mobile mob)
        {
            if (mob == null || mob.Map != Faction.Facet || EthicAllegiance == null)
            {
                return Allegiance.None;
            }

            var ethic = Ethic.Find(mob, true);

            if (ethic == null)
            {
                return Allegiance.None;
            }

            return ethic == EthicAllegiance ? Allegiance.Ally : Allegiance.Enemy;
        }

        public virtual void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            TriggerAbilityAlterDamageScalar(MonsterAbilityTrigger.TakeSpellDamage, caster, ref scalar);
        }

        public virtual void AlterDamageScalarTo(Mobile target, ref double scalar)
        {
            TriggerAbilityAlterDamageScalar(MonsterAbilityTrigger.GiveSpellDamage, target, ref scalar);
        }

        public virtual void AlterSpellDamageFrom(Mobile from, ref int damage)
        {
            TriggerAbilityAlterDamage(MonsterAbilityTrigger.TakeSpellDamage, from, ref damage);
        }

        public virtual void AlterSpellDamageTo(Mobile to, ref int damage)
        {
            TriggerAbilityAlterDamage(MonsterAbilityTrigger.GiveSpellDamage, to, ref damage);
        }

        public virtual void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            TriggerAbilityAlterDamage(MonsterAbilityTrigger.TakeMeleeDamage, from, ref damage);
        }

        public virtual void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            TriggerAbilityAlterDamage(MonsterAbilityTrigger.GiveMeleeDamage, to, ref damage);
        }

        public virtual bool CheckFoodPreference(Item f) =>
            CheckFoodPreference(f, FoodType.Eggs) ||
            CheckFoodPreference(f, FoodType.Fish) ||
            CheckFoodPreference(f, FoodType.GrainsAndHay) ||
            CheckFoodPreference(f, FoodType.Meat) ||
            CheckFoodPreference(f, FoodType.FruitsAndVeggies) ||
            CheckFoodPreference(f, FoodType.Gold) ||
            CheckFoodPreference(f, FoodType.Metal) ||
            CheckFoodPreference(f, FoodType.Leather);

        private bool CheckFoodPreference(Item fed, FoodType type)
        {
            if ((FavoriteFood & type) == 0)
            {
                return false;
            }

            // Goat special case
            if (type == FoodType.Leather)
            {
                if (fed is BaseLeather or Bag or Pouch or Server.Items.Backpack or StrongBackpack)
                {
                    return true;
                }

                if (fed is BaseArmor armor)
                {
                    return armor.MaterialType
                        is ArmorMaterialType.Leather
                        or ArmorMaterialType.Studded
                        or ArmorMaterialType.Spined
                        or ArmorMaterialType.Horned
                        or ArmorMaterialType.Barbed;
                }

                CraftResource craftResource;
                if (fed is BaseWeapon weapon)
                {
                    craftResource = weapon.Resource;
                }
                else if (fed is BaseClothing clothing)
                {
                    craftResource = clothing.Resource;
                }
                else
                {
                    return false;
                }

                return CraftResources.GetType(craftResource) == CraftResourceType.Leather;
            }

            if (type == FoodType.Metal)
            {
                if (fed is BaseArmor armor)
                {
                    return armor.MaterialType
                        is ArmorMaterialType.Ringmail
                        or ArmorMaterialType.Chainmail
                        or ArmorMaterialType.Plate;
                }

                CraftResource craftResource;
                if (fed is BaseWeapon weapon)
                {
                    craftResource = weapon.Resource;
                }
                else if (fed is BaseClothing clothing)
                {
                    craftResource = clothing.Resource;
                }
                else
                {
                    return fed.GetType().InTypeList(_metal);
                }

                return CraftResources.GetType(craftResource) == CraftResourceType.Metal;
            }

            var types = type switch
            {
                FoodType.Eggs             => _eggs,
                FoodType.Fish             => _fish,
                FoodType.GrainsAndHay     => _grainsAndHay,
                FoodType.Meat             => _meat,
                FoodType.FruitsAndVeggies => _fruitsAndVeggies,
                FoodType.Gold             => _gold,
            };

            return fed.GetType().InTypeList(types);
        }

        public virtual bool CheckFeed(Mobile from, Item dropped)
        {
            if (IsDeadPet || !Controlled || ControlMaster != from && !IsPetFriend(from))
            {
                return false;
            }

            if (!CheckFoodPreference(dropped))
            {
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1043257, from.NetState); // The animal shies away.
                return false;
            }

            var amount = dropped.Amount;

            if (amount > 0)
            {
                var stamGain = dropped switch
                {
                    Gold => amount - 50,
                    _    => amount * 15 - 50
                };

                if (stamGain > 0)
                {
                    Stam += stamGain;

                    // 61 food = 3,840 steps
                    StaminaSystem.RegenSteps(this as IHasSteps, stamGain * 4);
                }

                if (Core.SE)
                {
                    _loyalty = MaxLoyalty;
                }
                else if (_loyalty < MaxLoyalty)
                {
                    var loyaltyIncrease = Utility.CoinFlips(amount, MaxLoyaltyIncrease) * 10;

                    if (loyaltyIncrease > 0)
                    {
                        _loyalty = Math.Min(MaxLoyalty, _loyalty + loyaltyIncrease);
                        SayTo(from, 502060); // Your pet looks happier.
                    }
                }

                if (Body.IsAnimal)
                {
                    Animate(3, 5, 1, true, false, 0);
                }
                else if (Body.IsMonster)
                {
                    Animate(17, 5, 1, true, false, 0);
                }

                if (IsBondable && !IsBonded)
                {
                    var master = _controlMaster;

                    if (master != null && master == from) // So friends can't start the bonding process
                    {
                        if (MinTameSkill <= 29.1 || master.Skills.AnimalTaming.Base >= MinTameSkill ||
                            OverrideBondingReqs() ||
                            Core.ML && master.Skills.AnimalTaming.Value >= MinTameSkill)
                        {
                            if (BondingBegin == DateTime.MinValue)
                            {
                                BondingBegin = Core.Now;
                            }
                            else if (BondingBegin + BondingDelay <= Core.Now)
                            {
                                IsBonded = true;
                                BondingBegin = DateTime.MinValue;
                                from.SendLocalizedMessage(1049666); // Your pet has bonded with you!
                            }
                        }
                        else if (Core.ML)
                        {
                            // Your pet cannot form a bond with you until your animal taming ability has risen.
                            from.SendLocalizedMessage(1075268);
                        }
                    }
                }

                dropped.Delete();
                return true;
            }

            return false;
        }

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

        public virtual bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!CanTeach)
            {
                return false;
            }

            if (skill == SkillName.Stealth && from.Skills.Hiding.Base < Stealth.HidingRequirement)
            {
                return false;
            }

            if (skill == SkillName.RemoveTrap && (from.Skills.Lockpicking.Base < 50.0 ||
                                                  from.Skills.DetectHidden.Base < 50.0))
            {
                return false;
            }

            return Core.AOS || skill != SkillName.Focus && skill != SkillName.Chivalry && skill != SkillName.Necromancy;
        }

        public virtual TeachResult CheckTeachSkills(
            SkillName skill, Mobile m, int maxPointsToLearn, ref int pointsToLearn,
            bool doTeach
        )
        {
            if (!CheckTeach(skill, m) || !m.CheckAlive())
            {
                return TeachResult.Failure;
            }

            var ourSkill = Skills[skill];
            var theirSkill = m.Skills[skill];

            if (ourSkill == null || theirSkill == null)
            {
                return TeachResult.Failure;
            }

            var baseToSet = ourSkill.BaseFixedPoint / 3;

            if (baseToSet > 420)
            {
                baseToSet = 420;
            }
            else if (baseToSet < 200)
            {
                return TeachResult.Failure;
            }

            if (baseToSet > theirSkill.CapFixedPoint)
            {
                baseToSet = theirSkill.CapFixedPoint;
            }

            pointsToLearn = baseToSet - theirSkill.BaseFixedPoint;

            if (maxPointsToLearn > 0 && pointsToLearn > maxPointsToLearn)
            {
                pointsToLearn = maxPointsToLearn;
                baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
            }

            if (pointsToLearn < 0)
            {
                return TeachResult.KnowsMoreThanMe;
            }

            if (pointsToLearn == 0)
            {
                return TeachResult.KnowsWhatIKnow;
            }

            if (theirSkill.Lock != SkillLock.Up)
            {
                return TeachResult.SkillNotRaisable;
            }

            var freePoints = Math.Max(m.Skills.Cap - m.Skills.Total, 0);
            var freeablePoints = 0;

            for (var i = 0; freePoints + freeablePoints < pointsToLearn && i < m.Skills.Length; ++i)
            {
                var sk = m.Skills[i];

                if (sk == theirSkill || sk.Lock != SkillLock.Down)
                {
                    continue;
                }

                freeablePoints += sk.BaseFixedPoint;
            }

            if (freePoints + freeablePoints == 0)
            {
                return TeachResult.NotEnoughFreePoints;
            }

            if (freePoints + freeablePoints < pointsToLearn)
            {
                pointsToLearn = freePoints + freeablePoints;
                baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
            }

            if (doTeach)
            {
                var need = pointsToLearn - freePoints;

                for (var i = 0; need > 0 && i < m.Skills.Length; ++i)
                {
                    var sk = m.Skills[i];

                    if (sk == theirSkill || sk.Lock != SkillLock.Down)
                    {
                        continue;
                    }

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

                if (baseToSet > theirSkill.CapFixedPoint ||
                    m.Skills.Total - theirSkill.BaseFixedPoint + baseToSet > m.Skills.Cap)
                {
                    return TeachResult.NotEnoughFreePoints;
                }

                theirSkill.BaseFixedPoint = baseToSet;
            }

            return TeachResult.Success;
        }

        public virtual bool CheckTeachingMatch(Mobile m)
        {
            if (m_Teaching == (SkillName)(-1))
            {
                return false;
            }

            if (m is PlayerMobile mobile)
            {
                return mobile.Learning == m_Teaching;
            }

            return true;
        }

        public virtual bool Teach(SkillName skill, Mobile m, int maxPointsToLearn, bool doTeach)
        {
            var pointsToLearn = 0;
            var res = CheckTeachSkills(skill, m, maxPointsToLearn, ref pointsToLearn, doTeach);

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
                            Say(501539);                    // Let me show thee something of how this is done.
                            m.SendLocalizedMessage(501540); // Your skill level increases.

                            m_Teaching = (SkillName)(-1);

                            if (m is PlayerMobile mobile)
                            {
                                mobile.Learning = (SkillName)(-1);
                            }
                        }
                        else
                        {
                            // I will teach thee all I know, if paid the amount in full.  The price is:
                            Say(1019077, AffixType.Append, $" {pointsToLearn}", "");
                            Say(1043108); // For less I shall teach thee less.

                            m_Teaching = skill;

                            if (m is PlayerMobile mobile)
                            {
                                mobile.Learning = skill;
                            }
                        }

                        return true;
                    }
            }

            return false;
        }

        /// <summary>
        /// Sets the think clock and clears movement overrides (legacy one-clock semantics);
        /// use <see cref="SetMoveSpeed"/> for an independent movement pace.
        /// </summary>
        public void SetSpeed(double active, double passive, bool isPassive = true)
        {
            ActiveSpeed = active;
            PassiveSpeed = passive;
            ClearMoveSpeed();
            CurrentSpeed = isPassive ? PassiveSpeed : ActiveSpeed;
        }

        /// <summary>Sets only the movement clock (seconds per step).</summary>
        public void SetMoveSpeed(double active, double passive)
        {
            ActiveMoveSpeed = active;
            PassiveMoveSpeed = passive;
        }

        /// <summary>Clears movement overrides; steps pace off the think clock again.</summary>
        public void ClearMoveSpeed()
        {
            _activeMoveSpeed = 0;
            _passiveMoveSpeed = 0;
        }

        /// <summary>
        /// Scales movement overrides (paragon and similar buffs). Inheriting values stay
        /// inheriting — they already follow the scaled think clock.
        /// </summary>
        public void ScaleMoveSpeed(double scalar)
        {
            if (_activeMoveSpeed > 0)
            {
                _activeMoveSpeed *= scalar;
            }

            if (_passiveMoveSpeed > 0)
            {
                _passiveMoveSpeed *= scalar;
            }
        }

        /// <summary>
        /// Snaps speeds within rounding distance of the creature's table values back to
        /// exact. A scaling buff that divides then multiplies can drift by an ulp (e.g.
        /// 0.9 and 0.45 through 1.2), which would read as hand-tuned; call after undoing
        /// such a buff. Genuinely tuned speeds are nowhere near the epsilon and keep.
        /// </summary>
        public void SnapSpeedsToTable()
        {
            GetSpeeds(out var activeSpeed, out var passiveSpeed);

            if (Math.Abs(_activeSpeed - activeSpeed) < 0.0001 && Math.Abs(_passiveSpeed - passiveSpeed) < 0.0001)
            {
                _activeSpeed = activeSpeed;
                _passiveSpeed = passiveSpeed;
            }

            GetMoveSpeeds(out var activeMoveSpeed, out var passiveMoveSpeed);

            if (activeMoveSpeed > 0 && Math.Abs(_activeMoveSpeed - activeMoveSpeed) < 0.0001)
            {
                _activeMoveSpeed = activeMoveSpeed;
            }

            if (passiveMoveSpeed > 0 && Math.Abs(_passiveMoveSpeed - passiveMoveSpeed) < 0.0001)
            {
                _passiveMoveSpeed = passiveMoveSpeed;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCurrentSpeedToActive() => CurrentSpeed = ActiveSpeed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCurrentSpeedToPassive() => CurrentSpeed = PassiveSpeed;

        public void SetDamage(int val)
        {
            _damageMin = val;
            _damageMax = val;
        }

        public void SetDamage(int min, int max)
        {
            _damageMin = min;
            _damageMax = max;
        }

        public void SetHits(int val)
        {
            if (val < 1000 && !Core.AOS)
            {
                val = val * 100 / 60;
            }

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
                    {
                        PhysicalDamage = val;
                        break;
                    }
                case ResistanceType.Fire:
                    {
                        FireDamage = val;
                        break;
                    }
                case ResistanceType.Cold:
                    {
                        ColdDamage = val;
                        break;
                    }
                case ResistanceType.Poison:
                    {
                        PoisonDamage = val;
                        break;
                    }
                case ResistanceType.Energy:
                    {
                        EnergyDamage = val;
                        break;
                    }
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
                    {
                        _physicalResistanceSeed = val;
                        break;
                    }
                case ResistanceType.Fire:
                    {
                        _fireResistSeed = val;
                        break;
                    }
                case ResistanceType.Cold:
                    {
                        _coldResistSeed = val;
                        break;
                    }
                case ResistanceType.Poison:
                    {
                        _poisonResistSeed = val;
                        break;
                    }
                case ResistanceType.Energy:
                    {
                        _energyResistSeed = val;
                        break;
                    }
            }

            UpdateResistances();
        }

        public void SetSkill(SkillName name, double val)
        {
            Skills[name].BaseFixedPoint = (int)(val * 10);

            if (Skills[name].Base > Skills[name].Cap)
            {
                if (Core.SE)
                {
                    SkillsCap += Skills[name].BaseFixedPoint - Skills[name].CapFixedPoint;
                }

                Skills[name].Cap = Skills[name].Base;
            }
        }

        public void SetSkill(SkillName name, double min, double max)
        {
            var minFixed = (int)(min * 10);
            var maxFixed = (int)(max * 10);

            Skills[name].BaseFixedPoint = Utility.RandomMinMax(minFixed, maxFixed);

            if (Skills[name].Base > Skills[name].Cap)
            {
                if (Core.SE)
                {
                    SkillsCap += Skills[name].BaseFixedPoint - Skills[name].CapFixedPoint;
                }

                Skills[name].Cap = Skills[name].Base;
            }
        }

        public void SetFameLevel(int level)
        {
            Fame = level switch
            {
                1 => Utility.RandomMinMax(0, 1249),
                2 => Utility.RandomMinMax(1250, 2499),
                3 => Utility.RandomMinMax(2500, 4999),
                4 => Utility.RandomMinMax(5000, 9999),
                5 => Utility.RandomMinMax(10000, 10000),
                _ => Fame
            };
        }

        public void SetKarmaLevel(int level)
        {
            Karma = level switch
            {
                0 => -Utility.RandomMinMax(0, 624),
                1 => -Utility.RandomMinMax(625, 1249),
                2 => -Utility.RandomMinMax(1250, 2499),
                3 => -Utility.RandomMinMax(2500, 4999),
                4 => -Utility.RandomMinMax(5000, 9999),
                5 => -Utility.RandomMinMax(10000, 10000),
                _ => Karma
            };
        }

        public void PackArcaneScroll(int min, int max)
        {
            PackArcaneScroll(Utility.RandomMinMax(min, max));
        }

        public void PackArcaneScroll(int amount)
        {
            for (var i = 0; i < amount; ++i)
            {
                PackArcaneScroll();
            }
        }

        public void PackArcaneScroll()
        {
            if (!Core.ML)
            {
                return;
            }

            PackItem(Loot.Construct(Loot.ArcanistScrollTypes));
        }

        public void PackPotion()
        {
            PackItem(Loot.RandomPotion());
        }

        public void PackArcanceScroll(double chance)
        {
            if (!Core.ML || chance <= Utility.RandomDouble())
            {
                return;
            }

            PackItem(Loot.Construct(Loot.ArcanistScrollTypes));
        }

        public void PackNecroScroll(int index)
        {
            if (!Core.AOS || Utility.RandomDouble() < 0.95)
            {
                return;
            }

            PackItem(Loot.Construct(Loot.NecromancyScrollTypes, index));
        }

        public void PackScroll(int minCircle, int maxCircle)
        {
            PackScroll(Utility.RandomMinMax(minCircle, maxCircle));
        }

        public void PackScroll(int circle)
        {
            var min = (circle - 1) * 8;

            PackItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
        }

        public void PackMagicItems(int minLevel, int maxLevel, double armorChance = 0.30, double weaponChance = 0.15)
        {
            if (!PackArmor(minLevel, maxLevel, armorChance))
            {
                PackWeapon(minLevel, maxLevel, weaponChance);
            }
        }

        // A type's constant bucket; runtime state changes assign SpeedClass instead.
        public virtual SpeedLevel DefaultSpeedClass => SpeedLevel.None;

        // Resolved once per creature; serialization consults the table four times per mob
        // per save (and again on elided loads), so the dictionary walk must not repeat.
        private NPCSpeeds.SpeedClassEntry _speedEntry;

        private NPCSpeeds.SpeedClassEntry SpeedEntry => _speedEntry ??= NPCSpeeds.FindEntry(this);

        public virtual void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            var entry = SpeedEntry;

            if (entry == null)
            {
                if (_speedClass == SpeedLevel.Custom)
                {
                    // A custom creature is its own reference.
                    activeSpeed = _activeSpeed;
                    passiveSpeed = _passiveSpeed;
                    return;
                }

                throw new InvalidOperationException(
                    $"{GetType()} has no speed entry - is {"Data/npc-speeds.json"} missing?"
                );
            }

            activeSpeed = entry.ActiveSpeed;
            passiveSpeed = entry.PassiveSpeed;
        }

        // Move speeds are optional (0 = inherit), so this tolerates an unloaded table.
        public virtual void GetMoveSpeeds(out double activeMoveSpeed, out double passiveMoveSpeed)
        {
            var entry = SpeedEntry;

            activeMoveSpeed = entry?.ActiveMoveSpeed ?? 0;
            passiveMoveSpeed = entry?.PassiveMoveSpeed ?? 0;
        }

        // Pre-v22 saves carry no movement clock. Think speeds matching today's GetSpeeds
        // mean never hand-tuned: adopt today's move values; tuned creatures keep inheriting.
        internal void MigrateMoveSpeeds()
        {
            GetSpeeds(out var activeSpeed, out var passiveSpeed);

            if (_activeSpeed == activeSpeed && _passiveSpeed == passiveSpeed)
            {
                GetMoveSpeeds(out _activeMoveSpeed, out _passiveMoveSpeed);
            }
        }

        public virtual void DropBackpack()
        {
            var backpack = Backpack;
            if (!(backpack?.Items.Count > 0))
            {
                return;
            }

            var b = new CreatureBackpack(Name);
            using var queue = backpack.EnumerateItems();

            while (queue.Count > 0)
            {
                b.DropItem(queue.Dequeue());
            }

            var house = BaseHouse.FindHouseAt(this);
            if (house != null)
            {
                b.MoveToWorld(house.BanLocation, house.Map);
            }
            else
            {
                b.MoveToWorld(Location, Map);
            }
        }

        public virtual void GenerateLoot(bool spawning)
        {
            m_Spawning = spawning;

            if (!spawning)
            {
                m_KillersLuck = LootPack.GetLuckChanceForKiller(this);
            }

            GenerateLoot();

            if (_isParagon)
            {
                if (Fame < 1250)
                {
                    AddLoot(LootPack.Meager);
                }
                else if (Fame < 2500)
                {
                    AddLoot(LootPack.Average);
                }
                else if (Fame < 5000)
                {
                    AddLoot(LootPack.Rich);
                }
                else if (Fame < 10000)
                {
                    AddLoot(LootPack.FilthyRich);
                }
                else
                {
                    AddLoot(LootPack.UltraRich);
                }
            }

            m_Spawning = false;
            m_KillersLuck = 0;
        }

        public virtual void GenerateLoot()
        {
        }

        public virtual void AddLoot(LootPack pack, int amount)
        {
            for (var i = 0; i < amount; ++i)
            {
                AddLoot(pack);
            }
        }

        public virtual void AddLoot(LootPack pack)
        {
            if (Summoned)
            {
                return;
            }

            var backpack = Backpack ?? new Backpack { Movable = false };
            AddItem(backpack);

            pack.Generate(this, backpack, m_Spawning, m_KillersLuck);
        }

        public bool PackArmor(int minLevel, int maxLevel) => PackArmor(minLevel, maxLevel, 1.0);

        public bool PackArmor(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
            {
                return false;
            }

            minLevel = Math.Clamp(minLevel, 0, 5);
            maxLevel = Math.Clamp(maxLevel, 0, 5);

            if (Core.AOS)
            {
                var item = Loot.RandomArmorOrShieldOrJewelry();

                if (item == null)
                {
                    return false;
                }

                GetRandomAOSStats(minLevel, maxLevel, out var attributeCount, out var min, out var max);

                if (item is BaseArmor armor)
                {
                    BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
                }
                else if (item is BaseJewel jewel)
                {
                    BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);
                }

                PackItem(item);
            }
            else
            {
                var armor = Loot.RandomArmorOrShield();

                if (armor == null)
                {
                    return false;
                }

                armor.ProtectionLevel = (ArmorProtectionLevel)RandomMinMaxScaled(minLevel, maxLevel);
                armor.Durability = (ArmorDurabilityLevel)RandomMinMaxScaled(minLevel, maxLevel);

                PackItem(armor);
            }

            return true;
        }

        public static void GetRandomAOSStats(int minLevel, int maxLevel, out int attributeCount, out int min, out int max)
        {
            var v = RandomMinMaxScaled(minLevel, maxLevel);

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
            {
                return min;
            }

            if (min > max)
            {
                (min, max) = (max, min);
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

            var count = max - min + 1;
            int total = 0, toAdd = count;

            for (var i = 0; i < count; ++i, --toAdd)
            {
                total += toAdd * toAdd;
            }

            var rand = Utility.Random(total);
            toAdd = count;

            var val = min;

            for (var i = 0; i < count; ++i, --toAdd, ++val)
            {
                rand -= toAdd * toAdd;

                if (rand < 0)
                {
                    break;
                }
            }

            return val;
        }

        public bool PackSlayer(double chance = 0.05)
        {
            if (chance <= Utility.RandomDouble())
            {
                return false;
            }

            if (Utility.RandomBool())
            {
                var instrument = Loot.RandomInstrument();

                if (instrument != null)
                {
                    instrument.Slayer = SlayerGroup.GetLootSlayerType(GetType());
                    PackItem(instrument);
                }
            }
            else if (!Core.AOS)
            {
                var weapon = Loot.RandomWeapon();

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
            {
                return false;
            }

            minLevel = Math.Clamp(minLevel, 0, 5);
            maxLevel = Math.Clamp(maxLevel, 0, 5);

            if (Core.AOS)
            {
                var item = Loot.RandomWeaponOrJewelry();

                if (item == null)
                {
                    return false;
                }

                GetRandomAOSStats(minLevel, maxLevel, out var attributeCount, out var min, out var max);

                if (item is BaseWeapon weapon)
                {
                    BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
                }
                else if (item is BaseJewel jewel)
                {
                    BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);
                }

                PackItem(item);
            }
            else
            {
                var weapon = Loot.RandomWeapon();

                if (weapon == null)
                {
                    return false;
                }

                if (Utility.RandomDouble() < 0.05)
                {
                    weapon.Slayer = SlayerName.Silver;
                }

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
            {
                PackItem(new Gold(amount));
            }
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
            for (var i = 0; i < amount; ++i)
            {
                PackStatue();
            }
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
            {
                return;
            }

            var gem = Loot.RandomGem();

            gem.Amount = amount;

            PackItem(gem);
        }

        public void PackNecroReg(int min, int max)
        {
            PackNecroReg(Utility.RandomMinMax(min, max));
        }

        public void PackNecroReg(int amount)
        {
            for (var i = 0; i < amount; ++i)
            {
                PackNecroReg();
            }
        }

        public void PackNecroReg()
        {
            if (!Core.AOS)
            {
                return;
            }

            PackItem(Loot.RandomNecromancyReagent());
        }

        public void PackReg(int min, int max)
        {
            PackReg(Utility.RandomMinMax(min, max));
        }

        public void PackReg(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var reg = Loot.RandomReagent();

            reg.Amount = amount;

            PackItem(reg);
        }

        public void PackItem(Item item)
        {
            if (item == null)
            {
                return;
            }

            if (Summoned)
            {
                item.Delete();
                return;
            }

            var pack = Backpack ?? new Backpack { Movable = false };
            AddItem(pack);

            if (!item.Stackable || !pack.TryDropItem(this, item, false)) // try stack
            {
                pack.DropItem(item); // failed, drop it anyway
            }
        }

        public virtual void HealStart(Mobile patient)
        {
            StopHeal();

            var onSelf = patient == this;

            RevealingAction();

            if (!onSelf)
            {
                patient.RevealingAction();
                patient.SendLocalizedMessage(1008078, false, Name); // : Attempting to heal you.
            }

            var seconds = (onSelf ? HealDelay : HealOwnerDelay) + (patient.Alive ? 0.0 : 5.0);

            Timer.StartTimer(TimeSpan.FromSeconds(seconds), () => Heal(patient), out _healTimerToken);
        }

        public virtual void Heal(Mobile patient)
        {
            if (!Alive || Map == Map.Internal || !CanBeBeneficial(patient, true, true) || patient.Map != Map ||
                !InRange(patient, HealEndRange))
            {
                StopHeal();
                return;
            }

            if (!InRange(patient, HealStartRange))
            {
                return;
            }

            var onSelf = patient == this;
            if (patient.Poisoned)
            {
                var poisonLevel = patient.Poison.Level;

                var healing = Skills.Healing.Value;
                var anatomy = Skills.Anatomy.Value;
                var chance = (healing - 30.0) / 50.0 - poisonLevel * 0.1;

                if (healing >= 60.0 && anatomy >= 60.0 && chance > Utility.RandomDouble())
                {
                    if (patient.CurePoison(this))
                    {
                        patient.SendLocalizedMessage(1010059); // You have been cured of all poisons.

                        CheckSkill(SkillName.Healing, 0.0, 60.0 + poisonLevel * 10.0); //TODO Verify formula
                        CheckSkill(SkillName.Anatomy, 0.0, 100.0);
                    }
                }
            }
            else if (BleedAttack.IsBleeding(patient))
            {
                patient.SendLocalizedMessage(1060167); // The bleeding wounds have healed, you are no longer bleeding!
                BleedAttack.EndBleed(patient, false);
            }
            else
            {
                var healing = Skills.Healing.Value;
                var anatomy = Skills.Anatomy.Value;
                var chance = (healing + 10.0) / 100.0;

                if (chance > Utility.RandomDouble())
                {
                    var min = anatomy / 10.0 + healing / 6.0 + 4.0;
                    var max = anatomy / 8.0 + healing / 3.0 + 4.0;

                    if (onSelf)
                    {
                        max += 10;
                    }

                    var toHeal = min + Utility.RandomDouble() * (max - min);

                    toHeal *= HealScalar;

                    patient.Heal((int)toHeal);

                    CheckSkill(SkillName.Healing, 0.0, 90.0);
                    CheckSkill(SkillName.Anatomy, 0.0, 100.0);
                }
            }

            HealEffect(patient);

            StopHeal();

            if (onSelf && HealFully && Hits >= HealTrigger * HitsMax && Hits < HitsMax ||
                !onSelf && HealOwnerFully && patient.Hits >= HealOwnerTrigger * patient.HitsMax &&
                patient.Hits < patient.HitsMax)
            {
                HealStart(patient);
            }
        }

        public virtual void StopHeal()
        {
            _healTimerToken.Cancel();
        }

        public virtual void HealEffect(Mobile patient)
        {
            patient.PlaySound(HealSound);
        }

        public virtual void AuraDamage()
        {
            if (!Alive || IsDeadBondedPet)
            {
                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();
            foreach (var m in GetMobilesInRange(AuraRange))
            {
                if (m != this && CanBeHarmful(m, false) && (Core.AOS || InLOS(m)) &&
                    (m is BaseCreature bc && (bc.Controlled || bc.Summoned || bc.Team != Team) || m.Player))
                {
                    queue.Enqueue(m);
                }
            }

            while (queue.Count > 0)
            {
                var m = queue.Dequeue();

                AOS.Damage(
                    m,
                    this,
                    AuraBaseDamage,
                    AuraPhysicalDamage,
                    AuraFireDamage,
                    AuraColdDamage,
                    AuraPoisonDamage,
                    AuraEnergyDamage,
                    AuraChaosDamage
                );
                AuraEffect(m);
            }
        }

        public virtual void AuraEffect(Mobile m)
        {
        }

        private class TameEntry : ContextMenuEntry
        {
            public TameEntry(bool enabled) : base(6130, 6) => Enabled = enabled;

            public override void OnClick(Mobile from, IEntity target)
            {
                if (!from.CheckAlive() || target is not BaseCreature bc)
                {
                    return;
                }

                from.TargetLocked = true;
                AnimalTaming.DisableMessage = true;

                if (from.UseSkill(SkillName.AnimalTaming))
                {
                    from.Target.Invoke(from, bc);
                }

                AnimalTaming.DisableMessage = false;
                from.TargetLocked = false;
            }
        }

        private class DeathAdderCharmTarget : Target
        {
            private readonly BaseCreature m_Charmed;

            public DeathAdderCharmTarget(BaseCreature charmed) : base(-1, false, TargetFlags.Harmful) => m_Charmed = charmed;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!m_Charmed.DeathAdderCharmable || m_Charmed.Combatant != null || !from.CanBeHarmful(m_Charmed, false))
                {
                    return;
                }

                if (!(SummonFamiliarSpell.Table.TryGetValue(from, out var bc) && (bc as DeathAdder)?.Deleted == false))
                {
                    return;
                }

                if (!(targeted is Mobile targ && from.CanBeHarmful(targ, false)))
                {
                    return;
                }

                from.RevealingAction();
                from.DoHarmful(targ, true);

                m_Charmed.Combatant = targ;

                if (m_Charmed.AIObject != null)
                {
                    m_Charmed.AIObject.Action = ActionType.Combat;
                }
            }
        }

        private class DeleteTimer : Timer
        {
            private readonly Mobile m;

            public DeleteTimer(Mobile creature, TimeSpan delay) : base(delay)
            {
                m = creature;
            }

            protected override void OnTick()
            {
                m.Delete();
            }
        }
    }

    public class LoyaltyTimer : Timer
    {
        private static readonly TimeSpan InternalDelay = TimeSpan.FromMinutes(5.0);

        private DateTime m_NextHourlyCheck;

        public LoyaltyTimer() : base(InternalDelay, InternalDelay) =>
            m_NextHourlyCheck = Core.Now + TimeSpan.FromHours(1.0);

        public static void Initialize()
        {
            new LoyaltyTimer().Start();
        }

        protected override void OnTick()
        {
            if (Core.Now < m_NextHourlyCheck)
            {
                return;
            }

            m_NextHourlyCheck = Core.Now + TimeSpan.FromHours(1.0);

            using var toRelease = PooledRefQueue<BaseCreature>.Create();

            using var toRemove = PooledRefQueue<Mobile>.Create();

            foreach (var m in World.Mobiles.Values)
            {
                if (m is not BaseCreature c)
                {
                    continue;
                }

                if (c is BaseMount mount && mount.Rider != null)
                {
                    mount.OwnerAbandonTime = DateTime.MinValue;
                    continue;
                }

                if (c.IsDeadPet)
                {
                    var owner = c.ControlMaster;

                    if (!c.IsStabled && (owner?.Deleted != false || owner.Map != c.Map ||
                                         !owner.InRange(c, 12) || !c.CanSee(owner) || !c.InLOS(owner)))
                    {
                        if (c.OwnerAbandonTime == DateTime.MinValue)
                        {
                            c.OwnerAbandonTime = Core.Now;
                        }
                        else if (c.OwnerAbandonTime + c.BondingAbandonDelay <= Core.Now)
                        {
                            toRemove.Enqueue(c);
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
                        {
                            toRelease.Enqueue(c);
                        }
                    }
                }

                // Wild creatures squatting in houses are removed outright.
                if (!c.Controlled && !c.IsStabled && (c.Region.IsPartOf<HouseRegion>() && c.CanBeDamaged() ||
                                                      c.RemoveIfUntamed && c.Spawner == null))
                {
                    c.RemoveStep++;

                    if (c.RemoveStep >= 20)
                    {
                        toRemove.Enqueue(c);
                    }
                }
                else
                {
                    c.RemoveStep = 0;
                }
            }

            while (toRelease.Count > 0)
            {
                var c = toRelease.Dequeue();

                c.Say(1043255, c.Name); // ~1_NAME~ appears to have decided that is better off without a master!
                c.Loyalty = BaseCreature.MaxLoyalty;
                c.IsBonded = false;
                c.BondingBegin = DateTime.MinValue;
                c.OwnerAbandonTime = DateTime.MinValue;
                c.ControlTarget = null;
                // Release directly: a creature left alone with its AI disabled would
                // otherwise never release and permanently hold its owner's follower slots.
                c.AIObject.DoOrderRelease();
                c.DropBackpack();
            }

            while (toRemove.Count > 0)
            {
                toRemove.Dequeue().Delete();
            }
        }
    }
}
