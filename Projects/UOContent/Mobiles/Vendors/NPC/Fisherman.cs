using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class Fisherman : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Fisherman() : base("the fisher")
        {
            SetSkill(SkillName.Fishing, 75.0, 98.0);
        }

        public Fisherman(Serial serial) : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override NpcGuild NpcGuild => NpcGuild.FishermensGuild;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBFisherman());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new FishingPole());
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
