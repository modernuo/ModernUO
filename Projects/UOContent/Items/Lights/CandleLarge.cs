using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CandleLarge : BaseLight
{
    [Constructible]
    public CandleLarge() : base(0xA26)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(25) : TimeSpan.Zero;

        Burning = false;
        Light = LightType.Circle150;
        Weight = 2.0;
    }

    public override int LitItemID => 0xB1A;
    public override int UnlitItemID => 0xA26;
}
