using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ResGate : Item
{
    [Constructible]
    public ResGate() : base(0xF6C)
    {
        Movable = false;
        Hue = 0x2D1;
        Light = LightType.Circle300;
    }

    public override string DefaultName => "a resurrection gate";

    public override bool OnMoveOver(Mobile m)
    {
        if (!m.Alive && m.Map?.CanFit(m.Location, 16, false, false) == true)
        {
            m.PlaySound(0x214);
            m.FixedEffect(0x376A, 10, 16);

            m.CloseGump<ResurrectGump>();
            m.SendGump(new ResurrectGump(m));
        }
        else
        {
            m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
        }

        return false;
    }
}
