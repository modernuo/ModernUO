using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HeritageToken : Item
{
    [Constructible]
    public HeritageToken() : base(0x367A)
    {
        LootType = LootType.Blessed;
        Weight = 5.0;
    }

    public override int LabelNumber => 1076596; // A Heritage Token

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<HeritageTokenGump>();
            from.SendGump(new HeritageTokenGump(this));
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
