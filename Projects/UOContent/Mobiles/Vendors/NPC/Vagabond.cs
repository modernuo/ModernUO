using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class Vagabond : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Vagabond() : base("the vagabond")
        {
            SetSkill(SkillName.ItemID, 60.0, 83.0);
        }

        public Vagabond(Serial serial) : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBTinker());
            m_SBInfos.Add(new SBVagabond());
        }

        public override void InitOutfit()
        {
            AddItem(new FancyShirt(Utility.RandomBrightHue()));
            AddItem(new Shoes(GetShoeHue()));
            AddItem(new LongPants(GetRandomHue()));

            if (Utility.RandomBool())
            {
                AddItem(new Cloak(Utility.RandomBrightHue()));
            }

            AddItem(
                Utility.RandomBool()
                    ? (Item)new SkullCap(Utility.RandomNeutralHue())
                    : new Bandana(Utility.RandomNeutralHue())
            );

            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, HairHue);

            PackGold(100, 200);
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
