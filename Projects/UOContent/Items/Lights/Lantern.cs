using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Lantern : BaseEquipableLight
{
    public static TimeSpan FullDuration = TimeSpan.FromMinutes(20);

    [Constructible]
    public Lantern() : base(0xA25)
    {
        Duration = Burnout ? FullDuration : TimeSpan.Zero;

        Burning = false;
        Light = LightType.Circle300;
        Weight = 2.0;
    }

    public override int LitItemID => ItemID is 0xA15 or 0xA17 ? ItemID : 0xA22;

    public override int UnlitItemID => ItemID == 0xA18 ? ItemID : 0xA25;
}

[SerializationGenerator(0)]
public partial class LanternOfSouls : Lantern
{
    [Constructible]
    public LanternOfSouls() => Hue = 0x482;

    public override int LabelNumber => 1061618; // Lantern of Souls
}
