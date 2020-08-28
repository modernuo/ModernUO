namespace Server.Mobiles
{
    public class EnslavedSatyr : Satyr
    {
        [Constructible]
        public EnslavedSatyr()
        {
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

        public EnslavedSatyr(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "an enslaved satyr corpse";
        public override string DefaultName => "an enslaved satyr";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
