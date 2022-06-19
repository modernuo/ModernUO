using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LampPost1 : BaseLight
{
    [Constructible]
    public LampPost1() : base(0xB21)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle300;
        Weight = 40.0;
    }

    public override int LitItemID => 0xB20;
    public override int UnlitItemID => 0xB21;
}
