using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
[Flippable(0xF95, 0xF96, 0xF97, 0xF98, 0xF99, 0xF9A, 0xF9B, 0xF9C)]
public partial class BoltOfCloth : Item, IScissorable, IDyable, ICommodity
{
    [Constructible]
    public BoltOfCloth(int amount = 1) : base(0xF95)
    {
        Stackable = true;
        Weight = 5.0;
        Amount = amount;
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
        if (Deleted || !from.CanSee(this))
        {
            return false;
        }

        ScissorHelper(from, new Cloth(), 50);

        return true;
    }

    public override void OnSingleClick(Mobile from)
    {
        var number = Amount == 1 ? 1049122 : 1049121;
        var amount = (Amount * 50).ToString();
        from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, 0x3B2, 3, number, "", amount);
    }
}