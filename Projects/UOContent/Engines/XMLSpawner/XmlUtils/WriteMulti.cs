using System.IO;
using System.Collections;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.XmlSpawner2;

public class WriteMulti
{
    private class TileEntry
    {
        public int ID;
        public int X;
        public int Y;
        public int Z;

        public TileEntry(int id, int x, int y, int z)
        {
            ID = id;
            X = x;
            Y = y;
            Z = z;
        }
    }

    public static void Initialize()
    {

        CommandSystem.Register("WriteMulti", XmlSpawner.DiskAccessLevel, WriteMulti_OnCommand);
    }

    [Usage("WriteMulti <MultiFile> [zmin zmax][-noitems][-nostatics][-nomultis][-noaddons][-invisible]")]
    [Description("Creates a multi text file from the objects within the targeted area.  The min/max z range can also be specified.")]
    public static void WriteMulti_OnCommand(CommandEventArgs e)
    {
        if (e == null || e.Mobile == null)
        {
            return;
        }

        if (e.Mobile.AccessLevel < XmlSpawner.DiskAccessLevel)
        {
            e.Mobile.SendMessage("You do not have rights to perform this command.");
            return;
        }

        if (e.Arguments != null && e.Arguments.Length < 1)
        {
            e.Mobile.SendMessage($"Usage:  {e.Command} <MultiFile> [zmin zmax][-noitems][-nostatics][-nomultis][-noaddons][-invisible]");
            return;
        }

        string filename = e.Arguments[0];

        int zmin = int.MinValue;
        int zmax = int.MinValue;
        bool includeitems = true;
        bool includestatics = true;
        bool includemultis = true;
        bool includeaddons = true;
        bool includeinvisible = false;

        if (e.Arguments.Length > 1)
        {
            int index = 1;
            while (index < e.Arguments.Length)
            {
                if (e.Arguments[index] == "-noitems")
                {
                    includeitems = false;
                    index++;
                }
                else if (e.Arguments[index] == "-nostatics")
                {
                    includestatics = false;
                    index++;
                }
                else if (e.Arguments[index] == "-nomultis")
                {
                    includemultis = false;
                    index++;
                }
                else if (e.Arguments[index] == "-noaddons")
                {
                    includeaddons = false;
                    index++;
                }
                else if (e.Arguments[index] == "-invisible")
                {
                    includeinvisible = true;
                    index++;
                }
                else
                {
                    try
                    {
                        zmin = int.Parse(e.Arguments[index++]);
                        zmax = int.Parse(e.Arguments[index++]);
                    }
                    catch
                    {
                        e.Mobile.SendMessage($"{e.Command} : Invalid zmin zmax arguments");
                        return;
                    }
                }
            }
        }

        string dirname;
        if (Directory.Exists(XmlSpawner.XmlSpawnDir) && filename != null && !filename.StartsWith("/") && !filename.StartsWith("\\"))
        {
            // put it in the defaults directory if it exists
            dirname = $"{XmlSpawner.XmlSpawnDir}/{filename}";
        }
        else
        {
            // otherwise just put it in the main installation dir
            dirname = filename;
        }

        // check to see if the file already exists and can be written to by the owner
        if (File.Exists(dirname))
        {

            // check the file
            try
            {
                StreamReader op = new StreamReader(dirname, false);
                string line = op.ReadLine();

                op.Close();

                // check the first line
                if (line != null && line.Length > 0)
                {

                    string[] args = line.Split(" ".ToCharArray(), 3);
                    if (args.Length < 3)
                    {
                        e.Mobile.SendMessage($"Cannot overwrite file {dirname} : not owner");
                        return;
                    }

                    if (args[2] != e.Mobile.Name)
                    {
                        e.Mobile.SendMessage($"Cannot overwrite file {dirname} : not owner");
                        return;
                    }
                }
                else
                {
                    e.Mobile.SendMessage($"Cannot overwrite file {dirname} : not owner");
                    return;
                }

            }
            catch
            {
                e.Mobile.SendMessage($"Cannot overwrite file {dirname}");
                return;
            }

        }

        DefineMultiArea(e.Mobile, dirname, zmin, zmax, includeitems, includestatics, includemultis, includeinvisible, includeaddons);
    }

    public static void DefineMultiArea(Mobile m, string dirname, int zmin, int zmax, bool includeitems, bool includestatics,
        bool includemultis, bool includeinvisible, bool includeaddons)
    {
        BoundingBoxPicker.Begin(
            m,
            (map, start, end) => DefineMultiArea_Callback(
                m,
                map,
                start,
                end,
                dirname,
                zmin,
                zmax,
                includeitems,
                includestatics,
                includemultis,
                includeinvisible,
                includeaddons
            )
        );
    }

    private static void DefineMultiArea_Callback(
        Mobile from,
        Map map,
        Point3D start,
        Point3D end,
        string dirname,
        int zmin,
        int zmax,
        bool includeitems,
        bool includestatics,
        bool includemultis,
        bool includeinvisible,
        bool includeaddons
    )
    {
        if (from != null && map != null)
        {
            ArrayList itemlist = new ArrayList();
            ArrayList staticlist = new ArrayList();
            ArrayList tilelist = new ArrayList();

            int sx = start.X > end.X ? end.X : start.X;
            int sy = start.Y > end.Y ? end.Y : start.Y;
            int ex = start.X < end.X ? end.X : start.X;
            int ey = start.Y < end.Y ? end.Y : start.Y;

            // find all of the world-placed items within the specified area
            if (includeitems)
            {
                // make the first pass for items only
                IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(sx, sy, ex - sx + 1, ey - sy + 1));

                foreach (Item item in eable)
                {
                    // is it within the bounding area
                    if (item.Parent == null && (zmin == int.MinValue || item.Location.Z >= zmin && item.Location.Z <= zmax))
                    {
                        // add the item
                        if ((includeinvisible || item.Visible) && item.ItemID <= 16383)
                        {
                            itemlist.Add(item);
                        }
                    }
                }

                eable.Free();

                int searchrange = 100;

                // make the second expanded pass to pick up addon components and multi components
                eable = map.GetItemsInBounds(new Rectangle2D(sx - searchrange, sy - searchrange, ex - sy + searchrange * 2 + 1,
                    ey - sy + searchrange * 2 + 1));

                foreach (Item item in eable)
                {
                    // is it within the bounding area
                    if (item.Parent == null)
                    {

                        if (item is BaseAddon addon && includeaddons)
                        {
                            // go through all of the addon components
                            foreach (AddonComponent c in addon.Components)
                            {
                                int x = c.X;
                                int y = c.Y;
                                int z = c.Z;

                                if ((includeinvisible || addon.Visible) && (addon.ItemID <= 16383 || includemultis) &&
                                    x >= sx && x <= ex && y >= sy && y <= ey && (zmin == int.MinValue || z >= zmin && z <= zmax))
                                {
                                    itemlist.Add(c);
                                }
                            }
                        }

                        if (item is BaseMulti multi && includemultis)
                        {
                            // go through all of the multi components
                            MultiComponentList mcl = multi.Components;
                            if (mcl != null && mcl.List != null)
                            {
                                for (int i = 0; i < mcl.List.Length; i++)
                                {
                                    MultiTileEntry t = mcl.List[i];

                                    int x = t.OffsetX + multi.X;
                                    int y = t.OffsetY + multi.Y;
                                    int z = t.OffsetZ + multi.Z;
                                    int itemID = t.ItemId & 0x3FFF;

                                    if (x >= sx && x <= ex && y >= sy && y <= ey && (zmin == int.MinValue || z >= zmin && z <= zmax))
                                    {
                                        tilelist.Add(new TileEntry(itemID, x, y, z));
                                    }
                                }

                            }
                        }
                    }
                }

                eable.Free();
            }

            // find all of the static tiles within the specified area
            if (includestatics)
            {
                // count the statics
                for (int x = sx; x < ex; x++)
                {
                    for (int y = sy; y < ey; y++)
                    {
                        StaticTile[] statics = map.Tiles.GetStaticTiles(x, y, false);

                        for (int j = 0; j < statics.Length; j++)
                        {
                            if (zmin == int.MinValue || statics[j].Z >= zmin && statics[j].Z <= zmax)
                            {
                                staticlist.Add(new TileEntry(statics[j].ID & 0x3FFF, x, y, statics[j].Z));
                            }
                        }
                    }
                }
            }

            int nstatics = staticlist.Count;
            int nitems = itemlist.Count;
            int ntiles = tilelist.Count;

            int ntotal = nitems + nstatics + ntiles;

            int ninvisible = 0;
            int nmultis = ntiles;
            int naddons = 0;

            foreach (Item item in itemlist)
            {
                int x = item.X - from.X;
                int y = item.Y - from.Y;
                int z = item.Z - from.Z;

                if (item.ItemID > 16383)
                {
                    nmultis++;
                }
                if (!item.Visible)
                {
                    ninvisible++;
                }
                if (item is BaseAddon || item is AddonComponent)
                {
                    naddons++;
                }
            }

            try
            {
                // open the file, overwrite any previous contents
                StreamWriter op = new StreamWriter(dirname, false);

                if (op != null)
                {
                    // write the header
                    op.WriteLine("1 version {0}", from.Name);
                    op.WriteLine("{0} num components", ntotal);

                    // write out the items
                    foreach (Item item in itemlist)
                    {

                        int x = item.X - from.X;
                        int y = item.Y - from.Y;
                        int z = item.Z - from.Z;

                        if (item.Hue > 0)
                        {
                            // format is x y z visible hue
                            op.WriteLine("{0} {1} {2} {3} {4} {5}", item.ItemID, x, y, z, item.Visible ? 1 : 0, item.Hue);
                        }
                        else
                        {
                            // format is x y z visible
                            op.WriteLine("{0} {1} {2} {3} {4}", item.ItemID, x, y, z, item.Visible ? 1 : 0);
                        }
                    }

                    if (includestatics)
                    {
                        foreach (TileEntry s in staticlist)
                        {
                            int x = s.X - from.X;
                            int y = s.Y - from.Y;
                            int z = s.Z - from.Z;
                            int ID = s.ID;
                            op.WriteLine("{0} {1} {2} {3} {4}", ID, x, y, z, 1);
                        }
                    }

                    if (includemultis)
                    {
                        foreach (TileEntry s in tilelist)
                        {
                            int x = s.X - from.X;
                            int y = s.Y - from.Y;
                            int z = s.Z - from.Z;
                            int ID = s.ID;
                            op.WriteLine("{0} {1} {2} {3} {4}", ID, x, y, z, 1);
                        }
                    }
                }

                op.Close();
            }
            catch
            {
                from.SendMessage($"Error writing multi file {dirname}");
                return;
            }

            from.SendMessage(66, "WriteMulti results:");

            if (includeitems)
            {
                from.SendMessage(66, $"Included {nitems} items");

                if (includemultis)
                {
                    from.SendMessage($"{nmultis} multis");
                }
                else
                {
                    from.SendMessage(33, "Ignored multis");
                }

                if (includeinvisible)
                {
                    from.SendMessage($"{ninvisible} invisible");
                }
                else
                {
                    from.SendMessage(33, "Ignored invisible");
                }

                if (includeaddons)
                {
                    from.SendMessage($"{naddons} addons");
                }
                else
                {
                    from.SendMessage(33, "Ignored addons");
                }

            }
            else
            {
                from.SendMessage(33, "Ignored items");
            }

            if (includestatics)
            {
                from.SendMessage(66, $"Included {nstatics} statics");
            }
            else
            {
                from.SendMessage(33, "Ignored statics");
            }

            from.SendMessage(66, $"Saved {ntotal} components to {dirname}");
        }
    }
}
