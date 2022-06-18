using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class RoundPaperLantern : BaseLight
{
    [Constructible]
    public RoundPaperLantern() : base(0x24CA)
    {
        Movable = true;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle150;
        Weight = 3.0;
    }

    public override int LitItemID => 0x24C9;
    public override int UnlitItemID => 0x24CA;
}
