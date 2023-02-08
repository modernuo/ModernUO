using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class InterredGrizzle : BaseCreature
    {
        [Constructible]
        public InterredGrizzle() : base(AIType.AI_Mage)
        {
            Body = 259;

            SetStr(451, 500);
            SetDex(201, 250);
            SetInt(801, 850);

            SetHits(1500);
            SetStam(150);

            SetDamage(16, 19);

            SetDamageType(ResistanceType.Physical, 30);
            SetDamageType(ResistanceType.Fire, 70);

            SetResistance(ResistanceType.Physical, 35, 55);
            SetResistance(ResistanceType.Fire, 20, 65);
            SetResistance(ResistanceType.Cold, 55, 80);
            SetResistance(ResistanceType.Poison, 20, 35);
            SetResistance(ResistanceType.Energy, 60, 80);

            SetSkill(SkillName.Meditation, 77.7, 84.0);
            SetSkill(SkillName.EvalInt, 72.2, 79.6);
            SetSkill(SkillName.Magery, 83.7, 89.6);
            SetSkill(SkillName.Poisoning, 0);
            SetSkill(SkillName.Anatomy, 0);
            SetSkill(SkillName.MagicResist, 80.2, 87.3);
            SetSkill(SkillName.Tactics, 104.5, 105.1);
            SetSkill(SkillName.Wrestling, 105.1, 109.4);

            Fame = 3700;   // Guessed
            Karma = -3700; // Guessed
        }
        /*
        public override bool OnBeforeDeath()
        {
          SpillAcid( 1, 4, 10, 6, 10 );

          return base.OnBeforeDeath();
        }
        */

        public override string CorpseName => "an interred grizzle corpse";
        public override string DefaultName => "a interred grizzle";

        public override void GenerateLoot() // -- Need to verify
        {
            AddLoot(LootPack.FilthyRich);
        }

        // TODO: Acid Blood
        /*
         * Message: 1070820
         * Spits pool of acid (blood, hue 0x3F), hits lost 6-10 per second/step
         * Damage is resistable (physical)
         * Acid last 10 seconds
         */

        public override int GetAngerSound() => 0x581;

        public override int GetIdleSound() => 0x582;

        public override int GetAttackSound() => 0x580;

        public override int GetHurtSound() => 0x583;

        public override int GetDeathSound() => 0x584;

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (Utility.RandomDouble() < 0.1)
            {
                DropOoze();
            }

            base.OnDamage(amount, from, willKill);
        }

        private int RandomPoint(int mid) => mid + Utility.RandomMinMax(-2, 2);

        public virtual Point3D GetSpawnPosition(int range) => GetSpawnPosition(Location, Map, range);

        public virtual Point3D GetSpawnPosition(Point3D from, Map map, int range)
        {
            if (map == null)
            {
                return from;
            }

            var loc = new Point3D(RandomPoint(X), RandomPoint(Y), Z);

            loc.Z = Map.GetAverageZ(loc.X, loc.Y);

            return loc;
        }

        public virtual void DropOoze()
        {
            var amount = Utility.RandomMinMax(1, 3);
            var corrosive = Utility.RandomBool();

            for (var i = 0; i < amount; i++)
            {
                Item ooze = new StainedOoze(corrosive);
                var p = new Point3D(Location);

                for (var j = 0; j < 5; j++)
                {
                    p = GetSpawnPosition(2);

                    var eable = Map.GetItemsInRange<StainedOoze>(p, 0);
                    using var enumerator = eable.GetEnumerator();
                    bool atLocation = enumerator.MoveNext();
                    eable.Free();
                    if (!atLocation)
                    {
                        break;
                    }
                }

                ooze.MoveToWorld(p, Map);
            }

            if (Combatant != null)
            {
                if (corrosive)
                {
                    Combatant.SendLocalizedMessage(1072071); // A corrosive gas seeps out of your enemy's skin!
                }
                else
                {
                    Combatant.SendLocalizedMessage(1072072); // A poisonous gas seeps out of your enemy's skin!
                }
            }
        }
    }
}
