using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HeatingStand : BaseLight
{
    [Constructible]
    public HeatingStand() : base(0x1849)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(25) : TimeSpan.Zero;

        Burning = false;
        Light = LightType.Empty;
        Weight = 1.0;
    }

    public override int LitItemID => 0x184A;
    public override int UnlitItemID => 0x1849;

    public override void Ignite()
    {
        base.Ignite();

        if (ItemID == LitItemID)
        {
            Light = LightType.Circle150;
        }
        else if (ItemID == UnlitItemID)
        {
            Light = LightType.Empty;
        }
    }

    public override void Douse()
    {
        base.Douse();

        if (ItemID == LitItemID)
        {
            Light = LightType.Circle150;
        }
        else if (ItemID == UnlitItemID)
        {
            Light = LightType.Empty;
        }
    }
}
