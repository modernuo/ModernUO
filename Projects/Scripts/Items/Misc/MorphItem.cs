using System;
using System.Linq;

namespace Server.Items
{
  public class MorphItem : Item
  {
    private int m_InRange;
    private int m_OutRange;

    [Constructible]
    public MorphItem(int inactiveItemID, int activeItemID, int range) : this(inactiveItemID, activeItemID, range, range)
    {
    }

    [Constructible]
    public MorphItem(int inactiveItemID, int activeItemID, int inRange, int outRange) : base(inactiveItemID)
    {
      Movable = false;

      InactiveItemID = inactiveItemID;
      ActiveItemID = activeItemID;
      InRange = inRange;
      OutRange = outRange;
    }

    public MorphItem(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int InactiveItemID{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ActiveItemID{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int InRange
    {
      get => m_InRange;
      set
      {
        if (value > 18) value = 18;
        m_InRange = value;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int OutRange
    {
      get => m_OutRange;
      set
      {
        if (value > 18) value = 18;
        m_OutRange = value;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int CurrentRange => ItemID == InactiveItemID ? InRange : OutRange;

    public override bool HandlesOnMovement => true;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
      if (Utility.InRange(m.Location, Location, CurrentRange) || Utility.InRange(oldLocation, Location, CurrentRange))
        Refresh();
    }

    public override void OnMapChange()
    {
      if (!Deleted)
        Refresh();
    }

    public override void OnLocationChange(Point3D oldLoc)
    {
      if (!Deleted)
        Refresh();
    }

    public void Refresh()
    {
      bool found = GetMobilesInRange(CurrentRange).Any(mob => !mob.Hidden || mob.AccessLevel <= AccessLevel.Player);
      ItemID = found ? ActiveItemID : InactiveItemID;

      Visible = ItemID != 0x1;
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version

      writer.Write(m_OutRange);

      writer.Write(InactiveItemID);
      writer.Write(ActiveItemID);
      writer.Write(m_InRange);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
        {
          m_OutRange = reader.ReadInt();
          goto case 0;
        }
        case 0:
        {
          InactiveItemID = reader.ReadInt();
          ActiveItemID = reader.ReadInt();
          m_InRange = reader.ReadInt();

          if (version < 1)
            m_OutRange = m_InRange;

          break;
        }
      }

      Timer.DelayCall(TimeSpan.Zero, Refresh);
    }
  }
}
