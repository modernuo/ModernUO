using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class KhaldunSummoner : BaseCreature
    {
        [Constructible]
        public KhaldunSummoner() : base(AIType.AI_Mage)
        {
            Body = 0x190;
            Title = "the Summoner";

            SetStr(351, 400);
            SetDex(101, 150);
            SetInt(502, 700);

            SetHits(421, 480);

            SetDamage(5, 15);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Cold, 25);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 25, 30);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.Wrestling, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Meditation, 120.1, 130.0);

            VirtualArmor = 36;
            Fame = 10000;
            Karma = -10000;

            var gloves = new LeatherGloves();
            gloves.Hue = 0x66D;
            AddItem(gloves);

            var helm = new BoneHelm();
            helm.Hue = 0x835;
            AddItem(helm);

            var necklace = new Necklace();
            necklace.Hue = 0x66D;
            AddItem(necklace);

            var cloak = new Cloak();
            cloak.Hue = 0x66D;
            AddItem(cloak);

            var kilt = new Kilt();
            kilt.Hue = 0x66D;
            AddItem(kilt);

            var sandals = new Sandals();
            sandals.Hue = 0x66D;
            AddItem(sandals);
        }

        public override bool ClickTitle => false;
        public override bool ShowFameTitle => false;

        public override string DefaultName => "Zealot of Khaldun";

        public override bool AlwaysMurderer => true;
        public override bool Unprovokable => true;

        public override int GetIdleSound() => 0x184;

        public override int GetAngerSound() => 0x286;

        public override int GetDeathSound() => 0x288;

        public override int GetHurtSound() => 0x19F;

        public override bool OnBeforeDeath()
        {
            var rm = new BoneMagi();
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
