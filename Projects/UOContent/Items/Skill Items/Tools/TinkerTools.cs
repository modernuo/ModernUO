using Server.Engines.Craft;

namespace Server.Items
{
  [Flippable(0x1EB8, 0x1EB9)]
  public class TinkerTools : BaseTool
  {
    [Constructible]
    public TinkerTools() : base(0x1EB8) => Weight = 1.0;

    [Constructible]
    public TinkerTools(int uses) : base(uses, 0x1EB8) => Weight = 1.0;

    public TinkerTools(Serial serial) : base(serial)
    {
    }

    public override CraftSystem CraftSystem => DefTinkering.CraftSystem;

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

  public class TinkersTools : BaseTool
  {
    [Constructible]
    public TinkersTools()
      : base(0x1EBC) =>
      Weight = 1.0;

    [Constructible]
    public TinkersTools(int uses)
      : base(uses, 0x1EBC) =>
      Weight = 1.0;

    public TinkersTools(Serial serial)
      : base(serial)
    {
    }

    public override CraftSystem CraftSystem => DefTinkering.CraftSystem;

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