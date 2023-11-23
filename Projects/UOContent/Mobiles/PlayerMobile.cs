using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.BulkOrders;
using Server.Engines.CannedEvil;
using Server.Engines.ConPVP;
using Server.Engines.Craft;
using Server.Engines.Help;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Gumps;
using Server.Engines.PartySystem;
using Server.Engines.PlayerMurderSystem;
using Server.Engines.Quests;
using Server.Engines.Virtues;
using Server.Ethics;
using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Movement;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Fifth;
using Server.Spells.Fourth;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Spellweaving;
using Server.Targeting;
using Server.Utilities;
using BaseQuestGump = Server.Engines.MLQuests.Gumps.BaseQuestGump;
using CalcMoves = Server.Movement.Movement;
using QuestOfferGump = Server.Engines.MLQuests.Gumps.QuestOfferGump;
using RankDefinition = Server.Guilds.RankDefinition;

namespace Server.Mobiles
{
    [Flags]
    public enum PlayerFlag // First 16 bits are reserved for default-distro use, start custom flags at 0x00010000
    {
        None = 0x00000000,
        Glassblowing = 0x00000001,
        Masonry = 0x00000002,
        SandMining = 0x00000004,
        StoneMining = 0x00000008,
        ToggleMiningStone = 0x00000010,
        KarmaLocked = 0x00000020,
        AutoRenewInsurance = 0x00000040,
        UseOwnFilter = 0x00000080,
        PagingSquelched = 0x00000200,
        Young = 0x00000400,
        AcceptGuildInvites = 0x00000800,
        DisplayChampionTitle = 0x00001000,
        HasStatReward = 0x00002000,
        RefuseTrades = 0x00004000
    }

    public enum NpcGuild
    {
        None,
        MagesGuild,
        WarriorsGuild,
        ThievesGuild,
        RangersGuild,
        HealersGuild,
        MinersGuild,
        MerchantsGuild,
        TinkersGuild,
        TailorsGuild,
        FishermensGuild,
        BardsGuild,
        BlacksmithsGuild
    }

    public enum SolenFriendship
    {
        None,
        Red,
        Black
    }

    public enum BlockMountType
    {
        None = -1,
        Dazed = 1040024, // You are still too dazed from being knocked off your mount to ride!
        BolaRecovery = 1062910, // You cannot mount while recovering from a bola throw.
        DismountRecovery = 1070859 // You cannot mount while recovering from a dismount special maneuver.
    }

    public class PlayerMobile : Mobile, IHonorTarget, IHasSteps
    {
        private static bool m_NoRecursion;

        private static readonly Point3D[] m_TrammelDeathDestinations =
        {
            new(1481, 1612, 20),
            new(2708, 2153, 0),
            new(2249, 1230, 0),
            new(5197, 3994, 37),
            new(1412, 3793, 0),
            new(3688, 2232, 20),
            new(2578, 604, 0),
            new(4397, 1089, 0),
            new(5741, 3218, -2),
            new(2996, 3441, 15),
            new(624, 2225, 0),
            new(1916, 2814, 0),
            new(2929, 854, 0),
            new(545, 967, 0),
            new(3665, 2587, 0)
        };

        private static readonly Point3D[] m_IlshenarDeathDestinations =
        {
            new(1216, 468, -13),
            new(723, 1367, -60),
            new(745, 725, -28),
            new(281, 1017, 0),
            new(986, 1011, -32),
            new(1175, 1287, -30),
            new(1533, 1341, -3),
            new(529, 217, -44),
            new(1722, 219, 96)
        };

        private static readonly Point3D[] m_MalasDeathDestinations =
        {
            new(2079, 1376, -70),
            new(944, 519, -71)
        };

        private static readonly Point3D[] m_TokunoDeathDestinations =
        {
            new(1166, 801, 27),
            new(782, 1228, 25),
            new(268, 624, 15)
        };

        private HashSet<int> _acquiredRecipes;

        private HashSet<Mobile> _allFollowers;
        private int m_BeardModID = -1, m_BeardModHue;

        // TODO: Pool BuffInfo objects
        private Dictionary<BuffIcon, BuffInfo> m_BuffTable;

        private DuelPlayer m_DuelPlayer;

        private Type m_EnemyOfOneType;
        private TimeSpan m_GameTime;

        /*
         * a value of zero means, that the mobile is not executing the spell. Otherwise,
         * the value should match the BaseMana required
        */

        private RankDefinition m_GuildRank;

        private int m_HairModID = -1, m_HairModHue;

        public DateTime _honorTime;

        private Mobile m_InsuranceAward;
        private int m_InsuranceBonus;

        private int m_LastGlobalLight = -1, m_LastPersonalLight = -1;

        private bool m_LastProtectedMessage;

        private DateTime m_LastYoungHeal = DateTime.MinValue;

        private DateTime m_LastYoungMessage = DateTime.MinValue;

        private MountBlock _mountBlock;

        private DateTime m_NextJustAward;

        private int m_NextProtectionCheck = 10;
        private DateTime m_NextSmithBulkOrder;
        private DateTime m_NextTailorBulkOrder;

        private bool m_NoDeltaRecursion;

        // number of items that could not be automatically reinsured because gold in bank was not enough
        private int m_NonAutoreinsuredItems;

        private DateTime m_SavagePaintExpiration;

        private DateTime[] m_StuckMenuUses;

        private QuestArrow m_QuestArrow;

        public PlayerMobile()
        {
            VisibilityList = new List<Mobile>();
            PermaFlags = new List<Mobile>();

            BOBFilter = new BOBFilter();

            m_GameTime = TimeSpan.Zero;
            m_GuildRank = RankDefinition.Lowest;
        }

        public PlayerMobile(Serial s) : base(s)
        {
            VisibilityList = new List<Mobile>();
        }

        public int StepsMax => 16;

        public int StepsGainedPerIdleTime => 1;

        public TimeSpan IdleTimePerStepsGain => TimeSpan.FromSeconds(1);

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime AnkhNextUse { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan DisguiseTimeLeft => DisguisePersistence.TimeRemaining(this);

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime PeacedUntil { get; set; }

        public DesignContext DesignContext { get; set; }

        public BlockMountType MountBlockReason => _mountBlock?.MountBlockReason ?? BlockMountType.None;

        public override int MaxWeight => (Core.ML && Race == Race.Human ? 100 : 40) + (int)(3.5 * Str);

        public override double ArmorRating
        {
            get
            {
                // BaseArmor ar;
                var rating = 0.0;

                AddArmorRating(ref rating, NeckArmor);
                AddArmorRating(ref rating, HandArmor);
                AddArmorRating(ref rating, HeadArmor);
                AddArmorRating(ref rating, ArmsArmor);
                AddArmorRating(ref rating, LegsArmor);
                AddArmorRating(ref rating, ChestArmor);
                AddArmorRating(ref rating, ShieldArmor);

                return VirtualArmor + VirtualArmorMod + rating;
            }
        }

        public SkillName[] AnimalFormRestrictedSkills { get; } =
        {
            SkillName.ArmsLore, SkillName.Begging, SkillName.Discordance, SkillName.Forensics,
            SkillName.Inscribe, SkillName.ItemID, SkillName.Meditation, SkillName.Peacemaking,
            SkillName.Provocation, SkillName.RemoveTrap, SkillName.SpiritSpeak, SkillName.Stealing,
            SkillName.TasteID
        };

        public override double RacialSkillBonus
        {
            get
            {
                if (Core.ML && Race == Race.Human)
                {
                    return 20.0;
                }

                return 0;
            }
        }

        public List<Item> EquipSnapshot { get; private set; }

        public SkillName Learning { get; set; } = (SkillName)(-1);

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan SavagePaintExpiration
        {
            get => Utility.Max(m_SavagePaintExpiration - Core.Now, TimeSpan.Zero);
            set => m_SavagePaintExpiration = Core.Now + value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSmithBulkOrder
        {
            get => Utility.Max(m_NextSmithBulkOrder - Core.Now, TimeSpan.Zero);
            set
            {
                try
                {
                    m_NextSmithBulkOrder = Core.Now + value;
                }
                catch
                {
                    // ignored
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextTailorBulkOrder
        {
            get => Utility.Max(m_NextTailorBulkOrder - Core.Now, TimeSpan.Zero);
            set
            {
                try
                {
                    m_NextTailorBulkOrder = Core.Now + value;
                }
                catch
                {
                    // ignored
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastEscortTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastPetBallTime { get; set; }

        public List<Mobile> VisibilityList { get; }

        public List<Mobile> PermaFlags { get; private set; }

        public override int Luck => AosAttributes.GetValue(this, AosAttribute.Luck);

        public BOBFilter BOBFilter { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SessionStart { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan GameTime
        {
            get
            {
                if (NetState != null)
                {
                    return m_GameTime + (Core.Now - SessionStart);
                }

                return m_GameTime;
            }
        }

        public override bool NewGuildDisplay => Guilds.Guild.NewGuildSystem;

        public bool BedrollLogout { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Paralyzed
        {
            get => base.Paralyzed;
            set
            {
                base.Paralyzed = value;

                if (value)
                {
                    AddBuff(new BuffInfo(BuffIcon.Paralyze, 1075827)); // Paralyze/You are frozen and can not move
                }
                else
                {
                    RemoveBuff(BuffIcon.Paralyze);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Player EthicPlayer { get; set; }

        public PlayerState FactionPlayerState { get; set; }

        // WARNING - This can be null!!
        public HashSet<Mobile> Stabled { get; private set; }

        // WARNING - This can be null!!
        public HashSet<Mobile> AutoStabled { get; private set; }

        public bool NinjaWepCooldown { get; set; }

        // WARNING - This can be null!!
        public HashSet<Mobile> AllFollowers => _allFollowers;

        public RankDefinition GuildRank
        {
            get => AccessLevel >= AccessLevel.GameMaster ? RankDefinition.Leader : m_GuildRank;
            set => m_GuildRank = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GuildMessageHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AllianceMessageHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Profession { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsStealthing // IsStealthing should be moved to Server.Mobiles
        {
            get;
            set;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public NpcGuild NpcGuild { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NpcGuildJoinTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextBODTurnInTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastOnline { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public long LastMoved => LastMoveTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NpcGuildGameTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ToTItemsTurnedIn { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ToTTotalMonsterFame { get; set; }

        public int ExecutesLightningStrike { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ToothAche
        {
            get => CandyCane.GetToothAche(this);
            set => CandyCane.SetToothAche(this, value);
        }

        public PlayerFlag Flags { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PagingSquelched
        {
            get => GetFlag(PlayerFlag.PagingSquelched);
            set => SetFlag(PlayerFlag.PagingSquelched, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Glassblowing
        {
            get => GetFlag(PlayerFlag.Glassblowing);
            set => SetFlag(PlayerFlag.Glassblowing, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Masonry
        {
            get => GetFlag(PlayerFlag.Masonry);
            set => SetFlag(PlayerFlag.Masonry, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SandMining
        {
            get => GetFlag(PlayerFlag.SandMining);
            set => SetFlag(PlayerFlag.SandMining, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StoneMining
        {
            get => GetFlag(PlayerFlag.StoneMining);
            set => SetFlag(PlayerFlag.StoneMining, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ToggleMiningStone
        {
            get => GetFlag(PlayerFlag.ToggleMiningStone);
            set => SetFlag(PlayerFlag.ToggleMiningStone, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool KarmaLocked
        {
            get => GetFlag(PlayerFlag.KarmaLocked);
            set => SetFlag(PlayerFlag.KarmaLocked, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoRenewInsurance
        {
            get => GetFlag(PlayerFlag.AutoRenewInsurance);
            set => SetFlag(PlayerFlag.AutoRenewInsurance, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseOwnFilter
        {
            get => GetFlag(PlayerFlag.UseOwnFilter);
            set => SetFlag(PlayerFlag.UseOwnFilter, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AcceptGuildInvites
        {
            get => GetFlag(PlayerFlag.AcceptGuildInvites);
            set => SetFlag(PlayerFlag.AcceptGuildInvites, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasStatReward
        {
            get => GetFlag(PlayerFlag.HasStatReward);
            set => SetFlag(PlayerFlag.HasStatReward, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RefuseTrades
        {
            get => GetFlag(PlayerFlag.RefuseTrades);
            set => SetFlag(PlayerFlag.RefuseTrades, value);
        }

        public Dictionary<Type, int> RecoverableAmmo { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime AcceleratedStart { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName AcceleratedSkill { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax
        {
            get
            {
                int strBase;
                var strOffs = GetStatOffset(StatType.Str);

                if (Core.AOS)
                {
                    strBase = Str; // this.Str already includes GetStatOffset/str
                    strOffs = AosAttributes.GetValue(this, AosAttribute.BonusHits);

                    if (Core.ML && strOffs > 25 && AccessLevel <= AccessLevel.Player)
                    {
                        strOffs = 25;
                    }

                    if (AnimalForm.UnderTransformation(this, typeof(BakeKitsune)) ||
                        AnimalForm.UnderTransformation(this, typeof(GreyWolf)))
                    {
                        strOffs += 20;
                    }
                }
                else
                {
                    strBase = RawStr;
                }

                return strBase / 2 + 50 + strOffs;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int StamMax => base.StamMax + AosAttributes.GetValue(this, AosAttribute.BonusStam);

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ManaMax => base.ManaMax + AosAttributes.GetValue(this, AosAttribute.BonusMana) +
                                       (Core.ML && Race == Race.Elf ? 20 : 0);

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Str
        {
            get
            {
                if (Core.ML && AccessLevel == AccessLevel.Player)
                {
                    return Math.Min(base.Str, 150);
                }

                return base.Str;
            }
            set => base.Str = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Int
        {
            get
            {
                if (Core.ML && AccessLevel == AccessLevel.Player)
                {
                    return Math.Min(base.Int, 150);
                }

                return base.Int;
            }
            set => base.Int = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Dex
        {
            get
            {
                if (Core.ML && AccessLevel == AccessLevel.Player)
                {
                    return Math.Min(base.Dex, 150);
                }

                return base.Dex;
            }
            set => base.Dex = value;
        }

        public DuelContext DuelContext { get; private set; }

        public DuelPlayer DuelPlayer
        {
            get => m_DuelPlayer;
            set
            {
                var wasInTourney = DuelContext?.Finished == false && DuelContext.m_Tournament != null;

                m_DuelPlayer = value;

                DuelContext = m_DuelPlayer?.Participant.Context;

                var isInTourney = DuelContext?.Finished == false && DuelContext.m_Tournament != null;

                if (wasInTourney != isInTourney)
                {
                    SendEverything();
                }
            }
        }

        public QuestSystem Quest { get; set; }

        public List<QuestRestartInfo> DoneQuests { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SolenFriendship SolenFriendship { get; set; }

        public Type EnemyOfOneType
        {
            get => m_EnemyOfOneType;
            set
            {
                var oldType = m_EnemyOfOneType;
                var newType = value;

                if (oldType == newType)
                {
                    return;
                }

                m_EnemyOfOneType = value;

                DeltaEnemies(oldType, newType);
            }
        }

        public bool WaitingForEnemy { get; set; }

        public HonorContext SentHonorContext { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Young
        {
            get => GetFlag(PlayerFlag.Young);
            set
            {
                SetFlag(PlayerFlag.Young, value);
                InvalidateProperties();
            }
        }

        public SpeechLog SpeechLog { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DisplayChampionTitle
        {
            get => GetFlag(PlayerFlag.DisplayChampionTitle);
            set => SetFlag(PlayerFlag.DisplayChampionTitle, value);
        }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public ChampionTitleContext ChampionTitles => ChampionTitleSystem.GetOrCreateChampionTitleContext(this);

        [CommandProperty(AccessLevel.GameMaster)]
        public int ShortTermMurders
        {
            get => PlayerMurderSystem.GetMurderContext(this, out var context) ? context.ShortTermMurders : 0;
            set => PlayerMurderSystem.ManuallySetShortTermMurders(this, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ShortTermMurderExpiration
            => PlayerMurderSystem.GetMurderContext(this, out var context) && context.ShortTermMurders > 0
                ? Core.Now + (context.ShortTermElapse - GameTime)
                : DateTime.MinValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LongTermMurderExpiration
            => Kills > 0 && PlayerMurderSystem.GetMurderContext(this, out var context)
                ? Core.Now + (context.LongTermElapse - GameTime)
                : DateTime.MinValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public int KnownRecipes => _acquiredRecipes?.Count ?? 0;

        [CommandProperty(AccessLevel.Counselor, canModify: true)]
        public VirtueContext Virtues => VirtueSystem.GetOrCreateVirtues(this);

        public HonorContext ReceivedHonorContext { get; set; }

        public QuestArrow QuestArrow
        {
            get => m_QuestArrow;
            set
            {
                if (m_QuestArrow != value)
                {
                    m_QuestArrow?.Stop();

                    m_QuestArrow = value;
                }
            }
        }

        public void ClearQuestArrow() => m_QuestArrow = null;

        public override void ToggleFlying()
        {
            if (Race != Race.Gargoyle)
            {
                return;
            }

            if (Flying)
            {
                Freeze(TimeSpan.FromSeconds(1));
                Animate(61, 10, 1, true, false, 0);
                Flying = false;
                BuffInfo.RemoveBuff(this, BuffIcon.Fly);
                SendMessage("You have landed.");

                BaseMount.Dismount(this);
                return;
            }

            var type = MountBlockReason;

            if (!Alive)
            {
                SendLocalizedMessage(1113082); // You may not fly while dead.
            }
            else if (IsBodyMod && !(BodyMod == 666 || BodyMod == 667))
            {
                SendLocalizedMessage(1112453); // You can't fly in your current form!
            }
            else if (type != BlockMountType.None)
            {
                switch (type)
                {
                    case BlockMountType.Dazed:
                        {
                            SendLocalizedMessage(1112457); // You are still too dazed to fly.
                            break;
                        }
                    case BlockMountType.BolaRecovery:
                        {
                            SendLocalizedMessage(1112455); // You cannot fly while recovering from a bola throw.
                            break;
                        }
                    case BlockMountType.DismountRecovery:
                        {
                            // You cannot fly while recovering from a dismount maneuver.
                            SendLocalizedMessage(1112456);
                            break;
                        }
                }
            }
            else if (Hits < 25) // TODO confirm
            {
                SendLocalizedMessage(1112454); // You must heal before flying.
            }
            else
            {
                if (!Flying)
                {
                    // No message?
                    if (Spell is FlySpell spell)
                    {
                        spell.Stop();
                    }

                    new FlySpell(this).Cast();
                }
                else
                {
                    Flying = false;
                    BuffInfo.RemoveBuff(this, BuffIcon.Fly);
                }
            }
        }

        public static Direction GetDirection4(Point3D from, Point3D to)
        {
            var dx = from.X - to.X;
            var dy = from.Y - to.Y;

            var rx = dx - dy;
            var ry = dx + dy;

            Direction ret;

            if (rx >= 0 && ry >= 0)
            {
                ret = Direction.West;
            }
            else if (rx >= 0 && ry < 0)
            {
                ret = Direction.South;
            }
            else if (rx < 0 && ry < 0)
            {
                ret = Direction.East;
            }
            else
            {
                ret = Direction.North;
            }

            return ret;
        }

        public override bool OnDroppedItemToWorld(Item item, Point3D location)
        {
            if (!base.OnDroppedItemToWorld(item, location))
            {
                return false;
            }

            if (Core.AOS)
            {
                foreach (Mobile m in Map.GetMobilesAt(location))
                {
                    if (m.Z >= location.Z && m.Z < location.Z + 16 && (!m.Hidden || m.AccessLevel == AccessLevel.Player))
                    {
                        return false;
                    }
                }
            }

            var bi = item.GetBounce();

            if (bi == null)
            {
                return true;
            }

            var type = item.GetType();

            if (type.IsDefined(typeof(FurnitureAttribute), true) ||
                type.IsDefined(typeof(DynamicFlippingAttribute), true))
            {
                var objs = type.GetCustomAttributes(typeof(FlippableAttribute), true);

                if (objs.Length > 0)
                {
                    if (objs[0] is FlippableAttribute fp)
                    {
                        var itemIDs = fp.ItemIDs;

                        var oldWorldLoc = bi.WorldLoc;
                        var newWorldLoc = location;

                        if (oldWorldLoc.X != newWorldLoc.X || oldWorldLoc.Y != newWorldLoc.Y)
                        {
                            var dir = GetDirection4(oldWorldLoc, newWorldLoc);

                            item.ItemID = itemIDs.Length switch
                            {
                                2 => dir switch
                                {
                                    Direction.North => itemIDs[0],
                                    Direction.South => itemIDs[0],
                                    Direction.East  => itemIDs[1],
                                    Direction.West  => itemIDs[1],
                                    _               => item.ItemID
                                },
                                4 => dir switch
                                {
                                    Direction.South => itemIDs[0],
                                    Direction.East  => itemIDs[1],
                                    Direction.North => itemIDs[2],
                                    Direction.West  => itemIDs[3],
                                    _               => item.ItemID
                                },
                                _ => item.ItemID
                            };
                        }
                    }
                }
            }

            return true;
        }

        public bool GetFlag(PlayerFlag flag) => (Flags & flag) != 0;

        public void SetFlag(PlayerFlag flag, bool value)
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }

        public static void Initialize()
        {
            EventSink.Login += OnLogin;
            EventSink.Logout += OnLogout;
            EventSink.Connected += EventSink_Connected;
            EventSink.Disconnected += EventSink_Disconnected;

            EventSink.TargetedSkillUse += TargetedSkillUse;
            EventSink.EquipMacro += EquipMacro;
            EventSink.UnequipMacro += UnequipMacro;

            if (Core.SE)
            {
                Timer.StartTimer(CheckPets);
            }

            var stableMigrations = StableMigrations;
            if (stableMigrations?.Count > 0)
            {
                foreach (var (player, stabled) in stableMigrations)
                {
                    if (player is PlayerMobile pm)
                    {
                        pm.Stabled = stabled;
                    }
                }
            }
        }

        private static void TargetedSkillUse(Mobile from, IEntity target, int skillId)
        {
            if (from == null || target == null)
            {
                return;
            }

            from.TargetLocked = true;

            if (skillId == 35)
            {
                AnimalTaming.DisableMessage = true;
            }
            // AnimalTaming.DeferredTarget = false;

            if (from.UseSkill(skillId))
            {
                from.Target?.Invoke(from, target);
            }

            if (skillId == 35)
                // AnimalTaming.DeferredTarget = true;
            {
                AnimalTaming.DisableMessage = false;
            }

            from.TargetLocked = false;
        }

        public static void EquipMacro(Mobile m, List<Serial> list)
        {
            if (m is PlayerMobile { Alive: true } pm && pm.Backpack != null)
            {
                var pack = pm.Backpack;

                foreach (var serial in list)
                {
                    Item item = null;
                    foreach (var i in pack.Items)
                    {
                        if (i.Serial == serial)
                        {
                            item = i;
                            break;
                        }
                    }

                    if (item == null)
                    {
                        continue;
                    }

                    var toMove = pm.FindItemOnLayer(item.Layer);

                    if (toMove != null)
                    {
                        // pack.DropItem(toMove);
                        toMove.Internalize();

                        if (!pm.EquipItem(item))
                        {
                            pm.EquipItem(toMove);
                        }
                        else
                        {
                            pack.DropItem(toMove);
                        }
                    }
                    else
                    {
                        pm.EquipItem(item);
                    }
                }
            }
        }

        public static void UnequipMacro(Mobile m, List<Layer> layers)
        {
            if (m is PlayerMobile { Alive: true } pm && pm.Backpack != null)
            {
                var pack = pm.Backpack;
                var eq = m.Items;

                for (var i = eq.Count - 1; i >= 0; i--)
                {
                    var item = eq[i];
                    if (layers.Contains(item.Layer))
                    {
                        pack.TryDropItem(pm, item, false);
                    }
                }
            }
        }

        private static void CheckPets()
        {
            foreach (var m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm &&
                    ((!pm.Mounted || pm.Mount is EtherealMount) && pm.AllFollowers?.Count > pm.AutoStabled?.Count ||
                     pm.Mounted && pm.AllFollowers?.Count > (pm.AutoStabled?.Count ?? 0) + 1))
                {
                    pm.AutoStablePets(); /* autostable checks summons, et al: no need here */
                }
            }
        }

        public void SetMountBlock(BlockMountType type, bool dismount) =>
            SetMountBlock(type, TimeSpan.MaxValue, dismount);

        public void SetMountBlock(BlockMountType type, TimeSpan duration, bool dismount)
        {
            if (dismount)
            {
                if (Mount != null)
                {
                    Mount.Rider = null;
                }
                else if (AnimalForm.UnderTransformation(this))
                {
                    AnimalForm.RemoveContext(this, true);
                }
            }

            if (_mountBlock == null || !_mountBlock.CheckBlock() || _mountBlock.Expiration < Core.Now + duration)
            {
                _mountBlock?.RemoveBlock(this);
                _mountBlock = new MountBlock(duration, type, this);
            }
        }

        public override void OnSkillInvalidated(Skill skill)
        {
            if (Core.AOS && skill.SkillName == SkillName.MagicResist)
            {
                UpdateResistances();
            }
        }

        public override int GetMaxResistance(ResistanceType type)
        {
            if (AccessLevel > AccessLevel.Player)
            {
                return 100;
            }

            var max = base.GetMaxResistance(type);

            if (type != ResistanceType.Physical && CurseSpell.UnderEffect(this))
            {
                max -= 10;
            }

            if (Core.ML && Race == Race.Elf && type == ResistanceType.Energy)
            {
                max += 5; // Intended to go after the 60 max from curse
            }

            return max;
        }

        protected override void OnRaceChange(Race oldRace)
        {
            ValidateEquipment();
            UpdateResistances();
        }

        public override void OnNetStateChanged()
        {
            m_LastGlobalLight = -1;
            m_LastPersonalLight = -1;
        }

        public override void ComputeBaseLightLevels(out int global, out int personal)
        {
            global = LightCycle.ComputeLevelFor(this);

            var racialNightSight = Core.ML && Race == Race.Elf;

            if (LightLevel < 21 && (AosAttributes.GetValue(this, AosAttribute.NightSight) > 0 || racialNightSight))
            {
                personal = 21;
            }
            else
            {
                personal = LightLevel;
            }
        }

        public override void CheckLightLevels(bool forceResend)
        {
            var ns = NetState;

            if (ns == null)
            {
                return;
            }

            ComputeLightLevels(out var global, out var personal);

            if (!forceResend)
            {
                forceResend = global != m_LastGlobalLight || personal != m_LastPersonalLight;
            }

            if (!forceResend)
            {
                return;
            }

            m_LastGlobalLight = global;
            m_LastPersonalLight = personal;

            ns.SendGlobalLightLevel(global);
            ns.SendPersonalLightLevel(Serial, personal);
        }

        public override int GetMinResistance(ResistanceType type)
        {
            var magicResist = (int)(Skills.MagicResist.Value * 10);
            int min;

            if (magicResist >= 1000)
            {
                min = 40 + (magicResist - 1000) / 50;
            }
            else if (magicResist >= 400)
            {
                min = (magicResist - 400) / 15;
            }
            else
            {
                min = int.MinValue;
            }

            return Math.Clamp(min, base.GetMinResistance(type), MaxPlayerResistance);
        }

        public override void OnManaChange(int oldValue)
        {
            base.OnManaChange(oldValue);
            if (ExecutesLightningStrike > 0)
            {
                if (Mana < ExecutesLightningStrike)
                {
                    SpecialMove.ClearCurrentMove(this);
                }
            }
        }

        private static void OnLogin(Mobile from)
        {
            if (AccountHandler.LockdownLevel > AccessLevel.Player)
            {
                string notice;

                if (from.Account is not Account acct || !acct.HasAccess(from.NetState))
                {
                    if (from.AccessLevel == AccessLevel.Player)
                    {
                        notice = "The server is currently under lockdown. No players are allowed to log in at this time.";
                    }
                    else
                    {
                        notice =
                            "The server is currently under lockdown. You do not have sufficient access level to connect.";
                    }

                    if (from.NetState != null)
                    {
                        Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => from.NetState.Disconnect("Server is locked down"));
                    }
                }
                else if (from.AccessLevel >= AccessLevel.Administrator)
                {
                    notice =
                        "The server is currently under lockdown. As you are an administrator, you may change this from the [Admin gump.";
                }
                else
                {
                    notice = "The server is currently under lockdown. You have sufficient access level to connect.";
                }

                from.SendGump(new NoticeGump(1060637, 30720, notice, 0xFFC000, 300, 140));
                return;
            }

            if (from is PlayerMobile mobile)
            {
                VirtueSystem.CheckAtrophies(mobile);
                mobile.ClaimAutoStabledPets();
            }
        }

        public void ValidateEquipment()
        {
            if (m_NoDeltaRecursion || Map == null || Map == Map.Internal)
            {
                return;
            }

            if (Items == null)
            {
                return;
            }

            m_NoDeltaRecursion = true;
            Timer.StartTimer(ValidateEquipment_Sandbox);
        }

        private void ValidateEquipment_Sandbox()
        {
            try
            {
                if (Map == null || Map == Map.Internal)
                {
                    return;
                }

                var items = Items;

                if (items == null)
                {
                    return;
                }

                var moved = false;

                var str = Str;
                var dex = Dex;
                var intel = Int;

                var factionItemCount = 0;

                Mobile from = this;

                var ethic = Ethic.Find(from);

                for (var i = items.Count - 1; i >= 0; --i)
                {
                    if (i >= items.Count)
                    {
                        continue;
                    }

                    var item = items[i];

                    if ((item.SavedFlags & 0x100) != 0)
                    {
                        if (item.Hue != Ethic.Hero.Definition.PrimaryHue)
                        {
                            item.SavedFlags &= ~0x100;
                        }
                        else if (ethic != Ethic.Hero)
                        {
                            from.AddToBackpack(item);
                            moved = true;
                            continue;
                        }
                    }
                    else if ((item.SavedFlags & 0x200) != 0)
                    {
                        if (item.Hue != Ethic.Evil.Definition.PrimaryHue)
                        {
                            item.SavedFlags &= ~0x200;
                        }
                        else if (ethic != Ethic.Evil)
                        {
                            from.AddToBackpack(item);
                            moved = true;
                            continue;
                        }
                    }

                    if (item is BaseWeapon weapon)
                    {
                        var drop = false;

                        if (dex < weapon.DexRequirement)
                        {
                            drop = true;
                        }
                        else if (str < AOS.Scale(weapon.StrRequirement, 100 - weapon.GetLowerStatReq()))
                        {
                            drop = true;
                        }
                        else if (intel < weapon.IntRequirement)
                        {
                            drop = true;
                        }
                        else if (!weapon.CheckRace(Race))
                        {
                            drop = true;
                        }

                        if (drop)
                        {
                            // You can no longer wield your ~1_WEAPON~
                            from.SendLocalizedMessage(1062001, weapon.Name ?? $"#{weapon.LabelNumber}");
                            from.AddToBackpack(weapon);
                            moved = true;
                        }
                    }
                    else if (item is BaseArmor armor)
                    {
                        var drop = false;

                        if (!armor.AllowMaleWearer && !from.Female && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (!armor.AllowFemaleWearer && from.Female && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (!armor.CheckRace(Race))
                        {
                            drop = true;
                        }
                        else
                        {
                            int strBonus = armor.ComputeStatBonus(StatType.Str), strReq = armor.ComputeStatReq(StatType.Str);
                            int dexBonus = armor.ComputeStatBonus(StatType.Dex), dexReq = armor.ComputeStatReq(StatType.Dex);
                            int intBonus = armor.ComputeStatBonus(StatType.Int), intReq = armor.ComputeStatReq(StatType.Int);

                            if (dex < dexReq || dex + dexBonus < 1)
                            {
                                drop = true;
                            }
                            else if (str < strReq || str + strBonus < 1)
                            {
                                drop = true;
                            }
                            else if (intel < intReq || intel + intBonus < 1)
                            {
                                drop = true;
                            }
                        }

                        if (drop)
                        {
                            var name = armor.Name ?? $"#{armor.LabelNumber}";

                            if (armor is BaseShield)
                            {
                                from.SendLocalizedMessage(1062003, name); // You can no longer equip your ~1_SHIELD~
                            }
                            else
                            {
                                from.SendLocalizedMessage(1062002, name); // You can no longer wear your ~1_ARMOR~
                            }

                            from.AddToBackpack(armor);
                            moved = true;
                        }
                    }
                    else if (item is BaseClothing clothing)
                    {
                        var drop = false;

                        if (!clothing.AllowMaleWearer && !from.Female && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (!clothing.AllowFemaleWearer && from.Female && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (clothing.RequiredRace != null && clothing.RequiredRace != Race)
                        {
                            drop = true;
                        }
                        else
                        {
                            var strBonus = clothing.ComputeStatBonus(StatType.Str);
                            var strReq = clothing.ComputeStatReq(StatType.Str);

                            if (str < strReq || str + strBonus < 1)
                            {
                                drop = true;
                            }
                        }

                        if (drop)
                        {
                            // You can no longer wear your ~1_ARMOR~
                            from.SendLocalizedMessage(1062002, clothing.Name ?? $"#{clothing.LabelNumber}");

                            from.AddToBackpack(clothing);
                            moved = true;
                        }
                    }

                    var factionItem = FactionItem.Find(item);

                    if (factionItem != null)
                    {
                        var drop = false;

                        var ourFaction = Faction.Find(this);

                        if (ourFaction == null || ourFaction != factionItem.Faction)
                        {
                            drop = true;
                        }
                        else if (++factionItemCount > FactionItem.GetMaxWearables(this))
                        {
                            drop = true;
                        }

                        if (drop)
                        {
                            from.AddToBackpack(item);
                            moved = true;
                        }
                    }
                }

                if (moved)
                {
                    from.SendLocalizedMessage(500647); // Some equipment has been moved to your backpack.
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                m_NoDeltaRecursion = false;
            }
        }

        public override void Delta(MobileDelta flag)
        {
            base.Delta(flag);

            if ((flag & MobileDelta.Stat) != 0)
            {
                ValidateEquipment();
            }
        }

        private static void OnLogout(Mobile m)
        {
            (m as PlayerMobile)?.AutoStablePets();
        }

        private static void EventSink_Connected(Mobile m)
        {
            if (m is PlayerMobile pm)
            {
                pm.SessionStart = Core.Now;

                pm.Quest?.StartTimer();

                pm.BedrollLogout = false;
                pm.LastOnline = Core.Now;
            }

            DisguisePersistence.StartTimer(m);

            Timer.StartTimer(() => SpecialMove.ClearAllMoves(m));
        }

        private static void EventSink_Disconnected(Mobile from)
        {
            var context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client disconnected
                 *  - Remove design context
                 *  - Eject all from house
                 *  - Restore relocated entities
                 */

                // Remove design context
                DesignContext.Remove(from);

                // Eject all from house
                from.RevealingAction();

                foreach (var item in context.Foundation.GetItems())
                {
                    item.Location = context.Foundation.BanLocation;
                }

                foreach (var mobile in context.Foundation.GetMobiles())
                {
                    mobile.Location = context.Foundation.BanLocation;
                }

                // Restore relocated entities
                context.Foundation.RestoreRelocatedEntities();
            }

            if (from is PlayerMobile pm)
            {
                pm.m_GameTime += Core.Now - pm.SessionStart;

                pm.Quest?.StopTimer();

                pm.SpeechLog = null;
                pm.ClearQuestArrow();
                pm.LastOnline = Core.Now;
            }

            DisguisePersistence.StopTimer(from);
        }

        public override void RevealingAction()
        {
            if (DesignContext != null)
            {
                return;
            }

            InvisibilitySpell.StopTimer(this);

            base.RevealingAction();

            IsStealthing = false; // IsStealthing should be moved to Server.Mobiles
        }

        public override void OnHiddenChanged()
        {
            base.OnHiddenChanged();

            RemoveBuff(
                BuffIcon
                    .Invisibility
            ); // Always remove, default to the hiding icon EXCEPT in the invis spell where it's explicitly set

            if (!Hidden)
            {
                RemoveBuff(BuffIcon.HidingAndOrStealth);
            }
            else // if (!InvisibilitySpell.HasTimer( this ))
            {
                BuffInfo.AddBuff(
                    this,
                    new BuffInfo(BuffIcon.HidingAndOrStealth, 1075655)
                ); // Hidden/Stealthing & You Are Hidden
            }
        }

        public override void OnSubItemAdded(Item item)
        {
            if (AccessLevel < AccessLevel.GameMaster && item.IsChildOf(Backpack))
            {
                var maxWeight = StaminaSystem.GetMaxWeight(this);
                var curWeight = BodyWeight + TotalWeight;

                if (curWeight > maxWeight)
                {
                    SendLocalizedMessage(1019035, true, $" : {curWeight} / {maxWeight}");
                }
            }

            base.OnSubItemAdded(item);
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            if (DesignContext != null || target is PlayerMobile mobile && mobile.DesignContext != null)
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

        public override bool CanBeBeneficial(Mobile target, bool message, bool allowDead)
        {
            if (DesignContext != null || target is PlayerMobile mobile && mobile.DesignContext != null)
            {
                return false;
            }

            return base.CanBeBeneficial(target, message, allowDead);
        }

        public override bool CheckContextMenuDisplay(IEntity target) => DesignContext == null;

        public override void OnItemAdded(Item item)
        {
            base.OnItemAdded(item);

            if (item is BaseArmor or BaseWeapon)
            {
                CheckStatTimers();
            }

            if (NetState != null)
            {
                CheckLightLevels(false);
            }
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (item is BaseArmor or BaseWeapon)
            {
                CheckStatTimers();
            }

            if (NetState != null)
            {
                CheckLightLevels(false);
            }
        }

        private void AddArmorRating(ref double rating, Item armor)
        {
            if (armor is BaseArmor ar && (!Core.AOS || ar.ArmorAttributes.MageArmor == 0))
            {
                rating += ar.ArmorRatingScaled;
            }
        }

        public override bool Move(Direction d)
        {
            var ns = NetState;

            if (ns != null)
            {
                if (HasGump<ResurrectGump>())
                {
                    if (Alive)
                    {
                        CloseGump<ResurrectGump>();
                    }
                    else
                    {
                        SendLocalizedMessage(500111); // You are frozen and cannot move.
                        return false;
                    }
                }
            }

            // var speed = ComputeMovementSpeed(d);

            if (!Alive)
            {
                MovementImpl.IgnoreMovableImpassables = true;
            }

            var res = base.Move(d);
            MovementImpl.IgnoreMovableImpassables = false;
            return res;
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            var context = DesignContext;

            if (context == null)
            {
                return base.CheckMovement(d, out newZ);
            }

            var foundation = context.Foundation;

            newZ = foundation.Z + HouseFoundation.GetLevelZ(context.Level, context.Foundation);

            int newX = X, newY = Y;
            Movement.Movement.Offset(d, ref newX, ref newY);

            var startX = foundation.X + foundation.Components.Min.X + 1;
            var startY = foundation.Y + foundation.Components.Min.Y + 1;
            var endX = startX + foundation.Components.Width - 1;
            var endY = startY + foundation.Components.Height - 2;

            return newX >= startX && newY >= startY && newX < endX && newY < endY && Map == foundation.Map;
        }

        public override bool AllowItemUse(Item item)
        {
            if (DuelContext?.AllowItemUse(this, item) == false)
            {
                return false;
            }

            return DesignContext.Check(this);
        }

        public override bool AllowSkillUse(SkillName skill)
        {
            if (AnimalForm.UnderTransformation(this))
            {
                for (var i = 0; i < AnimalFormRestrictedSkills.Length; i++)
                {
                    if (AnimalFormRestrictedSkills[i] == skill)
                    {
                        SendLocalizedMessage(1070771); // You cannot use that skill in this form.
                        return false;
                    }
                }
            }

            return DuelContext?.AllowSkillUse(this, skill) != false && DesignContext.Check(this);
        }

        public virtual void RecheckTownProtection()
        {
            m_NextProtectionCheck = 10;

            var reg = Region.GetRegion<GuardedRegion>();
            var isProtected = reg?.IsDisabled() == false;

            if (isProtected != m_LastProtectedMessage)
            {
                if (isProtected)
                {
                    SendLocalizedMessage(500112); // You are now under the protection of the town guards.
                }
                else
                {
                    SendLocalizedMessage(500113); // You have left the protection of the town guards.
                }

                m_LastProtectedMessage = isProtected;
            }
        }

        public override void MoveToWorld(Point3D loc, Map map)
        {
            base.MoveToWorld(loc, map);

            RecheckTownProtection();
        }

        public override void SetLocation(Point3D loc, bool isTeleport)
        {
            if (!isTeleport && AccessLevel == AccessLevel.Player)
            {
                // moving, not teleporting
                var zDrop = Location.Z - loc.Z;

                if (zDrop > 20)                  // we fell more than one story
                {
                    Hits -= zDrop / 20 * 10 - 5; // deal some damage; does not kill, disrupt, etc
                }
            }

            base.SetLocation(loc, isTeleport);

            if (isTeleport || --m_NextProtectionCheck == 0)
            {
                RecheckTownProtection();
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from == this)
            {
                Quest?.GetContextMenuEntries(list);

                if (Alive)
                {
                    if (InsuranceEnabled)
                    {
                        if (Core.SA)
                        {
                            list.Add(new CallbackEntry(1114299, OpenItemInsuranceMenu)); // Open Item Insurance Menu
                        }

                        list.Add(new CallbackEntry(6201, ToggleItemInsurance)); // Toggle Item Insurance

                        if (!Core.SA)
                        {
                            if (AutoRenewInsurance)
                            {
                                // Cancel Renewing Inventory Insurance
                                list.Add(new CallbackEntry(6202, CancelRenewInventoryInsurance));
                            }
                            else
                            {
                                // Auto Renew Inventory Insurance
                                list.Add(new CallbackEntry(6200, AutoRenewInventoryInsurance));
                            }
                        }
                    }

                    if (MLQuestSystem.Enabled)
                    {
                        list.Add(new CallbackEntry(6169, ToggleQuestItem)); // Toggle Quest Item
                    }
                }

                var house = BaseHouse.FindHouseAt(this);

                if (house != null)
                {
                    if (Alive && house.InternalizedVendors.Count > 0 && house.IsOwner(this))
                    {
                        list.Add(new CallbackEntry(6204, GetVendor));
                    }

                    if (house.IsAosRules && !Region.IsPartOf<SafeZone>()) // Dueling
                    {
                        list.Add(new CallbackEntry(6207, LeaveHouse));
                    }
                }

                if (JusticeVirtue.IsProtected(this))
                {
                    list.Add(new CallbackEntry(6157, CancelProtection));
                }

                if (Alive)
                {
                    list.Add(new CallbackEntry(6210, ToggleChampionTitleDisplay));
                }

                if (Core.HS)
                {
                    var ns = from.NetState;

                    if (ns?.ExtendedStatus == true)
                    {
                        // Allow Trades / Refuse Trades
                        list.Add(new CallbackEntry(RefuseTrades ? 1154112 : 1154113, ToggleTrades));
                    }
                }
            }
            else
            {
                if (Core.TOL && from.InRange(this, 2))
                {
                    list.Add(new CallbackEntry(1077728, () => OpenTrade(from))); // Trade
                }

                if (Alive && Core.Expansion >= Expansion.AOS)
                {
                    var theirParty = from.Party as Party;
                    var ourParty = Party as Party;

                    if (theirParty == null && ourParty == null)
                    {
                        list.Add(new AddToPartyEntry(from, this));
                    }
                    else if (theirParty != null && theirParty.Leader == from)
                    {
                        if (ourParty == null)
                        {
                            list.Add(new AddToPartyEntry(from, this));
                        }
                        else if (ourParty == theirParty)
                        {
                            list.Add(new RemoveFromPartyEntry(from, this));
                        }
                    }
                }

                var curhouse = BaseHouse.FindHouseAt(this);

                if (curhouse != null && Alive && Core.Expansion >= Expansion.AOS && curhouse.IsAosRules &&
                    curhouse.IsFriend(from))
                {
                    list.Add(new EjectPlayerEntry(from, this));
                }
            }
        }

        private void CancelProtection()
        {
            if (JusticeVirtue.CancelProtection(this, out var prot))
            {
                var args = $"{Name}\t{prot.Name}";

                // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
                prot.SendLocalizedMessage(1049371, args);

                // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
                SendLocalizedMessage(1049371, args);
            }
        }

        private void ToggleTrades()
        {
            RefuseTrades = !RefuseTrades;
        }

        private void GetVendor()
        {
            var house = BaseHouse.FindHouseAt(this);

            if (CheckAlive() && house?.IsOwner(this) == true && house.InternalizedVendors.Count > 0)
            {
                CloseGump<ReclaimVendorGump>();
                SendGump(new ReclaimVendorGump(house));
            }
        }

        private void LeaveHouse()
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house != null)
            {
                Location = house.BanLocation;
            }
        }

        public override void DisruptiveAction()
        {
            if (Meditating)
            {
                RemoveBuff(BuffIcon.ActiveMeditation);
            }

            base.DisruptiveAction();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (this == from && !Warmode)
            {
                var mount = Mount;

                if (mount != null && !DesignContext.Check(this))
                {
                    return;
                }
            }

            base.OnDoubleClick(from);
        }

        public override void DisplayPaperdollTo(Mobile to)
        {
            if (DesignContext.Check(this))
            {
                base.DisplayPaperdollTo(to);
            }
        }

        public override bool CheckEquip(Item item)
        {
            if (!base.CheckEquip(item))
            {
                return false;
            }

            if (DuelContext?.AllowItemEquip(this, item) == false)
            {
                return false;
            }

            var factionItem = FactionItem.Find(item);

            if (factionItem != null)
            {
                var faction = Faction.Find(this);

                if (faction == null)
                {
                    SendLocalizedMessage(1010371); // You cannot equip a faction item!
                    return false;
                }

                if (faction != factionItem.Faction)
                {
                    SendLocalizedMessage(1010372); // You cannot equip an opposing faction's item!
                    return false;
                }

                var maxWearables = FactionItem.GetMaxWearables(this);

                for (var i = 0; i < Items.Count; ++i)
                {
                    var equipped = Items[i];

                    if (item != equipped && FactionItem.Find(equipped) != null)
                    {
                        if (--maxWearables == 0)
                        {
                            SendLocalizedMessage(1010373); // You do not have enough rank to equip more faction items!
                            return false;
                        }
                    }
                }
            }

            if (AccessLevel < AccessLevel.GameMaster && item.Layer != Layer.Mount && HasTrade)
            {
                var bounce = item.GetBounce();

                if (bounce != null)
                {
                    if (bounce.Parent is Item parent)
                    {
                        if (parent == Backpack || parent.IsChildOf(Backpack))
                        {
                            return true;
                        }
                    }
                    else if (bounce.Parent == this)
                    {
                        return true;
                    }
                }

                SendLocalizedMessage(
                    1004042
                ); // You can only equip what you are already carrying while you have a trade pending.
                return false;
            }

            return true;
        }

        public override bool CheckTrade(
            Mobile to, Item item, SecureTradeContainer cont, bool message, bool checkItems,
            int plusItems, int plusWeight
        )
        {
            var msgNum = 0;

            if (cont == null)
            {
                if (to.Holding != null)
                {
                    msgNum = 1062727; // You cannot trade with someone who is dragging something.
                }
                else if (HasTrade)
                {
                    msgNum = 1062781; // You are already trading with someone else!
                }
                else if (to.HasTrade)
                {
                    msgNum = 1062779; // That person is already involved in a trade
                }
                else if (to is PlayerMobile mobile && mobile.RefuseTrades)
                {
                    msgNum = 1154111; // ~1_NAME~ is refusing all trades.
                }
            }

            if (msgNum == 0 && item != null)
            {
                if (cont != null)
                {
                    plusItems += cont.TotalItems;
                    plusWeight += cont.TotalWeight;
                }

                if (Backpack?.CheckHold(this, item, false, checkItems, plusItems, plusWeight) != true)
                {
                    msgNum = 1004040; // You would not be able to hold this if the trade failed.
                }
                else if (to.Backpack?.CheckHold(to, item, false, checkItems, plusItems, plusWeight) != true)
                {
                    msgNum = 1004039; // The recipient of this trade would not be able to carry this.
                }
                else
                {
                    msgNum = CheckContentForTrade(item);
                }
            }

            if (msgNum != 0)
            {
                if (message)
                {
                    if (msgNum == 1154111)
                    {
                        SendLocalizedMessage(msgNum, to.Name);
                    }
                    else
                    {
                        SendLocalizedMessage(msgNum);
                    }
                }

                return false;
            }

            return true;
        }

        private static int CheckContentForTrade(Item item)
        {
            if (item is TrappableContainer container && container.TrapType != TrapType.None)
            {
                return 1004044; // You may not trade trapped items.
            }

            if (StolenItem.IsStolen(item))
            {
                return 1004043; // You may not trade recently stolen items.
            }

            if (item is Container)
            {
                foreach (var subItem in item.Items)
                {
                    var msg = CheckContentForTrade(subItem);

                    if (msg != 0)
                    {
                        return msg;
                    }
                }
            }

            return 0;
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            if (!base.CheckNonlocalDrop(from, item, target))
            {
                return false;
            }

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            var pack = Backpack;
            if (from == this && HasTrade && (target == pack || target.IsChildOf(pack)))
            {
                var bounce = item.GetBounce();

                if (bounce?.Parent is Item parent && (parent == pack || parent.IsChildOf(pack)))
                {
                    return true;
                }

                SendLocalizedMessage(1004041); // You can't do that while you have a trade pending.
                return false;
            }

            return true;
        }

        protected override void OnLocationChange(Point3D oldLocation)
        {
            CheckLightLevels(false);

            DuelContext?.OnLocationChanged(this);

            var context = DesignContext;

            if (context == null || m_NoRecursion)
            {
                return;
            }

            m_NoRecursion = true;

            var foundation = context.Foundation;

            int newX = X, newY = Y;
            var newZ = foundation.Z + HouseFoundation.GetLevelZ(context.Level, context.Foundation);

            var startX = foundation.X + foundation.Components.Min.X + 1;
            var startY = foundation.Y + foundation.Components.Min.Y + 1;
            var endX = startX + foundation.Components.Width - 1;
            var endY = startY + foundation.Components.Height - 2;

            if (newX >= startX && newY >= startY && newX < endX && newY < endY && Map == foundation.Map)
            {
                if (Z != newZ)
                {
                    Location = new Point3D(X, Y, newZ);
                }

                m_NoRecursion = false;
                return;
            }

            Location = new Point3D(foundation.X, foundation.Y, newZ);
            Map = foundation.Map;

            m_NoRecursion = false;
        }

        public override bool OnMoveOver(Mobile m) =>
            m is BaseCreature creature && !creature.Controlled
                ? !Alive || !creature.Alive || IsDeadBondedPet || creature.IsDeadBondedPet ||
                  Hidden && AccessLevel > AccessLevel.Player
                : Region.IsPartOf<SafeZone>() && m is PlayerMobile pm &&
                (pm.DuelContext == null || pm.DuelPlayer == null || !pm.DuelContext.Started || pm.DuelContext.Finished ||
                 pm.DuelPlayer.Eliminated) || base.OnMoveOver(m);

        public override bool CheckShove(Mobile shoved) =>
            IgnoreMobiles || shoved.IgnoreMobiles || TransformationSpellHelper.UnderTransformation(shoved, typeof(WraithFormSpell)) ||
            base.CheckShove(shoved);

        protected override void OnMapChange(Map oldMap)
        {
            if (Map != Faction.Facet && oldMap == Faction.Facet || Map == Faction.Facet && oldMap != Faction.Facet)
            {
                InvalidateProperties();
            }

            DuelContext?.OnMapChanged(this);

            var context = DesignContext;

            if (context == null || m_NoRecursion)
            {
                return;
            }

            m_NoRecursion = true;

            var foundation = context.Foundation;

            if (Map != foundation.Map)
            {
                Map = foundation.Map;
            }

            m_NoRecursion = false;
        }

        public override void OnBeneficialAction(Mobile target, bool isCriminal)
        {
            SentHonorContext?.OnSourceBeneficialAction(target);

            base.OnBeneficialAction(target, isCriminal);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
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

            ReceivedHonorContext?.OnTargetDamaged(from, amount);
            SentHonorContext?.OnSourceDamaged(from, amount);

            if (willKill && from is PlayerMobile mobile)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(10), mobile.RecoverAmmo);
            }

            base.OnDamage(amount, from, willKill);
        }

        public override void Resurrect()
        {
            var wasAlive = Alive;

            base.Resurrect();

            if (Alive && !wasAlive)
            {
                Item deathRobe = new DeathRobe();

                if (!EquipItem(deathRobe))
                {
                    deathRobe.Delete();
                }
            }
        }

        public override void OnWarmodeChanged()
        {
            if (!Warmode)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(10), RecoverAmmo);
            }
        }

        private bool FindItems_Callback(Item item) =>
            !item.Deleted && (item.LootType == LootType.Blessed || item.Insured) &&
            Backpack != item.Parent;

        public override bool OnBeforeDeath()
        {
            var state = NetState;

            state?.CancelAllTrades();

            DropHolding();

            // During AOS+, insured/blessed items are moved out of their child containers and put directly into the backpack.
            // This fixes a "bug" where players put blessed items in nested bags and they were dropped on death
            if (Core.AOS && Backpack?.Deleted == false)
            {
                foreach (var item in Backpack.EnumerateItems(true, FindItems_Callback))
                {
                    Backpack.AddItem(item);
                }
            }

            EquipSnapshot = new List<Item>(Items);

            m_NonAutoreinsuredItems = 0;
            m_InsuranceAward = FindMostRecentDamager(false);

            if (m_InsuranceAward is BaseCreature creature)
            {
                var master = creature.GetMaster();

                if (master != null)
                {
                    m_InsuranceAward = master;
                }
            }

            if (m_InsuranceAward != null && (!m_InsuranceAward.Player || m_InsuranceAward == this))
            {
                m_InsuranceAward = null;
            }

            if (m_InsuranceAward is PlayerMobile mobile)
            {
                mobile.m_InsuranceBonus = 0;
            }

            ReceivedHonorContext?.OnTargetKilled();
            SentHonorContext?.OnSourceKilled();

            RecoverAmmo();

            return base.OnBeforeDeath();
        }

        private bool CheckInsuranceOnDeath(Item item)
        {
            if (!InsuranceEnabled || !item.Insured)
            {
                return false;
            }

            if (DuelContext?.Registered == true && DuelContext.Started &&
                m_DuelPlayer?.Eliminated != true)
            {
                return true;
            }

            if (AutoRenewInsurance)
            {
                var cost = GetInsuranceCost(item);

                if (m_InsuranceAward != null)
                {
                    cost /= 2;
                }

                if (Banker.Withdraw(this, cost))
                {
                    item.PaidInsurance = true;
                    // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                    SendLocalizedMessage(1060398, cost.ToString());
                }
                else
                {
                    SendLocalizedMessage(1061079, "", 0x23); // You lack the funds to purchase the insurance
                    item.PaidInsurance = false;
                    item.Insured = false;
                    m_NonAutoreinsuredItems++;
                }
            }
            else
            {
                item.PaidInsurance = false;
                item.Insured = false;
            }

            if (m_InsuranceAward is PlayerMobile insurancePm && Banker.Deposit(m_InsuranceAward, 300))
            {
                insurancePm.m_InsuranceBonus += 300;
            }

            return true;
        }

        public override DeathMoveResult GetParentMoveResultFor(Item item)
        {
            // It seems all items are unmarked on death, even blessed/insured ones
            if (item.QuestItem)
            {
                item.QuestItem = false;
            }

            if (CheckInsuranceOnDeath(item))
            {
                return DeathMoveResult.MoveToBackpack;
            }

            var res = base.GetParentMoveResultFor(item);

            if (res == DeathMoveResult.MoveToCorpse && item.Movable && Young)
            {
                res = DeathMoveResult.MoveToBackpack;
            }

            return res;
        }

        public override DeathMoveResult GetInventoryMoveResultFor(Item item)
        {
            // It seems all items are unmarked on death, even blessed/insured ones
            if (item.QuestItem)
            {
                item.QuestItem = false;
            }

            if (CheckInsuranceOnDeath(item))
            {
                return DeathMoveResult.MoveToBackpack;
            }

            var res = base.GetInventoryMoveResultFor(item);

            if (res == DeathMoveResult.MoveToCorpse && item.Movable && Young)
            {
                res = DeathMoveResult.MoveToBackpack;
            }

            return res;
        }

        public override void OnDeath(Container c)
        {
            if (m_NonAutoreinsuredItems > 0)
            {
                SendLocalizedMessage(1061115);
            }

            base.OnDeath(c);

            EquipSnapshot = null;

            HueMod = -1;
            NameMod = null;
            SavagePaintExpiration = TimeSpan.Zero;

            SetHairMods(-1, -1);

            PolymorphSpell.StopTimer(this);
            IncognitoSpell.StopTimer(this);
            DisguisePersistence.RemoveTimer(this);

            EndAction<PolymorphSpell>();
            EndAction<IncognitoSpell>();

            MeerMage.StopEffect(this, false);

            if (Flying)
            {
                Flying = false;
                BuffInfo.RemoveBuff(this, BuffIcon.Fly);
            }

            StolenItem.ReturnOnDeath(this, c);

            if (PermaFlags.Count > 0)
            {
                PermaFlags.Clear();

                if (c is Corpse corpse)
                {
                    corpse.Criminal = true;
                }

                if (Stealing.ClassicMode)
                {
                    Criminal = true;
                }
            }

            if (Kills >= 5 && Core.Now >= m_NextJustAward)
            {
                var m = FindMostRecentDamager(false);

                if (m is BaseCreature bc)
                {
                    m = bc.GetMaster();
                }

                if (m != this && m is PlayerMobile pm)
                {
                    var gainedPath = false;

                    var pointsToGain = 0;

                    pointsToGain += (int)Math.Sqrt(GameTime.TotalSeconds * 4);
                    pointsToGain *= 5;
                    pointsToGain += (int)Math.Pow(Skills.Total / 250.0, 2);

                    if (VirtueSystem.Award(pm, VirtueName.Justice, pointsToGain, ref gainedPath))
                    {
                        if (gainedPath)
                        {
                            m.SendLocalizedMessage(1049367); // You have gained a path in Justice!
                        }
                        else
                        {
                            m.SendLocalizedMessage(1049363); // You have gained in Justice.
                        }

                        m.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);
                        m.PlaySound(0x1F7);

                        m_NextJustAward = Core.Now + TimeSpan.FromMinutes(pointsToGain / 3.0);
                    }
                }
            }

            if (m_InsuranceAward is PlayerMobile insurancePm && insurancePm.m_InsuranceBonus > 0)
            {
                // ~1_AMOUNT~ gold has been deposited into your bank box.
                insurancePm.SendLocalizedMessage(1060397, insurancePm.m_InsuranceBonus.ToString());
            }

            var killer = FindMostRecentDamager(true);

            if (killer is BaseCreature bcKiller)
            {
                var master = bcKiller.GetMaster();
                if (master != null)
                {
                    killer = master;
                }
            }

            if (Young && DuelContext == null)
            {
                if (YoungDeathTeleport())
                {
                    Timer.StartTimer(TimeSpan.FromSeconds(2.5), SendYoungDeathNotice);
                }
            }

            if (DuelContext?.Registered != true || !DuelContext.Started || m_DuelPlayer?.Eliminated != false)
            {
                Faction.HandleDeath(this, killer);
            }

            Guilds.Guild.HandleDeath(this, killer);

            MLQuestSystem.HandleDeath(this);

            DuelContext?.OnDeath(this, c);

            if (m_BuffTable != null)
            {
                using var queue = PooledRefQueue<BuffInfo>.Create();

                foreach (var buff in m_BuffTable.Values)
                {
                    if (!buff.RetainThroughDeath)
                    {
                        queue.Enqueue(buff);
                    }
                }

                while (queue.Count > 0)
                {
                    RemoveBuff(queue.Dequeue());
                }
            }
        }

        public override bool MutateSpeech(List<Mobile> hears, ref string text, ref object context)
        {
            if (Alive)
            {
                return false;
            }

            if (Core.ML && Skills.SpiritSpeak.Value >= 100.0)
            {
                return false;
            }

            if (Core.AOS)
            {
                for (var i = 0; i < hears.Count; ++i)
                {
                    var m = hears[i];

                    if (m != this && m.Skills.SpiritSpeak.Value >= 100.0)
                    {
                        return false;
                    }
                }
            }

            return base.MutateSpeech(hears, ref text, ref context);
        }

        public override void DoSpeech(string text, int[] keywords, MessageType type, int hue)
        {
            if (Guilds.Guild.NewGuildSystem && type is MessageType.Guild or MessageType.Alliance)
            {
                if (Guild is not Guild g)
                {
                    SendLocalizedMessage(1063142); // You are not in a guild!
                }
                else if (type == MessageType.Alliance)
                {
                    if (g.Alliance?.IsMember(g) == true)
                    {
                        // g.Alliance.AllianceTextMessage( hue, "[Alliance][{0}]: {1}", this.Name, text );
                        g.Alliance.AllianceChat(this, text);
                        SendToStaffMessage(this, $"[Alliance]: {text}");

                        AllianceMessageHue = hue;
                    }
                    else
                    {
                        SendLocalizedMessage(1071020); // You are not in an alliance!
                    }
                }
                else // Type == MessageType.Guild
                {
                    GuildMessageHue = hue;

                    g.GuildChat(this, text);
                    SendToStaffMessage(this, $"[Guild]: {text}");
                }
            }
            else
            {
                base.DoSpeech(text, keywords, type, hue);
            }
        }

        private static void SendToStaffMessage(Mobile from, string text)
        {
            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();

            foreach (var ns in from.GetClientsInRange(8))
            {
                var mob = ns.Mobile;

                if (mob?.AccessLevel >= AccessLevel.GameMaster && mob.AccessLevel > from.AccessLevel)
                {
                    var length = OutgoingMessagePackets.CreateMessage(
                        buffer,
                        from.Serial,
                        from.Body,
                        MessageType.Regular,
                        from.SpeechHue,
                        3,
                        false,
                        from.Language,
                        from.Name,
                        text
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    ns.Send(buffer);
                }
            }
        }

        public override void Damage(int amount, Mobile from = null, bool informMount = true)
        {
            var damageBonus = 1.0;

            if (EvilOmenSpell.EndEffect(this) && !PainSpikeSpell.UnderEffect(this))
            {
                damageBonus += 0.25;
            }

            var hasBloodOath = false;

            if (from != null)
            {
                if (Talisman is BaseTalisman talisman &&
                    talisman.Protection?.Type?.IsInstanceOfType(from) == true)
                {
                    damageBonus -= talisman.Protection.Amount / 100.0;
                }

                // Is the attacker attacking the blood oath caster?
                if (BloodOathSpell.GetBloodOath(from) == this)
                {
                    hasBloodOath = true;
                    damageBonus += 0.2;
                }
            }

            base.Damage((int)(amount * damageBonus), from, informMount);

            // If the blood oath caster will die then damage is not reflected back to the attacker
            if (hasBloodOath && Alive && !Deleted && !IsDeadBondedPet)
            {
                // In some expansions resisting spells reduces reflect dmg from monster blood oath
                var resistReflectedDamage = !from.Player && Core.ML && !Core.HS
                    ? (from.Skills.MagicResist.Value * 0.5 + 10) / 100
                    : 0;

                // Reflect damage to the attacker
                from.Damage((int)(amount * (1.0 - resistReflectedDamage)), this);
            }
        }

        public override bool IsHarmfulCriminal(Mobile target)
        {
            if (Stealing.ClassicMode && target is PlayerMobile mobile && mobile.PermaFlags.Count > 0)
            {
                if (Notoriety.Compute(this, mobile) == Notoriety.Innocent)
                {
                    mobile.Delta(MobileDelta.Noto);
                }

                return false;
            }

            var bc = target as BaseCreature;

            if (bc?.InitialInnocent == true && !bc.Controlled)
            {
                return false;
            }

            if (Core.ML && bc?.Controlled == true && this == bc.ControlMaster)
            {
                return false;
            }

            return base.IsHarmfulCriminal(target);
        }

        private void RevertHair()
        {
            SetHairMods(-1, -1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            VirtueContext virtues = version < 32 ? Virtues : null;

            switch (version)
            {
                case 34: // Acquired Recipes is now a Set
                case 33: // Removes champion title
                case 32: // Removes virtue properties
                case 31: // Removed Short/Long Term Elapse
                case 30:
                    {
                        Stabled = reader.ReadEntitySet<Mobile>(true);
                        goto case 29;
                    }
                case 29:
                    {
                        if (reader.ReadBool())
                        {
                            m_StuckMenuUses = new DateTime[reader.ReadInt()];

                            for (var i = 0; i < m_StuckMenuUses.Length; ++i)
                            {
                                m_StuckMenuUses[i] = reader.ReadDateTime();
                            }
                        }
                        else
                        {
                            m_StuckMenuUses = null;
                        }

                        goto case 28;
                    }
                case 28:
                    {
                        PeacedUntil = reader.ReadDateTime();

                        goto case 27;
                    }
                case 27:
                    {
                        AnkhNextUse = reader.ReadDateTime();

                        goto case 26;
                    }
                case 26:
                    {
                        AutoStabled = reader.ReadEntitySet<Mobile>(true);

                        goto case 25;
                    }
                case 25:
                    {
                        var recipeCount = reader.ReadInt();

                        if (recipeCount > 0)
                        {
                            _acquiredRecipes = new HashSet<int>();

                            for (var i = 0; i < recipeCount; i++)
                            {
                                var r = reader.ReadInt();
                                if (version > 33 || reader.ReadBool()) // Don't add in recipes which we haven't gotten or have been removed
                                {
                                    _acquiredRecipes.Add(r);
                                }
                            }
                        }

                        goto case 24;
                    }
                case 24:
                    {
                        if (version < 32)
                        {
                            reader.ReadDeltaTime(); // LastHonorLoss - Not even used
                        }
                        goto case 23;
                    }
                case 23:
                    {
                        if (version < 33)
                        {
                            ChampionTitles.Deserialize(reader);
                        }

                        goto case 22;
                    }
                case 22:
                    {
                        if (version < 32)
                        {
                            virtues.LastValorLoss = reader.ReadDateTime();
                        }

                        goto case 21;
                    }
                case 21:
                    {
                        ToTItemsTurnedIn = reader.ReadEncodedInt();
                        ToTTotalMonsterFame = reader.ReadInt();
                        goto case 20;
                    }
                case 20:
                    {
                        AllianceMessageHue = reader.ReadEncodedInt();
                        GuildMessageHue = reader.ReadEncodedInt();

                        goto case 19;
                    }
                case 19:
                    {
                        var rank = reader.ReadEncodedInt();
                        var maxRank = RankDefinition.Ranks.Length - 1;
                        if (rank > maxRank)
                        {
                            rank = maxRank;
                        }

                        m_GuildRank = RankDefinition.Ranks[rank];
                        LastOnline = reader.ReadDateTime();
                        goto case 18;
                    }
                case 18:
                    {
                        SolenFriendship = (SolenFriendship)reader.ReadEncodedInt();

                        goto case 17;
                    }
                case 17: // changed how DoneQuests is serialized
                case 16:
                    {
                        Quest = QuestSerializer.DeserializeQuest(reader);

                        if (Quest != null)
                        {
                            Quest.From = this;
                        }

                        var count = reader.ReadEncodedInt();

                        if (count > 0)
                        {
                            DoneQuests = new List<QuestRestartInfo>();

                            for (var i = 0; i < count; ++i)
                            {
                                var questType = QuestSerializer.ReadType(QuestSystem.QuestTypes, reader);
                                DateTime restartTime;

                                if (version < 17)
                                {
                                    restartTime = DateTime.MaxValue;
                                }
                                else
                                {
                                    restartTime = reader.ReadDateTime();
                                }

                                DoneQuests.Add(new QuestRestartInfo(questType, restartTime));
                            }
                        }

                        Profession = reader.ReadEncodedInt();
                        goto case 15;
                    }
                case 15:
                    {
                        if (version < 32)
                        {
                            virtues.LastCompassionLoss = reader.ReadDeltaTime();
                        }
                        goto case 14;
                    }
                case 14:
                    {
                        if (version < 32)
                        {
                            virtues.CompassionGains = reader.ReadEncodedInt();
                            if (virtues.CompassionGains > 0)
                            {
                                virtues.NextCompassionDay = reader.ReadDeltaTime();
                            }
                        }

                        goto case 13;
                    }
                case 13: // just removed m_PaidInsurance list
                case 12:
                    {
                        BOBFilter = new BOBFilter();
                        BOBFilter.Deserialize(reader);
                        goto case 11;
                    }
                case 11:
                    {
                        if (version < 13)
                        {
                            var paid = reader.ReadEntityList<Item>();

                            for (var i = 0; i < paid.Count; ++i)
                            {
                                paid[i].PaidInsurance = true;
                            }
                        }

                        goto case 10;
                    }
                case 10:
                    {
                        if (reader.ReadBool())
                        {
                            m_HairModID = reader.ReadInt();
                            m_HairModHue = reader.ReadInt();
                            m_BeardModID = reader.ReadInt();
                            m_BeardModHue = reader.ReadInt();
                        }

                        goto case 9;
                    }
                case 9:
                    {
                        SavagePaintExpiration = reader.ReadTimeSpan();

                        if (SavagePaintExpiration > TimeSpan.Zero)
                        {
                            BodyMod = Female ? 184 : 183;
                            HueMod = 0;
                        }

                        goto case 8;
                    }
                case 8:
                    {
                        NpcGuild = (NpcGuild)reader.ReadInt();
                        NpcGuildJoinTime = reader.ReadDateTime();
                        NpcGuildGameTime = reader.ReadTimeSpan();
                        goto case 7;
                    }
                case 7:
                    {
                        PermaFlags = reader.ReadEntityList<Mobile>();
                        goto case 6;
                    }
                case 6:
                    {
                        NextTailorBulkOrder = reader.ReadTimeSpan();
                        goto case 5;
                    }
                case 5:
                    {
                        NextSmithBulkOrder = reader.ReadTimeSpan();
                        goto case 4;
                    }
                case 4:
                    {
                        if (version < 32)
                        {
                            virtues.LastJusticeLoss = reader.ReadDeltaTime();
                            var protectors = reader.ReadEntityList<PlayerMobile>(); // Always a list of 0, or 1
                            if (protectors.Count > 0)
                            {
                                var protector = protectors[0];
                                if (protector != null)
                                {
                                    JusticeVirtue.AddProtection(protector, this);
                                }
                            }
                        }

                        goto case 3;
                    }
                case 3:
                    {
                        if (version < 32)
                        {
                            virtues.LastSacrificeGain = reader.ReadDeltaTime();
                            virtues.LastSacrificeLoss = reader.ReadDeltaTime();
                            virtues.AvailableResurrects = reader.ReadInt();
                        }

                        goto case 2;
                    }
                case 2:
                    {
                        Flags = (PlayerFlag)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 31)
                        {
                            var longTermElapse = reader.ReadTimeSpan();
                            var shortTermElapse = reader.ReadTimeSpan();

                            PlayerMurderSystem.MigrateContext(this, shortTermElapse, longTermElapse);
                        }

                        m_GameTime = reader.ReadTimeSpan();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            if (!CharacterCreation.VerifyProfession(Profession))
            {
                Profession = 0;
            }

            PermaFlags ??= new List<Mobile>();
            BOBFilter ??= new BOBFilter();

            // Default to member if going from older version to new version (only time it should be null)
            m_GuildRank ??= RankDefinition.Member;

            if (LastOnline == DateTime.MinValue && Account != null)
            {
                LastOnline = ((Account)Account).LastLogin;
            }

            if (AccessLevel > AccessLevel.Player)
            {
                IgnoreMobiles = true;
            }

            if (Stabled != null)
            {
                foreach (var stabled in Stabled)
                {
                    if (stabled is BaseCreature bc)
                    {
                        bc.IsStabled = true;
                        bc.StabledBy = this;
                    }
                }
            }

            if (Hidden) // Hiding is the only buff where it has an effect that's serialized.
            {
                AddBuff(new BuffInfo(BuffIcon.HidingAndOrStealth, 1075655));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(34); // version

            if (Stabled == null)
            {
                writer.Write(0);
            }
            else
            {
                Stabled.Tidy();
                writer.Write(Stabled);
            }

            if (m_StuckMenuUses != null)
            {
                writer.Write(true);

                writer.Write(m_StuckMenuUses.Length);

                for (var i = 0; i < m_StuckMenuUses.Length; ++i)
                {
                    writer.Write(m_StuckMenuUses[i]);
                }
            }
            else
            {
                writer.Write(false);
            }

            writer.Write(PeacedUntil);
            writer.Write(AnkhNextUse);
            if (AutoStabled == null)
            {
                writer.Write(0);
            }
            else
            {
                AutoStabled.Tidy();
                writer.Write(AutoStabled);
            }

            if (_acquiredRecipes == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(_acquiredRecipes.Count);

                foreach (var recipeId in _acquiredRecipes)
                {
                    writer.Write(recipeId);
                }
            }

            writer.WriteEncodedInt(ToTItemsTurnedIn);
            writer.Write(ToTTotalMonsterFame); // This ain't going to be a small #.

            writer.WriteEncodedInt(AllianceMessageHue);
            writer.WriteEncodedInt(GuildMessageHue);

            writer.WriteEncodedInt(m_GuildRank.Rank);
            writer.Write(LastOnline);

            writer.WriteEncodedInt((int)SolenFriendship);

            QuestSerializer.Serialize(Quest, writer);

            if (DoneQuests == null)
            {
                writer.WriteEncodedInt(0);
            }
            else
            {
                writer.WriteEncodedInt(DoneQuests.Count);

                for (var i = 0; i < DoneQuests.Count; ++i)
                {
                    var restartInfo = DoneQuests[i];

                    QuestSerializer.Write(restartInfo.QuestType, QuestSystem.QuestTypes, writer);
                    writer.Write(restartInfo.RestartTime);
                }
            }

            writer.WriteEncodedInt(Profession);

            BOBFilter.Serialize(writer);

            var useMods = m_HairModID != -1 || m_BeardModID != -1;

            writer.Write(useMods);

            if (useMods)
            {
                writer.Write(m_HairModID);
                writer.Write(m_HairModHue);
                writer.Write(m_BeardModID);
                writer.Write(m_BeardModHue);
            }

            writer.Write(SavagePaintExpiration);

            writer.Write((int)NpcGuild);
            writer.Write(NpcGuildJoinTime);
            writer.Write(NpcGuildGameTime);

            PermaFlags.Tidy();
            writer.Write(PermaFlags);

            writer.Write(NextTailorBulkOrder);

            writer.Write(NextSmithBulkOrder);

            writer.Write((int)Flags);

            writer.Write(GameTime);
        }

        public override bool CanSee(Mobile m)
        {
            if (m is CharacterStatue statue)
            {
                statue.OnRequestedAnimation(this);
            }

            if (m is PlayerMobile mobile && mobile.VisibilityList.Contains(this))
            {
                return true;
            }

            if (DuelContext?.Finished == false && DuelContext.m_Tournament != null && m_DuelPlayer?.Eliminated == false)
            {
                var owner = m;

                if (owner is BaseCreature bc)
                {
                    var master = bc.GetMaster();

                    if (master != null)
                    {
                        owner = master;
                    }
                }

                if (m.AccessLevel == AccessLevel.Player && owner is PlayerMobile pm && pm.DuelContext != DuelContext)
                {
                    return false;
                }
            }

            return base.CanSee(m);
        }

        public virtual void CheckedAnimate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
        {
            if (!Mounted)
            {
                Animate(action, frameCount, repeatCount, forward, repeat, delay);
            }
        }

        public override bool CanSee(Item item) =>
            DesignContext?.Foundation.IsHiddenToCustomizer(item) != true && base.CanSee(item);

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            var faction = Faction.Find(this);

            faction?.RemoveMember(this);

            MLQuestSystem.HandleDeletion(this);

            BaseHouse.HandleDeletion(this);

            DisguisePersistence.RemoveTimer(this);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Map == Faction.Facet)
            {
                var pl = PlayerState.Find(this);

                if (pl != null)
                {
                    var faction = pl.Faction;

                    if (faction.Commander == this)
                    {
                        // Commanding Lord of the ~1_FACTION_NAME~
                        list.Add(1042733, $"{faction.Definition.PropName}");
                    }
                    else if (pl.Sheriff != null)
                    {
                        list.Add(
                            1042734, // The Sheriff of  ~1_CITY~, ~2_FACTION_NAME~
                            $"{pl.Sheriff.Definition.FriendlyName}\t{faction.Definition.PropName}"
                        );
                    }
                    else if (pl.Finance != null)
                    {
                        list.Add(
                            1042735, // The Finance Minister of ~1_CITY~, ~2_FACTION_NAME~
                            $"{pl.Finance.Definition.FriendlyName}\t{faction.Definition.PropName}"
                        );
                    }
                    else if (pl.MerchantTitle != MerchantTitle.None)
                    {
                        list.Add(
                            1060776, // ~1_val~, ~2_val~
                            $"{MerchantTitles.GetInfo(pl.MerchantTitle).Title}\t{faction.Definition.PropName}"
                        );
                    }
                    else
                    {
                        list.Add(1060776, $"{pl.Rank.Title}\t{faction.Definition.PropName}"); // ~1_val~, ~2_val~
                    }
                }
            }

            // TODO: Add the Titles Menu for later Eras:
            // https://uo.com/wiki/ultima-online-wiki/player/skill-titles-order/
            if (DisplayChampionTitle)
            {
                var titleLabel = ChampionTitleSystem.GetChampionTitleLabel(this);
                if (titleLabel > 0)
                {
                    list.Add(titleLabel);
                }
            }

            if (Core.ML && AllFollowers != null)
            {
                foreach (var follower in AllFollowers)
                {
                    if (follower is BaseCreature { ControlOrder: OrderType.Guard })
                    {
                        list.Add(501129); // guarded
                        break;
                    }
                }
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Map == Faction.Facet)
            {
                var pl = PlayerState.Find(this);

                if (pl != null)
                {
                    string text;
                    var ascii = false;

                    var faction = pl.Faction;

                    if (faction.Commander == this)
                    {
                        text =
                            $"{(Female ? "(Commanding Lady of the " : "(Commanding Lord of the ")}{faction.Definition.FriendlyName})";
                    }
                    else if (pl.Sheriff != null)
                    {
                        text = $"(The Sheriff of {pl.Sheriff.Definition.FriendlyName}, {faction.Definition.FriendlyName})";
                    }
                    else if (pl.Finance != null)
                    {
                        text =
                            $"(The Finance Minister of {pl.Finance.Definition.FriendlyName}, {faction.Definition.FriendlyName})";
                    }
                    else
                    {
                        ascii = true;

                        var title = MerchantTitles.GetInfo(pl.MerchantTitle)?.Title?.String ?? pl.Rank.Title.String;
                        text = $"({title}, {faction.Definition.FriendlyName})";
                    }

                    var hue = Faction.Find(from) == faction ? 98 : 38;

                    PrivateOverheadMessage(MessageType.Label, hue, ascii, text, from.NetState);
                }
            }

            base.OnSingleClick(from);
        }

        protected override bool OnMove(Direction d)
        {
            if (!Core.SE)
            {
                return base.OnMove(d);
            }

            if (AccessLevel != AccessLevel.Player)
            {
                return true;
            }

            if (Hidden && DesignContext.Find(this) == null) // Hidden & NOT customizing a house
            {
                if (!Mounted && Skills.Stealth.Value >= 25.0)
                {
                    var running = (d & Direction.Running) != 0;

                    if (running)
                    {
                        if ((AllowedStealthSteps -= 2) <= 0)
                        {
                            RevealingAction();
                        }
                    }
                    else if (AllowedStealthSteps-- <= 0)
                    {
                        Stealth.OnUse(this);
                    }
                }
                else
                {
                    RevealingAction();
                }
            }

            return true;
        }

        public void AddFollower(Mobile m)
        {
            _allFollowers ??= new HashSet<Mobile>();
            _allFollowers.Add(m);
        }

        public void AddStabled(Mobile m)
        {
            Stabled ??= new HashSet<Mobile>();
            Stabled.Add(m);
        }

        public bool RemoveStabled(Mobile m)
        {
            if (Stabled?.Remove(m) == true)
            {
                if (Stabled.Count == 0)
                {
                    Stabled = null;
                }

                return true;
            }

            return false;
        }

        public bool RemoveFollower(Mobile m)
        {
            if (_allFollowers?.Remove(m) == true)
            {
                if (_allFollowers.Count == 0)
                {
                    _allFollowers = null;
                }

                return true;
            }

            return false;
        }

        public void AutoStablePets()
        {
            var allFollowers = _allFollowers;

            if (!Core.SE || !(allFollowers?.Count > 0))
            {
                return;
            }

            foreach (var follower in allFollowers)
            {
                if (follower is not BaseCreature pet || pet.ControlMaster == null)
                {
                    continue;
                }

                if (pet.Summoned)
                {
                    if (pet.Map != Map)
                    {
                        pet.PlaySound(pet.GetAngerSound());
                        Timer.StartTimer(pet.Delete);
                    }

                    continue;
                }

                if ((pet as IMount)?.Rider != null)
                {
                    continue;
                }

                if (pet is PackLlama or PackHorse or Beetle && pet.Backpack?.Items.Count > 0)
                {
                    continue;
                }

                if (pet is BaseEscortable)
                {
                    continue;
                }

                pet.ControlTarget = null;
                pet.ControlOrder = OrderType.Stay;
                pet.Internalize();

                pet.SetControlMaster(null);
                pet.SummonMaster = null;

                pet.IsStabled = true;
                pet.StabledBy = this;

                pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully happy

                Stabled ??= new HashSet<Mobile>();
                Stabled.Add(pet);

                AutoStabled ??= new HashSet<Mobile>();
                AutoStabled.Add(pet);
            }
        }

        public void ClaimAutoStabledPets()
        {
            if (!Core.SE || !(AutoStabled?.Count > 0))
            {
                return;
            }

            if (!Alive)
            {
                // Your pet was unable to join you while you are a ghost.  Please re-login once you have ressurected to claim your pets.
                SendLocalizedMessage(1076251);
                return;
            }

            foreach (var stabled in AutoStabled)
            {
                if (stabled is not BaseCreature pet)
                {
                    continue;
                }

                if (pet.Deleted)
                {
                    pet.IsStabled = false;
                    pet.StabledBy = null;

                    Stabled?.Remove(pet);
                    continue;
                }

                if (Followers + pet.ControlSlots <= FollowersMax)
                {
                    pet.SetControlMaster(this);

                    if (pet.Summoned)
                    {
                        pet.SummonMaster = this;
                    }

                    pet.ControlTarget = this;
                    pet.ControlOrder = OrderType.Follow;

                    pet.MoveToWorld(Location, Map);

                    pet.IsStabled = false;
                    pet.StabledBy = null;

                    pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully Happy

                    Stabled?.Remove(pet);
                }
                else
                {
                    // ~1_NAME~ remained in the stables because you have too many followers.
                    SendLocalizedMessage(1049612, pet.Name);
                }
            }

            AutoStabled = null;
        }

        public void RecoverAmmo()
        {
            if (!Core.SE || !Alive || RecoverableAmmo == null)
            {
                return;
            }

            foreach (var kvp in RecoverableAmmo)
            {
                if (kvp.Value > 0)
                {
                    Item ammo = null;

                    try
                    {
                        ammo = kvp.Key.CreateInstance<Item>();
                    }
                    catch
                    {
                        // ignored
                    }

                    if (ammo == null)
                    {
                        continue;
                    }

                    ammo.Amount = kvp.Value;

                    var name = ammo.Name ?? ammo switch
                    {
                        Arrow _ => $"arrow{(ammo.Amount != 1 ? "s" : "")}",
                        Bolt _  => $"bolt{(ammo.Amount != 1 ? "s" : "")}",
                        _       => $"#{ammo.LabelNumber}"
                    };

                    PlaceInBackpack(ammo);
                    SendLocalizedMessage(1073504, $"{ammo.Amount}\t{name}"); // You recover ~1_NUM~ ~2_AMMO~.
                }
            }

            RecoverableAmmo.Clear();
        }

        private static int GetInsuranceCost(Item item) => 600;

        private void ToggleItemInsurance()
        {
            if (!CheckAlive())
            {
                return;
            }

            BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
            SendLocalizedMessage(1060868); // Target the item you wish to toggle insurance status on <ESC> to cancel
        }

        private bool CanInsure(Item item)
        {
            if (item is Container && item is not BaseQuiver || item is BagOfSending or KeyRing or PotionKeg or Sigil)
            {
                return false;
            }

            if (item.Stackable)
            {
                return false;
            }

            if (item.LootType == LootType.Cursed)
            {
                return false;
            }

            if (item.ItemID == 0x204E) // death shroud
            {
                return false;
            }

            if (item.Layer == Layer.Mount)
            {
                return false;
            }

            return item.LootType != LootType.Blessed && item.LootType != LootType.Newbied && item.BlessedFor != this;
        }

        private void ToggleItemInsurance_Callback(Mobile from, object obj)
        {
            if (!CheckAlive())
            {
                return;
            }

            ToggleItemInsurance_Callback(from, obj as Item, true);
        }

        private void ToggleItemInsurance_Callback(Mobile from, Item item, bool target)
        {
            if (item?.IsChildOf(this) != true)
            {
                if (target)
                {
                    BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
                }

                SendLocalizedMessage(
                    1060871,
                    "",
                    0x23
                ); // You can only insure items that you have equipped or that are in your backpack
            }
            else if (item.Insured)
            {
                item.Insured = false;

                SendLocalizedMessage(1060874, "", 0x35); // You cancel the insurance on the item

                if (target)
                {
                    BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
                    SendLocalizedMessage(
                        1060868,
                        "",
                        0x23
                    ); // Target the item you wish to toggle insurance status on <ESC> to cancel
                }
            }
            else if (!CanInsure(item))
            {
                if (target)
                {
                    BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
                }

                SendLocalizedMessage(1060869, "", 0x23); // You cannot insure that
            }
            else
            {
                if (!item.PaidInsurance)
                {
                    var cost = GetInsuranceCost(item);

                    if (Banker.Withdraw(from, cost))
                    {
                        SendLocalizedMessage(
                            1060398,
                            cost.ToString()
                        ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                        item.PaidInsurance = true;
                    }
                    else
                    {
                        SendLocalizedMessage(1061079, "", 0x23); // You lack the funds to purchase the insurance
                        return;
                    }
                }

                item.Insured = true;

                SendLocalizedMessage(1060873, "", 0x23); // You have insured the item

                if (target)
                {
                    BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
                    SendLocalizedMessage(
                        1060868,
                        "",
                        0x23
                    ); // Target the item you wish to toggle insurance status on <ESC> to cancel
                }
            }
        }

        private void AutoRenewInventoryInsurance()
        {
            if (!CheckAlive())
            {
                return;
            }

            SendLocalizedMessage(
                1060881,
                "",
                0x23
            ); // You have selected to automatically reinsure all insured items upon death
            AutoRenewInsurance = true;
        }

        private void CancelRenewInventoryInsurance()
        {
            if (!CheckAlive())
            {
                return;
            }

            if (Core.SE)
            {
                if (!HasGump<CancelRenewInventoryInsuranceGump>())
                {
                    SendGump(new CancelRenewInventoryInsuranceGump(this, null));
                }
            }
            else
            {
                SendLocalizedMessage(
                    1061075,
                    "",
                    0x23
                ); // You have cancelled automatically reinsuring all insured items upon death
                AutoRenewInsurance = false;
            }
        }

        private void OpenItemInsuranceMenu()
        {
            if (!CheckAlive())
            {
                return;
            }

            using var queue = PooledRefQueue<Item>.Create(128);

            foreach (var item in Items)
            {
                if (DisplayInItemInsuranceGump(item))
                {
                    queue.Enqueue(item);
                }
            }

            var pack = Backpack;

            if (pack != null)
            {
                foreach (var item in pack.FindItems())
                {
                    if (DisplayInItemInsuranceGump(item))
                    {
                        queue.Enqueue(item);
                    }
                }
            }

            // TODO: Investigate item sorting

            CloseGump<ItemInsuranceMenuGump>();

            if (queue.Count == 0)
            {
                SendLocalizedMessage(1114915, "", 0x35); // None of your current items meet the requirements for insurance.
            }
            else
            {
                SendGump(new ItemInsuranceMenuGump(this, queue.ToArray()));
            }
        }

        private bool DisplayInItemInsuranceGump(Item item) => (item.Visible || AccessLevel >= AccessLevel.GameMaster) &&
                                                              (item.Insured || CanInsure(item));

        private void ToggleQuestItem()
        {
            if (!CheckAlive())
            {
                return;
            }

            ToggleQuestItemTarget();
        }

        private void ToggleQuestItemTarget()
        {
            BaseQuestGump.CloseOtherGumps(this);
            CloseGump<QuestLogDetailedGump>();
            CloseGump<QuestLogGump>();
            CloseGump<QuestOfferGump>();
            // CloseGump( typeof( UnknownGump802 ) );
            // CloseGump( typeof( UnknownGump804 ) );

            BeginTarget(-1, false, TargetFlags.None, ToggleQuestItem_Callback);
            SendLocalizedMessage(1072352); // Target the item you wish to toggle Quest Item status on <ESC> to cancel
        }

        private void ToggleQuestItem_Callback(Mobile from, object obj)
        {
            if (!CheckAlive())
            {
                return;
            }

            if (obj is not Item item)
            {
                return;
            }

            if (from.Backpack == null || item.Parent != from.Backpack)
            {
                // An item must be in your backpack (and not in a container within) to be toggled as a quest item.
                SendLocalizedMessage(1074769);
            }
            else if (item.QuestItem)
            {
                item.QuestItem = false;
                SendLocalizedMessage(1072354); // You remove Quest Item status from the item
            }
            else if (MLQuestSystem.MarkQuestItem(this, item))
            {
                SendLocalizedMessage(1072353); // You set the item to Quest Item status
            }
            else
            {
                // That item does not match any of your quest criteria
                SendLocalizedMessage(1072355, "", 0x23);
            }

            ToggleQuestItemTarget();
        }

        public bool CanUseStuckMenu()
        {
            if (m_StuckMenuUses == null)
            {
                return true;
            }

            for (var i = 0; i < m_StuckMenuUses.Length; ++i)
            {
                if (Core.Now - m_StuckMenuUses[i] > TimeSpan.FromDays(1.0))
                {
                    return true;
                }
            }

            return false;
        }

        public void UsedStuckMenu()
        {
            if (m_StuckMenuUses == null)
            {
                m_StuckMenuUses = new DateTime[2];
            }

            for (var i = 0; i < m_StuckMenuUses.Length; ++i)
            {
                if (Core.Now - m_StuckMenuUses[i] > TimeSpan.FromDays(1.0))
                {
                    m_StuckMenuUses[i] = Core.Now;
                    return;
                }
            }
        }

        public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (!Alive)
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
            Young && (DuelContext?.Started != true || DuelContext.Finished) || base.CheckPoisonImmunity(from, poison);

        public override void OnPoisonImmunity(Mobile from, Poison poison)
        {
            if (Young && (DuelContext?.Started != true || DuelContext.Finished))
            {
                // You would have been poisoned, were you not new to the land of Britannia.
                // Be careful in the future.
                SendLocalizedMessage(502808);
            }
            else
            {
                base.OnPoisonImmunity(from, poison);
            }
        }

        public override void OnKillsChange(int oldValue)
        {
            if (Young && Kills > oldValue)
            {
                ((Account)Account)?.RemoveYoungStatus(0);
            }
        }

        public override void OnGenderChanged(bool oldFemale)
        {
        }

        public override void OnGuildChange(BaseGuild oldGuild)
        {
        }

        public override void OnGuildTitleChange(string oldTitle)
        {
        }

        public override void OnKarmaChange(int oldValue)
        {
        }

        public override void OnFameChange(int oldValue)
        {
        }

        public override void OnSkillChange(SkillName skill, double oldBase)
        {
            if (Young && SkillsTotal >= 4500)
            {
                // You have successfully obtained a respectable skill level, and have outgrown your status as a young player!
                ((Account)Account)?.RemoveYoungStatus(1019036);
            }

            if (MLQuestSystem.Enabled)
            {
                MLQuestSystem.HandleSkillGain(this, skill);
            }
        }

        public override void OnAccessLevelChanged(AccessLevel oldLevel)
        {
            IgnoreMobiles = AccessLevel != AccessLevel.Player;
        }

        public override void OnRawStatChange(StatType stat, int oldValue)
        {
        }

        public override void OnDelete()
        {
            ReceivedHonorContext?.Cancel();
            SentHonorContext?.Cancel();

            if (Stabled != null)
            {
                foreach (var stabled in Stabled)
                {
                    stabled.Delete();
                }

                Stabled = null;
            }
        }

        public override int ComputeMovementSpeed(Direction dir, bool checkTurning = true)
        {
            if (checkTurning && (dir & Direction.Mask) != (Direction & Direction.Mask))
            {
                return CalcMoves.TurnDelay; // We are NOT actually moving (just a direction change)
            }

            var context = TransformationSpellHelper.GetContext(this);

            if (context?.Type == typeof(ReaperFormSpell))
            {
                return CalcMoves.WalkFootDelay;
            }

            var running = (dir & Direction.Running) != 0;

            var onHorse = Mount != null;

            if (onHorse || AnimalForm.GetContext(this)?.SpeedBoost == true)
            {
                return running ? CalcMoves.RunMountDelay : CalcMoves.WalkMountDelay;
            }

            return running ? CalcMoves.RunFootDelay : CalcMoves.WalkFootDelay;
        }

        private void DeltaEnemies(Type oldType, Type newType)
        {
            foreach (var m in GetMobilesInRange(18))
            {
                var t = m.GetType();

                if (t == oldType || t == newType)
                {
                    m.NetState.SendMobileMoving(this, m);
                }
            }
        }

        public void SetHairMods(int hairID, int beardID)
        {
            if (hairID == -1)
            {
                InternalRestoreHair(true, ref m_HairModID, ref m_HairModHue);
            }
            else if (hairID != -2)
            {
                InternalChangeHair(true, hairID, ref m_HairModID, ref m_HairModHue);
            }

            if (beardID == -1)
            {
                InternalRestoreHair(false, ref m_BeardModID, ref m_BeardModHue);
            }
            else if (beardID != -2)
            {
                InternalChangeHair(false, beardID, ref m_BeardModID, ref m_BeardModHue);
            }
        }

        private void CreateHair(bool hair, int id, int hue)
        {
            if (hair)
            {
                // TODO Verification?
                HairItemID = id;
                HairHue = hue;
            }
            else
            {
                FacialHairItemID = id;
                FacialHairHue = hue;
            }
        }

        private void InternalRestoreHair(bool hair, ref int id, ref int hue)
        {
            if (id == -1)
            {
                return;
            }

            if (hair)
            {
                HairItemID = 0;
            }
            else
            {
                FacialHairItemID = 0;
            }

            // if (id != 0)
            CreateHair(hair, id, hue);

            id = -1;
            hue = 0;
        }

        private void InternalChangeHair(bool hair, int id, ref int storeID, ref int storeHue)
        {
            if (storeID == -1)
            {
                storeID = hair ? HairItemID : FacialHairItemID;
                storeHue = hair ? HairHue : FacialHairHue;
            }

            CreateHair(hair, id, 0);
        }

        public override string ApplyNameSuffix(string suffix)
        {
            if (Young)
            {
                suffix = suffix.Length == 0 ? "(Young)" : $"{suffix} (Young)";
            }

            if (EthicPlayer != null)
            {
                if (suffix.Length == 0)
                {
                    suffix = EthicPlayer.Ethic.Definition.Adjunct.String;
                }
                else
                {
                    suffix = $"{suffix} {EthicPlayer.Ethic.Definition.Adjunct.String}";
                }
            }

            if (Core.ML && Map == Faction.Facet)
            {
                var faction = Faction.Find(this);

                if (faction != null)
                {
                    var adjunct = $"[{faction.Definition.Abbreviation}]";
                    suffix = suffix.Length == 0 ? adjunct : $"{suffix} {adjunct}";
                }
            }

            return base.ApplyNameSuffix(suffix);
        }

        public override TimeSpan GetLogoutDelay()
        {
            if (Young || BedrollLogout || TestCenter.Enabled)
            {
                return TimeSpan.Zero;
            }

            return base.GetLogoutDelay();
        }

        public bool CheckYoungProtection(Mobile from)
        {
            if (!Young)
            {
                return false;
            }

            if (Region is BaseRegion region && !region.YoungProtected)
            {
                return false;
            }

            if (from is BaseCreature creature && creature.IgnoreYoungProtection)
            {
                return false;
            }

            if (Quest?.IgnoreYoungProtection(from) == true)
            {
                return false;
            }

            if (Core.Now - m_LastYoungMessage > TimeSpan.FromMinutes(1.0))
            {
                m_LastYoungMessage = Core.Now;
                // A monster looks at you menacingly but does not attack.
                // You would be under attack now if not for your status as a new citizen of Britannia.
                SendLocalizedMessage(1019067);
            }

            return true;
        }

        public bool CheckYoungHealTime()
        {
            if (Core.Now - m_LastYoungHeal > TimeSpan.FromMinutes(5.0))
            {
                m_LastYoungHeal = Core.Now;
                return true;
            }

            return false;
        }

        public bool YoungDeathTeleport()
        {
            if (Region.IsPartOf<JailRegion>()
                || Region.IsPartOf("Samurai start location")
                || Region.IsPartOf("Ninja start location")
                || Region.IsPartOf("Ninja cave"))
            {
                return false;
            }

            Point3D loc;
            Map map;

            var dungeon = Region.GetRegion<DungeonRegion>();
            if (dungeon != null && dungeon.Entrance != Point3D.Zero)
            {
                loc = dungeon.Entrance;
                map = dungeon.Map;
            }
            else
            {
                loc = Location;
                map = Map;
            }

            Point3D[] list;

            if (map == Map.Trammel)
            {
                list = m_TrammelDeathDestinations;
            }
            else if (map == Map.Ilshenar)
            {
                list = m_IlshenarDeathDestinations;
            }
            else if (map == Map.Malas)
            {
                list = m_MalasDeathDestinations;
            }
            else if (map == Map.Tokuno)
            {
                list = m_TokunoDeathDestinations;
            }
            else
            {
                return false;
            }

            var dest = Point3D.Zero;
            var sqDistance = int.MaxValue;

            for (var i = 0; i < list.Length; i++)
            {
                var curDest = list[i];

                var width = loc.X - curDest.X;
                var height = loc.Y - curDest.Y;
                var curSqDistance = width * width + height * height;

                if (curSqDistance < sqDistance)
                {
                    dest = curDest;
                    sqDistance = curSqDistance;
                }
            }

            MoveToWorld(dest, map);
            return true;
        }

        private void SendYoungDeathNotice()
        {
            SendGump(new YoungDeathNotice());
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (SpeechLog.Enabled && NetState != null)
            {
                if (SpeechLog == null)
                {
                    SpeechLog = new SpeechLog();
                }

                SpeechLog.Add(e.Mobile, e.Speech);
            }
        }

        private void ToggleChampionTitleDisplay()
        {
            if (!CheckAlive())
            {
                return;
            }

            if (DisplayChampionTitle)
            {
                // You have chosen to hide your monster kill title.
                SendLocalizedMessage(1062419, "", 0x23);
            }
            else
            {
                // You have chosen to display your monster kill title.
                SendLocalizedMessage(1062418, "", 0x23);
            }

            DisplayChampionTitle = !DisplayChampionTitle;
            InvalidateProperties();
        }

        public bool HasRecipe(Recipe r) => r != null && HasRecipe(r.ID);

        public bool HasRecipe(int recipeID) => _acquiredRecipes?.Contains(recipeID) == true;

        public void AcquireRecipe(Recipe r)
        {
            if (r != null)
            {
                AcquireRecipe(r.ID);
            }
        }

        public void AcquireRecipe(int recipeID)
        {
            _acquiredRecipes ??= new HashSet<int>();
            _acquiredRecipes.Add(recipeID);
        }

        public void RemoveRecipe(int recipeID) => _acquiredRecipes?.Remove(recipeID);

        public void ResetRecipes() => _acquiredRecipes = null;

        public void ResendBuffs()
        {
            if (BuffInfo.Enabled && m_BuffTable != null && NetState?.BuffIcon == true)
            {
                foreach (var info in m_BuffTable.Values)
                {
                    info.SendAddBuffPacket(NetState, Serial);
                }
            }
        }

        public void AddBuff(BuffInfo b)
        {
            if (!BuffInfo.Enabled || b == null)
            {
                return;
            }

            RemoveBuff(b); // Check & subsequently remove the old one.

            m_BuffTable ??= new Dictionary<BuffIcon, BuffInfo>();

            m_BuffTable.Add(b.ID, b);

            if (NetState?.BuffIcon == true)
            {
                // Synchronize the buff icon as close to _on the second_ as we can.
                var msecs = b.TimeLength.Milliseconds;
                if (msecs >= 8)
                {
                    Timer.DelayCall(TimeSpan.FromMilliseconds(msecs), (buffInfo, pm) =>
                    {
                        // They are still online, we still have the buff icon in the table, and it is the same buff icon
                        if (pm.NetState != null && pm.m_BuffTable.TryGetValue(buffInfo.ID, out var checkBuff) && checkBuff == buffInfo)
                        {
                            buffInfo.SendAddBuffPacket(pm.NetState, pm.Serial);
                        }
                    }, b, this);
                }
                else
                {
                    b.SendAddBuffPacket(NetState, Serial);
                }
            }
        }

        public void RemoveBuff(BuffInfo b)
        {
            if (b == null)
            {
                return;
            }

            RemoveBuff(b.ID);
        }

        public void RemoveBuff(BuffIcon b)
        {
            if (m_BuffTable?.Remove(b) != true)
            {
                return;
            }

            if (NetState?.BuffIcon == true)
            {
                BuffInfo.SendRemoveBuffPacket(NetState, Serial, b);
            }

            if (m_BuffTable.Count <= 0)
            {
                m_BuffTable = null;
            }
        }

        private class MountBlock
        {
            private TimerExecutionToken _timerToken;
            private BlockMountType _type;

            public MountBlock(TimeSpan duration, BlockMountType type, Mobile mobile)
            {
                _type = type;

                if (duration < TimeSpan.MaxValue)
                {
                    Timer.StartTimer(duration, () => RemoveBlock(mobile), out _timerToken);
                }
            }

            public DateTime Expiration => _timerToken.Next;

            public BlockMountType MountBlockReason => CheckBlock() ? _type : BlockMountType.None;

            public bool CheckBlock() => _timerToken.Next == DateTime.MinValue || _timerToken.Running;

            public void RemoveBlock(Mobile mobile)
            {
                if (mobile is PlayerMobile pm)
                {
                    pm._mountBlock = null;
                }

                _timerToken.Cancel();
            }
        }

        private delegate void ContextCallback();

        private class CallbackEntry : ContextMenuEntry
        {
            private readonly ContextCallback m_Callback;

            public CallbackEntry(int number, ContextCallback callback) : this(number, -1, callback)
            {
            }

            public CallbackEntry(int number, int range, ContextCallback callback) : base(number, range) =>
                m_Callback = callback;

            public override void OnClick()
            {
                m_Callback?.Invoke();
            }
        }

        private class CancelRenewInventoryInsuranceGump : Gump
        {
            private readonly ItemInsuranceMenuGump m_InsuranceGump;
            private readonly PlayerMobile m_Player;

            public CancelRenewInventoryInsuranceGump(PlayerMobile player, ItemInsuranceMenuGump insuranceGump) : base(
                250,
                200
            )
            {
                m_Player = player;
                m_InsuranceGump = insuranceGump;

                AddBackground(0, 0, 240, 142, 0x13BE);
                AddImageTiled(6, 6, 228, 100, 0xA40);
                AddImageTiled(6, 116, 228, 20, 0xA40);
                AddAlphaRegion(6, 6, 228, 142);

                AddHtmlLocalized(
                    8,
                    8,
                    228,
                    100,
                    1071021,
                    0x7FFF
                ); // You are about to disable inventory insurance auto-renewal.

                AddButton(6, 116, 0xFB1, 0xFB2, 0);
                AddHtmlLocalized(40, 118, 450, 20, 1060051, 0x7FFF); // CANCEL

                AddButton(114, 116, 0xFA5, 0xFA7, 1);
                AddHtmlLocalized(148, 118, 450, 20, 1071022, 0x7FFF); // DISABLE IT!
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (!m_Player.CheckAlive())
                {
                    return;
                }

                if (info.ButtonID == 1)
                {
                    // You have cancelled automatically reinsuring all insured items upon death
                    m_Player.SendLocalizedMessage(1061075, "", 0x23);
                    m_Player.AutoRenewInsurance = false;
                }
                else
                {
                    m_Player.SendLocalizedMessage(1042021); // Cancelled.
                }

                if (m_InsuranceGump != null)
                {
                    m_Player.SendGump(m_InsuranceGump.NewInstance());
                }
            }
        }

        private class ItemInsuranceMenuGump : Gump
        {
            private readonly PlayerMobile m_From;
            private readonly bool[] m_Insure;
            private readonly Item[] m_Items;
            private readonly int m_Page;

            public ItemInsuranceMenuGump(PlayerMobile from, Item[] items, bool[] insure = null, int page = 0)
                : base(25, 50)
            {
                m_From = from;
                m_Items = items;

                if (insure == null)
                {
                    insure = new bool[items.Length];

                    for (var i = 0; i < items.Length; ++i)
                    {
                        insure[i] = items[i].Insured;
                    }
                }

                m_Insure = insure;
                m_Page = page;

                AddPage(0);

                AddBackground(0, 0, 520, 510, 0x13BE);
                AddImageTiled(10, 10, 500, 30, 0xA40);
                AddImageTiled(10, 50, 500, 355, 0xA40);
                AddImageTiled(10, 415, 500, 80, 0xA40);
                AddAlphaRegion(10, 10, 500, 485);

                AddButton(15, 470, 0xFB1, 0xFB2, 0);
                AddHtmlLocalized(50, 472, 80, 20, 1011012, 0x7FFF); // CANCEL

                if (from.AutoRenewInsurance)
                {
                    AddButton(360, 10, 9723, 9724, 1);
                }
                else
                {
                    AddButton(360, 10, 9720, 9722, 1);
                }

                AddHtmlLocalized(395, 14, 105, 20, 1114122, 0x7FFF); // AUTO REINSURE

                AddButton(395, 470, 0xFA5, 0xFA6, 2);
                AddHtmlLocalized(430, 472, 50, 20, 1006044, 0x7FFF); // OK

                AddHtmlLocalized(10, 14, 150, 20, 1114121, 0x7FFF); // <CENTER>ITEM INSURANCE MENU</CENTER>

                AddHtmlLocalized(45, 54, 70, 20, 1062214, 0x7FFF);  // Item
                AddHtmlLocalized(250, 54, 70, 20, 1061038, 0x7FFF); // Cost
                AddHtmlLocalized(400, 54, 70, 20, 1114311, 0x7FFF); // Insured

                var balance = Banker.GetBalance(from);
                var cost = 0;

                for (var i = 0; i < items.Length; ++i)
                {
                    if (insure[i])
                    {
                        cost += GetInsuranceCost(items[i]);
                    }
                }

                AddHtmlLocalized(15, 420, 300, 20, 1114310, 0x7FFF); // GOLD AVAILABLE:
                AddLabel(215, 420, 0x481, balance.ToString());
                AddHtmlLocalized(15, 435, 300, 20, 1114123, 0x7FFF); // TOTAL COST OF INSURANCE:
                AddLabel(215, 435, 0x481, cost.ToString());

                if (cost != 0)
                {
                    AddHtmlLocalized(15, 450, 300, 20, 1114125, 0x7FFF); // NUMBER OF DEATHS PAYABLE:
                    AddLabel(215, 450, 0x481, (balance / cost).ToString());
                }

                for (int i = page * 4, y = 72; i < (page + 1) * 4 && i < items.Length; ++i, y += 75)
                {
                    var item = items[i];
                    var b = ItemBounds.Table[item.ItemID];

                    AddImageTiledButton(
                        40,
                        y,
                        0x918,
                        0x918,
                        0,
                        GumpButtonType.Page,
                        0,
                        item.ItemID,
                        item.Hue,
                        40 - b.Width / 2 - b.X,
                        30 - b.Height / 2 - b.Y
                    );
                    AddItemProperty(item.Serial);

                    if (insure[i])
                    {
                        AddButton(400, y, 9723, 9724, 100 + i);
                        AddLabel(250, y, 0x481, GetInsuranceCost(item).ToString());
                    }
                    else
                    {
                        AddButton(400, y, 9720, 9722, 100 + i);
                        AddLabel(250, y, 0x66C, GetInsuranceCost(item).ToString());
                    }
                }

                if (page >= 1)
                {
                    AddButton(15, 380, 0xFAE, 0xFAF, 3);
                    AddHtmlLocalized(50, 380, 450, 20, 1044044, 0x7FFF); // PREV PAGE
                }

                if ((page + 1) * 4 < items.Length)
                {
                    AddButton(400, 380, 0xFA5, 0xFA7, 4);
                    AddHtmlLocalized(435, 380, 70, 20, 1044045, 0x7FFF); // NEXT PAGE
                }
            }

            public ItemInsuranceMenuGump NewInstance() => new(m_From, m_Items, m_Insure, m_Page);

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID == 0 || !m_From.CheckAlive())
                {
                    return;
                }

                switch (info.ButtonID)
                {
                    case 1: // Auto Reinsure
                        {
                            if (m_From.AutoRenewInsurance)
                            {
                                if (!m_From.HasGump<CancelRenewInventoryInsuranceGump>())
                                {
                                    m_From.SendGump(new CancelRenewInventoryInsuranceGump(m_From, this));
                                }
                            }
                            else
                            {
                                m_From.AutoRenewInventoryInsurance();
                                m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page));
                            }

                            break;
                        }
                    case 2: // OK
                        {
                            m_From.SendGump(new ItemInsuranceMenuConfirmGump(m_From, m_Items, m_Insure, m_Page));

                            break;
                        }
                    case 3: // Prev
                        {
                            if (m_Page >= 1)
                            {
                                m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page - 1));
                            }

                            break;
                        }
                    case 4: // Next
                        {
                            if ((m_Page + 1) * 4 < m_Items.Length)
                            {
                                m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page + 1));
                            }

                            break;
                        }
                    default:
                        {
                            var idx = info.ButtonID - 100;

                            if (idx >= 0 && idx < m_Items.Length)
                            {
                                m_Insure[idx] = !m_Insure[idx];
                            }

                            m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page));

                            break;
                        }
                }
            }
        }

        private class ItemInsuranceMenuConfirmGump : Gump
        {
            private readonly PlayerMobile m_From;
            private readonly bool[] m_Insure;
            private readonly Item[] m_Items;
            private readonly int m_Page;

            public ItemInsuranceMenuConfirmGump(PlayerMobile from, Item[] items, bool[] insure, int page)
                : base(250, 200)
            {
                m_From = from;
                m_Items = items;
                m_Insure = insure;
                m_Page = page;

                AddBackground(0, 0, 240, 142, 0x13BE);
                AddImageTiled(6, 6, 228, 100, 0xA40);
                AddImageTiled(6, 116, 228, 20, 0xA40);
                AddAlphaRegion(6, 6, 228, 142);

                AddHtmlLocalized(8, 8, 228, 100, 1114300, 0x7FFF); // Do you wish to insure all newly selected items?

                AddButton(6, 116, 0xFB1, 0xFB2, 0);
                AddHtmlLocalized(40, 118, 450, 20, 1060051, 0x7FFF); // CANCEL

                AddButton(114, 116, 0xFA5, 0xFA7, 1);
                AddHtmlLocalized(148, 118, 450, 20, 1073996, 0x7FFF); // ACCEPT
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (!m_From.CheckAlive())
                {
                    return;
                }

                if (info.ButtonID == 1)
                {
                    for (var i = 0; i < m_Items.Length; ++i)
                    {
                        var item = m_Items[i];

                        if (item.Insured != m_Insure[i])
                        {
                            m_From.ToggleItemInsurance_Callback(m_From, item, false);
                        }
                    }
                }
                else
                {
                    m_From.SendLocalizedMessage(1042021); // Cancelled.
                    m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page));
                }
            }
        }
    }
}
