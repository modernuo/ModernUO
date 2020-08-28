namespace Server.Items
{
    public class AlchemistsBauble : GoldBracelet
    {
        [Constructible]
        public AlchemistsBauble()
        {
            Hue = 0x290;
            SkillBonuses.SetValues(0, SkillName.Magery, 10.0);
            Attributes.EnhancePotions = 30;
            Attributes.LowerRegCost = 20;
            Resistances.Poison = 10;
        }

        public AlchemistsBauble(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1070638;

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
