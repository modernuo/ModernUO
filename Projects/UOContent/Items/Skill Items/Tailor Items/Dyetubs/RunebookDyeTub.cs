using Server.Engines.VeteranRewards;

namespace Server.Items
{
  public class RunebookDyeTub : DyeTub, IRewardItem
  {
    [Constructible]
    public RunebookDyeTub() => LootType = LootType.Blessed;

    public RunebookDyeTub(Serial serial) : base(serial)
    {
    }

    public override bool AllowDyables => false;
    public override bool AllowRunebooks => true;
    public override int TargetMessage => 1049774; // Target the runebook or runestone to dye
    public override int FailMessage => 1049775; // You can only dye runestones or runebooks with this tub.
    public override int LabelNumber => 1049740; // Runebook Dye Tub
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
        list.Add(1076220); // 4th Year Veteran Reward
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
