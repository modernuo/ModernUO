using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MageGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public MageGuildmaster() : base("mage")
        {
            SetSkill(SkillName.EvalInt, 85.0, 100.0);
            SetSkill(SkillName.Inscribe, 65.0, 88.0);
            SetSkill(SkillName.MagicResist, 64.0, 100.0);
            SetSkill(SkillName.Magery, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 60.0, 83.0);
            SetSkill(SkillName.Meditation, 85.0, 100.0);
            SetSkill(SkillName.Macing, 36.0, 68.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.MagesGuild;

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Robe(Utility.RandomBlueHue()));
            AddItem(new GnarledStaff());
        }
    }
}
