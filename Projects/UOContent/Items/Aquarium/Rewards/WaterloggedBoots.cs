using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WaterloggedBoots : BaseShoes
    {
        [Constructible]
        public WaterloggedBoots() : base(0x1711)
        {
            ItemID = Utility.RandomBool()
                // thigh boots
                ? 0x1711
                // boots
                : 0x170B;
        }

        public override double DefaultWeight => ItemID == 0x1711 ? 4.0 : 3.0;

        public override int LabelNumber => 1074364; // Waterlogged boots

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
