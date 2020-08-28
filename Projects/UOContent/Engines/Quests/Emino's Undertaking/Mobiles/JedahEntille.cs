using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class JedahEntille : BaseQuester
    {
        [Constructible]
        public JedahEntille() : base("the Silent")
        {
        }

        public JedahEntille(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Jedah Entille";

        public override int TalkNumber => -1;

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x83FE;
            Female = true;
            Body = 0x191;
        }

        public override void InitOutfit()
        {
            HairItemID = 0x203C;
            HairHue = 0x6BE;

            AddItem(new PlainDress(0x528));
            AddItem(new ThighBoots());
            AddItem(new FloppyHat());
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
