using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class NoteForZoel : QuestItem
    {
        [Constructible]
        public NoteForZoel() : base(0x14EF)
        {
            Weight = 1.0;
            Hue = 0x6B9;
        }

        public NoteForZoel(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063186; // A Note for Zoel

        public override bool CanDrop(PlayerMobile player) => !(player.Quest is EminosUndertakingQuest);

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
