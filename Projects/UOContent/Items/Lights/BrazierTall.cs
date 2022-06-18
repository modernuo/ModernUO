using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BrazierTall : BaseLight
{
    [Constructible]
    public BrazierTall() : base(0x19AA)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = true;
        Light = LightType.Circle300;
        Weight = 25.0;
    }

    public override int LitItemID => 0x19AA;
}
