using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class WhiteHangingLantern : BaseLight
{
    [Constructible]
    public WhiteHangingLantern() : base(0x24C6)
    {
        Movable = true;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle300;
        Weight = 3.0;
    }

    public override int LitItemID => ItemID == 0x24C6 ? 0x24C5 : 0x24C7;

    public override int UnlitItemID => ItemID == 0x24C5 ? 0x24C6 : 0x24C8;

    public void Flip()
    {
        Light = LightType.Circle300;

        ItemID = ItemID switch
        {
            0x24C6 => 0x24C8,
            0x24C5 => 0x24C7,
            0x24C8 => 0x24C6,
            0x24C7 => 0x24C5,
            _      => ItemID
        };
    }
}
