using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class EnslavedSatyr : Satyr
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

        public override string CorpseName => "an enslaved satyr corpse";
        public override string DefaultName => "an enslaved satyr";
    }
}
