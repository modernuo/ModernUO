using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Buffers;
using Server.ContextMenus;
using Server.Guilds;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Logging;
using Server.Menus;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using Server.Utilities;
using CalcMoves = Server.Movement.Movement;

namespace Server
{
    public delegate void TargetCallback(Mobile from, object targeted);

    public delegate void TargetStateCallback<in T>(Mobile from, object targeted, T state);

    public delegate void PromptCallback(Mobile from, string text);

    public delegate void PromptStateCallback<in T>(Mobile from, string text, T state);

    public class TimedSkillMod : SkillMod
    {
        private readonly DateTime m_Expire;

        public TimedSkillMod(SkillName skill, bool relative, double value, TimeSpan delay)
            : this(skill, relative, value, Core.Now + delay)
        {
        }

        public TimedSkillMod(SkillName skill, bool relative, double value, DateTime expire)
            : base(skill, relative, value) =>
            m_Expire = expire;

        public override bool CheckCondition() => Core.Now < m_Expire;
    }

    public class EquippedSkillMod : SkillMod
    {
        private readonly Item m_Item;
        private readonly Mobile m_Mobile;

        public EquippedSkillMod(SkillName skill, bool relative, double value, Item item, Mobile mobile)
            : base(skill, relative, value)
        {
            m_Item = item;
            m_Mobile = mobile;
        }

        public override bool CheckCondition() => !m_Item.Deleted && !m_Mobile.Deleted && m_Item.Parent == m_Mobile;
    }

    public class DefaultSkillMod : SkillMod
    {
        public DefaultSkillMod(SkillName skill, bool relative, double value)
            : base(skill, relative, value)
        {
        }

        public override bool CheckCondition() => true;
    }

    public abstract class SkillMod
    {
        private bool m_ObeyCap;
        private Mobile m_Owner;
        private bool m_Relative;
        private SkillName m_Skill;
        private double m_Value;

        protected SkillMod(SkillName skill, bool relative, double value)
        {
            m_Skill = skill;
            m_Relative = relative;
            m_Value = value;
        }

        public bool ObeyCap
        {
            get => m_ObeyCap;
            set
            {
                m_ObeyCap = value;

                var sk = m_Owner?.Skills[m_Skill];
                sk?.Update();
            }
        }

        public Mobile Owner
        {
            get => m_Owner;
            set
            {
                if (m_Owner != value)
                {
                    m_Owner?.RemoveSkillMod(this);

                    m_Owner = value;

                    if (m_Owner != value)
                    {
                        m_Owner.AddSkillMod(this);
                    }
                }
            }
        }

        public SkillName Skill
        {
            get => m_Skill;
            set
            {
                if (m_Skill != value)
                {
                    var oldUpdate = m_Owner?.Skills[m_Skill];

                    m_Skill = value;

                    var sk = m_Owner?.Skills[m_Skill];
                    sk?.Update();
                    oldUpdate?.Update();
                }
            }
        }

        public bool Relative
        {
            get => m_Relative;
            set
            {
                if (m_Relative != value)
                {
                    m_Relative = value;

                    var sk = m_Owner?.Skills[m_Skill];
                    sk?.Update();
                }
            }
        }

        public bool Absolute
        {
            get => !m_Relative;
            set
            {
                if (m_Relative == value)
                {
                    m_Relative = !value;

                    var sk = m_Owner?.Skills[m_Skill];
                    sk?.Update();
                }
            }
        }

        public double Value
        {
            get => m_Value;
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;

                    var sk = m_Owner?.Skills[m_Skill];
                    sk?.Update();
                }
            }
        }

        public void Remove()
        {
            Owner = null;
        }

        public abstract bool CheckCondition();
    }

    public class ResistanceMod
    {
        private int m_Offset;
        private ResistanceType m_Type;

        public ResistanceMod(ResistanceType type, int offset)
        {
            m_Type = type;
            m_Offset = offset;
        }

        public Mobile Owner { get; set; }

        public ResistanceType Type
        {
            get => m_Type;
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;

                    Owner?.UpdateResistances();
                }
            }
        }

        public int Offset
        {
            get => m_Offset;
            set
            {
                if (m_Offset != value)
                {
                    m_Offset = value;

                    Owner?.UpdateResistances();
                }
            }
        }
    }

    public class StatMod
    {
        private readonly DateTime m_Added;
        private readonly TimeSpan m_Duration;

        public StatMod(StatType type, string name, int offset, TimeSpan duration)
        {
            Type = type;
            Name = name;
            Offset = offset;
            m_Duration = duration;
            m_Added = Core.Now;
        }

        public StatType Type { get; }

        public string Name { get; }

        public int Offset { get; }

        public bool HasElapsed() => m_Duration != TimeSpan.Zero && Core.Now - m_Added >= m_Duration;
    }

    public class DamageEntry
    {
        public DamageEntry(Mobile damager) => Damager = damager;

        public Mobile Damager { get; }

        public int DamageGiven { get; set; }

        public DateTime LastDamage { get; set; }

        public bool HasExpired => Core.Now > LastDamage + ExpireDelay;

        public List<DamageEntry> Responsible { get; set; }

        public static TimeSpan ExpireDelay { get; set; } = TimeSpan.FromMinutes(2.0);
    }

    [Flags]
    public enum StatType
    {
        Str = 1,
        Dex = 2,
        Int = 4,
        All = 7
    }

    public enum StatLockType : byte
    {
        Up,
        Down,
        Locked
    }

    [Flags]
    [CustomEnum(new[] { "North", "Right", "East", "Down", "South", "Left", "West", "Up" })]
    public enum Direction : byte
    {
        North = 0x0,
        Right = 0x1,
        East = 0x2,
        Down = 0x3,
        South = 0x4,
        Left = 0x5,
        West = 0x6,
        Up = 0x7,

        Mask = 0x7,
        Running = 0x80,
        ValueMask = 0x87
    }

    [Flags]
    public enum MobileDelta
    {
        None = 0x00000000,
        Name = 0x00000001,
        Flags = 0x00000002,
        Hits = 0x00000004,
        Mana = 0x00000008,
        Stam = 0x00000010,
        Stat = 0x00000020,
        Noto = 0x00000040,
        Gold = 0x00000080,
        Weight = 0x00000100,
        Direction = 0x00000200,
        Hue = 0x00000400,
        Body = 0x00000800,
        Armor = 0x00001000,
        StatCap = 0x00002000,
        GhostUpdate = 0x00004000,
        Followers = 0x00008000,
        Properties = 0x00010000,
        TithingPoints = 0x00020000,
        Resistances = 0x00040000,
        WeaponDamage = 0x00080000,
        Hair = 0x00100000,
        FacialHair = 0x00200000,
        Race = 0x00400000,
        HealthbarYellow = 0x00800000,
        HealthbarPoison = 0x01000000,

        Attributes = 0x0000001C
    }

    public enum Healthbar
    {
        Normal,
        Poison,
        Yellow
    }

    public enum AccessLevel
    {
        Player,
        Counselor,
        GameMaster,
        Seer,
        Administrator,
        Developer,
        Owner
    }

    public enum VisibleDamageType
    {
        None,
        Related,
        Everyone,
        Selective
    }

    public enum ResistanceType
    {
        Physical,
        Fire,
        Cold,
        Poison,
        Energy
    }

    public enum ApplyPoisonResult
    {
        Poisoned,
        Immune,
        HigherPoisonActive,
        Cured
    }

    public delegate bool SkillCheckTargetHandler(
        Mobile from, SkillName skill, object target, double minSkill,
        double maxSkill
    );

    public delegate bool SkillCheckLocationHandler(Mobile from, SkillName skill, double minSkill, double maxSkill);

    public delegate bool SkillCheckDirectTargetHandler(Mobile from, SkillName skill, object target, double chance);

    public delegate bool SkillCheckDirectLocationHandler(Mobile from, SkillName skill, double chance);

    public delegate TimeSpan RegenRateHandler(Mobile from);

    public delegate bool AllowBeneficialHandler(Mobile from, Mobile target);

    public delegate bool AllowHarmfulHandler(Mobile from, Mobile target);

    public delegate Container CreateCorpseHandler(
        Mobile from, HairInfo hair, FacialHairInfo facialhair, List<Item> initialContent, List<Item> equippedItems
    );

    public delegate int AOSStatusHandler(Mobile from, int index);

    /// <summary>
    ///     Base class representing players, npcs, and creatures.
    /// </summary>
    public class Mobile : IHued, IComparable<Mobile>, ISpawnable, IPropertyListObject
    {
        // Allow four warmode changes in 0.5 seconds, any more will be delay for two seconds
        private const int WarmodeCatchCount = 4;

        private static readonly ILogger logger = LogFactory.GetLogger(typeof(Mobile));

        // TODO: Make these configurations
        private static readonly TimeSpan WarmodeSpamCatch = TimeSpan.FromSeconds(Core.SE ? 1.0 : 0.5);
        private static readonly TimeSpan WarmodeSpamDelay = TimeSpan.FromSeconds(Core.SE ? 4.0 : 2.0);
        private static readonly TimeSpan ExpireCombatantDelay = TimeSpan.FromMinutes(1.0);
        private static readonly TimeSpan ExpireAggressorsDelay = TimeSpan.FromSeconds(5.0);

        private static readonly List<IEntity> m_MoveList = new();
        private static readonly List<Mobile> m_MoveClientList = new();

        private static readonly object m_GhostMutateContext = new();

        private static readonly List<Mobile> m_Hears = new();
        private static readonly List<IEntity> m_OnSpeech = new();

        private static readonly string[] m_AccessLevelNames =
        {
            "a player",
            "a counselor",
            "a game master",
            "a seer",
            "an administrator",
            "a developer",
            "an owner"
        };

        private static readonly int[] m_InvalidBodies =
        {
            32,
            95,
            156,
            197,
            198
        };

        private static readonly Queue<Mobile> m_DeltaQueue = new();

        private static readonly string[] m_GuildTypes =
        {
            "",
            " (Chaos)",
            " (Order)"
        };

        private List<object> _actions;
        private AccessLevel m_AccessLevel;

        private TimerExecutionToken _autoManifestTimerToken;

        private Container m_Backpack;

        private BankBox m_BankBox;
        private Body m_Body;
        private Body m_BodyMod;
        private bool m_CanHearGhosts;

        private int m_ChangingCombatant;
        private Mobile m_Combatant;
        private TimerExecutionToken _combatTimerToken;
        private ContextMenu m_ContextMenu;
        private bool m_Criminal;

        private MobileDelta m_DeltaFlags;
        private Direction m_Direction;
        private bool m_DisplayGuildTitle;

        private TimerExecutionToken _expireAggrTimerToken;
        private TimerExecutionToken _expireCombatantTimerToken;
        private TimerExecutionToken _expireCriminalTimerToken;
        private FacialHairInfo m_FacialHair;
        private int m_Fame, m_Karma;
        private bool m_Female, m_Warmode, m_Hidden, m_Blessed, m_Flying;
        private int m_Followers, m_FollowersMax;
        private bool m_Frozen;
        private TimerExecutionToken _frozenTimerToken;
        private BaseGuild m_Guild;
        private string m_GuildTitle;

        private HairInfo m_Hair;
        private int m_Hits, m_Stam, m_Mana;

        private Item m_Holding;
        private int m_Hue;

        private int m_HueMod = -1;
        private int m_Hunger;

        private bool m_InDeltaQueue;
        private int m_Kills, m_ShortTermMurders;
        private string m_Language;
        private int m_LightLevel;
        private Point3D m_Location;
        private TimerExecutionToken _logoutTimerToken;
        private Timer m_ManaTimer, m_HitsTimer, m_StamTimer;

        private Map m_Map;

        /* Logout:
         *
         * When a client logs into mobile x
         *  - if (x is Internalized ) move x to logout location and map
         *
         * When a client attached to a mobile disconnects
         *  - LogoutTimer is started
         *     - Delay is taken from Region.GetLogoutDelay to allow insta-logout regions.
         *     - OnTick : Location and map are stored, and mobile is internalized
         *
         * Some things to consider:
         *  - An internalized person getting killed (say, by poison). Where does the body go?
         *  - Regions now have a GetLogoutDelay( Mobile m ); virtual function (see above)
         */

        private Item m_MountItem;
        private string m_Name;

        private string m_NameMod;
        private NetState m_NetState;
        private DateTime m_NextWarmodeChange;
        private bool m_Paralyzed;
        private TimerExecutionToken _paraTimerToken;
        private bool m_Player;
        private Poison m_Poison;
        private Prompt m_Prompt;
        private ObjectPropertyList m_PropertyList;
        private Race m_Race;
        private Region m_Region;

        private int m_SolidHueOverride = -1;
        private ISpell m_Spell;
        private int m_StatCap;
        private int m_Str, m_Dex, m_Int;

        private StatLockType m_StrLock, m_DexLock, m_IntLock;
        private Target m_Target;
        private int m_TithingPoints;
        private string m_Title;
        private int m_TotalGold, m_TotalItems, m_TotalWeight;
        private int m_VirtualArmor;
        private int m_VirtualArmorMod;
        private int m_WarmodeChanges;
        private bool _warmodeSpamValue;
        private IWeapon m_Weapon;

        private bool m_YellowHealthbar;

        public Mobile()
        {
            m_Region = Map.Internal.DefaultRegion;
            Serial = World.NewMobile;

            DefaultMobileInit();

            World.AddEntity(this);
            SetTypeRef(GetType());
        }

        public Mobile(Serial serial)
        {
            m_Region = Map.Internal.DefaultRegion;
            Serial = serial;
            Aggressors = new List<AggressorInfo>();
            Aggressed = new List<AggressorInfo>();
            NextSkillTime = Core.TickCount;
            DamageEntries = new List<DamageEntry>();

            SetTypeRef(GetType());
        }

        public void SetTypeRef(Type type)
        {
            TypeRef = World.MobileTypes.IndexOf(type);

            if (TypeRef == -1)
            {
                World.MobileTypes.Add(type);
                TypeRef = World.MobileTypes.Count - 1;
            }
        }

        public static bool DragEffects { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public Race Race
        {
            get => m_Race ??= Race.DefaultRace;
            set
            {
                var oldRace = Race;

                m_Race = value ?? Race.DefaultRace;

                Body = m_Race.Body(this);
                UpdateResistances();

                Delta(MobileDelta.Race);

                OnRaceChange(oldRace);
            }
        }

        public virtual double RacialSkillBonus => 0;

        public int[] Resistances { get; private set; }

        public virtual int BasePhysicalResistance => 0;
        public virtual int BaseFireResistance => 0;
        public virtual int BaseColdResistance => 0;
        public virtual int BasePoisonResistance => 0;
        public virtual int BaseEnergyResistance => 0;

        [CommandProperty(AccessLevel.Counselor)]
        public virtual int PhysicalResistance => GetResistance(ResistanceType.Physical);

        [CommandProperty(AccessLevel.Counselor)]
        public virtual int FireResistance => GetResistance(ResistanceType.Fire);

        [CommandProperty(AccessLevel.Counselor)]
        public virtual int ColdResistance => GetResistance(ResistanceType.Cold);

        [CommandProperty(AccessLevel.Counselor)]
        public virtual int PoisonResistance => GetResistance(ResistanceType.Poison);

        [CommandProperty(AccessLevel.Counselor)]
        public virtual int EnergyResistance => GetResistance(ResistanceType.Energy);

        public List<ResistanceMod> ResistanceMods { get; set; }

        public static int MaxPlayerResistance { get; set; } = 70;

        public virtual bool NewGuildDisplay => false;

        public List<Mobile> Stabled { get; private set; }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public VirtueInfo Virtues { get; private set; }

        public object Party { get; set; }

        public List<SkillMod> SkillMods { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VirtualArmorMod
        {
            get => m_VirtualArmorMod;
            set
            {
                if (m_VirtualArmorMod != value)
                {
                    m_VirtualArmorMod = value;

                    Delta(MobileDelta.Armor);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MeleeDamageAbsorb { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MagicDamageAbsorb { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SkillsTotal => Skills?.Total ?? 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public int SkillsCap
        {
            get => Skills?.Cap ?? 0;
            set
            {
                if (Skills != null)
                {
                    Skills.Cap = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BaseSoundID { get; set; }

        public long NextCombatTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NameHue { get; set; } = -1;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Hunger
        {
            get => m_Hunger;
            set
            {
                var oldValue = m_Hunger;

                if (oldValue != value)
                {
                    m_Hunger = value;

                    EventSink.InvokeHungerChanged(this, oldValue);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Thirst { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BAC { get; set; }

        /// <summary>
        ///     Gets or sets the number of steps this player may take when hidden before being revealed.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int AllowedStealthSteps { get; set; }

        public Item Holding
        {
            get => m_Holding;
            set
            {
                if (m_Holding != value)
                {
                    if (m_Holding != null)
                    {
                        UpdateTotal(m_Holding, TotalType.Weight, -(m_Holding.TotalWeight + m_Holding.PileWeight));

                        if (m_Holding.HeldBy == this)
                        {
                            m_Holding.HeldBy = null;
                        }
                    }

                    if (value != null && m_Holding != null)
                    {
                        DropHolding();
                    }

                    m_Holding = value;

                    if (m_Holding != null)
                    {
                        UpdateTotal(m_Holding, TotalType.Weight, m_Holding.TotalWeight + m_Holding.PileWeight);

                        m_Holding.HeldBy ??= this;
                    }
                }
            }
        }

        public long LastMoveTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Paralyzed
        {
            get => m_Paralyzed;
            set
            {
                if (m_Paralyzed != value)
                {
                    m_Paralyzed = value;
                    Delta(MobileDelta.Flags);

                    SendLocalizedMessage(m_Paralyzed ? 502381 : 502382);
                    _paraTimerToken.Cancel();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DisarmReady { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StunReady { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Frozen
        {
            get => m_Frozen;
            set
            {
                if (m_Frozen != value)
                {
                    m_Frozen = value;
                    Delta(MobileDelta.Flags);
                    _frozenTimerToken.Cancel();
                }
            }
        }

        /// <summary>
        ///     Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawStr" /> property.
        /// </summary>
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public StatLockType StrLock
        {
            get => m_StrLock;
            set
            {
                if (m_StrLock != value)
                {
                    m_StrLock = value;

                    m_NetState.SendStatLockInfo(this);
                }
            }
        }

        /// <summary>
        ///     Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawDex" /> property.
        /// </summary>
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public StatLockType DexLock
        {
            get => m_DexLock;
            set
            {
                if (m_DexLock != value)
                {
                    m_DexLock = value;

                    m_NetState.SendStatLockInfo(this);
                }
            }
        }

        /// <summary>
        ///     Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawInt" /> property.
        /// </summary>
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public StatLockType IntLock
        {
            get => m_IntLock;
            set
            {
                if (m_IntLock != value)
                {
                    m_IntLock = value;

                    m_NetState.SendStatLockInfo(this);
                }
            }
        }

        public long NextActionTime { get; set; }

        public long NextActionMessage { get; set; }

        public static int ActionMessageDelay { get; set; } = 125;

        public static bool GlobalRegenThroughPoison { get; set; } = true;

        public virtual bool RegenThroughPoison => GlobalRegenThroughPoison;

        public virtual bool CanRegenHits => Alive && (RegenThroughPoison || !Poisoned);
        public virtual bool CanRegenStam => Alive;
        public virtual bool CanRegenMana => Alive;

        public long NextSkillTime { get; set; }

        public List<AggressorInfo> Aggressors { get; private set; }

        public List<AggressorInfo> Aggressed { get; private set; }

        public bool ChangingCombatant => m_ChangingCombatant > 0;

        private void ExpireCombatant()
        {
            Combatant = null;
            _expireCombatantTimerToken.Cancel();
        }

        /// <summary>
        ///     Overridable. Gets or sets which Mobile that this Mobile is currently engaged in combat with.
        ///     <seealso cref="OnCombatantChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Mobile Combatant
        {
            get => m_Combatant;
            set
            {
                if (Deleted)
                {
                    return;
                }

                if (m_Combatant != value && value != this)
                {
                    var old = m_Combatant;

                    ++m_ChangingCombatant;
                    m_Combatant = value;

                    if (m_Combatant != null && !CanBeHarmful(m_Combatant, false) ||
                        !Region.OnCombatantChange(this, old, m_Combatant))
                    {
                        m_Combatant = old;
                        --m_ChangingCombatant;
                        return;
                    }

                    if (m_Combatant == null)
                    {
                        m_NetState.SendChangeCombatant(Serial.Zero);
                        _expireCombatantTimerToken.Cancel();
                        _combatTimerToken.Cancel();
                    }
                    else
                    {
                        m_NetState.SendChangeCombatant(m_Combatant.Serial);
                        if (!_expireCombatantTimerToken.Running)
                        {
                            Timer.StartTimer(ExpireCombatantDelay, ExpireCombatant, out _expireCombatantTimerToken);
                        }

                        if (!_combatTimerToken.Running)
                        {
                            Timer.StartTimer(TimeSpan.FromSeconds(0.01), 0, CheckCombatTime, out _combatTimerToken);
                        }

                        if (CanBeHarmful(m_Combatant, false))
                        {
                            DoHarmful(m_Combatant); // due to reflection, might make m_Combatant null
                            m_Combatant?.PlaySound(m_Combatant.GetAngerSound());
                        }
                    }

                    OnCombatantChange();
                    --m_ChangingCombatant;
                }
            }
        }

        private void CheckCombatTime()
        {
            if (Core.TickCount - NextCombatTime < 0)
            {
                return;
            }

            var combatant = Combatant;

            // If no combatant, wrong map, one of us is a ghost, or cannot see, or deleted, then stop combat
            if (combatant?.Deleted != false || Deleted || combatant.m_Map != m_Map ||
                !combatant.Alive || !Alive || !CanSee(combatant) || combatant.IsDeadBondedPet ||
                IsDeadBondedPet)
            {
                Combatant = null;
                return;
            }

            var weapon = Weapon;

            if (!InRange(combatant, weapon.MaxRange))
            {
                return;
            }

            if (InLOS(combatant))
            {
                weapon.OnBeforeSwing(this, combatant);
                RevealingAction();
                NextCombatTime =
                    Core.TickCount + (int)weapon.OnSwing(this, combatant).TotalMilliseconds;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalGold => GetTotal(TotalType.Gold);

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalItems => GetTotal(TotalType.Items);

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalWeight => GetTotal(TotalType.Weight);

        [CommandProperty(AccessLevel.GameMaster)]
        public int TithingPoints
        {
            get => m_TithingPoints;
            set
            {
                if (m_TithingPoints != value)
                {
                    m_TithingPoints = value;

                    Delta(MobileDelta.TithingPoints);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Followers
        {
            get => m_Followers;
            set
            {
                if (m_Followers != value)
                {
                    m_Followers = value;

                    Delta(MobileDelta.Followers);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FollowersMax
        {
            get => m_FollowersMax;
            set
            {
                if (m_FollowersMax != value)
                {
                    m_FollowersMax = value;

                    Delta(MobileDelta.Followers);
                }
            }
        }

        public bool TargetLocked { get; set; }

        public Target Target
        {
            get => m_Target;
            set
            {
                var oldTarget = m_Target;
                var newTarget = value;

                if (oldTarget == newTarget)
                {
                    return;
                }

                m_Target = null;

                if (oldTarget != null && newTarget != null)
                {
                    oldTarget.Cancel(this, TargetCancelType.Overridden);
                }

                m_Target = newTarget;

                if (newTarget != null && !TargetLocked)
                {
                    newTarget.SendTargetTo(m_NetState);
                }

                OnTargetChange();
            }
        }

        public ContextMenu ContextMenu
        {
            get => m_ContextMenu;
            set
            {
                m_ContextMenu = value;
                m_NetState.SendDisplayContextMenu(m_ContextMenu);
            }
        }

        public bool Pushing { get; set; }

        public virtual bool IsDeadBondedPet => false;

        public ISpell Spell
        {
            get => m_Spell;
            set
            {
                if (m_Spell != null && value != null)
                {
                    Console.WriteLine("Warning: Spell has been overwritten");
                }

                m_Spell = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool AutoPageNotify { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public IAccount Account { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VirtualArmor
        {
            get => m_VirtualArmor;
            set
            {
                if (m_VirtualArmor != value)
                {
                    m_VirtualArmor = value;

                    Delta(MobileDelta.Armor);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double ArmorRating => 0.0;

        /// <summary>
        ///     Overridable. Returns true if the player is alive, false if otherwise. By default, this is computed by:
        ///     <c>!Deleted &amp;&amp; (!Player || !Body.IsGhost)</c>
        /// </summary>
        [CommandProperty(AccessLevel.Counselor)]
        public virtual bool Alive => !Deleted && (!m_Player || !m_Body.IsGhost);

        public static CreateCorpseHandler CreateCorpseHandler { get; set; }

        public virtual bool RetainPackLocsOnDeath => Core.AOS;

        [CommandProperty(AccessLevel.GameMaster)]
        public Container Corpse { get; set; }

        public static char[] GhostChars { get; set; }
        public static bool NoSpeechLOS { get; set; }

        public static TimeSpan AutoManifestTimeout { get; set; } = TimeSpan.FromSeconds(5.0);

        public static bool InsuranceEnabled { get; set; }

        public static int ActionDelay { get; set; } = 500;

        public static VisibleDamageType VisibleDamageType { get; set; }

        public List<DamageEntry> DamageEntries { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile LastKiller { get; set; }

        public static bool DefaultShowVisibleDamage { get; set; }

        public static bool DefaultCanSeeVisibleDamage { get; set; }

        public virtual bool ShowVisibleDamage => DefaultShowVisibleDamage;
        public virtual bool CanSeeVisibleDamage => DefaultCanSeeVisibleDamage;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Squelched { get; set; }

        public virtual bool ShouldCheckStatTimers => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public int LightLevel
        {
            get => m_LightLevel;
            set
            {
                if (m_LightLevel != value)
                {
                    m_LightLevel = value;

                    CheckLightLevels(false);
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public string Profile { get; set; }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool ProfileLocked { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Player
        {
            get => m_Player;
            set
            {
                m_Player = value;
                InvalidateProperties();
                CheckStatTimers();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                InvalidateProperties();
            }
        }

        public List<Item> Items { get; private set; }

        public virtual int MaxWeight => int.MaxValue;

        public static IWeapon DefaultWeapon { get; set; }

        [CommandProperty(AccessLevel.Counselor)]
        public Skills Skills { get; private set; }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public AccessLevel AccessLevel
        {
            get => m_AccessLevel;
            set
            {
                var oldValue = m_AccessLevel;

                if (oldValue != value)
                {
                    m_AccessLevel = value;
                    Delta(MobileDelta.Noto);
                    InvalidateProperties();

                    SendMessage("Your access level has been changed. You are now {0}.", GetAccessLevelName(value));

                    ClearScreen();
                    SendEverything();

                    OnAccessLevelChanged(oldValue);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Fame
        {
            get => m_Fame;
            set
            {
                var oldValue = m_Fame;

                if (oldValue != value)
                {
                    m_Fame = value;

                    if (ShowFameTitle && (m_Player || m_Body.IsHuman) && oldValue >= 10000 != value >= 10000)
                    {
                        InvalidateProperties();
                    }

                    OnFameChange(oldValue);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Karma
        {
            get => m_Karma;
            set
            {
                var old = m_Karma;

                if (old != value)
                {
                    m_Karma = value;
                    OnKarmaChange(old);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Blessed
        {
            get => m_Blessed;
            set
            {
                if (m_Blessed != value)
                {
                    m_Blessed = value;
                    Delta(MobileDelta.HealthbarYellow);
                }
            }
        }

        public virtual int Luck => 0;

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public int HueMod
        {
            get => m_HueMod;
            set
            {
                if (m_HueMod != value)
                {
                    m_HueMod = value;

                    Delta(MobileDelta.Hue);
                }
            }
        }

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Hue
        {
            get => m_HueMod != -1 ? m_HueMod : m_Hue;
            set
            {
                var oldHue = m_Hue;

                if (oldHue != value)
                {
                    m_Hue = value;

                    Delta(MobileDelta.Hue);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Direction
        {
            get => m_Direction;
            set
            {
                if (m_Direction != value)
                {
                    m_Direction = value;
                    Delta(MobileDelta.Direction);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Female
        {
            get => m_Female;
            set
            {
                if (m_Female != value)
                {
                    m_Female = value;
                    Delta(MobileDelta.Flags);
                    OnGenderChanged(!m_Female);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Flying
        {
            get => m_Flying;
            set
            {
                if (m_Flying != value)
                {
                    m_Flying = value;
                    Delta(MobileDelta.Flags);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Warmode
        {
            get => m_Warmode;
            set
            {
                if (Deleted)
                {
                    return;
                }

                if (m_Warmode != value)
                {
                    _autoManifestTimerToken.Cancel();

                    m_Warmode = value;
                    Delta(MobileDelta.Flags);

                    m_NetState.SendSetWarMode(value);

                    if (!m_Warmode)
                    {
                        Combatant = null;
                    }

                    if (!Alive)
                    {
                        if (value)
                        {
                            Delta(MobileDelta.GhostUpdate);
                        }
                        else
                        {
                            SendRemovePacket(false);
                        }
                    }

                    OnWarmodeChanged();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Hidden
        {
            get => m_Hidden;
            set
            {
                if (m_Hidden != value)
                {
                    m_Hidden = value;
                    // Delta( MobileDelta.Flags );

                    OnHiddenChanged();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public NetState NetState
        {
            get
            {
                if (m_NetState?.Connection == null)
                {
                    m_NetState = null;
                }

                return m_NetState;
            }
            set
            {
                if (m_NetState == value)
                {
                    return;
                }

#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Attempting to set Mobile.NetState value from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

                m_Map?.OnClientChange(m_NetState, value, this);
                m_Target?.Cancel(this, TargetCancelType.Disconnected);
                m_Spell?.OnConnectionChanged();
                m_NetState?.CancelAllTrades();

                var box = FindBankNoCreate();

                if (box?.Opened == true)
                {
                    box.Close();
                }

                m_NetState = value;
                _logoutTimerToken.Cancel();

                if (m_NetState == null)
                {
                    OnDisconnected();
                    EventSink.InvokeDisconnected(this);

                    // Disconnected, start the logout timer
                    Timer.StartTimer(GetLogoutDelay(), Logout, out _logoutTimerToken);
                }
                else
                {
                    OnConnected();
                    EventSink.InvokeConnected(this);

                    if (m_Map == Map.Internal && LogoutMap != null)
                    {
                        Map = LogoutMap;
                        Location = LogoutLocation;
                    }
                }

                for (var i = Items.Count - 1; i >= 0; --i)
                {
                    if (i >= Items.Count)
                    {
                        continue;
                    }

                    var item = Items[i];

                    if (item is SecureTradeContainer)
                    {
                        for (var j = item.Items.Count - 1; j >= 0; --j)
                        {
                            if (j < item.Items.Count)
                            {
                                item.Items[j].OnSecureTrade(this, this, this, false);
                                AddToBackpack(item.Items[j]);
                            }
                        }

                        Timer.StartTimer(item.Delete);
                    }
                }

                DropHolding();
                OnNetStateChanged();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Language
        {
            get => m_Language;
            set => m_Language = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpeechHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EmoteHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WhisperHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int YellHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildTitle
        {
            get => m_GuildTitle;
            set
            {
                var old = m_GuildTitle;

                if (old != value)
                {
                    m_GuildTitle = value;

                    if (m_Guild?.Disbanded == false && m_GuildTitle != null)
                    {
                        SendLocalizedMessage(1018026, true, m_GuildTitle); // Your guild title has changed :
                    }

                    InvalidateProperties();

                    OnGuildTitleChange(old);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DisplayGuildTitle
        {
            get => m_DisplayGuildTitle;
            set
            {
                m_DisplayGuildTitle = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile GuildFealty { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string NameMod
        {
            get => m_NameMod;
            set
            {
                if (m_NameMod != value)
                {
                    m_NameMod = value;
                    Delta(MobileDelta.Name);
                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool YellowHealthbar
        {
            get => m_YellowHealthbar;
            set
            {
                m_YellowHealthbar = value;
                Delta(MobileDelta.HealthbarYellow);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RawName
        {
            get => m_Name;
            set => Name = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Name
        {
            get => m_NameMod ?? m_Name;
            set
            {
                if (m_Name != value) // I'm leaving out the && m_NameMod == null
                {
                    var oldName = m_Name;
                    m_Name = value;
                    OnAfterNameChange(oldName, m_Name);
                    Delta(MobileDelta.Name);
                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastStrGain { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastIntGain { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastDexGain { get; set; }

        public DateTime LastStatGain
        {
            get
            {
                var d = LastStrGain;

                if (LastIntGain > d)
                {
                    d = LastIntGain;
                }

                if (LastDexGain > d)
                {
                    d = LastDexGain;
                }

                return d;
            }
            set
            {
                LastStrGain = value;
                LastIntGain = value;
                LastDexGain = value;
            }
        }

        public BaseGuild Guild
        {
            get => m_Guild;
            set
            {
                var old = m_Guild;

                if (old != value)
                {
                    if (value == null)
                    {
                        GuildTitle = null;
                    }

                    m_Guild = value;

                    Delta(MobileDelta.Noto);
                    InvalidateProperties();

                    OnGuildChange(old);
                }
            }
        }

        public Region WalkRegion { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Poisoned => m_Poison != null;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBodyMod => m_BodyMod.BodyID != 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public Body BodyMod
        {
            get => m_BodyMod;
            set
            {
                if (m_BodyMod != value)
                {
                    m_BodyMod = value;

                    Delta(MobileDelta.Body);
                    InvalidateProperties();

                    CheckStatTimers();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Body Body
        {
            get => IsBodyMod ? m_BodyMod : m_Body;
            set
            {
                if (m_Body != value && !IsBodyMod)
                {
                    m_Body = SafeBody(value);

                    Delta(MobileDelta.Body);
                    InvalidateProperties();

                    CheckStatTimers();
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Point3D LogoutLocation { get; set; }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Map LogoutMap { get; set; }

        public Region Region => m_Region ?? (Map == null ? Map.Internal.DefaultRegion : Map.DefaultRegion);

        [CommandProperty(AccessLevel.GameMaster)]
        public int SolidHueOverride
        {
            get => m_SolidHueOverride;
            set
            {
                if (m_SolidHueOverride == value)
                {
                    return;
                }

                m_SolidHueOverride = value;
                Delta(MobileDelta.Hue | MobileDelta.Body);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual IWeapon Weapon
        {
            get
            {
                if (m_Weapon is Item item && !item.Deleted && item.Parent == this && CanSee(item))
                {
                    return m_Weapon;
                }

                m_Weapon = null;

                item = FindItemOnLayer(Layer.OneHanded) ?? FindItemOnLayer(Layer.TwoHanded);

                if (item is IWeapon weapon)
                {
                    return m_Weapon = weapon;
                }

                return GetDefaultWeapon();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BankBox BankBox
        {
            get
            {
                if (m_BankBox?.Deleted == false && m_BankBox.Parent == this)
                {
                    return m_BankBox;
                }

                m_BankBox = FindItemOnLayer(Layer.Bank) as BankBox;

                if (m_BankBox == null)
                {
                    AddItem(m_BankBox = new BankBox(this));
                }

                return m_BankBox;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Container Backpack
        {
            get
            {
                if (m_Backpack?.Deleted != false || m_Backpack.Parent != this)
                {
                    m_Backpack = FindItemOnLayer(Layer.Backpack) as Container;
                }

                return m_Backpack;
            }
        }

        public virtual bool KeepsItemsOnDeath => m_AccessLevel > AccessLevel.Player;

        public bool HasTrade => m_NetState?.Trades.Count > 0;

        public bool NoMoveHS { get; set; }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Kills
        {
            get => m_Kills;
            set
            {
                var oldValue = m_Kills;

                if (m_Kills != value)
                {
                    m_Kills = Math.Max(value, 0);

                    if (oldValue >= 5 != m_Kills >= 5)
                    {
                        Delta(MobileDelta.Noto);
                        InvalidateProperties();
                    }

                    OnKillsChange(oldValue);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ShortTermMurders
        {
            get => m_ShortTermMurders;
            set
            {
                if (m_ShortTermMurders != value)
                {
                    m_ShortTermMurders = Math.Max(value, 0);
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual bool Criminal
        {
            get => m_Criminal;
            set
            {
                if (m_Criminal != value)
                {
                    m_Criminal = value;
                    Delta(MobileDelta.Noto);
                    InvalidateProperties();
                }

                _expireCriminalTimerToken.Cancel();

                if (m_Criminal)
                {
                    Timer.StartTimer(ExpireCriminalDelay, ExpireCriminal, out _expireCriminalTimerToken);
                }
            }
        }

        public static bool DisableDismountInWarmode { get; set; }

        public static int BodyWeight { get; set; } = 11; // 11 + 3 for the backpack

        [CommandProperty(AccessLevel.GameMaster)]
        public IMount Mount
        {
            get
            {
                Item item = null;

                if (m_MountItem?.Deleted == false && m_MountItem.Parent == this)
                {
                    item = m_MountItem;
                }

                item ??= FindItemOnLayer(Layer.Mount);

                if (item is not IMountItem mountItem)
                {
                    return null;
                }

                m_MountItem = item;
                return mountItem.Mount;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Mounted => Mount != null;

        public virtual bool CanTarget => true;
        public virtual bool ClickTitle => true;

        public virtual bool PropertyTitle => OldPropertyTitles ? ClickTitle : true;

        public static bool DisableHiddenSelfClick { get; set; } = true;

        public static bool AsciiClickMessage { get; set; } = true;

        public static bool GuildClickMessage { get; set; } = true;

        public static bool OldPropertyTitles { get; set; }

        public virtual bool ShowFameTitle // (m_Player || m_Body.IsHuman) && m_Fame >= 10000; }
            => true;

        /// <summary>
        ///     Gets or sets the maximum attainable value for <see cref="RawStr" />, <see cref="RawDex" />, and <see cref="RawInt" />.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int StatCap
        {
            get => m_StatCap;
            set
            {
                if (m_StatCap != value)
                {
                    m_StatCap = value;

                    Delta(MobileDelta.StatCap);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Meditating { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanSwim { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CantWalk { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanHearGhosts
        {
            get => m_CanHearGhosts || AccessLevel >= AccessLevel.Counselor;
            set => m_CanHearGhosts = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RawStatTotal => RawStr + RawDex + RawInt;

        public long NextSpellTime { get; set; }

        public static AllowBeneficialHandler AllowBeneficialHandler { get; set; }

        public static AllowHarmfulHandler AllowHarmfulHandler { get; set; }

        public static SkillCheckTargetHandler SkillCheckTargetHandler { get; set; }

        public static SkillCheckLocationHandler SkillCheckLocationHandler { get; set; }

        public static SkillCheckDirectTargetHandler SkillCheckDirectTargetHandler { get; set; }

        public static SkillCheckDirectLocationHandler SkillCheckDirectLocationHandler { get; set; }

        public static AOSStatusHandler AOSStatusHandler { get; set; }

        public static RegenRateHandler HitsRegenRateHandler { get; set; }

        public static TimeSpan DefaultHitsRate { get; set; }

        public static RegenRateHandler StamRegenRateHandler { get; set; }

        public static TimeSpan DefaultStamRate { get; set; }

        public static RegenRateHandler ManaRegenRateHandler { get; set; }

        public static TimeSpan DefaultManaRate { get; set; }

        public static TimeSpan ExpireCriminalDelay { get; set; } = TimeSpan.FromMinutes(2.0);

        public Prompt Prompt
        {
            get => m_Prompt;
            set
            {
                var oldPrompt = m_Prompt;
                var newPrompt = value;

                if (oldPrompt == newPrompt)
                {
                    return;
                }

                m_Prompt = null;

                // TODO: Cancel the prompt anyway?
                if (newPrompt != null)
                {
                    oldPrompt?.OnCancel(this);
                }

                m_Prompt = newPrompt;
                NetState.SendPrompt(newPrompt);
            }
        }

        /// <summary>
        ///     Gets a list of all <see cref="StatMod">StatMod's</see> currently active for the Mobile.
        /// </summary>
        public List<StatMod> StatMods { get; private set; }

        /// <summary>
        ///     Gets or sets the base, unmodified, strength of the Mobile. Ranges from 1 to 65000, inclusive.
        ///     <seealso cref="Str" />
        ///     <seealso cref="StatMod" />
        ///     <seealso cref="OnRawStrChange" />
        ///     <seealso cref="OnRawStatChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int RawStr
        {
            get => m_Str;
            set
            {
                value = Math.Clamp(value, 1, 65000);

                if (m_Str != value)
                {
                    var oldValue = m_Str;

                    m_Str = value;
                    Delta(MobileDelta.Stat | MobileDelta.Hits);

                    if (Hits < HitsMax)
                    {
                        m_HitsTimer ??= new HitsTimer(this);
                        m_HitsTimer.Start();
                    }
                    else if (Hits > HitsMax)
                    {
                        Hits = HitsMax;
                    }

                    OnRawStrChange(oldValue);
                    OnRawStatChange(StatType.Str, oldValue);
                }
            }
        }

        /// <summary>
        ///     Gets or sets the effective strength of the Mobile. This is the sum of the <see cref="RawStr" /> plus any additional
        ///     modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change.
        ///     It ranges from 1 to 65000, inclusive.
        ///     <seealso cref="RawStr" />
        ///     <seealso cref="StatMod" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Str
        {
            get => Math.Clamp(m_Str + GetStatOffset(StatType.Str), 1, 65000);
            set
            {
                if (StatMods.Count == 0)
                {
                    RawStr = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the base, unmodified, dexterity of the Mobile. Ranges from 1 to 65000, inclusive.
        ///     <seealso cref="Dex" />
        ///     <seealso cref="StatMod" />
        ///     <seealso cref="OnRawDexChange" />
        ///     <seealso cref="OnRawStatChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int RawDex
        {
            get => m_Dex;
            set
            {
                value = Math.Clamp(value, 1, 65000);

                if (m_Dex != value)
                {
                    var oldValue = m_Dex;

                    m_Dex = value;
                    Delta(MobileDelta.Stat | MobileDelta.Stam);

                    if (Stam < StamMax)
                    {
                        m_StamTimer ??= new StamTimer(this);
                        m_StamTimer.Start();
                    }
                    else if (Stam > StamMax)
                    {
                        Stam = StamMax;
                    }

                    OnRawDexChange(oldValue);
                    OnRawStatChange(StatType.Dex, oldValue);
                }
            }
        }

        /// <summary>
        ///     Gets or sets the effective dexterity of the Mobile. This is the sum of the <see cref="RawDex" /> plus any additional
        ///     modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change.
        ///     It ranges from 1 to 65000, inclusive.
        ///     <seealso cref="RawDex" />
        ///     <seealso cref="StatMod" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Dex
        {
            get => Math.Clamp(m_Dex + GetStatOffset(StatType.Dex), 0, 65000);
            set
            {
                if (StatMods.Count == 0)
                {
                    RawDex = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the base, unmodified, intelligence of the Mobile. Ranges from 1 to 65000, inclusive.
        ///     <seealso cref="Int" />
        ///     <seealso cref="StatMod" />
        ///     <seealso cref="OnRawIntChange" />
        ///     <seealso cref="OnRawStatChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int RawInt
        {
            get => m_Int;
            set
            {
                value = Math.Clamp(value, 1, 65000);

                if (m_Int != value)
                {
                    var oldValue = m_Int;

                    m_Int = value;
                    Delta(MobileDelta.Stat | MobileDelta.Mana);

                    if (Mana < ManaMax)
                    {
                        m_ManaTimer ??= new ManaTimer(this);
                        m_ManaTimer.Start();
                    }
                    else if (Mana > ManaMax)
                    {
                        Mana = ManaMax;
                    }

                    OnRawIntChange(oldValue);
                    OnRawStatChange(StatType.Int, oldValue);
                }
            }
        }

        /// <summary>
        ///     Gets or sets the effective intelligence of the Mobile. This is the sum of the <see cref="RawInt" /> plus any additional
        ///     modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change.
        ///     It ranges from 1 to 65000, inclusive.
        ///     <seealso cref="RawInt" />
        ///     <seealso cref="StatMod" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Int
        {
            get => Math.Clamp(m_Int + GetStatOffset(StatType.Int), 0, 65000);
            set
            {
                if (StatMods.Count == 0)
                {
                    RawInt = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the current hit point of the Mobile. This value ranges from 0 to <see cref="HitsMax" />, inclusive. When
        ///     set
        ///     to the value of <see cref="HitsMax" />, the <see cref="AggressorInfo.CanReportMurder">CanReportMurder</see> flag of all
        ///     aggressors is reset to false, and the list of damage entries is cleared.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Hits
        {
            get => m_Hits;
            set
            {
                if (Deleted)
                {
                    return;
                }

                value = Math.Clamp(value, 0, HitsMax);

                if (value == HitsMax)
                {
                    m_HitsTimer?.Stop();

                    for (var i = 0; i < Aggressors.Count; i++) // reset reports on full HP
                    {
                        Aggressors[i].CanReportMurder = false;
                    }

                    if (DamageEntries.Count > 0)
                    {
                        DamageEntries.Clear(); // reset damage entries on full HP
                    }
                }
                else if (CanRegenHits)
                {
                    m_HitsTimer ??= new HitsTimer(this);
                    m_HitsTimer.Start();
                }
                else
                {
                    m_HitsTimer?.Stop();
                }

                if (m_Hits != value)
                {
                    var oldValue = m_Hits;
                    m_Hits = value;
                    Delta(MobileDelta.Hits);
                    OnHitsChange(oldValue);
                }
            }
        }

        /// <summary>
        ///     Overridable. Gets the maximum hit point of the Mobile. By default, this returns: <c>50 + (<see cref="Str" /> / 2)</c>
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int HitsMax => 50 + Str / 2;

        /// <summary>
        ///     Gets or sets the current stamina of the Mobile. This value ranges from 0 to <see cref="StamMax" />, inclusive.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Stam
        {
            get => m_Stam;
            set
            {
                if (Deleted)
                {
                    return;
                }

                value = Math.Clamp(value, 0, StamMax);

                if (CanRegenStam && value < StamMax)
                {
                    m_StamTimer ??= new StamTimer(this);
                    m_StamTimer.Start();
                }
                else
                {
                    m_StamTimer?.Stop();
                }

                if (m_Stam != value)
                {
                    var oldValue = m_Stam;
                    m_Stam = value;
                    Delta(MobileDelta.Stam);
                    OnStamChange(oldValue);
                }
            }
        }

        /// <summary>
        ///     Overridable. Gets the maximum stamina of the Mobile. By default, this returns:
        ///     <c>
        ///         <see cref="Dex" />
        ///     </c>
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int StamMax => Dex;

        /// <summary>
        ///     Gets or sets the current stamina of the Mobile. This value ranges from 0 to <see cref="ManaMax" />, inclusive.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Mana
        {
            get => m_Mana;
            set
            {
                if (Deleted)
                {
                    return;
                }

                value = Math.Clamp(value, 0, ManaMax);

                if (value == ManaMax)
                {
                    m_ManaTimer?.Stop();

                    if (Meditating)
                    {
                        Meditating = false;
                        SendLocalizedMessage(501846); // You are at peace.
                    }
                }
                else if (CanRegenMana)
                {
                    m_ManaTimer ??= new ManaTimer(this);
                    m_ManaTimer.Start();
                }
                else
                {
                    m_ManaTimer?.Stop();
                }

                if (m_Mana != value)
                {
                    var oldValue = m_Mana;
                    m_Mana = value;
                    Delta(MobileDelta.Mana);
                    OnManaChange(oldValue);
                }
            }
        }

        /// <summary>
        ///     Overridable. Gets the maximum mana of the Mobile. By default, this returns:
        ///     <c>
        ///         <see cref="Int" />
        ///     </c>
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int ManaMax => Int;

        public Timer PoisonTimer { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get => m_Poison;
            set
            {
                m_Poison = value;
                Delta(MobileDelta.HealthbarPoison);

                if (PoisonTimer != null)
                {
                    PoisonTimer.Stop();
                    PoisonTimer = null;
                }

                if (m_Poison != null)
                {
                    PoisonTimer = m_Poison.ConstructTimer(this);

                    PoisonTimer?.Start();
                }

                CheckStatTimers();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HairItemID
        {
            get => m_Hair?.ItemID ?? 0;
            set
            {
                if (m_Hair == null && value > 0)
                {
                    m_Hair = new HairInfo(value);
                }
                else if (value <= 0)
                {
                    m_Hair = null;
                }
                else if (m_Hair != null)
                {
                    m_Hair.ItemID = value;
                }

                Delta(MobileDelta.Hair);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FacialHairItemID
        {
            get => m_FacialHair?.ItemID ?? 0;
            set
            {
                if (m_FacialHair == null && value > 0)
                {
                    m_FacialHair = new FacialHairInfo(value);
                }
                else if (value <= 0)
                {
                    m_FacialHair = null;
                }
                else if (m_FacialHair != null)
                {
                    m_FacialHair.ItemID = value;
                }

                Delta(MobileDelta.FacialHair);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HairHue
        {
            get => m_Hair?.Hue ?? 0;
            set
            {
                if (m_Hair != null)
                {
                    m_Hair.Hue = value;
                    Delta(MobileDelta.Hair);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FacialHairHue
        {
            get => m_FacialHair?.Hue ?? 0;
            set
            {
                if (m_FacialHair != null)
                {
                    m_FacialHair.Hue = value;
                    Delta(MobileDelta.FacialHair);
                }
            }
        }

        public Item ShieldArmor => FindItemOnLayer(Layer.TwoHanded);

        public Item NeckArmor => FindItemOnLayer(Layer.Neck);

        public Item HandArmor => FindItemOnLayer(Layer.Gloves);

        public Item HeadArmor => FindItemOnLayer(Layer.Helm);

        public Item ArmsArmor => FindItemOnLayer(Layer.Arms);

        public Item LegsArmor => FindItemOnLayer(Layer.InnerLegs) ?? FindItemOnLayer(Layer.Pants);

        public Item ChestArmor => FindItemOnLayer(Layer.InnerTorso) ?? FindItemOnLayer(Layer.Shirt);

        public Item Talisman => FindItemOnLayer(Layer.Talisman);

        public int CompareTo(Mobile other) => other == null ? -1 : Serial.CompareTo(other.Serial);

        public virtual int HuedItemID => m_Female ? 0x2107 : 0x2106;
        public ObjectPropertyList PropertyList => m_PropertyList ??= InitializePropertyList(new ObjectPropertyList(this));

        public virtual void GetProperties(ObjectPropertyList list)
        {
            AddNameProperties(list);
        }

        [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
        public DateTime Created { get; set; } = Core.Now;

        [CommandProperty(AccessLevel.GameMaster)]
        DateTime ISerializable.LastSerialized { get; set; } = Core.Now;

        long ISerializable.SavePosition { get; set; } = -1;

        BufferWriter ISerializable.SaveBuffer { get; set; }

        [CommandProperty(AccessLevel.Counselor)]
        public Serial Serial { get; }

        public int TypeRef { get; private set; }

        public virtual void Serialize(IGenericWriter writer)
        {
            writer.Write(33); // version

            writer.WriteDeltaTime(LastStrGain);
            writer.WriteDeltaTime(LastIntGain);
            writer.WriteDeltaTime(LastDexGain);

            byte hairflag = 0x00;

            if (m_Hair != null)
            {
                hairflag |= 0x01;
            }

            if (m_FacialHair != null)
            {
                hairflag |= 0x02;
            }

            writer.Write(hairflag);

            if ((hairflag & 0x01) != 0)
            {
                m_Hair?.Serialize(writer);
            }

            if ((hairflag & 0x02) != 0)
            {
                m_FacialHair?.Serialize(writer);
            }

            writer.Write(Race);

            writer.Write(m_TithingPoints);

            writer.Write(Corpse);

            // writer.Write(CreationTime);

            Stabled.Tidy();
            writer.Write(Stabled);

            writer.Write(CantWalk);

            VirtueInfo.Serialize(writer, Virtues);

            writer.Write(Thirst);
            writer.Write(BAC);

            writer.Write(m_ShortTermMurders);
            // writer.Write( m_ShortTermElapse );
            // writer.Write( m_LongTermElapse );

            // writer.Write( m_Followers );
            writer.Write(m_FollowersMax);

            writer.Write(MagicDamageAbsorb);

            writer.Write(GuildFealty);

            writer.Write(m_Guild);

            writer.Write(m_DisplayGuildTitle);

            writer.Write(CanSwim);

            writer.Write(Squelched);

            writer.Write(m_Holding);

            writer.Write(m_VirtualArmor);

            writer.Write(BaseSoundID);

            writer.Write(DisarmReady);
            writer.Write(StunReady);

            // Poison.Serialize( m_Poison, writer );

            writer.Write(m_StatCap);

            writer.Write(NameHue);

            writer.Write(m_Hunger);

            writer.Write(m_Location);
            writer.Write(m_Body);
            writer.Write(m_Name);
            writer.Write(m_GuildTitle);
            writer.Write(m_Criminal);
            writer.Write(m_Kills);
            writer.Write(SpeechHue);
            writer.Write(EmoteHue);
            writer.Write(WhisperHue);
            writer.Write(YellHue);
            writer.Write(m_Language);
            writer.Write(m_Female);
            writer.Write(m_Warmode);
            writer.Write(m_Hidden);
            writer.Write((byte)m_Direction);
            writer.Write(m_Hue);
            writer.Write(m_Str);
            writer.Write(m_Dex);
            writer.Write(m_Int);
            writer.Write(m_Hits);
            writer.Write(m_Stam);
            writer.Write(m_Mana);

            writer.Write(m_Map);

            writer.Write(m_Blessed);
            writer.Write(m_Fame);
            writer.Write(m_Karma);
            writer.Write((byte)m_AccessLevel);
            Skills.Serialize(writer);

            writer.Write(Items);

            writer.Write(m_Player);
            writer.Write(m_Title);
            writer.Write(Profile);
            writer.Write(ProfileLocked);
            writer.Write(AutoPageNotify);

            writer.Write(LogoutLocation);
            writer.Write(LogoutMap);

            writer.Write((byte)m_StrLock);
            writer.Write((byte)m_DexLock);
            writer.Write((byte)m_IntLock);
        }

        public bool Deleted { get; private set; }

        public virtual void Delete()
        {
            if (Deleted)
            {
                return;
            }

            if (m_NetState != null)
            {
                m_NetState.CancelAllTrades();
                m_NetState.Disconnect($"Player {this} has been deleted.");
            }

            DropHolding();

            Region.OnRegionChange(this, m_Region, null);

            m_Region = null;
            // Is the above line REALLY needed?  The old Region system did NOT have said line
            // and worked fine, because of this a LOT of extra checks have to be done everywhere...
            // I guess this should be there for Garbage collection purposes, but, still, is it /really/ needed?

            OnDelete();

            for (var i = Items.Count - 1; i >= 0; --i)
            {
                if (i < Items.Count)
                {
                    Items[i].OnParentDeleted(this);
                }
            }

            for (var i = 0; i < Stabled.Count; i++)
            {
                Stabled[i].Delete();
            }

            SendRemovePacket();

            m_Guild?.OnDelete(this);

            Deleted = true;

            m_Map?.OnLeave(this);
            m_Map = null;

            m_Hair = null;
            m_FacialHair = null;
            m_MountItem = null;

            World.RemoveEntity(this);

            OnAfterDelete();

            m_PropertyList = null;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Map Map
        {
            get => m_Map;
            set
            {
                if (Deleted)
                {
                    return;
                }

                if (m_Map != value)
                {
                    m_NetState?.ValidateAllTrades();

                    var oldMap = m_Map;

                    if (m_Map != null)
                    {
                        m_Map.OnLeave(this);

                        ClearScreen();
                        SendRemovePacket();
                    }

                    for (var i = 0; i < Items.Count; ++i)
                    {
                        Items[i].Map = value;
                    }

                    m_Map = value;
                    InternalMapChange(oldMap);
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Point3D Location
        {
            get => m_Location;
            set => SetLocation(value, true);
        }

        private void InternalMapChange(Map oldMap)
        {
            var ns = m_NetState;
            m_Map?.OnEnter(this);
            UpdateRegion();

            if (ns != null)
            {
                ns.Sequence = 0;

                if (m_Map != null)
                {
                    ns.SendMapChange(Map);

                    ns.SendMapPatches();

                    ns.SendSeasonChange((byte)GetSeason(), true);

                    ns.SendMobileUpdate(this);
                    ns.SendServerChange(m_Location, m_Map);
                }

                ns.SendMobileIncoming(this, this);

                ns.SendMobileUpdate(this);
                CheckLightLevels(true);
                ns.SendMobileUpdate(this);
            }

            SendEverything();
            SendIncomingPacket();

            ns.SendMobileIncoming(this, this);
            ns.SendSupportedFeature();
            ns.SendMobileUpdate(this);
            ns.SendMobileAttributes(this);

            OnMapChange(oldMap);
        }

        public virtual void MoveToWorld(Point3D newLocation, Map map)
        {
            if (Deleted)
            {
                return;
            }

            if (m_Map == map)
            {
                SetLocation(newLocation, true);
                return;
            }

            var box = FindBankNoCreate();

            if (box?.Opened == true)
            {
                box.Close();
            }

            var oldLocation = m_Location;
            var oldMap = m_Map;

            if (oldMap != null)
            {
                oldMap.OnLeave(this);

                ClearScreen();
                SendRemovePacket();
            }

            for (var i = 0; i < Items.Count; ++i)
            {
                Items[i].Map = map;
            }

            m_Map = map;

            m_Location = newLocation;
            InternalMapChange(oldMap);
            OnLocationChange(oldLocation);

            m_Region?.OnLocationChanged(this, oldLocation);
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int X
        {
            get => m_Location.m_X;
            set => Location = new Point3D(value, m_Location.m_Y, m_Location.m_Z);
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Y
        {
            get => m_Location.m_Y;
            set => Location = new Point3D(m_Location.m_X, value, m_Location.m_Z);
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Z
        {
            get => m_Location.m_Z;
            set => Location = new Point3D(m_Location.m_X, m_Location.m_Y, value);
        }

        public virtual void ProcessDelta()
        {
            var delta = m_DeltaFlags;
            if (delta == MobileDelta.None)
            {
                return;
            }

            var attrs = delta & MobileDelta.Attributes;

            m_DeltaFlags = MobileDelta.None;
            m_InDeltaQueue = false;

            bool sendHits = false, sendStam = false, sendMana = false, sendAll = false, sendAny = false;
            bool sendIncoming = false, sendNonlocalIncoming = false;
            bool sendUpdate = false, sendRemove = false;
            bool sendPublicStats = false, sendPrivateStats = false;
            bool sendMoving = false, sendNonlocalMoving = false;
            var sendOPLUpdate = ObjectPropertyList.Enabled && (delta & MobileDelta.Properties) != 0;

            bool sendHair = false, sendFacialHair = false, removeHair = false, removeFacialHair = false;

            bool sendHealthbarPoison = false, sendHealthbarYellow = false;

            if (attrs != MobileDelta.None)
            {
                sendAny = true;

                if (attrs == MobileDelta.Attributes)
                {
                    sendAll = true;
                }
                else
                {
                    sendHits = (attrs & MobileDelta.Hits) != 0;
                    sendStam = (attrs & MobileDelta.Stam) != 0;
                    sendMana = (attrs & MobileDelta.Mana) != 0;
                }
            }

            if ((delta & MobileDelta.GhostUpdate) != 0)
            {
                sendNonlocalIncoming = true;
            }

            if ((delta & MobileDelta.Hue) != 0)
            {
                sendNonlocalIncoming = true;
                sendUpdate = true;
                sendRemove = true;
            }

            if ((delta & MobileDelta.Direction) != 0)
            {
                sendNonlocalMoving = true;
                sendUpdate = true;
            }

            if ((delta & MobileDelta.Body) != 0)
            {
                sendUpdate = true;
                sendIncoming = true;
            }

            if ((delta & (MobileDelta.Flags | MobileDelta.Noto)) != 0)
            {
                sendMoving = true;
            }

            if ((delta & MobileDelta.HealthbarPoison) != 0)
            {
                sendHealthbarPoison = true;
            }

            if ((delta & MobileDelta.HealthbarYellow) != 0)
            {
                sendHealthbarYellow = true;
            }

            if ((delta & MobileDelta.Name) != 0)
            {
                sendAll = false;
                sendHits = false;
                sendAny = sendStam || sendMana;
                sendPublicStats = true;
            }

            if ((delta & (MobileDelta.WeaponDamage | MobileDelta.Resistances | MobileDelta.Stat |
                          MobileDelta.Weight | MobileDelta.Gold | MobileDelta.Armor | MobileDelta.StatCap |
                          MobileDelta.Followers | MobileDelta.TithingPoints | MobileDelta.Race)) != 0)
            {
                sendPrivateStats = true;
            }

            if ((delta & MobileDelta.Hair) != 0)
            {
                if (HairItemID <= 0)
                {
                    removeHair = true;
                }

                sendHair = true;
            }

            if ((delta & MobileDelta.FacialHair) != 0)
            {
                if (FacialHairItemID <= 0)
                {
                    removeFacialHair = true;
                }

                sendFacialHair = true;
            }

            var hairSerial = HairInfo.FakeSerial(Serial);
            var hairLength = removeHair
                ? OutgoingVirtualHairPackets.RemovePacketLength
                : OutgoingVirtualHairPackets.EquipUpdatePacketLength;

            Span<byte> hairPacket = stackalloc byte[hairLength].InitializePacket();

            var facialHairSerial = FacialHairInfo.FakeSerial(Serial);
            var facialHairLength = removeFacialHair
                ? OutgoingVirtualHairPackets.RemovePacketLength
                : OutgoingVirtualHairPackets.EquipUpdatePacketLength;

            Span<byte> facialHairPacket = stackalloc byte[facialHairLength].InitializePacket();

            const int cacheLength = OutgoingMobilePackets.MobileMovingPacketCacheByteLength;
            const int width = OutgoingMobilePackets.MobileMovingPacketLength;

            var mobileMovingCache = stackalloc byte[cacheLength].InitializePackets(width);

            var ourState = m_NetState;

            if (ourState != null)
            {
                if (sendUpdate)
                {
                    ourState.Sequence = 0;
                    ourState.SendMobileUpdate(this);
                }

                if (sendIncoming)
                {
                    ourState.SendMobileIncoming(this, this);
                }

                if (sendMoving || !ourState.StygianAbyss && (sendHealthbarPoison || sendHealthbarYellow))
                {
                    ourState.SendMobileMovingUsingCache(mobileMovingCache, this, this);
                }

                if (ourState.StygianAbyss)
                {
                    if (sendHealthbarPoison)
                    {
                        ourState.SendMobileHealthbar(this, Healthbar.Poison);
                    }

                    if (sendHealthbarYellow)
                    {
                        ourState.SendMobileHealthbar(this, Healthbar.Yellow);
                    }
                }

                if (sendPublicStats || sendPrivateStats)
                {
                    ourState.SendMobileStatus(this);
                }
                else if (sendAll)
                {
                    ourState.SendMobileAttributes(this);
                }
                else if (sendAny)
                {
                    if (sendHits)
                    {
                        ourState.SendMobileHits(this);
                    }

                    if (sendMana)
                    {
                        ourState.SendMobileMana(this);
                    }

                    if (sendStam)
                    {
                        ourState.SendMobileStam(this);
                    }
                }

                if (sendStam || sendMana)
                {
                    if (Party is IParty ip)
                    {
                        if (sendMana)
                        {
                            ip.OnManaChanged(this);
                        }

                        if (sendStam)
                        {
                            ip.OnStamChanged(this);
                        }
                    }
                }

                if (sendHair)
                {
                    if (removeHair)
                    {
                        OutgoingVirtualHairPackets.CreateRemoveHairPacket(hairPacket, hairSerial);
                    }
                    else
                    {
                        OutgoingVirtualHairPackets.CreateHairEquipUpdatePacket(
                            hairPacket,
                            this,
                            hairSerial,
                            HairItemID,
                            HairHue,
                            Layer.Hair
                        );
                    }

                    ourState.Send(hairPacket);
                }

                if (sendFacialHair)
                {
                    if (removeFacialHair)
                    {
                        OutgoingVirtualHairPackets.CreateRemoveHairPacket(facialHairPacket, facialHairSerial);
                    }
                    else
                    {
                        OutgoingVirtualHairPackets.CreateHairEquipUpdatePacket(
                            facialHairPacket,
                            this,
                            facialHairSerial,
                            FacialHairItemID,
                            FacialHairHue,
                            Layer.FacialHair
                        );
                    }
                    ourState.Send(facialHairPacket);
                }

                if (sendOPLUpdate)
                {
                    SendOPLPacketTo(ourState);
                }
            }

            // TODO: Is it even valid to send packets to our state while we have a null map? Look into failing fast
            if (m_Map == null)
            {
                return;
            }

            sendMoving = sendMoving || sendNonlocalMoving;
            sendIncoming = sendIncoming || sendNonlocalIncoming;
            sendHits = sendHits || sendAll;

            if (!(sendRemove || sendIncoming || sendPublicStats || sendHits || sendMoving ||
                  sendOPLUpdate || sendHair || sendFacialHair || sendHealthbarPoison ||
                  sendHealthbarYellow))
            {
                return;
            }

            var eable = Map.GetClientsInRange(m_Location);

            Span<byte> statBufferTrue = stackalloc byte[OutgoingMobilePackets.MobileStatusCompactLength].InitializePacket();
            Span<byte> statBufferFalse = stackalloc byte[OutgoingMobilePackets.MobileStatusCompactLength].InitializePacket();
            Span<byte> hbpBuffer = stackalloc byte[OutgoingMobilePackets.MobileHealthbarPacketLength].InitializePacket();
            Span<byte> hbyBuffer = stackalloc byte[OutgoingMobilePackets.MobileHealthbarPacketLength].InitializePacket();
            Span<byte> deadBuffer = stackalloc byte[OutgoingMobilePackets.BondedStatusPacketLength].InitializePacket();
            Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();
            Span<byte> hitsPacket = stackalloc byte[OutgoingMobilePackets.MobileAttributePacketLength].InitializePacket();

            foreach (var state in eable)
            {
                var beholder = state.Mobile;

                if (beholder == this || !beholder.CanSee(this))
                {
                    continue;
                }

                if (sendRemove)
                {
                    OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                    state.Send(removeEntity);
                }

                if (sendIncoming)
                {
                    state.SendMobileIncoming(beholder, this);

                    if (IsDeadBondedPet)
                    {
                        OutgoingMobilePackets.CreateBondedStatus(deadBuffer, Serial, true);
                        state.Send(deadBuffer);
                    }
                }

                if (sendMoving || !state.StygianAbyss && (sendHealthbarPoison || sendHealthbarYellow))
                {
                    state.SendMobileMovingUsingCache(mobileMovingCache, beholder, this);
                }

                if (state.StygianAbyss)
                {
                    if (sendHealthbarPoison)
                    {
                        OutgoingMobilePackets.CreateMobileHealthbar(hbpBuffer, this, Healthbar.Poison);
                        state.Send(hbpBuffer);
                    }

                    if (sendHealthbarYellow)
                    {
                        OutgoingMobilePackets.CreateMobileHealthbar(hbyBuffer, this, Healthbar.Yellow);
                        state.Send(hbyBuffer);
                    }
                }

                if (sendPublicStats)
                {
                    if (CanBeRenamedBy(beholder))
                    {
                        OutgoingMobilePackets.CreateMobileStatusCompact(statBufferTrue, this, true);
                        state.Send(statBufferTrue);
                    }
                    else
                    {
                        OutgoingMobilePackets.CreateMobileStatusCompact(statBufferFalse, this, false);
                        state.Send(statBufferFalse);
                    }
                }
                else if (sendHits)
                {
                    OutgoingMobilePackets.CreateMobileHits(hitsPacket, this, true);
                    state.Send(hitsPacket);
                }

                if (sendHair)
                {
                    if (removeHair)
                    {
                        OutgoingVirtualHairPackets.CreateRemoveHairPacket(hairPacket, hairSerial);
                    }
                    else
                    {
                        OutgoingVirtualHairPackets.CreateHairEquipUpdatePacket(
                            hairPacket,
                            this,
                            hairSerial,
                            HairItemID,
                            HairHue,
                            Layer.Hair
                        );
                    }

                    state.Send(hairPacket);
                }

                if (sendFacialHair)
                {
                    if (removeFacialHair)
                    {
                        OutgoingVirtualHairPackets.CreateRemoveHairPacket(facialHairPacket, facialHairSerial);
                    }
                    else
                    {
                        OutgoingVirtualHairPackets.CreateHairEquipUpdatePacket(
                            facialHairPacket,
                            this,
                            facialHairSerial,
                            FacialHairItemID,
                            FacialHairHue,
                            Layer.FacialHair
                        );
                    }
                    state.Send(facialHairPacket);
                }

                SendOPLPacketTo(state);
            }

            eable.Free();
        }

        public ISpawner Spawner { get; set; }

        public virtual void OnBeforeSpawn(Point3D location, Map m)
        {
        }

        public virtual void OnAfterSpawn()
        {
        }

        public virtual bool InRange(Point2D p, int range) =>
            p.m_X >= Location.m_X - range
            && p.m_X <= Location.m_X + range
            && p.m_Y >= Location.m_Y - range
            && p.m_Y <= Location.m_Y + range;

        public virtual bool InRange(Point3D p, int range) =>
            p.m_X >= Location.m_X - range
            && p.m_X <= Location.m_X + range
            && p.m_Y >= Location.m_Y - range
            && p.m_Y <= Location.m_Y + range;

        public virtual bool InRange(IPoint2D p, int range) =>
            p.X >= Location.m_X - range
            && p.X <= Location.m_X + range
            && p.Y >= Location.m_Y - range
            && p.Y <= Location.m_Y + range;

        protected virtual void OnRaceChange(Race oldRace)
        {
        }

        public virtual void ComputeLightLevels(out int global, out int personal)
        {
            ComputeBaseLightLevels(out global, out personal);

            m_Region?.AlterLightLevel(this, ref global, ref personal);
        }

        public virtual void ComputeBaseLightLevels(out int global, out int personal)
        {
            global = 0;
            personal = m_LightLevel;
        }

        public virtual void CheckLightLevels(bool forceResend)
        {
        }

        public virtual void UpdateResistances()
        {
            Resistances ??= new[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

            var delta = false;

            for (var i = 0; i < Resistances.Length; ++i)
            {
                if (Resistances[i] != int.MinValue)
                {
                    Resistances[i] = int.MinValue;
                    delta = true;
                }
            }

            if (delta)
            {
                Delta(MobileDelta.Resistances);
            }
        }

        public virtual int GetResistance(ResistanceType type)
        {
            Resistances ??= new[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

            var v = (int)type;

            if (v < 0 || v >= Resistances.Length)
            {
                return 0;
            }

            var res = Resistances[v];

            if (res == int.MinValue)
            {
                ComputeResistances();
                res = Resistances[v];
            }

            return res;
        }

        public virtual void AddResistanceMod(ResistanceMod toAdd)
        {
            ResistanceMods ??= new List<ResistanceMod>();

            ResistanceMods.Add(toAdd);
            UpdateResistances();
        }

        public virtual void RemoveResistanceMod(ResistanceMod toRemove)
        {
            if (ResistanceMods != null)
            {
                ResistanceMods.Remove(toRemove);

                if (ResistanceMods.Count == 0)
                {
                    ResistanceMods = null;
                }
            }

            UpdateResistances();
        }

        public virtual void ComputeResistances()
        {
            Resistances ??= new[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

            for (var i = 0; i < Resistances.Length; ++i)
            {
                Resistances[i] = 0;
            }

            Resistances[0] += BasePhysicalResistance;
            Resistances[1] += BaseFireResistance;
            Resistances[2] += BaseColdResistance;
            Resistances[3] += BasePoisonResistance;
            Resistances[4] += BaseEnergyResistance;

            for (var i = 0; i < ResistanceMods?.Count; ++i)
            {
                var mod = ResistanceMods[i];
                var v = (int)mod.Type;

                if (v >= 0 && v < Resistances.Length)
                {
                    Resistances[v] += mod.Offset;
                }
            }

            for (var i = 0; i < Items.Count; ++i)
            {
                var item = Items[i];

                if (item.CheckPropertyConflict(this))
                {
                    continue;
                }

                Resistances[0] += item.PhysicalResistance;
                Resistances[1] += item.FireResistance;
                Resistances[2] += item.ColdResistance;
                Resistances[3] += item.PoisonResistance;
                Resistances[4] += item.EnergyResistance;
            }

            for (var i = 0; i < Resistances.Length; ++i)
            {
                var min = GetMinResistance((ResistanceType)i);
                var max = GetMaxResistance((ResistanceType)i);

                if (max < min)
                {
                    max = min;
                }

                if (Resistances[i] > max)
                {
                    Resistances[i] = max;
                }
                else if (Resistances[i] < min)
                {
                    Resistances[i] = min;
                }
            }
        }

        public virtual int GetMinResistance(ResistanceType type) => int.MinValue;

        public virtual int GetMaxResistance(ResistanceType type) => m_Player ? MaxPlayerResistance : int.MaxValue;

        public int GetAOSStatus(int index) => AOSStatusHandler?.Invoke(this, index) ?? 0;

        public virtual void SendPropertiesTo(Mobile from)
        {
            from.NetState?.Send(PropertyList.Buffer);
        }

        public virtual void OnAosSingleClick(Mobile from)
        {
            var opl = PropertyList;

            if (opl.Header > 0)
            {
                int hue;

                if (NameHue != -1)
                {
                    hue = NameHue;
                }
                else if (m_AccessLevel > AccessLevel.Player)
                {
                    hue = 11;
                }
                else
                {
                    hue = Notoriety.GetHue(Notoriety.Compute(from, this));
                }

                from.NetState.SendMessageLocalized(Serial, Body, MessageType.Label, hue, 3, opl.Header, Name, opl.HeaderArgs);
            }
        }

        public virtual string ApplyNameSuffix(string suffix) => suffix;

        public virtual void AddNameProperties(ObjectPropertyList list)
        {
            var name = Name ?? "";

            string prefix;

            if (ShowFameTitle && (m_Player || m_Body.IsHuman) && m_Fame >= 10000)
            {
                prefix = m_Female ? "Lady" : "Lord";
            }
            else
            {
                prefix = "";
            }

            var suffix = "";

            if (PropertyTitle && !string.IsNullOrEmpty(Title))
            {
                suffix = Title;
            }

            var guild = m_Guild;

            if (guild != null && (m_Player || m_DisplayGuildTitle))
            {
                suffix = suffix.Length > 0
                    ? $"{suffix} [{Utility.FixHtml(guild.Abbreviation)}]"
                    : $"[{Utility.FixHtml(guild.Abbreviation)}]";
            }

            suffix = ApplyNameSuffix(suffix);

            list.Add(1050045, "{0} \t{1}\t {2}", prefix, name, suffix); // ~1_PREFIX~~2_NAME~~3_SUFFIX~

            if (guild != null && (m_DisplayGuildTitle || m_Player && guild.Type != GuildType.Regular))
            {
                var type = guild.Type >= 0 && (int)guild.Type < m_GuildTypes.Length ? m_GuildTypes[(int)guild.Type] : "";

                var title = GuildTitle?.Trim() ?? "";

                if (title.Length > 0)
                {
                    if (NewGuildDisplay)
                    {
                        list.Add("{0}, {1}", Utility.FixHtml(title), Utility.FixHtml(guild.Name));
                    }
                    else
                    {
                        list.Add("{0}, {1} Guild{2}", Utility.FixHtml(title), Utility.FixHtml(guild.Name), type);
                    }
                }
                else
                {
                    list.Add(Utility.FixHtml(guild.Name));
                }
            }
        }

        public virtual void GetChildProperties(ObjectPropertyList list, Item item)
        {
        }

        public virtual void GetChildNameProperties(ObjectPropertyList list, Item item)
        {
        }

        private void UpdateAggrExpire()
        {
            if (Deleted || Aggressors.Count == 0 && Aggressed.Count == 0)
            {
                StopAggrExpire();
            }
            else if (!_expireAggrTimerToken.Running)
            {
                Timer.StartTimer(ExpireAggressorsDelay, ExpireAggressorsDelay, ExpireAggr, out _expireAggrTimerToken);
            }
        }

        private void ExpireAggr()
        {
            if (Deleted || Aggressors.Count == 0 && Aggressed.Count == 0)
            {
                StopAggrExpire();
            }
            else
            {
                CheckAggrExpire();
            }
        }

        private void StopAggrExpire()
        {
            _expireAggrTimerToken.Cancel();
        }

        private void CheckAggrExpire()
        {
            for (var i = Aggressors.Count - 1; i >= 0; --i)
            {
                if (i >= Aggressors.Count)
                {
                    continue;
                }

                var info = Aggressors[i];

                if (info.Expired)
                {
                    var attacker = info.Attacker;
                    attacker.RemoveAggressed(this);

                    Aggressors.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && CanSee(attacker) && Utility.InUpdateRange(m_Location, attacker.m_Location))
                    {
                        m_NetState.SendMobileIncoming(this, attacker);
                    }
                }
            }

            for (var i = Aggressed.Count - 1; i >= 0; --i)
            {
                if (i >= Aggressed.Count)
                {
                    continue;
                }

                var info = Aggressed[i];

                if (info.Expired)
                {
                    var defender = info.Defender;
                    defender.RemoveAggressor(this);

                    Aggressed.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && CanSee(defender) && Utility.InUpdateRange(m_Location, defender.m_Location))
                    {
                        m_NetState.SendMobileIncoming(this, defender);
                    }
                }
            }

            UpdateAggrExpire();
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <paramref name="skill" /> changes in some way.
        /// </summary>
        public virtual void OnSkillInvalidated(Skill skill)
        {
        }

        public virtual void UpdateSkillMods()
        {
            ValidateSkillMods();

            for (var i = 0; i < SkillMods.Count; ++i)
            {
                var mod = SkillMods[i];
                var sk = Skills[mod.Skill];
                sk?.Update();
            }
        }

        public virtual void ValidateSkillMods()
        {
            for (var i = 0; i < SkillMods.Count;)
            {
                var mod = SkillMods[i];

                if (mod.CheckCondition())
                {
                    ++i;
                }
                else
                {
                    InternalRemoveSkillMod(mod);
                }
            }
        }

        public virtual void AddSkillMod(SkillMod mod)
        {
            if (mod == null)
            {
                return;
            }

            ValidateSkillMods();

            if (!SkillMods.Contains(mod))
            {
                SkillMods.Add(mod);
                mod.Owner = this;

                var sk = Skills[mod.Skill];
                sk?.Update();
            }
        }

        public virtual void RemoveSkillMod(SkillMod mod)
        {
            if (mod == null)
            {
                return;
            }

            ValidateSkillMods();

            InternalRemoveSkillMod(mod);
        }

        private void InternalRemoveSkillMod(SkillMod mod)
        {
            if (SkillMods.Contains(mod))
            {
                SkillMods.Remove(mod);
                mod.Owner = null;

                var sk = Skills[mod.Skill];
                sk?.Update();
            }
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when a client, <paramref name="from" />, invokes a 'help request' for the Mobile.
        ///     Seemingly no longer functional in newer clients.
        /// </summary>
        public virtual void OnHelpRequest(Mobile from)
        {
        }

        public void DelayChangeWarmode(bool value)
        {
            if (m_WarmodeChanges > WarmodeCatchCount)
            {
                _warmodeSpamValue = value;
                m_WarmodeChanges++;
                return;
            }

            if (m_Warmode == value)
            {
                return;
            }

            var now = Core.Now;
            var next = m_NextWarmodeChange;

            if (now > next || m_WarmodeChanges == 0)
            {
                m_WarmodeChanges = 1;
                m_NextWarmodeChange = now + WarmodeSpamCatch;
            }
            else if (m_WarmodeChanges++ == WarmodeCatchCount)
            {
                Timer.StartTimer(WarmodeSpamDelay, WarmodeSpamTimeout);
                return;
            }

            Warmode = value;
        }

        private void WarmodeSpamTimeout()
        {
            Warmode = _warmodeSpamValue;
            m_WarmodeChanges = 0;
        }

        public bool InLOS(Mobile target) =>
            !Deleted && m_Map != null &&
            (target == this || m_AccessLevel > AccessLevel.Player || m_Map.LineOfSight(this, target));

        public bool InLOS(object target) =>
            !Deleted && m_Map != null &&
            (target == this || m_AccessLevel > AccessLevel.Player || target is Item item && item.RootParent == this
             || m_Map.LineOfSight(this, target));

        public bool InLOS(Point3D target) =>
            !Deleted && m_Map != null && (m_AccessLevel > AccessLevel.Player || m_Map.LineOfSight(this, target));

        public bool BeginAction<T>() => BeginAction(typeof(T));

        public bool BeginAction(object toLock)
        {
            if (_actions == null)
            {
                _actions = new List<object> { toLock };
                return true;
            }

            if (!_actions.Contains(toLock))
            {
                _actions.Add(toLock);
                return true;
            }

            return false;
        }

        public bool CanBeginAction<T>() => CanBeginAction(typeof(T));

        public bool CanBeginAction(object toLock) => _actions?.Contains(toLock) != true;

        public void EndAction<T>() => EndAction(typeof(T));

        public void EndAction(object toLock)
        {
            if (_actions != null)
            {
                _actions.Remove(toLock);

                if (_actions.Count == 0)
                {
                    _actions = null;
                }
            }
        }

        public virtual TimeSpan GetLogoutDelay() => Region.GetLogoutDelay(this);

        private void ExpireParalyzed()
        {
            Paralyzed = false;
        }

        public virtual void Paralyze(TimeSpan duration)
        {
            if (!m_Paralyzed)
            {
                Paralyzed = true;
                Timer.StartTimer(duration, ExpireParalyzed, out _paraTimerToken);
            }
        }

        private void ExpireFrozen()
        {
            Frozen = false;
        }

        public virtual void Freeze(TimeSpan duration)
        {
            if (!m_Frozen)
            {
                Frozen = true;
                Timer.StartTimer(duration, ExpireFrozen, out _frozenTimerToken);
            }
        }

        public override string ToString() => $"0x{Serial.Value:X} \"{Name}\"";

        public virtual void SendSkillMessage()
        {
            if (NextActionMessage - Core.TickCount >= 0)
            {
                return;
            }

            NextActionMessage = Core.TickCount + ActionMessageDelay;

            SendLocalizedMessage(500118); // You must wait a few moments to use another skill.
        }

        public virtual void SendActionMessage()
        {
            if (NextActionMessage - Core.TickCount >= 0)
            {
                return;
            }

            NextActionMessage = Core.TickCount + ActionMessageDelay;

            SendLocalizedMessage(500119); // You must wait to perform another action.
        }

        public virtual void ClearHands()
        {
            ClearHand(FindItemOnLayer(Layer.OneHanded));
            ClearHand(FindItemOnLayer(Layer.TwoHanded));
        }

        public virtual void ClearHand(Item item)
        {
            if (item?.Movable == true && !item.AllowEquippedCast(this))
            {
                var pack = Backpack;

                if (pack == null)
                {
                    AddToBackpack(item);
                }
                else
                {
                    pack.DropItem(item);
                }
            }
        }

        public virtual void Attack(Mobile m)
        {
            if (CheckAttack(m))
            {
                Combatant = m;
            }
        }

        public virtual bool CheckAttack(Mobile m) => Utility.InUpdateRange(Location, m.Location) && CanSee(m) && InLOS(m);

        /// <summary>
        ///     Overridable. Virtual event invoked after the <see cref="Combatant" /> property has changed.
        ///     <seealso cref="Combatant" />
        /// </summary>
        public virtual void OnCombatantChange()
        {
        }

        public double GetDistanceToSqrt(Point3D p)
        {
            var xDelta = m_Location.m_X - p.m_X;
            var yDelta = m_Location.m_Y - p.m_Y;

            return Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
        }

        public double GetDistanceToSqrt(Mobile m)
        {
            var xDelta = m_Location.m_X - m.m_Location.m_X;
            var yDelta = m_Location.m_Y - m.m_Location.m_Y;

            return Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
        }

        public double GetDistanceToSqrt(IPoint2D p)
        {
            var xDelta = m_Location.m_X - p.X;
            var yDelta = m_Location.m_Y - p.Y;

            return Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
        }

        public virtual void AggressiveAction(Mobile aggressor) => AggressiveAction(aggressor, false);

        public virtual void AggressiveAction(Mobile aggressor, bool criminal)
        {
            if (aggressor == this)
            {
                return;
            }

            var args = AggressiveActionEventArgs.Create(this, aggressor, criminal);

            EventSink.InvokeAggressiveAction(args);

            args.Free();

            if (Combatant == aggressor)
            {
                _expireCombatantTimerToken.Cancel();
                Timer.StartTimer(ExpireCombatantDelay, ExpireCombatant, out _expireCombatantTimerToken);
            }

            var addAggressor = true;

            var list = Aggressors;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Attacker == aggressor)
                {
                    info.Refresh();
                    info.CriminalAggression = criminal;
                    info.CanReportMurder = criminal;

                    addAggressor = false;
                }
            }

            list = aggressor.Aggressors;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Attacker == this)
                {
                    info.Refresh();

                    addAggressor = false;
                }
            }

            var addAggressed = true;

            list = Aggressed;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Defender == aggressor)
                {
                    info.Refresh();

                    addAggressed = false;
                }
            }

            list = aggressor.Aggressed;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Defender == this)
                {
                    info.Refresh();
                    info.CriminalAggression = criminal;
                    info.CanReportMurder = criminal;

                    addAggressed = false;
                }
            }

            var setCombatant = false;

            if (addAggressor)
            {
                Aggressors.Add(
                    AggressorInfo.Create(
                        aggressor,
                        this,
                        criminal
                    )
                );

                if (CanSee(aggressor))
                {
                    m_NetState.SendMobileIncoming(this, aggressor);
                }

                if (Combatant == null)
                {
                    setCombatant = true;
                }

                UpdateAggrExpire();
            }

            if (addAggressed)
            {
                aggressor.Aggressed.Add(
                    AggressorInfo.Create(
                        aggressor,
                        this,
                        criminal
                    )
                );

                if (CanSee(aggressor))
                {
                    m_NetState.SendMobileIncoming(this, aggressor);
                }

                if (Combatant == null)
                {
                    setCombatant = true;
                }

                UpdateAggrExpire();
            }

            if (setCombatant)
            {
                Combatant = aggressor;
            }

            Region.OnAggressed(aggressor, this, criminal);
        }

        public void RemoveAggressed(Mobile aggressed)
        {
            if (Deleted)
            {
                return;
            }

            var list = Aggressed;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Defender == aggressed)
                {
                    Aggressed.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && CanSee(aggressed))
                    {
                        m_NetState.SendMobileIncoming(this, aggressed);
                    }

                    break;
                }
            }

            UpdateAggrExpire();
        }

        public void RemoveAggressor(Mobile aggressor)
        {
            if (Deleted)
            {
                return;
            }

            var list = Aggressors;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Attacker == aggressor)
                {
                    Aggressors.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && CanSee(aggressor))
                    {
                        m_NetState.SendMobileIncoming(this, aggressor);
                    }

                    break;
                }
            }

            UpdateAggrExpire();
        }

        public virtual int GetTotal(TotalType type) =>
            type switch
            {
                TotalType.Gold   => m_TotalGold,
                TotalType.Items  => m_TotalItems,
                TotalType.Weight => m_TotalWeight,
                _                => 0
            };

        public virtual void UpdateTotal(Item sender, TotalType type, int delta)
        {
            if (delta == 0 || sender.IsVirtualItem)
            {
                return;
            }

            switch (type)
            {
                default:
                    {
                        m_TotalGold += delta;
                        Delta(MobileDelta.Gold);
                        break;
                    }

                case TotalType.Items:
                    {
                        m_TotalItems += delta;
                        break;
                    }

                case TotalType.Weight:
                    {
                        m_TotalWeight += delta;
                        Delta(MobileDelta.Weight);
                        OnWeightChange(m_TotalWeight - delta);
                        break;
                    }
            }
        }

        public virtual void UpdateTotals()
        {
            if (Items == null)
            {
                return;
            }

            var oldWeight = m_TotalWeight;

            m_TotalGold = 0;
            m_TotalItems = 0;
            m_TotalWeight = 0;

            for (var i = 0; i < Items.Count; ++i)
            {
                var item = Items[i];

                item.UpdateTotals();

                if (item.IsVirtualItem)
                {
                    continue;
                }

                m_TotalGold += item.TotalGold;
                m_TotalItems += item.TotalItems + 1;
                m_TotalWeight += item.TotalWeight + item.PileWeight;
            }

            if (m_Holding != null)
            {
                m_TotalWeight += m_Holding.TotalWeight + m_Holding.PileWeight;
            }

            if (m_TotalWeight != oldWeight)
            {
                OnWeightChange(oldWeight);
            }
        }

        public void ClearTarget() => m_Target = null;

        public Target BeginTarget(int range, bool allowGround, TargetFlags flags, TargetCallback callback) =>
            Target = new SimpleTarget(range, flags, allowGround, callback);

        public Target BeginTarget<T>(
            int range, bool allowGround, TargetFlags flags, TargetStateCallback<T> callback,
            T state
        ) =>
            Target = new SimpleStateTarget<T>(range, flags, allowGround, callback, state);

        /// <summary>
        ///     Overridable. Virtual event invoked after the <see cref="Target">Target property</see> has changed.
        /// </summary>
        protected virtual void OnTargetChange()
        {
        }

        public virtual bool CheckContextMenuDisplay(IEntity target) => true;

        private bool InternalOnMove(Direction d)
        {
            if (!OnMove(d))
            {
                return false;
            }

            var e = MovementEventArgs.Create(this, d);

            EventSink.InvokeMovement(e);

            var ret = !e.Blocked;

            e.Free();

            return ret;
        }

        /// <summary>
        ///     Overridable. Event invoked before the Mobile <see cref="Move">moves</see>.
        /// </summary>
        /// <returns>True if the move is allowed, false if not.</returns>
        protected virtual bool OnMove(Direction d)
        {
            if (m_Hidden && m_AccessLevel == AccessLevel.Player)
            {
                if (AllowedStealthSteps-- <= 0 || (d & Direction.Running) != 0 || Mounted)
                {
                    RevealingAction();
                }
            }

            return true;
        }

        public virtual bool CheckMovement(Direction d, out int newZ) => Movement.Movement.CheckMovement(this, d, out newZ);

        private bool CanMove(Direction d, Point3D oldLocation, ref Point3D newLocation)
        {
            if (m_Spell?.OnCasterMoving(d) == false)
            {
                return false;
            }

            if (m_Paralyzed || m_Frozen)
            {
                SendLocalizedMessage(500111); // You are frozen and can not move.

                return false;
            }

            if (!CheckMovement(d, out var newZ))
            {
                return false;
            }

            int x = oldLocation.m_X, y = oldLocation.m_Y;
            int oldX = x, oldY = y;
            var oldZ = oldLocation.m_Z;

            switch (d & Direction.Mask)
            {
                case Direction.North:
                    --y;
                    break;
                case Direction.Right:
                    ++x;
                    --y;
                    break;
                case Direction.East:
                    ++x;
                    break;
                case Direction.Down:
                    ++x;
                    ++y;
                    break;
                case Direction.South:
                    ++y;
                    break;
                case Direction.Left:
                    --x;
                    ++y;
                    break;
                case Direction.West:
                    --x;
                    break;
                case Direction.Up:
                    --x;
                    --y;
                    break;
            }

            newLocation.m_X = x;
            newLocation.m_Y = y;
            newLocation.m_Z = newZ;

            Pushing = false;

            var map = m_Map;

            if (map == null || !Region.CanMove(this, d, newLocation, oldLocation, map))
            {
                return false;
            }

            var oldSector = map.GetSector(oldX, oldY);
            var newSector = map.GetSector(x, y);

            if (oldSector != newSector)
            {
                for (var i = 0; i < oldSector.Mobiles.Count; ++i)
                {
                    var m = oldSector.Mobiles[i];

                    if (m != this && m.X == oldX && m.Y == oldY && m.Z + 15 > oldZ && oldZ + 15 > m.Z &&
                        !m.OnMoveOff(this))
                    {
                        return false;
                    }
                }

                for (var i = 0; i < oldSector.Items.Count; ++i)
                {
                    var item = oldSector.Items[i];

                    if (item.AtWorldPoint(oldX, oldY) &&
                        (item.Z == oldZ || item.Z + item.ItemData.Height > oldZ && oldZ + 15 > item.Z) &&
                        !item.OnMoveOff(this))
                    {
                        return false;
                    }
                }

                for (var i = 0; i < newSector.Mobiles.Count; ++i)
                {
                    var m = newSector.Mobiles[i];

                    if (m.X == x && m.Y == y && m.Z + 15 > newZ && newZ + 15 > m.Z && !m.OnMoveOver(this))
                    {
                        return false;
                    }
                }

                for (var i = 0; i < newSector.Items.Count; ++i)
                {
                    var item = newSector.Items[i];

                    if (item.AtWorldPoint(x, y) &&
                        (item.Z == newZ || item.Z + item.ItemData.Height > newZ && newZ + 15 > item.Z) &&
                        !item.OnMoveOver(this))
                    {
                        return false;
                    }
                }
            }
            else
            {
                for (var i = 0; i < oldSector.Mobiles.Count; ++i)
                {
                    var m = oldSector.Mobiles[i];

                    if (m != this && m.X == oldX && m.Y == oldY && m.Z + 15 > oldZ && oldZ + 15 > m.Z &&
                        !m.OnMoveOff(this))
                    {
                        return false;
                    }

                    if (m.X == x && m.Y == y && m.Z + 15 > newZ && newZ + 15 > m.Z && !m.OnMoveOver(this))
                    {
                        return false;
                    }
                }

                for (var i = 0; i < oldSector.Items.Count; ++i)
                {
                    var item = oldSector.Items[i];

                    if (item.AtWorldPoint(oldX, oldY) &&
                        (item.Z == oldZ || item.Z + item.ItemData.Height > oldZ && oldZ + 15 > item.Z) &&
                        !item.OnMoveOff(this))
                    {
                        return false;
                    }

                    if (item.AtWorldPoint(x, y) &&
                        (item.Z == newZ || item.Z + item.ItemData.Height > newZ && newZ + 15 > item.Z) &&
                        !item.OnMoveOver(this))
                    {
                        return false;
                    }
                }
            }

            if (!InternalOnMove(d))
            {
                return false;
            }

            if (
                CalcMoves.EnableFastwalkPrevention &&
                AccessLevel < CalcMoves.FastwalkExemptionLevel &&
                m_NetState?.AddStep(d) == false
            )
            {
                var fw = new FastWalkEventArgs(m_NetState);
                EventSink.InvokeFastWalk(fw);

                if (fw.Blocked)
                {
                    return false;
                }
            }

            LastMoveTime = Core.TickCount;

            return true;
        }

        public virtual bool Move(Direction d)
        {
            if (Deleted)
            {
                return false;
            }

            var box = FindBankNoCreate();

            if (box?.Opened == true)
            {
                box.Close();
            }

            var oldLocation = m_Location;
            Point3D newLocation = oldLocation;

            if ((m_Direction & Direction.Mask) == (d & Direction.Mask))
            {
                // We are actually moving (not just a direction change)
                if (!CanMove(d, oldLocation, ref newLocation))
                {
                    return false;
                }

                DisruptiveAction();
            }

            m_NetState?.SendMovementAck(m_NetState.Sequence, this);

            SetLocation(newLocation, false);
            SetDirection(d);

            if (m_Map != null)
            {
                var eable = m_Map.GetObjectsInRange(m_Location, Core.GlobalMaxUpdateRange);

                foreach (var o in eable)
                {
                    if (o == this)
                    {
                        continue;
                    }

                    if (o is Mobile mob)
                    {
                        if (mob.NetState != null)
                        {
                            m_MoveClientList.Add(mob);
                        }

                        m_MoveList.Add(mob);
                    }
                    else if (o is Item item && item.HandlesOnMovement)
                    {
                        m_MoveList.Add(item);
                    }
                }

                eable.Free();

                const int cacheLength = OutgoingMobilePackets.MobileMovingPacketCacheByteLength;
                const int width = OutgoingMobilePackets.MobileMovingPacketLength;

                var mobileMovingCache = stackalloc byte[cacheLength].InitializePackets(width);

                foreach (var m in m_MoveClientList)
                {
                    var ns = m.NetState;

                    if (ns != null && Utility.InUpdateRange(m_Location, m.m_Location) && m.CanSee(this))
                    {
                        ns.SendMobileMovingUsingCache(mobileMovingCache, m, this);
                    }
                }

                for (var i = 0; i < m_MoveList.Count; ++i)
                {
                    var o = m_MoveList[i];

                    if (o is Mobile mobile)
                    {
                        mobile.OnMovement(this, oldLocation);
                    }
                    else if (o is Item item)
                    {
                        item.OnMovement(this, oldLocation);
                    }
                }

                if (m_MoveList.Count > 0)
                {
                    m_MoveList.Clear();
                }

                if (m_MoveClientList.Count > 0)
                {
                    m_MoveClientList.Clear();
                }
            }

            OnAfterMove(oldLocation);
            return true;
        }

        public virtual void OnAfterMove(Point3D oldLocation)
        {
        }

        public int ComputeMovementSpeed() => ComputeMovementSpeed(Direction, false);

        public virtual int ComputeMovementSpeed(Direction dir, bool checkTurning = true)
        {
            if (Mounted)
            {
                return (dir & Direction.Running) != 0 ? CalcMoves.RunMountDelay : CalcMoves.WalkMountDelay;
            }

            return (dir & Direction.Running) != 0 ? CalcMoves.RunFootDelay : CalcMoves.WalkFootDelay;
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when a Mobile <paramref name="m" /> moves off this Mobile.
        /// </summary>
        /// <returns>True if the move is allowed, false if not.</returns>
        public virtual bool OnMoveOff(Mobile m) => true;

        /// <summary>
        ///     Overridable. Event invoked when a Mobile <paramref name="m" /> moves over this Mobile.
        /// </summary>
        /// <returns>True if the move is allowed, false if not.</returns>
        public virtual bool OnMoveOver(Mobile m) => m_Map == null || Deleted || m.CheckShove(this);

        public virtual bool CheckShove(Mobile shoved)
        {
            if ((m_Map.Rules & MapRules.FreeMovement) == 0)
            {
                if (!shoved.Alive || !Alive || shoved.IsDeadBondedPet || IsDeadBondedPet)
                {
                    return true;
                }

                if (shoved.m_Hidden && shoved.m_AccessLevel > AccessLevel.Player)
                {
                    return true;
                }

                if (!Pushing)
                {
                    Pushing = true;

                    int number;

                    if (AccessLevel > AccessLevel.Player)
                    {
                        number = shoved.m_Hidden ? 1019041 : 1019040;
                    }
                    else
                    {
                        if (Stam == StamMax)
                        {
                            number = shoved.m_Hidden ? 1019043 : 1019042;
                            Stam -= 10;

                            RevealingAction();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    SendLocalizedMessage(number);
                }
            }

            return true;
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile sees another Mobile, <paramref name="m" />, move.
        /// </summary>
        public virtual void OnMovement(Mobile m, Point3D oldLocation)
        {
        }

        public virtual void CriminalAction(bool message)
        {
            if (Deleted)
            {
                return;
            }

            Criminal = true;

            Region.OnCriminalAction(this, message);
        }

        public virtual bool IsSnoop(Mobile from) => from != this;

        /// <summary>
        ///     Overridable. Any call to <see cref="Resurrect" /> will silently fail if this method returns false.
        ///     <seealso cref="Resurrect" />
        /// </summary>
        public virtual bool CheckResurrect() => true;

        /// <summary>
        ///     Overridable. Event invoked before the Mobile is <see cref="Resurrect">resurrected</see>.
        ///     <seealso cref="Resurrect" />
        /// </summary>
        public virtual void OnBeforeResurrect()
        {
        }

        /// <summary>
        ///     Overridable. Event invoked after the Mobile is <see cref="Resurrect">resurrected</see>.
        ///     <seealso cref="Resurrect" />
        /// </summary>
        public virtual void OnAfterResurrect()
        {
        }

        public virtual void Resurrect()
        {
            if (!Alive)
            {
                if (!Region.OnResurrect(this))
                {
                    return;
                }

                if (!CheckResurrect())
                {
                    return;
                }

                OnBeforeResurrect();

                var box = FindBankNoCreate();

                if (box?.Opened == true)
                {
                    box.Close();
                }

                Poison = null;

                Warmode = false;

                Hits = 10;
                Stam = StamMax;
                Mana = 0;

                BodyMod = 0;
                Body = Race.AliveBody(this);

                ProcessDeltaQueue();

                for (var i = Items.Count - 1; i >= 0; --i)
                {
                    if (i >= Items.Count)
                    {
                        continue;
                    }

                    var item = Items[i];

                    if (item.ItemID == 0x204E)
                    {
                        item.Delete();
                    }
                }

                SendIncomingPacket();
                SendIncomingPacket();

                OnAfterResurrect();
            }
        }

        public void DropHolding()
        {
            var holding = m_Holding;

            if (holding != null)
            {
                if (!holding.Deleted && holding.HeldBy == this && holding.Map == Map.Internal)
                {
                    AddToBackpack(holding);
                }

                Holding = null;
                holding.ClearBounce();
            }
        }

        /// <summary>
        ///     Overridable. Virtual event invoked before the Mobile is deleted.
        /// </summary>
        public virtual void OnDelete()
        {
            Spawner?.Remove(this);
            Spawner = null;
        }

        public virtual bool CheckSpellCast(ISpell spell) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile casts a <paramref name="spell" />.
        /// </summary>
        /// <param name="spell"></param>
        public virtual void OnSpellCast(ISpell spell)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked after <see cref="TotalWeight" /> changes.
        /// </summary>
        public virtual void OnWeightChange(int oldValue)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the <see cref="Skill.Base" /> or <see cref="Skill.BaseFixedPoint" /> property of
        ///     <paramref name="skill" /> changes.
        /// </summary>
        public virtual void OnSkillChange(SkillName skill, double oldBase)
        {
        }

        /// <summary>
        ///     Overridable. Invoked after the mobile is deleted. When overridden, be sure to call the base method.
        /// </summary>
        public virtual void OnAfterDelete()
        {
            StopAggrExpire();

            CheckAggrExpire();

            PoisonTimer?.Stop();
            m_HitsTimer?.Stop();
            m_StamTimer?.Stop();
            m_ManaTimer?.Stop();
            _combatTimerToken.Cancel();
            _expireCombatantTimerToken.Cancel();
            _logoutTimerToken.Cancel();
            _expireCriminalTimerToken.Cancel();
            _paraTimerToken.Cancel();
            _frozenTimerToken.Cancel();
            _autoManifestTimerToken.Cancel();
        }

        public virtual bool AllowSkillUse(SkillName name) => true;

        public virtual bool UseSkill(SkillName name) => Skills.UseSkill(this, name);

        public virtual bool UseSkill(int skillID) => Skills.UseSkill(this, skillID);

        public virtual DeathMoveResult GetParentMoveResultFor(Item item) => item.OnParentDeath(this);

        public virtual DeathMoveResult GetInventoryMoveResultFor(Item item) => item.OnInventoryDeath(this);

        public virtual void Kill()
        {
            if (!CanBeDamaged())
            {
                return;
            }

            if (!Alive || IsDeadBondedPet)
            {
                return;
            }

            if (Deleted)
            {
                return;
            }

            if (!Region.OnBeforeDeath(this))
            {
                return;
            }

            if (!OnBeforeDeath())
            {
                return;
            }

            var box = FindBankNoCreate();

            if (box?.Opened == true)
            {
                box.Close();
            }

            m_NetState?.CancelAllTrades();

            m_Spell?.OnCasterKilled();
            // m_Spell.Disturb( DisturbType.Kill );

            m_Target?.Cancel(this, TargetCancelType.Canceled);

            DisruptiveAction();

            Warmode = false;

            DropHolding();

            Hits = 0;
            Stam = 0;
            Mana = 0;

            Poison = null;
            Combatant = null;

            if (Paralyzed)
            {
                Paralyzed = false;
            }

            if (Frozen)
            {
                Frozen = false;
            }

            var content = new List<Item>();
            var equip = new List<Item>();
            var moveToPack = new List<Item>();

            var itemsCopy = new List<Item>(Items);

            var pack = Backpack;

            for (var i = 0; i < itemsCopy.Count; ++i)
            {
                var item = itemsCopy[i];

                if (item == pack)
                {
                    continue;
                }

                var res = GetParentMoveResultFor(item);

                switch (res)
                {
                    case DeathMoveResult.MoveToCorpse:
                        {
                            content.Add(item);
                            equip.Add(item);
                            break;
                        }
                    case DeathMoveResult.MoveToBackpack:
                        {
                            moveToPack.Add(item);
                            break;
                        }
                }
            }

            if (pack != null)
            {
                var packCopy = new List<Item>(pack.Items);

                for (var i = 0; i < packCopy.Count; ++i)
                {
                    var item = packCopy[i];

                    var res = GetInventoryMoveResultFor(item);

                    if (res == DeathMoveResult.MoveToCorpse)
                    {
                        content.Add(item);
                    }
                    else
                    {
                        moveToPack.Add(item);
                    }
                }

                for (var i = 0; i < moveToPack.Count; ++i)
                {
                    var item = moveToPack[i];

                    if (RetainPackLocsOnDeath && item.Parent == pack)
                    {
                        continue;
                    }

                    pack.DropItem(item);
                }
            }

            HairInfo hair = null;
            if (m_Hair != null)
            {
                hair = new HairInfo(m_Hair.ItemID, m_Hair.Hue);
            }

            FacialHairInfo facialhair = null;
            if (m_FacialHair != null)
            {
                facialhair = new FacialHairInfo(m_FacialHair.ItemID, m_FacialHair.Hue);
            }

            var c = CreateCorpseHandler?.Invoke(this, hair, facialhair, content, equip);

            if (m_Map != null)
            {
                var eable = m_Map.GetClientsInRange(m_Location);
                var corpseSerial = c?.Serial ?? Serial.Zero;

                Span<byte> deathAnimation = stackalloc byte[OutgoingMobilePackets.DeathAnimationPacketLength].InitializePacket();
                Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

                foreach (var state in eable)
                {
                    if (state != m_NetState)
                    {
                        OutgoingMobilePackets.CreateDeathAnimation(deathAnimation, Serial, corpseSerial);
                        state.Send(deathAnimation);

                        if (!state.Mobile.CanSee(this))
                        {
                            OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                            state.Send(removeEntity);
                        }
                    }
                }

                eable.Free();
            }

            Region.OnDeath(this);
            OnDeath(c);
        }

        /// <summary>
        ///     Overridable. Event invoked before the Mobile is <see cref="Kill">killed</see>.
        ///     <seealso cref="Kill" />
        ///     <seealso cref="OnDeath" />
        /// </summary>
        /// <returns>True to continue with death, false to override it.</returns>
        public virtual bool OnBeforeDeath() => true;

        /// <summary>
        ///     Overridable. Event invoked after the Mobile is <see cref="Kill">killed</see>. Primarily, this method is responsible for
        ///     deleting an NPC or turning a PC into a ghost.
        ///     <seealso cref="Kill" />
        ///     <seealso cref="OnBeforeDeath" />
        /// </summary>
        public virtual void OnDeath(Container c)
        {
            var sound = GetDeathSound();

            if (sound >= 0)
            {
                Effects.PlaySound(this, sound);
            }

            if (!m_Player)
            {
                Delete();
            }
            else
            {
                m_NetState.SendDeathStatus(true);

                Warmode = false;

                BodyMod = 0;
                // Body = this.Female ? 0x193 : 0x192;
                Body = Race.GhostBody(this);

                var deathShroud = new Item(0x204E) { Movable = false, Layer = Layer.OuterTorso };

                AddItem(deathShroud);

                Items.Remove(deathShroud);
                Items.Insert(0, deathShroud);

                Poison = null;
                Combatant = null;

                Hits = 0;
                Stam = 0;
                Mana = 0;

                EventSink.InvokePlayerDeath(this);

                ProcessDeltaQueue();

                m_NetState.SendDeathStatus(false);

                CheckStatTimers();
            }
        }

        public virtual bool CheckTarget(Mobile from, Target targ, object targeted) => true;

        public virtual void Use(Item item)
        {
            if (item?.Deleted != false || item.QuestItem || Deleted)
            {
                return;
            }

            DisruptiveAction();

            if (m_Spell?.OnCasterUsingObject(item) == false)
            {
                return;
            }

            var root = item.RootParent;
            var okay = false;

            if (!Utility.InUpdateRange(Location, item.GetWorldLocation()))
            {
                item.OnDoubleClickOutOfRange(this);
            }
            else if (!CanSee(item))
            {
                item.OnDoubleClickCantSee(this);
            }
            else if (!item.IsAccessibleTo(this))
            {
                var reg = Region.Find(item.GetWorldLocation(), item.Map);

                if (reg?.SendInaccessibleMessage(item, this) != true)
                {
                    item.OnDoubleClickNotAccessible(this);
                }
            }
            else if (!CheckAlive(false))
            {
                item.OnDoubleClickDead(this);
            }
            else if (item.InSecureTrade)
            {
                item.OnDoubleClickSecureTrade(this);
            }
            else if (!AllowItemUse(item))
            {
            }
            else if (!item.CheckItemUse(this, item))
            {
            }
            else if (root is Mobile mobile && mobile.IsSnoop(this))
            {
                item.OnSnoop(this);
            }
            else if (Region.OnDoubleClick(this, item))
            {
                okay = true;
            }

            if (okay)
            {
                // TODO: Is this correct?
                if (!item.Deleted)
                {
                    item.OnItemUsed(this, item);
                }

                // TODO: Is this correct?
                if (!item.Deleted)
                {
                    item.OnDoubleClick(this);
                }
            }
        }

        public virtual void Use(Mobile m)
        {
            if (m?.Deleted != false || Deleted)
            {
                return;
            }

            DisruptiveAction();

            if (m_Spell?.OnCasterUsingObject(m) == false)
            {
                return;
            }

            if (!Utility.InUpdateRange(Location, m.Location))
            {
                m.OnDoubleClickOutOfRange(this);
            }
            else if (!CanSee(m))
            {
                m.OnDoubleClickCantSee(this);
            }
            else if (!CheckAlive(false))
            {
                m.OnDoubleClickDead(this);
            }
            else if (Region.OnDoubleClick(this, m) && !m.Deleted)
            {
                m.OnDoubleClick(this);
            }
        }

        public virtual void Lift(Item item, int amount, out bool rejected, out LRReason reject)
        {
            rejected = true;
            reject = LRReason.Inspecific;

            if (item == null)
            {
                return;
            }

            var from = this;
            var state = m_NetState;

            if (from.AccessLevel >= AccessLevel.GameMaster || Core.TickCount - from.NextActionTime >= 0)
            {
                if (from.CheckAlive())
                {
                    from.DisruptiveAction();

                    if (from.Holding != null)
                    {
                        reject = LRReason.AreHolding;
                    }
                    else if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(item.GetWorldLocation(), 2))
                    {
                        reject = LRReason.OutOfRange;
                    }
                    else if (!from.CanSee(item) || !from.InLOS(item))
                    {
                        reject = LRReason.OutOfSight;
                    }
                    else if (!item.VerifyMove(from))
                    {
                        reject = LRReason.CannotLift;
                    }
                    else if (!item.IsAccessibleTo(from))
                    {
                        reject = LRReason.CannotLift;
                    }
                    else if (item.Nontransferable && amount != item.Amount)
                    {
                        if (item.QuestItem)
                        {
                            from.SendLocalizedMessage(1074868); // Stacks of quest items cannot be unstacked.
                        }

                        reject = LRReason.CannotLift;
                    }
                    else if (!item.CheckLift(from, item, ref reject))
                    {
                    }
                    else
                    {
                        var root = item.RootParent;

                        if (root is Mobile mobile && !mobile.CheckNonlocalLift(from, item))
                        {
                            reject = LRReason.TryToSteal;
                        }
                        else if (!from.OnDragLift(item) || !item.OnDragLift(from))
                        {
                            reject = LRReason.Inspecific;
                        }
                        else if (!from.CheckAlive())
                        {
                            reject = LRReason.Inspecific;
                        }
                        else
                        {
                            item.SetLastMoved();

                            if (item.Spawner != null)
                            {
                                item.Spawner.Remove(item);
                                item.Spawner = null;
                            }

                            var oldAmount = item.Amount;

                            if (oldAmount <= 0)
                            {
                                logger.Error($"Item {item.GetType()} ({item.Serial}) has amount of {oldAmount}, but must be at least 1");
                            }
                            else
                            {
                                amount = Math.Clamp(amount, 1, oldAmount);

                                if (amount < oldAmount)
                                {
                                    LiftItemDupe(item, amount);
                                }
                            }

                            var map = from.Map;

                            if (DragEffects && map != null && root is null or Item)
                            {
                                var eable = map.GetClientsInRange(from.Location);
                                var rootItem = root as Item;

                                Span<byte> buffer = stackalloc byte[OutgoingPlayerPackets.DragEffectPacketLength].InitializePacket();

                                foreach (var ns in eable)
                                {
                                    if (ns.Mobile != from && ns.Mobile.CanSee(from) && ns.Mobile.InLOS(from) &&
                                        ns.Mobile.CanSee(root))
                                    {
                                        OutgoingPlayerPackets.CreateDragEffect(
                                            buffer,
                                            rootItem?.Serial ?? Serial.Zero,
                                            rootItem?.Location ?? item.Location,
                                            from.Serial,
                                            from.Location,
                                            item.ItemID,
                                            item.Hue,
                                            amount
                                        );

                                        ns.Send(buffer);
                                    }
                                }

                                eable.Free();
                            }

                            var fixLoc = item.Location;
                            var fixMap = item.Map;
                            var shouldFix = item.Parent == null;

                            item.RecordBounce();
                            item.OnItemLifted(from, item);
                            item.Internalize();

                            from.Holding = item;

                            from.SendSound(item.GetLiftSound(from));

                            from.NextActionTime = Core.TickCount + ActionDelay;

                            if (fixMap != null && shouldFix)
                            {
                                fixMap.FixColumn(fixLoc.m_X, fixLoc.m_Y);
                            }

                            reject = LRReason.Inspecific;
                            rejected = false;
                        }
                    }
                }
                else
                {
                    reject = LRReason.Inspecific;
                }
            }
            else
            {
                SendActionMessage();
                reject = LRReason.Inspecific;
            }

            if (rejected && state != null)
            {
                state.SendLiftReject(reject);

                if (item.Deleted)
                {
                    return;
                }

                if (item.Parent is Item)
                {
                    state.SendContainerContentUpdate(item);
                }
                else if (item.Parent is Mobile)
                {
                    state.SendEquipUpdate(item);
                }
                else
                {
                    item.SendInfoTo(state);
                }

                if (item.Parent != null)
                {
                    item.SendOPLPacketTo(state);
                }
            }
        }

        public static Item LiftItemDupe(Item oldItem, int amount)
        {
            Item item;
            try
            {
                item = oldItem.GetType().CreateInstance<Item>();
            }
            catch
            {
                Console.WriteLine(
                    "Warning: 0x{0:X}: Item must have a zero parameter constructor to be separated from a stack. '{1}'.",
                    oldItem.Serial.Value,
                    oldItem.GetType().Name
                );
                return null;
            }

            item.Visible = oldItem.Visible;
            item.Movable = oldItem.Movable;
            item.LootType = oldItem.LootType;
            item.Direction = oldItem.Direction;
            item.Hue = oldItem.Hue;
            item.ItemID = oldItem.ItemID;
            item.Location = oldItem.Location;
            item.Layer = oldItem.Layer;
            item.Name = oldItem.Name;
            item.Weight = oldItem.Weight;

            item.Amount = oldItem.Amount - amount;
            item.Map = oldItem.Map;

            oldItem.Amount = amount;
            oldItem.OnAfterDuped(item);

            if (oldItem.Parent is Mobile parentMobile)
            {
                parentMobile.AddItem(item);
            }
            else if (oldItem.Parent is Item parentItem)
            {
                parentItem.AddItem(item);
            }

            item.Delta(ItemDelta.Update);

            return item;
        }

        public virtual void SendDropEffect(Item item)
        {
            if (!DragEffects || item.Deleted)
            {
                return;
            }

            var map = m_Map;
            var root = item.RootParent;
            var rootItem = root as Item;

            if (map == null || root != null && rootItem == null)
            {
                return;
            }

            var eable = map.GetClientsInRange(m_Location);

            Span<byte> buffer = stackalloc byte[OutgoingPlayerPackets.DragEffectPacketLength].InitializePacket();

            foreach (var ns in eable)
            {
                if (ns.StygianAbyss || ns.Mobile == this ||
                    !ns.Mobile.CanSee(this) || !ns.Mobile.InLOS(this) || !ns.Mobile.CanSee(root))
                {
                    continue;
                }

                OutgoingPlayerPackets.CreateDragEffect(
                    buffer,
                    Serial,
                    Location,
                    rootItem?.Serial ?? Serial.Zero,
                    rootItem?.Location ?? item.Location,
                    item.ItemID,
                    item.Hue,
                    item.Amount
                );

                ns.Send(buffer);
            }

            eable.Free();
        }

        public virtual bool Drop(Item to, Point3D loc)
        {
            var from = this;
            var item = from.Holding;

            var valid = item != null && item.HeldBy == from && item.Map == Map.Internal;

            from.Holding = null;

            if (!valid)
            {
                return false;
            }

            var bounced = true;

            item.SetLastMoved();

            if (to == null || !item.DropToItem(from, to, loc))
            {
                item.Bounce(from);
            }
            else
            {
                bounced = false;
            }

            item.ClearBounce();

            if (!bounced)
            {
                SendDropEffect(item);
            }

            return !bounced;
        }

        public virtual bool Drop(Point3D loc)
        {
            var from = this;
            var item = from.Holding;

            var valid = item != null && item.HeldBy == from && item.Map == Map.Internal;

            from.Holding = null;

            if (!valid)
            {
                return false;
            }

            var bounced = true;

            item.SetLastMoved();

            if (!item.DropToWorld(from, loc))
            {
                item.Bounce(from);
            }
            else
            {
                bounced = false;
            }

            item.ClearBounce();

            if (!bounced)
            {
                SendDropEffect(item);
            }

            return !bounced;
        }

        public virtual bool Drop(Mobile to, Point3D loc)
        {
            var from = this;
            var item = from.Holding;

            var valid = item != null && item.HeldBy == from && item.Map == Map.Internal;

            from.Holding = null;

            if (!valid)
            {
                return false;
            }

            var bounced = true;

            item.SetLastMoved();

            if (to == null || !item.DropToMobile(from, to, loc))
            {
                item.Bounce(from);
            }
            else
            {
                bounced = false;
            }

            item.ClearBounce();

            if (!bounced)
            {
                SendDropEffect(item);
            }

            return !bounced;
        }

        public virtual bool MutateSpeech(List<Mobile> hears, ref string text, ref object context)
        {
            if (Alive)
            {
                return false;
            }

            using var sb = new ValueStringBuilder(stackalloc char[Math.Min(text.Length, 256)]);
            for (var i = 0; i < text.Length; ++i)
            {
                sb.Append(text[i] != ' ' ? (GhostChars ?? DefaultGhostChars).RandomElement() : ' ');
            }

            text = sb.ToString();
            context = m_GhostMutateContext;
            return true;
        }

        private void AutoManifest()
        {
            if (!Alive)
            {
                Warmode = false;
            }
        }

        public virtual void Manifest(TimeSpan delay)
        {
            Warmode = true;
            _autoManifestTimerToken.Cancel();
            Timer.StartTimer(delay, AutoManifest, out _autoManifestTimerToken);
        }

        public virtual bool CheckSpeechManifest()
        {
            if (Alive)
            {
                return false;
            }

            var delay = AutoManifestTimeout;

            if (delay > TimeSpan.Zero && (!Warmode || _autoManifestTimerToken.Running))
            {
                Manifest(delay);
                return true;
            }

            return false;
        }

        public virtual bool CheckHearsMutatedSpeech(Mobile m, object context) =>
            context != m_GhostMutateContext || m.Alive && !m.CanHearGhosts;

        private void AddSpeechItemsFrom(List<IEntity> list, Container cont)
        {
            for (var i = 0; i < cont.Items.Count; ++i)
            {
                var item = cont.Items[i];

                if (item.HandlesOnSpeech)
                {
                    list.Add(item);
                }

                if (item is Container container)
                {
                    AddSpeechItemsFrom(list, container);
                }
            }
        }

        public virtual void DoSpeech(string text, int[] keywords, MessageType type, int hue)
        {
            if (Deleted || CommandSystem.Handle(this, text, type))
            {
                return;
            }

            var range = 15;

            switch (type)
            {
                case MessageType.Regular:
                    SpeechHue = hue;
                    break;
                case MessageType.Emote:
                    EmoteHue = hue;
                    break;
                case MessageType.Whisper:
                    WhisperHue = hue;
                    range = 1;
                    break;
                case MessageType.Yell:
                    YellHue = hue;
                    range = 18;
                    break;
                case MessageType.System:
                    break;
                case MessageType.Label:
                    break;
                case MessageType.Focus:
                    break;
                case MessageType.Spell:
                    break;
                case MessageType.Guild:
                    break;
                case MessageType.Alliance:
                    break;
                case MessageType.Command:
                    break;
                case MessageType.Encoded:
                    break;
                default:
                    type = MessageType.Regular;
                    break;
            }

            var regArgs = new SpeechEventArgs(this, text, type, hue, keywords);

            EventSink.InvokeSpeech(regArgs);
            Region.OnSpeech(regArgs);
            OnSaid(regArgs);

            if (regArgs.Blocked)
            {
                return;
            }

            text = regArgs.Speech;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var hears = m_Hears;
            var onSpeech = m_OnSpeech;

            if (m_Map != null)
            {
                var eable = m_Map.GetObjectsInRange(m_Location, range);

                foreach (var o in eable)
                {
                    if (o is Mobile heard)
                    {
                        if (!heard.CanSee(this) || !NoSpeechLOS && heard.Player && !heard.InLOS(this))
                        {
                            continue;
                        }

                        if (heard.m_NetState != null)
                        {
                            hears.Add(heard);
                        }

                        if (heard.HandlesOnSpeech(this))
                        {
                            onSpeech.Add(heard);
                        }

                        for (var i = 0; i < heard.Items.Count; ++i)
                        {
                            var item = heard.Items[i];

                            if (item.HandlesOnSpeech)
                            {
                                onSpeech.Add(item);
                            }

                            if (item is Container container)
                            {
                                AddSpeechItemsFrom(onSpeech, container);
                            }
                        }
                    }
                    else if (o is Item item)
                    {
                        if (item.HandlesOnSpeech)
                        {
                            onSpeech.Add(item);
                        }

                        if (item is Container container)
                        {
                            AddSpeechItemsFrom(onSpeech, container);
                        }
                    }
                }

                eable.Free();

                object mutateContext = null;
                var mutatedText = text;
                SpeechEventArgs mutatedArgs = null;

                if (MutateSpeech(hears, ref mutatedText, ref mutateContext))
                {
                    mutatedArgs = new SpeechEventArgs(this, mutatedText, type, hue, Array.Empty<int>());
                }

                CheckSpeechManifest();

                ProcessDelta();

                Span<byte> regBuffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();
                Span<byte> mutBuffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(mutatedText)].InitializePacket();

                // TODO: Should this be sorted like onSpeech is below?
                for (var i = 0; i < hears.Count; ++i)
                {
                    var heard = hears[i];

                    if (mutatedArgs == null || !CheckHearsMutatedSpeech(heard, mutateContext))
                    {
                        var length = OutgoingMessagePackets.CreateMessage(
                            regBuffer, Serial, Body, type, hue, 3, false, m_Language, Name, text
                        );

                        if (length != regBuffer.Length)
                        {
                            regBuffer = regBuffer[..length]; // Adjust to the actual size
                        }

                        heard.OnSpeech(regArgs);
                        heard.NetState?.Send(regBuffer);
                    }
                    else
                    {
                        var length = OutgoingMessagePackets.CreateMessage(
                            mutBuffer, Serial, Body, type, hue, 3, false, m_Language, Name, mutatedText
                        );

                        if (length != mutBuffer.Length)
                        {
                            mutBuffer = mutBuffer[..length]; // Adjust to the actual size
                        }

                        heard.OnSpeech(mutatedArgs);
                        heard.NetState?.Send(mutBuffer);
                    }
                }

                if (onSpeech.Count > 1)
                {
                    onSpeech.Sort(LocationComparer.GetInstance(this));
                }

                for (var i = 0; i < onSpeech.Count; ++i)
                {
                    var obj = onSpeech[i];

                    if (obj is Mobile heard)
                    {
                        if (mutatedArgs == null || !CheckHearsMutatedSpeech(heard, mutateContext))
                        {
                            heard.OnSpeech(regArgs);
                        }
                        else
                        {
                            heard.OnSpeech(mutatedArgs);
                        }
                    }
                    else
                    {
                        ((Item)obj).OnSpeech(regArgs);
                    }
                }

                if (m_Hears.Count > 0)
                {
                    m_Hears.Clear();
                }

                if (m_OnSpeech.Count > 0)
                {
                    m_OnSpeech.Clear();
                }
            }
        }

        public Mobile FindMostRecentDamager(bool allowSelf) => FindMostRecentDamageEntry(allowSelf)?.Damager;

        public DamageEntry FindMostRecentDamageEntry(bool allowSelf)
        {
            for (var i = DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= DamageEntries.Count)
                {
                    continue;
                }

                var de = DamageEntries[i];

                if (de.HasExpired)
                {
                    DamageEntries.RemoveAt(i);
                }
                else if (allowSelf || de.Damager != this)
                {
                    return de;
                }
            }

            return null;
        }

        public Mobile FindLeastRecentDamager(bool allowSelf) => FindLeastRecentDamageEntry(allowSelf)?.Damager;

        public DamageEntry FindLeastRecentDamageEntry(bool allowSelf)
        {
            for (var i = 0; i < DamageEntries.Count; ++i)
            {
                if (i < 0)
                {
                    continue;
                }

                var de = DamageEntries[i];

                if (de.HasExpired)
                {
                    DamageEntries.RemoveAt(i);
                    --i;
                }
                else if (allowSelf || de.Damager != this)
                {
                    return de;
                }
            }

            return null;
        }

        public Mobile FindMostTotalDamager(bool allowSelf) => FindMostTotalDamageEntry(allowSelf)?.Damager;

        public DamageEntry FindMostTotalDamageEntry(bool allowSelf)
        {
            DamageEntry mostTotal = null;

            for (var i = DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= DamageEntries.Count)
                {
                    continue;
                }

                var de = DamageEntries[i];

                if (de.HasExpired)
                {
                    DamageEntries.RemoveAt(i);
                }
                else if ((allowSelf || de.Damager != this) && (mostTotal == null || de.DamageGiven > mostTotal.DamageGiven))
                {
                    mostTotal = de;
                }
            }

            return mostTotal;
        }

        public Mobile FindLeastTotalDamager(bool allowSelf) => FindLeastTotalDamageEntry(allowSelf)?.Damager;

        public DamageEntry FindLeastTotalDamageEntry(bool allowSelf)
        {
            DamageEntry mostTotal = null;

            for (var i = DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= DamageEntries.Count)
                {
                    continue;
                }

                var de = DamageEntries[i];

                if (de.HasExpired)
                {
                    DamageEntries.RemoveAt(i);
                }
                else if ((allowSelf || de.Damager != this) && (mostTotal == null || de.DamageGiven < mostTotal.DamageGiven))
                {
                    mostTotal = de;
                }
            }

            return mostTotal;
        }

        public DamageEntry FindDamageEntryFor(Mobile m)
        {
            for (var i = DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= DamageEntries.Count)
                {
                    continue;
                }

                var de = DamageEntries[i];

                if (de.HasExpired)
                {
                    DamageEntries.RemoveAt(i);
                }
                else if (de.Damager == m)
                {
                    return de;
                }
            }

            return null;
        }

        public virtual Mobile GetDamageMaster(Mobile damagee) => null;

        public virtual DamageEntry RegisterDamage(int amount, Mobile from)
        {
            var de = FindDamageEntryFor(from) ?? new DamageEntry(from);

            de.DamageGiven += amount;
            de.LastDamage = Core.Now;

            DamageEntries.Remove(de);
            DamageEntries.Add(de);

            var master = from.GetDamageMaster(this);

            if (master != null)
            {
                var list = de.Responsible;

                if (list == null)
                {
                    de.Responsible = list = new List<DamageEntry>();
                }

                DamageEntry resp = null;
                foreach (var check in list)
                {
                    if (check.Damager == master)
                    {
                        resp = check;
                        break;
                    }
                }

                if (resp == null)
                {
                    list.Add(resp = new DamageEntry(master));
                }

                resp.DamageGiven += amount;
                resp.LastDamage = Core.Now;
            }

            return de;
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile is <see cref="Damage">damaged</see>. It is called before
        ///     <see cref="Hits">hit points</see> are lowered or the Mobile is <see cref="Kill">killed</see>.
        ///     <seealso cref="Damage" />
        ///     <seealso cref="Hits" />
        ///     <seealso cref="Kill" />
        /// </summary>
        public virtual void OnDamage(int amount, Mobile from, bool willKill)
        {
        }

        public virtual bool CanBeDamaged() => !m_Blessed;

        public virtual void Damage(int amount, Mobile from = null, bool informMount = true)
        {
            if (!CanBeDamaged() || Deleted)
            {
                return;
            }

            if (!Region.OnDamage(this, ref amount))
            {
                return;
            }

            if (amount > 0)
            {
                var oldHits = Hits;
                var newHits = oldHits - amount;

                m_Spell?.OnCasterHurt();

                // if (m_Spell != null && m_Spell.State == SpellState.Casting)
                // m_Spell.Disturb( DisturbType.Hurt, false, true );

                if (from != null)
                {
                    RegisterDamage(amount, from);
                }

                DisruptiveAction();

                Paralyzed = false;

                switch (VisibleDamageType)
                {
                    case VisibleDamageType.Related:
                        {
                            SendVisibleDamageRelated(from, amount);
                            break;
                        }
                    case VisibleDamageType.Everyone:
                        {
                            SendVisibleDamageEveryone(amount);
                            break;
                        }
                    case VisibleDamageType.Selective:
                        {
                            SendVisibleDamageSelective(from, amount);
                            break;
                        }
                }

                OnDamage(amount, from, newHits < 0);

                if (informMount)
                {
                    Mount?.OnRiderDamaged(amount, from, newHits < 0);
                }

                if (newHits < 0)
                {
                    LastKiller = from;

                    Hits = 0;

                    if (oldHits >= 0)
                    {
                        Kill();
                    }
                }
                else
                {
                    Hits = newHits;
                }
            }
        }

        public void SendVisibleDamageRelated(Mobile from, int amount)
        {
            var ourState = m_NetState ?? GetDamageMaster(from)?.m_NetState;
            var theirState = from?.m_NetState ?? from?.GetDamageMaster(this)?.m_NetState;

            if (amount > 0)
            {
                ourState.SendDamage(Serial, amount);

                if (theirState != null && theirState != ourState)
                {
                    theirState.SendDamage(Serial, amount);
                }
            }
        }

        public void SendVisibleDamageEveryone(int amount)
        {
            if (amount < 0)
            {
                return;
            }

            var map = m_Map;

            if (map == null)
            {
                return;
            }

            var eable = map.GetClientsInRange(m_Location);

            foreach (var ns in eable)
            {
                if (ns.Mobile.CanSee(this))
                {
                    ns.SendDamage(Serial, amount);
                }
            }

            eable.Free();
        }

        public void SendVisibleDamageSelective(Mobile from, int amount)
        {
            var ourState = m_NetState;
            var theirState = from?.m_NetState;

            var damager = from;
            var damaged = this;

            if (ourState == null)
            {
                var master = GetDamageMaster(from);

                if (master != null)
                {
                    damaged = master;
                    ourState = master.m_NetState;
                }
            }

            if (!damaged.ShowVisibleDamage)
            {
                return;
            }

            if (theirState == null && from != null)
            {
                var master = from.GetDamageMaster(this);

                if (master != null)
                {
                    damager = master;
                    theirState = master.m_NetState;
                }
            }

            if (amount > 0)
            {
                if (damaged.CanSeeVisibleDamage)
                {
                    ourState.SendDamage(Serial, amount);
                }

                if (theirState != null && theirState != ourState && damager.CanSeeVisibleDamage)
                {
                    theirState.SendDamage(Serial, amount);
                }
            }
        }

        public void Heal(int amount) => Heal(amount, this);

        public void Heal(int amount, Mobile from, bool message = true)
        {
            if (!Alive || IsDeadBondedPet)
            {
                return;
            }

            if (!Region.OnHeal(this, ref amount))
            {
                return;
            }

            OnHeal(ref amount, from);

            if (Hits + amount > HitsMax)
            {
                amount = HitsMax - Hits;
            }

            Hits += amount;

            if (message && amount > 0)
            {
                m_NetState.SendMessageLocalizedAffix(
                    Serial.MinusOne,
                    -1,
                    MessageType.Label,
                    0x3B2,
                    3,
                    1008158,
                    "",
                    AffixType.Append | AffixType.System,
                    amount.ToString()
                );
            }
        }

        public virtual void OnHeal(ref int amount, Mobile from)
        {
        }

        public virtual void BeforeSerialize()
        {
        }

        public virtual void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 33:
                    {
                        // Removed created
                        goto case 32;
                    }
                case 32:
                    {
                        // Removed StuckMenu
                        goto case 31;
                    }
                case 31:
                    {
                        LastStrGain = reader.ReadDeltaTime();
                        LastIntGain = reader.ReadDeltaTime();
                        LastDexGain = reader.ReadDeltaTime();

                        goto case 30;
                    }
                case 30:
                    {
                        var hairflag = reader.ReadByte();

                        if ((hairflag & 0x01) != 0)
                        {
                            m_Hair = new HairInfo(reader);
                        }

                        if ((hairflag & 0x02) != 0)
                        {
                            m_FacialHair = new FacialHairInfo(reader);
                        }

                        goto case 29;
                    }
                case 29:
                    {
                        m_Race = reader.ReadRace();
                        goto case 28;
                    }
                case 28:
                    {
                        if (version <= 30)
                        {
                            LastStatGain = reader.ReadDeltaTime();
                        }

                        goto case 27;
                    }
                case 27:
                    {
                        m_TithingPoints = reader.ReadInt();

                        goto case 26;
                    }
                case 26:
                case 25:
                case 24:
                    {
                        Corpse = reader.ReadEntity<Container>();

                        goto case 23;
                    }
                case 23:
                    {
                        if (version < 33)
                        {
                            Created = reader.ReadDateTime();
                        }

                        goto case 22;
                    }
                case 22: // Just removed followers
                case 21:
                    {
                        Stabled = reader.ReadEntityList<Mobile>();

                        goto case 20;
                    }
                case 20:
                    {
                        CantWalk = reader.ReadBool();

                        goto case 19;
                    }
                case 19: // Just removed variables
                case 18:
                    {
                        Virtues = new VirtueInfo(reader);

                        goto case 17;
                    }
                case 17:
                    {
                        Thirst = reader.ReadInt();
                        BAC = reader.ReadInt();

                        goto case 16;
                    }
                case 16:
                    {
                        m_ShortTermMurders = reader.ReadInt();

                        if (version <= 24)
                        {
                            reader.ReadDateTime();
                            reader.ReadDateTime();
                        }

                        goto case 15;
                    }
                case 15:
                    {
                        if (version < 22)
                        {
                            reader.ReadInt(); // followers
                        }

                        m_FollowersMax = reader.ReadInt();

                        goto case 14;
                    }
                case 14:
                    {
                        MagicDamageAbsorb = reader.ReadInt();

                        goto case 13;
                    }
                case 13:
                    {
                        GuildFealty = reader.ReadEntity<Mobile>();

                        goto case 12;
                    }
                case 12:
                    {
                        m_Guild = reader.ReadEntity<BaseGuild>();

                        goto case 11;
                    }
                case 11:
                    {
                        m_DisplayGuildTitle = reader.ReadBool();

                        goto case 10;
                    }
                case 10:
                    {
                        CanSwim = reader.ReadBool();

                        goto case 9;
                    }
                case 9:
                    {
                        Squelched = reader.ReadBool();

                        goto case 8;
                    }
                case 8:
                    {
                        m_Holding = reader.ReadEntity<Item>();

                        goto case 7;
                    }
                case 7:
                    {
                        m_VirtualArmor = reader.ReadInt();

                        goto case 6;
                    }
                case 6:
                    {
                        BaseSoundID = reader.ReadInt();

                        goto case 5;
                    }
                case 5:
                    {
                        DisarmReady = reader.ReadBool();
                        StunReady = reader.ReadBool();

                        goto case 4;
                    }
                case 4:
                    {
                        if (version <= 25)
                        {
                            Poison.Deserialize(reader);
                        }

                        goto case 3;
                    }
                case 3:
                    {
                        m_StatCap = reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        NameHue = reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Hunger = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 21)
                        {
                            Stabled = new List<Mobile>();
                        }

                        if (version < 18)
                        {
                            Virtues = new VirtueInfo();
                        }

                        if (version < 11)
                        {
                            m_DisplayGuildTitle = true;
                        }

                        if (version < 3)
                        {
                            m_StatCap = 225;
                        }

                        if (version < 15)
                        {
                            m_Followers = 0;
                            m_FollowersMax = 5;
                        }

                        m_Location = reader.ReadPoint3D();
                        m_Body = new Body(reader.ReadInt());
                        m_Name = reader.ReadString();
                        m_GuildTitle = reader.ReadString();
                        m_Criminal = reader.ReadBool();
                        m_Kills = reader.ReadInt();
                        SpeechHue = reader.ReadInt();
                        EmoteHue = reader.ReadInt();
                        WhisperHue = reader.ReadInt();
                        YellHue = reader.ReadInt();
                        m_Language = reader.ReadString();
                        m_Female = reader.ReadBool();
                        m_Warmode = reader.ReadBool();
                        m_Hidden = reader.ReadBool();
                        m_Direction = (Direction)reader.ReadByte();
                        m_Hue = reader.ReadInt();
                        m_Str = reader.ReadInt();
                        m_Dex = reader.ReadInt();
                        m_Int = reader.ReadInt();
                        m_Hits = reader.ReadInt();
                        m_Stam = reader.ReadInt();
                        m_Mana = reader.ReadInt();
                        m_Map = reader.ReadMap();
                        m_Blessed = reader.ReadBool();
                        m_Fame = reader.ReadInt();
                        m_Karma = reader.ReadInt();
                        m_AccessLevel = (AccessLevel)reader.ReadByte();

                        Skills = new Skills(this, reader);

                        Items = reader.ReadEntityList<Item>();

                        m_Player = reader.ReadBool();
                        m_Title = reader.ReadString();
                        Profile = reader.ReadString();
                        ProfileLocked = reader.ReadBool();

                        if (version <= 18)
                        {
                            reader.ReadInt();
                            reader.ReadInt();
                            reader.ReadInt();
                        }

                        AutoPageNotify = reader.ReadBool();

                        LogoutLocation = reader.ReadPoint3D();
                        LogoutMap = reader.ReadMap();

                        m_StrLock = (StatLockType)reader.ReadByte();
                        m_DexLock = (StatLockType)reader.ReadByte();
                        m_IntLock = (StatLockType)reader.ReadByte();

                        StatMods = new List<StatMod>();
                        SkillMods = new List<SkillMod>();

                        if (version < 32)
                        {
                            if (reader.ReadBool())
                            {
                                var count = reader.ReadInt();
                                for (var i = 0; i < count; ++i)
                                {
                                    reader.ReadDateTime();
                                }
                            }
                        }

                        if (m_Player && m_Map != Map.Internal)
                        {
                            LogoutLocation = m_Location;
                            LogoutMap = m_Map;

                            m_Map = Map.Internal;
                        }

                        m_Map?.OnEnter(this);

                        if (m_Criminal)
                        {
                            Timer.StartTimer(ExpireCriminalDelay, ExpireCriminal, out _expireCriminalTimerToken);
                        }

                        if (ShouldCheckStatTimers)
                        {
                            CheckStatTimers();
                        }

                        UpdateRegion();

                        UpdateResistances();

                        break;
                    }
            }

            if (!m_Player)
            {
                Utility.Intern(ref m_Name);
            }

            Utility.Intern(ref m_Title);
            Utility.Intern(ref m_Language);
        }

        public void ConvertHair()
        {
            Item hair;

            if ((hair = FindItemOnLayer(Layer.Hair)) != null)
            {
                HairItemID = hair.ItemID;
                HairHue = hair.Hue;
                hair.Delete();
            }

            if ((hair = FindItemOnLayer(Layer.FacialHair)) != null)
            {
                FacialHairItemID = hair.ItemID;
                FacialHairHue = hair.Hue;
                hair.Delete();
            }
        }

        public virtual void CheckStatTimers()
        {
            if (Deleted)
            {
                return;
            }

            if (Hits < HitsMax)
            {
                if (CanRegenHits)
                {
                    m_HitsTimer ??= new HitsTimer(this);
                    m_HitsTimer.Start();
                }
                else
                {
                    m_HitsTimer?.Stop();
                }
            }
            else
            {
                Hits = HitsMax;
            }

            if (Stam < StamMax)
            {
                if (CanRegenStam)
                {
                    m_StamTimer ??= new StamTimer(this);
                    m_StamTimer.Start();
                }
                else
                {
                    m_StamTimer?.Stop();
                }
            }
            else
            {
                Stam = StamMax;
            }

            if (Mana < ManaMax)
            {
                if (CanRegenMana)
                {
                    m_ManaTimer ??= new ManaTimer(this);
                    m_ManaTimer.Start();
                }
                else
                {
                    m_ManaTimer?.Stop();
                }
            }
            else
            {
                Mana = ManaMax;
            }
        }

        public static string GetAccessLevelName(AccessLevel level) => m_AccessLevelNames[(int)level];

        public virtual bool CanPaperdollBeOpenedBy(Mobile from) => Body.IsHuman || Body.IsGhost || IsBodyMod;

        public virtual void GetChildContextMenuEntries(Mobile from, List<ContextMenuEntry> list, Item item)
        {
        }

        public virtual void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (Deleted)
            {
                return;
            }

            if (CanPaperdollBeOpenedBy(from))
            {
                list.Add(new PaperdollEntry(this));
            }

            if (from == this && Backpack != null && CanSee(Backpack) && CheckAlive(false))
            {
                list.Add(new OpenBackpackEntry(this));
            }
        }

        public void Internalize()
        {
            Map = Map.Internal;
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <paramref name="item" /> is <see cref="AddItem">added</see> from the Mobile,
        ///     such
        ///     as when it is equipped.
        ///     <seealso cref="Items" />
        ///     <seealso cref="OnItemRemoved" />
        /// </summary>
        public virtual void OnItemAdded(Item item)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <paramref name="item" /> is <see cref="RemoveItem">removed</see> from the
        ///     Mobile.
        ///     <seealso cref="Items" />
        ///     <seealso cref="OnItemAdded" />
        /// </summary>
        public virtual void OnItemRemoved(Item item)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <paramref name="item" /> is becomes a child of the Mobile; it's worn or
        ///     contained
        ///     at some level of the Mobile's <see cref="Mobile.Backpack">backpack</see> or <see cref="Mobile.BankBox">bank box</see>
        ///     <seealso cref="OnSubItemRemoved" />
        ///     <seealso cref="OnItemAdded" />
        /// </summary>
        public virtual void OnSubItemAdded(Item item)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <paramref name="item" /> is removed from the Mobile, its
        ///     <see cref="Mobile.Backpack">backpack</see>, or its <see cref="Mobile.BankBox">bank box</see>.
        ///     <seealso cref="OnSubItemAdded" />
        ///     <seealso cref="OnItemRemoved" />
        /// </summary>
        public virtual void OnSubItemRemoved(Item item)
        {
        }

        public virtual void OnItemBounceCleared(Item item)
        {
        }

        public virtual void OnSubItemBounceCleared(Item item)
        {
        }

        public void AddItem(Item item)
        {
            if (item?.Deleted != false)
            {
                return;
            }

            if (item.Parent == this)
            {
                return;
            }

            if (item.Parent is Mobile parentMobile)
            {
                parentMobile.RemoveItem(item);
            }
            else if (item.Parent is Item parentItem)
            {
                parentItem.RemoveItem(item);
            }
            else
            {
                item.SendRemovePacket();
            }

            item.Parent = this;
            item.Map = m_Map;

            Items.Add(item);

            if (!item.IsVirtualItem)
            {
                UpdateTotal(item, TotalType.Gold, item.TotalGold);
                UpdateTotal(item, TotalType.Items, item.TotalItems + 1);
                UpdateTotal(item, TotalType.Weight, item.TotalWeight + item.PileWeight);
            }

            item.Delta(ItemDelta.Update);

            item.OnAdded(this);
            OnItemAdded(item);

            if (item.PhysicalResistance != 0 || item.FireResistance != 0 || item.ColdResistance != 0 ||
                item.PoisonResistance != 0 || item.EnergyResistance != 0)
            {
                UpdateResistances();
            }
        }

        public void RemoveItem(Item item)
        {
            if (item == null || Items == null)
            {
                return;
            }

            if (Items.Remove(item))
            {
                item.SendRemovePacket();

                if (!item.IsVirtualItem)
                {
                    UpdateTotal(item, TotalType.Gold, -item.TotalGold);
                    UpdateTotal(item, TotalType.Items, -(item.TotalItems + 1));
                    UpdateTotal(item, TotalType.Weight, -(item.TotalWeight + item.PileWeight));
                }

                item.Parent = null;

                item.OnRemoved(this);
                OnItemRemoved(item);

                if (item.PhysicalResistance != 0 || item.FireResistance != 0 || item.ColdResistance != 0 ||
                    item.PoisonResistance != 0 || item.EnergyResistance != 0)
                {
                    UpdateResistances();
                }
            }
        }

        public virtual void Animate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
        {
            var map = m_Map;

            if (map == null)
            {
                return;
            }

            ProcessDelta();

            var eable = map.GetClientsInRange(m_Location);

            Span<byte> buffer = stackalloc byte[OutgoingMobilePackets.MobileAnimationPacketLength].InitializePacket();

            foreach (var state in eable)
            {
                if (!state.Mobile.CanSee(this))
                {
                    continue;
                }

                state.Mobile.ProcessDelta();

                if (Body.IsGargoyle)
                {
                    frameCount = 10;

                    if (Flying)
                    {
                        action = action switch
                        {
                            >= 9 and <= 11    => 71,
                            >= 12 and <= 14   => 72,
                            20                => 77,
                            31                => 71,
                            34                => 78,
                            >= 200 and <= 259 => 75,
                            >= 260 and <= 270 => 75,
                            _                 => action
                        };
                    }
                    else
                    {
                        action = action switch
                        {
                            >= 200 and <= 259 => 17,
                            >= 260 and <= 270 => 16,
                            _                 => action
                        };
                    }
                }

                OutgoingMobilePackets.CreateMobileAnimation(
                    buffer,
                    Serial,
                    action,
                    frameCount,
                    repeatCount,
                    forward,
                    repeat,
                    delay
                );

                state.Send(buffer);
            }

            eable.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendSound(int soundID)
        {
            m_NetState.SendSoundEffect(soundID, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendSound(int soundID, IPoint3D p)
        {
            m_NetState.SendSoundEffect(soundID, p);
        }

        /**
         * Plays a sound to netstates that can see this mobile
         */
        public void PlaySound(int soundID)
        {
            if (soundID == -1 || m_Map == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingEffectPackets.SoundPacketLength].InitializePacket();

            var eable = m_Map.GetClientsInRange(m_Location);

            foreach (var state in eable)
            {
                if (state.Mobile.CanSee(this))
                {
                    OutgoingEffectPackets.CreateSoundEffect(buffer, soundID, this);
                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public void SendOPLPacketTo(NetState state) => SendOPLPacketTo(state, ObjectPropertyList.Enabled);

        protected virtual void SendOPLPacketTo(NetState ns, bool sendOplPacket)
        {
            if (sendOplPacket)
            {
                ns.SendOPLInfo(this);
            }
        }

        public virtual void SendOPLPacketTo(NetState ns, ReadOnlySpan<byte> opl)
        {
            ns?.Send(opl);
        }

        public virtual void OnAccessLevelChanged(AccessLevel oldLevel)
        {
        }

        public virtual void OnFameChange(int oldValue)
        {
        }

        public virtual void OnKarmaChange(int oldValue)
        {
        }

        // Mobile did something which should unhide him
        public virtual void RevealingAction()
        {
            if (m_Hidden && m_AccessLevel == AccessLevel.Player)
            {
                Hidden = false;
            }

            DisruptiveAction(); // Anything that unhides you will also distrupt meditation
        }

        public void SendRemovePacket(bool everyone = true)
        {
            if (m_Map == null)
            {
                return;
            }

            var eable = m_Map.GetClientsInRange(m_Location);

            Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

            foreach (var state in eable)
            {
                if (state != m_NetState && (everyone || !state.Mobile.CanSee(this)))
                {
                    OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                    state.Send(removeEntity);
                }
            }

            eable.Free();
        }

        public void ClearScreen()
        {
            if (m_Map == null || m_NetState == null)
            {
                return;
            }

            var eable = m_Map.GetObjectsInRange(m_Location, Core.GlobalMaxUpdateRange);

            foreach (var o in eable)
            {
                if (o is Mobile m)
                {
                    if (m != this && Utility.InUpdateRange(m_Location, m.m_Location))
                    {
                        m_NetState.SendRemoveEntity(m.Serial);
                    }
                }
                else if (o is Item item)
                {
                    if (InRange(item.Location, item.GetUpdateRange(this)))
                    {
                        m_NetState.SendRemoveEntity(item.Serial);
                    }
                }
            }

            eable.Free();
        }

        /// <summary>
        ///     Overridable. Event invoked before the Mobile says something.
        ///     <seealso cref="DoSpeech" />
        /// </summary>
        public virtual void OnSaid(SpeechEventArgs e)
        {
            if (Squelched)
            {
                if (Core.ML)
                {
                    SendLocalizedMessage(500168); // You can not say anything, you have been muted.
                }
                else
                {
                    SendMessage("You can not say anything, you have been squelched."); // Cliloc ITSELF changed during ML.
                }

                e.Blocked = true;
            }

            if (!e.Blocked)
            {
                RevealingAction();
            }
        }

        public virtual bool HandlesOnSpeech(Mobile from) => false;

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile hears speech. This event will only be invoked if
        ///     <see cref="HandlesOnSpeech" /> returns true.
        ///     <seealso cref="DoSpeech" />
        /// </summary>
        public virtual void OnSpeech(SpeechEventArgs e)
        {
        }

        public void SendEverything()
        {
            var ns = m_NetState;

            if (m_Map == null || ns == null)
            {
                return;
            }

            var eable = m_Map.GetObjectsInRange(m_Location, Core.GlobalMaxUpdateRange);

            foreach (var o in eable)
            {
                if (o is Item item)
                {
                    if (CanSee(item) && InRange(item.Location, item.GetUpdateRange(this)))
                    {
                        item.SendInfoTo(ns);
                    }
                }
                else if (o is Mobile m)
                {
                    if (CanSee(m) && Utility.InUpdateRange(m_Location, m.m_Location))
                    {
                        ns.SendMobileIncoming(this, m);

                        if (ns.StygianAbyss)
                        {
                            ns.SendMobileHealthbar(m, Healthbar.Poison);
                            ns.SendMobileHealthbar(m, Healthbar.Yellow);
                        }

                        if (m.IsDeadBondedPet)
                        {
                            ns.SendBondedStatus(m.Serial, true);
                        }

                        m.SendOPLPacketTo(ns);
                    }
                }
            }

            eable.Free();
        }

        public void UpdateRegion()
        {
            if (Deleted)
            {
                return;
            }

            var newRegion = Region.Find(m_Location, m_Map);

            if (newRegion != m_Region)
            {
                Region.OnRegionChange(this, m_Region, newRegion);

                m_Region = newRegion;
                OnRegionChange(m_Region, newRegion);
            }
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <see cref="Map" /> changes.
        /// </summary>
        protected virtual void OnMapChange(Map oldMap)
        {
        }

        public void SetDirection(Direction dir)
        {
            m_Direction = dir;
        }

        public virtual int GetSeason() => m_Map?.Season ?? 1;

        public virtual int GetPacketFlags(bool stygianAbyss)
        {
            var flags = 0x0;

            if (m_Paralyzed || m_Frozen)
            {
                flags |= 0x01;
            }

            if (m_Female)
            {
                flags |= 0x02;
            }

            if (stygianAbyss)
            {
                if (m_Flying)
                {
                    flags |= 0x04;
                }
            }
            else
            {
                if (m_Poison != null)
                {
                    flags |= 0x04;
                }
            }

            if (m_Blessed || m_YellowHealthbar)
            {
                flags |= 0x08;
            }

            if (m_Warmode)
            {
                flags |= 0x40;
            }

            if (m_Hidden)
            {
                flags |= 0x80;
            }

            return flags;
        }

        public virtual void OnGenderChanged(bool oldFemale)
        {
        }

        public virtual void ToggleFlying()
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked after the Warmode property has changed.
        /// </summary>
        public virtual void OnWarmodeChanged()
        {
        }

        public virtual void OnHiddenChanged()
        {
            AllowedStealthSteps = 0;

            if (m_Map == null)
            {
                return;
            }

            var eable = m_Map.GetClientsInRange(m_Location);

            Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

            foreach (var state in eable)
            {
                var m = state.Mobile;
                if (!m.CanSee(this))
                {
                    OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                    state.Send(removeEntity);
                }
                else
                {
                    state.SendMobileIncoming(m, this);

                    if (IsDeadBondedPet)
                    {
                        state.SendBondedStatus(Serial, true);
                    }

                    SendOPLPacketTo(state);
                }
            }

            eable.Free();
        }

        public virtual void OnConnected()
        {
        }

        public virtual void OnDisconnected()
        {
        }

        public virtual void OnNetStateChanged()
        {
        }

        public virtual bool CanSee(object o)
        {
            return o switch
            {
                Item item     => CanSee(item),
                Mobile mobile => CanSee(mobile),
                _             => true
            };
        }

        public virtual bool CanSee(Item item)
        {
            if (m_Map == Map.Internal)
            {
                return false;
            }

            if (item.Map == Map.Internal)
            {
                return false;
            }

            if (item.Parent != null)
            {
                if (item.Parent is Item parent)
                {
                    if (!(CanSee(parent) && parent.IsChildVisibleTo(this, item)))
                    {
                        return false;
                    }
                }
                else if (item.Parent is Mobile mobile)
                {
                    if (!CanSee(mobile))
                    {
                        return false;
                    }
                }
            }

            if (item is BankBox box && m_AccessLevel <= AccessLevel.Counselor && (box.Owner != this || !box.Opened))
            {
                return false;
            }

            if (item is SecureTradeContainer container)
            {
                var trade = container.Trade;

                if (trade != null && trade.From.Mobile != this && trade.To.Mobile != this)
                {
                    return false;
                }
            }

            return !item.Deleted && item.Map == m_Map && (item.Visible || item.CanSeeStaffOnly(this));
        }

        public virtual bool CanSee(Mobile m)
        {
            if (Deleted || m.Deleted || m_Map == Map.Internal || m.m_Map == Map.Internal)
            {
                return false;
            }

            return this == m || m.m_Map == m_Map &&
                (!m.Hidden || m_AccessLevel != AccessLevel.Player &&
                    (m_AccessLevel >= m.AccessLevel || m_AccessLevel >= AccessLevel.Administrator)) &&
                (m.Alive || Core.SE && Skills.SpiritSpeak.Value >= 100.0 || !Alive ||
                 m_AccessLevel > AccessLevel.Player || m.Warmode);
        }

        public virtual bool CanBeRenamedBy(Mobile from) =>
            from.AccessLevel >= AccessLevel.GameMaster && from.m_AccessLevel > m_AccessLevel;

        public virtual void OnGuildTitleChange(string oldTitle)
        {
        }

        public virtual void OnAfterNameChange(string oldName, string newName)
        {
        }

        public virtual void OnGuildChange(BaseGuild oldGuild)
        {
        }

        public virtual int SafeBody(int body)
        {
            var delta = -1;

            for (var i = 0; delta < 0 && i < m_InvalidBodies.Length; ++i)
            {
                delta = m_InvalidBodies[i] - body;
            }

            return delta != 0 ? body : 0;
        }

        private ObjectPropertyList InitializePropertyList(ObjectPropertyList list)
        {
            GetProperties(list);
            list.Terminate();
            return list;
        }

        public void ClearProperties()
        {
            m_PropertyList = null;
        }

#nullable enable
        public void InvalidateProperties()
        {
            if (!ObjectPropertyList.Enabled)
            {
                return;
            }

            if (m_Map != null && m_Map != Map.Internal && !World.Loading)
            {
                int? oldHash;
                int newHash;
                if (m_PropertyList != null)
                {
                    oldHash = m_PropertyList.Hash;
                    m_PropertyList.Reset();
                    InitializePropertyList(m_PropertyList);
                    newHash = m_PropertyList.Hash;
                }
                else
                {
                    oldHash = null;
                    newHash = PropertyList.Hash;
                }

                if (oldHash != newHash)
                {
                    Delta(MobileDelta.Properties);
                }
            }
            else
            {
                ClearProperties();
            }
        }
#nullable restore

        public virtual void SetLocation(Point3D newLocation, bool isTeleport)
        {
            if (Deleted)
            {
                return;
            }

            var oldLocation = m_Location;

            if (oldLocation == newLocation)
            {
                return;
            }

            m_Location = newLocation;
            UpdateRegion();

            var box = FindBankNoCreate();

            if (box?.Opened == true)
            {
                box.Close();
            }

            m_NetState?.ValidateAllTrades();

            m_Map?.OnMove(oldLocation, this);

            if (isTeleport && m_NetState != null && (!m_NetState.HighSeas || !NoMoveHS))
            {
                m_NetState.Sequence = 0;
                m_NetState.SendMobileUpdate(this);
            }

            var map = m_Map;

            if (map != null)
            {
                // First, send a remove message to everyone who can no longer see us. (inOldRange && !inNewRange)

                var eable = map.GetClientsInRange(oldLocation);

                Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

                foreach (var ns in eable)
                {
                    if (ns != m_NetState && !Utility.InUpdateRange(newLocation, ns.Mobile.Location))
                    {
                        OutgoingEntityPackets.CreateRemoveEntity(removeEntity, Serial);
                        ns.Send(removeEntity);
                    }
                }

                eable.Free();

                var ourState = m_NetState;

                // Check to see if we are attached to a client
                if (ourState != null)
                {
                    var eeable = map.GetObjectsInRange(newLocation, Core.GlobalMaxUpdateRange);

                    // We are attached to a client, so it's a bit more complex. We need to send new items and people to ourself, and ourself to other clients

                    foreach (var o in eeable)
                    {
                        if (o is Item item)
                        {
                            var range = item.GetUpdateRange(this);
                            var loc = item.Location;

                            if (!Utility.InRange(oldLocation, loc, range) && Utility.InRange(newLocation, loc, range) &&
                                CanSee(item))
                            {
                                item.SendInfoTo(ourState);
                            }
                        }
                        else if (o != this && o is Mobile m)
                        {
                            if (!Utility.InUpdateRange(newLocation, m.m_Location))
                            {
                                continue;
                            }

                            var inOldRange = Utility.InUpdateRange(oldLocation, m.m_Location);
                            var ns = m.m_NetState;

                            if (ns != null &&
                                (isTeleport && (!ns.HighSeas || !NoMoveHS) || !inOldRange) && m.CanSee(this))
                            {
                                ns.SendMobileIncoming(m, this);

                                if (ns.StygianAbyss)
                                {
                                    ns.SendMobileHealthbar(this, Healthbar.Poison);
                                    ns.SendMobileHealthbar(this, Healthbar.Yellow);
                                }

                                if (IsDeadBondedPet)
                                {
                                    ns.SendBondedStatus(Serial, true);
                                }

                                SendOPLPacketTo(ns);
                            }

                            if (inOldRange || !CanSee(m))
                            {
                                continue;
                            }

                            ourState.SendMobileIncoming(this, m);

                            if (ourState.StygianAbyss)
                            {
                                ourState.SendMobileHealthbar(m, Healthbar.Poison);
                                ourState.SendMobileHealthbar(m, Healthbar.Yellow);
                            }

                            if (m.IsDeadBondedPet)
                            {
                                ourState.SendBondedStatus(m.Serial, true);
                            }

                            m.SendOPLPacketTo(ourState);
                        }
                    }

                    eeable.Free();
                }
                else
                {
                    eable = map.GetClientsInRange(newLocation);

                    // We're not attached to a client, so simply send an Incoming
                    foreach (var ns in eable)
                    {
                        var m = ns.Mobile;
                        if ((isTeleport && (!ns.HighSeas || !NoMoveHS) || !Utility.InUpdateRange(oldLocation, m.Location)) &&
                            m.CanSee(this))
                        {
                            ns.SendMobileIncoming(m, this);

                            if (ns.StygianAbyss)
                            {
                                ns.SendMobileHealthbar(this, Healthbar.Poison);
                                ns.SendMobileHealthbar(this, Healthbar.Yellow);
                            }

                            if (IsDeadBondedPet)
                            {
                                ns.SendBondedStatus(Serial, true);
                            }

                            SendOPLPacketTo(ns);
                        }
                    }

                    eable.Free();
                }
            }

            OnLocationChange(oldLocation);

            Region.OnLocationChanged(this, oldLocation);
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <see cref="Location" /> changes.
        /// </summary>
        protected virtual void OnLocationChange(Point3D oldLocation)
        {
        }

        public bool HasFreeHand() => FindItemOnLayer(Layer.TwoHanded) == null;

        public virtual IWeapon GetDefaultWeapon() => DefaultWeapon;

        public BankBox FindBankNoCreate()
        {
            if (m_BankBox?.Deleted != false || m_BankBox.Parent != this)
            {
                m_BankBox = FindItemOnLayer(Layer.Bank) as BankBox;
            }

            return m_BankBox;
        }

        public Item FindItemOnLayer(Layer layer)
        {
            var eq = Items;
            var count = eq.Count;

            for (var i = 0; i < count; ++i)
            {
                var item = eq[i];

                if (!item.Deleted && item.Layer == layer)
                {
                    return item;
                }
            }

            return null;
        }

        public void SendIncomingPacket()
        {
            if (m_Map == null)
            {
                return;
            }

            var eable = m_Map.GetClientsInRange(m_Location);

            foreach (var state in eable)
            {
                var m = state.Mobile;
                if (m.CanSee(this))
                {
                    state.SendMobileIncoming(m, this);

                    if (state.StygianAbyss)
                    {
                        state.SendMobileHealthbar(this, Healthbar.Poison);
                        state.SendMobileHealthbar(this, Healthbar.Yellow);
                    }

                    if (IsDeadBondedPet)
                    {
                        state.SendBondedStatus(Serial, true);
                    }

                    SendOPLPacketTo(state);
                }
            }

            eable.Free();
        }

        public bool PlaceInBackpack(Item item)
        {
            if (item.Deleted)
            {
                return false;
            }

            return Backpack?.TryDropItem(this, item, false) == true;
        }

        public bool AddToBackpack(Item item)
        {
            if (item.Deleted)
            {
                return false;
            }

            if (!PlaceInBackpack(item))
            {
                var loc = m_Location;
                var map = m_Map;

                if ((map == null || map == Map.Internal) && LogoutMap != null)
                {
                    loc = LogoutLocation;
                    map = LogoutMap;
                }

                item.MoveToWorld(loc, map);
                return false;
            }

            return true;
        }

        public virtual bool CheckLift(Mobile from, Item item, ref LRReason reject) => true;

        public virtual bool CheckNonlocalLift(Mobile from, Item item) =>
            from == this || from.AccessLevel > AccessLevel && from.AccessLevel >= AccessLevel.GameMaster;

        public virtual bool CheckTrade(
            Mobile to, Item item, SecureTradeContainer cont, bool message, bool checkItems,
            int plusItems, int plusWeight
        ) =>
            true;

        public virtual bool OpenTrade(Mobile from, Item offer = null)
        {
            if (!from.Player || !Player || !from.Alive || !Alive)
            {
                return false;
            }

            var ourState = m_NetState;
            var theirState = from.m_NetState;

            if (ourState == null || theirState == null)
            {
                return false;
            }

            var cont = theirState.FindTradeContainer(this);

            if (!from.CheckTrade(this, offer, cont, true, true, 0, 0))
            {
                return false;
            }

            cont ??= theirState.AddTrade(ourState);

            if (offer != null)
            {
                cont.DropItem(offer);
            }

            return true;
        }

        /// <summary>
        ///     Overridable. Event invoked when a Mobile (<paramref name="from" />) drops an
        ///     <see cref="Item">
        ///         <paramref name="dropped" />
        ///     </see>
        ///     onto the Mobile.
        /// </summary>
        public virtual bool OnDragDrop(Mobile from, Item dropped)
        {
            if (from == this)
            {
                var pack = Backpack;
                return pack != null && dropped.DropToItem(from, pack, new Point3D(-1, -1, 0));
            }

            return from.InRange(Location, 2) && OpenTrade(from, dropped);
        }

        public virtual bool CheckEquip(Item item)
        {
            for (var i = 0; i < Items.Count; ++i)
            {
                if (Items[i].CheckConflictingLayer(this, item, item.Layer) ||
                    item.CheckConflictingLayer(this, Items[i], Items[i].Layer))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile attempts to wear <paramref name="item" />.
        /// </summary>
        /// <returns>True if the request is accepted, false if otherwise.</returns>
        public virtual bool OnEquip(Item item)
        {
            // For some reason OSI allows equipping quest items, but they are unmarked in the process
            if (item.QuestItem)
            {
                item.QuestItem = false;

                // An item must be in your backpack (and not in a container within) to be toggled as a quest item.
                SendLocalizedMessage(1074769);
            }

            return true;
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile attempts to lift <paramref name="item" />.
        /// </summary>
        /// <returns>True if the lift is allowed, false if otherwise.</returns>
        /// <example>
        ///     The following example demonstrates usage. It will disallow any attempts to pick up a pick axe if the Mobile does not
        ///     have
        ///     enough strength.
        ///     <code>
        ///   public override bool OnDragLift( Item item )
        ///   {
        ///     if (item is Pickaxe &amp;&amp; this.Str &lt; 60)
        ///     {
        ///       SendMessage( "That is too heavy for you to lift." );
        ///       return false;
        ///     }
        ///
        ///     return base.OnDragLift( item );
        ///   }</code>
        /// </example>
        public virtual bool OnDragLift(Item item) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> into a
        ///     <see cref="Container">
        ///         <paramref name="container" />
        ///     </see>
        ///     .
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemInto(Item item, Container container, Point3D loc) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> directly onto another
        ///     <see cref="Item" />, <paramref name="target" />. This is the case of stacking items.
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemOnto(Item item, Item target) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> into another
        ///     <see cref="Item" />, <paramref name="target" />. The target item is most likely a <see cref="Container" />.
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemToItem(Item item, Item target, Point3D loc) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile attempts to give <paramref name="item" /> to a Mobile (
        ///     <paramref name="target" />).
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemToMobile(Item item, Mobile target) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> to the world at a
        ///     <see cref="Point3D">
        ///         <paramref name="location" />
        ///     </see>
        ///     .
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemToWorld(Item item, Point3D location) => true;

        /// <summary>
        ///     Overridable. Virtual event when <paramref name="from" /> successfully uses <paramref name="item" /> while it's on this
        ///     Mobile.
        ///     <seealso cref="Item.OnItemUsed" />
        /// </summary>
        public virtual void OnItemUsed(Mobile from, Item item)
        {
        }

        public virtual bool CheckNonlocalDrop(Mobile from, Item item, Item target) =>
            from == this || from.AccessLevel > AccessLevel && from.AccessLevel >= AccessLevel.GameMaster;

        public virtual bool CheckItemUse(Mobile from, Item item) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when <paramref name="from" /> successfully lifts <paramref name="item" /> from this
        ///     Mobile.
        ///     <seealso cref="Item.OnItemLifted" />
        /// </summary>
        public virtual void OnItemLifted(Mobile from, Item item)
        {
        }

        public virtual bool AllowItemUse(Item item) => true;

        public virtual bool AllowEquipFrom(Mobile mob) =>
            mob == this || mob.AccessLevel >= AccessLevel.GameMaster && mob.AccessLevel > AccessLevel;

        public virtual bool EquipItem(Item item)
        {
            if (item?.Deleted != false || !item.CanEquip(this))
            {
                return false;
            }

            if (CheckEquip(item) && OnEquip(item) && item.OnEquip(this))
            {
                if (m_Spell?.OnCasterEquipping(item) == false)
                {
                    return false;
                }

                // if (m_Spell != null && m_Spell.State == SpellState.Casting)
                // m_Spell.Disturb( DisturbType.EquipRequest );

                AddItem(item);
                return true;
            }

            return false;
        }

        public void DefaultMobileInit()
        {
            m_StatCap = 225;
            m_FollowersMax = 5;
            Skills = new Skills(this);
            Items = new List<Item>();
            StatMods = new List<StatMod>();
            SkillMods = new List<SkillMod>();
            Map = Map.Internal;
            AutoPageNotify = true;
            Aggressors = new List<AggressorInfo>();
            Aggressed = new List<AggressorInfo>();
            Virtues = new VirtueInfo();
            Stabled = new List<Mobile>();
            DamageEntries = new List<DamageEntry>();

            NextSkillTime = Core.TickCount;
        }

        public virtual void Delta(MobileDelta flag)
        {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Attempting to queue a delta change from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

            if (m_Map == null || m_Map == Map.Internal || Deleted)
            {
                return;
            }

            m_DeltaFlags |= flag;

            if (!m_InDeltaQueue)
            {
                m_InDeltaQueue = true;
                m_DeltaQueue.Enqueue(this);
            }
        }

        public static void ProcessDeltaQueue()
        {
            var limit = m_DeltaQueue.Count;

            while (m_DeltaQueue.Count > 0 && --limit >= 0)
            {
                var mob = m_DeltaQueue.Dequeue();

                if (mob == null)
                {
                    continue;
                }

                mob.m_InDeltaQueue = false;

                try
                {
                    mob.ProcessDelta();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine("Process Delta Queue for {0} failed: {1}", mob, ex);
#endif
                }
            }

            if (m_DeltaQueue.Count > 0)
            {
                Utility.PushColor(ConsoleColor.DarkYellow);
                Console.WriteLine("Warning: {0} mobiles left in delta queue after processing.", m_DeltaQueue.Count);
                Utility.PopColor();
            }
        }

        public virtual void OnKillsChange(int oldValue)
        {
        }

        public bool CheckAlive(bool message = true)
        {
            if (Alive)
            {
                return true;
            }

            if (message)
            {
                LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019048); // I am dead and cannot do that.
            }

            return false;
        }

        public void LaunchBrowser(string url)
        {
            m_NetState?.LaunchBrowser(url);
        }

        public void InitStats(int str, int dex, int intel)
        {
            m_Str = str;
            m_Dex = dex;
            m_Int = intel;

            Hits = HitsMax;
            Stam = StamMax;
            Mana = ManaMax;

            Delta(MobileDelta.Stat | MobileDelta.Hits | MobileDelta.Stam | MobileDelta.Mana);
        }

        public virtual void DisplayPaperdollTo(Mobile to)
        {
            EventSink.InvokePaperdollRequest(to, this);
        }

        /// <summary>
        ///     Overridable. Event invoked when the Mobile requests to open his own paperdoll via the 'Open Paperdoll' macro.
        /// </summary>
        public virtual void OnPaperdollRequest()
        {
            if (CanPaperdollBeOpenedBy(this))
            {
                DisplayPaperdollTo(this);
            }
        }

        /// <summary>
        ///     Overridable. Event invoked when <paramref name="from" /> wants to see this Mobile's stats.
        /// </summary>
        /// <param name="from"></param>
        public virtual void OnStatsQuery(Mobile from)
        {
            if (from.Map == Map && Utility.InUpdateRange(Location, from.Location) && from.CanSee(this))
            {
                from.m_NetState.SendMobileStatus(from, this);
            }

            if (from == this)
            {
                m_NetState.SendStatLockInfo(this);
            }

            if (Party is IParty ip)
            {
                ip.OnStatsQuery(from, this);
            }
        }

        /// <summary>
        ///     Overridable. Event invoked when <paramref name="from" /> wants to see this Mobile's skills.
        /// </summary>
        public virtual void OnSkillsQuery(Mobile from)
        {
            if (from == this)
            {
                m_NetState.SendSkillsUpdate(Skills);
            }
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <see cref="Region" /> changes.
        /// </summary>
        public virtual void OnRegionChange(Region old, Region @new)
        {
        }

        /// <summary>
        ///     Overridable. Event invoked when the Mobile is single clicked.
        /// </summary>
        public virtual void OnSingleClick(Mobile from)
        {
            if (Deleted || AccessLevel == AccessLevel.Player && DisableHiddenSelfClick && Hidden && from == this)
            {
                return;
            }

            if (GuildClickMessage)
            {
                var guild = m_Guild;

                if (guild != null && (m_DisplayGuildTitle || m_Player && guild.Type != GuildType.Regular))
                {
                    var title = GuildTitle?.Trim() ?? "";
                    string type;

                    if (guild.Type >= 0 && (int)guild.Type < m_GuildTypes.Length)
                    {
                        type = m_GuildTypes[(int)guild.Type];
                    }
                    else
                    {
                        type = "";
                    }

                    var text = string.Format(
                        title.Length <= 0 ? "[{1}]{2}" : "[{0}, {1}]{2}",
                        title,
                        guild.Abbreviation,
                        type
                    );

                    PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, text, from.NetState);
                }
            }

            int hue;

            if (NameHue != -1)
            {
                hue = NameHue;
            }
            else if (AccessLevel > AccessLevel.Player)
            {
                hue = 11;
            }
            else
            {
                hue = Notoriety.GetHue(Notoriety.Compute(from, this));
            }

            var name = Name ?? "";

            var prefix = "";

            if (ShowFameTitle && (m_Player || m_Body.IsHuman) && m_Fame >= 10000)
            {
                prefix = m_Female ? "Lady" : "Lord";
            }

            var suffix = "";

            if (ClickTitle && !string.IsNullOrEmpty(Title))
            {
                suffix = Title;
            }

            suffix = ApplyNameSuffix(suffix);

            string val;

            if (prefix.Length > 0 && suffix.Length > 0)
            {
                val = $"{prefix} {name} {suffix}";
            }
            else if (prefix.Length > 0)
            {
                val = $"{prefix} {name}";
            }
            else if (suffix.Length > 0)
            {
                val = $"{name} {suffix}";
            }
            else
            {
                val = name;
            }

            PrivateOverheadMessage(MessageType.Label, hue, AsciiClickMessage, val, from.NetState);
        }

        public bool CheckSkill(SkillName skill, double minSkill, double maxSkill) =>
            SkillCheckLocationHandler?.Invoke(this, skill, minSkill, maxSkill) == true;

        public bool CheckSkill(SkillName skill, double chance) =>
            SkillCheckDirectLocationHandler?.Invoke(this, skill, chance) == true;

        public bool CheckTargetSkill(SkillName skill, object target, double minSkill, double maxSkill) =>
            SkillCheckTargetHandler?.Invoke(this, skill, target, minSkill, maxSkill) == true;

        public bool CheckTargetSkill(SkillName skill, object target, double chance) =>
            SkillCheckDirectTargetHandler?.Invoke(this, skill, target, chance) == true;

        public virtual void DisruptiveAction()
        {
            if (Meditating)
            {
                Meditating = false;
                SendLocalizedMessage(500134); // You stop meditating.
            }
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the sector this Mobile is in gets <see cref="Sector.Activate">activated</see>.
        /// </summary>
        public virtual void OnSectorActivate()
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the sector this Mobile is in gets
        ///     <see cref="Sector.Deactivate">deactivated</see>.
        /// </summary>
        public virtual void OnSectorDeactivate()
        {
        }

        public static TimeSpan GetHitsRegenRate(Mobile m) => HitsRegenRateHandler?.Invoke(m) ?? DefaultHitsRate;

        public static TimeSpan GetStamRegenRate(Mobile m) => StamRegenRateHandler?.Invoke(m) ?? DefaultStamRate;

        public static TimeSpan GetManaRegenRate(Mobile m) => ManaRegenRateHandler?.Invoke(m) ?? DefaultManaRate;

        public static char[] DefaultGhostChars = { 'o', 'O' };

        public Prompt BeginPrompt(PromptCallback callback, PromptCallback cancelCallback) =>
            Prompt = new SimplePrompt(callback, cancelCallback);

        public Prompt BeginPrompt(PromptCallback callback, bool callbackHandlesCancel = false) =>
            Prompt = new SimplePrompt(callback, callbackHandlesCancel);

        public Prompt BeginPrompt<T>(PromptStateCallback<T> callback, PromptStateCallback<T> cancelCallback, T state) =>
            Prompt = new SimpleStatePrompt<T>(callback, cancelCallback, state);

        public Prompt BeginPrompt<T>(PromptStateCallback<T> callback, bool callbackHandlesCancel, T state) =>
            Prompt = new SimpleStatePrompt<T>(callback, callbackHandlesCancel, state);

        public Prompt BeginPrompt<T>(PromptStateCallback<T> callback, T state) =>
            BeginPrompt(callback, false, state);

        public virtual int GetAngerSound()
        {
            if (BaseSoundID != 0)
            {
                return BaseSoundID;
            }

            return -1;
        }

        public virtual int GetIdleSound()
        {
            if (BaseSoundID != 0)
            {
                return BaseSoundID + 1;
            }

            return -1;
        }

        public virtual int GetAttackSound()
        {
            if (BaseSoundID != 0)
            {
                return BaseSoundID + 2;
            }

            return -1;
        }

        public virtual int GetHurtSound()
        {
            if (BaseSoundID != 0)
            {
                return BaseSoundID + 3;
            }

            return -1;
        }

        public virtual int GetDeathSound()
        {
            if (BaseSoundID != 0)
            {
                return BaseSoundID + 4;
            }

            if (m_Body.IsHuman)
            {
                return Utility.Random(m_Female ? 0x314 : 0x423, m_Female ? 4 : 5);
            }

            return -1;
        }

        public IPooledEnumerable<Item> GetItemsInRange(int range) => GetItemsInRange<Item>(range);

        public IPooledEnumerable<T> GetItemsInRange<T>(int range) where T : Item =>
            m_Map?.GetItemsInRange<T>(m_Location, range) ?? Map.NullEnumerable<T>.Instance;

        public IPooledEnumerable<IEntity> GetObjectsInRange(int range) =>
            m_Map?.GetObjectsInRange(m_Location, range) ?? Map.NullEnumerable<IEntity>.Instance;

        public IPooledEnumerable<Mobile> GetMobilesInRange(int range) => GetMobilesInRange<Mobile>(range);

        public IPooledEnumerable<T> GetMobilesInRange<T>(int range) where T : Mobile =>
            m_Map?.GetMobilesInRange<T>(m_Location, range) ?? Map.NullEnumerable<T>.Instance;

        public IPooledEnumerable<NetState> GetClientsInRange(int range) =>
            m_Map?.GetClientsInRange(m_Location, range) ?? Map.NullEnumerable<NetState>.Instance;

        public void SayTo(Mobile to, bool ascii, string text) =>
            PrivateOverheadMessage(MessageType.Regular, SpeechHue, ascii, text, to.NetState);

        public void SayTo(Mobile to, string text) => SayTo(to, false, text);

        public void SayTo(Mobile to, string format, params object[] args) => SayTo(to, false, string.Format(format, args));

        public void SayTo(Mobile to, bool ascii, string format, params object[] args) =>
            SayTo(to, ascii, string.Format(format, args));

        public void SayTo(Mobile to, int number, string args = "") =>
            to.NetState.SendMessageLocalized(Serial, Body, MessageType.Regular, SpeechHue, 3, number, Name, args);

        public void Say(bool ascii, string text) => PublicOverheadMessage(MessageType.Regular, SpeechHue, ascii, text);

        public void Say(string text) => PublicOverheadMessage(MessageType.Regular, SpeechHue, false, text);

        public void Say(string format, params object[] args) => Say(string.Format(format, args));

        public void Say(int number, AffixType type, string affix, string args) =>
            PublicOverheadMessage(MessageType.Regular, SpeechHue, number, type, affix, args);

        public void Say(int number, string args = "") => PublicOverheadMessage(MessageType.Regular, SpeechHue, number, args);

        public void Emote(string text) => PublicOverheadMessage(MessageType.Emote, EmoteHue, false, text);

        public void Emote(string format, params object[] args) => Emote(string.Format(format, args));

        public void Emote(int number, string args = "") => PublicOverheadMessage(MessageType.Emote, EmoteHue, number, args);

        public void Whisper(string text) => PublicOverheadMessage(MessageType.Whisper, WhisperHue, false, text);

        public void Whisper(string format, params object[] args) => Whisper(string.Format(format, args));

        public void Whisper(int number, string args = "") =>
            PublicOverheadMessage(MessageType.Whisper, WhisperHue, number, args);

        public void Yell(string text) => PublicOverheadMessage(MessageType.Yell, YellHue, false, text);

        public void Yell(string format, params object[] args) => Yell(string.Format(format, args));

        public void Yell(int number, string args = "") =>
            PublicOverheadMessage(MessageType.Yell, YellHue, number, args);

        public bool SendHuePicker(HuePicker p)
        {
            if (m_NetState != null)
            {
                p.SendTo(m_NetState);
                return true;
            }

            return false;
        }

        public Gump FindGump<T>() where T : Gump => m_NetState?.Gumps.Find(g => g is T);

        public bool CloseGump<T>() where T : Gump
        {
            if (m_NetState == null)
            {
                return false;
            }

            var gump = FindGump<T>();

            if (gump != null)
            {
                m_NetState.SendCloseGump(gump.TypeID, 0);
                m_NetState.RemoveGump(gump);
                gump.OnServerClose(m_NetState);
            }

            return true;
        }

        public bool CloseAllGumps()
        {
            var ns = m_NetState;

            if (ns.CannotSendPackets())
            {
                return false;
            }

            var gumps = new List<Gump>(ns.Gumps);

            ns.ClearGumps();

            foreach (var gump in gumps)
            {
                ns.SendCloseGump(gump.TypeID, 0);

                gump.OnServerClose(ns);
            }

            return true;
        }

        public bool HasGump<T>() where T : Gump => FindGump<T>() != null;

        public bool SendGump(Gump g)
        {
            if (m_NetState == null)
            {
                return false;
            }

            g.SendTo(m_NetState);
            return true;
        }

        public bool SendMenu(IMenu m)
        {
            if (m_NetState == null)
            {
                return false;
            }

            m.SendTo(m_NetState);
            return true;
        }

        public virtual bool CanBeBeneficial(Mobile target) => CanBeBeneficial(target, true, false);

        public virtual bool CanBeBeneficial(Mobile target, bool message) => CanBeBeneficial(target, message, false);

        public virtual bool CanBeBeneficial(Mobile target, bool message, bool allowDead)
        {
            if (target == null)
            {
                return false;
            }

            if (Deleted || target.Deleted || !Alive || IsDeadBondedPet ||
                !allowDead && (!target.Alive || target.IsDeadBondedPet))
            {
                if (message)
                {
                    SendLocalizedMessage(1001017); // You can not perform beneficial acts on your target.
                }

                return false;
            }

            if (target == this)
            {
                return true;
            }

            if ( /*m_Player &&*/!Region.AllowBeneficial(this, target))
            {
                // TODO: Pets
                // if (!(target.m_Player || target.Body.IsHuman || target.Body.IsAnimal))
                // {
                if (message)
                {
                    SendLocalizedMessage(1001017); // You can not perform beneficial acts on your target.
                }

                return false;
                // }
            }

            return true;
        }

        public virtual bool IsBeneficialCriminal(Mobile target)
        {
            if (this == target)
            {
                return false;
            }

            var n = Notoriety.Compute(this, target);

            return n is Notoriety.Criminal or Notoriety.Murderer;
        }

        /// <summary>
        ///     Overridable. Event invoked when the Mobile <see cref="DoBeneficial">does a beneficial action</see>.
        /// </summary>
        public virtual void OnBeneficialAction(Mobile target, bool isCriminal)
        {
            if (isCriminal)
            {
                CriminalAction(false);
            }
        }

        public virtual void DoBeneficial(Mobile target)
        {
            if (target == null)
            {
                return;
            }

            OnBeneficialAction(target, IsBeneficialCriminal(target));

            Region.OnBeneficialAction(this, target);
            target.Region.OnGotBeneficialAction(this, target);
        }

        public virtual bool BeneficialCheck(Mobile target)
        {
            if (CanBeBeneficial(target, true))
            {
                DoBeneficial(target);
                return true;
            }

            return false;
        }

        public virtual bool CanBeHarmful(Mobile target) => CanBeHarmful(target, true);

        public virtual bool CanBeHarmful(Mobile target, bool message) => CanBeHarmful(target, message, false);

        public virtual bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            if (target == null)
            {
                return false;
            }

            if (Deleted || !ignoreOurBlessedness && m_Blessed || target.Deleted || target.m_Blessed || !Alive ||
                IsDeadBondedPet || !target.Alive || target.IsDeadBondedPet)
            {
                if (message)
                {
                    SendLocalizedMessage(1001018); // You can not perform negative acts on your target.
                }

                return false;
            }

            if (target == this)
            {
                return true;
            }

            // TODO: Pets
            if (!Region.AllowHarmful(this, target))
            {
                if (message)
                {
                    SendLocalizedMessage(1001018); // You can not perform negative acts on your target.
                }

                return false;
            }

            return true;
        }

        public virtual bool IsHarmfulCriminal(Mobile target) =>
            this != target && Notoriety.Compute(this, target) == Notoriety.Innocent;

        /// <summary>
        ///     Overridable. Event invoked when the Mobile <see cref="DoHarmful">does a harmful action</see>.
        /// </summary>
        public virtual void OnHarmfulAction(Mobile target, bool isCriminal)
        {
            if (isCriminal)
            {
                CriminalAction(false);
            }
        }

        public virtual void DoHarmful(Mobile target, bool indirect = false)
        {
            if (target == null || Deleted)
            {
                return;
            }

            var isCriminal = IsHarmfulCriminal(target);

            OnHarmfulAction(target, isCriminal);
            target.AggressiveAction(this, isCriminal);

            Region.OnDidHarmful(this, target);
            target.Region.OnGotHarmful(this, target);

            if (!indirect)
            {
                Combatant = target;
            }

            _expireCombatantTimerToken.Cancel();
            Timer.StartTimer(ExpireCombatantDelay, ExpireCombatant, out _expireCombatantTimerToken);
        }

        public virtual bool HarmfulCheck(Mobile target)
        {
            if (CanBeHarmful(target))
            {
                DoHarmful(target);
                return true;
            }

            return false;
        }

        public bool RemoveStatMod(string name)
        {
            StatMods ??= new List<StatMod>();

            for (var i = 0; i < StatMods.Count; ++i)
            {
                var check = StatMods[i];

                if (check.Name == name)
                {
                    StatMods.RemoveAt(i);
                    CheckStatTimers();
                    Delta(MobileDelta.Stat | GetStatDelta(check.Type));
                    return true;
                }
            }

            return false;
        }

        public StatMod GetStatMod(string name)
        {
            StatMods ??= new List<StatMod>();

            for (var i = 0; i < StatMods.Count; ++i)
            {
                var check = StatMods[i];

                if (check.Name == name)
                {
                    return check;
                }
            }

            return null;
        }

        public void AddStatMod(StatMod mod)
        {
            StatMods ??= new List<StatMod>();

            for (var i = 0; i < StatMods.Count; ++i)
            {
                var check = StatMods[i];

                if (check.Name == mod.Name)
                {
                    Delta(MobileDelta.Stat | GetStatDelta(check.Type));
                    StatMods.RemoveAt(i);
                    break;
                }
            }

            StatMods.Add(mod);
            Delta(MobileDelta.Stat | GetStatDelta(mod.Type));
            CheckStatTimers();
        }

        private static MobileDelta GetStatDelta(StatType type)
        {
            MobileDelta delta = 0;

            if ((type & StatType.Str) != 0)
            {
                delta |= MobileDelta.Hits;
            }

            if ((type & StatType.Dex) != 0)
            {
                delta |= MobileDelta.Stam;
            }

            if ((type & StatType.Int) != 0)
            {
                delta |= MobileDelta.Mana;
            }

            return delta;
        }

        /// <summary>
        ///     Computes the total modified offset for the specified stat type. Expired <see cref="StatMod" /> instances are removed.
        /// </summary>
        public int GetStatOffset(StatType type)
        {
            var offset = 0;

            StatMods ??= new List<StatMod>();

            for (var i = 0; i < StatMods.Count; ++i)
            {
                var mod = StatMods[i];

                if (mod.HasElapsed())
                {
                    StatMods.RemoveAt(i);
                    Delta(MobileDelta.Stat | GetStatDelta(mod.Type));
                    CheckStatTimers();

                    --i;
                }
                else if ((mod.Type & type) != 0)
                {
                    offset += mod.Offset;
                }
            }

            return offset;
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the <see cref="RawStr" /> changes.
        ///     <seealso cref="RawStr" />
        ///     <seealso cref="OnRawStatChange" />
        /// </summary>
        public virtual void OnRawStrChange(int oldValue)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when <see cref="RawDex" /> changes.
        ///     <seealso cref="RawDex" />
        ///     <seealso cref="OnRawStatChange" />
        /// </summary>
        public virtual void OnRawDexChange(int oldValue)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the <see cref="RawInt" /> changes.
        ///     <seealso cref="RawInt" />
        ///     <seealso cref="OnRawStatChange" />
        /// </summary>
        public virtual void OnRawIntChange(int oldValue)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the <see cref="RawStr" />, <see cref="RawDex" />, or <see cref="RawInt" />
        ///     changes.
        ///     <seealso cref="OnRawStrChange" />
        ///     <seealso cref="OnRawDexChange" />
        ///     <seealso cref="OnRawIntChange" />
        /// </summary>
        public virtual void OnRawStatChange(StatType stat, int oldValue)
        {
        }

        public virtual void OnHitsChange(int oldValue)
        {
        }

        public virtual void OnStamChange(int oldValue)
        {
        }

        public virtual void OnManaChange(int oldValue)
        {
        }

        /// <summary>
        ///     Overridable. Event invoked when a call to <see cref="ApplyPoison" /> failed because <see cref="CheckPoisonImmunity" />
        ///     returned false: the Mobile was resistant to the poison. By default, this broadcasts an overhead message: * The poison
        ///     seems to have no effect. *
        ///     <seealso cref="CheckPoisonImmunity" />
        ///     <seealso cref="ApplyPoison" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual void OnPoisonImmunity(Mobile from, Poison poison)
        {
            PublicOverheadMessage(MessageType.Emote, 0x3B2, 1005534); // * The poison seems to have no effect. *
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when a call to <see cref="ApplyPoison" /> failed because
        ///     <see cref="CheckHigherPoison" /> returned false: the Mobile was already poisoned by an equal or greater strength poison.
        ///     <seealso cref="CheckHigherPoison" />
        ///     <seealso cref="ApplyPoison" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual void OnHigherPoison(Mobile from, Poison poison)
        {
        }

        /// <summary>
        ///     Overridable. Event invoked when a call to <see cref="ApplyPoison" /> succeeded. By default, this broadcasts an overhead
        ///     message varying by the level of the poison. Example: * Zippy begins to spasm uncontrollably. *
        ///     <seealso cref="ApplyPoison" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual void OnPoisoned(Mobile from, Poison poison, Poison oldPoison)
        {
            if (poison != null)
            {
                LocalOverheadMessage(MessageType.Regular, 0x21, 1042857 + poison.Level * 2);
                NonlocalOverheadMessage(MessageType.Regular, 0x21, 1042858 + poison.Level * 2, Name);
            }
        }

        /// <summary>
        ///     Overridable. Called from <see cref="ApplyPoison" />, this method checks if the Mobile is immune to some
        ///     <see cref="Poison" />. If true, <see cref="OnPoisonImmunity" /> will be invoked and
        ///     <see cref="ApplyPoisonResult.Immune" /> is returned.
        ///     <seealso cref="OnPoisonImmunity" />
        ///     <seealso cref="ApplyPoison" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual bool CheckPoisonImmunity(Mobile from, Poison poison) => false;

        /// <summary>
        ///     Overridable. Called from <see cref="ApplyPoison" />, this method checks if the Mobile is already poisoned by some
        ///     <see cref="Poison" /> of equal or greater strength. If true, <see cref="OnHigherPoison" /> will be invoked and
        ///     <see cref="ApplyPoisonResult.HigherPoisonActive" /> is returned.
        ///     <seealso cref="OnHigherPoison" />
        ///     <seealso cref="ApplyPoison" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual bool CheckHigherPoison(Mobile from, Poison poison) =>
            m_Poison != null && m_Poison.Level >= poison.Level;

        /// <summary>
        ///     Overridable. Attempts to apply poison to the Mobile. Checks are made such that no
        ///     <see cref="CheckHigherPoison">higher poison is active</see> and that the Mobile is not
        ///     <see cref="CheckPoisonImmunity">immune to the poison</see>. Provided those assertions are true, the
        ///     <paramref name="poison" /> is applied and <see cref="OnPoisoned" /> is invoked.
        ///     <seealso cref="Poison" />
        ///     <seealso cref="CurePoison" />
        /// </summary>
        /// <returns>
        ///     One of four possible values:
        ///     <list type="table">
        ///         <item>
        ///             <term>
        ///                 <see cref="ApplyPoisonResult.Cured">Cured</see>
        ///             </term>
        ///             <description>The <paramref name="poison" /> parameter was null and so <see cref="CurePoison" /> was invoked.</description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ApplyPoisonResult.HigherPoisonActive">HigherPoisonActive</see>
        ///             </term>
        ///             <description>The call to <see cref="CheckHigherPoison" /> returned false.</description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ApplyPoisonResult.Immune">Immune</see>
        ///             </term>
        ///             <description>The call to <see cref="CheckPoisonImmunity" /> returned false.</description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ApplyPoisonResult.Poisoned">Poisoned</see>
        ///             </term>
        ///             <description>The <paramref name="poison" /> was successfully applied.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public virtual ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (poison == null)
            {
                CurePoison(from);
                return ApplyPoisonResult.Cured;
            }

            if (CheckHigherPoison(from, poison))
            {
                OnHigherPoison(from, poison);
                return ApplyPoisonResult.HigherPoisonActive;
            }

            if (CheckPoisonImmunity(from, poison))
            {
                OnPoisonImmunity(from, poison);
                return ApplyPoisonResult.Immune;
            }

            var oldPoison = m_Poison;
            Poison = poison;

            OnPoisoned(from, poison, oldPoison);

            return ApplyPoisonResult.Poisoned;
        }

        /// <summary>
        ///     Overridable. Called from <see cref="CurePoison" />, this method checks to see that the Mobile can be cured of
        ///     <see cref="Poison" />
        ///     <seealso cref="CurePoison" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual bool CheckCure(Mobile from) => true;

        /// <summary>
        ///     Overridable. Virtual event invoked when a call to <see cref="CurePoison" /> succeeded.
        ///     <seealso cref="CurePoison" />
        ///     <seealso cref="CheckCure" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual void OnCured(Mobile from, Poison oldPoison)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when a call to <see cref="CurePoison" /> failed.
        ///     <seealso cref="CurePoison" />
        ///     <seealso cref="CheckCure" />
        ///     <seealso cref="Poison" />
        /// </summary>
        public virtual void OnFailedCure(Mobile from)
        {
        }

        /// <summary>
        ///     Overridable. Attempts to cure any poison that is currently active.
        /// </summary>
        /// <returns>True if poison was cured, false if otherwise.</returns>
        public virtual bool CurePoison(Mobile from)
        {
            if (CheckCure(from))
            {
                var oldPoison = m_Poison;
                Poison = null;

                OnCured(from, oldPoison);

                return true;
            }

            OnFailedCure(from);

            return false;
        }

        public void MovingEffect(
            IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes,
            int hue, int renderMode
        ) =>
            Effects.SendMovingEffect(this, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode);

        public void MovingEffect(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes) =>
            Effects.SendMovingEffect(this, to, itemID, speed, duration, fixedDirection, explodes);

        public void MovingParticles(
            IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes,
            int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer, int unknown
        ) =>
            Effects.SendMovingParticles(
                this,
                to,
                itemID,
                speed,
                duration,
                fixedDirection,
                explodes,
                hue,
                renderMode,
                effect,
                explodeEffect,
                explodeSound,
                layer,
                unknown
            );

        public void MovingParticles(
            IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes,
            int hue, int renderMode, int effect, int explodeEffect, int explodeSound, int unknown
        ) =>
            Effects.SendMovingParticles(
                this,
                to,
                itemID,
                speed,
                duration,
                fixedDirection,
                explodes,
                hue,
                renderMode,
                effect,
                explodeEffect,
                explodeSound,
                (EffectLayer)255,
                unknown
            );

        public void MovingParticles(
            IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes,
            int effect, int explodeEffect, int explodeSound, int unknown
        ) =>
            Effects.SendMovingParticles(
                this,
                to,
                itemID,
                speed,
                duration,
                fixedDirection,
                explodes,
                effect,
                explodeEffect,
                explodeSound,
                unknown
            );

        public void MovingParticles(
            IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes,
            int effect, int explodeEffect, int explodeSound
        ) =>
            Effects.SendMovingParticles(
                this,
                to,
                itemID,
                speed,
                duration,
                fixedDirection,
                explodes,
                0,
                0,
                effect,
                explodeEffect,
                explodeSound,
                0
            );

        public void FixedEffect(int itemID, int speed, int duration, int hue, int renderMode) =>
            Effects.SendTargetEffect(this, itemID, speed, duration, hue, renderMode);

        public void FixedEffect(int itemID, int speed, int duration) =>
            Effects.SendTargetEffect(this, itemID, speed, duration);

        public void FixedParticles(
            int itemID, int speed, int duration, int effect, int hue, int renderMode, EffectLayer layer, int unknown
        ) =>
            Effects.SendTargetParticles(this, itemID, speed, duration, hue, renderMode, effect, layer, unknown);

        public void FixedParticles(
            int itemID, int speed, int duration, int effect, int hue, int renderMode, EffectLayer layer
        ) =>
            Effects.SendTargetParticles(this, itemID, speed, duration, hue, renderMode, effect, layer);

        public void FixedParticles(int itemID, int speed, int duration, int effect, EffectLayer layer, int unknown) =>
            Effects.SendTargetParticles(this, itemID, speed, duration, 0, 0, effect, layer, unknown);

        public void FixedParticles(int itemID, int speed, int duration, int effect, EffectLayer layer) =>
            Effects.SendTargetParticles(this, itemID, speed, duration, 0, 0, effect, layer);

        public void BoltEffect(int hue) => Effects.SendBoltEffect(this, true, hue);

        public Direction GetDirectionTo(int x, int y, bool run = false)
        {
            var dx = m_Location.m_X - x;
            var dy = m_Location.m_Y - y;

            var rx = (dx - dy) * 44;
            var ry = (dx + dy) * 44;

            var ax = rx.Abs();
            var ay = ry.Abs();

            Direction ret;

            if ((ay >> 1) - ax >= 0)
            {
                ret = ry > 0 ? Direction.Up : Direction.Down;
            }
            else if ((ax >> 1) - ay >= 0)
            {
                ret = rx > 0 ? Direction.Left : Direction.Right;
            }
            else
            {
                ret = rx switch
                {
                    >= 0 when ry >= 0 => Direction.West,
                    >= 0              => Direction.South,
                    < 0 when ry < 0   => Direction.East,
                    _                 => Direction.North
                };
            }

            return ret | (run ? Direction.Running : 0);
        }

        public Direction GetDirectionTo(Point2D p, bool run = false) => GetDirectionTo(p.m_X, p.m_Y, run);
        public Direction GetDirectionTo(Point3D p, bool run = false) => GetDirectionTo(p.m_X, p.m_Y, run);

        public Direction GetDirectionTo(IPoint2D p, bool run = false) =>
            p == null ? Direction.North | (run ? Direction.Running : 0) : GetDirectionTo(p.X, p.Y, run);

        public void PublicOverheadMessage(
            MessageType type, int hue, bool ascii, string text, bool noLineOfSight = true,
            AccessLevel accessLevel = AccessLevel.Player
        )
        {
            if (m_Map == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();

            var eable = m_Map.GetClientsInRange(m_Location);

            foreach (var state in eable)
            {
                if (
                    state.Mobile.AccessLevel >= accessLevel &&
                    state.Mobile.CanSee(this) &&
                    (noLineOfSight || state.Mobile.InLOS(this))
                )
                {
                    var length = OutgoingMessagePackets.CreateMessage(
                        buffer, Serial, Body, type, hue, 3, ascii, Language, Name, text
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, string args = "", bool noLineOfSight = true)
        {
            if (m_Map == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength(args)].InitializePacket();

            var eable = m_Map.GetClientsInRange(m_Location);

            foreach (var state in eable)
            {
                if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.InLOS(this)))
                {
                    var length = OutgoingMessagePackets.CreateMessageLocalized(
                        buffer, Serial, Body, type, hue, 3, number, Name, args
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public void PublicOverheadMessage(
            MessageType type, int hue, int number, AffixType affixType, string affix,
            string args = "", bool noLineOfSight = false,
            AccessLevel accessLevel = AccessLevel.Player
        )
        {
            if (m_Map == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedAffixLength(affix, args)].InitializePacket();

            var eable = m_Map.GetClientsInRange(m_Location);

            foreach (var state in eable)
            {
                if (
                    state.Mobile.AccessLevel >= accessLevel &&
                    state.Mobile.CanSee(this) &&
                    (noLineOfSight || state.Mobile.InLOS(this))
                )
                {
                    var length = OutgoingMessagePackets.CreateMessageLocalizedAffix(
                        buffer, Serial, Body, type, hue, 3, number, Name, affixType, affix, args
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public void PrivateOverheadMessage(MessageType type, int hue, bool ascii, string text, NetState state)
        {
            state.SendMessage(Serial, Body, type, hue, 3, ascii, m_Language, Name, text);
        }

        public void PrivateOverheadMessage(MessageType type, int hue, int number, NetState state) =>
            PrivateOverheadMessage(type, hue, number, "", state);

        public void PrivateOverheadMessage(MessageType type, int hue, int number, string args, NetState state) =>
            state.SendMessageLocalized(Serial, Body, type, hue, 3, number, Name, args);

        public void LocalOverheadMessage(MessageType type, int hue, bool ascii, string text) =>
            m_NetState.SendMessage(Serial, Body, type, hue, 3, ascii, m_Language, Name, text);

        public void LocalOverheadMessage(MessageType type, int hue, int number, string args = "") =>
            m_NetState.SendMessageLocalized(Serial, Body, type, hue, 3, number, Name, args);

        public void NonlocalOverheadMessage(MessageType type, int hue, int number, string args = "")
        {
            if (m_Map == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength(args)].InitializePacket();

            var eable = m_Map.GetClientsInRange(m_Location);

            foreach (var state in eable)
            {
                if (state != m_NetState && state.Mobile.CanSee(this))
                {
                    var length = OutgoingMessagePackets.CreateMessageLocalized(
                        buffer, Serial, Body, type, hue, 3, number, Name, args
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public void NonlocalOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            if (m_Map == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();

            var eable = m_Map.GetClientsInRange(m_Location);

            foreach (var state in eable)
            {
                if (state != m_NetState && state.Mobile.CanSee(this))
                {
                    var length = OutgoingMessagePackets.CreateMessage(
                        buffer, Serial, Body, type, hue, 3, ascii, Language, Name, text
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    state.Send(buffer);
                }
            }

            eable.Free();
        }

        public void SendLocalizedMessage(int number, string args = "", int hue = 0x3B2) =>
            m_NetState.SendMessageLocalized(Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", args);

        public void SendLocalizedMessage(int number, bool append, string affix, string args = "", int hue = 0x3B2) =>
            m_NetState.SendMessageLocalizedAffix(
                Serial.MinusOne,
                -1,
                MessageType.Regular,
                hue,
                3,
                number,
                "System",
                (append ? AffixType.Append : AffixType.Prepend) | AffixType.System,
                affix,
                args
            );

        public void SendMessage(string text) => SendMessage(0x3B2, text);

        public void SendMessage(string format, params object[] args) =>
            SendMessage(0x3B2, string.Format(format, args));

        public void SendMessage(int hue, string text) =>
            m_NetState.SendMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, false, "ENU", "System", text);

        public void SendMessage(int hue, string format, params object[] args) =>
            SendMessage(hue, string.Format(format, args));

        public void SendAsciiMessage(string text) => SendAsciiMessage(0x3B2, text);

        public void SendAsciiMessage(string format, params object[] args) =>
            SendAsciiMessage(0x3B2, string.Format(format, args));

        public void SendAsciiMessage(int hue, string text) =>
            m_NetState.SendMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, true, null, "System", text);

        public void SendAsciiMessage(int hue, string format, params object[] args) =>
            SendAsciiMessage(hue, string.Format(format, args));

        /// <summary>
        ///     Overridable. Event invoked when the Mobile is double clicked. By default, this method can either dismount or open the
        ///     paperdoll.
        ///     <seealso cref="CanPaperdollBeOpenedBy" />
        ///     <seealso cref="DisplayPaperdollTo" />
        /// </summary>
        public virtual void OnDoubleClick(Mobile from)
        {
            if (this == from && (!DisableDismountInWarmode || !m_Warmode))
            {
                var mount = Mount;

                if (mount != null)
                {
                    mount.Rider = null;
                    return;
                }
            }

            if (CanPaperdollBeOpenedBy(from))
            {
                DisplayPaperdollTo(from);
            }
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile is double clicked by someone who is over 18 tiles away.
        ///     <seealso cref="OnDoubleClick" />
        /// </summary>
        public virtual void OnDoubleClickOutOfRange(Mobile from)
        {
        }

        /// <summary>
        ///     Overridable. Virtual event invoked when the Mobile is double clicked by someone who can no longer see the Mobile. This
        ///     may
        ///     happen, for example, using 'Last Object' after the Mobile has hidden.
        ///     <seealso cref="OnDoubleClick" />
        /// </summary>
        public virtual void OnDoubleClickCantSee(Mobile from)
        {
        }

        /// <summary>
        ///     Overridable. Event invoked when the Mobile is double clicked by someone who is not alive. Similar to
        ///     <see cref="OnDoubleClick" />, this method will show the paperdoll. It does not, however, provide any dismount
        ///     functionality.
        ///     <seealso cref="OnDoubleClick" />
        /// </summary>
        public virtual void OnDoubleClickDead(Mobile from)
        {
            if (CanPaperdollBeOpenedBy(from))
            {
                DisplayPaperdollTo(from);
            }
        }

        private class ManaTimer : Timer
        {
            private readonly Mobile m_Owner;

            public ManaTimer(Mobile m) : base(GetManaRegenRate(m), GetManaRegenRate(m)) => m_Owner = m;

            protected override void OnTick()
            {
                if (m_Owner.CanRegenMana)
                {
                    m_Owner.Mana++;
                }

                Delay = Interval = GetManaRegenRate(m_Owner);
            }
        }

        private class HitsTimer : Timer
        {
            private readonly Mobile m_Owner;

            public HitsTimer(Mobile m) : base(GetHitsRegenRate(m), GetHitsRegenRate(m)) => m_Owner = m;

            protected override void OnTick()
            {
                if (m_Owner.CanRegenHits)
                {
                    m_Owner.Hits++;
                }

                Delay = Interval = GetHitsRegenRate(m_Owner);
            }
        }

        private class StamTimer : Timer
        {
            private readonly Mobile m_Owner;

            public StamTimer(Mobile m) : base(GetStamRegenRate(m), GetStamRegenRate(m)) => m_Owner = m;

            protected override void OnTick()
            {
                if (m_Owner.CanRegenStam)
                {
                    m_Owner.Stam++;
                }

                Delay = Interval = GetStamRegenRate(m_Owner);
            }
        }

        private void Logout()
        {
            if (m_Map != Map.Internal)
            {
                EventSink.InvokeLogout(this);

                LogoutLocation = m_Location;
                LogoutMap = m_Map;

                Internalize();
            }
        }

        private void ExpireCriminal()
        {
            Criminal = false;
        }
    }
}
