using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Shell : Item
    {
        [Constructible]
        public Shell() : base(Utility.Random(0x3B12, 2))
        {
        }

        public override int LabelNumber => 1074598; // A shell
        public override double DefaultWeight => 1.0;

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
