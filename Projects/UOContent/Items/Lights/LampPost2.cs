using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LampPost2 : BaseLight
{
    [Constructible]
    public LampPost2() : base(0xB23)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle300;
        Weight = 40.0;
    }

    public override int LitItemID => 0xB22;
    public override int UnlitItemID => 0xB23;
}
