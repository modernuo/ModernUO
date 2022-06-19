using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class PaperLantern : BaseLight
{
    [Constructible]
    public PaperLantern() : base(0x24BE)
    {
        Movable = true;
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle150;
        Weight = 3.0;
    }

    public override int LitItemID => 0x24BD;
    public override int UnlitItemID => 0x24BE;
}
