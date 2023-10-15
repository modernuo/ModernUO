using System;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;

namespace Server.Commands
{
    public static class Wipe
    {
        [Flags]
        public enum WipeType
        {
            Items = 0x01,
            Mobiles = 0x02,
            Multis = 0x04,
            All = Items | Mobiles | Multis
        }

        public static void Initialize()
        {
            CommandSystem.Register("Wipe", AccessLevel.GameMaster, WipeAll_OnCommand);
            CommandSystem.Register("WipeItems", AccessLevel.GameMaster, WipeItems_OnCommand);
            CommandSystem.Register("WipeNPCs", AccessLevel.GameMaster, WipeNPCs_OnCommand);
            CommandSystem.Register("WipeMultis", AccessLevel.GameMaster, WipeMultis_OnCommand);
        }

        [Usage("Wipe")]
        [Description("Wipes all items and npcs in a targeted bounding box.")]
        private static void WipeAll_OnCommand(CommandEventArgs e)
        {
            BeginWipe(e.Mobile, WipeType.Items | WipeType.Mobiles);
        }

        [Usage("WipeItems")]
        [Description("Wipes all items in a targeted bounding box.")]
        private static void WipeItems_OnCommand(CommandEventArgs e)
        {
            BeginWipe(e.Mobile, WipeType.Items);
        }

        [Usage("WipeNPCs")]
        [Description("Wipes all npcs in a targeted bounding box.")]
        private static void WipeNPCs_OnCommand(CommandEventArgs e)
        {
            BeginWipe(e.Mobile, WipeType.Mobiles);
        }

        [Usage("WipeMultis")]
        [Description("Wipes all multis in a targeted bounding box.")]
        private static void WipeMultis_OnCommand(CommandEventArgs e)
        {
            BeginWipe(e.Mobile, WipeType.Multis);
        }

        public static void BeginWipe(Mobile from, WipeType type)
        {
            BoundingBoxPicker.Begin(from, (map, start, end) => DoWipe(from, map, start, end, type));
        }

        public static void DoWipe(Mobile from, Map map, Point3D start, Point3D end, WipeType type)
        {
            CommandLogging.WriteLine(
                from,
                $"{from.AccessLevel} {CommandLogging.Format(from)} wiping from {start} to {end} in {type} ({map})"
            );

            var mobiles = (type & WipeType.Mobiles) != 0;
            var multis = (type & WipeType.Multis) != 0;
            var items = (type & WipeType.Items) != 0;

            var toDelete = new List<IEntity>();

            var rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

            IPooledEnumerable<IEntity> eable;

            if (!items && !multis || !mobiles)
            {
                return;
            }

            eable = map.GetObjectsInBounds(rect);

            foreach (var obj in eable)
            {
                if (items && obj is Item && !(obj is BaseMulti or HouseSign))
                {
                    toDelete.Add(obj);
                }
                else if (multis && obj is BaseMulti)
                {
                    toDelete.Add(obj);
                }
                else if (obj is Mobile mobile && !mobile.Player)
                {
                    toDelete.Add(mobile);
                }
            }

            for (var i = 0; i < toDelete.Count; ++i)
            {
                toDelete[i].Delete();
            }
        }
    }
}
