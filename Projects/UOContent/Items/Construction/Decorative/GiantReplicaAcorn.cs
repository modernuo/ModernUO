using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [SerializationGenerator(0)]
    public partial class GiantReplicaAcorn : Item
    {
        [Constructible]
        public GiantReplicaAcorn() : base(0x2D4A) => Weight = 1.0;

        public override int LabelNumber => 1072889; // giant replica acorn
    }
}
