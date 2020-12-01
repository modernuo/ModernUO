using System.Collections.Generic;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.GargoyleAlchemist")]
    public class Glassblower : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Glassblower() : base("the alchemist")
        {
            SetSkill(SkillName.Alchemy, 85.0, 100.0);
            SetSkill(SkillName.TasteID, 85.0, 100.0);
        }

        public Glassblower(Serial serial) : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.MagesGuild;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBGlassblower());
            m_SBInfos.Add(new SBAlchemist());
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

            if (Body == 0x2F2)
            {
                Body = 0x2F6;
            }
        }
    }
}
