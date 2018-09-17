using Server.Engines.Harvest;

namespace Server.Items
{
  public class Shovel : BaseHarvestTool
  {
    [Constructible]
    public Shovel() : this(50)
    {
    }

    [Constructible]
    public Shovel(int uses) : base(uses, 0xF39)
    {
      Weight = 5.0;
    }

    public Shovel(Serial serial) : base(serial)
    {
    }

    public override HarvestSystem HarvestSystem => Mining.System;

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