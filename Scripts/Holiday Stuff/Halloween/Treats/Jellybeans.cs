namespace Server.Items
{
  public class JellyBeans : CandyCane
  {
    [Constructible]
    public JellyBeans()
      : this(1)
    {
    }

    public JellyBeans(int amount)
      : base(0x468C)
    {
      Stackable = true;
    }

    public JellyBeans(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1096932; /* jellybeans */

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