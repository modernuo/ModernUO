using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class LadyLissith : GiantBlackWidow
    {
        [Constructible]
        public LadyLissith()
        {
            IsParagon = true;
            Hue = 0x452;

            SetStr(81, 130);
            SetDex(116, 152);
            SetInt(44, 100);

            SetHits(245, 375);
            SetStam(116, 152);
            SetMana(44, 100);

            SetDamage(15, 22);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 31, 39);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 71, 80);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Wrestling, 108.6, 123.0);
            SetSkill(SkillName.Tactics, 102.7, 119.0);
            SetSkill(SkillName.MagicResist, 78.8, 95.6);
            SetSkill(SkillName.Anatomy, 68.6, 106.8);
            SetSkill(SkillName.Poisoning, 96.6, 112.9);

            Fame = 18900;
            Karma = -18900;
        }

        public override string CorpseName => "a Lady Lissith corpse";
        public override string DefaultName => "Lady Lissith";

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );
    
          if (Utility.RandomDouble() < 0.025)
            c.DropItem( new GreymistChest() );
    
          if (Utility.RandomDouble() < 0.45)
            c.DropItem( new LissithsSilk() );
    
          if (Utility.RandomDouble() < 0.1)
            c.DropItem( new ParrotItem() );
        }
        */

        public override bool GivesMLMinorArtifact => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;
    }
}
