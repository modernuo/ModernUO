using System.Collections.Generic;

namespace Server.Engines.BulkOrders
{
  public abstract class BaseBOD : Item
  {
    private int m_AmountMax;
    private bool m_RequireExceptional;
    private BulkMaterialType m_Material;

    public static BulkMaterialType GetRandomMaterial(BulkMaterialType start, double[] chances)
    {
      double random = Utility.RandomDouble();

      for (int i = 0; i < chances.Length; ++i)
      {
        if (random < chances[i])
          return i == 0 ? BulkMaterialType.None : start + (i - 1);

        random -= chances[i];
      }

      return BulkMaterialType.None;
    }

    public BaseBOD(int hue, int amountMax, bool requireExeptional, BulkMaterialType material) : this()
    {
      Hue = hue;
      AmountMax = amountMax;
      RequireExceptional = requireExeptional;
      Material = material;
    }

    public BaseBOD() : base(Core.AOS ? 0x2258 : 0x14EF)
    {
      Weight = 1.0;
      LootType = LootType.Blessed;
    }

    public BaseBOD(Serial serial) : base(serial)
    {
    }

    public abstract bool Complete { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public sealed override int Hue { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int AmountMax
    {
      get => m_AmountMax;
      set { m_AmountMax = value; InvalidateProperties(); }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool RequireExceptional
    {
      get => m_RequireExceptional;
      set { m_RequireExceptional = value; InvalidateProperties(); }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public BulkMaterialType Material
    {
      get => m_Material;
      set { m_Material = value; InvalidateProperties(); }
    }

    public abstract RewardGroup GetRewardGroup();

    public abstract int ComputeGold();
    public abstract int ComputeFame();
    public abstract void EndCombine(Mobile from, Item item);

    public virtual void GetRewards(out Item reward, out int gold, out int fame)
    {
      gold = ComputeGold();
      fame = ComputeFame();

      List<RewardItem> rewards = ComputeRewards(false);

      reward = rewards.RandomElement()?.Construct();
    }

    public virtual List<RewardItem> ComputeRewards(bool full)
    {
      RewardGroup rewardGroup = GetRewardGroup();

      List<RewardItem> list = new List<RewardItem>();

      if (full)
      {
        for (int i = 0; i < rewardGroup?.Items.Length; ++i)
        {
          RewardItem reward = rewardGroup.Items[i];

          if (reward != null)
            list.Add(reward);
        }
      }
      else
      {
        RewardItem reward = rewardGroup.AcquireItem();

        if (reward != null)
          list.Add(reward);
      }

      return list;
    }

    public virtual void BeginCombine(Mobile from)
    {
      if (Complete)
        from.SendLocalizedMessage(1045166); // The maximum amount of requested items have already been combined to this deed.
      else
        from.Target = new BODTarget(this);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version

      writer.Write(m_AmountMax);
      writer.Write(m_RequireExceptional);
      writer.Write((int)m_Material);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();

      switch (version)
      {
        case 0:
          {
            m_AmountMax = reader.ReadInt();
            m_RequireExceptional = reader.ReadBool();
            m_Material = (BulkMaterialType)reader.ReadInt();
            break;
          }
      }

      if (Parent == null && Map == Map.Internal && Location == Point3D.Zero)
        Delete();
    }
  }
}
