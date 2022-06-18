using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LampPost3 : BaseLight
{
    [Constructible]
    public LampPost3() : base(0xb25)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle300;
        Weight = 40.0;
    }

    public override int LitItemID => 0xB24;
    public override int UnlitItemID => 0xB25;
}
