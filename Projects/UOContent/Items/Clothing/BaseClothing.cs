using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Ethics;
using Server.Factions;
using Server.Network;
using Server.Utilities;

namespace Server.Items
{
    public enum ClothingQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public interface IArcaneEquip
    {
        bool IsArcane { get; }
        int CurArcaneCharges { get; set; }
        int MaxArcaneCharges { get; set; }
    }

    public abstract class BaseClothing : Item, IDyable, IScissorable, IFactionItem, ICraftable, IWearableDurability
    {
        private Mobile m_Crafter;

        private FactionItem m_FactionState;
        private int m_HitPoints;

        private int m_MaxHitPoints;
        private ClothingQuality m_Quality;
        protected CraftResource m_Resource;
        private int m_StrReq = -1;

        public BaseClothing(int itemID, Layer layer, int hue = 0) : base(itemID)
        {
            Layer = layer;
            Hue = hue;

            m_Resource = DefaultResource;
            m_Quality = ClothingQuality.Regular;

            m_HitPoints = m_MaxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);

            Attributes = new AosAttributes(this);
            ClothingAttributes = new AosArmorAttributes(this);
            SkillBonuses = new AosSkillBonuses(this);
            Resistances = new AosElementAttributes(this);
        }

        public BaseClothing(Serial serial) : base(serial)
        {
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
        public int StrRequirement
        {
            get => m_StrReq == -1 ? Core.AOS ? AosStrReq : OldStrReq : m_StrReq;
            set
            {
                m_StrReq = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ClothingQuality Quality
        {
            get => m_Quality;
            set
            {
                m_Quality = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PlayerConstructed { get; set; }

        public virtual CraftResource DefaultResource => CraftResource.None;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                m_Resource = value;
                Hue = CraftResources.GetHue(m_Resource);
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosAttributes Attributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosArmorAttributes ClothingAttributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosSkillBonuses SkillBonuses { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosElementAttributes Resistances { get; private set; }

        public virtual int BasePhysicalResistance => 0;
        public virtual int BaseFireResistance => 0;
        public virtual int BaseColdResistance => 0;
        public virtual int BasePoisonResistance => 0;
        public virtual int BaseEnergyResistance => 0;

        public override int PhysicalResistance => BasePhysicalResistance + Resistances.Physical;
        public override int FireResistance => BaseFireResistance + Resistances.Fire;
        public override int ColdResistance => BaseColdResistance + Resistances.Cold;
        public override int PoisonResistance => BasePoisonResistance + Resistances.Poison;
        public override int EnergyResistance => BaseEnergyResistance + Resistances.Energy;

        public virtual int ArtifactRarity => 0;

        public virtual int BaseStrBonus => 0;
        public virtual int BaseDexBonus => 0;
        public virtual int BaseIntBonus => 0;

        public virtual Race RequiredRace => null;

        public virtual int AosStrReq => 10;
        public virtual int OldStrReq => 0;

        public virtual bool AllowMaleWearer => true;
        public virtual bool AllowFemaleWearer => true;
        public virtual bool CanBeBlessed => true;

        public virtual int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            BaseTool tool, CraftItem craftItem, int resHue
        )
        {
            Quality = (ClothingQuality)quality;

            if (makersMark)
            {
                Crafter = from;
            }

            if (DefaultResource != CraftResource.None)
            {
                var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

                Resource = CraftResources.GetFromType(resourceType);
            }
            else
            {
                Hue = resHue;
            }

            PlayerConstructed = true;

            var context = craftSystem.GetContext(from);

            if (context?.DoNotColor == true)
            {
                Hue = 0;
            }

            return quality;
        }

        public virtual bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
            {
                return false;
            }

            if (RootParent is Mobile && from != RootParent)
            {
                return false;
            }

            Hue = sender.DyedHue;

            return true;
        }

        public FactionItem FactionItemState
        {
            get => m_FactionState;
            set
            {
                m_FactionState = value;

                if (m_FactionState == null)
                {
                    Hue = 0;
                }

                LootType = m_FactionState == null ? LootType.Regular : LootType.Blessed;
            }
        }

        public virtual bool Scissor(Mobile from, Scissors scissors)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack.
                return false;
            }

            if (Ethic.IsImbued(this))
            {
                from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
                return false;
            }

            var system = DefTailoring.CraftSystem;

            var item = system.CraftItems.SearchFor(GetType());

            if (item?.Resources.Count == 1 && item.Resources[0].Amount >= 2)
            {
                try
                {
                    var info = CraftResources.GetInfo(m_Resource);

                    var resourceType = info.ResourceTypes?[0] ?? item.Resources[0].ItemType;

                    var res = resourceType.CreateInstance<Item>();

                    ScissorHelper(from, res, PlayerConstructed ? item.Resources[0].Amount / 2 : 1);

                    res.LootType = LootType.Regular;

                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
        }

        public virtual bool CanFortify => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHitPoints
        {
            get => m_MaxHitPoints;
            set
            {
                m_MaxHitPoints = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitPoints
        {
            get => m_HitPoints;
            set
            {
                if (value != m_HitPoints && MaxHitPoints > 0)
                {
                    m_HitPoints = value;

                    if (m_HitPoints < 0)
                    {
                        Delete();
                    }
                    else if (m_HitPoints > MaxHitPoints)
                    {
                        m_HitPoints = MaxHitPoints;
                    }

                    InvalidateProperties();
                }
            }
        }

        public virtual int InitMinHits => 0;
        public virtual int InitMaxHits => 0;

        public virtual int OnHit(BaseWeapon weapon, int damageTaken)
        {
            var absorbed = Utility.RandomMinMax(1, 4);

            // Don't go below zero
            damageTaken = Math.Min(absorbed, damageTaken);

            if (Utility.Random(100) < 25) // 25% chance to lower durability
            {
                if (Core.AOS && ClothingAttributes.SelfRepair > Utility.Random(10))
                {
                    HitPoints += 2;
                }
                else
                {
                    int wear;

                    if (weapon.Type == WeaponType.Bashing)
                    {
                        wear = absorbed / 2;
                    }
                    else
                    {
                        wear = Utility.Random(2);
                    }

                    if (wear > 0 && m_MaxHitPoints > 0)
                    {
                        if (m_HitPoints >= wear)
                        {
                            HitPoints -= wear;
                            wear = 0;
                        }
                        else
                        {
                            wear -= HitPoints;
                            HitPoints = 0;
                        }

                        if (wear > 0)
                        {
                            if (m_MaxHitPoints > wear)
                            {
                                MaxHitPoints -= wear;

                                (Parent as Mobile)?.LocalOverheadMessage(
                                    MessageType.Regular,
                                    0x3B2,
                                    1061121
                                ); // Your equipment is severely damaged.
                            }
                            else
                            {
                                Delete();
                            }
                        }
                    }
                }
            }

            return damageTaken;
        }

        public void UnscaleDurability()
        {
            var scale = 100 + ClothingAttributes.DurabilityBonus;

            m_HitPoints = (m_HitPoints * 100 + (scale - 1)) / scale;
            m_MaxHitPoints = (m_MaxHitPoints * 100 + (scale - 1)) / scale;

            InvalidateProperties();
        }

        public void ScaleDurability()
        {
            var scale = 100 + ClothingAttributes.DurabilityBonus;

            m_HitPoints = (m_HitPoints * scale + 99) / 100;
            m_MaxHitPoints = (m_MaxHitPoints * scale + 99) / 100;

            InvalidateProperties();
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) =>
            Ethic.CheckTrade(from, to, newOwner, this) && base.AllowSecureTrade(from, to, newOwner, accepted);

        public override bool CanEquip(Mobile from)
        {
            if (!Ethic.CheckEquip(from, this))
            {
                return false;
            }

            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                if (RequiredRace != null && from.Race != RequiredRace)
                {
                    if (RequiredRace == Race.Elf)
                    {
                        from.SendLocalizedMessage(1072203); // Only Elves may use this.
                    }
                    else
                    {
                        from.SendMessage("Only {0} may use this.", RequiredRace.PluralName);
                    }

                    return false;
                }

                if (!AllowMaleWearer && !from.Female)
                {
                    if (AllowFemaleWearer)
                    {
                        from.SendLocalizedMessage(1010388); // Only females can wear this.
                    }
                    else
                    {
                        from.SendMessage("You may not wear this.");
                    }

                    return false;
                }

                if (!AllowFemaleWearer && from.Female)
                {
                    if (AllowMaleWearer)
                    {
                        from.SendLocalizedMessage(1063343); // Only males can wear this.
                    }
                    else
                    {
                        from.SendMessage("You may not wear this.");
                    }

                    return false;
                }

                var strBonus = ComputeStatBonus(StatType.Str);
                var strReq = ComputeStatReq(StatType.Str);

                if (from.Str < strReq || from.Str + strBonus < 1)
                {
                    from.SendLocalizedMessage(500213); // You are not strong enough to equip that.
                    return false;
                }
            }

            return base.CanEquip(from);
        }

        public int ComputeStatReq(StatType type)
        {
            int v;

            // if (type == StatType.Str)
            v = StrRequirement;

            return AOS.Scale(v, 100 - GetLowerStatReq());
        }

        public int ComputeStatBonus(StatType type) =>
            type switch
            {
                StatType.Str => BaseStrBonus + Attributes.BonusStr,
                StatType.Dex => BaseDexBonus + Attributes.BonusDex,
                _            => BaseIntBonus + Attributes.BonusInt
            };

        public virtual void AddStatBonuses(Mobile parent)
        {
            if (parent == null)
            {
                return;
            }

            var strBonus = ComputeStatBonus(StatType.Str);
            var dexBonus = ComputeStatBonus(StatType.Dex);
            var intBonus = ComputeStatBonus(StatType.Int);

            if (strBonus == 0 && dexBonus == 0 && intBonus == 0)
            {
                return;
            }

            var modName = Serial.ToString();

            if (strBonus != 0)
            {
                parent.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", strBonus, TimeSpan.Zero));
            }

            if (dexBonus != 0)
            {
                parent.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", dexBonus, TimeSpan.Zero));
            }

            if (intBonus != 0)
            {
                parent.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", intBonus, TimeSpan.Zero));
            }
        }

        public static void ValidateMobile(Mobile m)
        {
            for (var i = m.Items.Count - 1; i >= 0; --i)
            {
                if (i >= m.Items.Count)
                {
                    continue;
                }

                var item = m.Items[i];

                if (item is BaseClothing clothing)
                {
                    if (clothing.RequiredRace != null && m.Race != clothing.RequiredRace)
                    {
                        if (clothing.RequiredRace == Race.Elf)
                        {
                            m.SendLocalizedMessage(1072203); // Only Elves may use this.
                        }
                        else
                        {
                            m.SendMessage("Only {0} may use this.", clothing.RequiredRace.PluralName);
                        }

                        m.AddToBackpack(clothing);
                    }
                    else if (!clothing.AllowMaleWearer && !m.Female && m.AccessLevel < AccessLevel.GameMaster)
                    {
                        if (clothing.AllowFemaleWearer)
                        {
                            m.SendLocalizedMessage(1010388); // Only females can wear this.
                        }
                        else
                        {
                            m.SendMessage("You may not wear this.");
                        }

                        m.AddToBackpack(clothing);
                    }
                    else if (!clothing.AllowFemaleWearer && m.Female && m.AccessLevel < AccessLevel.GameMaster)
                    {
                        if (clothing.AllowMaleWearer)
                        {
                            m.SendLocalizedMessage(1063343); // Only males can wear this.
                        }
                        else
                        {
                            m.SendMessage("You may not wear this.");
                        }

                        m.AddToBackpack(clothing);
                    }
                }
            }
        }

        public int GetLowerStatReq()
        {
            if (!Core.AOS)
            {
                return 0;
            }

            return ClothingAttributes.LowerStatReq;
        }

        public override void OnAdded(IEntity parent)
        {
            if (parent is Mobile mob)
            {
                if (Core.AOS)
                {
                    SkillBonuses.AddTo(mob);
                }

                AddStatBonuses(mob);
                mob.CheckStatTimers();
            }

            base.OnAdded(parent);
        }

        public override void OnRemoved(IEntity parent)
        {
            if (parent is Mobile mob)
            {
                if (Core.AOS)
                {
                    SkillBonuses.Remove();
                }

                var modName = Serial.ToString();

                mob.RemoveStatMod($"{modName}Str");
                mob.RemoveStatMod($"{modName}Dex");
                mob.RemoveStatMod($"{modName}Int");

                mob.CheckStatTimers();
            }

            base.OnRemoved(parent);
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (!(newItem is BaseClothing clothing))
            {
                return;
            }

            clothing.Attributes = new AosAttributes(newItem, Attributes);
            clothing.Resistances = new AosElementAttributes(newItem, Resistances);
            clothing.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
            clothing.ClothingAttributes = new AosArmorAttributes(newItem, ClothingAttributes);
        }

        public override bool AllowEquippedCast(Mobile from) =>
            base.AllowEquippedCast(from) || Attributes.SpellChanneling != 0;

        public override bool CheckPropertyConflict(Mobile m)
        {
            if (base.CheckPropertyConflict(m))
            {
                return true;
            }

            return Layer switch
            {
                Layer.Pants => m.FindItemOnLayer(Layer.InnerLegs) != null,
                Layer.Shirt => m.FindItemOnLayer(Layer.InnerTorso) != null,
                _           => false
            };
        }

        private string GetNameString() => Name ?? $"#{LabelNumber}";

        public override void AddNameProperty(ObjectPropertyList list)
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

            if (oreType != 0)
            {
                list.Add(1053099, "#{0}\t{1}", oreType, GetNameString()); // ~1_oretype~ ~2_armortype~
            }
            else if (Name == null)
            {
                list.Add(LabelNumber);
            }
            else
            {
                list.Add(Name);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
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

            if (m_Quality == ClothingQuality.Exceptional)
            {
                list.Add(1060636); // exceptional
            }

            if (RequiredRace == Race.Elf)
            {
                list.Add(1075086); // Elves Only
            }

            SkillBonuses?.GetProperties(list);

            int prop;

            if ((prop = ArtifactRarity) > 0)
            {
                list.Add(1061078, prop.ToString()); // artifact rarity ~1_val~
            }

            if ((prop = Attributes.WeaponDamage) != 0)
            {
                list.Add(1060401, prop.ToString()); // damage increase ~1_val~%
            }

            if ((prop = Attributes.DefendChance) != 0)
            {
                list.Add(1060408, prop.ToString()); // defense chance increase ~1_val~%
            }

            if ((prop = Attributes.BonusDex) != 0)
            {
                list.Add(1060409, prop.ToString()); // dexterity bonus ~1_val~
            }

            if ((prop = Attributes.EnhancePotions) != 0)
            {
                list.Add(1060411, prop.ToString()); // enhance potions ~1_val~%
            }

            if ((prop = Attributes.CastRecovery) != 0)
            {
                list.Add(1060412, prop.ToString()); // faster cast recovery ~1_val~
            }

            if ((prop = Attributes.CastSpeed) != 0)
            {
                list.Add(1060413, prop.ToString()); // faster casting ~1_val~
            }

            if ((prop = Attributes.AttackChance) != 0)
            {
                list.Add(1060415, prop.ToString()); // hit chance increase ~1_val~%
            }

            if ((prop = Attributes.BonusHits) != 0)
            {
                list.Add(1060431, prop.ToString()); // hit point increase ~1_val~
            }

            if ((prop = Attributes.BonusInt) != 0)
            {
                list.Add(1060432, prop.ToString()); // intelligence bonus ~1_val~
            }

            if ((prop = Attributes.LowerManaCost) != 0)
            {
                list.Add(1060433, prop.ToString()); // lower mana cost ~1_val~%
            }

            if ((prop = Attributes.LowerRegCost) != 0)
            {
                list.Add(1060434, prop.ToString()); // lower reagent cost ~1_val~%
            }

            if ((prop = ClothingAttributes.LowerStatReq) != 0)
            {
                list.Add(1060435, prop.ToString()); // lower requirements ~1_val~%
            }

            if ((prop = Attributes.Luck) != 0)
            {
                list.Add(1060436, prop.ToString()); // luck ~1_val~
            }

            if ((prop = ClothingAttributes.MageArmor) != 0)
            {
                list.Add(1060437); // mage armor
            }

            if ((prop = Attributes.BonusMana) != 0)
            {
                list.Add(1060439, prop.ToString()); // mana increase ~1_val~
            }

            if ((prop = Attributes.RegenMana) != 0)
            {
                list.Add(1060440, prop.ToString()); // mana regeneration ~1_val~
            }

            if ((prop = Attributes.NightSight) != 0)
            {
                list.Add(1060441); // night sight
            }

            if ((prop = Attributes.ReflectPhysical) != 0)
            {
                list.Add(1060442, prop.ToString()); // reflect physical damage ~1_val~%
            }

            if ((prop = Attributes.RegenStam) != 0)
            {
                list.Add(1060443, prop.ToString()); // stamina regeneration ~1_val~
            }

            if ((prop = Attributes.RegenHits) != 0)
            {
                list.Add(1060444, prop.ToString()); // hit point regeneration ~1_val~
            }

            if ((prop = ClothingAttributes.SelfRepair) != 0)
            {
                list.Add(1060450, prop.ToString()); // self repair ~1_val~
            }

            if ((prop = Attributes.SpellChanneling) != 0)
            {
                list.Add(1060482); // spell channeling
            }

            if ((prop = Attributes.SpellDamage) != 0)
            {
                list.Add(1060483, prop.ToString()); // spell damage increase ~1_val~%
            }

            if ((prop = Attributes.BonusStam) != 0)
            {
                list.Add(1060484, prop.ToString()); // stamina increase ~1_val~
            }

            if ((prop = Attributes.BonusStr) != 0)
            {
                list.Add(1060485, prop.ToString()); // strength bonus ~1_val~
            }

            if ((prop = Attributes.WeaponSpeed) != 0)
            {
                list.Add(1060486, prop.ToString()); // swing speed increase ~1_val~%
            }

            if (Core.ML && (prop = Attributes.IncreasedKarmaLoss) != 0)
            {
                list.Add(1075210, prop.ToString()); // Increased Karma Loss ~1val~%
            }

            AddResistanceProperties(list);

            if ((prop = ClothingAttributes.DurabilityBonus) > 0)
            {
                list.Add(1060410, prop.ToString()); // durability ~1_val~%
            }

            if ((prop = ComputeStatReq(StatType.Str)) > 0)
            {
                list.Add(1061170, prop.ToString()); // strength requirement ~1_val~
            }

            if (m_HitPoints >= 0 && m_MaxHitPoints > 0)
            {
                list.Add(1060639, "{0}\t{1}", m_HitPoints, m_MaxHitPoints); // durability ~1_val~ / ~2_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            var attrs = new List<EquipInfoAttribute>();

            AddEquipInfoAttributes(from, attrs);

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

            from.NetState.SendDisplayEquipmentInfo(Serial, number, m_Crafter?.RawName, false, attrs);
        }

        public virtual void AddEquipInfoAttributes(Mobile from, List<EquipInfoAttribute> attrs)
        {
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

            if (m_Quality == ClothingQuality.Exceptional)
            {
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));
            }
        }

        public void DistributeBonuses(int amount)
        {
            for (var i = 0; i < amount; ++i)
            {
                switch (Utility.Random(5))
                {
                    case 0:
                        ++Resistances.Physical;
                        break;
                    case 1:
                        ++Resistances.Fire;
                        break;
                    case 2:
                        ++Resistances.Cold;
                        break;
                    case 3:
                        ++Resistances.Poison;
                        break;
                    case 4:
                        ++Resistances.Energy;
                        break;
                }
            }

            InvalidateProperties();
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

            writer.Write(5); // version

            var flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.Resource, m_Resource != DefaultResource);
            SetSaveFlag(ref flags, SaveFlag.Attributes, !Attributes.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.ClothingAttributes, !ClothingAttributes.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.SkillBonuses, !SkillBonuses.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.Resistances, !Resistances.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.MaxHitPoints, m_MaxHitPoints != 0);
            SetSaveFlag(ref flags, SaveFlag.HitPoints, m_HitPoints != 0);
            SetSaveFlag(ref flags, SaveFlag.PlayerConstructed, PlayerConstructed);
            SetSaveFlag(ref flags, SaveFlag.Crafter, m_Crafter != null);
            SetSaveFlag(ref flags, SaveFlag.Quality, m_Quality != ClothingQuality.Regular);
            SetSaveFlag(ref flags, SaveFlag.StrReq, m_StrReq != -1);

            writer.WriteEncodedInt((int)flags);

            if (GetSaveFlag(flags, SaveFlag.Resource))
            {
                writer.WriteEncodedInt((int)m_Resource);
            }

            if (GetSaveFlag(flags, SaveFlag.Attributes))
            {
                Attributes.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.ClothingAttributes))
            {
                ClothingAttributes.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
            {
                SkillBonuses.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.Resistances))
            {
                Resistances.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.MaxHitPoints))
            {
                writer.WriteEncodedInt(m_MaxHitPoints);
            }

            if (GetSaveFlag(flags, SaveFlag.HitPoints))
            {
                writer.WriteEncodedInt(m_HitPoints);
            }

            if (GetSaveFlag(flags, SaveFlag.Crafter))
            {
                writer.Write(m_Crafter);
            }

            if (GetSaveFlag(flags, SaveFlag.Quality))
            {
                writer.WriteEncodedInt((int)m_Quality);
            }

            if (GetSaveFlag(flags, SaveFlag.StrReq))
            {
                writer.WriteEncodedInt(m_StrReq);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {
                        var flags = (SaveFlag)reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Resource))
                        {
                            m_Resource = (CraftResource)reader.ReadEncodedInt();
                        }
                        else
                        {
                            m_Resource = DefaultResource;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Attributes))
                        {
                            Attributes = new AosAttributes(this, reader);
                        }
                        else
                        {
                            Attributes = new AosAttributes(this);
                        }

                        if (GetSaveFlag(flags, SaveFlag.ClothingAttributes))
                        {
                            ClothingAttributes = new AosArmorAttributes(this, reader);
                        }
                        else
                        {
                            ClothingAttributes = new AosArmorAttributes(this);
                        }

                        if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
                        {
                            SkillBonuses = new AosSkillBonuses(this, reader);
                        }
                        else
                        {
                            SkillBonuses = new AosSkillBonuses(this);
                        }

                        if (GetSaveFlag(flags, SaveFlag.Resistances))
                        {
                            Resistances = new AosElementAttributes(this, reader);
                        }
                        else
                        {
                            Resistances = new AosElementAttributes(this);
                        }

                        if (GetSaveFlag(flags, SaveFlag.MaxHitPoints))
                        {
                            m_MaxHitPoints = reader.ReadEncodedInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.HitPoints))
                        {
                            m_HitPoints = reader.ReadEncodedInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Crafter))
                        {
                            m_Crafter = reader.ReadEntity<Mobile>();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Quality))
                        {
                            m_Quality = (ClothingQuality)reader.ReadEncodedInt();
                        }
                        else
                        {
                            m_Quality = ClothingQuality.Regular;
                        }

                        if (GetSaveFlag(flags, SaveFlag.StrReq))
                        {
                            m_StrReq = reader.ReadEncodedInt();
                        }
                        else
                        {
                            m_StrReq = -1;
                        }

                        if (GetSaveFlag(flags, SaveFlag.PlayerConstructed))
                        {
                            PlayerConstructed = true;
                        }

                        break;
                    }
                case 4:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();

                        goto case 3;
                    }
                case 3:
                    {
                        Attributes = new AosAttributes(this, reader);
                        ClothingAttributes = new AosArmorAttributes(this, reader);
                        SkillBonuses = new AosSkillBonuses(this, reader);
                        Resistances = new AosElementAttributes(this, reader);

                        goto case 2;
                    }
                case 2:
                    {
                        PlayerConstructed = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Crafter = reader.ReadEntity<Mobile>();
                        m_Quality = (ClothingQuality)reader.ReadInt();
                        break;
                    }
                case 0:
                    {
                        m_Crafter = null;
                        m_Quality = ClothingQuality.Regular;
                        break;
                    }
            }

            if (version < 2)
            {
                PlayerConstructed = true; // we don't know, so, assume it's crafted
            }

            if (version < 3)
            {
                Attributes = new AosAttributes(this);
                ClothingAttributes = new AosArmorAttributes(this);
                SkillBonuses = new AosSkillBonuses(this);
                Resistances = new AosElementAttributes(this);
            }

            if (version < 4)
            {
                m_Resource = DefaultResource;
            }

            if (m_MaxHitPoints == 0 && m_HitPoints == 0)
            {
                m_HitPoints = m_MaxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);
            }

            if (Parent is Mobile parent)
            {
                if (Core.AOS)
                {
                    SkillBonuses.AddTo(parent);
                }

                AddStatBonuses(parent);
                parent.CheckStatTimers();
            }
        }

        [Flags]
        private enum SaveFlag
        {
            None = 0x00000000,
            Resource = 0x00000001,
            Attributes = 0x00000002,
            ClothingAttributes = 0x00000004,
            SkillBonuses = 0x00000008,
            Resistances = 0x00000010,
            MaxHitPoints = 0x00000020,
            HitPoints = 0x00000040,
            PlayerConstructed = 0x00000080,
            Crafter = 0x00000100,
            Quality = 0x00000200,
            StrReq = 0x00000400
        }
    }
}
