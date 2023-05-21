using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class CrystalSeaSerpent : SeaSerpent
    {
        [Constructible]
        public CrystalSeaSerpent()
        {
            Hue = 0x47E;

            SetStr(250, 450);
            SetDex(100, 150);
            SetInt(90, 190);

            SetHits(230, 330);

            SetDamage(10, 18);

            SetDamageType(ResistanceType.Physical, 10);
            SetDamageType(ResistanceType.Cold, 45);
            SetDamageType(ResistanceType.Energy, 45);

            SetResistance(ResistanceType.Physical, 50, 70);
            SetResistance(ResistanceType.Fire, 0);
            SetResistance(ResistanceType.Cold, 70, 90);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 60, 80);
        }

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );
    
          if (Utility.RandomDouble() < 0.05)
            c.DropItem( new CrushedCrystals() );
    
          if (Utility.RandomDouble() < 0.1)
            c.DropItem( new IcyHeart() );
    
          if (Utility.RandomDouble() < 0.1)
            c.DropItem( new LuckyDagger() );
        }
        */

        public override string CorpseName => "a crystal sea serpent corpse";
        public override string DefaultName => "a crystal sea serpent";
    }
}
