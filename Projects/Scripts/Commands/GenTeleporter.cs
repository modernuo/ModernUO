using Server.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Commands
{
  public class MapConverter : JsonConverter<Map>
  {
    public override Map Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      => Map.Parse(reader.GetString());
    public override void Write(Utf8JsonWriter writer, Map value, JsonSerializerOptions options)
      => writer.WriteStringValue(value.Name);
  }
  public class Point3DConverter : JsonConverter<Point3D>
  {
    public override Point3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartArray)
        throw new JsonException($"Point3D requires an array of exactly 3 integers. Invalid Starting TokenType: ${reader.TokenType}");
      reader.Read();
      var data = new List<int>();
      for (; reader.TokenType != JsonTokenType.EndArray; reader.Read())
        data.Add(reader.GetInt32());
      if (data.Count < 2 || data.Count > 3)
        throw new JsonException($"Invalid number of parameters for Point3D, {data.Count} were provided when either 2 or 3 expected.");
      return new Point3D(data[0], data[1], data.Count == 3 ? data[2] : 0);
    }
    public override void Write(Utf8JsonWriter writer, Point3D value, JsonSerializerOptions options)
    {
      writer.WriteStartArray();
      writer.WriteNumberValue(value.X);
      writer.WriteNumberValue(value.Y);
      writer.WriteNumberValue(value.Z);
      writer.WriteEndArray();
    }
  }

  public struct Location
  {
    [JsonPropertyName("p"), JsonConverter(typeof(Point3DConverter))] public Point3D Pos { get; set; }
    [JsonPropertyName("m"), JsonConverter(typeof(MapConverter))] public Map Map { get; set; }
    public override string ToString() => $"({Map.Name}:{Pos.X},{Pos.Y},{Pos.Z})";
    public override int GetHashCode() => ToString().GetHashCode();
  }
  public struct TeleporterDefinition
  {
    [JsonPropertyName("s")] public Location Source { get; set; }
    [JsonPropertyName("d")] public Location Destination { get; set; }
    [JsonPropertyName("b")] public bool Back { get; set; }
    public override string ToString() => $"{{{Source},{Destination},{Back}}}";
    public override int GetHashCode() => ToString().GetHashCode();
  }
  public class GenTeleporter
  {
    private const int SuccessHue = 72, WarningHue = 53, ErrorHue = 33;
    private static readonly string TeleporterDataPath = Path.Combine("Data", "teleporters.csv");
    private static readonly string TeleporterJsonDataPath = Path.Combine("Data", "teleporters.json");
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
      AllowTrailingCommas = true,
      PropertyNameCaseInsensitive = true,
      ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static void Initialize()
    {
      CommandSystem.Register("TelGen", AccessLevel.Administrator, new CommandEventHandler(GenTeleporter_OnCommand));
      CommandSystem.Register("TelGenDelete", AccessLevel.Administrator, new CommandEventHandler(TelGenDelete_OnCommand));
    }

    [Usage("TelGenDelete")]
    [Description("Destroys world/dungeon teleporters for all facets.")]
    public static void TelGenDelete_OnCommand(CommandEventArgs e)
    {
      e.Mobile.SendMessage("Removing teleporters, please wait.");
      int count = 0;
      if (ProcessTeleporterData(e, x =>
       {
         count += TeleportersCreator.DeleteTeleporters(x.Source);
         if (x.Back) count += TeleportersCreator.DeleteTeleporters(x.Destination);
       }))
      {
        if (count > 0)
          e.Mobile.SendMessage(WarningHue, $"Partial Completion, {count} Teleporters Removed.");
        return;
      }
      e.Mobile.SendMessage(WarningHue, $"{count} Teleporters Removed.");
    }

    [Usage("TelGen")]
    [Description("Generates world/dungeon teleporters for all facets.")]
    public static void GenTeleporter_OnCommand(CommandEventArgs e)
    {
      e.Mobile.SendMessage("Generating teleporters, please wait.");
      TeleportersCreator c = new TeleportersCreator();
      if (ProcessTeleporterData(e, c.CreateTeleporter))
      {
        if (c.DelCount > 0)
          e.Mobile.SendMessage(WarningHue, $"Partial Completion: {c.DelCount} Teleporters Removed.");
        if (c.Count > 0)
          e.Mobile.SendMessage(WarningHue, $"Partial Completion: {c.Count} Teleporters Added.");
        return;
      }
      e.Mobile.SendMessage(SuccessHue, $"Teleporter generating complete.");
      e.Mobile.SendMessage(WarningHue, $"{c.DelCount} Teleporters Removed.");
      e.Mobile.SendMessage(SuccessHue, $"{c.Count} Teleporters Added.");
    }

    private static bool ProcessTeleporterData(CommandEventArgs e, Action<TeleporterDefinition> processor)
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
        e.Mobile.SendMessage(ErrorHue, $"Failed to load/process data file '{TeleporterJsonDataPath}'");
        return true;
      }
      return false;
    }

    class TeleportersCreator
    {
      public int Count { get; private set; }
      public int DelCount { get; private set; }

      public static int DeleteTeleporters(Location location)
      {
        bool IsWithinZ(int delta) => -12 <= delta && delta <= 12;
        IPooledEnumerable<Teleporter> eable = location.Map.GetItemsInRange<Teleporter>(location.Pos, 0);
        var items = eable.Where(x => !(x is KeywordTeleporter || x is SkillTeleporter) && IsWithinZ(x.Z - location.Pos.Z)).ToList();
        eable.Free();
        int count = 0;
        foreach (var item in items)
        {
          count++;
          item.Delete();
        }
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
