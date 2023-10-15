using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Items;
using Server.Json;
using Server.Network;

namespace Server.Commands
{
    public struct TeleporterDefinition
    {
        [JsonPropertyName("src")]
        public WorldLocation Source { get; set; }

        [JsonPropertyName("dst")]
        public WorldLocation Destination { get; set; }

        [JsonPropertyName("back")]
        public bool Back { get; set; }

        public override string ToString() => $"{{{Source},{Destination},{Back}}}";
    }

    public static class GenTeleporter
    {
        private const int SuccessHue = 72, WarningHue = 53, ErrorHue = 33;
        private static readonly string TeleporterJsonDataPath = Path.Combine(Core.BaseDirectory, "Data/teleporters.json");

        public static void Initialize()
        {
            CommandSystem.Register("TelGen", AccessLevel.Administrator, GenTeleporter_OnCommand);
            CommandSystem.Register("TelGenDelete", AccessLevel.Administrator, TelGenDelete_OnCommand);
        }

        [Usage("TelGenDelete")]
        [Description("Destroys world/dungeon teleporters for all facets.")]
        public static void TelGenDelete_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;

            from.SendMessage("Removing teleporters, please wait.");
            var count = 0;

            void ProcessDeletion(TeleporterDefinition x)
            {
                count += TeleportersCreator.DeleteTeleporters(x.Source);
                if (x.Back)
                {
                    count += TeleportersCreator.DeleteTeleporters(x.Destination);
                }
            }

            if (!ProcessTeleporterData(from, ProcessDeletion))
            {
                if (count > 0)
                {
                    from.SendMessage(WarningHue, $"Partial Completion, {count} Teleporters Removed.");
                }

                return;
            }

            from.SendMessage(WarningHue, $"{count} Teleporters Removed.");
        }

        [Usage("TelGen")]
        [Description("Generates world/dungeon teleporters for all facets.")]
        public static void GenTeleporter_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;

            from.SendMessage("Generating teleporters, please wait.");

            NetState.FlushAll();

            var c = new TeleportersCreator();

            if (!ProcessTeleporterData(from, c.CreateTeleporter))
            {
                if (c.DelCount > 0)
                {
                    from.SendMessage(WarningHue, $"Partial Completion: {c.DelCount} Teleporters Removed.");
                }

                if (c.Count > 0)
                {
                    from.SendMessage(WarningHue, $"Partial Completion: {c.Count} Teleporters Added.");
                }

                return;
            }

            from.SendMessage(SuccessHue, "Teleporter generating complete.");
            from.SendMessage(WarningHue, $"{c.DelCount} Teleporters Removed.");
            from.SendMessage(SuccessHue, $"{c.Count} Teleporters Added.");
        }

        private static bool ProcessTeleporterData(Mobile m, Action<TeleporterDefinition> processor)
        {
            try
            {
                JsonConfig.Deserialize<List<TeleporterDefinition>>(TeleporterJsonDataPath).ForEach(processor);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                m.SendMessage(ErrorHue, $"Failed to load/process data file '{TeleporterJsonDataPath}'");
                return false;
            }

            return true;
        }

        private class TeleportersCreator
        {
            public int Count { get; private set; }
            public int DelCount { get; private set; }

            private static bool IsWithinZ(int delta) => delta >= -12 && delta <= 12;

            public static int DeleteTeleporters(WorldLocation worldLocation)
            {
                var eable = worldLocation.Map.GetItemsInRange<Teleporter>(worldLocation, 0);

                var count = 0;
                foreach (var item in eable)
                {
                    if (!(item is KeywordTeleporter or SkillTeleporter) && IsWithinZ(item.Z - worldLocation.Z))
                    {
                        count++;
                        item.Delete();
                    }
                }
                return count;
            }

            public void CreateTeleporter(TeleporterDefinition telDef)
            {
                DelCount += DeleteTeleporters(telDef.Source);
                Count++;
                new Teleporter(telDef.Destination, telDef.Destination.Map).MoveToWorld(telDef.Source, telDef.Source.Map);
                if (!telDef.Back)
                {
                    return;
                }

                DelCount += DeleteTeleporters(telDef.Destination);
                Count++;
                new Teleporter(telDef.Source, telDef.Source.Map).MoveToWorld(telDef.Destination, telDef.Destination.Map);
            }
        }
    }
}
