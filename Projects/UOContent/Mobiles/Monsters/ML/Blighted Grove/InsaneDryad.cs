namespace Server.Mobiles
{
  public class InsaneDryad : MLDryad
  {
    [Constructible]
    public InsaneDryad()
    {
      // TODO: Perhaps these should have negative karma?
    }

    /*
    // TODO: uncomment once added
    public override void OnDeath( Container c )
    {
      base.OnDeath( c );

      if (Utility.RandomDouble() < 0.1)
        c.DropItem( new ParrotItem() );
    }
    */

    public InsaneDryad(Serial serial)
      : base(serial)
    {
    }

    public override string CorpseName => "an insane dryad corpse";
    public override bool InitialInnocent => false;

    public override string DefaultName => "an insane dryad";

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
