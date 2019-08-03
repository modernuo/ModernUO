namespace Server.Items
{
  public enum WaterState
  {
    Dead,
    Dying,
    Unhealthy,
    Healthy,
    Strong
  }

  public enum FoodState
  {
    Dead,
    Starving,
    Hungry,
    Full,
    Overfed
  }

  [PropertyObject]
  public class AquariumState
  {
    private int m_State;

    [CommandProperty(AccessLevel.GameMaster)]
    public int State
    {
      get => m_State;
      set
      {
        m_State = value;

        if (m_State < 0)
          m_State = 0;

        if (m_State > 4)
          m_State = 4;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Maintain{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Improve{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Added{ get; set; }

    public override string ToString()
    {
      return "...";
    }

    public virtual void Serialize(GenericWriter writer)
    {
      writer.Write(0); // version

      writer.Write(m_State);
      writer.Write(Maintain);
      writer.Write(Improve);
      writer.Write(Added);
    }

    public virtual void Deserialize(GenericReader reader)
    {
      int version = reader.ReadInt();

      m_State = reader.ReadInt();
      Maintain = reader.ReadInt();
      Improve = reader.ReadInt();
      Added = reader.ReadInt();
    }
  }
}