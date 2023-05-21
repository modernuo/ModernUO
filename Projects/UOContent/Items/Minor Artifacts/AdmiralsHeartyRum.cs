using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AdmiralsHeartyRum : BeverageBottle
{
    [Constructible]
    public AdmiralsHeartyRum() : base(BeverageType.Ale) => Hue = 0x66C;

    public override int LabelNumber => 1063477;
}
