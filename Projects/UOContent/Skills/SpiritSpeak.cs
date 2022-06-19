using System;
using Server.Items;
using Server.Network;
using Server.Spells;

namespace Server.SkillHandlers
{
    public static class SpiritSpeak
    {
        public static void Initialize()
        {
            SkillInfo.Table[32].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (Core.AOS)
            {
                Spell spell = new SpiritSpeakSpell(m);

                spell.Cast();

                if (spell.IsCasting)
                {
                    return TimeSpan.FromSeconds(5.0);
                }

                return TimeSpan.Zero;
            }

            m.RevealingAction();

            if (m.CheckSkill(SkillName.SpiritSpeak, 0, 100))
            {
                if (!m.CanHearGhosts)
                {
                    Timer t = new SpiritSpeakTimer(m);
                    var secs = m.Skills.SpiritSpeak.Base / 50;
                    secs *= 90;
                    if (secs < 15)
                    {
                        secs = 15;
                    }

                    t.Delay = TimeSpan.FromSeconds(secs); // 15seconds to 3 minutes
                    t.Start();
                    m.CanHearGhosts = true;
                }

                m.PlaySound(0x24A);
                m.SendLocalizedMessage(502444); // You contact the neitherworld.
            }
            else
            {
                m.SendLocalizedMessage(502443); // You fail to contact the neitherworld.
                m.CanHearGhosts = false;
            }

            return TimeSpan.FromSeconds(1.0);
        }

        private class SpiritSpeakTimer : Timer
        {
            private readonly Mobile m_Owner;

            public SpiritSpeakTimer(Mobile m) : base(TimeSpan.FromMinutes(2.0))
            {
                m_Owner = m;
            }

            protected override void OnTick()
            {
                m_Owner.CanHearGhosts = false;
                m_Owner.SendLocalizedMessage(502445); // You feel your contact with the neitherworld fading.
            }
        }

        private class SpiritSpeakSpell : Spell
        {
            private static readonly SpellInfo m_Info = new("Spirit Speak", "", 269);

            public SpiritSpeakSpell(Mobile caster) : base(caster, null, m_Info)
            {
            }

            public override bool BlockedByHorrificBeast => false;

            public override bool ClearHandsOnCast => false;

            public override double CastDelayFastScalar => 0;
            public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

            public override bool CheckNextSpellTime => false;

            public override int GetMana() => 0;

            public override void OnCasterHurt()
            {
                if (IsCasting)
                {
                    Disturb(DisturbType.Hurt, false, true);
                }
            }

            public override bool ConsumeReagents() => true;

            public override bool CheckFizzle() => true;

            public override void OnDisturb(DisturbType type, bool message)
            {
                Caster.NextSkillTime = Core.TickCount;

                base.OnDisturb(type, message);
            }

            public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable)
            {
                if (type is DisturbType.EquipRequest or DisturbType.UseRequest)
                {
                    return false;
                }

                return true;
            }

            public override void SayMantra()
            {
                // Anh Mi Sah Ko
                Caster.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1062074, "", false);
                Caster.PlaySound(0x24A);
            }

            public override void OnCast()
            {
                var eable = Caster.GetItemsInRange<Corpse>(3);
                Corpse toChannel = null;
                foreach (var corpse in eable)
                {
                    if (!corpse.Channeled)
                    {
                        toChannel = corpse;
                        break;
                    }
                }
                eable.Free();

                var min = 1 + (int)(Caster.Skills.SpiritSpeak.Value * 0.25);
                var max = min + 4;

                int mana, number;

                if (toChannel != null)
                {
                    mana = 0;
                    number = 1061287; // You channel energy from a nearby corpse to heal your wounds.
                }
                else
                {
                    mana = 10;
                    number = 1061286; // You channel your own spiritual energy to heal your wounds.
                }

                if (Caster.Mana < mana)
                {
                    Caster.SendLocalizedMessage(1061285); // You lack the mana required to use this skill.
                }
                else
                {
                    Caster.CheckSkill(SkillName.SpiritSpeak, 0.0, 120.0);

                    if (Utility.RandomDouble() > Caster.Skills.SpiritSpeak.Value / 100.0)
                    {
                        Caster.SendLocalizedMessage(502443); // You fail your attempt at contacting the netherworld.
                    }
                    else
                    {
                        if (toChannel != null)
                        {
                            toChannel.Channeled = true;
                            toChannel.Hue = 0x835;
                        }

                        Caster.Mana -= mana;
                        Caster.SendLocalizedMessage(number);

                        if (min > max)
                        {
                            min = max;
                        }

                        Caster.Hits += Utility.RandomMinMax(min, max);

                        Caster.FixedParticles(0x375A, 1, 15, 9501, 2100, 4, EffectLayer.Waist);
                    }
                }

                FinishSequence();
            }
        }
    }
}
