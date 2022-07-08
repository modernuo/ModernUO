using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CandleSkull : BaseLight
{
    [Constructible]
    public CandleSkull() : base(0x1853)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(25) : TimeSpan.Zero;

        Burning = false;
        Light = LightType.Circle150;
        Weight = 5.0;
    }

    public override int LitItemID => ItemID is 0x1583 or 0x1854 ? 0x1854 : 0x1858;

    public override int UnlitItemID => ItemID is 0x1853 or 0x1584 ? 0x1853 : 0x1857;
}
