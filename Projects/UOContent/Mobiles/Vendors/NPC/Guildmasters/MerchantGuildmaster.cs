namespace Server.Mobiles
{
    public class MerchantGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public MerchantGuildmaster() : base("merchant")
        {
            SetSkill(SkillName.ItemID, 85.0, 100.0);
            SetSkill(SkillName.ArmsLore, 85.0, 100.0);
        }

        public MerchantGuildmaster(Serial serial) : base(serial)
        {
        }

        public override NpcGuild NpcGuild => NpcGuild.MerchantsGuild;

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
