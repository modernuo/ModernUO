using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ButchersWarCleaver : WarCleaver
    {
        [Constructible]
        public ButchersWarCleaver()
        {
        }

        public override int LabelNumber => 1073526; // butcher's war cleaver

        public override void AppendChildNameProperties(IPropertyList list)
        {
            base.AppendChildNameProperties(list);

            list.Add(1072512); // Bovine Slayer
        }
    }
}
