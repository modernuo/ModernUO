using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands.Generic;
using Server.Items;

namespace Server.Engines.Spawners;

public class ProjectSpawnerCommand : BaseCommand
{
    // ItemID constants for border pieces
    private const int SouthEastCorner = 0xF8; // Bottom-right corner
    private const int NorthSouthWall = 0xF9;  // Horizontal walls
    private const int EastWestWall = 0xFA;    // Vertical walls
    private const int NorthWestCorner = 0xFB; // Top-left corner

    private static readonly Dictionary<Item, List<SpawnerBorder>> _projectedSpawners = [];

    public static void Configure()
    {
        TargetCommands.Register(new ProjectSpawnerCommand());
    }

    public ProjectSpawnerCommand()
    {
        AccessLevel = AccessLevel.Developer;
        Supports = CommandSupport.Simple;
        Commands = ["ProjectSpawner"];
        ObjectTypes = ObjectTypes.Items;
        Usage = "ProjectSpawner";
        Description = "Toggles spawn bounds projection for the targeted spawner.";
    }

    public override void Execute(CommandEventArgs e, object obj)
    {
        if (obj is not BaseSpawner spawner)
        {
            LogFailure("That is not a spawner.");
            return;
        }

        if (_projectedSpawners.Remove(spawner, out var borders))
        {
            foreach (var border in borders)
            {
                border.Delete();
            }

            e.Mobile.SendMessage("Spawner projection removed.");
            return;
        }

        var bounds = spawner.SpawnBounds;
        if (bounds == default)
        {
            LogFailure("Spawner has no spawn bounds defined.");
            return;
        }

        borders = CreateBorders(spawner, bounds);
        if (borders.Count == 0)
        {
            LogFailure("Could not create borders for spawner bounds.");
            return;
        }

        _projectedSpawners[spawner] = borders;
        e.Mobile.SendMessage($"Spawner projection created with {borders.Count} border pieces.");
    }

    private static List<SpawnerBorder> CreateBorders(Item spawner, Rectangle3D bounds)
    {
        var borders = new List<SpawnerBorder>();
        var map = spawner.Map;
        var z = spawner.Z;

        var minX = bounds.Start.X;
        var minY = bounds.Start.Y;
        var maxX = bounds.End.X; // Exclusive
        var maxY = bounds.End.Y; // Exclusive

        // NW Corner at (minX - 1, minY - 1)
        borders.Add(CreateBorder(spawner, NorthWestCorner, minX - 1, minY - 1, z, map));

        // SE Corner at (maxX - 1, maxY - 1)
        borders.Add(CreateBorder(spawner, SouthEastCorner, maxX - 1, maxY - 1, z, map));

        // North bound: y = minY - 1, x from minX to maxX - 1
        for (var x = minX; x < maxX; x++)
        {
            borders.Add(CreateBorder(spawner, NorthSouthWall, x, minY - 1, z, map));
        }

        // South bound: y = maxY - 1, x from minX to maxX - 2 (excluding SE corner)
        for (var x = minX; x < maxX - 1; x++)
        {
            borders.Add(CreateBorder(spawner, NorthSouthWall, x, maxY - 1, z, map));
        }

        // West bound: x = minX - 1, y from minY to maxY - 1
        for (var y = minY; y < maxY; y++)
        {
            borders.Add(CreateBorder(spawner, EastWestWall, minX - 1, y, z, map));
        }

        // East bound: x = maxX - 1, y from minY to maxY - 2 (excluding SE corner)
        for (var y = minY; y < maxY - 1; y++)
        {
            borders.Add(CreateBorder(spawner, EastWestWall, maxX - 1, y, z, map));
        }

        return borders;
    }

    private static SpawnerBorder CreateBorder(Item spawner, int itemId, int x, int y, int z, Map map)
    {
        var border = new SpawnerBorder(itemId)
        {
            Spawner = spawner
        };
        border.MoveToWorld(new Point3D(x, y, z), map);
        return border;
    }

    public static void RemoveProjection(Item spawner)
    {
        if (_projectedSpawners.Remove(spawner, out var borders))
        {
            foreach (var border in borders)
            {
                border.Delete();
            }
        }
    }

    public static void RemoveBorderFromProjection(Item spawner, SpawnerBorder border)
    {
        if (_projectedSpawners.TryGetValue(spawner, out var borders))
        {
            borders.Remove(border);
            if (borders.Count == 0)
            {
                _projectedSpawners.Remove(spawner);
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class SpawnerBorder : ProjectedItem
{
    public override string DefaultName => "Spawn Border";

    [CommandProperty(AccessLevel.Developer, readOnly: true)]
    public Item Spawner { get; set; }

    [Constructible]
    public SpawnerBorder(int effectItemId) : base(effectItemId)
    {
        Hue = 0x3F;
        MinimumVisible = AccessLevel.Developer;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }

    protected override bool SendEffect()
    {
        // Check if spawner was deleted
        if (Spawner.Deleted)
        {
            ProjectSpawnerCommand.RemoveBorderFromProjection(Spawner, this);
            Spawner = null;
            Delete();
            return false;
        }

        return base.SendEffect();
    }
}
