using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class WhiteNinjaQuestTeleporter : DynamicTeleporter
    {
        [Constructible]
        public WhiteNinjaQuestTeleporter() : base(0x51C, 0x47E)
        {
        }

        public WhiteNinjaQuestTeleporter(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1026157; // teleporter

        public override int NotWorkingMessage => 1063198; // You stand on the strange floor tile but nothing happens.

        public override bool GetDestination(PlayerMobile player, ref Point3D loc, ref Map map)
        {
            var qs = player.Quest;

            if (qs is EminosUndertakingQuest)
            {
                QuestObjective obj = qs.FindObjective<SearchForSwordObjective>();

                if (obj != null)
                {
                    if (!obj.Completed)
                    {
                        obj.Complete();
                    }

                    loc = new Point3D(411, 1085, 0);
                    map = Map.Malas;

                    return true;
                }
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
