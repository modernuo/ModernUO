using Server.Engines.Craft;

namespace Server.Items
{
  [Flippable(0x0FBF, 0x0FC0)]
  public class ScribesPen : BaseTool
  {
    [Constructible]
    public ScribesPen() : base(0x0FBF) => Weight = 1.0;

    [Constructible]
    public ScribesPen(int uses) : base(uses, 0x0FBF) => Weight = 1.0;

    public ScribesPen(Serial serial) : base(serial)
    {
    }

    public override CraftSystem CraftSystem => DefInscription.CraftSystem;

    public override int LabelNumber => 1044168; // scribe's pen

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (Weight == 2.0)
        Weight = 1.0;
    }
  }
}