using ModernUO.Serialization;
using System;
using Server.Engines.Plants;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MLDryad : BaseCreature
    {
        private DateTime m_NextPeace;

        private DateTime m_NextUndress;

        [Constructible]
        public MLDryad() : base(AIType.AI_Mage, FightMode.Evil)
        {
            Body = 266;
            BaseSoundID = 0x57B;

            SetStr(132, 149);
            SetDex(152, 168);
            SetInt(251, 280);

            SetHits(304, 321);

            SetDamage(11, 20);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 40, 45);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.Meditation, 80.0, 90.0);
            SetSkill(SkillName.EvalInt, 70.0, 80.0);
            SetSkill(SkillName.Magery, 70.0, 80.0);
            SetSkill(SkillName.Anatomy, 0);
            SetSkill(SkillName.MagicResist, 100.0, 120.0);
            SetSkill(SkillName.Tactics, 70.0, 80.0);
            SetSkill(SkillName.Wrestling, 70.0, 80.0);

            Fame = 5000;
            Karma = 5000;

            VirtualArmor = 28; // Don't know what it should be

            if (Core.ML && Utility.RandomDouble() < .60)
            {
                PackItem(Seed.RandomPeculiarSeed(1));
            }

            PackArcanceScroll(0.05);
        }

        public override string CorpseName => "a dryad's corpse";
        public override bool InitialInnocent => true;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override string DefaultName => "a dryad";

        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.MlRich);
        }

        public override void OnThink()
        {
            base.OnThink();

            AreaPeace();
            AreaUndress();
        }

        public void AreaPeace()
        {
            if (Combatant == null || Deleted || !Alive || m_NextPeace > Core.Now || Utility.RandomDouble() < 0.9)
            {
                return;
            }

            var duration = TimeSpan.FromSeconds(Utility.RandomMinMax(20, 80));

            foreach (var m in GetMobilesInRange(RangePerception))
            {
                if (m is PlayerMobile pm && IsValidTarget(pm))
                {
                    pm.PeacedUntil = Core.Now + duration;
                    m.SendLocalizedMessage(1072065); // You gaze upon the dryad's beauty, and forget to continue battling!
                    m.FixedParticles(0x376A, 1, 20, 0x7F5, EffectLayer.Waist);
                    m.Combatant = null;
                }
            }

            m_NextPeace = Core.Now + TimeSpan.FromSeconds(10);
            PlaySound(0x1D3);
        }

        public bool IsValidTarget(PlayerMobile m) =>
            m?.PeacedUntil < Core.Now && !m.Hidden && m.AccessLevel == AccessLevel.Player &&
            CanBeHarmful(m);

        public void AreaUndress()
        {
            if (Combatant == null || Deleted || !Alive || m_NextUndress > Core.Now || Utility.RandomDouble() >= 0.005)
            {
                return;
            }

            foreach (var m in GetMobilesInRange(RangePerception))
            {
                if (m?.Player == true && !m.Female && !m.Hidden && m.AccessLevel == AccessLevel.Player &&
                    CanBeHarmful(m))
                {
                    UndressItem(m, Layer.OuterTorso);
                    UndressItem(m, Layer.InnerTorso);
                    UndressItem(m, Layer.MiddleTorso);
                    UndressItem(m, Layer.Pants);
                    UndressItem(m, Layer.Shirt);

                    m.SendLocalizedMessage(
                        1072197
                    ); // The dryad's beauty makes your blood race. Your clothing is too confining.
                }
            }

            m_NextUndress = Core.Now + TimeSpan.FromMinutes(1);
        }

        public void UndressItem(Mobile m, Layer layer)
        {
            var item = m.FindItemOnLayer(layer);

            if (item?.Movable == true)
            {
                m.PlaceInBackpack(item);
            }
        }
    }
}
