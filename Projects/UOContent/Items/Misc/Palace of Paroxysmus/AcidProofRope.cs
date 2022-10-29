using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AcidProofRope : Item
{
    [Constructible]
    public AcidProofRope() : base(0x20D) => Hue = 0x3D1;

    public override int LabelNumber => 1074886; // Acid Proof Rope
}
