using Server.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Commands
{
  public class Location : IComparable
  {
    public Point3D Pos;
    public Map Map;
    public Location(int x, int y, int z, Map m)
    {
      Pos = new Point3D(x, y, z);
      Map = m;
    }

    public int CompareTo(object obj)
    {
      if (!(obj is Location))
        return GetHashCode().CompareTo(obj.GetHashCode());

      Location l = (Location)obj;
      if (l.Map.MapID != Map.MapID)
        return l.Map.MapID - Map.MapID;
      if (l.Pos.X != Pos.X)
        return l.Pos.X - Pos.X;
      if (l.Pos.Y != Pos.Y)
        return l.Pos.Y - Pos.Y;
      return l.Pos.Z - Pos.Z;
    }

    public override string ToString() => $"{{{Map.MapID}}}:({Pos.X},{Pos.Y},{Pos.Z})";
    public override int GetHashCode() => ToString().GetHashCode();
  }
  public class TeleporterDefinition
  {
    public Location Source;
    public Location Destination;
    public bool Back;
    public TeleporterDefinition(Location s, Location d, bool b)
    {
      Source = s;
      Destination = d;
      Back = b;
    }
  }
  public class GenTeleporter
  {
    private const int SuccessHue = 72;
    private const int WarningHue = 53;
    private const int ErrorHue = 33;
    private const string CSVSeperator = ",";
    private static readonly string TeleporterDataPath = Path.Combine("Data", "teleporters.csv");
    private static readonly List<Item> m_List = new List<Item>();

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
      ProcessTeleporterData(e, x =>
      {
        count += TeleportersCreator.DeleteTeleporters(x.Source);
        if (x.Back) count += TeleportersCreator.DeleteTeleporters(x.Destination);
      });
      e.Mobile.SendMessage(WarningHue, $"{count} Teleporters Removed.");
    }

    [Usage("TelGen")]
    [Description("Generates world/dungeon teleporters for all facets.")]
    public static void GenTeleporter_OnCommand(CommandEventArgs e)
    {
      e.Mobile.SendMessage("Generating teleporters, please wait.");
      TeleportersCreator c = new TeleportersCreator();
      ProcessTeleporterData(e, c.CreateTeleporter);
      e.Mobile.SendMessage(SuccessHue, $"Teleporter generating complete.");
      e.Mobile.SendMessage(WarningHue, $"{c.DelCount} Teleporters Removed.");
      e.Mobile.SendMessage(SuccessHue, $"{c.Count} Teleporters Added.");
    }

    private static void ProcessTeleporterData(CommandEventArgs e, Action<TeleporterDefinition> processor)
    {
      using StreamReader reader = new StreamReader(TeleporterDataPath);
      string line;
      int lineNum = 0;
      while ((line = reader.ReadLine()) != null)
      {
        lineNum++;
        line = line.Trim();
        if (line.StartsWith("#"))
          continue;
        string[] parts = line.Split(CSVSeperator);
        if (parts.Length != 9)
        {
          e.Mobile.SendMessage(ErrorHue, $"Bad teleporter definition on line {lineNum}");
          continue;
        }
        try
        {
          processor(new TeleporterDefinition(
            new Location(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), Map.Parse(parts[3])),
            new Location(int.Parse(parts[4]), int.Parse(parts[5]), int.Parse(parts[6]), Map.Parse(parts[7])),
            bool.Parse(parts[8])));
        }
        catch (FormatException)
        {
          e.Mobile.SendMessage(ErrorHue, $"Bad number format on line {lineNum}");
        }
        catch (ArgumentException ex)
        {
          e.Mobile.SendMessage(ErrorHue, $"Argument Execption {ex.Message} on line {lineNum}");
        }
      }
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
