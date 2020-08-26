using Server.Engines.VeteranRewards;

namespace Server.Items
{
  public class LeatherDyeTub : DyeTub, IRewardItem
  {
    [Constructible]
    public LeatherDyeTub() => LootType = LootType.Blessed;

    public LeatherDyeTub(Serial serial) : base(serial)
    {
    }

    public override bool AllowDyables => false;
    public override bool AllowLeather => true;
    public override int TargetMessage => 1042416; // Select the leather item to dye.
    public override int FailMessage => 1042418; // You can only dye leather with this tub.
    public override int LabelNumber => 1041284; // Leather Dye Tub
    public override CustomHuePicker CustomHuePicker => CustomHuePicker.LeatherDyeTub;

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
        list.Add(1076218); // 2nd Year Veteran Reward
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
