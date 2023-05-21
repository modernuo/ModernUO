using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PowerCrystal : Item
{
    [Constructible]
    public PowerCrystal() : base(0x1F1C) => Weight = 1.0;

    public override string DefaultName => "power crystal";

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else
        {
            from.SendAsciiMessage("This looks like part of a larger contraption.");
        }
    }
}
