using ModernUO.Serialization;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class OrcCaptain : BaseCreature
    {
        [Constructible]
        public OrcCaptain() : base(AIType.AI_Melee)
        {
            Name = NameList.RandomName("orc");
            Body = 7;
            BaseSoundID = 0x45A;

            SetStr(111, 145);
            SetDex(101, 135);
            SetInt(86, 110);

            SetHits(67, 87);

            SetDamage(5, 15);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 35);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Swords, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 85.1, 100.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 34;

            // TODO: Skull?
            PackItem(
                Utility.Random(7) switch
                {
                    0 => new Arrow(),
                    1 => new Lockpick(),
                    2 => new Shaft(),
                    3 => new Ribs(),
                    4 => new Bandage(),
                    5 => new BeverageBottle(BeverageType.Wine),
                    _ => new Jug(BeverageType.Cider) // 6
                }
            );

            if (Core.AOS)
            {
                PackItem(Loot.RandomNecromancyReagent());
            }
        }

        public override string CorpseName => "an orcish corpse";
        public override InhumanSpeech SpeechType => InhumanSpeech.Orc;

        public override bool CanRummageCorpses => true;
        public override int Meat => 1;

        public override OppositionGroup OppositionGroup => OppositionGroup.SavagesAndOrcs;

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            // TODO: Check drop rate
            if (Utility.RandomDouble() < 0.05)
            {
                c.DropItem(new StoutWhip());
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager, 2);
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
    }
}
