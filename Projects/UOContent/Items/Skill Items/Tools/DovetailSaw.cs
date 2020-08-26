using Server.Engines.Craft;

namespace Server.Items
{
  [Flippable(0x1028, 0x1029)]
  public class DovetailSaw : BaseTool
  {
    [Constructible]
    public DovetailSaw() : base(0x1028) => Weight = 2.0;

    [Constructible]
    public DovetailSaw(int uses) : base(uses, 0x1028) => Weight = 2.0;

    public DovetailSaw(Serial serial) : base(serial)
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

      if (Weight == 1.0)
        Weight = 2.0;
    }
  }
}