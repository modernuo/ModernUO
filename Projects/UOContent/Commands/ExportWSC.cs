using System.Collections.Generic;
using System.IO;
using Server.Items;

namespace Server.Commands
{
    public static class ExportCommand
    {
        private const string ExportFile = @"C:\Uo\WorldForge\items.wsc";

        public static void Initialize()
        {
            CommandSystem.Register("ExportWSC", AccessLevel.Administrator, Export_OnCommand);
        }

        public static void Export_OnCommand(CommandEventArgs e)
        {
            var w = new StreamWriter(ExportFile);
            var remove = new List<Item>();
            var count = 0;

            e.Mobile.SendMessage($"Exporting all static items to \"{ExportFile}\"...");
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

            e.Mobile.SendMessage($"Export complete.  Exported {count} statics.");
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
