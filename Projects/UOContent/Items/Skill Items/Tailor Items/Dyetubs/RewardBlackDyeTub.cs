using Server.Engines.VeteranRewards;

namespace Server.Items
{
  public class RewardBlackDyeTub : DyeTub, IRewardItem
  {
    [Constructible]
    public RewardBlackDyeTub()
    {
      Hue = DyedHue = 0x0001;
      Redyable = false;
      LootType = LootType.Blessed;
    }

    public RewardBlackDyeTub(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1006008; // Black Dye Tub

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem { get; set; }

    public override void OnDoubleClick(Mobile from)
    {
      if (IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        return;

      base.OnDoubleClick(from);
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      if (Core.ML && IsRewardItem)
        list.Add(1076217); // 1st Year Veteran Reward
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version

      writer.Write(IsRewardItem);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
          {
            IsRewardItem = reader.ReadBool();
            break;
          }
      }
    }
  }
}
