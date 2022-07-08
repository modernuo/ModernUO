using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class NougatSwirl : CandyCane
    {
        [Constructible]
        public NougatSwirl(int amount = 1) : base(0x4690) => Stackable = true;

        public override int LabelNumber => 1096936; /* nougat swirl */
    }
}
