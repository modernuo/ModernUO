using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LieutenantOfTheBritannianRoyalGuard : BodySash
    {
        [Constructible]
        public LieutenantOfTheBritannianRoyalGuard()
        {
            Hue = 0xe8;

            Attributes.BonusInt = 5;
            Attributes.RegenMana = 2;
            Attributes.LowerRegCost = 10;
        }

        public override int LabelNumber => 1094910; // Lieutenant of the Britannian Royal Guard [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
