namespace Server.Targeting;

public class StaticTarget : IPoint3D
{
    private Point3D m_Location;

    public StaticTarget(Point3D location, int itemID)
    {
        m_Location = location;
        ItemID = itemID & TileData.MaxItemValue;
        m_Location.Z += TileData.ItemTable[ItemID].CalcHeight;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point3D Location => m_Location;

    [CommandProperty(AccessLevel.Counselor)]
    public string Name => TileData.ItemTable[ItemID].Name;

    [CommandProperty(AccessLevel.Counselor)]
    public TileFlag Flags => TileData.ItemTable[ItemID].Flags;

    [CommandProperty(AccessLevel.Counselor)]
    public int ItemID { get; }

    [CommandProperty(AccessLevel.Counselor)]
    public int X => m_Location.X;

    [CommandProperty(AccessLevel.Counselor)]
    public int Y => m_Location.Y;

    [CommandProperty(AccessLevel.Counselor)]
    public int Z => m_Location.Z;
}
