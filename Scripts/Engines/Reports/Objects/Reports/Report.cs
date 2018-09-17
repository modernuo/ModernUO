namespace Server.Engines.Reports
{
  public class Report : PersistableObject
  {
    private Report() : this(null, null)
    {
    }

    public Report(string name, string width)
    {
      Name = name;
      Width = width;
      Columns = new ReportColumnCollection();
      Items = new ReportItemCollection();
    }

    public string Name{ get; set; }

    public string Width{ get; set; }

    public ReportColumnCollection Columns{ get; }

    public ReportItemCollection Items{ get; }

    public override void SerializeAttributes(PersistanceWriter op)
    {
      op.SetString("n", Name);
      op.SetString("w", Width);
    }

    public override void DeserializeAttributes(PersistanceReader ip)
    {
      Name = Utility.Intern(ip.GetString("n"));
      Width = Utility.Intern(ip.GetString("w"));
    }

    public override void SerializeChildren(PersistanceWriter op)
    {
      for (int i = 0; i < Columns.Count; ++i)
        Columns[i].Serialize(op);

      for (int i = 0; i < Items.Count; ++i)
        Items[i].Serialize(op);
    }

    public override void DeserializeChildren(PersistanceReader ip)
    {
      while (ip.HasChild)
      {
        PersistableObject child = ip.GetChild();

        if (child is ReportColumn column)
          Columns.Add(column);
        else if (child is ReportItem item)
          Items.Add(item);
      }
    }

    #region Type Identification

    public static readonly PersistableType ThisTypeID = new PersistableType("rp", Construct);

    private static PersistableObject Construct()
    {
      return new Report();
    }

    public override PersistableType TypeID => ThisTypeID;

    #endregion
  }
}