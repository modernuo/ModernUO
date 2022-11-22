namespace Server.Regions;

// TODO: Implement
public class DoomGuardianRegion : DungeonRegion
{
    public DoomGuardianRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public DoomGuardianRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }
}
