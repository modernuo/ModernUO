using Server.Spells.Fourth;

namespace Server.Items
{
  public class LightningWand : BaseWand
  {
    [Constructible]
    public LightningWand() : base(WandEffect.Lightning, 5, Core.ML ? 109 : 20)
    {
    }

    public LightningWand(Serial serial) : base(serial)
    {
    }

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

    public override void OnWandUse(Mobile from)
    {
      Cast(new LightningSpell(from, this));
    }
  }
}