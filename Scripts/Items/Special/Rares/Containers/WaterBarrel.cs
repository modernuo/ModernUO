namespace Server.Items
{
  internal class WaterBarrel : BaseWaterContainer
  {
    private static int vItemID = 0xe77;
    private static int fItemID = 0x154d;

    [Constructible]
    public WaterBarrel(bool filled = false)
      : base(filled ? fItemID : vItemID, filled)
    {
    }

    public WaterBarrel(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1025453; /* water barrel */

    public override int voidItem_ID => vItemID;
    public override int fullItem_ID => fItemID;
    public override int MaxQuantity => 100;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
