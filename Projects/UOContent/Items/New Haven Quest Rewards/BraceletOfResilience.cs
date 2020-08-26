namespace Server.Items
{
    public class BraceletOfResilience : GoldBracelet
    {
        [Constructible]
        public BraceletOfResilience()
        {
            LootType = LootType.Blessed;

            Attributes.DefendChance = 5;
            Resistances.Fire = 5;
            Resistances.Cold = 5;
            Resistances.Poison = 5;
            Resistances.Energy = 5;
        }

        public BraceletOfResilience(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1077627; // Bracelet of Resilience

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
