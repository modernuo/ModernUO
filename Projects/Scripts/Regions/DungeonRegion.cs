using System.Xml;

namespace Server.Regions
{
  public class DungeonRegion : BaseRegion
  {
    private Point3D m_EntranceLocation;

    public DungeonRegion(XmlElement xml, Map map, Region parent) : base(xml, map, parent)
    {
      XmlElement entrEl = xml["entrance"];

      Map entrMap = map;
      ReadMap(entrEl, "map", ref entrMap, false);

      if (ReadPoint3D(entrEl, entrMap, ref m_EntranceLocation, false))
        EntranceMap = entrMap;
    }

    public override bool YoungProtected => false;

    public Point3D EntranceLocation
    {
      get => m_EntranceLocation;
      set => m_EntranceLocation = value;
    }

    public Map EntranceMap{ get; set; }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
    {
      global = LightCycle.DungeonLevel;
    }

    public override bool CanUseStuckMenu(Mobile m)
    {
      if (Map == Map.Felucca)
        return false;

      return base.CanUseStuckMenu(m);
    }
  }
}