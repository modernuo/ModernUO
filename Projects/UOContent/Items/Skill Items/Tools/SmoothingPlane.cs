using Server.Engines.Craft;

namespace Server.Items
{
  [Flippable(0x1032, 0x1033)]
  public class SmoothingPlane : BaseTool
  {
    [Constructible]
    public SmoothingPlane() : base(0x1032) => Weight = 1.0;

    [Constructible]
    public SmoothingPlane(int uses) : base(uses, 0x1032) => Weight = 1.0;

    public SmoothingPlane(Serial serial) : base(serial)
    {
    }

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

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