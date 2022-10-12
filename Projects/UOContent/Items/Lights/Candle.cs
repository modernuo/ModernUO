using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Candle : BaseEquipableLight
{
    [Constructible]
    public Candle() : base(0xA28)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(20) : TimeSpan.Zero;
        Burning = false;

        Stackable = true;
        Light = LightType.Circle150;
        Weight = 1.0;
    }

    public override int LitItemID => 0xA0F;
    public override int UnlitItemID => 0xA28;
}
