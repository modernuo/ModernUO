using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CandleShort : BaseLight
{
    [Constructible]
    public CandleShort() : base(0x142F)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(25) : TimeSpan.Zero;

        Burning = false;
        Light = LightType.Circle150;
        Weight = 1.0;
    }

    public override int LitItemID => 0x142C;
    public override int UnlitItemID => 0x142F;
}
