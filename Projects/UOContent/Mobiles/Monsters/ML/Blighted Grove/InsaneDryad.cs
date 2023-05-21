using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class InsaneDryad : MLDryad
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

        public override string CorpseName => "an insane dryad corpse";
        public override bool InitialInnocent => false;

        public override string DefaultName => "an insane dryad";
    }
}
