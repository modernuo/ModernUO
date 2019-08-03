namespace Server.Items
{
  public class ReactiveArmorScroll : SpellScroll
  {
    [Constructible]
    public ReactiveArmorScroll(int amount = 1) : base(6, 0x1F2D, amount)
    {
    }

    public ReactiveArmorScroll(Serial ser) : base(ser)
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
