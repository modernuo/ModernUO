using Server.Spells.Second;

namespace Server.Items
{
  public class HarmWand : BaseWand
  {
    [Constructible]
    public HarmWand() : base(WandEffect.Harming, 5, Core.ML ? 109 : 30)
    {
    }

    public HarmWand(Serial serial) : base(serial)
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
      Cast(new HarmSpell(from, this));
    }
  }
}