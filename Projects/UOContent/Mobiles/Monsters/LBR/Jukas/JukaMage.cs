using System;
using Server.Engines.Plants;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
    public class JukaMage : BaseCreature
    {
        private DateTime m_NextAbilityTime;

        [Constructible]
        public JukaMage() : base(AIType.AI_Mage)
        {
            Body = 765;

            SetStr(201, 300);
            SetDex(71, 90);
            SetInt(451, 500);

            SetHits(121, 180);

            SetDamage(4, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 30);
            SetResistance(ResistanceType.Fire, 35, 45);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.Anatomy, 80.1, 90.0);
            SetSkill(SkillName.EvalInt, 80.2, 100.0);
            SetSkill(SkillName.Magery, 99.1, 100.0);
            SetSkill(SkillName.Meditation, 80.2, 100.0);
            SetSkill(SkillName.MagicResist, 140.1, 150.0);
            SetSkill(SkillName.Tactics, 80.1, 90.0);
            SetSkill(SkillName.Wrestling, 80.1, 90.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 16;

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

            PackItem(bag);

            PackItem(new ArcaneGem());

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(4));
            }

            m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));
        }

        public JukaMage(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a jukan corpse";
        public override string DefaultName => "a juka mage";

        public override bool AlwaysMurderer => true;
        public override bool CanRummageCorpses => true;
        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
            AddLoot(LootPack.MedScrolls, 2);
        }

        public override int GetIdleSound() => 0x1AC;

        public override int GetAngerSound() => 0x1CD;

        public override int GetHurtSound() => 0x1D0;

        public override int GetDeathSound() => 0x28D;

        public override void OnThink()
        {
            if (Core.Now >= m_NextAbilityTime)
            {
                JukaLord toBuff = null;

                foreach (var m in GetMobilesInRange(8))
                {
                    if (m is JukaLord lord && IsFriend(lord) && lord.Combatant != null && CanBeBeneficial(lord) &&
                        lord.CanBeginAction<JukaMage>() && InLOS(lord))
                    {
                        toBuff = lord;
                        break;
                    }
                }

                if (toBuff != null)
                {
                    if (CanBeBeneficial(toBuff) && toBuff.BeginAction<JukaMage>())
                    {
                        m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));

                        toBuff.Say(true, "Give me the power to destroy my enemies!");
                        Say(true, "Fight well my lord!");

                        DoBeneficial(toBuff);

                        SpellHelper.Turn(this, toBuff);

                        var toScale = toBuff.HitsMaxSeed;

                        if (toScale > 0)
                        {
                            toBuff.HitsMaxSeed += AOS.Scale(toScale, 75);
                            toBuff.Hits += AOS.Scale(toScale, 75);
                        }

                        toScale = toBuff.RawStr;

                        if (toScale > 0)
                        {
                            toBuff.RawStr += AOS.Scale(toScale, 50);
                        }

                        toScale = toBuff.RawDex;

                        if (toScale > 0)
                        {
                            toBuff.RawDex += AOS.Scale(toScale, 50);
                            toBuff.Stam += AOS.Scale(toScale, 50);
                        }

                        toBuff.Hits = toBuff.Hits;
                        toBuff.Stam = toBuff.Stam;

                        toBuff.FixedParticles(0x375A, 10, 15, 5017, EffectLayer.Waist);
                        toBuff.PlaySound(0x1EE);
                        var maxHits = toBuff.HitsMaxSeed;
                        var rawStr = toBuff.RawStr;
                        var rawDex = toBuff.RawDex;

                        Timer.StartTimer(
                            TimeSpan.FromSeconds(20.0),
                            () => Unbuff(toBuff, maxHits, rawStr, rawDex)
                        );
                    }
                }
                else
                {
                    m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));
                }
            }

            base.OnThink();
        }

        private void Unbuff(JukaLord toDebuff, int hitsMaxSeed, int rawStr, int rawDex)
        {
            toDebuff.EndAction<JukaMage>();

            if (toDebuff.Deleted)
            {
                return;
            }

            toDebuff.HitsMaxSeed = hitsMaxSeed;
            toDebuff.RawStr = rawStr;
            toDebuff.RawDex = rawDex;

            toDebuff.Hits = toDebuff.Hits;
            toDebuff.Stam = toDebuff.Stam;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
