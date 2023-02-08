using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    partial class LadyMarai : SkeletalKnight
    {
        [Constructible]
        public LadyMarai()
        {
            IsParagon = true;

            Hue = 0x21;

            SetStr(221, 304);
            SetDex(98, 138);
            SetInt(54, 99);

            SetHits(694, 846);

            SetDamage(15, 25);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Cold, 60);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 70, 80);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 50, 60);

            SetSkill(SkillName.Wrestling, 126.6, 137.2);
            SetSkill(SkillName.Tactics, 128.7, 134.5);
            SetSkill(SkillName.MagicResist, 102.1, 119.1);
            SetSkill(SkillName.Anatomy, 126.2, 136.5);

            Fame = 18000;
            Karma = -18000;
        }

        public override string CorpseName => "a Lady Marai corpse";
        public override string DefaultName => "Lady Marai";

        /*
        // TODO: Uncomment once added
        public override void OnDeath( Container c )
        {
            base.OnDeath( c );
    
            if (Utility.RandomDouble() < 0.15)
            c.DropItem( new DisintegratingThesisNotes() );
    
            if (Utility.RandomDouble() < 0.1)
            c.DropItem( new ParrotItem() );
        }
        */

        public override bool GivesMLMinorArtifact => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 3);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.CrushingBlow;
    }
}
