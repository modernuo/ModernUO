using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class WallTorch : BaseLight
{
    [Constructible]
    public WallTorch() : base(0xA05)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.WestBig;
        Weight = 3.0;
    }

    public override int LitItemID => ItemID == 0xA05 ? 0xA07 : 0xA0C;

    public override int UnlitItemID => ItemID == 0xA07 ? 0xA05 : 0xA0A;

    public void Flip()
    {
        Light = Light switch
        {
            LightType.WestBig  => LightType.NorthBig,
            LightType.NorthBig => LightType.WestBig,
            _                  => Light
        };

        ItemID = ItemID switch
        {
            0xA05 => 0xA0A,
            0xA07 => 0xA0C,
            0xA0A => 0xA05,
            0xA0C => 0xA07,
            _     => ItemID
        };
    }
}
