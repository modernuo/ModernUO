using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class EminosKatana : QuestItem
    {
        [Constructible]
        public EminosKatana() : base(0x13FF) => Weight = 1.0;

        public EminosKatana(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063214; // Daimyo Emino's Katana

        public override bool CanDrop(PlayerMobile player) => player.Quest is not EminosUndertakingQuest;

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
