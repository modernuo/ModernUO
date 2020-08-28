namespace Server.Mobiles
{
    public class TailorGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public TailorGuildmaster() : base("tailor")
        {
            SetSkill(SkillName.Tailoring, 90.0, 100.0);
        }

        public TailorGuildmaster(Serial serial) : base(serial)
        {
        }

        public override NpcGuild NpcGuild => NpcGuild.TailorsGuild;

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
