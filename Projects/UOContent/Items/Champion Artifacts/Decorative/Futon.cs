using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable]
    [SerializationGenerator(0, false)]
    public partial class Futon : Item
    {
        [Constructible]
        public Futon() : base(Utility.RandomDouble() > 0.5 ? 0x295C : 0x295E)
        {
        }

        public void Flip()
        {
            ItemID = ItemID switch
            {
                0x295C => 0x295D,
                0x295E => 0x295F,
                0x295D => 0x295C,
                0x295F => 0x295E,
                _      => ItemID
            };
        }
    }
}
