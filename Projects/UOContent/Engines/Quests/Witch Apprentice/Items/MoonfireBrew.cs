using ModernUO.Serialization;

namespace Server.Engines.Quests.Hag;

[SerializationGenerator(0, false)]
public partial class MoonfireBrew : Item
{
    [Constructible]
    public MoonfireBrew() : base(0xF04)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1055065; // a bottle of magical moonfire brew
}
