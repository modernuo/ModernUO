using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class KhaldunZealot : BaseCreature
    {
        [Constructible]
        public KhaldunZealot() : base(AIType.AI_Melee)
        {
            Body = 0x190;
            Title = "the Knight";
            Hue = 0;

            SetStr(351, 400);
            SetDex(151, 165);
            SetInt(76, 100);

            SetHits(448, 470);

            SetDamage(15, 25);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Cold, 25);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 25, 30);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.Wrestling, 70.1, 80.0);
            SetSkill(SkillName.Swords, 120.1, 130.0);
            SetSkill(SkillName.Anatomy, 120.1, 130.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);

            Fame = 10000;
            Karma = -10000;
            VirtualArmor = 40;

            var weapon = new VikingSword();
            weapon.Hue = 0x835;
            weapon.Movable = false;
            AddItem(weapon);

            var shield = new MetalShield();
            shield.Hue = 0x835;
            shield.Movable = false;
            AddItem(shield);

            var helm = new BoneHelm();
            helm.Hue = 0x835;
            AddItem(helm);

            var arms = new BoneArms();
            arms.Hue = 0x835;
            AddItem(arms);

            var gloves = new BoneGloves();
            gloves.Hue = 0x835;
            AddItem(gloves);

            var tunic = new BoneChest();
            tunic.Hue = 0x835;
            AddItem(tunic);

            var legs = new BoneLegs();
            legs.Hue = 0x835;
            AddItem(legs);

            AddItem(new Boots());
        }

        public override bool ClickTitle => false;
        public override bool ShowFameTitle => false;

        public override string DefaultName => "Zealot of Khaldun";

        public override bool AlwaysMurderer => true;
        public override bool Unprovokable => true;
        public override Poison PoisonImmune => Poison.Deadly;

        public override int GetIdleSound() => 0x184;

        public override int GetAngerSound() => 0x286;

        public override int GetDeathSound() => 0x288;

        public override int GetHurtSound() => 0x19F;

        public override bool OnBeforeDeath()
        {
            var rm = new BoneKnight();
            rm.Team = Team;
            rm.Combatant = Combatant;
            rm.NoKillAwards = true;

            if (rm.Backpack == null)
            {
                var pack = new Backpack();
                pack.Movable = false;
                rm.AddItem(pack);
            }

            for (var i = 0; i < 2; i++)
            {
                LootPack.FilthyRich.Generate(this, rm.Backpack, true, LootPack.GetLuckChanceForKiller(this));
                LootPack.FilthyRich.Generate(this, rm.Backpack, false, LootPack.GetLuckChanceForKiller(this));
            }

            Effects.PlaySound(this, GetDeathSound());
            Effects.SendLocationEffect(Location, Map, 0x3709, 30, 10, 0x835);
            rm.MoveToWorld(Location, Map);

            Delete();
            return false;
        }
    }
}
