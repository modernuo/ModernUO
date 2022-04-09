using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AlbinoCourtesanFish : BaseFish
    {
        [Constructible]
        public AlbinoCourtesanFish() : base(0x3B04)
        {
        }

        public override int LabelNumber => 1074592; // Albino Courtesan Fish
    }
}
