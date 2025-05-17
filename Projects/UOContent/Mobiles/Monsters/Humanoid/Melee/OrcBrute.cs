using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class OrcBrute : BaseCreature
    {
        [Constructible]
        public OrcBrute() : base(AIType.AI_Melee)
        {
            Body = 189;
            BaseSoundID = 0x45A;

            SetStr(767, 945);
            SetDex(66, 75);
            SetInt(46, 70);

            SetHits(476, 552);

            SetDamage(20, 25);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.Macing, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 125.1, 140.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 50;

            PackItem(new ShadowIronOre(25)
            {
                ItemID = 0x19B9
            });
            PackItem(new IronIngot(10));

            if (Utility.RandomDouble() < 0.05)
            {
                PackItem(new OrcishKinMask());
            }

            if (Utility.RandomDouble() < 0.2)
            {
                PackItem(new BolaBall());
            }
        }

        public override string CorpseName => "an orcish corpse";
        public override string DefaultName => "an orc brute";

        public override bool BardImmune => !Core.AOS;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int Meat => 2;

        public override OppositionGroup OppositionGroup => OppositionGroup.SavagesAndOrcs;

        public override bool CanRummageCorpses => true;
        public override bool AutoDispel => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
        }

        public override bool IsEnemy(Mobile m) =>
            (!m.Player || m.FindItemOnLayer<OrcishKinMask>(Layer.Helm) == null) && base.IsEnemy(m);

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);

            if (aggressor.FindItemOnLayer(Layer.Helm) is OrcishKinMask item)
            {
                AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0);
                item.Delete();
                aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                aggressor.PlaySound(0x307);
            }
        }

        public override void OnDamagedBySpell(Mobile caster, int damage)
        {
            if (caster == this)
            {
                return;
            }

            SpawnOrcLord(caster);
        }

        public void SpawnOrcLord(Mobile target)
        {
            var map = target.Map;

            if (map == null || map == Map.Internal)
            {
                return;
            }

            var count = 0;
            foreach (var m in GetMobilesInRange<OrcishLord>(10))
            {
                if (++count == 10)
                {
                    return;
                }
            }

            var location = map.GetRandomNearbyLocation(target.Location);
            var orc = new SpawnedOrcishLord
            {
                Team = Team,
                Home = location,
                RangeHome = 10,
                Combatant = target
            };

            orc.MoveToWorld(location, map);
        }
    }
}
