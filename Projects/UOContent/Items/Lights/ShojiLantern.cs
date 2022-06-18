using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class ShojiLantern : BaseLight
{
    [Constructible]
    public ShojiLantern() : base(0x24BC)
    {
        Movable = true;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle150;
        Weight = 3.0;
    }

    public override int LitItemID => 0x24BB;
    public override int UnlitItemID => 0x24BC;
}
