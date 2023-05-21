using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class HealerGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public HealerGuildmaster() : base("healer")
        {
            SetSkill(SkillName.Anatomy, 85.0, 100.0);
            SetSkill(SkillName.Healing, 90.0, 100.0);
            SetSkill(SkillName.Forensics, 75.0, 98.0);
            SetSkill(SkillName.MagicResist, 75.0, 98.0);
            SetSkill(SkillName.SpiritSpeak, 65.0, 88.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.HealersGuild;

        public override VendorShoeType ShoeType => VendorShoeType.Sandals;

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Robe(Utility.RandomYellowHue()));
        }
    }
}
