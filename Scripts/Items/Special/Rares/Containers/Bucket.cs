namespace Server.Items
{
  internal class Bucket : BaseWaterContainer
  {
    private static int vItemID = 0x14e0;
    private static int fItemID = 0x2004;

    [Constructible]
    public Bucket()
      : this(false)
    {
    }

    [Constructible]
    public Bucket(bool filled)
      : base(filled ? fItemID : vItemID, filled)
    {
    }

    public Bucket(Serial serial)
      : base(serial)
    {
    }

    public override int voidItem_ID => vItemID;
    public override int fullItem_ID => fItemID;
    public override int MaxQuantity => 25;

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