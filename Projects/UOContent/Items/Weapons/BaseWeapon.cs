using System;
using System.Collections.Generic;
using Server.Collections;
using Server.Engines.Craft;
using Server.Ethics;
using Server.Factions;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Chivalry;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Sixth;
using Server.Spells.Spellweaving;

namespace Server.Items
{
    public interface ISlayer
    {
        SlayerName Slayer { get; set; }
        SlayerName Slayer2 { get; set; }
    }

    public abstract class BaseWeapon : Item, IWeapon, IFactionItem, ICraftable, ISlayer, IDurability, IAosItem
    {
        private static bool _enableInstaHit;

        public static void Configure()
        {
            _enableInstaHit = ServerConfiguration.GetSetting("melee.enableInstaHit", !Core.UOR);
        }

        private WeaponAccuracyLevel m_AccuracyLevel;
        private WeaponAnimation m_Animation;
        private Mobile m_Crafter;

        /* Weapon internals work differently now (Mar 13 2003)
         *
         * The attributes defined below default to -1.
         * If the value is -1, the corresponding virtual 'Aos/Old' property is used.
         * If not, the attribute value itself is used. Here's the list:
         *  - MinDamage
         *  - MaxDamage
         *  - Speed
         *  - HitSound
         *  - MissSound
         *  - StrRequirement, DexRequirement, IntRequirement
         *  - WeaponType
         *  - WeaponAnimation
         *  - MaxRange
         */

        // Instance values. These values are unique to each weapon.
        private WeaponDamageLevel m_DamageLevel;
        private WeaponDurabilityLevel m_DurabilityLevel;
        private string m_EngravedText;

        private FactionItem m_FactionState;
        private int m_Hits;
        private int m_HitSound, m_MissSound;
        private bool m_Identified;
        private int m_MaxHits;
        private int m_MaxRange;
        private int m_MinDamage, m_MaxDamage;
        private Poison m_Poison;
        private int m_PoisonCharges;
        private WeaponQuality m_Quality;
        private CraftResource m_Resource;
        private SkillName m_Skill;
        private SkillMod m_SkillMod, m_MageMod;
        private SlayerName m_Slayer;
        private SlayerName m_Slayer2;
        private float m_Speed;

        // Overridable values. These values are provided to override the defaults which get defined in the individual weapon scripts.
        private int m_StrReq, m_DexReq, m_IntReq;
        private WeaponType m_Type;

        public BaseWeapon(int itemID) : base(itemID)
        {
            Layer = (Layer)ItemData.Quality;

            m_Quality = WeaponQuality.Regular;
            m_StrReq = -1;
            m_DexReq = -1;
            m_IntReq = -1;
            m_MinDamage = -1;
            m_MaxDamage = -1;
            m_HitSound = -1;
            m_MissSound = -1;
            m_Speed = -1;
            m_MaxRange = -1;
            m_Skill = (SkillName)(-1);
            m_Type = (WeaponType)(-1);
            m_Animation = (WeaponAnimation)(-1);

            m_Hits = m_MaxHits = Utility.RandomMinMax(InitMinHits, InitMaxHits);

            m_Resource = CraftResource.Iron;

            Attributes = new AosAttributes(this);
            WeaponAttributes = new AosWeaponAttributes(this);
            SkillBonuses = new AosSkillBonuses(this);
            AosElementDamages = new AosElementAttributes(this);
        }

        public BaseWeapon(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string EngravedText
        {
            get => m_EngravedText;
            set
            {
                m_EngravedText = value;
                InvalidateProperties();
            }
        }

        public virtual bool UseSkillMod => !Core.AOS;

        public static bool InDoubleStrike { get; set; }

        public virtual int VirtualDamageBonus => 0;

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                base.Hue = value;
                InvalidateProperties();
            }
        }

        public virtual int ArtifactRarity => 0;

        public static BaseWeapon Fists { get; set; }

        public virtual WeaponAbility PrimaryAbility => null;
        public virtual WeaponAbility SecondaryAbility => null;

        public virtual int DefMaxRange => 1;
        public virtual int DefHitSound => 0;
        public virtual int DefMissSound => 0;
        public virtual SkillName DefSkill => SkillName.Swords;
        public virtual WeaponType DefType => WeaponType.Slashing;
        public virtual WeaponAnimation DefAnimation => WeaponAnimation.Slash1H;

        public virtual int AosStrengthReq => 0;
        public virtual int AosDexterityReq => 0;
        public virtual int AosIntelligenceReq => 0;
        public virtual int AosMinDamage => 0;
        public virtual int AosMaxDamage => 0;
        public virtual int AosSpeed => 0;
        public virtual float MlSpeed => 0.0f;
        public virtual int AosMaxRange => DefMaxRange;
        public virtual int AosHitSound => DefHitSound;
        public virtual int AosMissSound => DefMissSound;
        public virtual SkillName AosSkill => DefSkill;
        public virtual WeaponType AosType => DefType;
        public virtual WeaponAnimation AosAnimation => DefAnimation;

        public virtual int OldStrengthReq => 0;
        public virtual int OldDexterityReq => 0;
        public virtual int OldIntelligenceReq => 0;
        public virtual int OldMinDamage => 0;
        public virtual int OldMaxDamage => 0;
        public virtual int OldSpeed => 0;
        public virtual int OldMaxRange => DefMaxRange;
        public virtual int OldHitSound => DefHitSound;
        public virtual int OldMissSound => DefMissSound;
        public virtual SkillName OldSkill => DefSkill;
        public virtual WeaponType OldType => DefType;
        public virtual WeaponAnimation OldAnimation => DefAnimation;

        public override int PhysicalResistance => WeaponAttributes.ResistPhysicalBonus;
        public override int FireResistance => WeaponAttributes.ResistFireBonus;
        public override int ColdResistance => WeaponAttributes.ResistColdBonus;
        public override int PoisonResistance => WeaponAttributes.ResistPoisonBonus;
        public override int EnergyResistance => WeaponAttributes.ResistEnergyBonus;

        public virtual SkillName AccuracySkill => SkillName.Tactics;

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosAttributes Attributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosWeaponAttributes WeaponAttributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosSkillBonuses SkillBonuses { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosElementAttributes AosElementDamages { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Cursed { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Consecrated { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Identified
        {
            get => m_Identified;
            set
            {
                m_Identified = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonCharges
        {
            get => m_PoisonCharges;
            set
            {
                m_PoisonCharges = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get => m_Poison;
            set
            {
                m_Poison = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponQuality Quality
        {
            get => m_Quality;
            set
            {
                UnscaleDurability();
                m_Quality = value;
                ScaleDurability();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get => m_Crafter;
            set
            {
                m_Crafter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                UnscaleDurability();
                m_Resource = value;
                Hue = CraftResources.GetHue(m_Resource);
                InvalidateProperties();
                ScaleDurability();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponDamageLevel DamageLevel
        {
            get => m_DamageLevel;
            set
            {
                m_DamageLevel = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponDurabilityLevel DurabilityLevel
        {
            get => m_DurabilityLevel;
            set
            {
                UnscaleDurability();
                m_DurabilityLevel = value;
                InvalidateProperties();
                ScaleDurability();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PlayerConstructed { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponAnimation Animation
        {
            get => m_Animation == (WeaponAnimation)(-1) ? Core.AOS ? AosAnimation : OldAnimation : m_Animation;
            set => m_Animation = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponType Type
        {
            get => m_Type == (WeaponType)(-1) ? Core.AOS ? AosType : OldType : m_Type;
            set => m_Type = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill
        {
            get => m_Skill == (SkillName)(-1) ? Core.AOS ? AosSkill : OldSkill : m_Skill;
            set
            {
                m_Skill = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitSound
        {
            get => m_HitSound == -1 ? Core.AOS ? AosHitSound : OldHitSound : m_HitSound;
            set => m_HitSound = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MissSound
        {
            get => m_MissSound == -1 ? Core.AOS ? AosMissSound : OldMissSound : m_MissSound;
            set => m_MissSound = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MinDamage
        {
            get => m_MinDamage == -1 ? Core.AOS ? AosMinDamage : OldMinDamage : m_MinDamage;
            set
            {
                m_MinDamage = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxDamage
        {
            get => m_MaxDamage == -1 ? Core.AOS ? AosMaxDamage : OldMaxDamage : m_MaxDamage;
            set
            {
                m_MaxDamage = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Speed
        {
            get
            {
                if (m_Speed != -1)
                {
                    return m_Speed;
                }

                if (Core.ML)
                {
                    return MlSpeed;
                }

                if (Core.AOS)
                {
                    return AosSpeed;
                }

                return OldSpeed;
            }
            set
            {
                m_Speed = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StrRequirement
        {
            get => m_StrReq == -1 ? Core.AOS ? AosStrengthReq : OldStrengthReq : m_StrReq;
            set
            {
                m_StrReq = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DexRequirement
        {
            get => m_DexReq == -1 ? Core.AOS ? AosDexterityReq : OldDexterityReq : m_DexReq;
            set => m_DexReq = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int IntRequirement
        {
            get => m_IntReq == -1 ? Core.AOS ? AosIntelligenceReq : OldIntelligenceReq : m_IntReq;
            set => m_IntReq = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponAccuracyLevel AccuracyLevel
        {
            get => m_AccuracyLevel;
            set
            {
                if (m_AccuracyLevel != value)
                {
                    m_AccuracyLevel = value;

                    if (UseSkillMod)
                    {
                        if (m_AccuracyLevel == WeaponAccuracyLevel.Regular)
                        {
                            m_SkillMod?.Remove();

                            m_SkillMod = null;
                        }
                        else if (m_SkillMod == null && Parent is Mobile mobile)
                        {
                            m_SkillMod = new DefaultSkillMod(AccuracySkill, "WeaponAccuracy", true, (int)m_AccuracyLevel * 5);
                            mobile.AddSkillMod(m_SkillMod);
                        }
                        else if (m_SkillMod != null)
                        {
                            m_SkillMod.Value = (int)m_AccuracyLevel * 5;
                        }
                    }

                    InvalidateProperties();
                }
            }
        }

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            Quality = (WeaponQuality)quality;

            if (makersMark)
            {
                Crafter = from;
            }

            PlayerConstructed = true;

            var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

            if (Core.AOS)
            {
                Resource = CraftResources.GetFromType(resourceType);

                var context = craftSystem.GetContext(from);

                if (context?.DoNotColor == true)
                {
                    Hue = 0;
                }

                if (tool is BaseRunicTool runicTool)
                {
                    runicTool.ApplyAttributesTo(this);
                }

                if (Quality == WeaponQuality.Exceptional)
                {
                    Attributes.WeaponDamage = 35;

                    if (Core.ML)
                    {
                        Attributes.WeaponDamage += (int)(from.Skills.ArmsLore.Value / 20);

                        if (Attributes.WeaponDamage > 50)
                        {
                            Attributes.WeaponDamage = 50;
                        }

                        from.CheckSkill(SkillName.ArmsLore, 0, 100);
                    }
                }
            }
            else if (tool is BaseRunicTool runicTool)
            {
                var thisResource = CraftResources.GetFromType(resourceType);

                if (thisResource == runicTool.Resource)
                {
                    Resource = thisResource;

                    var context = craftSystem.GetContext(from);

                    if (context?.DoNotColor == true)
                    {
                        Hue = 0;
                    }

                    switch (thisResource)
                    {
                        case CraftResource.DullCopper:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Durable;
                                AccuracyLevel = WeaponAccuracyLevel.Accurate;
                                break;
                            }
                        case CraftResource.ShadowIron:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Durable;
                                DamageLevel = WeaponDamageLevel.Ruin;
                                break;
                            }
                        case CraftResource.Copper:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Fortified;
                                DamageLevel = WeaponDamageLevel.Ruin;
                                AccuracyLevel = WeaponAccuracyLevel.Surpassingly;
                                break;
                            }
                        case CraftResource.Bronze:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Fortified;
                                DamageLevel = WeaponDamageLevel.Might;
                                AccuracyLevel = WeaponAccuracyLevel.Surpassingly;
                                break;
                            }
                        case CraftResource.Gold:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Indestructible;
                                DamageLevel = WeaponDamageLevel.Force;
                                AccuracyLevel = WeaponAccuracyLevel.Eminently;
                                break;
                            }
                        case CraftResource.Agapite:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Indestructible;
                                DamageLevel = WeaponDamageLevel.Power;
                                AccuracyLevel = WeaponAccuracyLevel.Eminently;
                                break;
                            }
                        case CraftResource.Verite:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Indestructible;
                                DamageLevel = WeaponDamageLevel.Power;
                                AccuracyLevel = WeaponAccuracyLevel.Exceedingly;
                                break;
                            }
                        case CraftResource.Valorite:
                            {
                                Identified = true;
                                DurabilityLevel = WeaponDurabilityLevel.Indestructible;
                                DamageLevel = WeaponDamageLevel.Vanq;
                                AccuracyLevel = WeaponAccuracyLevel.Supremely;
                                break;
                            }
                    }
                }
            }

            return quality;
        }

        public virtual void UnscaleDurability()
        {
            var scale = 100 + GetDurabilityBonus();

            m_Hits = (m_Hits * 100 + (scale - 1)) / scale;
            m_MaxHits = (m_MaxHits * 100 + (scale - 1)) / scale;
            InvalidateProperties();
        }

        public virtual void ScaleDurability()
        {
            var scale = 100 + GetDurabilityBonus();

            m_Hits = (m_Hits * scale + 99) / 100;
            m_MaxHits = (m_MaxHits * scale + 99) / 100;
            InvalidateProperties();
        }

        public virtual int InitMinHits => 0;
        public virtual int InitMaxHits => 0;

        public virtual bool CanFortify => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitPoints
        {
            get => m_Hits;
            set
            {
                if (m_Hits == value)
                {
                    return;
                }

                if (value > m_MaxHits)
                {
                    value = m_MaxHits;
                }

                m_Hits = value;

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHitPoints
        {
            get => m_MaxHits;
            set
            {
                m_MaxHits = value;
                InvalidateProperties();
            }
        }

        public FactionItem FactionItemState
        {
            get => m_FactionState;
            set
            {
                m_FactionState = value;

                if (m_FactionState == null)
                {
                    Hue = CraftResources.GetHue(Resource);
                }

                LootType = m_FactionState == null ? LootType.Regular : LootType.Blessed;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SlayerName Slayer
        {
            get => m_Slayer;
            set
            {
                m_Slayer = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SlayerName Slayer2
        {
            get => m_Slayer2;
            set
            {
                m_Slayer2 = value;
                InvalidateProperties();
            }
        }

        public virtual void OnBeforeSwing(Mobile attacker, Mobile defender)
        {
            if (WeaponAbility.GetCurrentAbility(attacker)?.OnBeforeSwing(attacker, defender) == false)
            {
                WeaponAbility.ClearCurrentAbility(attacker);
            }

            if (SpecialMove.GetCurrentMove(attacker)?.OnBeforeSwing(attacker, defender) == false)
            {
                SpecialMove.ClearCurrentMove(attacker);
            }
        }

        public virtual void GetStatusDamage(Mobile from, out int min, out int max)
        {
            GetBaseDamageRange(from, out var baseMin, out var baseMax);

            if (Core.AOS)
            {
                min = Math.Max((int)ScaleDamageAOS(from, baseMin, false), 1);
                max = Math.Max((int)ScaleDamageAOS(from, baseMax, false), 1);
            }
            else
            {
                min = Math.Max((int)ScaleDamageOld(from, baseMin, false), 1);
                max = Math.Max((int)ScaleDamageOld(from, baseMax, false), 1);
            }
        }

        public virtual TimeSpan OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0)
        {
            var canSwing = true;

            if (Core.AOS)
            {
                canSwing = !attacker.Paralyzed && !attacker.Frozen;

                if (canSwing)
                {
                    canSwing = attacker.Spell is not Spell sp || !sp.IsCasting || !sp.BlocksMovement;
                }

                if (canSwing)
                {
                    canSwing = attacker is not PlayerMobile p || p.PeacedUntil <= Core.Now;
                }
            }

            if ((attacker as PlayerMobile)?.DuelContext?.CheckItemEquip(attacker, this) == false)
            {
                canSwing = false;
            }

            if (canSwing && attacker.HarmfulCheck(defender))
            {
                attacker.DisruptiveAction();

                attacker.NetState?.SendSwing(attacker.Serial, defender.Serial);

                if (attacker is BaseCreature bc)
                {
                    var ab = bc.GetWeaponAbility();

                    if (ab != null)
                    {
                        if (bc.WeaponAbilityChance > Utility.RandomDouble())
                        {
                            WeaponAbility.SetCurrentAbility(bc, ab);
                        }
                        else
                        {
                            WeaponAbility.ClearCurrentAbility(bc);
                        }
                    }
                }

                if (CheckHit(attacker, defender))
                {
                    OnHit(attacker, defender, damageBonus);
                }
                else
                {
                    OnMiss(attacker, defender);
                }
            }

            return GetDelay(attacker);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxRange
        {
            get => m_MaxRange == -1 ? Core.AOS ? AosMaxRange : OldMaxRange : m_MaxRange;
            set
            {
                m_MaxRange = value;
                InvalidateProperties();
            }
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is not BaseWeapon weap)
            {
                return;
            }

            weap.Attributes = new AosAttributes(newItem, Attributes);
            weap.AosElementDamages = new AosElementAttributes(newItem, AosElementDamages);
            weap.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
            weap.WeaponAttributes = new AosWeaponAttributes(newItem, WeaponAttributes);
        }

        public int GetDurabilityBonus()
        {
            var bonus = m_Quality == WeaponQuality.Exceptional ? 20 : 0;

            bonus += m_DurabilityLevel switch
            {
                WeaponDurabilityLevel.Durable        => 20,
                WeaponDurabilityLevel.Substantial    => 50,
                WeaponDurabilityLevel.Massive        => 70,
                WeaponDurabilityLevel.Fortified      => 100,
                WeaponDurabilityLevel.Indestructible => 120,
                _                                    => 0
            };

            if (Core.AOS)
            {
                bonus += WeaponAttributes.DurabilityBonus;

                var resInfo = CraftResources.GetInfo(m_Resource);
                CraftAttributeInfo attrInfo = null;

                if (resInfo != null)
                {
                    attrInfo = resInfo.AttributeInfo;
                }

                if (attrInfo != null)
                {
                    bonus += attrInfo.WeaponDurability;
                }
            }

            return bonus;
        }

        public int GetLowerStatReq()
        {
            if (!Core.AOS)
            {
                return 0;
            }

            var v = WeaponAttributes.LowerStatReq;

            var attrInfo = CraftResources.GetInfo(m_Resource)?.AttributeInfo;

            if (attrInfo != null)
            {
                v += attrInfo.WeaponLowerRequirements;
            }

            if (v > 100)
            {
                v = 100;
            }

            return v;
        }

        public static void BlockEquip(Mobile m, TimeSpan duration)
        {
            if (m.BeginAction<BaseWeapon>())
            {
                new ResetEquipTimer(m, duration).Start();
            }
        }

        public override bool CheckConflictingLayer(Mobile m, Item item, Layer layer)
        {
            if (base.CheckConflictingLayer(m, item, layer))
            {
                return true;
            }

            if (Layer == Layer.TwoHanded && layer == Layer.OneHanded)
            {
                m.SendLocalizedMessage(500214); // You already have something in both hands.
                return true;
            }

            if (Layer == Layer.OneHanded && layer == Layer.TwoHanded && item is not BaseShield &&
                item is not BaseEquipableLight)
            {
                m.SendLocalizedMessage(500215); // You can only wield one weapon at a time.
                return true;
            }

            return false;
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) =>
            Ethic.CheckTrade(from, to, newOwner, this) &&
            base.AllowSecureTrade(from, to, newOwner, accepted);

        public override bool CanEquip(Mobile from)
        {
            if (!Ethic.CheckEquip(from, this))
            {
                return false;
            }

            if (!CheckRace(from))
            {
                return false;
            }

            if (from.Dex < DexRequirement)
            {
                from.SendMessage("You are not nimble enough to equip that.");
                return false;
            }

            if (from.Str < AOS.Scale(StrRequirement, 100 - GetLowerStatReq()))
            {
                from.SendLocalizedMessage(500213); // You are not strong enough to equip that.
                return false;
            }

            if (from.Int < IntRequirement)
            {
                from.SendMessage("You are not smart enough to equip that.");
                return false;
            }

            return from.CanBeginAction<BaseWeapon>() && base.CanEquip(from);
        }

        public override bool OnEquip(Mobile from)
        {
            var strBonus = Attributes.BonusStr;
            var dexBonus = Attributes.BonusDex;
            var intBonus = Attributes.BonusInt;

            if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
            {
                var m = from;

                var serial = Serial;

                if (strBonus != 0)
                {
                    m.AddStatMod(new StatMod(StatType.Str, $"{serial}Str", strBonus, TimeSpan.Zero));
                }

                if (dexBonus != 0)
                {
                    m.AddStatMod(new StatMod(StatType.Dex, $"{serial}Dex", dexBonus, TimeSpan.Zero));
                }

                if (intBonus != 0)
                {
                    m.AddStatMod(new StatMod(StatType.Int, $"{serial}Int", intBonus, TimeSpan.Zero));
                }
            }

            if (!_enableInstaHit)
            {
                from.NextCombatTime = Core.TickCount + (int)GetDelay(from).TotalMilliseconds;
            }

            if (UseSkillMod && m_AccuracyLevel != WeaponAccuracyLevel.Regular)
            {
                m_SkillMod?.Remove();

                m_SkillMod = new DefaultSkillMod(AccuracySkill, "WeaponAccuracy", true, (int)m_AccuracyLevel * 5);
                from.AddSkillMod(m_SkillMod);
            }

            if (Core.AOS && WeaponAttributes.MageWeapon != 0 && WeaponAttributes.MageWeapon != 30)
            {
                m_MageMod?.Remove();

                m_MageMod = new DefaultSkillMod(SkillName.Magery, "MageWeapon", true, -30 + WeaponAttributes.MageWeapon);
                from.AddSkillMod(m_MageMod);
            }

            return true;
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile from)
            {
                if (Core.AOS)
                {
                    SkillBonuses.AddTo(from);
                }

                from.CheckStatTimers();
                from.Delta(MobileDelta.WeaponDamage);
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            if (parent is not Mobile m)
            {
                return;
            }

            var serial = Serial;

            m.RemoveStatMod($"{serial}Str");
            m.RemoveStatMod($"{serial}Dex");
            m.RemoveStatMod($"{serial}Int");

            if (!_enableInstaHit && m.Weapon is BaseWeapon weapon)
            {
                m.NextCombatTime = Core.TickCount + (long)weapon.GetDelay(m).TotalMilliseconds;
            }

            if (UseSkillMod && m_SkillMod != null)
            {
                m_SkillMod.Remove();
                m_SkillMod = null;
            }

            if (m_MageMod != null)
            {
                m_MageMod.Remove();
                m_MageMod = null;
            }

            if (Core.AOS)
            {
                SkillBonuses.Remove();
            }

            ImmolatingWeaponSpell.StopImmolating(this);
            ForceOfNature.Remove(m);

            m.CheckStatTimers();

            m.Delta(MobileDelta.WeaponDamage);
        }

        public virtual SkillName GetUsedSkill(Mobile m, bool checkSkillAttrs)
        {
            SkillName sk;

            if (checkSkillAttrs && WeaponAttributes.UseBestSkill != 0)
            {
                var swrd = m.Skills.Swords.Value;
                var fenc = m.Skills.Fencing.Value;
                var mcng = m.Skills.Macing.Value;
                double val;

                sk = SkillName.Swords;
                val = swrd;

                if (fenc > val)
                {
                    sk = SkillName.Fencing;
                    val = fenc;
                }

                if (mcng > val)
                {
                    sk = SkillName.Macing;
                }
            }
            else if (WeaponAttributes.MageWeapon != 0)
            {
                if (m.Skills.Magery.Value > m.Skills[Skill].Value)
                {
                    sk = SkillName.Magery;
                }
                else
                {
                    sk = Skill;
                }
            }
            else
            {
                sk = Skill;

                if (sk != SkillName.Wrestling && !m.Player && !m.Body.IsHuman &&
                    m.Skills.Wrestling.Value > m.Skills[sk].Value)
                {
                    sk = SkillName.Wrestling;
                }
            }

            return sk;
        }

        public virtual double GetAttackSkillValue(Mobile attacker, Mobile defender) =>
            attacker.Skills[GetUsedSkill(attacker, true)].Value;

        public virtual double GetDefendSkillValue(Mobile attacker, Mobile defender) =>
            defender.Skills[GetUsedSkill(defender, true)].Value;

        private static bool CheckAnimal(Mobile m, Type type) => AnimalForm.UnderTransformation(m, type);

        public virtual bool CheckHit(Mobile attacker, Mobile defender)
        {
            var atkWeapon = attacker.Weapon as BaseWeapon;
            var defWeapon = defender.Weapon as BaseWeapon;

            var atkSkill = attacker.Skills[atkWeapon?.Skill ?? SkillName.Wrestling];

            var atkValue = atkWeapon?.GetAttackSkillValue(attacker, defender) ?? 0.0;
            var defValue = defWeapon?.GetDefendSkillValue(attacker, defender) ?? 0.0;

            double ourValue, theirValue;

            var bonus = GetHitChanceBonus();

            if (Core.AOS)
            {
                if (atkValue <= -20.0)
                {
                    atkValue = -19.9;
                }

                if (defValue <= -20.0)
                {
                    defValue = -19.9;
                }

                bonus += AosAttributes.GetValue(attacker, AosAttribute.AttackChance);

                if (DivineFurySpell.UnderEffect(attacker))
                {
                    bonus += 10; // attacker gets 10% bonus when they're under divine fury
                }

                if (CheckAnimal(attacker, typeof(GreyWolf)) || CheckAnimal(attacker, typeof(BakeKitsune)))
                {
                    bonus += 20; // attacker gets 20% bonus when under Wolf or Bake Kitsune form
                }

                if (HitLower.IsUnderAttackEffect(attacker))
                {
                    bonus -= 25; // Under Hit Lower Attack effect -> 25% malus
                }

                var ability = WeaponAbility.GetCurrentAbility(attacker);

                if (ability != null)
                {
                    bonus += ability.AccuracyBonus;
                }

                var move = SpecialMove.GetCurrentMove(attacker);

                if (move != null)
                {
                    bonus += move.GetAccuracyBonus(attacker);
                }

                // Max Hit Chance Increase = 45%
                if (bonus > 45)
                {
                    bonus = 45;
                }

                ourValue = (atkValue + 20.0) * (100 + bonus);

                bonus = AosAttributes.GetValue(defender, AosAttribute.DefendChance);

                var info = ForceArrow.GetInfo(attacker, defender);

                if (info != null && info.Defender == defender)
                {
                    bonus -= info.DefenseChanceMalus;
                }

                if (DivineFurySpell.UnderEffect(defender))
                {
                    bonus -= 20; // defender loses 20% bonus when they're under divine fury
                }

                if (HitLower.IsUnderDefenseEffect(defender))
                {
                    bonus -= 25; // Under Hit Lower Defense effect -> 25% malus
                }

                var blockBonus = 0;

                if (Block.GetBonus(defender, ref blockBonus))
                {
                    bonus += blockBonus;
                }

                var surpriseMalus = 0;

                if (SurpriseAttack.GetMalus(defender, ref surpriseMalus))
                {
                    bonus -= surpriseMalus;
                }

                var discordanceEffect = 0;

                // Defender loses -0/-28% if under the effect of Discordance.
                if (Discordance.GetEffect(attacker, ref discordanceEffect))
                {
                    bonus -= discordanceEffect;
                }

                // Defense Chance Increase = 45%
                if (bonus > 45)
                {
                    bonus = 45;
                }

                theirValue = (defValue + 20.0) * (100 + bonus);

                bonus = 0;
            }
            else
            {
                ourValue = Math.Max(0.1, atkValue + 50.0);
                theirValue = Math.Max(0.1, defValue + 50.0);
            }

            var chance = ourValue / (theirValue * 2.0) * 1.0 + (double)bonus / 100;

            if (Core.AOS && chance < 0.02)
            {
                chance = 0.02;
            }

            return attacker.CheckSkill(atkSkill.SkillName, chance);
        }

        public virtual TimeSpan GetDelay(Mobile m)
        {
            double speed = Speed;

            if (speed == 0)
            {
                return TimeSpan.FromHours(1.0);
            }

            double delayInSeconds;

            if (Core.SE)
            {
                /*
                 * This is likely true for Core.AOS as well... both guides report the same
                 * formula, and both are wrong.
                 * The old formula left in for AOS for legacy & because we aren't quite 100%
                 * Sure that AOS has THIS formula
                 */
                var bonus = AosAttributes.GetValue(m, AosAttribute.WeaponSpeed);

                bonus += DivineFurySpell.GetWeaponSpeed(m);

                // Bonus granted by successful use of Honorable Execution.
                bonus += HonorableExecution.GetSwingBonus(m);

                if (DualWield.Registry.TryGetValue(m, out var duelWield))
                {
                    bonus += duelWield.BonusSwingSpeed;
                }

                if (Feint.Registry.TryGetValue(m, out var feint))
                {
                    bonus -= feint.SwingSpeedReduction;
                }

                var context = TransformationSpellHelper.GetContext(m);

                if (context?.Spell is ReaperFormSpell spell)
                {
                    bonus += spell.SwingSpeedBonus;
                }

                var discordanceEffect = 0;

                // Discordance gives a malus of -0/-28% to swing speed.
                if (Discordance.GetEffect(m, ref discordanceEffect))
                {
                    bonus -= discordanceEffect;
                }

                if (EssenceOfWindSpell.IsDebuffed(m))
                {
                    bonus -= EssenceOfWindSpell.GetSSIMalus(m);
                }

                if (bonus > 60)
                {
                    bonus = 60;
                }

                double ticks;

                if (Core.ML)
                {
                    var stamTicks = m.Stam / 30;

                    ticks = speed * 4;
                    ticks = Math.Floor((ticks - stamTicks) * (100.0 / (100 + bonus)));
                }
                else
                {
                    speed = Math.Floor(speed * (bonus + 100.0) / 100.0);

                    if (speed <= 0)
                    {
                        speed = 1;
                    }

                    ticks = Math.Floor(80000.0 / ((m.Stam + 100) * speed) - 2);
                }

                // Swing speed currently capped at one swing every 1.25 seconds (5 ticks).
                if (ticks < 5)
                {
                    ticks = 5;
                }

                delayInSeconds = ticks * 0.25;
            }
            else if (Core.AOS)
            {
                var v = (m.Stam + 100) * (int)speed;

                var bonus = AosAttributes.GetValue(m, AosAttribute.WeaponSpeed);

                if (DivineFurySpell.UnderEffect(m))
                {
                    bonus += 10;
                }

                var discordanceEffect = 0;

                // Discordance gives a malus of -0/-28% to swing speed.
                if (Discordance.GetEffect(m, ref discordanceEffect))
                {
                    bonus -= discordanceEffect;
                }

                v += AOS.Scale(v, bonus);

                if (v <= 0)
                {
                    v = 1;
                }

                delayInSeconds = Math.Floor(40000.0 / v) * 0.5;

                // Maximum swing rate capped at one swing per second
                // OSI dev said that it has and is supposed to be 1.25
                if (delayInSeconds < 1.25)
                {
                    delayInSeconds = 1.25;
                }
            }
            else
            {
                var v = (m.Stam + 100) * (int)speed;

                if (v <= 0)
                {
                    v = 1;
                }

                delayInSeconds = 15000.0 / v;
            }

            return TimeSpan.FromSeconds(delayInSeconds);
        }

        public static bool CheckParry(Mobile defender)
        {
            if (defender == null)
            {
                return false;
            }

            var shield = defender.FindItemOnLayer<BaseShield>(Layer.TwoHanded);

            var parry = defender.Skills.Parry.Value;
            var bushidoNonRacial = defender.Skills.Bushido.NonRacialValue;
            var bushido = defender.Skills.Bushido.Value;
            double chance;

            if (shield != null)
            {
                // As per OSI, no genitive effect from the Racial stuffs, ie, 120 parry and '0' bushido with humans
                chance = Math.Max((parry - bushidoNonRacial) / 400.0, 0);

                // Parry/Bushido over 100 grants a 5% bonus.
                if (parry >= 100.0 || bushido >= 100.0)
                {
                    chance += 0.05;
                }

                // Evasion grants a variable bonus post ML. 50% prior.
                if (Evasion.IsEvading(defender))
                {
                    chance *= Evasion.GetParryScalar(defender);
                }

                // Low dexterity lowers the chance.
                if (defender.Dex < 80)
                {
                    chance = chance * (20 + defender.Dex) / 100;
                }

                return defender.CheckSkill(SkillName.Parry, chance);
            }

            if (defender.Weapon is Server.Items.Fists or BaseRanged)
            {
                return false;
            }

            var weapon = defender.Weapon as BaseWeapon;

            var divisor = weapon?.Layer == Layer.OneHanded ? 48000.0 : 41140.0;

            chance = parry * bushido / divisor;

            var aosChance = parry / 800.0;

            // Parry or Bushido over 100 grant a 5% bonus.
            if (parry >= 100.0)
            {
                chance += 0.05;
                aosChance += 0.05;
            }
            else if (bushido >= 100.0)
            {
                chance += 0.05;
            }

            // Evasion grants a variable bonus post ML. 50% prior.
            if (Evasion.IsEvading(defender))
            {
                chance *= Evasion.GetParryScalar(defender);
            }

            // Low dexterity lowers the chance.
            if (defender.Dex < 80)
            {
                chance = chance * (20 + defender.Dex) / 100;
            }

            if (chance > aosChance)
            {
                return defender.CheckSkill(SkillName.Parry, chance);
            }

            // Only skillcheck if wielding a shield & there's no effect from Bushido
            return aosChance > Utility.RandomDouble();
        }

        public virtual int AbsorbDamageAOS(Mobile attacker, Mobile defender, int damage)
        {
            var blocked = false;

            if (defender.Player || defender.Body.IsHuman)
            {
                blocked = CheckParry(defender);

                if (blocked)
                {
                    defender.FixedEffect(0x37B9, 10, 16);
                    damage = 0;

                    // Successful block removes the Honorable Execution penalty.
                    HonorableExecution.RemovePenalty(defender);

                    if (CounterAttack.IsCountering(defender))
                    {
                        if (defender.Weapon is BaseWeapon weapon)
                        {
                            defender.FixedParticles(0x3779, 1, 15, 0x158B, 0x0, 0x3, EffectLayer.Waist);
                            weapon.OnSwing(defender, attacker);
                        }

                        CounterAttack.StopCountering(defender);
                    }

                    if (Confidence.IsConfident(defender))
                    {
                        // Your confidence reassures you as you successfully block your opponent's blow.
                        defender.SendLocalizedMessage(1063117);

                        var bushido = defender.Skills.Bushido.Value;

                        defender.Hits += Utility.RandomMinMax(1, (int)(bushido / 12));
                        defender.Stam += Utility.RandomMinMax(1, (int)(bushido / 5));
                    }

                    var shield = defender.FindItemOnLayer<BaseShield>(Layer.TwoHanded);

                    shield?.OnHit(this, damage);
                }
            }

            if (!blocked)
            {
                var positionChance = Utility.RandomDouble();

                Item armorItem = positionChance switch
                {
                    < 0.07 => defender.NeckArmor,
                    < 0.14 => defender.HandArmor,
                    < 0.28 => defender.ArmsArmor,
                    < 0.43 => defender.HeadArmor,
                    < 0.65 => defender.LegsArmor,
                    _      => defender.ChestArmor
                };

                if (armorItem is IWearableDurability armor)
                {
                    armor.OnHit(this, damage); // call OnHit to lose durability
                }
            }

            return damage;
        }

        public virtual int AbsorbDamage(Mobile attacker, Mobile defender, int damage)
        {
            if (Core.AOS)
            {
                return AbsorbDamageAOS(attacker, defender, damage);
            }

            if (defender.FindItemOnLayer(Layer.TwoHanded) is BaseShield shield)
            {
                damage = shield.OnHit(this, damage);
            }

            var chance = Utility.RandomDouble();

            Item armorItem = chance switch
            {
                < 0.07 => defender.NeckArmor,
                < 0.14 => defender.HandArmor,
                < 0.28 => defender.ArmsArmor,
                < 0.43 => defender.HeadArmor,
                < 0.65 => defender.LegsArmor,
                _      => defender.ChestArmor
            };

            if (armorItem is IWearableDurability armor)
            {
                damage = armor.OnHit(this, damage);
            }

            var virtualArmor = defender.VirtualArmor + defender.VirtualArmorMod;

            if (virtualArmor > 0)
            {
                double scalar = chance switch
                {
                    < 0.14 => 0.07,
                    < 0.28 => 0.14,
                    < 0.43 => 0.15,
                    < 0.65 => 0.22,
                    _      => 0.35
                };

                var from = (int)(virtualArmor * scalar) / 2;
                var to = (int)(virtualArmor * scalar);

                damage -= Utility.Random(from, to - from + 1);
            }

            return damage;
        }

        public virtual int GetPackInstinctBonus(Mobile attacker, Mobile defender)
        {
            if (attacker.Player || defender.Player)
            {
                return 0;
            }

            if (attacker is not BaseCreature bc || bc.PackInstinct == PackInstinct.None || !bc.Controlled && !bc.Summoned)
            {
                return 0;
            }

            var master = bc.ControlMaster ?? bc.SummonMaster;

            if (master == null)
            {
                return 0;
            }

            var eable = defender.GetMobilesInRange<BaseCreature>(1);
            var inPack = 1;
            foreach (var m in eable)
            {
                if (m != attacker && (m.PackInstinct & bc.PackInstinct) != 0 && (m.Controlled || m.Summoned) &&
                    master == (m.ControlMaster ?? m.SummonMaster) && m.Combatant == defender)
                {
                    inPack++;
                }
            }

            eable.Free();

            return inPack switch
            {
                >= 5 => 100,
                4    => 75,
                3    => 50,
                2    => 25,
                _    => 0
            };
        }

        public virtual void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1.0)
        {
            if (MirrorImage.HasClone(defender) && defender.Skills.Ninjitsu.Value / 150.0 > Utility.RandomDouble())
            {
                var eable = defender.GetMobilesInRange<Clone>(4);
                foreach (var m in eable)
                {
                    if (m?.Summoned == true && m.SummonMaster == defender)
                    {
                        // Your attack has been diverted to a nearby mirror image of your target!
                        attacker.SendLocalizedMessage(1063141);
                        // You manage to divert the attack onto one of your nearby mirror images.
                        defender.SendLocalizedMessage(1063140);

                        /*
                         * TODO: What happens if the Clone parries a blow?
                         * And what about if the attacker is using Honorable Execution
                         * and kills it?
                         */

                        defender = m;
                        break;
                    }
                }

                eable.Free();
            }

            PlaySwingAnimation(attacker);
            PlayHurtAnimation(defender);

            attacker.PlaySound(GetHitAttackSound(attacker, defender));
            defender.PlaySound(GetHitDefendSound(attacker, defender));

            var damage = ComputeDamage(attacker, defender);

            /*
             * The following damage bonuses multiply damage by a factor.
             * Capped at x3 (300%).
             */
            var percentageBonus = 0;

            var a = WeaponAbility.GetCurrentAbility(attacker);
            var move = SpecialMove.GetCurrentMove(attacker);

            if (a != null)
            {
                percentageBonus += (int)(a.DamageScalar * 100) - 100;
            }

            if (move != null)
            {
                percentageBonus += (int)(move.GetDamageScalar(attacker, defender) * 100) - 100;
            }

            percentageBonus += (int)(ForceOfNature.GetDamageScalar(attacker, defender) * 100) - 100;

            percentageBonus += (int)(damageBonus * 100) - 100;

            var cs = CheckSlayers(attacker, defender);

            if (cs != CheckSlayerResult.None)
            {
                if (cs == CheckSlayerResult.Slayer)
                {
                    defender.FixedEffect(0x37B9, 10, 5);
                }

                percentageBonus += 100;
            }

            if (!attacker.Player)
            {
                if (defender is PlayerMobile pm && pm.EnemyOfOneType != null && pm.EnemyOfOneType != attacker.GetType())
                {
                    percentageBonus += 100;
                }
            }
            else if (!defender.Player && attacker is PlayerMobile pm)
            {
                if (pm.WaitingForEnemy)
                {
                    pm.EnemyOfOneType = defender.GetType();
                    pm.WaitingForEnemy = false;
                }

                if (pm.EnemyOfOneType == defender.GetType())
                {
                    defender.FixedEffect(0x37B9, 10, 5, 1160, 0);

                    percentageBonus += 50;
                }
            }

            var packInstinctBonus = GetPackInstinctBonus(attacker, defender);

            if (packInstinctBonus != 0)
            {
                percentageBonus += packInstinctBonus;
            }

            if (InDoubleStrike)
            {
                percentageBonus -= 10;
            }

            var context = TransformationSpellHelper.GetContext(defender);

            if ((m_Slayer == SlayerName.Silver || m_Slayer2 == SlayerName.Silver) && context?.Spell is NecromancerSpell &&
                context.Type != typeof(HorrificBeastSpell))
            {
                percentageBonus += 25;
            }

            if (attacker is PlayerMobile pmAttacker && !(Core.ML && defender is PlayerMobile))
            {
                if (pmAttacker.HonorActive && pmAttacker.InRange(defender, 1))
                {
                    percentageBonus += 25;
                }

                if (pmAttacker.SentHonorContext != null && pmAttacker.SentHonorContext.Target == defender)
                {
                    percentageBonus += pmAttacker.SentHonorContext.PerfectionDamageBonus;
                }
            }

            if (attacker.Talisman is BaseTalisman talisman && talisman.Killer != null)
            {
                percentageBonus += talisman.Killer.DamageBonus(defender);
            }

            percentageBonus = Math.Min(percentageBonus, 300);

            damage = AOS.Scale(damage, 100 + percentageBonus);

            var bcAtt = attacker as BaseCreature;
            var bcDef = defender as BaseCreature;

            bcAtt?.AlterMeleeDamageTo(defender, ref damage);
            bcDef?.AlterMeleeDamageFrom(attacker, ref damage);

            damage = AbsorbDamage(attacker, defender, damage);

            if (!Core.AOS && damage < 1)
            {
                damage = 1;
            }
            // Parried
            else if (Core.AOS && damage == 0 && a?.Validate(attacker) == true)
            {
                a = null;
                WeaponAbility.ClearCurrentAbility(attacker);
                attacker.SendLocalizedMessage(1061140); // Your attack was parried!
            }

            AddBlood(attacker, defender, damage);

            GetDamageTypes(
                attacker,
                out var phys,
                out var fire,
                out var cold,
                out var pois,
                out var nrgy,
                out var chaos,
                out var direct
            );

            if (Core.ML && this is BaseRanged)
            {
                attacker
                    .FindItemOnLayer<BaseQuiver>(Layer.Cloak)
                    ?.AlterBowDamage(ref phys, ref fire, ref cold, ref pois, ref nrgy, ref chaos, ref direct);
            }

            if (Consecrated)
            {
                phys = defender.PhysicalResistance;
                fire = defender.FireResistance;
                cold = defender.ColdResistance;
                pois = defender.PoisonResistance;
                nrgy = defender.EnergyResistance;

                int low = phys, type = 0;

                if (fire < low)
                {
                    low = fire;
                    type = 1;
                }

                if (cold < low)
                {
                    low = cold;
                    type = 2;
                }

                if (pois < low)
                {
                    low = pois;
                    type = 3;
                }

                if (nrgy < low)
                {
                    type = 4;
                }

                phys = fire = cold = pois = nrgy = chaos = direct = 0;

                if (type == 0)
                {
                    phys = 100;
                }
                else if (type == 1)
                {
                    fire = 100;
                }
                else if (type == 2)
                {
                    cold = 100;
                }
                else if (type == 3)
                {
                    pois = 100;
                }
                else
                {
                    nrgy = 100;
                }
            }

            // TODO: Scale damage, alongside the leech effects below, to weapon speed.
            if (damage > 0 && ImmolatingWeaponSpell.IsImmolating(this))
            {
                ImmolatingWeaponSpell.DoEffect(this, defender);
            }

            if (a?.OnBeforeDamage(attacker, defender) == false)
            {
                WeaponAbility.ClearCurrentAbility(attacker);
                a = null;
            }

            if (move?.OnBeforeDamage(attacker, defender) == false)
            {
                SpecialMove.ClearCurrentMove(attacker);
                move = null;
            }

            var ignoreArmor = a is ArmorIgnore ||
                              move?.IgnoreArmor(attacker) == true ||
                              Bladeweave.BladeWeaving(attacker, out var bladeweavingAbi) &&
                              bladeweavingAbi is ArmorIgnore;

            var damageGiven = AOS.Damage(
                defender,
                attacker,
                damage,
                ignoreArmor,
                phys,
                fire,
                cold,
                pois,
                nrgy,
                chaos,
                direct,
                false,
                this is BaseRanged
            );

            if (damageGiven > 0)
            {
                var propertyBonus = move?.GetPropertyBonus(attacker) ?? 1.0;

                // Leech abilities
                if (Core.AOS)
                {
                    var lifeLeech = 0;
                    var stamLeech = 0;
                    var manaLeech = 0;

                    if ((int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitLeechHits) * propertyBonus) >
                        Utility.Random(100))
                    {
                        lifeLeech += 30; // HitLeechHits% chance to leech 30% of damage as hit points
                    }

                    if ((int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitLeechStam) * propertyBonus) >
                        Utility.Random(100))
                    {
                        stamLeech += 100; // HitLeechStam% chance to leech 100% of damage as stamina
                    }

                    if ((int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitLeechMana) * propertyBonus) >
                        Utility.Random(100))
                    {
                        manaLeech += 40; // HitLeechMana% chance to leech 40% of damage as mana
                    }

                    if (Cursed)
                    {
                        lifeLeech += 50; // Additional 50% life leech for cursed weapons (necro spell)
                    }

                    if (lifeLeech != 0)
                    {
                        attacker.Hits += AOS.Scale(damageGiven, lifeLeech);
                    }

                    if (stamLeech != 0)
                    {
                        attacker.Stam += AOS.Scale(damageGiven, stamLeech);
                    }

                    if (manaLeech != 0)
                    {
                        attacker.Mana += AOS.Scale(damageGiven, manaLeech);
                    }

                    if (lifeLeech != 0 || stamLeech != 0 || manaLeech != 0)
                    {
                        attacker.PlaySound(0x44D);
                    }
                }

                var isAcidMonster =
                    m_MaxHits > 0 && MaxRange <= 1 && Attributes.SpellChanneling == 0 &&
                    defender is Slime or AcidElemental;

                // Stratics says 50% chance, seems more like 4%..
                if (isAcidMonster || Utility.Random(25) == 0)
                {
                    if (isAcidMonster)
                    {
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500263); // *Acid blood scars your weapon!*
                    }

                    if (Core.AOS && WeaponAttributes.SelfRepair > Utility.Random(10))
                    {
                        HitPoints += 2;
                    }
                    else if (m_Hits > 0)
                    {
                        --HitPoints;
                    }
                    else if (m_MaxHits > 1)
                    {
                        --MaxHitPoints;

                        if (Parent is Mobile mobile)
                        {
                            // Your equipment is severely damaged.
                            mobile.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061121);
                        }
                    }
                    else
                    {
                        Delete();
                    }
                }

                if (attacker is VampireBatFamiliar bc)
                {
                    var caster = bc.ControlMaster ?? bc.SummonMaster;

                    if (caster != null && caster.Map == bc.Map && caster.InRange(bc, 2))
                    {
                        caster.Hits += damage;
                    }
                    else
                    {
                        bc.Hits += damage;
                    }
                }

                if (Core.AOS)
                {
                    var physChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitPhysicalArea) * propertyBonus);

                    var fireChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitFireArea) * propertyBonus);

                    var coldChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitColdArea) * propertyBonus);

                    var poisChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitPoisonArea) * propertyBonus);

                    var nrgyChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitEnergyArea) * propertyBonus);

                    if (physChance != 0 && physChance > Utility.Random(100))
                    {
                        DoAreaAttack(attacker, defender, 0x10E, 50, 100, 0, 0, 0, 0);
                    }

                    if (fireChance != 0 && fireChance > Utility.Random(100))
                    {
                        DoAreaAttack(attacker, defender, 0x11D, 1160, 0, 100, 0, 0, 0);
                    }

                    if (coldChance != 0 && coldChance > Utility.Random(100))
                    {
                        DoAreaAttack(attacker, defender, 0x0FC, 2100, 0, 0, 100, 0, 0);
                    }

                    if (poisChance != 0 && poisChance > Utility.Random(100))
                    {
                        DoAreaAttack(attacker, defender, 0x205, 1166, 0, 0, 0, 100, 0);
                    }

                    if (nrgyChance != 0 && nrgyChance > Utility.Random(100))
                    {
                        DoAreaAttack(attacker, defender, 0x1F1, 120, 0, 0, 0, 0, 100);
                    }

                    var maChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitMagicArrow) * propertyBonus);
                    var harmChance = (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitHarm) * propertyBonus);
                    var fireballChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitFireball) * propertyBonus);
                    var lightningChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitLightning) * propertyBonus);
                    var dispelChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitDispel) * propertyBonus);

                    if (maChance != 0 && maChance > Utility.Random(100))
                    {
                        DoMagicArrow(attacker, defender);
                    }

                    if (harmChance != 0 && harmChance > Utility.Random(100))
                    {
                        DoHarm(attacker, defender);
                    }

                    if (fireballChance != 0 && fireballChance > Utility.Random(100))
                    {
                        DoFireball(attacker, defender);
                    }

                    if (lightningChance != 0 && lightningChance > Utility.Random(100))
                    {
                        DoLightning(attacker, defender);
                    }

                    if (dispelChance != 0 && dispelChance > Utility.Random(100))
                    {
                        DoDispel(attacker, defender);
                    }

                    var laChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitLowerAttack) * propertyBonus);
                    var ldChance =
                        (int)(AosWeaponAttributes.GetValue(attacker, AosWeaponAttribute.HitLowerDefend) * propertyBonus);

                    if (laChance != 0 && laChance > Utility.Random(100))
                    {
                        DoLowerAttack(attacker, defender);
                    }

                    if (ldChance != 0 && ldChance > Utility.Random(100))
                    {
                        DoLowerDefense(attacker, defender);
                    }
                }
            }

            bcAtt?.OnGaveMeleeAttack(defender, damage);
            bcDef?.OnGotMeleeAttack(attacker, damage);

            a?.OnHit(attacker, defender, damage);
            move?.OnHit(attacker, defender, damage);

            ForceOfNature.OnHit(attacker, defender);

            if (defender is IHonorTarget it)
            {
                it.ReceivedHonorContext?.OnTargetHit(attacker);
            }

            if (this is not BaseRanged)
            {
                if (AnimalForm.UnderTransformation(attacker, typeof(GiantSerpent)))
                {
                    defender.ApplyPoison(attacker, Poison.Lesser);
                }

                if (AnimalForm.UnderTransformation(defender, typeof(BullFrog)))
                {
                    attacker.ApplyPoison(defender, Poison.Regular);
                }
            }
        }

        public virtual double GetAosDamage(Mobile attacker, int bonus, uint dice, uint sides)
        {
            var damage = Utility.Dice(dice, sides, bonus) * 100;

            // Inscription bonus
            var inscribeSkill = attacker.Skills.Inscribe.Fixed;

            var damageBonus = inscribeSkill / 200;

            if (inscribeSkill >= 1000)
            {
                damageBonus += 5;
            }

            if (attacker.Player)
            {
                // Int bonus
                damageBonus += attacker.Int / 10;

                // SDI bonus
                damageBonus += AosAttributes.GetValue(attacker, AosAttribute.SpellDamage);

                if (PsychicAttack.Registry.TryGetValue(attacker,out var timer))
                {
                    damageBonus -= timer.SpellDamageMalus;
                }

                var context = TransformationSpellHelper.GetContext(attacker);

                if (context?.Spell is ReaperFormSpell spell)
                {
                    damageBonus += spell.SpellDamageBonus;
                }
            }

            damage = AOS.Scale(damage, 100 + damageBonus);

            return damage / 100.0;
        }

        public virtual CheckSlayerResult CheckSlayers(Mobile attacker, Mobile defender)
        {
            var atkWeapon = attacker.Weapon as BaseWeapon;
            var atkSlayer = SlayerGroup.GetEntryByName(atkWeapon?.Slayer ?? SlayerName.None);
            var atkSlayer2 = SlayerGroup.GetEntryByName(atkWeapon?.Slayer2 ?? SlayerName.None);

            if (atkWeapon is ButchersWarCleaver && TalismanSlayer.Slays(TalismanSlayerName.Bovine, defender))
            {
                return CheckSlayerResult.Slayer;
            }

            if (atkSlayer?.Slays(defender) == true || atkSlayer2?.Slays(defender) == true)
            {
                return CheckSlayerResult.Slayer;
            }

            if (attacker.Talisman is BaseTalisman talisman && TalismanSlayer.Slays(talisman.Slayer, defender))
            {
                return CheckSlayerResult.Slayer;
            }

            if (!Core.SE)
            {
                var defISlayer = Spellbook.FindEquippedSpellbook(defender) ?? defender.Weapon as ISlayer;

                if (defISlayer != null)
                {
                    var defSlayer = SlayerGroup.GetEntryByName(defISlayer.Slayer);
                    var defSlayer2 = SlayerGroup.GetEntryByName(defISlayer.Slayer2);

                    if (defSlayer?.Group.OppositionSuperSlays(attacker) == true ||
                        defSlayer2?.Group.OppositionSuperSlays(attacker) == true)
                    {
                        return CheckSlayerResult.Opposition;
                    }
                }
            }

            return CheckSlayerResult.None;
        }

        public virtual void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            new Blood().MoveToWorld(defender.Location, defender.Map);

            var extraBlood = Core.SE ? Utility.RandomMinMax(3, 4) : Utility.RandomMinMax(0, 1);

            for (var i = 0; i < extraBlood; i++)
            {
                new Blood().MoveToWorld(
                    new Point3D(
                        defender.X + Utility.RandomMinMax(-1, 1),
                        defender.Y + Utility.RandomMinMax(-1, 1),
                        defender.Z
                    ),
                    defender.Map
                );
            }
        }

        public virtual void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            if (wielder is BaseCreature bc)
            {
                phys = bc.PhysicalDamage;
                fire = bc.FireDamage;
                cold = bc.ColdDamage;
                pois = bc.PoisonDamage;
                nrgy = bc.EnergyDamage;
                chaos = bc.ChaosDamage;
                direct = bc.DirectDamage;
            }
            else
            {
                fire = AosElementDamages.Fire;
                cold = AosElementDamages.Cold;
                pois = AosElementDamages.Poison;
                nrgy = AosElementDamages.Energy;
                chaos = AosElementDamages.Chaos;
                direct = AosElementDamages.Direct;

                phys = 100 - fire - cold - pois - nrgy - chaos - direct;

                var attrInfo = CraftResources.GetInfo(m_Resource)?.AttributeInfo;

                if (attrInfo != null)
                {
                    var left = phys;

                    left = ApplyCraftAttributeElementDamage(attrInfo.WeaponColdDamage, ref cold, left);
                    left = ApplyCraftAttributeElementDamage(attrInfo.WeaponEnergyDamage, ref nrgy, left);
                    left = ApplyCraftAttributeElementDamage(attrInfo.WeaponFireDamage, ref fire, left);
                    left = ApplyCraftAttributeElementDamage(attrInfo.WeaponPoisonDamage, ref pois, left);
                    left = ApplyCraftAttributeElementDamage(attrInfo.WeaponChaosDamage, ref chaos, left);
                    left = ApplyCraftAttributeElementDamage(attrInfo.WeaponDirectDamage, ref direct, left);

                    phys = left;
                }
            }
        }

        private int ApplyCraftAttributeElementDamage(int attrDamage, ref int element, int totalRemaining)
        {
            if (totalRemaining <= 0)
            {
                return 0;
            }

            if (attrDamage <= 0)
            {
                return totalRemaining;
            }

            var appliedDamage = attrDamage;

            if (appliedDamage + element > 100)
            {
                appliedDamage = 100 - element;
            }

            if (appliedDamage > totalRemaining)
            {
                appliedDamage = totalRemaining;
            }

            element += appliedDamage;

            return totalRemaining - appliedDamage;
        }

        public virtual void OnMiss(Mobile attacker, Mobile defender)
        {
            PlaySwingAnimation(attacker);
            attacker.PlaySound(GetMissAttackSound(attacker, defender));
            defender.PlaySound(GetMissDefendSound(attacker, defender));

            WeaponAbility.GetCurrentAbility(attacker)?.OnMiss(attacker, defender);
            SpecialMove.GetCurrentMove(attacker)?.OnMiss(attacker, defender);

            if (defender is IHonorTarget target)
            {
                target.ReceivedHonorContext?.OnTargetMissed(attacker);
            }
        }

        public virtual void GetBaseDamageRange(Mobile attacker, out int min, out int max)
        {
            if (attacker is BaseCreature c)
            {
                if (c.DamageMin >= 0)
                {
                    min = c.DamageMin;
                    max = c.DamageMax;
                    return;
                }

                if (this is Fists && !c.Body.IsHuman)
                {
                    min = c.Str / 28;
                    max = c.Str / 28;
                    return;
                }
            }

            min = MinDamage;
            max = MaxDamage;
        }

        public virtual double GetBaseDamage(Mobile attacker)
        {
            GetBaseDamageRange(attacker, out var min, out var max);

            var damage = Utility.RandomMinMax(min, max);

            if (Core.AOS)
            {
                return damage;
            }

            /* Apply damage level offset
             * : Regular : 0
             * : Ruin    : 1
             * : Might   : 3
             * : Force   : 5
             * : Power   : 7
             * : Vanq    : 9
             */
            if (m_DamageLevel != WeaponDamageLevel.Regular)
            {
                damage += 2 * (int)m_DamageLevel - 1;
            }

            return damage;
        }

        public virtual double GetBonus(double value, double scalar, double threshold, double offset)
        {
            var bonus = value * scalar;

            if (value >= threshold)
            {
                bonus += offset;
            }

            return bonus / 100;
        }

        public virtual int GetHitChanceBonus()
        {
            if (!Core.AOS)
            {
                return 0;
            }

            return m_AccuracyLevel switch
            {
                WeaponAccuracyLevel.Accurate     => 2,
                WeaponAccuracyLevel.Surpassingly => 4,
                WeaponAccuracyLevel.Eminently    => 6,
                WeaponAccuracyLevel.Exceedingly  => 8,
                WeaponAccuracyLevel.Supremely    => 10,
                _                                => 0
            };
        }

        // Note: AOS quality/damage bonuses removed since they are incorporated into the crafting already
        public virtual int GetDamageBonus() => VirtualDamageBonus;

        public virtual double ScaleDamageAOS(Mobile attacker, double damage, bool checkSkills)
        {
            if (checkSkills)
            {
                // Passively check tactics for gain
                attacker.CheckSkill(SkillName.Tactics, 0.0, attacker.Skills.Tactics.Cap);

                // Passively check Anatomy for gain
                attacker.CheckSkill(SkillName.Anatomy, 0.0, attacker.Skills.Anatomy.Cap);

                if (Type == WeaponType.Axe)
                {
                    // Passively check Lumberjacking for gain
                    attacker.CheckSkill(SkillName.Lumberjacking, 0.0, 100.0);
                }
            }

            /*
             * These are the bonuses given by the physical characteristics of the mobile.
             * No caps apply.
             */
            var strengthBonus = GetBonus(attacker.Str, 0.300, 100.0, 5.00);
            var anatomyBonus = GetBonus(attacker.Skills.Anatomy.Value, 0.500, 100.0, 5.00);
            var tacticsBonus = GetBonus(attacker.Skills.Tactics.Value, 0.625, 100.0, 6.25);
            var lumberBonus = GetBonus(attacker.Skills.Lumberjacking.Value, 0.200, 100.0, 10.00);

            if (Type != WeaponType.Axe)
            {
                lumberBonus = 0.0;
            }

            /*
             * The following are damage modifiers whose effect shows on the status bar.
             * Capped at 100% total.
             */
            var damageBonus = AosAttributes.GetValue(attacker, AosAttribute.WeaponDamage);

            // Horrific Beast transformation gives a +25% bonus to damage.
            if (TransformationSpellHelper.UnderTransformation(attacker, typeof(HorrificBeastSpell)))
            {
                damageBonus += 25;
            }

            // Divine Fury gives a +10% bonus to damage.
            damageBonus += DivineFurySpell.GetDamageBonus(attacker);

            var defenseMasteryMalus = 0;

            // Defense Mastery gives a -50%/-80% malus to damage.
            if (DefenseMastery.GetMalus(attacker, ref defenseMasteryMalus))
            {
                damageBonus -= defenseMasteryMalus;
            }

            var discordanceEffect = 0;

            // Discordance gives a -2%/-48% malus to damage.
            if (Discordance.GetEffect(attacker, ref discordanceEffect))
            {
                damageBonus -= discordanceEffect * 2;
            }

            if (damageBonus > 100)
            {
                damageBonus = 100;
            }

            var totalBonus = strengthBonus + anatomyBonus + tacticsBonus + lumberBonus + damageBonus + GetDamageBonus();

            return damage + damage * totalBonus / 100.0;
        }

        public virtual int ComputeDamageAOS(Mobile attacker, Mobile defender) =>
            (int)ScaleDamageAOS(attacker, GetBaseDamage(attacker), true);

        public virtual double ScaleDamageOld(Mobile attacker, double damage, bool checkSkills)
        {
            if (checkSkills)
            {
                // Passively check tactics for gain
                attacker.CheckSkill(SkillName.Tactics, 0.0, attacker.Skills.Tactics.Cap);

                // Passively check Anatomy for gain
                attacker.CheckSkill(SkillName.Anatomy, 0.0, attacker.Skills.Anatomy.Cap);

                if (Type == WeaponType.Axe)
                {
                    // Passively check Lumberjacking for gain
                    attacker.CheckSkill(SkillName.Lumberjacking, 0.0, 100.0);
                }
            }

            /* Compute tactics modifier
             * :   0.0 = 50% loss
             * :  50.0 = unchanged
             * : 100.0 = 50% bonus
             */
            damage += damage * ((attacker.Skills.Tactics.Value - 50.0) / 100.0);

            /* Compute strength modifier
             * : 1% bonus for every 5 strength
             */
            var modifiers = attacker.Str / 5.0 / 100.0;

            /* Compute anatomy modifier
             * : 1% bonus for every 5 points of anatomy
             * : +10% bonus at Grandmaster or higher
             */
            var anatomyValue = attacker.Skills.Anatomy.Value;
            modifiers += anatomyValue / 5.0 / 100.0;

            if (anatomyValue >= 100.0)
            {
                modifiers += 0.1;
            }

            /* Compute lumberjacking bonus
             * : 1% bonus for every 5 points of lumberjacking
             * : +10% bonus at Grandmaster or higher
             */
            if (Type == WeaponType.Axe)
            {
                var lumberValue = attacker.Skills.Lumberjacking.Value;

                modifiers += lumberValue / 5.0 / 100.0;

                if (lumberValue >= 100.0)
                {
                    modifiers += 0.1;
                }
            }

            // New quality bonus:
            if (m_Quality != WeaponQuality.Regular)
            {
                modifiers += ((int)m_Quality - 1) * 0.2;
            }

            // Virtual damage bonus:
            if (VirtualDamageBonus != 0)
            {
                modifiers += VirtualDamageBonus / 100.0;
            }

            // Apply bonuses
            damage += damage * modifiers;

            return ScaleDamageByDurability((int)damage);
        }

        public virtual int ScaleDamageByDurability(int damage)
        {
            var scale = 100;

            if (m_MaxHits > 0 && m_Hits < m_MaxHits)
            {
                scale = 50 + 50 * m_Hits / m_MaxHits;
            }

            return AOS.Scale(damage, scale);
        }

        public virtual int ComputeDamage(Mobile attacker, Mobile defender)
        {
            if (Core.AOS)
            {
                return ComputeDamageAOS(attacker, defender);
            }

            var damage = (int)ScaleDamageOld(attacker, GetBaseDamage(attacker), true);

            // pre-AOS, halve damage if the defender is a player or the attacker is not a player
            if (defender is PlayerMobile || attacker is not PlayerMobile)
            {
                damage /= 2;
            }

            return damage;
        }

        public virtual void PlayHurtAnimation(Mobile from)
        {
            if (from.Mounted)
            {
                return;
            }

            int action;
            int frames;

            switch (from.Body.Type)
            {
                case BodyType.Sea:
                case BodyType.Animal:
                    {
                        action = 7;
                        frames = 5;
                        break;
                    }
                case BodyType.Monster:
                    {
                        action = 10;
                        frames = 4;
                        break;
                    }
                case BodyType.Human:
                    {
                        action = 20;
                        frames = 5;
                        break;
                    }
                default:
                    {
                        return;
                    }
            }

            from.Animate(action, frames, 1, true, false, 0);
        }

        public virtual void PlaySwingAnimation(Mobile from)
        {
            int action;

            switch (from.Body.Type)
            {
                case BodyType.Sea:
                case BodyType.Animal:
                    {
                        action = Utility.Random(5, 2);
                        break;
                    }
                case BodyType.Monster:
                    {
                        switch (Animation)
                        {
                            default:
                                {
                                    action = Utility.Random(4, 3);
                                    break;
                                }
                            case WeaponAnimation.ShootBow:
                                {
                                    return; // 7
                                }
                            case WeaponAnimation.ShootXBow:
                                {
                                    return; // 8
                                }
                        }

                        break;
                    }
                case BodyType.Human:
                    {
                        if (!from.Mounted)
                        {
                            action = (int)Animation;
                        }
                        else
                        {
                            action = Animation switch
                            {
                                WeaponAnimation.Wrestle   => 26,
                                WeaponAnimation.Bash1H    => 26,
                                WeaponAnimation.Pierce1H  => 26,
                                WeaponAnimation.Slash1H   => 26,
                                WeaponAnimation.Bash2H    => 29,
                                WeaponAnimation.Pierce2H  => 29,
                                WeaponAnimation.Slash2H   => 29,
                                WeaponAnimation.ShootBow  => 27,
                                WeaponAnimation.ShootXBow => 28,
                                _                         => 26
                            };
                        }

                        break;
                    }
                default:
                    {
                        return;
                    }
            }

            from.Animate(action, 7, 1, true, false, 0);
        }

        public int GetElementalDamageHue()
        {
            GetDamageTypes(null, out _, out var fire, out var cold, out var pois, out var nrgy, out _, out _);

            var currentMax = 50;
            var hue = 0;

            // Order is Cold, Energy, Fire, Poison, Physical
            if (pois >= currentMax)
            {
                hue = 1267 + (pois - 50) / 10;
                currentMax = pois;
            }

            if (fire >= currentMax)
            {
                hue = 1255 + (fire - 50) / 10;
                currentMax = fire;
            }

            if (nrgy >= currentMax)
            {
                hue = 1273 + (nrgy - 50) / 10;
                currentMax = nrgy;
            }

            if (cold >= currentMax)
            {
                hue = 1261 + (cold - 50) / 10;
            }

            return hue;
        }

        public override void AddNameProperty(IPropertyList list)
        {
            var oreType = m_Resource switch
            {
                CraftResource.DullCopper    => 1053108,
                CraftResource.ShadowIron    => 1053107,
                CraftResource.Copper        => 1053106,
                CraftResource.Bronze        => 1053105,
                CraftResource.Gold          => 1053104,
                CraftResource.Agapite       => 1053103,
                CraftResource.Verite        => 1053102,
                CraftResource.Valorite      => 1053101,
                CraftResource.SpinedLeather => 1061118,
                CraftResource.HornedLeather => 1061117,
                CraftResource.BarbedLeather => 1061116,
                CraftResource.RedScales     => 1060814,
                CraftResource.YellowScales  => 1060818,
                CraftResource.BlackScales   => 1060820,
                CraftResource.GreenScales   => 1060819,
                CraftResource.WhiteScales   => 1060821,
                CraftResource.BlueScales    => 1060815,
                _                           => 0
            };

            var name = Name;

            if (oreType != 0)
            {
                if (name != null)
                {
                    list.Add(1053099, $"{oreType:#}\t{name}"); // ~1_oretype~ ~2_armortype~
                }
                else
                {
                    list.Add(1053099, $"{oreType:#}\t{LabelNumber:#}"); // ~1_oretype~ ~2_armortype~
                }
            }
            else if (name == null)
            {
                list.Add(LabelNumber);
            }
            else
            {
                list.Add(name);
            }

            /*
             * Want to move this to the engraving tool, let the non-harmful
             * formatting show, and remove CLILOCs embedded: more like OSI
             * did with the books that had markup, etc.
             *
             * This will have a negative effect on a few event things in-game
             * as is.
             *
             * If we cant find a more OSI-ish way to clean it up, we can
             * easily put this back, and use it in the deserialize
             * method and engraving tool, to make it perm cleaned up.
             */

            if (!string.IsNullOrEmpty(m_EngravedText))
            {
                list.Add(1062613, m_EngravedText);
            }

            /* list.Add( 1062613, Utility.FixHtml( m_EngravedText ) ); */
        }

        public override bool AllowEquippedCast(Mobile from) =>
            base.AllowEquippedCast(from) || Attributes.SpellChanneling != 0;

        public virtual int GetLuckBonus() => CraftResources.GetInfo(m_Resource)?.AttributeInfo?.WeaponLuck ?? 0;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Crafter != null)
            {
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~
            }

            if (m_FactionState != null)
            {
                list.Add(1041350); // faction item
            }

            SkillBonuses?.GetProperties(list);

            if (m_Quality == WeaponQuality.Exceptional)
            {
                list.Add(1060636); // exceptional
            }

            if (RequiredRaces == Race.AllowElvesOnly)
            {
                list.Add(1075086); // Elves Only
            }

            if (RequiredRaces == Race.AllowGargoylesOnly)
            {
                list.Add(1111709); // Gargoyles Only
            }

            if (ArtifactRarity > 0)
            {
                list.Add(1061078, ArtifactRarity); // artifact rarity ~1_val~
            }

            if (this is IUsesRemaining usesRemaining && usesRemaining.ShowUsesRemaining)
            {
                list.Add(1060584, usesRemaining.UsesRemaining); // uses remaining: ~1_val~
            }

            if (m_Poison != null && m_PoisonCharges > 0)
            {
                list.Add(1062412 + m_Poison.Level, m_PoisonCharges);
            }

            if (m_Slayer != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer);
                if (entry != null)
                {
                    list.Add(entry.Title);
                }
            }

            if (m_Slayer2 != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer2);
                if (entry != null)
                {
                    list.Add(entry.Title);
                }
            }

            AddResistanceProperties(list);

            int prop;

            var ranged = this as BaseRanged;

            if (Core.ML && ranged?.Balanced == true)
            {
                list.Add(1072792); // Balanced
            }

            if (WeaponAttributes.UseBestSkill != 0)
            {
                list.Add(1060400); // use best weapon skill
            }

            if ((prop = GetDamageBonus() + Attributes.WeaponDamage) != 0)
            {
                list.Add(1060401, prop); // damage increase ~1_val~%
            }

            if ((prop = Attributes.DefendChance) != 0)
            {
                list.Add(1060408, prop); // defense chance increase ~1_val~%
            }

            if ((prop = Attributes.EnhancePotions) != 0)
            {
                list.Add(1060411, prop); // enhance potions ~1_val~%
            }

            if ((prop = Attributes.CastRecovery) != 0)
            {
                list.Add(1060412, prop); // faster cast recovery ~1_val~
            }

            if ((prop = Attributes.CastSpeed) != 0)
            {
                list.Add(1060413, prop); // faster casting ~1_val~
            }

            if ((prop = GetHitChanceBonus() + Attributes.AttackChance) != 0)
            {
                list.Add(1060415, prop); // hit chance increase ~1_val~%
            }

            if ((prop = WeaponAttributes.HitColdArea) != 0)
            {
                list.Add(1060416, prop); // hit cold area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitDispel) != 0)
            {
                list.Add(1060417, prop); // hit dispel ~1_val~%
            }

            if ((prop = WeaponAttributes.HitEnergyArea) != 0)
            {
                list.Add(1060418, prop); // hit energy area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitFireArea) != 0)
            {
                list.Add(1060419, prop); // hit fire area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitFireball) != 0)
            {
                list.Add(1060420, prop); // hit fireball ~1_val~%
            }

            if ((prop = WeaponAttributes.HitHarm) != 0)
            {
                list.Add(1060421, prop); // hit harm ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLeechHits) != 0)
            {
                list.Add(1060422, prop); // hit life leech ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLightning) != 0)
            {
                list.Add(1060423, prop); // hit lightning ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLowerAttack) != 0)
            {
                list.Add(1060424, prop); // hit lower attack ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLowerDefend) != 0)
            {
                list.Add(1060425, prop); // hit lower defense ~1_val~%
            }

            if ((prop = WeaponAttributes.HitMagicArrow) != 0)
            {
                list.Add(1060426, prop); // hit magic arrow ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLeechMana) != 0)
            {
                list.Add(1060427, prop); // hit mana leech ~1_val~%
            }

            if ((prop = WeaponAttributes.HitPhysicalArea) != 0)
            {
                list.Add(1060428, prop); // hit physical area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitPoisonArea) != 0)
            {
                list.Add(1060429, prop); // hit poison area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLeechStam) != 0)
            {
                list.Add(1060430, prop); // hit stamina leech ~1_val~%
            }

            if (ImmolatingWeaponSpell.IsImmolating(this))
            {
                list.Add(1111917); // Immolated
            }

            if (Core.ML && (ranged?.Velocity ?? 0) != 0)
            {
                list.Add(1072793, prop); // Velocity ~1_val~%
            }

            if ((prop = Attributes.BonusDex) != 0)
            {
                list.Add(1060409, prop); // dexterity bonus ~1_val~
            }

            if ((prop = Attributes.BonusHits) != 0)
            {
                list.Add(1060431, prop); // hit point increase ~1_val~
            }

            if ((prop = Attributes.BonusInt) != 0)
            {
                list.Add(1060432, prop); // intelligence bonus ~1_val~
            }

            if ((prop = Attributes.LowerManaCost) != 0)
            {
                list.Add(1060433, prop); // lower mana cost ~1_val~%
            }

            if ((prop = Attributes.LowerRegCost) != 0)
            {
                list.Add(1060434, prop); // lower reagent cost ~1_val~%
            }

            if ((prop = GetLowerStatReq()) != 0)
            {
                list.Add(1060435, prop); // lower requirements ~1_val~%
            }

            if ((prop = GetLuckBonus() + Attributes.Luck) != 0)
            {
                list.Add(1060436, prop); // luck ~1_val~
            }

            if ((prop = WeaponAttributes.MageWeapon) != 0)
            {
                list.Add(1060438, 30 - prop); // mage weapon -~1_val~ skill
            }

            if ((prop = Attributes.BonusMana) != 0)
            {
                list.Add(1060439, prop); // mana increase ~1_val~
            }

            if ((prop = Attributes.RegenMana) != 0)
            {
                list.Add(1060440, prop); // mana regeneration ~1_val~
            }

            if (Attributes.NightSight != 0)
            {
                list.Add(1060441); // night sight
            }

            if ((prop = Attributes.ReflectPhysical) != 0)
            {
                list.Add(1060442, prop); // reflect physical damage ~1_val~%
            }

            if ((prop = Attributes.RegenStam) != 0)
            {
                list.Add(1060443, prop); // stamina regeneration ~1_val~
            }

            if ((prop = Attributes.RegenHits) != 0)
            {
                list.Add(1060444, prop); // hit point regeneration ~1_val~
            }

            if ((prop = WeaponAttributes.SelfRepair) != 0)
            {
                list.Add(1060450, prop); // self repair ~1_val~
            }

            if (Attributes.SpellChanneling != 0)
            {
                list.Add(1060482); // spell channeling
            }

            if ((prop = Attributes.SpellDamage) != 0)
            {
                list.Add(1060483, prop); // spell damage increase ~1_val~%
            }

            if ((prop = Attributes.BonusStam) != 0)
            {
                list.Add(1060484, prop); // stamina increase ~1_val~
            }

            if ((prop = Attributes.BonusStr) != 0)
            {
                list.Add(1060485, prop); // strength bonus ~1_val~
            }

            if ((prop = Attributes.WeaponSpeed) != 0)
            {
                list.Add(1060486, prop); // swing speed increase ~1_val~%
            }

            if (Core.ML && (prop = Attributes.IncreasedKarmaLoss) != 0)
            {
                list.Add(1075210, prop); // Increased Karma Loss ~1val~%
            }

            GetDamageTypes(
                null,
                out var phys,
                out var fire,
                out var cold,
                out var pois,
                out var nrgy,
                out var chaos,
                out var direct
            );

            if (phys != 0)
            {
                list.Add(1060403, phys); // physical damage ~1_val~%
            }

            if (fire != 0)
            {
                list.Add(1060405, fire); // fire damage ~1_val~%
            }

            if (cold != 0)
            {
                list.Add(1060404, cold); // cold damage ~1_val~%
            }

            if (pois != 0)
            {
                list.Add(1060406, pois); // poison damage ~1_val~%
            }

            if (nrgy != 0)
            {
                list.Add(1060407, nrgy); // energy damage ~1_val
            }

            if (Core.ML && chaos != 0)
            {
                list.Add(1072846, chaos); // chaos damage ~1_val~%
            }

            if (Core.ML && direct != 0)
            {
                list.Add(1079978, direct); // Direct Damage: ~1_PERCENT~%
            }

            list.Add(1061168, $"{MinDamage}\t{MaxDamage}"); // weapon damage ~1_val~ - ~2_val~

            if (Core.ML)
            {
                list.Add(1061167, $"{Speed}s"); // weapon speed ~1_val~
            }
            else
            {
                list.Add(1061167, $"{Speed}");
            }

            if (MaxRange > 1)
            {
                list.Add(1061169, MaxRange); // range ~1_val~
            }

            var strReq = AOS.Scale(StrRequirement, 100 - GetLowerStatReq());

            if (strReq > 0)
            {
                list.Add(1061170, strReq); // strength requirement ~1_val~
            }

            if (Layer == Layer.TwoHanded)
            {
                list.Add(1061171); // two-handed weapon
            }
            else
            {
                list.Add(1061824); // one-handed weapon
            }

            if (Core.SE || WeaponAttributes.UseBestSkill == 0)
            {
                switch (Skill)
                {
                    case SkillName.Swords:
                        {
                            list.Add(1061172); // skill required: swordsmanship
                            break;
                        }
                    case SkillName.Macing:
                        {
                            list.Add(1061173); // skill required: mace fighting
                            break;
                        }
                    case SkillName.Fencing:
                        {
                            list.Add(1061174); // skill required: fencing
                            break;
                        }
                    case SkillName.Archery:
                        {
                            list.Add(1061175); // skill required: archery
                            break;
                        }
                }
            }

            if (m_Hits >= 0 && m_MaxHits > 0)
            {
                list.Add(1060639, $"{m_Hits}\t{m_MaxHits}"); // durability ~1_val~ / ~2_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            var attrs = new List<EquipInfoAttribute>();

            if (DisplayLootType)
            {
                if (LootType == LootType.Blessed)
                {
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                }
                else if (LootType == LootType.Cursed)
                {
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
                }
            }

            if (m_FactionState != null)
            {
                attrs.Add(new EquipInfoAttribute(1041350)); // faction item
            }

            if (m_Quality == WeaponQuality.Exceptional)
            {
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));
            }

            if (m_Identified || from.AccessLevel >= AccessLevel.GameMaster)
            {
                if (m_Slayer != SlayerName.None)
                {
                    var entry = SlayerGroup.GetEntryByName(m_Slayer);
                    if (entry != null)
                    {
                        attrs.Add(new EquipInfoAttribute(entry.Title));
                    }
                }

                if (m_Slayer2 != SlayerName.None)
                {
                    var entry = SlayerGroup.GetEntryByName(m_Slayer2);
                    if (entry != null)
                    {
                        attrs.Add(new EquipInfoAttribute(entry.Title));
                    }
                }

                if (m_DurabilityLevel != WeaponDurabilityLevel.Regular)
                {
                    attrs.Add(new EquipInfoAttribute(1038000 + (int)m_DurabilityLevel));
                }

                if (m_DamageLevel != WeaponDamageLevel.Regular)
                {
                    attrs.Add(new EquipInfoAttribute(1038015 + (int)m_DamageLevel));
                }

                if (m_AccuracyLevel != WeaponAccuracyLevel.Regular)
                {
                    attrs.Add(new EquipInfoAttribute(1038010 + (int)m_AccuracyLevel));
                }
            }
            else if (m_Slayer != SlayerName.None || m_Slayer2 != SlayerName.None ||
                     m_DurabilityLevel != WeaponDurabilityLevel.Regular || m_DamageLevel != WeaponDamageLevel.Regular ||
                     m_AccuracyLevel != WeaponAccuracyLevel.Regular)
            {
                attrs.Add(new EquipInfoAttribute(1038000)); // Unidentified
            }

            if (m_Poison != null && m_PoisonCharges > 0)
            {
                attrs.Add(new EquipInfoAttribute(1017383, m_PoisonCharges));
            }

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
            {
                return;
            }

            from.NetState?.SendDisplayEquipmentInfo(Serial, number, m_Crafter?.RawName, false, attrs);
        }

        public virtual int GetHitAttackSound(Mobile attacker, Mobile defender)
        {
            var sound = attacker.GetAttackSound();
            return sound == -1 ? HitSound : sound;
        }

        public virtual int GetHitDefendSound(Mobile attacker, Mobile defender) => defender.GetHurtSound();

        public virtual int GetMissAttackSound(Mobile attacker, Mobile defender) =>
            attacker.GetAttackSound() == -1 ? MissSound : -1;

        public virtual int GetMissDefendSound(Mobile attacker, Mobile defender) => -1;

        public virtual void DoMagicArrow(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
            {
                return;
            }

            attacker.DoHarmful(defender);

            var damage = GetAosDamage(attacker, 10, 1, 4);

            attacker.MovingParticles(defender, 0x36E4, 5, 0, false, true, 3006, 4006, 0);
            attacker.PlaySound(0x1E5);

            SpellHelper.Damage(TimeSpan.FromSeconds(1.0), defender, attacker, damage, 0, 100, 0, 0, 0);
        }

        public virtual void DoHarm(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
            {
                return;
            }

            attacker.DoHarmful(defender);

            var damage = GetAosDamage(attacker, 17, 1, 5);

            if (!defender.InRange(attacker, 2))
            {
                damage *= 0.25; // 1/4 damage at > 2 tile range
            }
            else if (!defender.InRange(attacker, 1))
            {
                damage *= 0.50; // 1/2 damage at 2 tile range
            }

            defender.FixedParticles(0x374A, 10, 30, 5013, 1153, 2, EffectLayer.Waist);
            defender.PlaySound(0x0FC);

            SpellHelper.Damage(TimeSpan.Zero, defender, attacker, damage, 0, 0, 100, 0, 0);
        }

        public virtual void DoFireball(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
            {
                return;
            }

            attacker.DoHarmful(defender);

            var damage = GetAosDamage(attacker, 19, 1, 5);

            attacker.MovingParticles(defender, 0x36D4, 7, 0, false, true, 9502, 4019, 0x160);
            attacker.PlaySound(0x15E);

            SpellHelper.Damage(TimeSpan.FromSeconds(1.0), defender, attacker, damage, 0, 100, 0, 0, 0);
        }

        public virtual void DoLightning(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
            {
                return;
            }

            attacker.DoHarmful(defender);

            var damage = GetAosDamage(attacker, 23, 1, 4);

            defender.BoltEffect(0);

            SpellHelper.Damage(TimeSpan.Zero, defender, attacker, damage, 0, 0, 0, 0, 100);
        }

        public virtual void DoDispel(Mobile attacker, Mobile defender)
        {
            var dispellable = false;

            if (defender is BaseCreature creature)
            {
                dispellable = creature.Summoned && !creature.IsAnimatedDead;
            }

            if (!dispellable)
            {
                return;
            }

            if (!attacker.CanBeHarmful(defender, false))
            {
                return;
            }

            attacker.DoHarmful(defender);

            MagerySpell sp = new DispelSpell(attacker);

            if (sp.CheckResisted(defender))
            {
                defender.FixedEffect(0x3779, 10, 20);
            }
            else
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(defender.Location, defender.Map, EffectItem.DefaultDuration),
                    0x3728,
                    8,
                    20,
                    5042
                );
                Effects.PlaySound(defender, 0x201);

                defender.Delete();
            }
        }

        public virtual void DoLowerAttack(Mobile from, Mobile defender)
        {
            if (HitLower.ApplyAttack(defender))
            {
                defender.PlaySound(0x28E);
                Effects.SendTargetEffect(defender, 0x37BE, 1, 4, 0xA, 3);
            }
        }

        public virtual void DoLowerDefense(Mobile from, Mobile defender)
        {
            if (HitLower.ApplyDefense(defender))
            {
                defender.PlaySound(0x28E);
                Effects.SendTargetEffect(defender, 0x37BE, 1, 4, 0x23, 3);
            }
        }

        public virtual void DoAreaAttack(
            Mobile from, Mobile defender, int sound, int hue, int phys, int fire, int cold,
            int pois, int nrgy
        )
        {
            var map = from.Map;

            if (map == null)
            {
                return;
            }

            var range = Core.ML ? 5 : 10;

            var eable = from.GetMobilesInRange(range);
            using var queue = PooledRefQueue<Mobile>.Create();
            foreach (var m in eable)
            {
                if (from != m && defender != m && SpellHelper.ValidIndirectTarget(from, m)
                    && from.CanBeHarmful(m, false) && (!Core.ML || from.InLOS(m)))
                {
                    queue.Enqueue(m);
                }
            }
            eable.Free();

            if (queue.Count == 0)
            {
                return;
            }

            Effects.PlaySound(from.Location, map, sound);

            while (queue.Count > 0)
            {
                var m = queue.Dequeue();

                var scalar = Core.ML ? 1.0 : (11 - from.GetDistanceToSqrt(m)) / 10;

                if (scalar <= 0)
                {
                    continue;
                }

                var damage = GetBaseDamage(from);

                if (scalar < 1.0)
                {
                    damage *= (11 - from.GetDistanceToSqrt(m)) / 10;
                }

                from.DoHarmful(m, true);
                m.FixedEffect(0x3779, 1, 15, hue, 0);
                AOS.Damage(m, from, (int)damage, phys, fire, cold, pois, nrgy);
            }
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
            {
                flags |= toSet;
            }
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(9); // version

            var flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.DamageLevel, m_DamageLevel != WeaponDamageLevel.Regular);
            SetSaveFlag(ref flags, SaveFlag.AccuracyLevel, m_AccuracyLevel != WeaponAccuracyLevel.Regular);
            SetSaveFlag(ref flags, SaveFlag.DurabilityLevel, m_DurabilityLevel != WeaponDurabilityLevel.Regular);
            SetSaveFlag(ref flags, SaveFlag.Quality, m_Quality != WeaponQuality.Regular);
            SetSaveFlag(ref flags, SaveFlag.Hits, m_Hits != 0);
            SetSaveFlag(ref flags, SaveFlag.MaxHits, m_MaxHits != 0);
            SetSaveFlag(ref flags, SaveFlag.Slayer, m_Slayer != SlayerName.None);
            SetSaveFlag(ref flags, SaveFlag.Poison, m_Poison != null);
            SetSaveFlag(ref flags, SaveFlag.PoisonCharges, m_PoisonCharges != 0);
            SetSaveFlag(ref flags, SaveFlag.Crafter, m_Crafter != null);
            SetSaveFlag(ref flags, SaveFlag.Identified, m_Identified);
            SetSaveFlag(ref flags, SaveFlag.StrReq, m_StrReq != -1);
            SetSaveFlag(ref flags, SaveFlag.DexReq, m_DexReq != -1);
            SetSaveFlag(ref flags, SaveFlag.IntReq, m_IntReq != -1);
            SetSaveFlag(ref flags, SaveFlag.MinDamage, m_MinDamage != -1);
            SetSaveFlag(ref flags, SaveFlag.MaxDamage, m_MaxDamage != -1);
            SetSaveFlag(ref flags, SaveFlag.HitSound, m_HitSound != -1);
            SetSaveFlag(ref flags, SaveFlag.MissSound, m_MissSound != -1);
            SetSaveFlag(ref flags, SaveFlag.Speed, m_Speed != -1);
            SetSaveFlag(ref flags, SaveFlag.MaxRange, m_MaxRange != -1);
            SetSaveFlag(ref flags, SaveFlag.Skill, m_Skill != (SkillName)(-1));
            SetSaveFlag(ref flags, SaveFlag.Type, m_Type != (WeaponType)(-1));
            SetSaveFlag(ref flags, SaveFlag.Animation, m_Animation != (WeaponAnimation)(-1));
            SetSaveFlag(ref flags, SaveFlag.Resource, m_Resource != CraftResource.Iron);
            SetSaveFlag(ref flags, SaveFlag.xAttributes, !Attributes.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.xWeaponAttributes, !WeaponAttributes.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.PlayerConstructed, PlayerConstructed);
            SetSaveFlag(ref flags, SaveFlag.SkillBonuses, !SkillBonuses.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.Slayer2, m_Slayer2 != SlayerName.None);
            SetSaveFlag(ref flags, SaveFlag.ElementalDamages, !AosElementDamages.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.EngravedText, !string.IsNullOrEmpty(m_EngravedText));

            writer.Write((int)flags);

            if (GetSaveFlag(flags, SaveFlag.DamageLevel))
            {
                writer.Write((int)m_DamageLevel);
            }

            if (GetSaveFlag(flags, SaveFlag.AccuracyLevel))
            {
                writer.Write((int)m_AccuracyLevel);
            }

            if (GetSaveFlag(flags, SaveFlag.DurabilityLevel))
            {
                writer.Write((int)m_DurabilityLevel);
            }

            if (GetSaveFlag(flags, SaveFlag.Quality))
            {
                writer.Write((int)m_Quality);
            }

            if (GetSaveFlag(flags, SaveFlag.Hits))
            {
                writer.Write(m_Hits);
            }

            if (GetSaveFlag(flags, SaveFlag.MaxHits))
            {
                writer.Write(m_MaxHits);
            }

            if (GetSaveFlag(flags, SaveFlag.Slayer))
            {
                writer.Write((int)m_Slayer);
            }

            if (GetSaveFlag(flags, SaveFlag.Poison))
            {
                writer.Write(m_Poison);
            }

            if (GetSaveFlag(flags, SaveFlag.PoisonCharges))
            {
                writer.Write(m_PoisonCharges);
            }

            if (GetSaveFlag(flags, SaveFlag.Crafter))
            {
                writer.Write(m_Crafter);
            }

            if (GetSaveFlag(flags, SaveFlag.StrReq))
            {
                writer.Write(m_StrReq);
            }

            if (GetSaveFlag(flags, SaveFlag.DexReq))
            {
                writer.Write(m_DexReq);
            }

            if (GetSaveFlag(flags, SaveFlag.IntReq))
            {
                writer.Write(m_IntReq);
            }

            if (GetSaveFlag(flags, SaveFlag.MinDamage))
            {
                writer.Write(m_MinDamage);
            }

            if (GetSaveFlag(flags, SaveFlag.MaxDamage))
            {
                writer.Write(m_MaxDamage);
            }

            if (GetSaveFlag(flags, SaveFlag.HitSound))
            {
                writer.Write(m_HitSound);
            }

            if (GetSaveFlag(flags, SaveFlag.MissSound))
            {
                writer.Write(m_MissSound);
            }

            if (GetSaveFlag(flags, SaveFlag.Speed))
            {
                writer.Write(m_Speed);
            }

            if (GetSaveFlag(flags, SaveFlag.MaxRange))
            {
                writer.Write(m_MaxRange);
            }

            if (GetSaveFlag(flags, SaveFlag.Skill))
            {
                writer.Write((int)m_Skill);
            }

            if (GetSaveFlag(flags, SaveFlag.Type))
            {
                writer.Write((int)m_Type);
            }

            if (GetSaveFlag(flags, SaveFlag.Animation))
            {
                writer.Write((int)m_Animation);
            }

            if (GetSaveFlag(flags, SaveFlag.Resource))
            {
                writer.Write((int)m_Resource);
            }

            if (GetSaveFlag(flags, SaveFlag.xAttributes))
            {
                Attributes.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.xWeaponAttributes))
            {
                WeaponAttributes.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
            {
                SkillBonuses.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.Slayer2))
            {
                writer.Write((int)m_Slayer2);
            }

            if (GetSaveFlag(flags, SaveFlag.ElementalDamages))
            {
                AosElementDamages.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.EngravedText))
            {
                writer.Write(m_EngravedText);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            var parentMobile = Parent as Mobile;

            switch (version)
            {
                case 9:
                case 8:
                case 7:
                case 6:
                case 5:
                    {
                        var flags = (SaveFlag)reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.DamageLevel))
                        {
                            m_DamageLevel = (WeaponDamageLevel)reader.ReadInt();

                            if (m_DamageLevel > WeaponDamageLevel.Vanq)
                            {
                                m_DamageLevel = WeaponDamageLevel.Ruin;
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.AccuracyLevel))
                        {
                            m_AccuracyLevel = (WeaponAccuracyLevel)reader.ReadInt();

                            if (m_AccuracyLevel > WeaponAccuracyLevel.Supremely)
                            {
                                m_AccuracyLevel = WeaponAccuracyLevel.Accurate;
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.DurabilityLevel))
                        {
                            m_DurabilityLevel = (WeaponDurabilityLevel)reader.ReadInt();

                            if (m_DurabilityLevel > WeaponDurabilityLevel.Indestructible)
                            {
                                m_DurabilityLevel = WeaponDurabilityLevel.Durable;
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Quality))
                        {
                            m_Quality = (WeaponQuality)reader.ReadInt();
                        }
                        else
                        {
                            m_Quality = WeaponQuality.Regular;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Hits))
                        {
                            m_Hits = reader.ReadInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.MaxHits))
                        {
                            m_MaxHits = reader.ReadInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Slayer))
                        {
                            m_Slayer = (SlayerName)reader.ReadInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Poison))
                        {
                            m_Poison = reader.ReadPoison();
                        }

                        if (GetSaveFlag(flags, SaveFlag.PoisonCharges))
                        {
                            m_PoisonCharges = reader.ReadInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Crafter))
                        {
                            m_Crafter = reader.ReadEntity<Mobile>();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Identified))
                        {
                            m_Identified = version >= 6 || reader.ReadBool();
                        }

                        if (GetSaveFlag(flags, SaveFlag.StrReq))
                        {
                            m_StrReq = reader.ReadInt();
                        }
                        else
                        {
                            m_StrReq = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.DexReq))
                        {
                            m_DexReq = reader.ReadInt();
                        }
                        else
                        {
                            m_DexReq = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.IntReq))
                        {
                            m_IntReq = reader.ReadInt();
                        }
                        else
                        {
                            m_IntReq = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.MinDamage))
                        {
                            m_MinDamage = reader.ReadInt();
                        }
                        else
                        {
                            m_MinDamage = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.MaxDamage))
                        {
                            m_MaxDamage = reader.ReadInt();
                        }
                        else
                        {
                            m_MaxDamage = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.HitSound))
                        {
                            m_HitSound = reader.ReadInt();
                        }
                        else
                        {
                            m_HitSound = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.MissSound))
                        {
                            m_MissSound = reader.ReadInt();
                        }
                        else
                        {
                            m_MissSound = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Speed))
                        {
                            if (version < 9)
                            {
                                m_Speed = reader.ReadInt();
                            }
                            else
                            {
                                m_Speed = reader.ReadFloat();
                            }
                        }
                        else
                        {
                            m_Speed = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.MaxRange))
                        {
                            m_MaxRange = reader.ReadInt();
                        }
                        else
                        {
                            m_MaxRange = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Skill))
                        {
                            m_Skill = (SkillName)reader.ReadInt();
                        }
                        else
                        {
                            m_Skill = (SkillName)(-1);
                        }

                        if (GetSaveFlag(flags, SaveFlag.Type))
                        {
                            m_Type = (WeaponType)reader.ReadInt();
                        }
                        else
                        {
                            m_Type = (WeaponType)(-1);
                        }

                        if (GetSaveFlag(flags, SaveFlag.Animation))
                        {
                            m_Animation = (WeaponAnimation)reader.ReadInt();
                        }
                        else
                        {
                            m_Animation = (WeaponAnimation)(-1);
                        }

                        if (GetSaveFlag(flags, SaveFlag.Resource))
                        {
                            m_Resource = (CraftResource)reader.ReadInt();
                        }
                        else
                        {
                            m_Resource = CraftResource.Iron;
                        }

                        Attributes = new AosAttributes(this);

                        if (GetSaveFlag(flags, SaveFlag.xAttributes))
                        {
                            Attributes.Deserialize(reader);
                        }

                        WeaponAttributes = new AosWeaponAttributes(this);

                        if (GetSaveFlag(flags, SaveFlag.xWeaponAttributes))
                        {
                            WeaponAttributes.Deserialize(reader);
                        }

                        if (UseSkillMod && m_AccuracyLevel != WeaponAccuracyLevel.Regular && parentMobile != null)
                        {
                            m_SkillMod = new DefaultSkillMod(AccuracySkill, "WeaponAccuracy", true, (int)m_AccuracyLevel * 5);
                            parentMobile.AddSkillMod(m_SkillMod);
                        }

                        if (version < 7 && WeaponAttributes.MageWeapon != 0)
                        {
                            WeaponAttributes.MageWeapon = 30 - WeaponAttributes.MageWeapon;
                        }

                        if (Core.AOS && WeaponAttributes.MageWeapon != 0 && WeaponAttributes.MageWeapon != 30 &&
                            parentMobile != null)
                        {
                            m_MageMod = new DefaultSkillMod(SkillName.Magery, "MageWeapon", true, -30 + WeaponAttributes.MageWeapon);
                            parentMobile.AddSkillMod(m_MageMod);
                        }

                        PlayerConstructed = GetSaveFlag(flags, SaveFlag.PlayerConstructed);

                        SkillBonuses = new AosSkillBonuses(this);

                        if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
                        {
                            SkillBonuses.Deserialize(reader);
                        }

                        if (GetSaveFlag(flags, SaveFlag.Slayer2))
                        {
                            m_Slayer2 = (SlayerName)reader.ReadInt();
                        }

                        AosElementDamages = new AosElementAttributes(this);

                        if (GetSaveFlag(flags, SaveFlag.ElementalDamages))
                        {
                            AosElementDamages.Deserialize(reader);
                        }

                        if (GetSaveFlag(flags, SaveFlag.EngravedText))
                        {
                            m_EngravedText = reader.ReadString();
                        }

                        break;
                    }
                case 4:
                    {
                        m_Slayer = (SlayerName)reader.ReadInt();

                        goto case 3;
                    }
                case 3:
                    {
                        m_StrReq = reader.ReadInt();
                        m_DexReq = reader.ReadInt();
                        m_IntReq = reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Identified = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_MaxRange = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version == 0)
                        {
                            m_MaxRange = 1; // default
                        }

                        // version < 5
                        m_Resource = CraftResource.Iron;
                        Attributes = new AosAttributes(this);
                        WeaponAttributes = new AosWeaponAttributes(this);
                        AosElementDamages = new AosElementAttributes(this);
                        SkillBonuses = new AosSkillBonuses(this);

                        m_MinDamage = reader.ReadInt();
                        m_MaxDamage = reader.ReadInt();

                        m_Speed = reader.ReadInt();

                        m_HitSound = reader.ReadInt();
                        m_MissSound = reader.ReadInt();

                        m_Skill = (SkillName)reader.ReadInt();
                        m_Type = (WeaponType)reader.ReadInt();
                        m_Animation = (WeaponAnimation)reader.ReadInt();
                        m_DamageLevel = (WeaponDamageLevel)reader.ReadInt();
                        m_AccuracyLevel = (WeaponAccuracyLevel)reader.ReadInt();
                        m_DurabilityLevel = (WeaponDurabilityLevel)reader.ReadInt();
                        m_Quality = (WeaponQuality)reader.ReadInt();

                        m_Crafter = reader.ReadEntity<Mobile>();

                        m_Poison = reader.ReadPoison();
                        m_PoisonCharges = reader.ReadInt();

                        if (m_StrReq == OldStrengthReq)
                        {
                            m_StrReq = -1;
                        }

                        if (m_DexReq == OldDexterityReq)
                        {
                            m_DexReq = -1;
                        }

                        if (m_IntReq == OldIntelligenceReq)
                        {
                            m_IntReq = -1;
                        }

                        if (m_MinDamage == OldMinDamage)
                        {
                            m_MinDamage = -1;
                        }

                        if (m_MaxDamage == OldMaxDamage)
                        {
                            m_MaxDamage = -1;
                        }

                        if (m_HitSound == OldHitSound)
                        {
                            m_HitSound = -1;
                        }

                        if (m_MissSound == OldMissSound)
                        {
                            m_MissSound = -1;
                        }

                        if (m_Speed == OldSpeed)
                        {
                            m_Speed = -1;
                        }

                        if (m_MaxRange == OldMaxRange)
                        {
                            m_MaxRange = -1;
                        }

                        if (m_Skill == OldSkill)
                        {
                            m_Skill = (SkillName)(-1);
                        }

                        if (m_Type == OldType)
                        {
                            m_Type = (WeaponType)(-1);
                        }

                        if (m_Animation == OldAnimation)
                        {
                            m_Animation = (WeaponAnimation)(-1);
                        }

                        if (UseSkillMod && m_AccuracyLevel != WeaponAccuracyLevel.Regular && parentMobile != null)
                        {
                            m_SkillMod = new DefaultSkillMod(AccuracySkill, "WeaponAccuracy", true, (int)m_AccuracyLevel * 5);
                            parentMobile.AddSkillMod(m_SkillMod);
                        }

                        break;
                    }
            }

            if (Core.AOS && parentMobile != null)
            {
                SkillBonuses.AddTo(parentMobile);
            }

            var strBonus = Attributes.BonusStr;
            var dexBonus = Attributes.BonusDex;
            var intBonus = Attributes.BonusInt;

            if (parentMobile != null && (strBonus != 0 || dexBonus != 0 || intBonus != 0))
            {
                var serial = Serial;

                if (strBonus != 0)
                {
                    parentMobile.AddStatMod(new StatMod(StatType.Str, $"{serial}Str", strBonus, TimeSpan.Zero));
                }

                if (dexBonus != 0)
                {
                    parentMobile.AddStatMod(new StatMod(StatType.Dex, $"{serial}Dex", dexBonus, TimeSpan.Zero));
                }

                if (intBonus != 0)
                {
                    parentMobile.AddStatMod(new StatMod(StatType.Int, $"{serial}Int", intBonus, TimeSpan.Zero));
                }
            }

            parentMobile?.CheckStatTimers();

            if (m_Hits <= 0 && m_MaxHits <= 0)
            {
                m_Hits = m_MaxHits = Utility.RandomMinMax(InitMinHits, InitMaxHits);
            }

            if (version < 6)
            {
                PlayerConstructed = true; // we don't know, so, assume it's crafted
            }
        }

        private class ResetEquipTimer : Timer
        {
            private readonly Mobile m_Mobile;

            public ResetEquipTimer(Mobile m, TimeSpan duration) : base(duration) => m_Mobile = m;

            protected override void OnTick()
            {
                m_Mobile.EndAction<BaseWeapon>();
            }
        }

        [Flags]
        private enum SaveFlag
        {
            None = 0x00000000,
            DamageLevel = 0x00000001,
            AccuracyLevel = 0x00000002,
            DurabilityLevel = 0x00000004,
            Quality = 0x00000008,
            Hits = 0x00000010,
            MaxHits = 0x00000020,
            Slayer = 0x00000040,
            Poison = 0x00000080,
            PoisonCharges = 0x00000100,
            Crafter = 0x00000200,
            Identified = 0x00000400,
            StrReq = 0x00000800,
            DexReq = 0x00001000,
            IntReq = 0x00002000,
            MinDamage = 0x00004000,
            MaxDamage = 0x00008000,
            HitSound = 0x00010000,
            MissSound = 0x00020000,
            Speed = 0x00040000,
            MaxRange = 0x00080000,
            Skill = 0x00100000,
            Type = 0x00200000,
            Animation = 0x00400000,
            Resource = 0x00800000,
            xAttributes = 0x01000000,
            xWeaponAttributes = 0x02000000,
            PlayerConstructed = 0x04000000,
            SkillBonuses = 0x08000000,
            Slayer2 = 0x10000000,
            ElementalDamages = 0x20000000,
            EngravedText = 0x40000000
        }
    }

    public enum CheckSlayerResult
    {
        None,
        Slayer,
        Opposition
    }
}
