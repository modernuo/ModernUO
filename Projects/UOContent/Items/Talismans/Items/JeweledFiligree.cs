using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class JeweledFiligree : Item
{
    [Constructible]
    public JeweledFiligree() : base(0x2F5E) => Weight = 1.0;

    public override int LabelNumber => 1072894; // jeweled filigree
}
