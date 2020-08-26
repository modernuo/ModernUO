using Server.Spells.First;

namespace Server.Items
{
  public class ClumsyWand : BaseWand
  {
    [Constructible]
    public ClumsyWand() : base(WandEffect.Clumsiness, 5, 30)
    {
    }

    public ClumsyWand(Serial serial) : base(serial)
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
      Cast(new ClumsySpell(from, this));
    }
  }
}