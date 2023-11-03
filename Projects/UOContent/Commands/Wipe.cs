using System;
using Server.Collections;
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

            var rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

            using var toDelete = PooledRefQueue<IEntity>.Create();

            if (mobiles)
            {
                foreach (var mobile in map.GetMobilesInBounds(rect))
                {
                    if (!mobile.Player)
                    {
                        toDelete.Enqueue(mobile);
                    }
                }
            }

            if (items || multis)
            {
                foreach (var item in map.GetItemsInBounds(rect))
                {
                    if (item is BaseMulti)
                    {
                        if (multis)
                        {
                            toDelete.Enqueue(item);
                        }
                    }
                    else if (items && item is not HouseSign)
                    {
                        toDelete.Enqueue(item);
                    }
                }
            }

            while (toDelete.Count > 0)
            {
                toDelete.Dequeue().Delete();
            }
        }
    }
}
