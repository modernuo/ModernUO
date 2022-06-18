using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CandleLong : BaseLight
{
    [Constructible]
    public CandleLong() : base(0x1433)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(30) : TimeSpan.Zero;

        Burning = false;
        Light = LightType.Circle150;
        Weight = 1.0;
    }

    public override int LitItemID => 0x1430;
    public override int UnlitItemID => 0x1433;
}
