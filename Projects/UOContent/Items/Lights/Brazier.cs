using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Brazier : BaseLight
{
    [Constructible]
    public Brazier() : base(0xE31)
    {
        Movable = false;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = true;
        Light = LightType.Circle225;
        Weight = 20.0;
    }

    public override int LitItemID => 0xE31;
}
