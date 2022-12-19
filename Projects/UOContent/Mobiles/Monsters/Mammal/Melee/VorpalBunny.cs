using ModernUO.Serialization;
using System;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class VorpalBunny : BaseCreature
    {
        [Constructible]
        public VorpalBunny() : base(AIType.AI_Melee)
        {
            Body = 205;
            Hue = 0x480;

            SetStr(15);
            SetDex(2000);
            SetInt(1000);

            SetHits(2000);
            SetStam(500);
            SetMana(0);

            SetDamage(1);

            SetDamageType(ResistanceType.Physical, 100);

            SetSkill(SkillName.MagicResist, 200.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 1000;
            Karma = 0;

            VirtualArmor = 4;

            var carrots = Utility.RandomMinMax(5, 10);
            PackItem(new Carrot(carrots));

            if (Utility.Random(5) == 0)
            {
                PackItem(new BrightlyColoredEggs());
            }

            PackStatue();

            DelayBeginTunnel();
        }

        public override string CorpseName => "a vorpal bunny corpse";
        public override string DefaultName => "a vorpal bunny";

        public override int Meat => 1;
        public override int Hides => 1;
        public override bool BardImmune => !Core.AOS;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich, 2);
        }

        public virtual void DelayBeginTunnel()
        {
            Timer.StartTimer(TimeSpan.FromMinutes(3.0), BeginTunnel);
        }

        public virtual void BeginTunnel()
        {
            if (Deleted)
            {
                return;
            }

            new BunnyHole().MoveToWorld(Location, Map);

            Frozen = true;
            Say("* The bunny begins to dig a tunnel back to its underground lair *");
            PlaySound(0x247);

            Timer.StartTimer(TimeSpan.FromSeconds(5.0), Delete);
        }

        public override int GetAttackSound() => 0xC9;

        public override int GetHurtSound() => 0xCA;

        public override int GetDeathSound() => 0xCB;

        [SerializationGenerator(0, false)]
        public partial class BunnyHole : Item
        {
            public BunnyHole() : base(0x913)
            {
                Movable = false;
                Hue = 1;

                Timer.StartTimer(TimeSpan.FromSeconds(40.0), Delete);
            }

            public override string DefaultName => "a mysterious rabbit hole";

            [AfterDeserialization(false)]
            private void AfterDeserialization()
            {
                Delete();
            }
        }
    }
}
