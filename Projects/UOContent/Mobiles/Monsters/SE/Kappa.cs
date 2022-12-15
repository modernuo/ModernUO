using ModernUO.Serialization;
using System;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Kappa : BaseCreature
    {
        [Constructible]
        public Kappa() : base(AIType.AI_Melee)
        {
            Body = 240;

            SetStr(186, 230);
            SetDex(51, 75);
            SetInt(41, 55);

            SetMana(30);

            SetHits(151, 180);

            SetDamage(6, 12);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 50);
            SetResistance(ResistanceType.Fire, 35, 50);
            SetResistance(ResistanceType.Cold, 25, 50);
            SetResistance(ResistanceType.Poison, 35, 50);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, 60.1, 70.0);
            SetSkill(SkillName.Tactics, 79.1, 89.0);
            SetSkill(SkillName.Wrestling, 60.1, 70.0);

            Fame = 1700;
            Karma = -1700;

            PackItem(new RawFishSteak(3));
            for (var i = 0; i < 2; i++)
            {
                switch (Utility.Random(6))
                {
                    case 0:
                        {
                            PackItem(new Gears());
                            break;
                        }
                    case 1:
                        {
                            PackItem(new Hinge());
                            break;
                        }
                    case 2:
                        {
                            PackItem(new Axle());
                            break;
                        }
                }
            }

            if (Core.ML && Utility.Random(3) == 0)
            {
                PackItem(Seed.RandomPeculiarSeed(4));
            }
        }

        public override string CorpseName => "a kappa corpse";
        public override string DefaultName => "a kappa";

        private static MonsterAbility[] _abilities = { MonsterAbilities.DrainLifeAttack };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.Average);
        }

        public override int GetAngerSound() => 0x50B;

        public override int GetIdleSound() => 0x50A;

        public override int GetAttackSound() => 0x509;

        public override int GetHurtSound() => 0x50C;

        public override int GetDeathSound() => 0x508;

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            // Acid Blood ability
            if (from?.Map != null)
            {
                var amt = 0;
                Mobile target = this;
                var rand = Utility.Random(1, 100);
                if (willKill)
                {
                    amt = ((rand % 5) >> 2) + 3;
                }

                if (Hits < 100 && rand < 21)
                {
                    target = rand % 2 < 1 ? this : from;
                    amt++;
                }

                if (amt > 0)
                {
                    SpillAcid(target, amt);
                    from.SendLocalizedMessage(1070820); // The creature spills a pool of acidic slime!
                    if (Mana > 14)
                    {
                        Mana -= 15;
                    }
                }
            }

            base.OnDamage(amount, from, willKill);
        }

        public override Item NewHarmfulItem() => new PoolOfAcid(TimeSpan.FromSeconds(10), 5, 10)
        {
            Name = "slime"
        };
    }
}
