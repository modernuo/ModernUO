using System;

namespace Server.Items
{
  public class BaseTreasureChest : LockableContainer
  {
    public enum TreasureLevel
    {
      Level1,
      Level2,
      Level3,
      Level4,
      Level5,
      Level6
    }

    private TreasureResetTimer m_ResetTimer;

    public BaseTreasureChest(int itemID) : this(itemID, TreasureLevel.Level2)
    {
    }

    public BaseTreasureChest(int itemID, TreasureLevel level) : base(itemID)
    {
      Level = level;
      Locked = true;
      Movable = false;

      SetLockLevel();
      GenerateTreasure();
    }

    public BaseTreasureChest(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public TreasureLevel Level{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public short MaxSpawnTime{ get; set; } = 60;

    [CommandProperty(AccessLevel.GameMaster)]
    public short MinSpawnTime{ get; set; } = 10;

    [CommandProperty(AccessLevel.GameMaster)]
    public override bool Locked
    {
      get => base.Locked;
      set
      {
        if (base.Locked != value)
        {
          base.Locked = value;

          if (!value)
            StartResetTimer();
        }
      }
    }

    public override bool IsDecoContainer => false;

    public override string DefaultName
    {
      get
      {
        if (Locked)
          return "a locked treasure chest";

        return "a treasure chest";
      }
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
      writer.Write((byte)Level);
      writer.Write(MinSpawnTime);
      writer.Write(MaxSpawnTime);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      Level = (TreasureLevel)reader.ReadByte();
      MinSpawnTime = reader.ReadShort();
      MaxSpawnTime = reader.ReadShort();

      if (!Locked)
        StartResetTimer();
    }

    protected virtual void SetLockLevel()
    {
      switch (Level)
      {
        case TreasureLevel.Level1:
          RequiredSkill = LockLevel = 5;
          break;

        case TreasureLevel.Level2:
          RequiredSkill = LockLevel = 20;
          break;

        case TreasureLevel.Level3:
          RequiredSkill = LockLevel = 50;
          break;

        case TreasureLevel.Level4:
          RequiredSkill = LockLevel = 70;
          break;

        case TreasureLevel.Level5:
          RequiredSkill = LockLevel = 90;
          break;

        case TreasureLevel.Level6:
          RequiredSkill = LockLevel = 100;
          break;
      }
    }

    private void StartResetTimer()
    {
      if (m_ResetTimer == null)
        m_ResetTimer = new TreasureResetTimer(this);
      else
        m_ResetTimer.Delay = TimeSpan.FromMinutes(Utility.Random(MinSpawnTime, MaxSpawnTime));

      m_ResetTimer.Start();
    }

    protected virtual void GenerateTreasure()
    {
      int MinGold = 1;
      int MaxGold = 2;

      switch (Level)
      {
        case TreasureLevel.Level1:
          MinGold = 100;
          MaxGold = 300;
          break;

        case TreasureLevel.Level2:
          MinGold = 300;
          MaxGold = 600;
          break;

        case TreasureLevel.Level3:
          MinGold = 600;
          MaxGold = 900;
          break;

        case TreasureLevel.Level4:
          MinGold = 900;
          MaxGold = 1200;
          break;

        case TreasureLevel.Level5:
          MinGold = 1200;
          MaxGold = 5000;
          break;

        case TreasureLevel.Level6:
          MinGold = 5000;
          MaxGold = 9000;
          break;
      }

      DropItem(new Gold(MinGold, MaxGold));
    }

    public void ClearContents()
    {
      for (int i = Items.Count - 1; i >= 0; --i)
        if (i < Items.Count)
          Items[i].Delete();
    }

    public void Reset()
    {
      if (m_ResetTimer != null)
        if (m_ResetTimer.Running)
          m_ResetTimer.Stop();

      Locked = true;
      ClearContents();
      GenerateTreasure();
    }

    private class TreasureResetTimer : Timer
    {
      private BaseTreasureChest m_Chest;

      public TreasureResetTimer(BaseTreasureChest chest) : base(
        TimeSpan.FromMinutes(Utility.Random(chest.MinSpawnTime, chest.MaxSpawnTime)))
      {
        m_Chest = chest;
        Priority = TimerPriority.OneMinute;
      }

      protected override void OnTick()
      {
        m_Chest.Reset();
      }
    }
  }
}