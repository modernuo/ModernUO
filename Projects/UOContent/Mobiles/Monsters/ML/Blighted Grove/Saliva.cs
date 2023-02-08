using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Saliva : Harpy
    {
        [Constructible]
        public Saliva()
        {
            // TODO: Not a paragon? No ML arties?
            // It moves like a paragon on OSI...

            Hue = 0x11E;

            SetStr(110, 206);
            SetDex(123, 222);
            SetInt(80, 127);

            SetHits(409, 842);

            SetDamage(20, 22);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 46, 48);
            SetResistance(ResistanceType.Fire, 32, 40);
            SetResistance(ResistanceType.Cold, 34, 49);
            SetResistance(ResistanceType.Poison, 40, 48);
            SetResistance(ResistanceType.Energy, 35, 39);

            SetSkill(SkillName.Wrestling, 106.4, 128.8);
            SetSkill(SkillName.Tactics, 129.9, 141.0);
            SetSkill(SkillName.MagicResist, 84.3, 105.0);

            // TODO: Fame/Karma?
        }

        public override string CorpseName => "a Saliva corpse";
        public override string DefaultName => "Saliva";

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            c.DropItem(new SalivasFeather());

            // TODO: uncomment once added
            // if (Utility.RandomDouble() < 0.1)
            // c.DropItem( new ParrotItem() );
        }
    }
}
