using ModernUO.Serialization;
using System;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MeerCaptain : BaseCreature
    {
        private DateTime m_NextAbilityTime;

        [Constructible]
        public MeerCaptain() : base(AIType.AI_Archer, FightMode.Evil)
        {
            Body = 773;

            SetStr(96, 110);
            SetDex(186, 200);
            SetInt(96, 110);

            SetHits(58, 66);

            SetDamage(5, 15);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 35, 45);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.Archery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 91.0, 100.0);
            SetSkill(SkillName.Swords, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 91.0, 100.0);
            SetSkill(SkillName.Wrestling, 80.9, 89.9);

            Fame = 2000;
            Karma = 5000;

            VirtualArmor = 28;

            Container pack = new Backpack();

            pack.DropItem(new Bolt(Utility.RandomMinMax(10, 20)));
            pack.DropItem(new Bolt(Utility.RandomMinMax(10, 20)));

            AddItem(
                Utility.Random(6) switch
                {
                    0 => new Longsword(),
                    1 => new Cutlass(),
                    2 => new Broadsword(),
                    3 => new Katana(),
                    4 => new Scimitar(),
                    _ => new VikingSword() // 5
                }
            );

            Container bag = new Bag();

            var count = Utility.RandomMinMax(10, 20);

            for (var i = 0; i < count; ++i)
            {
                var item = Loot.RandomReagent();

                if (item == null)
                {
                    continue;
                }

                if (!bag.TryDropItem(this, item, false))
                {
                    item.Delete();
                }
            }

            pack.DropItem(bag);

            AddItem(new Crossbow());
            PackItem(pack);

            m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));
        }

        public override string CorpseName => "a meer corpse";
        public override string DefaultName => "a meer captain";

        public override bool BardImmune => !Core.AOS;
        public override bool CanRummageCorpses => true;

        public override bool InitialInnocent => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }

        public override int GetHurtSound() => 0x14D;

        public override int GetDeathSound() => 0x314;

        public override int GetAttackSound() => 0x75;

        public override void OnThink()
        {
            if (Combatant != null && MagicDamageAbsorb < 1)
            {
                MagicDamageAbsorb = Utility.RandomMinMax(5, 7);
                FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);
                PlaySound(0x1E9);
            }

            if (Core.Now >= m_NextAbilityTime)
            {
                m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(10, 15));

                var eable = GetMobilesInRange(8);

                foreach (var m in eable)
                {
                    if (m is not MeerWarrior || !IsFriend(m) || !CanBeBeneficial(m) || m.Hits >= m.HitsMax || m.Poisoned ||
                        MortalStrike.IsWounded(m))
                    {
                        continue;
                    }

                    DoBeneficial(m);

                    var toHeal = Utility.RandomMinMax(20, 30);

                    SpellHelper.Turn(this, m);

                    m.Heal(toHeal, this);

                    m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
                    m.PlaySound(0x202);
                }
            }

            base.OnThink();
        }
    }
}
