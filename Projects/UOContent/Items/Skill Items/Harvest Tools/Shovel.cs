using Server.Engines.Harvest;

namespace Server.Items
{
  public class Shovel : BaseHarvestTool
  {
    [Constructible]
    public Shovel(int uses = 50) : base(0xF39, uses) => Weight = 5.0;

    public Shovel(Serial serial) : base(serial)
    {
    }

    public override HarvestSystem HarvestSystem => Mining.System;

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
