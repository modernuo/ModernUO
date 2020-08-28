namespace Server.Items
{
    public class LieutenantOfTheBritannianRoyalGuard : BodySash
    {
        [Constructible]
        public LieutenantOfTheBritannianRoyalGuard()
        {
            Hue = 0xe8;

            Attributes.BonusInt = 5;
            Attributes.RegenMana = 2;
            Attributes.LowerRegCost = 10;
        }

        public LieutenantOfTheBritannianRoyalGuard(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094910; // Lieutenant of the Britannian Royal Guard [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
