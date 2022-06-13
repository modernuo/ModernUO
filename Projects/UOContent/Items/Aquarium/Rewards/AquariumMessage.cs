using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AquariumMessage : MessageInABottle
    {
        [Constructible]
        public AquariumMessage()
        {
        }

        public override int LabelNumber => 1073894; // Message in a Bottle

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
