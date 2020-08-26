namespace Server.Mobiles
{
    public class FisherGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public FisherGuildmaster() : base("fisher")
        {
            SetSkill(SkillName.Fishing, 80.0, 100.0);
        }

        public FisherGuildmaster(Serial serial) : base(serial)
        {
        }

        public override NpcGuild NpcGuild => NpcGuild.FishermensGuild;

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
