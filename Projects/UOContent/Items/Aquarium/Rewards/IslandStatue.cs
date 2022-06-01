using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class IslandStatue : Item
    {
        [Constructible]
        public IslandStatue() : base(0x3B0F)
        {
        }

        public override int LabelNumber => 1074600; // An island statue
        public override double DefaultWeight => 1.0;

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
