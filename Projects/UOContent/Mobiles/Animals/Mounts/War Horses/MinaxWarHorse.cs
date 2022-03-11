namespace Server.Mobiles
{
    public class MinaxWarHorse : BaseWarHorse
    {
        [Constructible]
        public MinaxWarHorse() : base(0x78, 0x3EAF, AIType.AI_Melee, FightMode.Aggressor, 10, 1)
        {
        }

        public MinaxWarHorse(Serial serial) : base(serial)
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
