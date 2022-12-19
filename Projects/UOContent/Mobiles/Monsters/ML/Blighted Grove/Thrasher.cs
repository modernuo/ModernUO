using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Thrasher : Alligator
    {
        [Constructible]
        public Thrasher()
        {
            IsParagon = true;

            Hue = 0x497;

            SetStr(93, 327);
            SetDex(7, 201);
            SetInt(15, 67);

            SetHits(260, 984);
            SetStam(56, 75);
            SetMana(25, 30);

            SetDamage(20, 30);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 50, 55);
            SetResistance(ResistanceType.Fire, 25, 29);
            SetResistance(ResistanceType.Poison, 25, 28);

            SetSkill(SkillName.Wrestling, 101.2, 118.3);
            SetSkill(SkillName.Tactics, 96.3, 117.3);
            SetSkill(SkillName.MagicResist, 102.4, 118.6);

            // TODO: Fame/Karma
        }

        public override string CorpseName => "a Thrasher corpse";
        public override string DefaultName => "Thrasher";

        public override bool GivesMLMinorArtifact => true;
        public override int Hides => 48;
        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 4);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.ArmorIgnore;

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            c.DropItem(new ThrashersTail());
        }
    }
}
