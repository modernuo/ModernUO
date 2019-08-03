namespace Server.Items
{
  public class AsandosSatchel : Backpack
  {
    [Constructible]
    public AsandosSatchel()
    {
      Hue = Utility.RandomBrightHue();
      DropItem(new SackFlour());
      DropItem(new Skillet());
    }

    public AsandosSatchel(Serial serial)
      : base(serial)
    {
    }

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