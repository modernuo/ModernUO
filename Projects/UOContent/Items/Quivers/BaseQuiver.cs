using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(1, false)]
public partial class BaseQuiver : Container, ICraftable, IAosItem
{
    private static Type[] m_Ammo = { typeof(Arrow), typeof(Bolt) };

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster, canModify: true)]
    private AosAttributes _attributes;

    [SerializableFieldSaveFlag(0)]
    private bool ShouldSerializeAosAttributes() => !_attributes.IsEmpty;

    [SerializableFieldDefault(0)]
    private AosAttributes AttributesDefaultValue() => new(this);

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _lowerAmmoCost;

    [SerializableFieldSaveFlag(1)]
    private bool ShouldSerializeLowerAmmoCost() => _lowerAmmoCost != 0;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _weightReduction;

    [SerializableFieldSaveFlag(2)]
    private bool ShouldSerializeWeightReduction() => _weightReduction != 0;

    [InvalidateProperties]
    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _damageIncrease;

    [SerializableFieldSaveFlag(3)]
    private bool ShouldSerializeDamageIncrease() => _damageIncrease != 0;

    [InvalidateProperties]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _crafter;

    [SerializableFieldSaveFlag(4)]
    private bool ShouldSerializeCrafter() => _crafter != null;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ClothingQuality _quality;

    [SerializableFieldSaveFlag(5)]
    private bool ShouldSerializeQuality() => _quality != ClothingQuality.Regular;

    [SerializableFieldDefault(5)]
    private ClothingQuality QualityDefaultValue() => ClothingQuality.Regular;

    [InvalidateProperties]
    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _capacity;

    [SerializableFieldSaveFlag(6)]
    private bool ShouldSerializeCapacity() => _capacity != 0;

    public BaseQuiver(int itemID = 0x2FB7) : base(itemID)
    {
        Weight = 2.0;
        Capacity = 500;
        Layer = Layer.Cloak;

        Attributes = new AosAttributes(this);

        DamageIncrease = 10;
    }

    public override int DefaultGumpID => 0x108;
    public override int DefaultMaxItems => 1;
    public override int DefaultMaxWeight => 50;
    public override double DefaultWeight => 2.0;

    public Item Ammo => Items.Count > 0 ? Items[0] : null;

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

        return quality;
    }

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is not BaseQuiver quiver)
        {
            return;
        }

        quiver.Attributes = new AosAttributes(newItem, Attributes);
    }

    public override void UpdateTotal(Item sender, TotalType type, int delta)
    {
        InvalidateProperties();

        base.UpdateTotal(sender, type, delta);
    }

    public override int GetTotal(TotalType type)
    {
        var total = base.GetTotal(type);

        if (type == TotalType.Weight)
        {
            total -= total * _weightReduction / 100;
        }

        return total;
    }

    public bool CheckType(Item item)
    {
        var type = item.GetType();
        var ammo = Ammo;

        if (ammo != null)
        {
            if (ammo.GetType() == type)
            {
                return true;
            }
        }
        else
        {
            for (var i = 0; i < m_Ammo.Length; i++)
            {
                if (type == m_Ammo[i])
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (!CheckType(item))
        {
            if (message)
            {
                m.SendLocalizedMessage( 1074836 ); // The container can not hold that type of object.
            }

            return false;
        }

        if (Items.Count < DefaultMaxItems)
        {
            return item.Amount <= _capacity && base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        if (checkItems)
        {
            return false;
        }

        Item ammo = Ammo;

        return ammo?.Deleted == false && ammo.Amount + item.Amount <= _capacity;
    }

    public override void AddItem(Item dropped)
    {
        base.AddItem(dropped);

        InvalidateWeight();
    }

    public override void RemoveItem(Item dropped)
    {
        base.RemoveItem(dropped);

        InvalidateWeight();
    }

    public override void OnAdded(IEntity parent)
    {
        if (parent is Mobile mob)
        {
            Attributes.AddStatBonuses(mob);
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        if (parent is Mobile mob)
        {
            Attributes.RemoveStatBonuses(mob);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_crafter != null)
        {
            list.Add(1050043, _crafter); // crafted by ~1_NAME~
        }

        if (_quality == ClothingQuality.Exceptional)
        {
            list.Add(1063341); // exceptional
        }

        var ammo = Ammo;

        if (ammo == null)
        {
            list.Add(1075265, $"0\t{Capacity}"); // Ammo: ~1_QUANTITY~/~2_CAPACITY~ arrows
        }
        else if (ammo is Arrow)
        {
            list.Add(1075265, $"{ammo.Amount}\t{Capacity}"); // Ammo: ~1_QUANTITY~/~2_CAPACITY~ arrows
        }
        else if (ammo is Bolt)
        {
            list.Add(1075266, $"{ammo.Amount}\t{Capacity}"); // Ammo: ~1_QUANTITY~/~2_CAPACITY~ bolts
        }

        int prop;

        if ((prop = _damageIncrease) != 0)
        {
            list.Add(1074762, prop); // Damage modifier: ~1_PERCENT~%
        }

        int phys = 0, fire = 0, cold = 0, pois = 0, nrgy = 0, chaos = 0, direct = 0;

        AlterBowDamage(ref phys, ref fire, ref cold, ref pois, ref nrgy, ref chaos, ref direct);

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

        if (chaos != 0)
        {
            list.Add(1072846, chaos); // chaos damage ~1_val~%
        }

        if (direct != 0)
        {
            list.Add(1079978, direct); // Direct Damage: ~1_PERCENT~%
        }

        list.Add(1075085); // Requirement: Mondain's Legacy

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

        if ((prop = _lowerAmmoCost) > 0)
        {
            list.Add(1075208, prop); // Lower Ammo Cost ~1_Percentage~%
        }

        var weight = ammo != null ? ammo.Weight + ammo.Amount : 0;

        list.Add(
            1072241, // Contents: ~1_COUNT~/~2_MAXCOUNT items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones
            $"{Items.Count}\t{DefaultMaxItems}\t{(int)weight}\t{DefaultMaxWeight}"
        );

        if ((prop = _weightReduction) != 0)
        {
            list.Add(1072210, prop); // Weight reduction: ~1_PERCENTAGE~%
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var flags = reader.ReadEncodedInt();

        _attributes = new AosAttributes(this);

        if ((flags & 0x1) != 0)
        {
            _attributes.Deserialize(reader);
        }

        if ((flags & 0x4) != 0)
        {
            _lowerAmmoCost = reader.ReadInt();
        }

        if ((flags & 0x8) != 0)
        {
            _weightReduction = reader.ReadInt();
        }

        if ((flags & 0x80) != 0)
        {
            _damageIncrease = reader.ReadInt();
        }

        if ((flags & 0x10) != 0)
        {
            Timer.DelayCall((item, crafter) => item._crafter = crafter?.RawName, this, reader.ReadEntity<Mobile>());
        }

        if ((flags & 0x20) != 0)
        {
            _quality = (ClothingQuality)reader.ReadInt();
        }

        if ((flags & 0x40) != 0)
        {
            _capacity = reader.ReadInt();
        }
    }

    public virtual void AlterBowDamage(
        ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy, ref int chaos, ref int direct
    )
    {
    }

    public void InvalidateWeight()
    {
        if (RootParent is Mobile m)
        {
            m.UpdateTotals();
        }
    }
}
