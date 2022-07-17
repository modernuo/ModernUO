using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class RedHangingLantern : BaseLight
{
    [Constructible]
    public RedHangingLantern() : base(0x24C2)
    {
        Movable = true;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle300;
        Weight = 3.0;
    }

    public override int LitItemID => ItemID == 0x24C2 ? 0x24C1 : 0x24C3;

    public override int UnlitItemID => ItemID == 0x24C1 ? 0x24C2 : 0x24C4;

    public void Flip()
    {
        Light = LightType.Circle300;

        ItemID = ItemID switch
        {
            0x24C2 => 0x24C4,
            0x24C1 => 0x24C3,
            0x24C4 => 0x24C2,
            0x24C3 => 0x24C1,
            _      => ItemID
        };
    }
}
