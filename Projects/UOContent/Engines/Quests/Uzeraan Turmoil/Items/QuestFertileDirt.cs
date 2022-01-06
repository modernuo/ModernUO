using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
    public class QuestFertileDirt : QuestItem
    {
        [Constructible]
        public QuestFertileDirt() : base(0xF81) => Weight = 1.0;

        public QuestFertileDirt(Serial serial) : base(serial)
        {
        }

        public override bool CanDrop(PlayerMobile player) => player.Quest is not UzeraanTurmoilQuest;

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
