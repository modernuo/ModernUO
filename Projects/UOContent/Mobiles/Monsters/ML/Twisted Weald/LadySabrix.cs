using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class LadySabrix : GiantBlackWidow
    {
        [Constructible]
        public LadySabrix()
        {
            IsParagon = true;
            Hue = 0x497;

            SetStr(82, 130);
            SetDex(117, 146);
            SetInt(50, 98);

            SetHits(233, 361);
            SetStam(117, 146);
            SetMana(50, 98);

            SetDamage(15, 22);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 39);
            SetResistance(ResistanceType.Poison, 70, 80);
            SetResistance(ResistanceType.Energy, 35, 44);

            SetSkill(SkillName.Wrestling, 109.8, 122.8);
            SetSkill(SkillName.Tactics, 102.8, 120.0);
            SetSkill(SkillName.MagicResist, 79.4, 95.1);
            SetSkill(SkillName.Anatomy, 68.8, 105.1);
            SetSkill(SkillName.Poisoning, 97.8, 116.7);

            Fame = 18900;
            Karma = -18900;
        }

        public override string CorpseName => "a Lady Sabrix corpse";
        public override string DefaultName => "Lady Sabrix";

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );
    
          if (Utility.RandomDouble() < 0.2)
            c.DropItem( new SabrixsEye() );
    
          if (Utility.RandomDouble() < 0.25)
          {
            switch ( Utility.Random( 2 ) )
            {
              case 0: AddToBackpack( new PaladinArms() ); break;
              case 1: AddToBackpack( new HunterLegs() ); break;
            }
          }
    
          if (Utility.RandomDouble() < 0.1)
            c.DropItem( new ParrotItem() );
        }
        */

        public override bool GivesMLMinorArtifact => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.ArmorIgnore;
    }
}
