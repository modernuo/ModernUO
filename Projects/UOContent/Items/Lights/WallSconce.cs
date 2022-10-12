using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class WallSconce : BaseLight
{
    [Constructible]
    public WallSconce() : base(0x9FB)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.WestBig;
        Weight = 3.0;
    }

    public override int LitItemID => ItemID == 0x9FB ? 0x9FD : 0xA02;

    public override int UnlitItemID => ItemID == 0x9FD ? 0x9FB : 0xA00;

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
            0x9FB => 0xA00,
            0x9FD => 0xA02,
            0xA00 => 0x9FB,
            0xA02 => 0x9FD,
            _     => ItemID
        };
    }
}
