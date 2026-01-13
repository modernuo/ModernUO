using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PileOfGlacialSnow : Item
{
    [Constructible]
    public PileOfGlacialSnow() : base(0x913)
    {
        Hue = 0x480;
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1070874; // a Pile of Glacial Snow

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);
        LabelTo(from, 1070880); // Winter 2004
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1070880); // Winter 2004
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
        }
        else if (from.Mounted)
        {
            from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
        }
        else if (from.CanBeginAction<SnowPile>())
        {
            from.SendLocalizedMessage(1005575); // You carefully pack the snow into a ball...
            from.Target = new SnowTarget();
        }
        else
        {
            from.SendLocalizedMessage(1005574); // The snow is not ready to be packed yet.  Keep trying.
        }
    }
}
