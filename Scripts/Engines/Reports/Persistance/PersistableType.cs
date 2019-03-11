using System;
using System.Collections.Generic;

namespace Server.Engines.Reports
{
  public delegate PersistableObject ConstructCallback();

  public sealed class PersistableTypeRegistry
  {
    private static Dictionary<string, PersistableType> m_Table;

    static PersistableTypeRegistry()
    {
      m_Table = new Dictionary<string, PersistableType>(StringComparer.OrdinalIgnoreCase);

      Register(Report.ThisTypeID);
      Register(BarGraph.ThisTypeID);
      Register(PieChart.ThisTypeID);
      Register(Snapshot.ThisTypeID);
      Register(ItemValue.ThisTypeID);
      Register(ChartItem.ThisTypeID);
      Register(ReportItem.ThisTypeID);
      Register(ReportColumn.ThisTypeID);
      Register(SnapshotHistory.ThisTypeID);

      Register(PageInfo.ThisTypeID);
      Register(QueueStatus.ThisTypeID);
      Register(StaffHistory.ThisTypeID);
      Register(ResponseInfo.ThisTypeID);
    }

    public static PersistableType Find(string name)
    {
      m_Table.TryGetValue(name, out PersistableType value);
      return value;
    }

    public static void Register(PersistableType type)
    {
      if (type != null)
        m_Table[type.Name] = type;
    }
  }

  public sealed class PersistableType
  {
    public PersistableType(string name, ConstructCallback constructor)
    {
      Name = name;
      Constructor = constructor;
    }

    public string Name{ get; }

    public ConstructCallback Constructor{ get; }
  }
}
