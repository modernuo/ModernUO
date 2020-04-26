using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Items;
using Server.Json;

namespace Server.Commands
{
  public struct Location
  {
    [JsonPropertyName("point"), JsonConverter(typeof(Point3dConverter))]
    public Point3D Pos { get; set; }
    [JsonPropertyName("map"), JsonConverter(typeof(MapConverter))]
    public Map Map { get; set; }
    public override string ToString() => $"({Map.Name}:{Pos.X},{Pos.Y},{Pos.Z})";
    public override int GetHashCode() => ToString().GetHashCode();
  }

  public struct TeleporterDefinition
  {
    [JsonPropertyName("source")]
    public Location Source { get; set; }
    [JsonPropertyName("destination")]
    public Location Destination { get; set; }
    [JsonPropertyName("back")]
    public bool Back { get; set; }
    public override string ToString() => $"{{{Source},{Destination},{Back}}}";
    public override int GetHashCode() => ToString().GetHashCode();
  }

  public static class GenTeleporter
  {
    private const int SuccessHue = 72, WarningHue = 53, ErrorHue = 33;
    private static readonly string TeleporterJsonDataPath = Path.Combine("Data", "teleporters.json");
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
      AllowTrailingCommas = true,
      PropertyNameCaseInsensitive = true,
      ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static void Initialize()
    {
      CommandSystem.Register("TelGen", AccessLevel.Administrator, GenTeleporter_OnCommand);
      CommandSystem.Register("TelGenDelete", AccessLevel.Administrator, TelGenDelete_OnCommand);
    }

    [Usage("TelGenDelete")]
    [Description("Destroys world/dungeon teleporters for all facets.")]
    public static void TelGenDelete_OnCommand(CommandEventArgs e)
    {
      Mobile from = e.Mobile;

      from.SendMessage("Removing teleporters, please wait.");
      int count = 0;

      void ProcessDeletion(TeleporterDefinition x)
      {
        count += TeleportersCreator.DeleteTeleporters(x.Source);
        if (x.Back) count += TeleportersCreator.DeleteTeleporters(x.Destination);
      }

      if (!ProcessTeleporterData(from, ProcessDeletion))
      {
        if (count > 0)
          from.SendMessage(WarningHue, $"Partial Completion, {count} Teleporters Removed.");
        return;
      }

      from.SendMessage(WarningHue, $"{count} Teleporters Removed.");
    }

    [Usage("TelGen")]
    [Description("Generates world/dungeon teleporters for all facets.")]
    public static void GenTeleporter_OnCommand(CommandEventArgs e)
    {
      Mobile from = e.Mobile;

      from.SendMessage("Generating teleporters, please wait.");
      TeleportersCreator c = new TeleportersCreator();

      if (!ProcessTeleporterData(from, c.CreateTeleporter))
      {
        if (c.DelCount > 0)
          from.SendMessage(WarningHue, $"Partial Completion: {c.DelCount} Teleporters Removed.");
        if (c.Count > 0)
          from.SendMessage(WarningHue, $"Partial Completion: {c.Count} Teleporters Added.");
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
        string json;
        using (StreamReader reader = new StreamReader(TeleporterJsonDataPath))
          json = reader.ReadToEnd();
        var teleporters = JsonSerializer.Deserialize<List<TeleporterDefinition>>(json, JsonOptions);
        for (int i = 0; i < teleporters.Count; i++)
          processor(teleporters[i]);
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

      public static int DeleteTeleporters(Location location)
      {
        IPooledEnumerable<Teleporter> eable = location.Map.GetItemsInRange<Teleporter>(location.Pos, 0);
        var items = eable
          .Where(x => !(x is KeywordTeleporter || x is SkillTeleporter) && IsWithinZ(x.Z - location.Pos.Z));
        int count = 0;
        foreach (var item in items)
        {
          count++;
          item.Delete();
        }
        eable.Free();
        return count;
      }

      public void CreateTeleporter(TeleporterDefinition telDef)
      {
        DelCount += DeleteTeleporters(telDef.Source);
        Count++;
        new Teleporter(telDef.Destination.Pos, telDef.Destination.Map).MoveToWorld(telDef.Source.Pos, telDef.Source.Map);
        if (!telDef.Back) return;
        DelCount += DeleteTeleporters(telDef.Destination);
        Count++;
        new Teleporter(telDef.Source.Pos, telDef.Source.Map).MoveToWorld(telDef.Destination.Pos, telDef.Destination.Map);
      }
    }
  }
}
