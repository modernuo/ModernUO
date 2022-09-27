using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

public enum GemType
{
    None,
    StarSapphire,
    Emerald,
    Sapphire,
    Ruby,
    Citrine,
    Amethyst,
    Tourmaline,
    Amber,
    Diamond
}

[SerializationGenerator(4, false)]
public abstract partial class BaseJewel : Item, ICraftable
{
    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _maxHitPoints;

    [SerializableField(3)]
    [InvalidateProperties]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private GemType _gemType;

    [SerializableField(4, setter: "private")]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
    private AosAttributes _attributes;

    [SerializableField(5, setter: "private")]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
    private AosElementAttributes _resistances;

    [SerializableField(6, setter: "private")]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
    private AosSkillBonuses _skillBonuses;

    public BaseJewel(int itemID, Layer layer) : base(itemID)
    {
        _attributes = new AosAttributes(this);
        _resistances = new AosElementAttributes(this);
        _skillBonuses = new AosSkillBonuses(this);
        _resource = CraftResource.Iron;
        Hue = CraftResources.GetHue(_resource);
        _gemType = GemType.None;

        Layer = layer;

        _hitPoints = _maxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);
    }

    [EncodedInt]
    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int HitPoints
    {
        get => _hitPoints;
        set
        {
            if (value != _hitPoints && _maxHitPoints > 0)
            {
                _hitPoints = value;

                if (_hitPoints < 0)
                {
                    Delete();
                }
                else if (_hitPoints > _maxHitPoints)
                {
                    _hitPoints = _maxHitPoints;
                }

                InvalidateProperties();
                this.MarkDirty();
            }
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
        get => _resource;
        set
        {
            _resource = value;
            Hue = CraftResources.GetHue(_resource);
        }
    }

    public override int PhysicalResistance => Resistances.Physical;
    public override int FireResistance => Resistances.Fire;
    public override int ColdResistance => Resistances.Cold;
    public override int PoisonResistance => Resistances.Poison;
    public override int EnergyResistance => Resistances.Energy;
    public virtual int BaseGemTypeNumber => 0;

    public virtual int InitMinHits => 0;
    public virtual int InitMaxHits => 0;

    public override int LabelNumber
    {
        get
        {
            if (_gemType == GemType.None)
            {
                return base.LabelNumber;
            }

            return BaseGemTypeNumber + (int)_gemType - 1;
        }
    }

    public virtual int ArtifactRarity => 0;

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

        Resource = CraftResources.GetFromType(resourceType);

        var context = craftSystem.GetContext(from);

        if (context?.DoNotColor == true)
        {
            Hue = 0;
        }

        if (craftItem.Resources.Count > 1)
        {
            resourceType = craftItem.Resources[1].ItemType;

            if (resourceType == typeof(StarSapphire))
            {
                GemType = GemType.StarSapphire;
            }
            else if (resourceType == typeof(Emerald))
            {
                GemType = GemType.Emerald;
            }
            else if (resourceType == typeof(Sapphire))
            {
                GemType = GemType.Sapphire;
            }
            else if (resourceType == typeof(Ruby))
            {
                GemType = GemType.Ruby;
            }
            else if (resourceType == typeof(Citrine))
            {
                GemType = GemType.Citrine;
            }
            else if (resourceType == typeof(Amethyst))
            {
                GemType = GemType.Amethyst;
            }
            else if (resourceType == typeof(Tourmaline))
            {
                GemType = GemType.Tourmaline;
            }
            else if (resourceType == typeof(Amber))
            {
                GemType = GemType.Amber;
            }
            else if (resourceType == typeof(Diamond))
            {
                GemType = GemType.Diamond;
            }
        }

        return 1;
    }

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is not BaseJewel jewel)
        {
            return;
        }

        jewel.Attributes = new AosAttributes(newItem, Attributes);
        jewel.Resistances = new AosElementAttributes(newItem, Resistances);
        jewel.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
    }

    public override void OnAdded(IEntity parent)
    {
        if (Core.AOS && parent is Mobile from)
        {
            SkillBonuses.AddTo(from);

            var strBonus = Attributes.BonusStr;
            var dexBonus = Attributes.BonusDex;
            var intBonus = Attributes.BonusInt;

            if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
            {
                var serial = Serial;

                if (strBonus != 0)
                {
                    from.AddStatMod(new StatMod(StatType.Str, $"{serial}Str", strBonus, TimeSpan.Zero));
                }

                if (dexBonus != 0)
                {
                    from.AddStatMod(new StatMod(StatType.Dex, $"{serial}Dex", dexBonus, TimeSpan.Zero));
                }

                if (intBonus != 0)
                {
                    from.AddStatMod(new StatMod(StatType.Int, $"{serial}Int", intBonus, TimeSpan.Zero));
                }
            }

            from.CheckStatTimers();
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        if (Core.AOS && parent is Mobile from)
        {
            SkillBonuses.Remove();

            var serial = Serial;

            from.RemoveStatMod($"{serial}Str");
            from.RemoveStatMod($"{serial}Dex");
            from.RemoveStatMod($"{serial}Int");

            from.CheckStatTimers();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        SkillBonuses.GetProperties(list);

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

        if ((prop = Attributes.Luck) != 0)
        {
            list.Add(1060436, prop); // luck ~1_val~
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

        if (_hitPoints >= 0 && _maxHitPoints > 0)
        {
            list.Add(1060639, $"{_hitPoints}\t{_maxHitPoints}"); // durability ~1_val~ / ~2_val~
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _maxHitPoints = reader.ReadEncodedInt();
        _hitPoints = reader.ReadEncodedInt();
        _resource = (CraftResource)reader.ReadEncodedInt();
        _gemType = (GemType)reader.ReadEncodedInt();
        _attributes = new AosAttributes(this);
        _attributes.Deserialize(reader);
        _resistances = new AosElementAttributes(this);
        _resistances.Deserialize(reader);
        _skillBonuses = new AosSkillBonuses(this);
        _skillBonuses.Deserialize(reader);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var m = Parent as Mobile;

        if (Core.AOS && m != null)
        {
            SkillBonuses.AddTo(m);
        }

        var strBonus = Attributes.BonusStr;
        var dexBonus = Attributes.BonusDex;
        var intBonus = Attributes.BonusInt;

        if (m != null && (strBonus != 0 || dexBonus != 0 || intBonus != 0))
        {
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

        m?.CheckStatTimers();
    }
}
