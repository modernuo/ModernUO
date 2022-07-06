using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HangingLantern : BaseLight
{
    [Constructible]
    public HangingLantern() : base(0xA1D)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle300;
        Weight = 40.0;
    }

    public override int LitItemID => 0xA1A;
    public override int UnlitItemID => 0xA1D;
}
