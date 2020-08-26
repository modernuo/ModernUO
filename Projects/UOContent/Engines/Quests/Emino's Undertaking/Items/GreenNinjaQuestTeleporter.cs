using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class GreenNinjaQuestTeleporter : DynamicTeleporter
    {
        [Constructible]
        public GreenNinjaQuestTeleporter() : base(0x51C, 0x17E)
        {
        }

        public GreenNinjaQuestTeleporter(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1026157; // teleporter

        public override int NotWorkingMessage => 1063198; // You stand on the strange floor tile but nothing happens.

        public override bool GetDestination(PlayerMobile player, ref Point3D loc, ref Map map)
        {
            var qs = player.Quest;

            if (qs is EminosUndertakingQuest && qs.FindObjective<UseTeleporterObjective>() != null)
            {
                loc = new Point3D(410, 1125, 0);
                map = Map.Malas;

                return true;
            }

            return false;
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
