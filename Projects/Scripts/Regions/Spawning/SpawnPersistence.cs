namespace Server.Regions
{
  [TypeAlias("Server.Regions.SpawnPersistance")]
  public class SpawnPersistence : Item
  {
    private static SpawnPersistence m_Instance;

    private SpawnPersistence() : base(1) => Movable = false;

    public SpawnPersistence(Serial serial) : base(serial) => m_Instance = this;

    public SpawnPersistence Instance => m_Instance;

    public override string DefaultName => "Region spawn persistence - Internal";

    public static void EnsureExistence()
    {
      if (m_Instance == null)
        m_Instance = new SpawnPersistence();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version

      writer.Write(SpawnEntry.Table.Values.Count);
      foreach (SpawnEntry entry in SpawnEntry.Table.Values)
      {
        writer.Write(entry.ID);

        entry.Serialize(writer);
      }
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();

      int count = reader.ReadInt();
      for (int i = 0; i < count; i++)
      {
        int id = reader.ReadInt();

        SpawnEntry entry = SpawnEntry.Table[id];

        if (entry != null)
          entry.Deserialize(reader, version);
        else
          SpawnEntry.Remove(reader, version);
      }
    }
  }
}