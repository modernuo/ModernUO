using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x14F3, 0x14F4)]
    public partial class ToyBoat : Item
    {
        [Constructible]
        public ToyBoat() : base(0x14F4)
        {
        }

        public override int LabelNumber => 1074363; // A toy boat
        public override double DefaultWeight => 1.0;

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
