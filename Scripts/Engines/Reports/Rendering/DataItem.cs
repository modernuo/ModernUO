using System.Collections;
using System.Drawing;

namespace Server.Engines.Reports
{
  public class DataItem
  {
    private DataItem()
    {
    }

    public DataItem(string label, string desc, float data, float start, float sweep, Color clr)
    {
      Label = label;
      Description = desc;
      Value = data;
      StartPos = start;
      SweepSize = sweep;
      ItemColor = clr;
    }

    public string Label{ get; set; }

    public string Description{ get; set; }

    public float Value{ get; set; }

    public Color ItemColor{ get; set; }

    public float StartPos{ get; set; }

    public float SweepSize{ get; set; }
  }

  public class ChartItemsCollection : CollectionBase
  {
    public DataItem this[int index]
    {
      get => (DataItem)List[index];
      set => List[index] = value;
    }

    public int Add(DataItem value)
    {
      return List.Add(value);
    }

    public int IndexOf(DataItem value)
    {
      return List.IndexOf(value);
    }

    public bool Contains(DataItem value)
    {
      return List.Contains(value);
    }

    public void Remove(DataItem value)
    {
      List.Remove(value);
    }
  }
}