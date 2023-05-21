using ModernUO.Serialization;

namespace Server.Items;

public interface IShipwreckedItem
{
    bool IsShipwreckedItem { get; set; }
}

[SerializationGenerator(0, false)]
public partial class ShipwreckedItem : Item, IDyable, IShipwreckedItem
{
    public ShipwreckedItem(int itemID) : base(itemID)
    {
        var weight = ItemData.Weight;

        if (weight >= 255)
        {
            weight = 1;
        }

        Weight = weight;
    }

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        if (ItemID >= 0x13A4 && ItemID <= 0x13AE)
        {
            Hue = sender.DyedHue;
            return true;
        }

        from.SendLocalizedMessage(sender.FailMessage);
        return false;
    }

    bool IShipwreckedItem.IsShipwreckedItem
    {
        get => true;
        set { }
    }

    public override void OnSingleClick(Mobile from)
    {
        LabelTo(from, 1050039, $"{LabelNumber:#}\t{1041645:#}");
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        list.Add(1041645); // recovered from a shipwreck
    }
}
