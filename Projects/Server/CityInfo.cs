namespace Server
{
  public sealed class CityInfo
  {
    private Point3D m_Location;

    public CityInfo(string city, string building, int description, int x, int y, int z, Map m)
    {
      City = city;
      Building = building;
      Description = description;
      Location = new Point3D(x, y, z);
      Map = m;
    }

    public CityInfo(string city, string building, int x, int y, int z, Map m) : this(city, building, 0, x, y, z, m)
    {
    }

    public CityInfo(string city, string building, int description, int x, int y, int z) : this(city, building,
      description, x, y, z, Map.Trammel)
    {
    }

    public CityInfo(string city, string building, int x, int y, int z) : this(city, building, 0, x, y, z, Map.Trammel)
    {
    }

    public string City { get; set; }

    public string Building { get; set; }

    public int Description { get; set; }

    public int X
    {
      get => m_Location.X;
      set => m_Location.X = value;
    }

    public int Y
    {
      get => m_Location.Y;
      set => m_Location.Y = value;
    }

    public int Z
    {
      get => m_Location.Z;
      set => m_Location.Z = value;
    }

    public Point3D Location
    {
      get => Location;
      set => Location = value;
    }

    public Map Map { get; set; }
  }
}
