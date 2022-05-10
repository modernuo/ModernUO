using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ThornedWildStaff : WildStaff
    {
        [Constructible]
        public ThornedWildStaff() => Attributes.ReflectPhysical = 12;

        public override int LabelNumber => 1073551; // thorned wild staff
    }
}
