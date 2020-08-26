namespace Server.Items
{
    public class ArmorOfFortune : StuddedChest
    {
        [Constructible]
        public ArmorOfFortune()
        {
            Hue = 0x501;
            Attributes.Luck = 200;
            Attributes.DefendChance = 15;
            Attributes.LowerRegCost = 40;
            ArmorAttributes.MageArmor = 1;
        }

        public ArmorOfFortune(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061098; // Armor of Fortune
        public override int ArtifactRarity => 11;

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
