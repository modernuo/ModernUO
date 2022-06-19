using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Torch : BaseEquipableLight
{
    [Constructible]
    public Torch() : base(0xF6B)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(30) : TimeSpan.Zero;
        Burning = false;

        Stackable = true;
        Light = LightType.Circle300;
        Weight = 1.0;
    }

    public override int LitItemID => 0xA12;
    public override int UnlitItemID => 0xF6B;

    public override int LitSound => 0x54;
    public override int UnlitSound => 0x4BB;

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (parent is Mobile mobile && Burning)
        {
            MeerMage.StopEffect(mobile, true);
        }
    }

    public override void Ignite()
    {
        base.Ignite();

        if (Parent is Mobile mobile && Burning)
        {
            MeerMage.StopEffect(mobile, true);
        }
    }
}
