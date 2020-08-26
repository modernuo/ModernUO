namespace Server.Items
{
  public class GreaterConfusionBlastPotion : BaseConfusionBlastPotion
  {
    [Constructible]
    public GreaterConfusionBlastPotion() : base(PotionEffect.ConfusionBlastGreater)
    {
    }

    public GreaterConfusionBlastPotion(Serial serial) : base(serial)
    {
    }

    public override int Radius => 7;

    public override int LabelNumber => 1072108; // a Greater Confusion Blast potion

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}