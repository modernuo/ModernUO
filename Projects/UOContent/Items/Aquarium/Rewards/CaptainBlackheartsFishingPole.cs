using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class CaptainBlackheartsFishingPole : FishingPole
    {
        [Constructible]
        public CaptainBlackheartsFishingPole()
        {
        }

        public override int LabelNumber => 1074571; // Captain Blackheart's Fishing Pole

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }
    }
}
