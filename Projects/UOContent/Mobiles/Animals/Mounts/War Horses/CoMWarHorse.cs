namespace Server.Mobiles
{
    public class CoMWarHorse : BaseWarHorse
    {
        [Constructible]
        public CoMWarHorse() : base(0x77, 0x3EB1, AIType.AI_Melee, FightMode.Aggressor, 10, 1)
        {
        }

        public CoMWarHorse(Serial serial) : base(serial)
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
