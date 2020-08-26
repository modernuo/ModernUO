using Server.Engines.Craft;

namespace Server.Items
{
  [Flippable(0xE8A, 0xE89)]
  public class Blowpipe : BaseTool
  {
    [Constructible]
    public Blowpipe() : base(0xE8A)
    {
      Weight = 4.0;
      Hue = 0x3B9;
    }

    [Constructible]
    public Blowpipe(int uses) : base(uses, 0xE8A)
    {
      Weight = 4.0;
      Hue = 0x3B9;
    }

    public Blowpipe(Serial serial) : base(serial)
    {
    }

    public override CraftSystem CraftSystem => DefGlassblowing.CraftSystem;

    public override int LabelNumber => 1044608; // blow pipe

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
        Weight = 4.0;
    }
  }
}