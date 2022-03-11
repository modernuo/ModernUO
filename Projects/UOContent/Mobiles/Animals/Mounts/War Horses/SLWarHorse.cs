namespace Server.Mobiles
{
    public class SLWarHorse : BaseWarHorse
    {
        [Constructible]
        public SLWarHorse() : base(0x79, 0x3EB0, AIType.AI_Melee, FightMode.Aggressor, 10, 1)
        {
        }

        public SLWarHorse(Serial serial) : base(serial)
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
