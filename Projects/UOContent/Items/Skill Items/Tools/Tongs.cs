using Server.Engines.Craft;

namespace Server.Items
{
  [Flippable(0xfbb, 0xfbc)]
  public class Tongs : BaseTool
  {
    [Constructible]
    public Tongs() : base(0xFBB) => Weight = 2.0;

    [Constructible]
    public Tongs(int uses) : base(uses, 0xFBB) => Weight = 2.0;

    public Tongs(Serial serial) : base(serial)
    {
    }

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;

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