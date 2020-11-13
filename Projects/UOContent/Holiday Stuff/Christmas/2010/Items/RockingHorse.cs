namespace Server.Items.Holiday
{
    [TypeAlias("Server.Items.RockingHorse"), Flippable(0x4214, 0x4215)]
    public class RockingHorse : Item
    {
        public RockingHorse() : base(0x4214)
        {
            LootType = LootType.Blessed;

            Weight = 30;
        }

        public RockingHorse(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
