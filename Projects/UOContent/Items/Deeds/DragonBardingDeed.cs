using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[TypeAlias("Server.Items.DragonBarding")]
[SerializationGenerator(2, false)]
public partial class DragonBardingDeed : Item, ICraftable
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _craftedBy;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _exceptional;

    public DragonBardingDeed() : base(0x14F0) => Weight = 1.0;

    public override int LabelNumber => _exceptional ? 1053181 : 1053012; // dragon barding deed

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
        get => _resource;
        set
        {
            _resource = value;
            Hue = CraftResources.GetHue(value);
            InvalidateProperties();
        }
    }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        Exceptional = quality >= 2;

        if (makersMark)
        {
            CraftedBy = from?.RawName;
        }

        var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

        Resource = CraftResources.GetFromType(resourceType);

        var context = craftSystem.GetContext(from);

        if (context?.DoNotColor == true)
        {
            Hue = 0;
        }

        return quality;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_exceptional && _craftedBy != null)
        {
            list.Add(1050043, _craftedBy); // crafted by ~1_NAME~
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.BeginTarget(6, false, TargetFlags.None, OnTarget);
            from.SendLocalizedMessage(1053024); // Select the swamp dragon you wish to place the barding on.
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public virtual void OnTarget(Mobile from, object obj)
    {
        if (Deleted)
        {
            return;
        }

        if (obj is not SwampDragon pet || pet.HasBarding)
        {
            from.SendLocalizedMessage(1053025); // That is not an unarmored swamp dragon.
        }
        else if (!pet.Controlled || pet.ControlMaster != from)
        {
            from.SendLocalizedMessage(1053026); // You can only put barding on a tamed swamp dragon that you own.
        }
        else if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
        }
        else
        {
            pet.BardingExceptional = Exceptional;
            pet.BardingCraftedBy = _craftedBy;
            pet.BardingHP = pet.BardingMaxHP;
            pet.BardingResource = Resource;
            pet.HasBarding = true;
            pet.Hue = Hue;

            Delete();

            // You place the barding on your swamp dragon.  Use a bladed item on your dragon to remove the armor.
            from.SendLocalizedMessage(1053027);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _exceptional = reader.ReadBool();
        var crafter = reader.ReadEntity<Mobile>();
        Timer.StartTimer(() => _craftedBy = crafter?.RawName);

        if (version < 1)
        {
            reader.ReadInt();
        }

        _resource = (CraftResource)reader.ReadInt();
    }
}
