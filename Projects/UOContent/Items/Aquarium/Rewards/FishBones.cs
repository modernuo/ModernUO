using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FishBones : Item
    {
        [Constructible]
        public FishBones() : base(0x3B0C)
        {
        }

        public override int LabelNumber => 1074601; // Fish bones
        public override double DefaultWeight => 1.0;

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
