using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class Satyr : BaseCreature
    {
        private static readonly Dictionary<Mobile, Timer> m_Suppressed = new();

        private DateTime m_NextPeace;

        private DateTime m_NextProvoke;
        private DateTime m_NextSuppress;

        private DateTime m_NextUndress;

        [Constructible]
        public Satyr() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 271;
            BaseSoundID = 0x586;

            SetStr(177, 195);
            SetDex(251, 269);
            SetInt(153, 170);

            SetHits(350, 400);

            SetDamage(13, 24);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55, 60);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 55.0, 65.0);
            SetSkill(SkillName.Tactics, 80.0, 100.0);
            SetSkill(SkillName.Wrestling, 80.0, 100.0);

            Fame = 5000;
            Karma = 0;

            VirtualArmor = 28; // Don't know what it should be

            PackArcanceScroll(0.05);
        }

        public Satyr(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a satyr's corpse";
        public override string DefaultName => "a satyr";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.MlRich);
        }

        public override void OnThink()
        {
            base.OnThink();

            Peace(Combatant);
            Undress(Combatant);
            Suppress(Combatant);
            Provoke(Combatant);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public void Peace(Mobile target)
        {
            if (target == null || Deleted || !Alive || m_NextPeace > Core.Now || Utility.RandomDouble() < 0.9)
            {
                return;
            }

            if (target is PlayerMobile p && p.PeacedUntil < Core.Now && !p.Hidden && CanBeHarmful(p))
            {
                p.PeacedUntil = Core.Now + TimeSpan.FromMinutes(1);
                p.SendLocalizedMessage(500616); // You hear lovely music, and forget to continue battling!
                p.FixedParticles(0x376A, 1, 32, 0x15BD, EffectLayer.Waist);
                p.Combatant = null;

                PlaySound(0x58D);
            }

            m_NextPeace = Core.Now + TimeSpan.FromSeconds(10);
        }

        public void Suppress(Mobile target)
        {
            if (target == null || m_Suppressed.ContainsKey(target) || Deleted || !Alive ||
                m_NextSuppress > Core.Now || Utility.RandomDouble() < 0.9)
            {
                return;
            }

            var delay = TimeSpan.FromSeconds(Utility.RandomMinMax(20, 80));

            if (!target.Hidden && CanBeHarmful(target))
            {
                target.SendLocalizedMessage(1072061); // You hear jarring music, suppressing your strength.

                for (var i = 0; i < target.Skills.Length; i++)
                {
                    var s = target.Skills[i];

                    target.AddSkillMod(new TimedSkillMod(s.SkillName, $"{s.Name}Satyr", true, s.Base * -0.28, delay));
                }

                var count = (int)Math.Round(delay.TotalSeconds / 1.25);
                Timer timer = new AnimateTimer(target, count);
                m_Suppressed.Add(target, timer);
                timer.Start();

                PlaySound(0x58C);
            }

            m_NextSuppress = Core.Now + TimeSpan.FromSeconds(10);
        }

        public static void SuppressRemove(Mobile target)
        {
            if (target == null)
            {
                return;
            }

            if (m_Suppressed.TryGetValue(target, out var t))
            {
                if (t.Running)
                {
                    t.Stop();
                }

                m_Suppressed.Remove(target);
            }
        }

        public void Undress(Mobile target)
        {
            if (target == null || Deleted || !Alive || m_NextUndress > Core.Now || Utility.RandomDouble() >= 0.005)
            {
                return;
            }

            if (target.Player && target.Female && !target.Hidden && CanBeHarmful(target))
            {
                UndressItem(target, Layer.OuterTorso);
                UndressItem(target, Layer.InnerTorso);
                UndressItem(target, Layer.MiddleTorso);
                UndressItem(target, Layer.Pants);
                UndressItem(target, Layer.Shirt);

                target.SendLocalizedMessage(
                    1072196
                ); // The satyr's music makes your blood race. Your clothing is too confining.
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

        public void Provoke(Mobile target)
        {
            if (target == null || Deleted || !Alive || m_NextProvoke > Core.Now || Utility.RandomDouble() < 0.95)
            {
                return;
            }

            foreach (var m in GetMobilesInRange(RangePerception))
            {
                if (m is BaseCreature c)
                {
                    if (c == this || c == target || c.Unprovokable || c.IsParagon || c.BardProvoked ||
                        c.AccessLevel != AccessLevel.Player || !c.CanBeHarmful(target))
                    {
                        continue;
                    }

                    c.Provoke(this, target, true);

                    if (target.Player)
                    {
                        target.SendLocalizedMessage(1072062); // You hear angry music, and start to fight.
                    }

                    PlaySound(0x58A);
                    break;
                }
            }

            m_NextProvoke = Core.Now + TimeSpan.FromSeconds(10);
        }

        private class AnimateTimer : Timer
        {
            private readonly Mobile m_Owner;
            private int m_Count;

            public AnimateTimer(Mobile owner, int count) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.25))
            {
                m_Owner = owner;
                m_Count = count;
            }

            protected override void OnTick()
            {
                if (m_Owner.Deleted || !m_Owner.Alive || m_Count-- < 0)
                {
                    SuppressRemove(m_Owner);
                }
                else
                {
                    m_Owner.FixedParticles(0x376A, 1, 32, 0x15BD, EffectLayer.Waist);
                }
            }
        }
    }
}
