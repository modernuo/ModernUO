using System;
using System.Buffers;
using Server.Mobiles;
using Server.Network;

namespace Server
{
    public class BuffInfo
    {
        private TimerExecutionToken _timerToken;

        public BuffInfo(BuffIcon iconID, int titleCliloc)
            : this(iconID, titleCliloc, titleCliloc + 1)
        {
        }

        public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc)
        {
            ID = iconID;
            TitleCliloc = titleCliloc;
            SecondaryCliloc = secondaryCliloc;
        }

        public BuffInfo(BuffIcon iconID, int titleCliloc, TimeSpan length, Mobile m)
            : this(iconID, titleCliloc, titleCliloc + 1, length, m)
        {
        }

        // Only the timed one needs the Mobile to know when to automagically remove it.
        public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan length, Mobile m)
            : this(iconID, titleCliloc, secondaryCliloc)
        {
            TimeLength = length;
            TimeStart = Core.TickCount;

            Timer.StartTimer(length, () => RemoveBuff(m, this), out _timerToken);
        }

        public BuffInfo(BuffIcon iconID, int titleCliloc, TextDefinition args)
            : this(iconID, titleCliloc, titleCliloc + 1, args)
        {
        }

        public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TextDefinition args)
            : this(iconID, titleCliloc, secondaryCliloc) =>
            Args = args;

        public BuffInfo(BuffIcon iconID, int titleCliloc, bool retainThroughDeath)
            : this(iconID, titleCliloc, titleCliloc + 1, retainThroughDeath)
        {
        }

        public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, bool retainThroughDeath)
            : this(iconID, titleCliloc, secondaryCliloc) =>
            RetainThroughDeath = retainThroughDeath;

        public BuffInfo(BuffIcon iconID, int titleCliloc, TextDefinition args, bool retainThroughDeath)
            : this(iconID, titleCliloc, titleCliloc + 1, args, retainThroughDeath)
        {
        }

        public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TextDefinition args, bool retainThroughDeath)
            : this(iconID, titleCliloc, secondaryCliloc, args) =>
            RetainThroughDeath = retainThroughDeath;

        public BuffInfo(BuffIcon iconID, int titleCliloc, TimeSpan length, Mobile m, TextDefinition args)
            : this(iconID, titleCliloc, titleCliloc + 1, length, m, args)
        {
        }

        public BuffInfo(
            BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan length, Mobile m,
            TextDefinition args
        )
            : this(iconID, titleCliloc, secondaryCliloc, length, m) =>
            Args = args;

        public BuffInfo(
            BuffIcon iconID, int titleCliloc, TimeSpan length, Mobile m, TextDefinition args,
            bool retainThroughDeath
        )
            : this(iconID, titleCliloc, titleCliloc + 1, length, m, args, retainThroughDeath)
        {
        }

        public BuffInfo(
            BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan length, Mobile m,
            TextDefinition args, bool retainThroughDeath
        )
            : this(iconID, titleCliloc, secondaryCliloc, length, m)
        {
            Args = args;
            RetainThroughDeath = retainThroughDeath;
        }

        public static bool Enabled { get; private set; }

        public BuffIcon ID { get; }

        public int TitleCliloc { get; }

        public int SecondaryCliloc { get; }

        public TimeSpan TimeLength { get; }

        public long TimeStart { get; }

        public TimerExecutionToken TimerToken => _timerToken;

        public bool RetainThroughDeath { get; }

        public TextDefinition Args { get; }

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("buffIcons.enable", Core.ML);
        }

        public static void Initialize()
        {
            if (Enabled)
            {
                EventSink.ClientVersionReceived += ResendBuffsOnClientVersionReceived;
            }
        }

        public static void ResendBuffsOnClientVersionReceived(NetState ns, ClientVersion cv)
        {
            if (ns.Mobile is PlayerMobile pm)
            {
                Timer.StartTimer(pm.ResendBuffs);
            }
        }

        public static void AddBuff(Mobile m, BuffInfo b)
        {
            (m as PlayerMobile)?.AddBuff(b);
        }

        public static void RemoveBuff(Mobile m, BuffInfo b)
        {
            (m as PlayerMobile)?.RemoveBuff(b);
        }

        public static void RemoveBuff(Mobile m, BuffIcon b)
        {
            (m as PlayerMobile)?.RemoveBuff(b);
        }

        public void SendAddBuffPacket(NetState ns, Serial m) => SendAddBuffPacket(
            ns,
            m,
            ID,
            TitleCliloc,
            SecondaryCliloc,
            Args,
            TimeStart == 0 ? 0 : Math.Max(TimeStart + (long)TimeLength.TotalMilliseconds - Core.TickCount, 0)
        );

        public static void SendAddBuffPacket(
            NetState ns, Serial mob, BuffIcon iconID, int titleCliloc, int secondaryCliloc, TextDefinition args,
            long ticks
        )
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var hasArgs = args != null;
            var length = hasArgs ? args.ToString().Length * 2 + 52 : 46;
            var writer = new SpanWriter(stackalloc byte[length]);
            writer.Write((byte)0xDF); // Packet ID
            writer.Write((ushort)length);
            writer.Write(mob);
            writer.Write((short)iconID);
            writer.Write((short)0x1); // command (0 = remove, 1 = add, 2 = data)
            writer.Write(0);

            writer.Write((short)iconID);
            writer.Write((short)0x1); // command (0 = remove, 1 = add, 2 = data)
            writer.Write(0);
            writer.Write((short)(ticks / 1000));
            writer.Clear(3);
            writer.Write(titleCliloc);
            writer.Write(secondaryCliloc);

            if (hasArgs)
            {
                writer.Write(0);
                writer.Write((short)0x1);
                writer.Write((ushort)0);
                writer.WriteLE('\t');
                writer.WriteLittleUniNull(args);
                writer.Write((short)0x1);
                writer.Write((ushort)0);
            }
            else
            {
                writer.Clear(10);
            }

            ns.Send(writer.Span);
        }

        public void SendRemoveBuffPacket(NetState ns, Serial mob) => SendRemoveBuffPacket(ns, mob, ID);

        public static void SendRemoveBuffPacket(NetState ns, Serial mob, BuffIcon iconID)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[15]);
            writer.Write((byte)0xDF); // Packet ID
            writer.Write((ushort)15);
            writer.Write(mob);
            writer.Write((short)iconID);
            writer.Write((short)0x0); // command (0 = remove, 1 = add, 2 = data)
            writer.Write(0);

            ns.Send(writer.Span);
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
        DivineFury,			//*
        EnemyOfOne,			//*
        HidingAndOrStealth,	//*
        ActiveMeditation,	//*
        BloodOathCaster,	//*
        BloodOathCurse,		//*
        CorpseSkin,			//*
        Mindrot,			//*
        PainSpike,			//*
        Strangle,
        GiftOfRenewal,		//*
        AttuneWeapon,		//*
        Thunderstorm,		//*
        EssenceOfWind,		//*
        EtherealVoyage,		//*
        GiftOfLife,			//*
        ArcaneEmpowerment,	//*
        MortalStrike,
        ReactiveArmor,		//*
        Protection,			//*
        ArchProtection,
        MagicReflection,	//*
        Incognito,			//*
        Disguised,
        AnimalForm,
        Polymorph,
        Invisibility,		//*
        Paralyze,			//*
        Poison,
        Bleed,
        Clumsy,				//*
        FeebleMind,			//*
        Weaken,				//*
        Curse,				//*
        MassCurse,
        Agility,			//*
        Cunning,			//*
        Strength,			//*
        Bless,				//*
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
}
