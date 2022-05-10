using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SmallMouthSuckerFin : BaseFish
    {
        [Constructible]
        public SmallMouthSuckerFin() : base(0x3B01)
        {
        }

        public override int LabelNumber => 1074590; // Small Mouth Sucker Fin
    }
}
