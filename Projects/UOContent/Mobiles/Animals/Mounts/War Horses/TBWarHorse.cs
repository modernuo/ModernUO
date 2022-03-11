namespace Server.Mobiles
{
    public class TBWarHorse : BaseWarHorse
    {
        [Constructible]
        public TBWarHorse() : base(0x76, 0x3EB2, AIType.AI_Melee, FightMode.Aggressor, 10, 1)
        {
        }

        public TBWarHorse(Serial serial) : base(serial)
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
