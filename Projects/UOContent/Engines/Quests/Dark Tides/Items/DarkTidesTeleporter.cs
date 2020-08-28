using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
    public class DarkTidesTeleporter : DynamicTeleporter
    {
        [Constructible]
        public DarkTidesTeleporter()
        {
        }

        public DarkTidesTeleporter(Serial serial) : base(serial)
        {
        }

        public override bool GetDestination(PlayerMobile player, ref Point3D loc, ref Map map)
        {
            var qs = player.Quest;

            if (qs is DarkTidesQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(FindMaabusTombObjective)))
                {
                    loc = new Point3D(2038, 1263, -90);
                    map = Map.Malas;
                    qs.AddConversation(new RadarConversation());
                    return true;
                }

                if (qs.IsObjectiveInProgress(typeof(FindCrystalCaveObjective)))
                {
                    loc = new Point3D(1194, 521, -90);
                    map = Map.Malas;
                    return true;
                }

                if (qs.IsObjectiveInProgress(typeof(FindCityOfLightObjective)))
                {
                    loc = new Point3D(1091, 519, -90);
                    map = Map.Malas;
                    return true;
                }

                if (qs.IsObjectiveInProgress(typeof(ReturnToCrystalCaveObjective)))
                {
                    loc = new Point3D(1194, 521, -90);
                    map = Map.Malas;
                    return true;
                }

                if (DarkTidesQuest.HasLostCallingScroll(player))
                {
                    loc = new Point3D(1194, 521, -90);
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
