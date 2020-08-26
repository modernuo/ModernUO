using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
    public class QuestDaemonBone : QuestItem
    {
        [Constructible]
        public QuestDaemonBone() : base(0xF80) => Weight = 1.0;

        public QuestDaemonBone(Serial serial) : base(serial)
        {
        }

        public override bool CanDrop(PlayerMobile player) => !(player.Quest is UzeraanTurmoilQuest);

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
