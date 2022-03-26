using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Ethics;
using Server.Factions;
using Server.Network;
using Server.Utilities;
using AMA = Server.Items.ArmorMeditationAllowance;
using AMT = Server.Items.ArmorMaterialType;

namespace Server.Items
{
    [Serializable(9, false)]
    public abstract partial class BaseArmor : Item, IScissorable, IFactionItem, ICraftable, IWearableDurability
    {
        [SerializableField(0, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        private AosAttributes _attributes;

        [SerializableFieldSaveFlag(0)]
        private bool ShouldSerializeAosAttributes() => !_attributes.IsEmpty;

        [SerializableFieldDefault(0)]
        private AosAttributes AttributesDefaultValue() => new(this);

        [SerializableField(1, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        private AosArmorAttributes _armorAttributes;

        [SerializableFieldSaveFlag(1)]
        private bool ShouldSerializeArmorAttributes() => !_armorAttributes.IsEmpty;

        [SerializableFieldDefault(1)]
        private AosArmorAttributes ArmorAttributesDefaultValue() => new(this);

        [EncodedInt]
        [InvalidateProperties]
        [SerializableField(2)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _physicalBonus;

        [SerializableFieldSaveFlag(2)]
        private bool ShouldSerializePhysicalBonus() => _physicalBonus != 0;

        [EncodedInt]
        [InvalidateProperties]
        [SerializableField(3)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _fireBonus;

        [SerializableFieldSaveFlag(3)]
        private bool ShouldSerializeFireBonus() => _fireBonus != 0;

        [EncodedInt]
        [InvalidateProperties]
        [SerializableField(4)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _coldBonus;

        [SerializableFieldSaveFlag(4)]
        private bool ShouldSerializeColdBonus() => _coldBonus != 0;

        [EncodedInt]
        [InvalidateProperties]
        [SerializableField(5)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _poisonBonus;

        [SerializableFieldSaveFlag(5)]
        private bool ShouldSerializePoisonBonus() => _poisonBonus != 0;

        [EncodedInt]
        [InvalidateProperties]
        [SerializableField(6)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _energyBonus;

        [SerializableFieldSaveFlag(6)]
        private bool ShouldSerializeEnergyBonus() => _energyBonus != 0;

        [SerializableField(7)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _identified;

        [SerializableFieldSaveFlag(7)]
        private bool ShouldSerializeIdentified() => _identified;

        [EncodedInt]
        [SerializableField(8)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _maxHitPoints;

        [SerializableFieldSaveFlag(8)]
        private bool ShouldSerializeMaxHitPoints() => _maxHitPoints != 0;

        // Field 9
        private int _hitPoints;

        [InvalidateProperties]
        [SerializableField(10)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _crafter;

        [SerializableFieldSaveFlag(10)]
        private bool ShouldSerializeCrafter() => _crafter != null;

        // Field 11
        private ArmorQuality _quality = ArmorQuality.Regular;

        // Field 12
        private ArmorDurabilityLevel _durability = ArmorDurabilityLevel.Regular;

        // Field 13
        private ArmorProtectionLevel _protection = ArmorProtectionLevel.Regular;

        // Field 14
        [SerializableField(14, "private", "private")]
        private CraftResource _rawResource;

        [SerializableFieldSaveFlag(14)]
        private bool ShouldSerializeResource() => _rawResource != DefaultResource;

        // Field 15
        private int _armorBase = -1;

        // Field 16
        private int _strBonus = -1;

        // Field 17
        private int _dexBonus = -1;

        // Field 18
        private int _intBonus = -1;

        // Field 19
        private int _strReq = -1;

        // Field 20
        private int _dexReq = -1;

        // Field 21
        private int _intReq = -1;

        // Field 22
        private AMA _meditate = (AMA)(-1);

        [SerializableField(23, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        public AosSkillBonuses _skillBonuses;

        [SerializableFieldSaveFlag(23)]
        private bool ShouldSerializeSkillBonuses() => !_skillBonuses.IsEmpty;

        [SerializableFieldDefault(23)]
        private AosSkillBonuses SkillBonusesDefaultValue() => new(this);

        [SerializableField(24)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        public bool _playerConstructed;

        [SerializableFieldSaveFlag(24)]
        private bool ShouldSerializePlayerConstructed() => _playerConstructed;

        private FactionItem m_FactionState;

        public BaseArmor(int itemID) : base(itemID)
        {
            _crafter = null;

            _rawResource = DefaultResource;
            Hue = CraftResources.GetHue(_rawResource);

            _hitPoints = _maxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);

            Layer = (Layer)ItemData.Quality;

            Attributes = new AosAttributes(this);
            ArmorAttributes = new AosArmorAttributes(this);
            SkillBonuses = new AosSkillBonuses(this);
        }

        public virtual bool AllowMaleWearer => true;
        public virtual bool AllowFemaleWearer => true;

        public abstract AMT MaterialType { get; }

        public virtual int RevertArmorBase => ArmorBase;
        public virtual int ArmorBase => 0;

        public virtual AMA DefMedAllowance => AMA.None;
        public virtual AMA AosMedAllowance => DefMedAllowance;
        public virtual AMA OldMedAllowance => DefMedAllowance;

        public virtual int AosStrBonus => 0;
        public virtual int AosDexBonus => 0;
        public virtual int AosIntBonus => 0;
        public virtual int AosStrReq => 0;
        public virtual int AosDexReq => 0;
        public virtual int AosIntReq => 0;

        public virtual int OldStrBonus => 0;
        public virtual int OldDexBonus => 0;
        public virtual int OldIntBonus => 0;
        public virtual int OldStrReq => 0;
        public virtual int OldDexReq => 0;
        public virtual int OldIntReq => 0;

        [SerializableField(11)]
        [CommandProperty(AccessLevel.GameMaster)]
        public ArmorQuality Quality
        {
            get => _quality;
            set
            {
                if (World.Loading)
                {
                    _quality = value;
                    return;
                }

                UnscaleDurability();
                _quality = value;
                ScaleDurability();
            }
        }

        [SerializableFieldSaveFlag(11)]
        private bool ShouldSerializeArmorQuality() => _quality != ArmorQuality.Regular;

        [SerializableField(12)]
        [CommandProperty(AccessLevel.GameMaster)]
        public ArmorDurabilityLevel Durability
        {
            get => _durability;
            set
            {
                if (World.Loading)
                {
                    _durability = value;
                    return;
                }

                UnscaleDurability();
                _durability = value;
                ScaleDurability();
            }
        }

        [SerializableFieldSaveFlag(12)]
        private bool ShouldSerializeDurability() => _durability != ArmorDurabilityLevel.Regular;

        [SerializableField(13)]
        [CommandProperty(AccessLevel.GameMaster)]
        public ArmorProtectionLevel ProtectionLevel
        {
            get => _protection;
            set
            {
                if (_protection != value)
                {
                    _protection = value;

                    Invalidate();
                    InvalidateProperties();

                    (Parent as Mobile)?.UpdateResistances();
                    this.MarkDirty();
                }
            }
        }

        [SerializableFieldSaveFlag(13)]
        private bool ShouldSerializeProtectionLevel() => _protection != ArmorProtectionLevel.Regular;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => _rawResource;
            set
            {
                if (_rawResource != value)
                {
                    UnscaleDurability();

                    RawResource = value;

                    if (CraftItem.RetainsColor(GetType()))
                    {
                        Hue = CraftResources.GetHue(_rawResource);
                    }

                    Invalidate();
                    (Parent as Mobile)?.UpdateResistances();

                    ScaleDurability();
                }
            }
        }

        [SerializableFieldDefault(14)]
        private CraftResource ResourceDefaultValue() => DefaultResource;

        [EncodedInt]
        [SerializableField(15)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int BaseArmorRating
        {
            get => _armorBase == -1 ? ArmorBase : _armorBase;
            set
            {
                _armorBase = value;
                Invalidate();
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(15)]
        private bool ShouldSerializeArmorBase() => _armorBase != -1;

        [SerializableFieldDefault(15)]
        private int ArmorBaseDefaultValue() => -1;

        public double BaseArmorRatingScaled => BaseArmorRating * ArmorScalar;

        public virtual double ArmorRating
        {
            get
            {
                var ar = BaseArmorRating;

                if (_protection != ArmorProtectionLevel.Regular)
                {
                    ar += 10 + 5 * (int)_protection;
                }

                ar += _rawResource switch
                {
                    CraftResource.DullCopper    => 2,
                    CraftResource.ShadowIron    => 4,
                    CraftResource.Copper        => 6,
                    CraftResource.Bronze        => 8,
                    CraftResource.Gold          => 10,
                    CraftResource.Agapite       => 12,
                    CraftResource.Verite        => 14,
                    CraftResource.Valorite      => 16,
                    CraftResource.SpinedLeather => 10,
                    CraftResource.HornedLeather => 13,
                    CraftResource.BarbedLeather => 16,
                    _                           => 0
                };

                ar += -8 + 8 * (int)_quality;
                return ScaleArmorByDurability(ar);
            }
        }

        public double ArmorRatingScaled => ArmorRating * ArmorScalar;

        [EncodedInt]
        [SerializableField(16)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int StrBonus
        {
            get => _strBonus == -1 ? Core.AOS ? AosStrBonus : OldStrBonus : _strBonus;
            set
            {
                _strBonus = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(16)]
        private bool ShouldSerializeStrBonus() => _strBonus != -1;

        [SerializableFieldDefault(16)]
        private int StrBonusDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(17)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int DexBonus
        {
            get => _dexBonus == -1 ? Core.AOS ? AosDexBonus : OldDexBonus : _dexBonus;
            set
            {
                _dexBonus = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(17)]
        private bool ShouldSerializeDexBonus() => _dexBonus != -1;

        [SerializableFieldDefault(17)]
        private int DexBonusDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(18)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int IntBonus
        {
            get => _intBonus == -1 ? Core.AOS ? AosIntBonus : OldIntBonus : _intBonus;
            set
            {
                _intBonus = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(18)]
        private bool ShouldSerializeIntBonus() => _intBonus != -1;

        [SerializableFieldDefault(18)]
        private int IntBonusDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(19)]
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

        [SerializableFieldSaveFlag(19)]
        private bool ShouldSerializeStrReq() => _strReq != -1;

        [SerializableFieldDefault(19)]
        private int StrReqDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(20)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int DexRequirement
        {
            get => _dexReq == -1 ? Core.AOS ? AosDexReq : OldDexReq : _dexReq;
            set
            {
                _dexReq = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(20)]
        private bool ShouldSerializeDexReq() => _dexReq != -1;

        [SerializableFieldDefault(20)]
        private int DexReqDefaultValue() => -1;

        [EncodedInt]
        [SerializableField(21)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int IntRequirement
        {
            get => _intReq == -1 ? Core.AOS ? AosIntReq : OldIntReq : _intReq;
            set
            {
                _intReq = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(21)]
        private bool ShouldSerializeIntReq() => _intReq != -1;

        [SerializableFieldDefault(21)]
        private int IntReqDefaultValue() => -1;

        [SerializableField(22)]
        [CommandProperty(AccessLevel.GameMaster)]
        public AMA MeditationAllowance
        {
            get => _meditate < AMA.All ? Core.AOS ? AosMedAllowance : OldMedAllowance : _meditate;
            set
            {
                _meditate = value;
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(22)]
        private bool ShouldSerializeMeditationAllowance() => _meditate >= AMA.All;

        public virtual double ArmorScalar
        {
            get
            {
                var pos = (int)BodyPosition;

                if (pos >= 0 && pos < ArmorScalars.Length)
                {
                    return ArmorScalars[pos];
                }

                return 1.0;
            }
        }

        public virtual int ArtifactRarity => 0;

        public virtual int BasePhysicalResistance => 0;
        public virtual int BaseFireResistance => 0;
        public virtual int BaseColdResistance => 0;
        public virtual int BasePoisonResistance => 0;
        public virtual int BaseEnergyResistance => 0;

        public override int PhysicalResistance => BasePhysicalResistance + GetProtOffset() +
                                                  GetResourceAttrs().ArmorPhysicalResist + _physicalBonus;

        public override int FireResistance =>
            BaseFireResistance + GetProtOffset() + GetResourceAttrs().ArmorFireResist + _fireBonus;

        public override int ColdResistance =>
            BaseColdResistance + GetProtOffset() + GetResourceAttrs().ArmorColdResist + _coldBonus;

        public override int PoisonResistance =>
            BasePoisonResistance + GetProtOffset() + GetResourceAttrs().ArmorPoisonResist + _poisonBonus;

        public override int EnergyResistance =>
            BaseEnergyResistance + GetProtOffset() + GetResourceAttrs().ArmorEnergyResist + _energyBonus;

        [CommandProperty(AccessLevel.GameMaster)]
        public ArmorBodyType BodyPosition =>
            Layer switch
            {
                Layer.Neck       => ArmorBodyType.Gorget,
                Layer.TwoHanded  => ArmorBodyType.Shield,
                Layer.Gloves     => ArmorBodyType.Gloves,
                Layer.Helm       => ArmorBodyType.Helmet,
                Layer.Arms       => ArmorBodyType.Arms,
                Layer.InnerLegs  => ArmorBodyType.Legs,
                Layer.OuterLegs  => ArmorBodyType.Legs,
                Layer.Pants      => ArmorBodyType.Legs,
                Layer.InnerTorso => ArmorBodyType.Chest,
                Layer.OuterTorso => ArmorBodyType.Chest,
                Layer.Shirt      => ArmorBodyType.Chest,
                _                => ArmorBodyType.Gorget
            };

        public static double[] ArmorScalars { get; set; } = { 0.07, 0.07, 0.14, 0.15, 0.22, 0.35 };

        public virtual CraftResource DefaultResource => CraftResource.Iron;

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

        public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
        {
            Quality = (ArmorQuality)quality;

            if (makersMark)
            {
                Crafter = from.RawName;
            }

            var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

            Resource = CraftResources.GetFromType(resourceType);
            PlayerConstructed = true;

            var context = craftSystem.GetContext(from);

            if (context?.DoNotColor == true)
            {
                Hue = 0;
            }

            if (Quality == ArmorQuality.Exceptional)
            {
                if (!(Core.ML && this is BaseShield)
                ) // Guessed Core.ML removed exceptional resist bonuses from crafted shields
                {
                    DistributeBonuses(
                        tool is BaseRunicTool ? 6 :
                        Core.SE ? 15 : 14
                    ); // Not sure since when, but right now 15 points are added, not 14.
                }

                if (Core.ML && this is not BaseShield)
                {
                    var bonus = (int)(from.Skills.ArmsLore.Value / 20);

                    for (var i = 0; i < bonus; i++)
                    {
                        switch (Utility.Random(5))
                        {
                            case 0:
                                PhysicalBonus++;
                                break;
                            case 1:
                                FireBonus++;
                                break;
                            case 2:
                                ColdBonus++;
                                break;
                            case 3:
                                EnergyBonus++;
                                break;
                            case 4:
                                PoisonBonus++;
                                break;
                        }
                    }

                    from.CheckSkill(SkillName.ArmsLore, 0, 100);
                }
            }

            if (Core.AOS)
            {
                (tool as BaseRunicTool)?.ApplyAttributesTo(this);
            }

            return quality;
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

        public bool Scissor(Mobile from, Scissors scissors)
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
                    var res = CraftResources.GetInfo(_rawResource).ResourceTypes[0].CreateInstance<Item>();

                    ScissorHelper(from, res, PlayerConstructed ? item.Resources[0].Amount / 2 : 1);
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

        [EncodedInt]
        [SerializableField(9)]
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
                        return;
                    }

                    if (_hitPoints > MaxHitPoints)
                    {
                        _hitPoints = MaxHitPoints;
                    }

                    InvalidateProperties();
                    this.MarkDirty();
                }
            }
        }

        [SerializableFieldSaveFlag(9)]
        private bool ShouldSerializeHitPoints() => _hitPoints != 0;

        public virtual int InitMinHits => 0;
        public virtual int InitMaxHits => 0;

        public void UnscaleDurability()
        {
            var scale = 100 + GetDurabilityBonus();

            _maxHitPoints = (_maxHitPoints * 100 + (scale - 1)) / scale;
            _hitPoints = (_hitPoints * 100 + (scale - 1)) / scale;
            InvalidateProperties();
            this.MarkDirty();
        }

        public void ScaleDurability()
        {
            var scale = 100 + GetDurabilityBonus();

            _maxHitPoints = (_maxHitPoints * scale + 99) / 100;
            _hitPoints = (_hitPoints * scale + 99) / 100;
            InvalidateProperties();
            this.MarkDirty();
        }

        public virtual int OnHit(BaseWeapon weapon, int damageTaken)
        {
            var halfar = ArmorRating / 2.0;
            var absorbed = (int)(halfar + halfar * Utility.RandomDouble());

            // Don't go below zero
            damageTaken = Math.Min(absorbed, damageTaken);

            if (absorbed < 2)
            {
                absorbed = 2;
            }

            if (Utility.Random(100) < 25) // 25% chance to lower durability
            {
                if (Core.AOS && ArmorAttributes.SelfRepair > Utility.Random(10))
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

                                if (Parent is Mobile mobile)
                                {
                                    mobile.LocalOverheadMessage(
                                        MessageType.Regular,
                                        0x3B2,
                                        1061121
                                    ); // Your equipment is severely damaged.
                                }
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

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is not BaseArmor armor)
            {
                return;
            }

            armor.Attributes = new AosAttributes(newItem, Attributes);
            armor.ArmorAttributes = new AosArmorAttributes(newItem, ArmorAttributes);
            armor.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
        }

        public int ComputeStatReq(StatType type)
        {
            int v = type switch
            {
                StatType.Str => StrRequirement,
                StatType.Dex => DexRequirement,
                _            => IntRequirement
            };

            return AOS.Scale(v, 100 - GetLowerStatReq());
        }

        public int ComputeStatBonus(StatType type)
        {
            return type switch
            {
                StatType.Str => StrBonus + Attributes.BonusStr,
                StatType.Dex => DexBonus + Attributes.BonusDex,
                _            => IntBonus + Attributes.BonusInt
            };
        }

        public void DistributeBonuses(int amount)
        {
            for (var i = 0; i < amount; ++i)
            {
                switch (Utility.Random(5))
                {
                    case 0:
                        ++PhysicalBonus;
                        break;
                    case 1:
                        ++FireBonus;
                        break;
                    case 2:
                        ++ColdBonus;
                        break;
                    case 3:
                        ++PoisonBonus;
                        break;
                    case 4:
                        ++EnergyBonus;
                        break;
                }
            }

            InvalidateProperties();
        }

        public CraftAttributeInfo GetResourceAttrs() =>
            CraftResources.GetInfo(_rawResource)?.AttributeInfo ?? CraftAttributeInfo.Blank;

        public int GetProtOffset()
        {
            return _protection switch
            {
                ArmorProtectionLevel.Guarding        => 1,
                ArmorProtectionLevel.Hardening       => 2,
                ArmorProtectionLevel.Fortification   => 3,
                ArmorProtectionLevel.Invulnerability => 4,
                _                                    => 0
            };
        }

        public int GetDurabilityBonus()
        {
            var bonus = _quality == ArmorQuality.Exceptional ? 20 : 0;

            bonus += _durability switch
            {
                ArmorDurabilityLevel.Durable        => 20,
                ArmorDurabilityLevel.Substantial    => 50,
                ArmorDurabilityLevel.Massive        => 70,
                ArmorDurabilityLevel.Fortified      => 100,
                ArmorDurabilityLevel.Indestructible => 120,
                _                                   => 0
            };

            if (Core.AOS)
            {
                bonus += ArmorAttributes.DurabilityBonus;

                var resInfo = CraftResources.GetInfo(_rawResource);
                CraftAttributeInfo attrInfo = null;

                if (resInfo != null)
                {
                    attrInfo = resInfo.AttributeInfo;
                }

                if (attrInfo != null)
                {
                    bonus += attrInfo.ArmorDurability;
                }
            }

            return bonus;
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

                if (item is BaseArmor armor)
                {
                    if (!armor.CheckRace(m))
                    {
                        m.AddToBackpack(armor);
                    }
                    else if (!armor.AllowMaleWearer && !m.Female && m.AccessLevel < AccessLevel.GameMaster)
                    {
                        if (armor.AllowFemaleWearer)
                        {
                            m.SendLocalizedMessage(1010388); // Only females can wear this.
                        }
                        else
                        {
                            m.SendMessage("You may not wear this.");
                        }

                        m.AddToBackpack(armor);
                    }
                    else if (!armor.AllowFemaleWearer && m.Female && m.AccessLevel < AccessLevel.GameMaster)
                    {
                        if (armor.AllowMaleWearer)
                        {
                            m.SendLocalizedMessage(1063343); // Only males can wear this.
                        }
                        else
                        {
                            m.SendMessage("You may not wear this.");
                        }

                        m.AddToBackpack(armor);
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

            var v = ArmorAttributes.LowerStatReq;

            var info = CraftResources.GetInfo(_rawResource);

            var attrInfo = info?.AttributeInfo;

            if (attrInfo != null)
            {
                v += attrInfo.ArmorLowerRequirements;
            }

            if (v > 100)
            {
                v = 100;
            }

            return v;
        }

        public override void OnAdded(IEntity parent)
        {
            if (parent is Mobile from)
            {
                if (Core.AOS)
                {
                    SkillBonuses.AddTo(from);
                }

                from.Delta(MobileDelta.Armor); // Tell them armor rating has changed
            }
        }

        public virtual double ScaleArmorByDurability(double armor)
        {
            var scale = 100;

            if (_maxHitPoints > 0 && _hitPoints < _maxHitPoints)
            {
                scale = 50 + 50 * _hitPoints / _maxHitPoints;
            }

            return armor * scale / 100;
        }

        protected void Invalidate()
        {
            (Parent as Mobile)?.Delta(MobileDelta.Armor); // Tell them armor rating has changed
        }

        private static bool GetSaveFlag(OldSaveFlag flags, OldSaveFlag toGet) => (flags & toGet) != 0;

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            var m = Parent as Mobile;

            if (Core.AOS && m != null)
            {
                SkillBonuses.AddTo(m);
            }

            if (_rawResource == CraftResource.None)
            {
                _rawResource = DefaultResource;
            }

            var strBonus = ComputeStatBonus(StatType.Str);
            var dexBonus = ComputeStatBonus(StatType.Dex);
            var intBonus = ComputeStatBonus(StatType.Int);

            if (m != null && (strBonus != 0 || dexBonus != 0 || intBonus != 0))
            {
                var modName = Serial.ToString();

                if (strBonus != 0)
                {
                    m.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", strBonus, TimeSpan.Zero));
                }

                if (dexBonus != 0)
                {
                    m.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", dexBonus, TimeSpan.Zero));
                }

                if (intBonus != 0)
                {
                    m.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", intBonus, TimeSpan.Zero));
                }
            }

            m?.CheckStatTimers();
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (!Ethic.CheckTrade(from, to, newOwner, this))
            {
                return false;
            }

            return base.AllowSecureTrade(from, to, newOwner, accepted);
        }

        public override bool CanEquip(Mobile from)
        {
            if (!Ethic.CheckEquip(from, this))
            {
                return false;
            }

            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                if (!CheckRace(from))
                {
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

                int strBonus = ComputeStatBonus(StatType.Str), strReq = ComputeStatReq(StatType.Str);
                int dexBonus = ComputeStatBonus(StatType.Dex), dexReq = ComputeStatReq(StatType.Dex);
                int intBonus = ComputeStatBonus(StatType.Int), intReq = ComputeStatReq(StatType.Int);

                if (from.Dex < dexReq || from.Dex + dexBonus < 1)
                {
                    from.SendLocalizedMessage(502077); // You do not have enough dexterity to equip this item.
                    return false;
                }

                if (from.Str < strReq || from.Str + strBonus < 1)
                {
                    from.SendLocalizedMessage(500213); // You are not strong enough to equip that.
                    return false;
                }

                if (from.Int < intReq || from.Int + intBonus < 1)
                {
                    from.SendMessage("You are not smart enough to equip that.");
                    return false;
                }
            }

            return base.CanEquip(from);
        }

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

        public override bool OnEquip(Mobile from)
        {
            from.CheckStatTimers();

            var strBonus = ComputeStatBonus(StatType.Str);
            var dexBonus = ComputeStatBonus(StatType.Dex);
            var intBonus = ComputeStatBonus(StatType.Int);

            if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
            {
                var modName = Serial.ToString();

                if (strBonus != 0)
                {
                    from.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", strBonus, TimeSpan.Zero));
                }

                if (dexBonus != 0)
                {
                    from.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", dexBonus, TimeSpan.Zero));
                }

                if (intBonus != 0)
                {
                    from.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", intBonus, TimeSpan.Zero));
                }
            }

            return base.OnEquip(from);
        }

        public override void OnRemoved(IEntity parent)
        {
            if (parent is Mobile m)
            {
                var modName = Serial.ToString();

                m.RemoveStatMod($"{modName}Str");
                m.RemoveStatMod($"{modName}Dex");
                m.RemoveStatMod($"{modName}Int");

                if (Core.AOS)
                {
                    SkillBonuses.Remove();
                }

                m.Delta(MobileDelta.Armor); // Tell them armor rating has changed
                m.CheckStatTimers();
            }

            base.OnRemoved(parent);
        }

        private string GetNameString() => Name ?? $"#{LabelNumber}";

        public override void AddNameProperty(ObjectPropertyList list)
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

            if (_quality == ArmorQuality.Exceptional)
            {
                if (oreType != 0)
                {
                    list.Add(1053100, "#{0}\t{1}", oreType, GetNameString()); // exceptional ~1_oretype~ ~2_armortype~
                }
                else
                {
                    list.Add(1050040, GetNameString()); // exceptional ~1_ITEMNAME~
                }
            }
            else
            {
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
        }

        public override bool AllowEquippedCast(Mobile from)
        {
            if (base.AllowEquippedCast(from))
            {
                return true;
            }

            return Attributes.SpellChanneling != 0;
        }

        public virtual int GetLuckBonus() => CraftResources.GetInfo(_rawResource)?.AttributeInfo?.ArmorLuck ?? 0;

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (_crafter != null)
            {
                list.Add(1050043, _crafter); // crafted by ~1_NAME~
            }

            if (m_FactionState != null)
            {
                list.Add(1041350); // faction item
            }

            if (RequiredRaces == Race.AllowElvesOnly)
            {
                list.Add(1075086); // Elves Only
            }

            if (RequiredRaces == Race.AllowGargoylesOnly)
            {
                list.Add(1111709); // Gargoyles Only
            }

            SkillBonuses.GetProperties(list);

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

            if ((prop = GetLowerStatReq()) != 0)
            {
                list.Add(1060435, prop.ToString()); // lower requirements ~1_val~%
            }

            if ((prop = GetLuckBonus() + Attributes.Luck) != 0)
            {
                list.Add(1060436, prop.ToString()); // luck ~1_val~
            }

            if (ArmorAttributes.MageArmor != 0)
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

            if (Attributes.NightSight != 0)
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

            if ((prop = ArmorAttributes.SelfRepair) != 0)
            {
                list.Add(1060450, prop.ToString()); // self repair ~1_val~
            }

            if (Attributes.SpellChanneling != 0)
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

            if ((prop = GetDurabilityBonus()) > 0)
            {
                list.Add(1060410, prop.ToString()); // durability ~1_val~%
            }

            if ((prop = ComputeStatReq(StatType.Str)) > 0)
            {
                list.Add(1061170, prop.ToString()); // strength requirement ~1_val~
            }

            if (_hitPoints >= 0 && _maxHitPoints > 0)
            {
                list.Add(1060639, "{0}\t{1}", _hitPoints, _maxHitPoints); // durability ~1_val~ / ~2_val~
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

            if (_quality == ArmorQuality.Exceptional)
            {
                attrs.Add(new EquipInfoAttribute(1018305 - (int)_quality));
            }

            if (_identified || from.AccessLevel >= AccessLevel.GameMaster)
            {
                if (_durability != ArmorDurabilityLevel.Regular)
                {
                    attrs.Add(new EquipInfoAttribute(1038000 + (int)_durability));
                }

                if (_protection > ArmorProtectionLevel.Regular && _protection <= ArmorProtectionLevel.Invulnerability)
                {
                    attrs.Add(new EquipInfoAttribute(1038005 + (int)_protection));
                }
            }
            else if (_durability != ArmorDurabilityLevel.Regular || _protection > ArmorProtectionLevel.Regular &&
                _protection <= ArmorProtectionLevel.Invulnerability)
            {
                attrs.Add(new EquipInfoAttribute(1038000)); // Unidentified
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

            from.NetState.SendDisplayEquipmentInfo(Serial, number, _crafter, false, attrs);
        }

        [Flags]
        private enum OldSaveFlag
        {
            None = 0x00000000,
            Attributes = 0x00000001,
            ArmorAttributes = 0x00000002,
            PhysicalBonus = 0x00000004,
            FireBonus = 0x00000008,
            ColdBonus = 0x00000010,
            PoisonBonus = 0x00000020,
            EnergyBonus = 0x00000040,
            Identified = 0x00000080,
            MaxHitPoints = 0x00000100,
            HitPoints = 0x00000200,
            Crafter = 0x00000400,
            Quality = 0x00000800,
            Durability = 0x00001000,
            Protection = 0x00002000,
            Resource = 0x00004000,
            BaseArmor = 0x00008000,
            StrBonus = 0x00010000,
            DexBonus = 0x00020000,
            IntBonus = 0x00040000,
            StrReq = 0x00080000,
            DexReq = 0x00100000,
            IntReq = 0x00200000,
            MedAllowance = 0x00400000,
            SkillBonuses = 0x00800000,
            PlayerConstructed = 0x01000000
        }
    }
}
