using Server.Items;

namespace Server.Mobiles
{
  public class SpawnedOrcishLord : OrcishLord
  {
    [Constructible]
    public SpawnedOrcishLord()
    {
      Container pack = Backpack;

      pack?.Delete();

      NoKillAwards = true;
    }

    public SpawnedOrcishLord(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "an orcish corpse";

    public override void OnDeath(Container c)
    {
      base.OnDeath(c);

      c.Delete();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
      NoKillAwards = true;
    }
  }
}