using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class JukaLord : BaseCreature
    {
        [Constructible]
        public JukaLord() : base(AIType.AI_Archer, FightMode.Closest, 10, 3)
        {
            Body = 766;

            SetStr(401, 500);
            SetDex(81, 100);
            SetInt(151, 200);

            SetHits(241, 300);

            SetDamage(10, 12);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 45, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 20, 25);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Anatomy, 90.1, 100.0);
            SetSkill(SkillName.Archery, 95.1, 100.0);
            SetSkill(SkillName.Healing, 80.1, 100.0);
            SetSkill(SkillName.MagicResist, 120.1, 130.0);
            SetSkill(SkillName.Swords, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 95.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 28;

            Container pack = new Backpack();

            pack.DropItem(new Arrow(Utility.RandomMinMax(25, 35)));
            pack.DropItem(new Arrow(Utility.RandomMinMax(25, 35)));
            pack.DropItem(new Bandage(Utility.RandomMinMax(5, 15)));
            pack.DropItem(new Bandage(Utility.RandomMinMax(5, 15)));
            pack.DropItem(Loot.RandomGem());
            pack.DropItem(new ArcaneGem());

            PackItem(pack);

            AddItem(new JukaBow());

            // TODO: Bandage self
        }

        public override string CorpseName => "a jukan corpse";
        public override string DefaultName => "a juka lord";

        public override bool AlwaysMurderer => true;
        public override bool BardImmune => !Core.AOS;
        public override bool CanRummageCorpses => true;
        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (!willKill && amount > 5 && from?.Player == true && Utility.Random(100) < 5)
            {
                string[] toSay =
                {
                    "{0}!!  You will have to do better than that!",
                    "{0}!!  Prepare to meet your doom!",
                    "{0}!!  My armies will crush you!",
                    "{0}!!  You will pay for that!"
                };

                Say(true, string.Format(toSay.RandomElement(), from.Name));
            }

            base.OnDamage(amount, from, willKill);
        }

        public override int GetIdleSound() => 0x262;

        public override int GetAngerSound() => 0x263;

        public override int GetHurtSound() => 0x1D0;

        public override int GetDeathSound() => 0x28D;
    }
}
