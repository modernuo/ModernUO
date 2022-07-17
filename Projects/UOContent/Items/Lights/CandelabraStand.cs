using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CandelabraStand : BaseLight
{
    [Constructible]
    public CandelabraStand() : base(0xA29)
    {
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle225;
        Weight = 20.0;
    }

    public override int LitItemID => 0xB26;
    public override int UnlitItemID => 0xA29;
}
