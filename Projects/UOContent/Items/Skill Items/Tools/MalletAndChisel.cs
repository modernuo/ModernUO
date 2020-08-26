using Server.Engines.Craft;

namespace Server.Items
{
  public class MalletAndChisel : BaseTool
  {
    [Constructible]
    public MalletAndChisel() : base(0x12B3) => Weight = 1.0;

    [Constructible]
    public MalletAndChisel(int uses) : base(uses, 0x12B3) => Weight = 1.0;

    public MalletAndChisel(Serial serial) : base(serial)
    {
    }

    public override CraftSystem CraftSystem => DefMasonry.CraftSystem;

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