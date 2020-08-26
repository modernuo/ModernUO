namespace Server.Items
{
    public class TradeGlasses : ElvenGlasses
    {
        [Constructible]
        public TradeGlasses()
        {
            Attributes.BonusStr = 10;
            Attributes.BonusInt = 10;
        }

        public TradeGlasses(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073362; // Reading Glasses of the Trades

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

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
