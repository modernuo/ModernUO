using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MerchantGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public MerchantGuildmaster() : base("merchant")
        {
            SetSkill(SkillName.ItemID, 85.0, 100.0);
            SetSkill(SkillName.ArmsLore, 85.0, 100.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.MerchantsGuild;
    }
}
