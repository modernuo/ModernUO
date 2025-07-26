using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HeritageToken : Item
{
    [Constructible]
    public HeritageToken() : base(0x367A) => LootType = LootType.Blessed;

    public override double DefaultWeight => 5.0;
    public override int LabelNumber => 1076596; // A Heritage Token

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(new HeritageTokenGump(this), true);
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.AddLocalized(1070998, 1076595); // Use this to redeem<br>Your Heritage Items
    }
}
