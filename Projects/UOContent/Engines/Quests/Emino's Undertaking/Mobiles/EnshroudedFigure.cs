using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class EnshroudedFigure : BaseQuester
    {
        [Constructible]
        public EnshroudedFigure()
        {
        }

        public EnshroudedFigure(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "an enshrouded figure";

        public override int TalkNumber => -1;

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x8401;
            Female = false;
            Body = 0x190;
        }

        public override void InitOutfit()
        {
            AddItem(new DeathShroud());
            AddItem(new ThighBoots());
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
