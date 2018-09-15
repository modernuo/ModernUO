using System.IO;

namespace Server.Engines.Reports
{
  public class SnapshotHistory : PersistableObject
  {
    public SnapshotHistory()
    {
      Snapshots = new SnapshotCollection();
    }

    public SnapshotCollection Snapshots{ get; set; }

    public void Save()
    {
      string path = Path.Combine(Core.BaseDirectory, "reportHistory.xml");
      PersistanceWriter pw = new XmlPersistanceWriter(path, "Stats");

      pw.WriteDocument(this);

      pw.Close();
    }

    public void Load()
    {
      string path = Path.Combine(Core.BaseDirectory, "reportHistory.xml");

      if (!File.Exists(path))
        return;

      PersistanceReader pr = new XmlPersistanceReader(path, "Stats");

      pr.ReadDocument(this);

      pr.Close();
    }

    public override void SerializeChildren(PersistanceWriter op)
    {
      for (int i = 0; i < Snapshots.Count; ++i)
        Snapshots[i].Serialize(op);
    }

    public override void DeserializeChildren(PersistanceReader ip)
    {
      while (ip.HasChild)
        Snapshots.Add(ip.GetChild() as Snapshot);
    }

    #region Type Identification

    public static readonly PersistableType ThisTypeID = new PersistableType("sh", Construct);

    private static PersistableObject Construct()
    {
      return new SnapshotHistory();
    }

    public override PersistableType TypeID => ThisTypeID;

    #endregion
  }
}