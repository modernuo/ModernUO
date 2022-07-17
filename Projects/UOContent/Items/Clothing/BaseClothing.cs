using System;
using System.Collections.Generic;
using ModernUO.Serialization;
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

    [SerializationGenerator(7, false)]
    public abstract partial class BaseClothing : Item, IDyable, IScissorable, IFactionItem, ICraftable, IWearableDurability
    {
        [SerializableField(0, "private", "private")]
        private CraftResource _rawResource;

        [SerializableFieldSaveFlag(0)]
        private bool ShouldSerializeResource() => _rawResource != DefaultResource;

        [SerializableField(1, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        private AosAttributes _attributes;

        [SerializableFieldSaveFlag(1)]
        private bool ShouldSerializeAttributes() => !_attributes.IsEmpty;

        [SerializableFieldDefault(1)]
        private AosAttributes AttributesDefaultValue() => new(this);

        [SerializableField(2, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        private AosArmorAttributes _clothingAttributes;

        [SerializableFieldSaveFlag(2)]
        private bool ShouldSerializeClothingAttributes() => !_clothingAttributes.IsEmpty;

        [SerializableFieldDefault(2)]
        private AosArmorAttributes ClothingAttributesDefaultValue() => new(this);

        [SerializableField(3, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        private AosSkillBonuses _skillBonuses;

        [SerializableFieldSaveFlag(3)]
        private bool ShouldSerializeSkillBonuses() => !_skillBonuses.IsEmpty;

        [SerializableFieldDefault(3)]
        private AosSkillBonuses SkillBonusesDefaultValue() => new(this);

        [SerializableField(4, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        private AosElementAttributes _resistances;

        [SerializableFieldSaveFlag(4)]
        private bool ShouldSerializeResistances() => !_resistances.IsEmpty;

        [SerializableFieldDefault(4)]
        private AosElementAttributes ResistancesDefaultValue() => new(this);

        [EncodedInt]
        [InvalidateProperties]
        [SerializableField(5)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _maxHitPoints;

        [SerializableFieldSaveFlag(5)]
        private bool ShouldSerializeMaxHitPoints() => _maxHitPoints != 0;

        // Field 6
        private int _hitPoints;

        [SerializableField(7)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _playerConstructed;

        [SerializableFieldSaveFlag(7)]
        private bool ShouldSerializePlayerConstructed() => _playerConstructed;

        [InvalidateProperties]
        [SerializableField(8)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _crafter;

        [SerializableFieldSaveFlag(8)]
        private bool ShouldSerializeCrafter() => _crafter != null;

        [InvalidateProperties]
        [SerializableField(9)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private ClothingQuality _quality = ClothingQuality.Regular;

        [SerializableFieldSaveFlag(9)]
        private bool ShouldSerializeQuality() => _quality != ClothingQuality.Regular;

        // Field 10
        private int _strReq = -1;

        private FactionItem _factionState;

        public BaseClothing(int itemID, Layer layer, int hue = 0) : base(itemID)
        {
            Layer = layer;
            Hue = hue;

            _rawResource = DefaultResource;

            _hitPoints = _maxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);

            Attributes = new AosAttributes(this);
            ClothingAttributes = new AosArmorAttributes(this);
            SkillBonuses = new AosSkillBonuses(this);
            Resistances = new AosElementAttributes(this);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => _rawResource;
            set
            {
                RawResource = value;
                Hue = CraftResources.GetHue(_rawResource);
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [SerializableField(10)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int StrRequirement
        {
            get => _strReq == -1 ? Core.AOS ? AosStrReq : OldStrReq : _strReq;
            set
            {
                _strReq = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(10)]
        private bool ShouldSerializeStrReq() => _strReq != -1;

        public virtual CraftResource DefaultResource => CraftResource.None;

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
                Crafter = from.RawName;
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
            get => _factionState;
            set
            {
                _factionState = value;

                if (_factionState == null)
                {
                    Hue = 0;
                }

                LootType = _factionState == null ? LootType.Regular : LootType.Blessed;
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

            if (item?.Resources.Count == 1)
            {
                var resource = item.Resources[0];
                if (resource.Amount >= 2)
                {
                    try
                    {
                        var info = CraftResources.GetInfo(_rawResource);

                        Type resourceType = null;
                        if (info?.ResourceTypes.Length > 0)
                        {
                            resourceType = info.ResourceTypes[0];
                        }

                        var res = (resourceType ?? resource.ItemType).CreateInstance<Item>();

                        ScissorHelper(from, res, PlayerConstructed ? resource.Amount / 2 : 1);

                        res.LootType = LootType.Regular;

                        return true;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
        }

        public virtual bool CanFortify => true;

        [EncodedInt]
        [SerializableField(6)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int HitPoints
        {
            get => _hitPoints;
            set
            {
                if (value != _hitPoints && MaxHitPoints > 0)
                {
                    _hitPoints = value;

                    if (_hitPoints < 0)
                    {
                        Delete();
                    }
                    else if (_hitPoints > MaxHitPoints)
                    {
                        _hitPoints = MaxHitPoints;
                    }

                    InvalidateProperties();
                    this.MarkDirty();
                }
            }
        }

        [SerializableFieldSaveFlag(6)]
        private bool ShouldSerializeHitPoints() => _hitPoints != 0;

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

                    if (wear > 0 && _maxHitPoints > 0)
                    {
                        if (_hitPoints >= wear)
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
                            if (_maxHitPoints > wear)
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

            _hitPoints = (_hitPoints * 100 + (scale - 1)) / scale;
            _maxHitPoints = (_maxHitPoints * 100 + (scale - 1)) / scale;

            InvalidateProperties();
            this.MarkDirty();
        }

        public void ScaleDurability()
        {
            var scale = 100 + ClothingAttributes.DurabilityBonus;

            _hitPoints = (_hitPoints * scale + 99) / 100;
            _maxHitPoints = (_maxHitPoints * scale + 99) / 100;

            InvalidateProperties();
            this.MarkDirty();
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

        public int ComputeStatReq(StatType type) => AOS.Scale(StrRequirement, 100 - GetLowerStatReq());

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

            var serial = Serial;

            if (strBonus != 0)
            {
                parent.AddStatMod(new StatMod(StatType.Str, $"{serial}Str", strBonus, TimeSpan.Zero));
            }

            if (dexBonus != 0)
            {
                parent.AddStatMod(new StatMod(StatType.Dex, $"{serial}Dex", dexBonus, TimeSpan.Zero));
            }

            if (intBonus != 0)
            {
                parent.AddStatMod(new StatMod(StatType.Int, $"{serial}Int", intBonus, TimeSpan.Zero));
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

        public int GetLowerStatReq() => !Core.AOS ? 0 : ClothingAttributes.LowerStatReq;

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

                var serial = Serial;

                mob.RemoveStatMod($"{serial}Str");
                mob.RemoveStatMod($"{serial}Dex");
                mob.RemoveStatMod($"{serial}Int");

                mob.CheckStatTimers();
            }

            base.OnRemoved(parent);
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is not BaseClothing clothing)
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

        public override void AddNameProperty(IPropertyList list)
        {
            var oreType = _rawResource switch
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
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (_crafter != null)
            {
                list.Add(1050043, _crafter); // crafted by ~1_NAME~
            }

            if (_factionState != null)
            {
                list.Add(1041350); // faction item
            }

            if (_quality == ClothingQuality.Exceptional)
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

            SkillBonuses?.GetProperties(list);

            int prop;

            if ((prop = ArtifactRarity) > 0)
            {
                list.Add(1061078, prop); // artifact rarity ~1_val~
            }

            if ((prop = Attributes.WeaponDamage) != 0)
            {
                list.Add(1060401, prop); // damage increase ~1_val~%
            }

            if ((prop = Attributes.DefendChance) != 0)
            {
                list.Add(1060408, prop); // defense chance increase ~1_val~%
            }

            if ((prop = Attributes.BonusDex) != 0)
            {
                list.Add(1060409, prop); // dexterity bonus ~1_val~
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

            if ((prop = Attributes.AttackChance) != 0)
            {
                list.Add(1060415, prop); // hit chance increase ~1_val~%
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

            if ((prop = ClothingAttributes.LowerStatReq) != 0)
            {
                list.Add(1060435, prop); // lower requirements ~1_val~%
            }

            if ((prop = Attributes.Luck) != 0)
            {
                list.Add(1060436, prop); // luck ~1_val~
            }

            if (ClothingAttributes.MageArmor != 0)
            {
                list.Add(1060437); // mage armor
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

            if ((prop = ClothingAttributes.SelfRepair) != 0)
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

            AddResistanceProperties(list);

            if ((prop = ClothingAttributes.DurabilityBonus) > 0)
            {
                list.Add(1060410, prop); // durability ~1_val~%
            }

            if ((prop = ComputeStatReq(StatType.Str)) > 0)
            {
                list.Add(1061170, prop); // strength requirement ~1_val~
            }

            if (_hitPoints >= 0 && _maxHitPoints > 0)
            {
                list.Add(1060639, $"{_hitPoints}\t{_maxHitPoints}"); // durability ~1_val~ / ~2_val~
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

            from.NetState.SendDisplayEquipmentInfo(Serial, number, _crafter, false, attrs);
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

            if (_factionState != null)
            {
                attrs.Add(new EquipInfoAttribute(1041350)); // faction item
            }

            if (_quality == ClothingQuality.Exceptional)
            {
                attrs.Add(new EquipInfoAttribute(1018305 - (int)_quality));
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

        private static bool GetSaveFlag(OldSaveFlag flags, OldSaveFlag toGet) => (flags & toGet) != 0;

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (_maxHitPoints == 0 && _hitPoints == 0)
            {
                _hitPoints = _maxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);
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
        private enum OldSaveFlag
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
