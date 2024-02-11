using System.Collections.Generic;
using System.IO;
using Server.Items;

namespace Server.Commands
{
    public static class ExportCommand
    {
        public static void Configure()
        {
            CommandSystem.Register("ExportWSC", AccessLevel.Developer, Export_OnCommand);
        }

        [Usage("ExportWSC <file-path>")]
        [Description("Exports all static items to a WSC file.  This will delete all static items in the world.  Please make a backup.")]
        public static void Export_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length < 1)
            {
                e.Mobile.SendMessage("Usage: [ExportWSC <file-path>]");
                return;
            }

            var exportFilePath = e.GetString(0);
            var fi = new FileInfo(exportFilePath);
            fi.Directory.EnsureDirectory();

            var w = new StreamWriter(exportFilePath);
            var remove = new List<Item>();
            var count = 0;

            e.Mobile.SendMessage($"Exporting all static items to \"{exportFilePath}\"...");
            e.Mobile.SendMessage("This will delete all static items in the world.  Please make a backup.");

            foreach (var item in World.Items.Values)
            {
                if (item is Static or BaseFloor or BaseWall
                    && item.RootParent == null)
                {
                    w.WriteLine("SECTION WORLDITEM {0}", count);
                    w.WriteLine("{");
                    w.WriteLine("SERIAL {0}", item.Serial);
                    w.WriteLine("NAME #");
                    w.WriteLine("NAME2 #");
                    w.WriteLine("ID {0}", item.ItemID);
                    w.WriteLine("X {0}", item.X);
                    w.WriteLine("Y {0}", item.Y);
                    w.WriteLine("Z {0}", item.Z);
                    w.WriteLine("COLOR {0}", item.Hue);
                    w.WriteLine("CONT -1");
                    w.WriteLine("TYPE 0");
                    w.WriteLine("AMOUNT 1");
                    w.WriteLine("WEIGHT 255");
                    w.WriteLine("OWNER -1");
                    w.WriteLine("SPAWN -1");
                    w.WriteLine("VALUE 1");
                    w.WriteLine("}");
                    w.WriteLine("");

                    count++;
                    remove.Add(item);
                    w.Flush();
                }
            }

            w.Close();

            foreach (var item in remove)
            {
                item.Delete();
            }

            e.Mobile.SendMessage($"Export complete. Exported {count} statics.");
        }
    }
}
/*SECTION WORLDITEM 1
{
SERIAL 1073741830
NAME #
NAME2 #
ID 1709
X 1439
Y 1613
Z 20
CONT -1
TYPE 12
AMOUNT 1
WEIGHT 25500
OWNER -1
SPAWN -1
VALUE 1
}*/
