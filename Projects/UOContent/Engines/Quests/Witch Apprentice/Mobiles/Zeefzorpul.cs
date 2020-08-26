using Server.Mobiles;

namespace Server.Engines.Quests.Hag
{
    public class Zeefzorpul : BaseQuester
    {
        public Zeefzorpul()
        {
        }

        public Zeefzorpul(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Zeefzorpul";

        public override void InitBody()
        {
            Body = 0x4A;
        }

        public override bool CanTalkTo(PlayerMobile to) => false;

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
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

            Delete();
        }
    }
}
